using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SearchFlightsService.Containers;
using System.Collections;

namespace SearchFlightsService.Core
{
    public class FlightsToFareProcessor
    {
        //статический метод. преобразует перелеты в тарифы
        public static Fare[] FlightsToFares(Flight[] flights, bool is_one_way)
        {
            if (flights == null) return new Fare[0];


            foreach (Flight fl in flights)
                fl.Key = MakeFlightKey(fl);

            ArrayList flightsList = new ArrayList(flights);
            flightsList.Sort(new FlightsComparer());

            ArrayList faresList = new ArrayList();
            ArrayList flightsPack = new ArrayList();
            for (int i = 0; i < flights.Length; i++)
            {
                if (flightsPack.Count > 0)
                {
                    if ((flights[i].Price != flights[i - 1].Price) || (flights[i].Key != flights[i - 1].Key))
                    { 
                        //make fares from flights pack
                        Fare[] faresPack = ComposeFlights(flightsPack.ToArray(typeof(Flight)) as Flight[], is_one_way);
                        faresList.AddRange(faresPack);

                        flightsPack = new ArrayList();
                    }
                }
                flightsPack.Add(flights[i]);
            }

            Fare[] faresPack2 = ComposeFlights(flightsPack.ToArray(typeof(Flight)) as Flight[], is_one_way);
            faresList.AddRange(faresPack2);

            return faresList.ToArray(typeof(Fare)) as Fare[];
        }

        public class FlightComparer : IComparer
        {
            public int Compare(object A, object B)
            {
                Flight flA = A as Flight;
                Flight flB = B as Flight;

                if (flA.Price == flB.Price) return flA.Key.CompareTo(flB.Key);

                return flA.Price - flB.Price;
            }
        }

        private static string MakeFlightKey(Flight fl)
        {
            string res = "aw";
            if(!fl.Id.Contains("aw_"))
                res = "pb";

            return res + fl.AirlineCode;
        }

        //группу флайтов с одинаковой ценой, от одного поставщика и одной авиакомпании разбивает на группу тарифов
        private static Fare[] ComposeFlights(Flight[] flights, bool is_one_way)
        {
            //если пришел пустой массив
            if ((flights == null) || (flights.Length == 0)) return new Fare[0];

            //если пришли перелеты one-way
            if ((is_one_way) || ((flights.Length == 1)))
                //нечего стыковать, просто создаем тариф
                return new Fare[] { CreateFare(flights) };

            //немного уличной магии )
            //получаем множества вариантов вылета на каждом участке
            HashSet<DateTime> list_to = new HashSet<DateTime>();
            HashSet<DateTime> list_return = new HashSet<DateTime>();

            foreach (Flight fl in flights)
            { 
               list_to.Add(fl.Parts[0].Legs[0].DateBegin);
               list_return.Add(fl.Parts[1].Legs[0].DateBegin);
            }

            //выбираем меньшее по количеству вершин
            int active_dir = 0;
            int answer_dir = 1;
            DateTime[] act_lib = list_to.ToArray<DateTime>();
            DateTime[] answ_lib = list_return.ToArray<DateTime>();

            if (list_to.Count > list_return.Count)
            {
                active_dir = 1;
                answer_dir = 0;
                act_lib = list_return.ToArray<DateTime>();
                answ_lib = list_to.ToArray<DateTime>();
            }

            Dictionary<DateTime, int> act_Dict = MakeDictionary(act_lib);
            Dictionary<DateTime, int> answ_Dict = MakeDictionary(answ_lib);
            
            //делаем массив связей для каждой вершины
            HashSet<int>[] base_links = new HashSet<int>[act_Dict.Keys.Count];

            foreach (Flight fl in flights)
            {
                HashSet<int> set = base_links[act_Dict[fl.Parts[active_dir].Legs[0].DateBegin]];
                if (set == null)
                    set = new HashSet<int>();
                set.Add(answ_Dict[fl.Parts[answer_dir].Legs[0].DateBegin]);
                base_links[act_Dict[fl.Parts[active_dir].Legs[0].DateBegin]] = set;
            }
            //объединяем
            int[][] base_arr = ToIntIntArray(base_links);

            //если одна базовая вершина, ничего не комбинируем, просто создаем тариф
            if (base_links.Length == 1) return new Fare[] { CreateFare(flights) };

            int[][] sets = null;
            int[] powers = null;

            ArrayList fares = new ArrayList();
            ArrayList flArrList = new ArrayList(flights);

            while (BasePower(base_arr) > 0)
            {
                int[][] sochs_all = makeSochet(MakeArray(act_lib.Length), 0, ref sets, ref powers, ref base_arr);

                //выбираем максимальное сочетание
                int max_power = 0;
                int max_index = 0;
                for (int i = 0; i < powers.Length; i++)
                {
                    if (powers[i] > max_power)
                    {
                        max_power = powers[i];
                        max_index = i;
                    }
                }

                var set_args = sochs_all[max_index];
                var set_answ = sets[max_index];

                //корректируем base_arr
                correctBase(set_args, set_answ, ref base_arr);

                ArrayList flightsPack = new ArrayList();
                //создаем fare
                for (int i = 0; i < flArrList.Count; i++)
                { 
                    int arg_index = act_Dict[(flArrList[i] as Flight).Parts[active_dir].Legs[0].DateBegin];
                    int ans_index = answ_Dict[(flArrList[i] as Flight).Parts[answer_dir].Legs[0].DateBegin];

                    //проверяем, подходит ли флайт
                    if((set_args.Contains<int>(arg_index)) && (set_answ.Contains<int>(ans_index)))
                    {
                        flightsPack.Add(flArrList[i]);
                        flArrList.RemoveAt(i--);
                    }
                }

                fares.Add(CreateFare(flightsPack.ToArray(typeof(Flight)) as Flight[]));

               // break;
            }

            return fares.ToArray(typeof(Fare)) as Fare[];
        }

