using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;


namespace SearchFlightsService.Containers
{
    public class FlightPart
    {
        //fields
        private int duration;

        public int FlightLong
        {
            get { return duration; }
            set { duration = value; }
        }

        private Leg[] legs;

        public Leg[] Legs
        {
            get { return legs; }
            set { legs = value; }
        }


        //constructor
        public FlightPart() { }

        public FlightPart(Leg[] legs, int duration)
        {
            this.legs = legs;
            this.duration = duration;
        }

        public FlightPart(JsonObject refl)
        {
            JsonArray legsArr = refl["l"] as JsonArray;

            this.legs = new Leg[legsArr.Length];


            for (int i = 0; i < this.legs.Length; i++)
                this.legs[i] = new Leg(legsArr[i] as JsonObject);

            this.duration = refl["tt"] == null ? 0 : Convert.ToInt32(refl["tt"]);
        }

        //converter
        public JsonObject ToJson()
        {
            JsonObject reflection = new JsonObject();

            JsonArray legsArr = new JsonArray();

            foreach(Leg leg in legs)
                legsArr.Add(leg.ToJson());

            reflection.Add("l", legsArr);

            reflection.Add("tt", this.duration);

            return reflection;
        }
    }
}