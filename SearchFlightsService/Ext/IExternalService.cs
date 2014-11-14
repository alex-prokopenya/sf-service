using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SearchFlightsService.Containers;

namespace SearchFlightsService.Ext
{
    public interface IExternalService
    {
        string InitSearch(Route route, int adult, int chidren, int inf, string serviceClass);

        FlightDetails[] GetFlightDetails(string flightToken);
        
        Flight[] GetFlights(string search_id);
    }
}