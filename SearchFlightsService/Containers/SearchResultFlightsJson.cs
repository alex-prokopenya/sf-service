using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;

namespace SearchFlightsService.Containers
{
    public class SearchResultFlightsJson
    {
        private string flights;

        public string Flights
        {
            get { return flights; }
            set { flights = value; }
        }

        public SearchResultFlightsJson() { }

        public SearchResultFlightsJson(Flight[] flights, decimal course)
        {
            if (flights == null)
            {
                this.flights = "[]";
                return;
            }


            JsonArray jArr = new JsonArray();

            foreach (Flight itemFlight in flights)
            {
                itemFlight.Price = Convert.ToInt32( Math.Ceiling(itemFlight.Price / course) );
                jArr.Add(itemFlight.ToJson());
            }

            this.flights = jArr.ToString();
        }
    }
}