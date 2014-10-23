using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;

/// <summary>
/// Summary description for Route
/// </summary>

namespace SearchFlightsService
{
    public class Route_reduced
    {
        /*
         * маршрут -- состоит из сегментов перелёта (например, в одну сторону – 1 сегмент или туда-обратно – 2 сегмента) 
         * 
         */

        public Route_reduced()
        {
        }

        public Route_reduced(JsonArray inp)
        {
            this.Segments = new Segment_reduced[inp.Count];

            int cnt = 0;

            foreach (object item in inp)
                this.Segments[cnt++] = new Segment_reduced(item as JsonArray);
        }

        public JsonArray ToJsonArray()
        {
            JsonArray res = new JsonArray();

            foreach (Segment_reduced item in this.Segments)
                res.Add(item.ToJsonArray());

            return res;
        }

        public Segment_reduced[] Segments = null;
    }
}