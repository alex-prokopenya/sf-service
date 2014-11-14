using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.Services;
using SearchFlightsService.Containers;
using SearchFlightsService.Core;
using SearchFlightsService.Ext;
using System.Threading;
using System.Web.Configuration;

namespace SearchFlightsService
{
    /// <summary>
    /// Summary description for Service1
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class SF_service : System.Web.Services.WebService
    {
        public SF_service()
        {
            if (ConfigurationManager.AppSettings["TOTAL_COEF"] != null)
            {
                try
                {

                    TOTAL_COEF = Convert.ToDecimal(ConfigurationManager.AppSettings["TOTAL_COEF"]);
                }
                catch (Exception ex) { }
            }
        }

        private string secret_key = WebConfigurationManager.AppSettings["SecretKey"];

        private int searchResultTimeout = 1200; //время в секундах, в течение которого не будет делаться новый поиск при аналогичном запросе
        private int searchFullTimeout = 60; //время в секундах, в течение которого ожидается результат

        public static decimal TOTAL_COEF = 1;


        private int searchResultTimeout = 1200; //время в секундах, в течение которого не будет делаться новый поиск при аналогичном запросе
        private int searchFullTimeout = 60; //время в секундах, в течение которого ожидается результат

        //метод для проверки маршрута:
        //если маршрут строго по СНГ, не ведем поиск по энивэй

        private bool CheckRoute(Route route)
        {
            //список кодов в снг
            string[] sng_codes = new string[] { "AAQ", "ABA", "ACS", "ADH", "AER", "AKX", "ALA", "AMV", "ARH", "ASB", "ASF", "AYK",
                                                "AZN", "BAK", "BAX", "BCX", "BHK", "BQS", "BQT", "BTK", "BUS", "BWO", "BZK", "CEE", 
                                                "CEJ", "CEK", "CIT", "CKC", "CKH", "CNN", "CRZ", "CSY", "CWC", "CYX", "DMB", "DME", 
                                                "DNK", "DOK", "DYR", "DYU", "DZN", "EGO", "EIE", "ERD", "ESL", "FEG", "FRU", "GDG", 
                                                "GDX", "GDZ", "GME", "GNA", "GOJ", "GRV", "GUW", "GYD", "HMA", "HMJ", "HRK", "HTA", 
                                                "HTG", "IAA", "IAR", "IEV", "IFO", "IJK", "IKS", "IKT", "INA", "IRM", "IWA", "JOK", 
                                                "KBP", "KCP", "KEJ", "KGD", "KGF", "KGO", "KGP", "KHC", "KHE", "KHU", "KHV", "KJA", 
                                                "KLF", "KMW", "KOV", "KRO", "KRQ", "KRR", "KRW", "KSN", "KSQ", "KSZ", "KUF", "KUT", 
                                                "KVD", "KVX", "KWG", "KXK", "KYK", "KYZ", "KZN", "KZO", "LBD", "LDG", "LED", "LNX", 
                                                "LPK", "LWO", "MCX", "MJZ", "MMK", "MOW", "MPW", "MQF", "MRV", "MSQ", "MVQ", "MYP", 
                                                "NAL", "NBC", "NCU", "NEF", "NER", "NFG", "NJC", "NLV", "NMA", "NMN", "NOI", "NOJ", 
                                                "NOZ", "NSK", "NUX", "NVR", "NYM", "ODS", "OEL", "OGZ", "OHO", "OMS", "OSS", "OSW", 
                                                "OVB", "OZH", "PEE", "PES", "PEZ", "PKC", "PKV", "PLV", "PLX", "PPK", "PVS", "PWE", 
                                                "PWQ", "PYJ", "RAT", "REN", "ROV", "RTW", "RWN", "RYB", "RZN", "SCO", "SCW", "SGC", 
                                                "SIP", "SKD", "SKX", "SLY", "STW", "SUI", "SVO", "SVX", "SWT", "TAS", "TAZ", "TBS", 
                                                "TDK", "TJM", "TNL", "TOF", "TOX", "TSE", "TWB", "TYA", "UCK", "UCT", "UDJ", "UFA", 
                                                "UGC", "UIK", "UKK", "UKX", "ULY", "UMY", "URA", "URJ", "URS", "USK", "UUA", "UUD", 
                                                "UUS", "VGD", "VIN", "VKO", "VKT", "VLK", "VLU", "VOG", "VOZ", "VSG", "VTB", "VVO", "YKS", "ZTR" };

            bool is_sng = true;

            foreach (RouteSegment segment in route.Segments)
            {
                if ((sng_codes.Contains(segment.LocationBegin)) && (sng_codes.Contains(segment.LocationEnd)))
                    continue;
                else
                {
                    is_sng = false;
                    break;
                }
            }

            return is_sng;
        }

        //вариант поиска для момондо
        //отличия:-- поиск только по одному оператору
        //        -- исключаем из поиска базу визита
        //        -- замеряем время отклика (запись в лог-файл)
        [WebMethod]
        public SearchResultFlights Search_Full_Momondo(Route route, int adult, int children, int inf, string serviceClass, string is_rambler = "false")
        {
            bool is_one_way = route.Segments.Length == 1;

            ///вызов всех сервисов, поиск, получение результата, объединение, ответ
            string my_search_id = generateSearchId(route, adult, children, inf, serviceClass);

            //берем сохраненный результат в БД
            SearchResultFlights srf = GetCurrentResultsFlights(my_search_id);

            //если есть сохраненные в кэше перелеты
            if (srf != null)
            {
                Logger.Logger.WriteToTimeLog("cache: " + route.Segments[0].LocationBegin + " - " + route.Segments[0].LocationEnd + " " + route.Segments[0].Date.ToString("dd.MM"));
                return srf; //отдаем их
            }
            else
            {
                // DB.DataStore ds = new DB.DataStore(route, adult, children, inf, serviceClass, my_search_id, null, null, 0);
                // ds.SaveEmpty();
                // ds.Dispose();

                DateTime beg = DateTime.Now;

                int timer = 45;// searchFullTimeout;
                //если данных в кэше нет, делаем новый поиск

                //starting portbilet
                PortbiletService psrvc = new PortbiletService(route, adult, children, inf, serviceClass);
                Thread portObj = new Thread(new ThreadStart(psrvc.InitSearch));
                Flight[] flightsPort = null;


                AwadService awad_srvc = new AwadService();
                string awad_search_id = "";
                Flight[] flightsAwad = null;

                string system = "PORT";

                //если СНГ просто делаем поиск по портбилету

                bool check_route = Convert.ToBoolean(WebConfigurationManager.AppSettings["CheckRoute"]);
                int awad_rate = Convert.ToInt32(WebConfigurationManager.AppSettings["AWADPercentRate"]);


                if ((check_route) && (CheckRoute(route)))
                {
                    portObj.Start();
                    flightsAwad = new Flight[0];
                    Thread.Sleep(5000);
                    timer -= 5;
                }
                else
                {
                    Random rand = new Random();

                    if (rand.Next(100) > awad_rate)
                    {
                        portObj.Start();
                        flightsAwad = new Flight[0];
                        Thread.Sleep(5000);
                        timer -= 5;
                    }
                    else
                    {
                        system = "AWAD";

                        awad_search_id = awad_srvc.InitSearch(route, adult, children, inf, serviceClass);
                        flightsPort = new Flight[0];

                        //если осталось больше 35 секунд
                        if (getSeconds(beg) < 10)
                            Thread.Sleep(5000);
                    }
                }


                Flight[] flights = null;

                if (getSeconds(beg) < 25)
                {
                    while (true)
                    {
                        if (getSeconds(beg) > 42) //если прошло 45 секунд
                        {
                            flights = new Flight[0];
                            break;
                        }

                        //ждем секунду
                        Thread.Sleep(1000);

                        //если еще не получен результат, пробуем получить от Портбилет
                        if (flightsPort == null)
                            flights = psrvc.GetFlights(psrvc.SearchId);

                        //если еще не получен результат, пробуем получить от Энивэй
                        if (flightsAwad == null)
                            flights = awad_srvc.GetFlights(awad_search_id);

                        //если получили оба, уходим
                        if (flights != null) break;
                    }
                }
                else
                    flights = new Flight[0];


                SearchResultFlights res = new SearchResultFlights() { Flights = flights, IsFinished = 1, RequestId = my_search_id };

                if (flights.Length == 0)
                {
                    Logger.Logger.WriteToTimeLog(system + ": timeout; " + getSeconds(beg) + " sec; request: " + route.ToString());
                    return res;
                }

                //берем сохраненный результат в БД
                srf = GetCurrentResultsFlights(my_search_id);

                Logger.Logger.WriteToTimeLog("cache: " + route.Segments[0].LocationBegin + " - " + route.Segments[0].LocationEnd + " " + route.Segments[0].Date.ToString("dd.MM") + " " + getSeconds(beg) + " sec.");
                //если есть сохраненные в кэше перелеты
                if (srf != null) return srf; //отдаем их

                //  DateTime end = DateTime.Now;

                //new thread сохраняем в БД
                DB.DataStore ds = new DB.DataStore(route, adult, children, inf, serviceClass, my_search_id, null, res.Flights, 1);
                ds.route_id = my_search_id;
                ds.is_rambler = is_rambler;
                Thread threadObj = new Thread(new ThreadStart(ds.SaveResults));
                threadObj.Start();

                Logger.Logger.WriteToTimeLog(system + ": " + getSeconds(beg) + " sec; res " + flights.Length);
                return res;
            }
        }

        private double getSeconds(DateTime start)
        {
            return (DateTime.Now - start).TotalSeconds;
        }

        [WebMethod]
        public SearchResultFlights Search_Full(Route route, int adult, int children, int inf, string serviceClass, string is_rambler = "false")
        {
            bool is_one_way = route.Segments.Length == 1;

            ///вызов всех сервисов, поиск, получение результата, объединение, ответ
            string my_search_id = generateSearchId(route, adult, children, inf, serviceClass);

            //берем сохраненный результат в БД
            SearchResultFlights srf = GetCurrentResultsFlights(my_search_id);

            //если есть сохраненные в кэше перелеты
            if (srf != null)
                return srf; //отдаем их
            else
            {
                // DB.DataStore ds = new DB.DataStore(route, adult, children, inf, serviceClass, my_search_id, null, null, 0);
                // ds.SaveEmpty();
                // ds.Dispose();

                int timer = searchFullTimeout;
                //если данных в кэше нет, делаем новый поиск

                //starting portbilet
                PortbiletService psrvc = new PortbiletService(route, adult, children, inf, serviceClass);
                Thread portObj = new Thread(new ThreadStart(psrvc.InitSearch));
                portObj.Start();

                Flight[] flightsPort = null;


                //starting awad
                AwadService awad_srvc = new AwadService();
                //перед стартом поиска по энивэй проверяем маршрут
                string awad_search_id = "";
                Flight[] flightsAwad = null;

                if (!CheckRoute(route))
                    awad_search_id = awad_srvc.InitSearch(route, adult, children, inf, serviceClass);
                else
                    flightsAwad = new Flight[0];

                //starting vizit
                VizitService vsrvc = null;// new VizitService(adult, children, inf, serviceClass, route);
                //Thread vtObj = new Thread(new ThreadStart(vsrvc.InitSearch));
                //vtObj.Start();

                Flight[] flightsVizit = new Flight[0];

                while (true)
                {
                    //ждем секунду
                    Thread.Sleep(3000);

                    //если еще не получен результат, пробуем получить от Портбилет
                    if (flightsPort == null)
                        flightsPort = psrvc.GetFlights(psrvc.SearchId);

                    //если еще не получен результат, пробуем получить от Энивэй
                    if (flightsAwad == null)
                        flightsAwad = awad_srvc.GetFlights(awad_search_id);

                    //если еще не получен результат, пробуем получить от Визита
                    if (flightsVizit == null)
                        flightsVizit = vsrvc.GetFlights("");

                    //если получили оба, уходим
                    if ((flightsPort != null) && (flightsAwad != null) && (flightsVizit != null))
                        break;

                    if (--timer < 0) break;
                }

                //
                Flight[] flights = FlightsToFareProcessor.MergeFlights(new Flight[][] { flightsPort, flightsAwad, flightsVizit });

                //создаем объект ответа
                SearchResultFlights res = new SearchResultFlights() { Flights = flights, IsFinished = 1, RequestId = my_search_id };

                //flights группируем в fares
                Fare[] fares_arr = FlightsToFareProcessor.FlightsToFares(res.Flights, is_one_way);

                //берем сохраненный результат в БД
                srf = GetCurrentResultsFlights(my_search_id);

                //если есть сохраненные в кэше перелеты
                if (srf != null)
                    return srf; //отдаем их

                //new thread сохраняем в БД
                DB.DataStore ds = new DB.DataStore(route, adult, children, inf, serviceClass, my_search_id, fares_arr, res.Flights, 1);
                ds.route_id = my_search_id;
                ds.is_rambler = is_rambler;
                Thread threadObj = new Thread(new ThreadStart(ds.SaveResults));
                threadObj.Start();

                return res;
            }
        }

        [WebMethod]
        public SearchResultFlightsJson Search_Full_JSON(Route route, int adult, int children, int inf, string serviceClass, decimal course)
        {
            ///вызов всех сервисов, поиск, получение результата, объединение, ответ
            SearchResultFlights sr = Search_Full(route, adult, children, inf, serviceClass);

            return new SearchResultFlightsJson(sr.Flights, course);
        }

        [WebMethod]
        //функция возвращает первый полученный результат и ссылку на поиск для последующего обращения
        //данные передает в SOAP
        //возвращает набор из FARES
        public SearchResult Search_First(Route route, int adult, int children, int inf, string serviceClass, string my_search_id)
        {
            try
            {
                ///проверка, есть ли результаты в кэше
                ///вызов всех сервисов, поиск, получение первого результата
                ///регистрация запроса
                ///ответ

                bool is_one_way = route.Segments.Length == 1;

                if ((my_search_id == null) || (my_search_id == String.Empty))
                {
                    my_search_id = generateSearchId(route, adult, children, inf, serviceClass);

                    //check saved results
                    DB.DataStore dataStore = new DB.DataStore();
                    SearchResult sr = dataStore.GetFaresFromDB(my_search_id, DateTime.Now.AddSeconds(-1 * this.searchResultTimeout));

                    //if finded some fares, return it
                    if (sr != null) return sr;
                }
                //if have no saved results, make new search



                //начинаем поиск в энивэй
                AwadService awad_srvc = new AwadService();
                string awad_search_id = awad_srvc.InitSearch(route, adult, children, inf, serviceClass);

                PortbiletService psrvc = null;
                VizitService vsrvc = null;
                //начинаем поиск в портбилет
              psrvc = new PortbiletService(route, adult, children, inf, serviceClass);
                Thread portObj = new Thread(new ThreadStart(psrvc.InitSearch));
                portObj.Start();
               
                //начинаем поиск в визите
              vsrvc = new VizitService(adult, children, inf, serviceClass, route);
                Thread vtObj = new Thread(new ThreadStart(vsrvc.InitSearch));
                vtObj.Start();
              
                Flight[] res_flights = null;
                Fare[] res_fares = null;


                int timer = searchFullTimeout;
                bool is_awad = false;
                bool is_port = false;

                int vizit_finished = 0;
                int awad_finished = 0;
                int port_finished = 0;

                while (res_flights == null)
                {
                    Thread.Sleep(3000);

                    //берем найденные тарифы
                    if ((res_fares == null) && (awad_srvc != null))
                    {
                        res_fares = awad_srvc.Get_Fares(awad_search_id);

                        //если поиск в энивэй уже завершен
                        if (res_fares != null)
                        {
                            awad_finished = 1;
                            //fares переделываем в flights
                            if (res_fares.Length > 0)
                            {
                                res_flights = awad_srvc.FaresToFlights(res_fares);

                                is_awad = true;
                                is_port = false;
                                break;
                            }
                            else
                            {
                                awad_srvc = null;
                                res_fares = null;
                            }
                        }
                    }
                    //берем результаты от портбилет
                    if ((res_flights == null) && (psrvc != null))
                    {
                        res_flights = psrvc.GetFlights(psrvc.SearchId);

                        //если поиск уже завершен
                        if (res_flights != null)
                        {
                            port_finished = 1;
                            if (res_flights.Length > 0)
                            {
                                //ставим метку, что это не энивэй, а портбилет
                                is_awad = false;
                                is_port = true;
                                //уходим из цикла
                                break;
                            }
                            else
                            {
                                psrvc = null;
                                //  res_fares = null;
                                res_flights = null;
                            }
                        }
                    }

                    //берем результаты от Визита
                    if ((res_flights == null) && (vsrvc != null))
                    {
                        res_flights = vsrvc.GetFlights("");

                        //если поиск уже завершен
                        if (res_flights != null)
                        {
                            vizit_finished = 1;
                            if (res_flights.Length > 0)
                            {
                                //ставим метку, что это не энивэй
                                is_awad = false;
                                is_port = false;
                                //уходим из цикла
                                break;
                            }
                            else
                            {
                                vsrvc = null;
                                //  res_fares = null;
                                res_flights = null;
                            }
                        }
                    }

                    if (--timer < 0) break;
                }

                if (is_awad)//если первым был энивэй
                {
                    //отправляем ждать результаты от портбилет
                    Thread threadWaitFull = new Thread(() => WaitFinishSearch(new IExternalService[] { psrvc, vsrvc }, my_search_id, res_flights, is_one_way, adult, children, inf));
                    threadWaitFull.Start();
                }
                else if (is_port) //если поиск от портбилет завершен
                {
                    //ждем результатов от энивэя
                    Thread threadWaitFull = new Thread(() => WaitFinishSearch(new IExternalService[] { awad_srvc, vsrvc }, my_search_id, res_flights, is_one_way, adult, children, inf));
                    threadWaitFull.Start();

                    //группируем полученные от портбилета flights в fares
                    res_fares = FlightsToFareProcessor.FlightsToFares(res_flights, is_one_way);
                }
                else //если поиск от визита завершен
                {
                    //ждем результатов от энивэя
                    Thread threadWaitFull = new Thread(() => WaitFinishSearch(new IExternalService[] { awad_srvc, psrvc }, my_search_id, res_flights, is_one_way, adult, children, inf));
                    threadWaitFull.Start();

                    //группируем полученные от визита flights в fares
                    res_fares = FlightsToFareProcessor.FlightsToFares(res_flights, is_one_way);
                }
                //new thread
                //сохраняем полученные предварительные результаты в базу данных
                DB.DataStore ds = new DB.DataStore(route, adult, children, inf, serviceClass, my_search_id, res_fares, res_flights, awad_finished * port_finished * vizit_finished);
                ds.route_id = my_search_id;
                Thread threadObj = new Thread(new ThreadStart(ds.SaveResults));
                threadObj.Start();

                return new SearchResult(my_search_id, res_fares, 0);

            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog(ex.Message + " " + ex.StackTrace);
            }
            return null;
        }

        /// <summary>
        /// ждем окончания поиска
        /// </summary>
        private void WaitFinishSearch(IExternalService[] service, string searchId, Flight[] old_flights, bool is_one_way, int adl, int ch, int inf)
        {
            try
            {
                //если получили пустой объект с сервисом, уходим
                if (service == null) return;

                Flight[][] new_flights = new Flight[service.Length][];

                int timer = this.searchFullTimeout;

                while (true)
                {
                    //ждем секунду
                    Thread.Sleep(3000);

                    //проверяем результат
                    bool allFinished = true;
                    for (int i = 0; i < service.Length; i++)
                    {
                        //если сервис еще не отдавал перелеты
                        if (service[i] != null)
                        {
                            //пробуем забрать найденные перелеты
                            new_flights[i] = service[i].GetFlights(searchId);

                            //если поиск закончился и перелеты есть
                            if (new_flights[i] != null)
                                service[i] = null; //отключаемся от сервиса
                        }

                        //меняем флаг, проверяющий все ли сервисы закончили поиск
                        allFinished = allFinished && (service[i] == null);
                    }

                    //если поиск завершен, уходим
                    if (allFinished) break;

                    //проверяем таймер
                    if (--timer < 0) break;
                }

                Flight[] all_flights = null;
                int first_index = 0;

                //проходимся по циклу, ищем первый непустой результат
                for (int i = 0; i < new_flights.Length; i++)
                    if ((new_flights[i] != null) && (new_flights[i].Length > 0))
                    {
                        all_flights = new_flights[i];
                        first_index = i;
                    }

                //если ничего не найдено, или сработал таймер, просто уходим
                if ((all_flights == null) || (all_flights.Length == 0))
                {
                    DB.DataStore temp_ds = new DB.DataStore();
                    temp_ds.UpdateRequestState(searchId, 1, DateTime.Now.AddSeconds(-1 * this.searchResultTimeout));
                    return;
                }
                else
                {
                    //идем по циклу дальше, добавляем непустые результаты
                    for (int i = first_index + 1; i < new_flights.Length; i++)
                        if ((new_flights[i] != null) && (new_flights[i].Length > 0))
                            all_flights = FlightsToFareProcessor.MergeFlights(new Flight[][] { all_flights, new_flights[i] });
                }

                //merge flights
                //объединяем найденные перелеты с уже сохраненными
                all_flights = FlightsToFareProcessor.MergeFlights(new Flight[][] { old_flights, all_flights });

                //make fares
                //преобразуем перелеты в тарифы
                Fare[] all_fares = FlightsToFareProcessor.FlightsToFares(all_flights, is_one_way);

                //save flights and fares to DataBase
                //записываем резульаты в БД
                DB.DataStore ds = new DB.DataStore(null, adl, ch, inf, null, searchId, all_fares, all_flights, 1);
                ds.route_id = searchId;
                ds.UpdateResults();
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("!!!!!!!!!!!!!!!!!!!!!");
                Logger.Logger.WriteToLog("DB exception " + ex.Message + " " + ex.StackTrace);
            }
        }

        [WebMethod]
        public string InitSearch(Route route, int adult, int children, int inf, string serviceClass)
        {
            //генерируем новый серч айди
            string my_search_id = generateSearchId(route, adult, children, inf, serviceClass);

            //проверить, идет ли поиск
            DB.DataStore ds = new DB.DataStore();

            DateTime Timeout = DateTime.Now.AddSeconds(-1 * this.searchResultTimeout);

            int isFin = ds.GetRequestState(my_search_id, Timeout);

            //если поиск уже идет, просто отдаем айдишник
            if ((isFin == 0) || (isFin == 1)) return my_search_id;

            //если нужно запустить поиск
            //создаем в базе запись с пустыми результатами и статусом "поиск не завершен"
            ds = new DB.DataStore(route, adult, children, inf, serviceClass, my_search_id, new Fare[0], new Flight[0], 0);
            Thread saveThread = new Thread(new ThreadStart(ds.SaveEmpty));
            saveThread.Start();

            //стартуем новый поиск
            Thread searchThread = new Thread(() => Search_First(route, adult, children, inf, serviceClass, my_search_id));
            searchThread.Start();

            return my_search_id;
        }

        [WebMethod]
        //функция возвращает первый полученный результат и ссылку на поиск для последующего обращения
        //данные передает в JSON
        //возвращает набор из FARES
        public SearchResultJson Search_First_JSON(Route route, int adult, int children, int inf, string serviceClass, decimal course)
        {
            SearchResultJson result = new SearchResultJson();

            SearchResult soap_result = Search_First(route, adult, children, inf, serviceClass, "");
            result.setFares(soap_result.Fares, course);
            result.SearchId = soap_result.SearchId;
            result.IsFinished = soap_result.IsFinished;
            return result;
        }

        [WebMethod]
        //функция показывает, завершен ли поиск
        public int GetCurrentState(string requestId) //возвращает состояние поиска
        {
            DB.DataStore ds = new DB.DataStore();

            DateTime Timeout = DateTime.Now.AddSeconds(-1 * this.searchResultTimeout);

            int isFin = ds.GetRequestState(requestId, Timeout);

            if (isFin == -1) return 0;
            if (isFin == 1) return 100;
            return 50;
        }

        [WebMethod]
        //функция ожидает завершения поиска
        //данные передает в SOAP
        //возвращает набор из FARES
        public SearchResult GetCurrentResults(string requestId) //возвращает сохраненные цены в SOAP, цены в рублях
        {
            //ВНИМАНИЕ!!! Метод дожидается завершения поиска

            DB.DataStore ds = new DB.DataStore();

            DateTime Timeout = DateTime.Now.AddSeconds(-1 * this.searchResultTimeout);

            //запрашиваем состояние поиска
            int isFin = ds.GetRequestState(requestId, Timeout);

            //если такой поиск не стартовал или устарел, возвращаем null
            if (isFin == -1) return null;


            int timer = searchFullTimeout;

            //если поиск стартовал но не завершен, ждем
            while (isFin != 1)
            {
                if (--timer < 0) //если прошло время ожидания
                {
                    //сделать асинхронно !!!
                    ds.UpdateRequestState(requestId, 1, Timeout); //меняем состояние поиска на "завершен"
                    break; // уходим из цикла
                }
                isFin = ds.GetRequestState(requestId, Timeout); //уточняем состояние поиска
                Thread.Sleep(3000);
            }

            SearchResult sr = ds.GetFaresFromDB(requestId, Timeout); // забираем сохраненные билеты
            return sr;
        }

        [WebMethod]
        //функция ожидает завершения поиска
        //данные передает в SOAP
        //возвращает набор из FLIGHTS
        public SearchResultFlights GetCurrentResultsFlights(string requestId) //возвращает сохраненные цены в SOAP, цены в рублях
        {
            DB.DataStore ds = new DB.DataStore();

            DateTime Timeout = DateTime.Now.AddSeconds(-1 * this.searchResultTimeout);

            int isFin = ds.GetRequestState(requestId, Timeout);

            if (isFin == -1)
            {
                ds.Dispose();
                return null;
            }

            /*  int timer = searchFullTimeout;
              while (isFin != 1)
              {
                  if (--timer < 0)
                  {
                      ds.UpdateRequestState(requestId, 1, Timeout);
                      isFin = 1;
                      break;
                  }
                  isFin = ds.GetRequestState(requestId, Timeout);
                  Thread.Sleep(1000);
              }
              */


            SearchResultFlights srf = ds.GetFlightsFromDB(requestId, Timeout);
            ds.Dispose();

            return srf;
        }

        [WebMethod]
        //функция ожидает завершения поиска
        //данные передает в JSON
        //возвращает набор из FARES
        public SearchResultJson GetCurrentResultsJson(string requestId, decimal course) //сохраненные цены, посчитанные по курсу. Формат JSON
        {
            SearchResultJson result = new SearchResultJson();

            SearchResult soap_result = GetCurrentResults(requestId);

            if (soap_result == null) return null;

            result.setFares(soap_result.Fares, course);
            result.SearchId = soap_result.SearchId;
            result.IsFinished = soap_result.IsFinished;
            return result;
        }

        [WebMethod]
        //ожидаем окончания поиска
        public int WaitSearch(string requestId)
        {
            SearchResult sr = GetCurrentResults(requestId);

            if ((sr != null) && (sr.Fares != null)) return sr.Fares.Length;

            return -1;
        }

        [WebMethod]
        public FlightDetails[] Flight_Details(string flightToken, string searchId)
        {
            ///детальная информация о перелете
            DB.DataStore ds = new DB.DataStore();

            Flight[] flights = ds.GetFlightsFromDB(searchId, DateTime.Now.AddSeconds(-36000)).Flights;

            Flight flight = null;
            foreach (Flight item in flights)
                if (item.Id.Contains(flightToken))
                {
                    flight = item;
                    break;
                }

            ArrayList arr_legs = new ArrayList();
            ArrayList details = new ArrayList();

            foreach (FlightPart fp in flight.Parts)
                foreach (Leg leg in fp.Legs)
                    arr_legs.Add(leg.RemarksSearchContext);

            PortbiletService ps = new PortbiletService();

            foreach (string key in arr_legs)
                details.AddRange(ps.GetFlightDetails(key));

            return details.ToArray(typeof(FlightDetails)) as FlightDetails[];
        }

        [WebMethod]
        //ТОЛЬКО ДЛЯ ПЕРЕЛЕТОВ ВИЗИТА
        public Flight GetFlightInfo(string flightToken)
        {
            VizitService vs = new VizitService();

            return vs.Get_Flight_Info(flightToken);
        }

        [WebMethod]
        public Flight GetFlight(string flightToken, string searchId)
        {
            try
            {
               // return GetFlightByTicketId(Convert.ToInt64(flightToken));

                if (flightToken.IndexOf("vt_") == 0)
                    return GetFlightInfo(flightToken);

                ///детальная информация о перелете
                DB.DataStore ds = new DB.DataStore();

                Flight[] flights = ds.GetFlightsFromDB(searchId, DateTime.Now.AddSeconds(-36000)).Flights;

                foreach (Flight item in flights)
                    if (item.Id.Contains(flightToken))
                    {
                        if (flightToken.IndexOf("aw_") == 0)
                        {
                            AwadService aws = new AwadService();
                            item.TimeLimit = aws.Get_Fare_TimeLimit(flightToken);
                        }
                        
                        return item;
                    }
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("\n\nGetFlight Exception\n " + ex.Message + "\n\n" + ex.StackTrace);
            }

            return null;
        }

        private Flight GetFlightByTicketId(long ticket_id)
        {
            DB.DataStore ds = new DB.DataStore();
            int search_key = Convert.ToInt32(Math.Floor(ticket_id / 10000M));

            int poosition = Convert.ToInt32(ticket_id - search_key * 10000);

            Flight[] flights = ds.GetFlightsFromDB(search_key).Flights;

            return flights[poosition];
        }

        [WebMethod]
        public FlightRules GetFlightRulesByTicketId(long ticket_id)
        {
            string token = GetFlightByTicketId(ticket_id).Id;

            if(token.IndexOf("aw_") == 0)
            {
                AwadService aws = new AwadService();
                return aws.GetFareRules(token);
            }

            return null;
        }

        public class BookingResult
        {
            public string code;
            public int timer;
        }

        [WebMethod]
        public BookingResult BookFlight(string flightToken, Customer customer, Passenger[] passengers, DateTime tour_end_date)
        {
            try
            {
                flightToken = GetFlightByTicketId(Convert.ToInt64(flightToken)).Id;

                ///бронирование авиабилетов
                if (flightToken.IndexOf("aw_") == 0)
                {
                    AwadService awad_srvc = new AwadService();
                    string awad_order = awad_srvc.BookFlight(flightToken, customer, passengers);
                    return new BookingResult() { code = awad_order, timer = awad_srvc.GetTimelimit(awad_order) };
                }
                else if (flightToken.IndexOf("pb_") == 0)
                {
                    PortbiletService pb_srvc = new PortbiletService();
                    string pb_order = pb_srvc.BookFlight(flightToken, customer, passengers, tour_end_date);
                    return new BookingResult() { code = pb_order, timer = pb_srvc.GetTimelimit(pb_order) };
                }
                else if (flightToken.IndexOf("vt_") == 0)
                {
                    VizitService vs = new VizitService();
                    string vs_code = vs.BookFlight(flightToken, customer, passengers);
                    return new BookingResult() { code = vs_code, timer = vs.GetTimelimit(vs_code) };
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("\n\nBook Exception\n " + ex.Message + "\n\n" + ex.StackTrace);
            }

            return new BookingResult() { code = "Exception", timer = 0 };
        }

        [WebMethod]
        public PriceLink[] CheckFlights(string[] hashes)
        {
            DB.DataStore ds = new DB.DataStore();
            return ds.CheckTickets(hashes);
        }


        [WebMethod]
        public void Clearing()
        {
            DB.DataStore ds = null;
            try
            {
                ds = new DB.DataStore();
                ds.Clearing();
                ds.Dispose();
            }
            catch (Exception ex)
            {
                if (ds != null) ds.Dispose();
                Logger.Logger.WriteToLog("CLEARING EXCEPTION!! " + ex.Message);
            }
        }

        private void sendToRambler(Route route, int adult, int children, int inf, string serviceClass, SearchResult result)
        {
            //как и сохранение в базу данных
            //запускать в новом потоке
            //при поиске с сайта
        }

        //генерирует из входных параметров идентификатор поиска
        //при повторном поиске с теми же параметрами, результат поиска вернется из кэша
        private string generateSearchId(Route route, int adult, int children, int inf, string serviceClass)
        {

            int hours_limit = Convert.ToInt32(WebConfigurationManager.AppSettings["HoursLimit"]);

            Logger.Logger.WriteToLog("gen: " + route.Segments[0].Date.ToString("dd.MM.yyyy") + " " + route.Segments[0].LocationBegin + " " + route.Segments[0].LocationEnd);
            if (route.Segments[0].Date < DateTime.Now.AddHours(hours_limit)) throw new Exception("wrong date");

            return route.ToString() + "-" + adult + "-" + children + "-" + inf + "-" + serviceClass;
        }
    }
}