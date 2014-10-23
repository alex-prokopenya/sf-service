using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;

/// <summary>
/// Summary description for Flight
/// </summary>
/// 
namespace SearchFlightsService
{
    public class Flight_reduced
    {
        /*   Flight_reduced содержит те же поля, что и Flight, кроме полей duration и plane code
        */

        public Flight_reduced()
        {
        }

        public Flight_reduced(JsonArray inp)
        {
            this.DepartureAirport = Convert.ToString(inp[0]);               // аэропорт вылета
            this.DepartureDateTime = JsonArrayToDate(inp[1] as JsonArray);  // дата и время вылета

            this.ArrivalAirport = Convert.ToString(inp[2]);                 // аэропорт прилета
            this.ArrivalDateTime = JsonArrayToDate(inp[3] as JsonArray);    // дата и время прилета

            this.AirlineCode = Convert.ToString(inp[4]);                    // код авиакомпании
            this.FlightNumber = Convert.ToString(inp[5]);                   // номер рейса
            this.CabinClass = Convert.ToString(inp[6]);                     // класс перелета
            this.BookingClass = Convert.ToString(inp[7]);                   // класс бронирования
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

        private int[] JsonArrayToDate(JsonArray arr)
        {
            int[] date = new int[5];

            int cnt = 0;
            foreach (object item in arr)
                date[cnt++] = Convert.ToInt32(item);

            return date;
        }

        private JsonArray DateToJsonArray(int[] date)
        {
            JsonArray res = new JsonArray();

            foreach (int item in date)
                res.Add(item);

            return res;
        }

        public JsonArray ToJsonArray()
        {
            //[departure airport, departure datetime, arrival_airport, arrival_datetime, airline code, flight number, cabin class, booking class]

            JsonArray jArr = new JsonArray();

            jArr.Add(this.DepartureAirport);
            jArr.Add(DateToJsonArray(this.DepartureDateTime));
            jArr.Add(this.ArrivalAirport);
            jArr.Add(DateToJsonArray(this.ArrivalDateTime));
            jArr.Add(this.AirlineCode);
            jArr.Add(this.FlightNumber);
            jArr.Add(this.CabinClass);
            jArr.Add(this.BookingClass);

            return null;
        }

        public string DepartureAirport = "";
        public string ArrivalAirport = "";

        public int[] DepartureDateTime = new int[5];
        public int[] ArrivalDateTime = new int[5];

        public string AirlineCode = "";

        public string FlightNumber = "";

        public string CabinClass = "";

        public string BookingClass = "";
    }
}