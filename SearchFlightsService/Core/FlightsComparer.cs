using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SearchFlightsService.Containers;
using System.Collections;

namespace SearchFlightsService.Core
{
    //класс для сравнения прелетов по цене
    public class FlightsComparer : IComparer
    {
        public int Compare(object A, object B)
        {
            return (A as Flight).Price - (B as Flight).Price;
        }
    }
}