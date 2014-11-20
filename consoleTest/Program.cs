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
      //      Flight[] ticketsArr = TestSearch();

      //      Flight ticket = ticketsArr[0];

            var tickets = new TicketsUa();
            var orderId = "tk_6OLOP3";// TestBooking(tickets);


            string res = tickets.AuthUser("al.prokopenya@gmail.com", "1loIbaR4");

            TicketInfo info = tickets.GetTicketInfo(orderId);
            Console.WriteLine(info.MainTurist);

            var result = tickets.BuyTicket(orderId);

            Console.WriteLine(result);
            Console.ReadKey();
        }

        private static string TestBooking(TicketsUa tickets)
        {
            var passenger = new Passenger()
            {
                Birth = new DateTime(1990, 5, 8),
                Citizen = "BY",
                Fname = "Antonovich",
                Gender = "M",
                Name = "Alexey",
                Otc = "Antonovich",
                Pasport = "MP1675674",
                PassportExpireDate = new DateTime(2030, 5, 8),
                FrequentFlyerAirline = "",
                FrequentFlyerNumber = ""

            };
            var orderId = tickets.BookFlight("tk_3c26a097531c0659e6f76e838ebe5b41_95cc7e6ad27895db04c27b1656215145_18^^1",
                                            null,
                                            new Passenger[] { passenger },
                                            Convert.ToDateTime("2015-05-30"));

            Console.WriteLine(orderId);

            return orderId;
        }

        private static Flight[] TestSearch()
        {
            var route = new Route()
            {

                Segments = new RouteSegment[] { 
                    new RouteSegment()
                    {
                        LocationBegin = "MSQ",
                        LocationEnd = "MOW",
                        Date = DateTime.Today.AddDays(180)
                    }
                }
            };

            var tickets = new TicketsUa(route, 1,0,0, "A");

            Console.WriteLine(DateTime.Now.ToString());
            tickets.InitSearch();

            Console.WriteLine(DateTime.Now.ToString()+" "+tickets.GetFlights("").Length);
            return tickets.GetFlights("");
        }
    }
}
