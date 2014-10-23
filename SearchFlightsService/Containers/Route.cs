using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class Route: Object
    {
        private RouteSegment[] segments;

        public RouteSegment[] Segments
        {
            get { return segments; }
            set { segments = value; }
        }

        public Route()
        { }

        public Route(RouteSegment[] segments)
        {
            this.segments = segments;
        }

        public Route(string Route)
        {
            int cnt = Route.Length / 10; //количество сегментов в маршруте    
            
            this.segments = new RouteSegment[cnt];

            for (int i = 0; i < cnt; i++)
            {
                this.segments[i] = new RouteSegment(Route.Substring(0, 10));
                Route = Route.Substring(10);
            }
        }

        public override string ToString()
        {
            string result = String.Empty;

            foreach (RouteSegment rs in segments)
                result += rs.ToString();

            return result;
        }
    }
}