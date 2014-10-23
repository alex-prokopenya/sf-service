using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;

/// <summary>
/// Summary description for Flight
/// </summary>
/// 
namespace SearchFlightsService.Containers.JSON
{
    public class FlightJson
    {
        /*  Аналог - класс Leg из xml API
            Описание полей Flight:
            departure/arrival airport – IATA или ICAO код. Соответственно, 3 или 4 символа.
            departure/arrival datetime – список из 5 целых чисел [year, month, day, hour, minute].
            duration – полное время перелёта (включая все технические остановки) в минутах. Целое число.
            airline code – IATA код авиакомпании. Строка 2 символа.
            flight number – номер рейса. Строка.
            plane code – IATA код самолёта. Строка 3 символа.
            cabin class – однобуквенный код класса перелёта по первой букве англоязычного названия: "E" – эконом, "B" – бизнес, "F" – первый, "P" – премиум.
            booking class – однобуквенный код класса бронирования. Иногда также называется подкласс.
        */
        
        public FlightJson()
        {
        }

        private int StringToDuration(string inp)
        {
            try
            {
                string[] arr = inp.Split(':');

                return Convert.ToInt32(arr[0]) * 60 + Convert.ToInt32(arr[1]);
            }
            catch (Exception ex)
            {
                throw new Exception(inp + "\n\n" + ex.Message + "\n\n" + ex.StackTrace);
            }

        }

        private int[] ParseDateTime(string date, string time)
        {
            int[] result = new int[5];
            try
            {
                string[] arr_date = date.Split('-');

                result[0] = Convert.ToInt32(arr_date[0]);
                result[1] = Convert.ToInt32(arr_date[1]);
                result[2] = Convert.ToInt32(arr_date[2]);

                string[] arr_time = time.Split(':');

                result[3] = Convert.ToInt32(arr_time[0]);
                result[4] = Convert.ToInt32(arr_time[1]);
            }
            catch (Exception ex)
            {
                throw new Exception("Exception while parse date\n" + ex.Message + "\n" + ex.StackTrace);
            }

            return result;
        }

        private JsonArray DateToJsonArray(int[] date)
        {
            JsonArray res = new JsonArray();

            foreach (int item in date)
                res.Add(item);

            return res;
        }

        public FlightJson(XmlNode input) //получаем элемент Leg 
        {

            try
            {
                XmlElement leg = input as XmlElement;

                this.FlightNumber = Convert.ToString(leg.GetAttribute("FN"));
                

                this.AirlineCode = this.FlightNumber.Substring(0, 2);

                if (this.FlightNumber.Contains("-"))
                    this.FlightNumber = this.FlightNumber.Substring(3);

                this.CabinClass = Convert.ToString(leg.GetAttribute("SC"));
                this.BookingClass = Convert.ToString(leg.GetAttribute("BC"));
                this.Duration = StringToDuration(Convert.ToString(leg.GetAttribute("FT")));
                
                XmlNodeList planes = leg.GetElementsByTagName("Plane");

                this.PlaneCode = Convert.ToString((planes[0] as XmlElement).GetAttribute("C"));

                XmlNodeList deps = leg.GetElementsByTagName("Departure");
                XmlNodeList arrivals = leg.GetElementsByTagName("Arrival");

                this.DepartureAirport = Convert.ToString((deps[0] as XmlElement).GetAttribute("Code"));

                this.ArrivalAirport = Convert.ToString((arrivals[0] as XmlElement).GetAttribute("Code"));

                this.DepartureDateTime = ParseDateTime(Convert.ToString((deps[0] as XmlElement).GetAttribute("Date")), Convert.ToString((deps[0] as XmlElement).GetAttribute("Time")));
                this.ArrivalDateTime = ParseDateTime(Convert.ToString((arrivals[0] as XmlElement).GetAttribute("Date")), Convert.ToString((arrivals[0] as XmlElement).GetAttribute("Time")));
            }
            catch(Exception ex)
            {
                throw new Exception("Exception while parse Leg\n" + ex.Message + "\n" + ex.StackTrace); 
            }
        }

        public JsonArray ToJsonArray()
        {
            //[departure airport, departure datetime, arrival_airport, arrival_datetime, duration, airline code, flight number, plane code, cabin class, booking class]

            JsonArray jArr = new JsonArray();

            jArr.Add(this.DepartureAirport);
            jArr.Add(DateToJsonArray(this.DepartureDateTime));
            jArr.Add(this.ArrivalAirport);
            jArr.Add(DateToJsonArray(this.ArrivalDateTime));
            jArr.Add(this.Duration);
            jArr.Add(this.AirlineCode);
            jArr.Add(this.FlightNumber);
            jArr.Add(this.PlaneCode);
            jArr.Add(this.CabinClass);
            jArr.Add(this.BookingClass);

            return jArr;
        }

        public string DepartureAirport = "";
        public string ArrivalAirport = "";

        public int[] DepartureDateTime = new int[5];
        public int[] ArrivalDateTime = new int[5];

        public int Duration = 0;

        public string AirlineCode = "";

        public string FlightNumber = "";

        public string PlaneCode = "";

        public string CabinClass = "";

        public string BookingClass = "";
    }
}