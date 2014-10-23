using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;
using Jayrock.JsonRpc;
using Jayrock.JsonRpc.Web;

namespace SearchFlightsService
{
    public class QuerySegment
    {
        public QuerySegment() { }

        public QuerySegment(JsonArray arr)
        {
            //узнаем город вылета
            this.from = arr[0] as String;

            //узнаем город прилета
            this.to = arr[1] as String;

            //парсим дату
            JsonArray date_arr = arr[2] as JsonArray;
            this.date = new int[] { Convert.ToInt32(date_arr[0]), Convert.ToInt32(date_arr[1]), Convert.ToInt32(date_arr[2]) };
        }

        public JsonArray ToJsonArray()
        {
            JsonArray jAr = new JsonArray();

            jAr.Add(this.from);  //откуда
            jAr.Add(this.to);    //куда

            JsonArray dateArr = new JsonArray();

            dateArr.Add(this.date[0]); dateArr.Add(this.date[1]); dateArr.Add(this.date[2]); // дата перелета
            jAr.Add(dateArr);

            return jAr;
        }

        public string from = "";
        public string to = "";
        public int[] date = new int[3];
    }
}