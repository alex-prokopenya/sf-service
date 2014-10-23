using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using System.Web.Services;
using SearchFlightsService.Containers;
using SearchFlightsService.Core;
using SearchFlightsService.Ext;
using System.Threading;

namespace SearchFlightsService
{
    /// <summary>
    /// Сводное описание для VizitFlights
    /// </summary>
    [WebService(Namespace = "http://tempuri.org/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // Чтобы разрешить вызывать веб-службу из скрипта с помощью ASP.NET AJAX, раскомментируйте следующую строку. 
    // [System.Web.Script.Services.ScriptService]
    public class VizitFlights : System.Web.Services.WebService
    {
        private int searchFullTimeout = 50;

        [WebMethod]
        public Flight[] Search(Route route, int adult, int children, int inf, string serviceClass)
        {
            bool is_one_way = route.Segments.Length == 1;

            //starting vizit
            VizitService vsrvc = new VizitService(adult, children, inf, serviceClass, route);
            Thread vtObj = new Thread(new ThreadStart(vsrvc.InitSearch));
            vtObj.Start();

            Flight[] flightsVizit = null;
            int timer = searchFullTimeout;


            while (true)
            {
                //ждем секунду
                Thread.Sleep(2000);

                //если еще не получен результат, пробуем получить от Визита
                if (flightsVizit == null)
                    flightsVizit = vsrvc.Get_Flights("");

                //если получили оба, уходим
                if ((flightsVizit != null))
                    break;

                if (--timer < 0) break;
            }

            return flightsVizit;

        }

        public class BookingResult
        {
            public string code;
            public int timer;
        }

        [WebMethod]
        public BookingResult BookFlight(string flightToken, Customer customer, Passenger[] passengers, DateTime tour_end_date)
        {
            try
            {
                ///бронирование авиабилетов

                //if (flightToken.IndexOf("vt_") == 0)
                {
                    VizitService vs = new VizitService();
                    string vs_code = vs.BookFlight(flightToken, customer, passengers);
                    return new BookingResult() { code = vs_code, timer = vs.GetTimelimit(vs_code) };
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("\n\nBook Exception\n " + ex.Message + "\n\n" + ex.StackTrace);
            }

            return new BookingResult() { code = "Exception", timer = 0 };

        }
    }
}
