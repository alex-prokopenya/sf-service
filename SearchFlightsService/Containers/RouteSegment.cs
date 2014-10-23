using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class RouteSegment
    {
        private DateTime date;

        public DateTime Date
        {
            get { return date; }
            set {
                if (value < DateTime.Today)
                    throw new SearchFlightException("Invalid route segment date '"+value.ToString() +"'");

                date = value; 
            }
        }

        private string locationBegin;

        public string LocationBegin
        {
            get { return locationBegin; }
            set {

                if (value.Length != 3)
                    throw new SearchFlightException("Invalid locationBegin code '" + value + "'");   

                locationBegin = value; 
            }
        }

        private string locationEnd;

        public string LocationEnd
        {
            get { return locationEnd; }
            set {

                if (value.Length != 3)
                    throw new SearchFlightException("Invalid LocationEnd code '" + value + "'");   

                locationEnd = value; 
            }
        }

        public RouteSegment()
        { }

        public RouteSegment(DateTime date,  string locationBegin, string locationEnd)
        {
            this.Date = date;
            this.LocationBegin = locationBegin;
            this.LocationEnd = locationEnd;
        }

        public RouteSegment(string Segment)
        {
            if (Segment.Length != 10) throw new SearchFlightException("wrong segment");

            string dateS = Segment.Substring(0, 2);

            string monthS = Segment.Substring(2, 2);

            this.date = Convert.ToDateTime(DateTime.Now.Year + monthS + dateS);

            if (date < DateTime.Now) date.AddYears(1);

            this.locationBegin = Segment.Substring(4, 3);
            this.locationEnd = Segment.Substring(7, 3);
        }

        public override string ToString()
        {
            return this.date.ToString("ddMM") + this.locationBegin + this.locationEnd;
        }
    }
}