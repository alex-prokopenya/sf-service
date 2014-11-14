using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchFlightsService.Ext;
using SearchFlightsService.Containers;

namespace consoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var route = new Route() {

                Segments = new RouteSegment[] { 
                    new RouteSegment(){
                        LocationBegin = "MSQ",
                        LocationEnd = "MOW",
                        Date = DateTime.Today.AddDays(180)
                    },
                
                    new RouteSegment(){
                      LocationBegin = "MOW",
                        LocationEnd = "LED",
                        Date = DateTime.Today.AddDays(185)
                    },

                    new RouteSegment(){
                      LocationBegin = "LED",
                        LocationEnd = "MSQ",
                        Date = DateTime.Today.AddDays(190)
                    }
                }
            };

            var tickets = new TicketsUa(route, 2, 1, 1, "A");

            tickets.InitSearch();

            Console.ReadKey();
        }
    }
}