        private static void correctBase(int[] args, int[] answ, ref int[][] base_arr)
        {
            foreach (int arg in args)
            {
                var new_arr = base_arr[arg].Except(answ);

                base_arr[arg] = new_arr.Count<int>() > 0 ? new_arr.ToArray<int>() : null;
            }
        }

        private static int[][] makeSochet(int[] arr, int ind, ref int[][] sets, ref int[] powers, ref int[][] base_sets)
        {
            if (ind == arr.Length - 1)
            {
                sets = new int[][] { base_sets[ind] };

                powers = new int[] { base_sets[ind] == null ? 0 : base_sets[ind].Length };

                return new int[][] { new int[] { arr[ind] } };
            }

            int[][] old_soch = makeSochet(arr, ind + 1, ref sets, ref powers, ref base_sets);

            int item = arr[ind];
            int length = old_soch.Length;

            int[][] new_soch = new int[length * 2 + 1][];


            int[] new_power = new int[powers.Length * 2 + 1];
            int[][] new_sets = new int[sets.Length * 2 + 1][];


            old_soch.CopyTo(new_soch, 0);
            powers.CopyTo(new_power, 0);
            sets.CopyTo(new_sets, 0);


            new_soch[length] = new int[] { arr[ind] };
            new_power[length] = base_sets[ind] == null ? 0 : base_sets[ind].Length;
            new_sets[length] = base_sets[ind];

            int cnt = length;

            if (new_power[length] > 0)
                for (int i = 0; i < length; i++)
                {
                    if ((old_soch[i] == null) || (sets[i] == null))
                    {
                        cnt++;
                        continue;
                    }

                    int[] new_arr = new int[] { item };
                    var set_args = new_arr.Union(old_soch[i]);

                    var set_answ = base_sets[ind].Intersect(sets[i]).ToArray<int>();
                    new_power[++cnt] = set_answ.Length * (old_soch[i].Length + 1);

                    if (new_power[cnt] > 0)
                    {
                        new_soch[cnt] = set_args.ToArray<int>();
                        new_sets[cnt] = set_answ;
                    }
                }

            powers = new_power;
            sets = new_sets;

            return new_soch;
        }

        private static int BasePower(int[][] arr)
        {
            int cnt = 0;

            foreach (int[] item in arr)
                if (item != null)
                    cnt += item.Length;

            return cnt;
        }

