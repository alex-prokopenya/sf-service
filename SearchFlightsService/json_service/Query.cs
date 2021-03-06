﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;
using Jayrock.JsonRpc;
using Jayrock.JsonRpc.Web;

namespace SearchFlightsService
{
     public class Query
     {
            public Query()
            {

            }

            public Query(JsonArray InputArray)
            {
                //узнаем количество участков маршрута            
                int segmentsCount = (InputArray[0] as JsonArray).Count;

                //создаем массив из этих сегментов
                this.QuerySegments = new QuerySegment[segmentsCount];

                //проходимся по каждому
                int cnt = 0;
                foreach (JsonArray segment in (InputArray[0] as JsonArray))
                    this.QuerySegments[cnt++] = new QuerySegment(segment);

                //проходимся по пассажирам
                this.Adults = Convert.ToInt32(InputArray[1]);                   //взрослые
                this.Children = Convert.ToInt32(InputArray[2]);                 //дети
                this.Infants = Convert.ToInt32(InputArray[3]);                  //инфанты

                if (this.Adults + this.Children + this.Infants > 8)
                    throw new Core.RamblerAviaException("превышено максимальное число пассажиров (8)", 32013);

                this.InfantsWithoutSeat = Convert.ToInt32(InputArray[4]);       //инфанты без места

                if (this.InfantsWithoutSeat > 2)
                    throw new Core.RamblerAviaException("превышено максимальное число младенцев (2)", 32013);

                this.CabinClass = Convert.ToString(InputArray[5]);            //класс перелета: "E" – эконом, "B" – бизнес, "F" – первый, "P" – премиум. null – любой
            }

            public JsonArray ToJsonArray()
            {
                JsonArray jAr = new JsonArray();

                JsonArray segments = new JsonArray();

                foreach (QuerySegment qs in this.QuerySegments)
                    segments.Add(qs.ToJsonArray());

                jAr.Add(segments); // участки маршрута

                jAr.Add(this.Adults);
                jAr.Add(this.Children);
                jAr.Add(this.Infants);
                jAr.Add(this.InfantsWithoutSeat);
                jAr.Add(this.CabinClass);

                return jAr;
            }

            public QuerySegment[] QuerySegments = null; // участки маршрута
            public int Adults = 0;                      // взрослых
            public int Children = 0;                    // детей
            public int Infants = 0;                     // младенцев
            public int InfantsWithoutSeat = 0;          // младенцев без мест
            public string CabinClass = "";              // класс перелета
        }
}