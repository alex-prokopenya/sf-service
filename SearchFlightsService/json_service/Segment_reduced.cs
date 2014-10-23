using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;


/// <summary>
/// Summary description for Segment
/// </summary>

namespace SearchFlightsService
{
    public class Segment_reduced
    {

        public Segment_reduced()
        {

        }

        public Segment_reduced(JsonArray inp)
        {
            this.Flights = new Flight_reduced[inp.Count];

            int cnt = 0;

            foreach (object flight in inp)
                this.Flights[cnt++] = new Flight_reduced(flight as JsonArray);
        }

        public JsonArray ToJsonArray()
        {
            JsonArray jArr = new JsonArray();

            foreach (Flight_reduced fl in this.Flights)
                jArr.Add(fl.ToJsonArray());

            return jArr;
        }

        public Flight_reduced[] Flights = null;
    }
}