        private static int[][] ToIntIntArray(HashSet<int>[] links)
        {
            int[][] res = new int[links.Length][];

            for (int i = 0; i < links.Length; i++)
                res[i] = links[i].ToArray<int>() as int[];

            return res;
        }

        private static Dictionary<DateTime, int> MakeDictionary(DateTime[] lib)
        {
            Dictionary<DateTime, int> dict = new Dictionary<DateTime, int>();

            for (int i = 0; i < lib.Length; i++)
                dict.Add(lib[i], i);

            return dict;
        }

        private static int[] MakeArray(int cnt)
        {
            int[] res = new int[cnt];
            for (int i = 0; i < cnt; i++)
                res[i] = i;

            return res;
        }

        //влайты объединяет в фэйр
        private static Fare CreateFare(Flight[] flights)
        {
            Fare fare = new Fare();
            Flight firstFlight = flights[0];

            fare.Airline = firstFlight.Airline;
            fare.AirlineCode = firstFlight.AirlineCode;
            fare.Id = firstFlight.Id + "*";
            fare.Price = firstFlight.Price;
            fare.Directions = new Direction[firstFlight.Parts.Length];

            Variant[][] comp_vars =  ComposeVariants(flights);

            for (int i = 0; i < firstFlight.Parts.Length; i++)
                fare.Directions[i] = new Direction(comp_vars[i]);

            return fare;
        }

        //флайты разбиваются на массивы вариантов для каждого отрезка
        private static Variant[][] ComposeVariants(Flight[] flights)
        {
            Variant[][] res = new Variant[flights[0].Parts.Length][];

            int dir_cnt = flights[0].Parts.Length;

            for (int i = 0; i < dir_cnt; i++)
                res[i] = new Variant[flights.Length];

            for (int i = 0; i < flights.Length; i++)
                for (int j = 0; j < dir_cnt; j++ )
                {
                    Variant vrnt = new Variant()
                    {
                        FlightTime = CalcVariantTime(flights[i].Parts[j].Legs),
                        Id = flights[i].Id,
                        Legs = flights[i].Parts[j].Legs,
                        Key = GetVariantKey(flights[i].Parts[j].Legs),
                        StartTime = GetVariantStartTime(flights[i].Parts[j].Legs[0])
                    };

                    res[j][i] = vrnt;
                }

            for (int i = 0; i < res.Length; i++ )
                res[i] = ZipVariants(res[i]);

            return res;
        }

        //класс сравнивает варианты для сортировки по времени вылета и ключу
        private class VariantsComparer : IComparer
        { 
            public int Compare(object A, object B)
            {
                Variant vA = A as Variant;
                Variant vB = B as Variant;

                if (vA.StartTime == vB.StartTime)
                    return vA.Key.CompareTo(vB.Key);

                return vA.StartTime - vB.StartTime;
            }
        }

        //"сжимаем" массив вариантов. сортируем и убираем повторяющиеся
        private static Variant[] ZipVariants(Variant[] variants)
        {
            ArrayList tmp = new ArrayList(variants);
            tmp.Sort(new VariantsComparer());

            for (int i = 0; i < tmp.Count; i++ )
            {
                if(i > 0)
                    if ((tmp[i] as Variant).Key == (tmp[i - 1] as Variant).Key)
                    {
                        (tmp[i-1] as Variant).Id += "$" + (tmp[i] as Variant).Id;
                        tmp.RemoveAt(i--);
                    }
            }

            return tmp.ToArray(typeof(Variant)) as Variant[];
        }

