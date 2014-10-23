using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;


/// <summary>
/// Summary description for Segment
/// </summary>

namespace SearchFlightsService.Containers.JSON
{
    public class SegmentJson
    {
        /*
         * сегмент маршрута, описывает последовательность перелётов
         * 
         * Аналог - класс Trip из xml API
         * 
         */

        public SegmentJson()
        {
                        
        }

        public SegmentJson(XmlNode input) //получаем элемент Variant
        { 
            XmlElement variant = input as XmlElement;

            this.VariantId = Convert.ToInt32(variant.GetAttribute("Id")); 

            XmlNodeList xml_legs = variant.GetElementsByTagName("Leg");

            this.Flights = new FlightJson[xml_legs.Count];

            int cnt = 0;

            foreach (XmlNode xml_leg in xml_legs)
                this.Flights[cnt++] = new FlightJson(xml_leg);
        }

        public JsonArray ToJsonArray()
        {
            JsonArray jArr = new JsonArray();

            foreach (FlightJson fl in this.Flights)
                jArr.Add(fl.ToJsonArray());

            return jArr;        
        }

        public int VariantId = 0;
        public FlightJson[] Flights = null;
    }
}