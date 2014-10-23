using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;

namespace SearchFlightsService.Containers
{
    public class Variant
    {
        //fields
        private string flightTime;

        public string FlightTime
        {
            get { return flightTime; }
            set { flightTime = value; }
        }

        private string id;

        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        private Leg[] legs;

        public Leg[] Legs
        {
            get { return legs; }
            set { legs = value; }
        }

        private string key;

        public string Key
        {
            get { return key; }
            set { key = value; }
        }

        private int startTime;

        public int StartTime
        {
            get { return startTime; }
            set { startTime = value; }
        }
        

        //constructos
        public Variant()
        { }

        public Variant(Leg[] leg, string id, string flightTime)
        {
            this.legs = leg;
            this.id = id;
            this.flightTime = flightTime;
        }

        public Variant(JsonObject refl)
        {
            this.id = refl["i"].ToString();
            this.flightTime = refl["t"].ToString();

            JsonArray legsArr = refl["l"] as JsonArray;

            this.legs = new Leg[legsArr.Length];

            for(int i = 0; i<legsArr.Length; i++)
                legs[i] = new Leg(legsArr[i] as JsonObject);
        }


        //convert
        public JsonObject ToJson()
        {
            JsonObject reflection = new JsonObject();

            reflection.Add("t", this.flightTime);
            reflection.Add("i", this.id);

            JsonArray legsArr = new JsonArray();

            foreach(Leg leg in this.legs)
                legsArr.Add(leg.ToJson());

            reflection.Add("l", legsArr);

            return reflection;
        }
    }
}