        //статический метод, объединяет массивы перелетов от разных поставщиков, выбирая меньшую цену
        public static Flight[] MergeFlights(Flight[][] flights_array)
        {
            //если массивов больше двух
            if(flights_array.Length > 2)
            {
                List<Flight[]> temp = flights_array.ToList();
                Flight[] last_array = flights_array[flights_array.Length -1];

                temp.RemoveAt(temp.Count-1);

                return MergeFlights(new Flight[][] { MergeFlights(temp.ToArray()), last_array });
            }

            //хотя бы один из них пустой
            ArrayList megaFlights = new ArrayList();      // Массив всех перелетов
            int services = 0;                             // Количество поставщиков

            foreach (Flight[] arr in flights_array)
            {
                if ((arr == null) || (arr.Length == 0)) continue;
                services++;
                megaFlights.AddRange(arr.ToList<Flight>());
            }

            //если меньше двух поставщиков, нечего объединять
            if (services < 2)
                return megaFlights.ToArray(typeof(Flight)) as Flight[];


            //если все-таки нужно объединить
            //в первый массив добавляем второй
            Dictionary<string, int> flightsDict = new Dictionary<string, int>();

            List<Flight> base_array = flights_array[0].ToList();

            int index = 0;
            foreach (Flight fl in base_array)
            {
                if (!flightsDict.ContainsKey(fl.FlightMask))
                    flightsDict.Add(fl.FlightMask, index++);
                else
                    index++;

            }

            foreach (Flight fl in flights_array[1])
            {
                if (flightsDict.ContainsKey(fl.FlightMask))
                {
                    if (fl.Price < base_array[flightsDict[fl.FlightMask]].Price)
                        base_array[flightsDict[fl.FlightMask]] = fl; //заменяем билет во временной библиотеке
                }
                else
                    base_array.Add(fl);
            }

            //ОГРАИЧИВАЕМ РАЗМЕР МАССИВА
            if (base_array.Count > 10000)
                base_array.Capacity = 10000;

            return base_array.ToArray();


            megaFlights.Sort(new FlightsComparer());

            //filter Flights
            ArrayList filteredFlights = new ArrayList();
            HashSet<string> names = new HashSet<string>();
            

            foreach (Flight fl in megaFlights)
            {
                string mask = fl.FlightMask;
                if (names.Contains(mask))
                    continue;
                else
                {
                    names.Add(mask);
                    filteredFlights.Add(fl);
                }
            }

            return filteredFlights.ToArray(typeof(Flight)) as Flight[];
        }

        //статический метод. преобразует перелет в тариф
        //метод-заглушка, создающий тариф для каждого перелета
        private static Fare FlightToFare(Containers.Flight flight)
        {
            Fare res = new Fare()
            {
                Airline = flight.Airline,
                AirlineCode = flight.AirlineCode,
                Id = flight.Id,
                Price = flight.Price,
                Directions = new Direction[flight.Parts.Length]
            };

            for (int i = 0; i < flight.Parts.Length; i++)
            {
                Direction dir = new Direction() { Variants = new Variant[1] };
                Variant vrnt = new Variant() { Id = flight.Id, Legs = flight.Parts[i].Legs, FlightTime = CalcVariantTime(flight.Parts[i].Legs) };
                dir.Variants[0] = vrnt;

                res.Directions[i] = dir;
            }

            return res;
        }

        //расчет продолжительности перелетов + ожиданий на всем участке маршрута
        private static string CalcVariantTime(Leg[] Legs)
        {
            int totalMins = 0;

            for (int i = 0; i < Legs.Length; i++)
            {
                if (i != 0)
                {
                    //calculate waiting time
                    if (Legs[i - 1].DateEnd > Legs[i].DateBegin)
                        Legs[i].DateBegin.AddDays(1);

                    TimeSpan ts = Legs[i].DateBegin - Legs[i - 1].DateEnd;
                    totalMins += Convert.ToInt32( ts.TotalMinutes );
                }
                totalMins += Legs[i].Duration;
            }

            TimeSpan tSpan = new TimeSpan(0, totalMins, 0);
          //  int hours = Convert.ToInt32(Math.Floor((decimal)totalMins / 60));
          //  int minutes = totalMins - hours * 60;

            return "" + Math.Floor( tSpan.TotalHours ) + " ч. " + ("00".Substring(0, 2 - tSpan.Minutes.ToString().Length)) + tSpan.Minutes + " мин.";
        }

        private static string GetVariantKey(Leg[] legs)
        {
            string key = "";

            foreach (Leg leg in legs)
                key +=  ""+ leg.DateBegin.Hour + leg.DateBegin.Minute + leg.BookingClass + leg.ServiceClass + leg.FlightNumber;

            return key;
        }

        private static int GetVariantStartTime(Leg leg)
        {
            return leg.DateBegin.Hour * 100 + leg.DateBegin.Minute;
        }
    }
}