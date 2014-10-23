using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;

namespace SearchFlightsService.Containers
{
    public class Fare
    {
        #region fields
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

        private int price;

        public int Price
        {
            get { return price; }
            set { price = value; }
        }

        private string id;

        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        private Direction[] directions;

        public Direction[] Directions
        {
            get { return directions; }
            set { directions = value; }
        }
        #endregion

        #region constructors
        public Fare() { }

        public Fare(string airline, string airlineCode, int price, string id, Direction[] directions)
        {
            this.airlineCode = airlineCode;
            this.airline = airline;
            this.price = price;
            this.id = id;
            this.directions = directions;
        }

        public Fare(JsonObject refl)
        {
            this.id = refl["i"].ToString();
            this.airline = refl["n"].ToString();
            this.airlineCode = refl["c"].ToString();
            this.price = Convert.ToInt32(refl["p"]);

            JsonArray dirArr = refl["d"] as JsonArray;

            this.directions = new Direction[dirArr.Length];

            for (int i = 0; i < dirArr.Length; i++)
                this.directions[i] = new Direction(dirArr[i] as JsonObject);
        }
        #endregion

        public JsonObject ToJson()
        {
            JsonObject reflection = new JsonObject();
            reflection.Add("i", this.id.Replace("\"", "'"));
            reflection.Add("n", this.airline.Replace("\"", "'"));
            reflection.Add("c", this.airlineCode.Replace("\"", "'"));
            reflection.Add("p", this.price);

            JsonArray directions = new JsonArray();
            foreach (Direction dir in this.directions)
                directions.Add(dir.ToJson());

            reflection.Add("d", directions);

            return reflection;
        }
    }
}