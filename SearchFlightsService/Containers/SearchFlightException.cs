using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class SearchFlightException:Exception
    {
        public SearchFlightException()
           : base()
        { }

        public SearchFlightException(string Message)
            : base(Message)
        { }
    }
}