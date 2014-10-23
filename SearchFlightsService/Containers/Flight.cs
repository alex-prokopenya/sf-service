using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;

namespace SearchFlightsService.Containers
{
    public class Flight
    {

        public static int RAMBLER_MARGIN = 10;

        #region fields
        private string id;

        public string Id
        {
            get {
                if (id.Length > 300) return id.Substring(0, 300);

                if ((id == String.Empty) || (id == null))
                    return "0";

                return id; }
            set { id = value; }
        }

        private string full_id;

        public string FullId
        {
            get {
                if ((full_id == String.Empty) || (full_id == null))
                    return this.Id;

                return full_id; 
            }
            set { full_id = value; }
        }

        private int price;

        public int Price
        {
            get { return price; }
            set { price = value; }
        }

        private string airline;

        public string Airline
        {
            get { return airline; }
            set { airline = value; }
        }

        private string airlineCode;

        public string AirlineCode
        {
            get { return airlineCode; }
            set { airlineCode = value; }
        }

        private string key;

        public string Key
        {
            get { return key; }
            set { key = value; }
        }

        private string timeLimit;

        public string TimeLimit
        {
            get { return timeLimit; }
            set { timeLimit = value; }
        }

        private FlightPart[] parts;
        
        public FlightPart[] Parts
        {
            get { return parts; }
            set { parts = value; }
        }

        private string _flightMask = "";
        public string FlightMask
        {
            get {

                if (this._flightMask.Length > 0) return this._flightMask;

                string res = this.airlineCode;

                foreach (FlightPart fp in this.parts)
                    foreach (Leg leg in fp.Legs)
                        res += leg.FlightNumber + leg.ServiceClass + leg.BookingClass;

                this._flightMask = res;

                return res;
            }
        }

        public string FlightHash
        {
            get 
            {
                string res = "";
                foreach (FlightPart fp in this.Parts)
                {
                    foreach (Leg leg in fp.Legs)
                    {
                        res += leg.LocationBegin + "," 
                            + leg.DateBegin.ToString("yyyy,M,d,H,m,")
                            + leg.LocationEnd + ","
                            + leg.DateEnd.ToString("yyyy,M,d,H,m,")
                            + leg.Airline + ","
                            + leg.FlightNumber + ","
                            + leg.ServiceClass + "," 
                            + leg.BookingClass;

                        res += "/";
                    }

                    res += "|";
                }
                return res;
            }
        }

        #endregion 

        #region construct
        public Flight()
        { }

        public Flight(string id, int price, string airline, string airlineCode, FlightPart[] parts, string timeLimit)
        {
            this.id = id;
            this.price = price;
            this.parts = parts;
            this.airline = airline;
            this.airlineCode = airlineCode;
            this.timeLimit = timeLimit;
        }

        public Flight(JsonObject refl) 
        { 
            this.id = refl["i"] == null? null : refl["i"].ToString();
            this.price = refl["p"] == null ? 0 : Convert.ToInt32(refl["p"]);
            this.airline = refl["a"] == null ? null : refl["a"].ToString();
            this.airlineCode = refl["ac"] == null ? null : refl["ac"].ToString();

            this.timeLimit = refl["tl"] == null ? null : refl["tl"].ToString();

            JsonArray partsArr = refl["ps"] as JsonArray;

            this.parts = new FlightPart[partsArr.Length];

            for (int i = 0; i < this.parts.Length; i++)
                this.parts[i] = new FlightPart(partsArr[i] as JsonObject);
        }
        #endregion

        public JsonObject ToJson()
        {
            JsonObject reflection = new JsonObject();

            reflection.Add("i", this.id.Replace("\"","'"));
            reflection.Add("p", this.price);
            reflection.Add("a", this.airline.Replace("\"", "'"));
            reflection.Add("tl", this.timeLimit.Replace("\"", "'"));
            reflection.Add("ac", this.airlineCode.Replace("\"", "'"));

            JsonArray partsArray = new JsonArray();

            foreach (FlightPart part in this.parts)
                partsArray.Add(part.ToJson());

            reflection.Add("ps", partsArray);

            return reflection;
        }

        private static JsonArray DateToJsonArray(DateTime date)
        {
            string strDate = date.ToString("yyyy MM dd HH mm");
            string[] strArray = strDate.Split(' ');

            JsonArray res = new JsonArray();

            for (int i = 0; i < strArray.Length; i++)
                res.Add( Convert.ToInt32(strArray[i]));

            return res;
        }

        public JsonArray ToJsonArray()
        {
            JsonArray jArr = new JsonArray();

            JsonArray jArrParts = new JsonArray();

            foreach (FlightPart fp in this.parts)
            {
                JsonArray segmJson = new JsonArray();
                foreach (Leg leg in fp.Legs)
                {
                    JsonArray fpJson = new JsonArray();
                    fpJson.Add(leg.LocationBegin);
                    fpJson.Add(DateToJsonArray(leg.DateBegin));
                    fpJson.Add(leg.LocationEnd);
                    fpJson.Add(DateToJsonArray(leg.DateEnd));
                    fpJson.Add((leg.Duration));
                    fpJson.Add((leg.Airline));
                    fpJson.Add((leg.FlightNumber));
                    fpJson.Add((leg.Board));
                    fpJson.Add((leg.ServiceClass));
                    fpJson.Add((leg.BookingClass));
                    /*
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
                     */
                    segmJson.Add(fpJson);
                }
                jArrParts.Add(segmJson);
            }

            jArr.Add(jArrParts);
            jArr.Add(this.Price  - RAMBLER_MARGIN);

            return jArr;
        }
    }
}