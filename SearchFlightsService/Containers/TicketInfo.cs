using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class TicketInfo
    {
        private int _rubPrice;

        public int RubPrice
        {
            get { return _rubPrice; }
            set { _rubPrice = value; }
        }
        private string _MainTurist;

        public string MainTurist
        {
            get { return _MainTurist; }
            set { _MainTurist = value; }
        }

        private int _turistCount;

        public int TuristCount
        {
            get { return _turistCount; }
            set { _turistCount = value; }
        }

        private string _RouteFrom;

        public string RouteFrom
        {
            get { return _RouteFrom; }
            set { _RouteFrom = value; }
        }
        private string _RouteTo;

        public string RouteTo
        {
            get { return _RouteTo; }
            set { _RouteTo = value; }
        }

        private bool _isBooking;

        public bool IsBooking
        {
            get { return _isBooking; }
            set { _isBooking = value; }
        }

    }
}