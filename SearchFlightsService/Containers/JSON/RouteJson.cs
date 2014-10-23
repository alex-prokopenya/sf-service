using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;

/// <summary>
/// Summary description for Route
/// </summary>

namespace SearchFlightsService.Containers.JSON
{
    public class RouteJson
    {
        /*
         * маршрут -- состоит из сегментов перелёта (например, в одну сторону – 1 сегмент или туда-обратно – 2 сегмента ) 
         * 
         */

        public RouteJson()
        {
        }

        public RouteJson(XmlNode[] input) //получаем варианты перелета "туда" и "обратно", или только "туда" для OW 
        { 
            this.Segments = new SegmentJson[input.Length];

            int cnt = 0;

            foreach (XmlNode item in input)
                this.Segments[cnt++] = new SegmentJson(item);
        }

        public JsonArray ToJsonArray()
        {
            JsonArray res = new JsonArray();

            foreach (SegmentJson item in this.Segments)
                res.Add(item.ToJsonArray());

            return res;
        }

        public SegmentJson[] Segments = null;
    }
}