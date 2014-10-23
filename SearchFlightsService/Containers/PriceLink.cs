using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class PriceLink
    {
        private int price = 0;

        public int Price
        {
            get { return price; }
            set { price = value; }
        }

        private string link = "";

        public string Link
        {
            get { return link; }
            set { link = value; }
        }

        public PriceLink()
        { }

        public PriceLink(int price, string link)
        {
            this.price = price;
            this.link = link;
        }
    }
}