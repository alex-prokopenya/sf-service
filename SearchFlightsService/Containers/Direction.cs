using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;

namespace SearchFlightsService.Containers
{
    public class Direction
    {
        //fields
        private Variant[] variants = null;

        public Variant[] Variants
        {
            get { return variants; }
            set { variants = value; }
        }

        //constructors
        public Direction(){}

        public Direction(Variant[] variants) {
            this.variants = variants;
        }

        public Direction(JsonObject refl)
        { 
            JsonArray varArr = refl["v"] as JsonArray;

            this.variants = new Variant[varArr.Length];

            for (int i = 0; i < varArr.Length; i++)
                this.variants[i] = new Variant(varArr[i] as JsonObject);
        }

        //convert
        public JsonObject ToJson()
        {
            JsonObject reflection = new JsonObject();

            JsonArray varArr = new JsonArray();

            foreach (Variant vrnt in this.variants)
                varArr.Add(vrnt.ToJson());

            reflection.Add("v", varArr);

            return reflection;
        }
    }
}