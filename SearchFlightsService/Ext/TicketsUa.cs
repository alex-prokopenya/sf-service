using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using SearchFlightsService.Containers;
using SearchFlightsService.Core;
using System.Net;
using System.Xml;
using SearchFlightsService.Logger;
using System.Web.Configuration;


namespace SearchFlightsService.Ext
{
    public class TicketsUa : IExternalService
    {
        #region Constants
        private const string TKTS_service_uri = "https://v2.api.tickets.ua";
        private const string TKTS_key = "6210cf64-6e8f-4f97-9c57-b5c101fadc21";
        private const string TKTS_search = "/avia/search.xml?";
        private const string TKTS_Rules = "/avia/fare_conditions.xml?";

        private const string TKTS_signup = "/user/signup?";
        private const string TKTS_signin = "/user/signin?";
        
        private const string TKTS_book = "/avia/book.xml?";

        private const string TKTS_confirm = "/payment/commit?";

        private const string TKTS_booking_details = "/avia/bookings_list.xml?";

        private const string TKTS_get_ticket = "/avia/eticket.json?";

        private const string TKTS_service = "avia";
        #endregion

        #region Fields
        private Hashtable hashResults = new Hashtable();

        private Containers.Flight[] result = null;

        private Route route;

        private int adult;

        private int children;

        private int inf;

        private string serviceClass;

        private string searchId;

        public string SearchId
        {
            get { return searchId; }
            set { searchId = value; }
        }
        #endregion

        #region Constructors
        public TicketsUa()
        { }

        public TicketsUa(Route route, int adult, int children, int inf, string serviceClass)
        {
            this.route = route;
            this.adult = adult;
            this.children = children;
            this.inf = inf;
            this.serviceClass = serviceClass;
            this.searchId = route.ToString() + "-" + adult + "-" + children + "-" + inf + "-" + serviceClass;
        }
        #endregion

        private string request_id = "";

        #region Public methods
        public string InitSearch(Route route, int adult, int children, int inf, string serviceClass)
        {
            throw new SearchFlightException("Use only async InitSearch()");
        }

        public void InitSearch() // делает поиск и конвертирует результаты в массив билетов
        {
            //сгенерировать запрос в виде url
            string request = TKTS_service_uri + TKTS_search + "key=" + TKTS_key;

            //сделать http: запрос сервису

            //добавляем маршрут
            for (int i = 0; i < this.route.Segments.Length; i++)
                request += SegmentToRequestString(i, this.route.Segments[i]);

            //добавляем инфо по туристам
            request += string.Format("&adt={0}&chd={1}&inf={2}&service_class={3}", this.adult, this.children, this.inf, this.serviceClass );

            //делаем запрос и получаем XML
            var resp = this.makeHttpRequest(request, 40);

            //сконвертировать ответ
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(resp);

            //result code
            string resultCode = (xDoc.GetElementsByTagName("result")[0] as XmlElement).GetAttribute("code");

            //если не удалось завершить поиск
            if (resultCode != "0")
            {
                Logger.Logger.WriteToLog("tkts fail: " + resp);
                return; 
            }

            //session Id
            string sesId = (xDoc.GetElementsByTagName("session")[0] as XmlElement).GetElementsByTagName("id")[0].InnerText;
            XmlNodeList recList = xDoc.GetElementsByTagName("recommendation");

            //обработка результатов
            var flights = new List<Flight>();

            foreach (XmlElement el in recList)
                flights.Add(ConvertXmlToFlight(el, sesId));

            this.result = flights.ToArray();
        }

        public Flight[] GetFlights(string search_id)
        {
            return this.result;
        }

        private string SegmentToRequestString(int index, RouteSegment item)
        {
            return String.Format("&destinations[{0}][departure]={1}&destinations[{0}][arrival]={2}&destinations[{0}][date]={3:dd-MM-yyyy}", 
                                    index,
                                    item.LocationBegin,
                                    item.LocationEnd,
                                    item.Date);
        }

        public string BookFlight(string flightId, Customer customer, Passenger[] passengers)
        {
            string[] arr = flightId.Replace("~", "_").Split('_');
            string requestStr = "R=" + arr[1] + "&F=" + arr[2] + "&V=" + arr[3].Replace("^", ";").TrimEnd(new char[] { ';' });
            string customerSrt = "&Phone=" + customer.Phone + "&Email=" + customer.Mail + "&PhoneCountry=RU|7&PersonalEmail=it@viziteurope.eu";

            requestStr += customerSrt;

            for (int i = 0; i < passengers.Length; i++)
            {
                if (passengers[i].Citizen.Length > 2)
                    passengers[i].Citizen = passengers[i].Citizen.Substring(2);

                requestStr += "&FName" + (i + 1) + "=" + passengers[i].Name +
                              "&LName" + (i + 1) + "=" + passengers[i].Fname +
                              "&PCountry" + (i + 1) + "=" + passengers[i].Citizen +
                              "&G" + (i + 1) + "=" + passengers[i].Gender +
                              "&BDate" + (i + 1) + "=" + passengers[i].Birth.ToString("dd.MM.yyyy") +
                              "&PNumber" + (i + 1) + "=" + passengers[i].Pasport +
                              "&PExpDate" + (i + 1) + "=" + passengers[i].Passport_expire_date.ToString("dd.MM.yyyy");

                if ((passengers[i].FrequentFlyerAirline != null) && (passengers[i].FrequentFlyerNumber != null) && (passengers[i].FrequentFlyerAirline.Length * passengers[i].FrequentFlyerNumber.Length != 0))
                    requestStr += "&FrequentFlyerAirline" + (i + 1) + passengers[i].FrequentFlyerAirline +
                                   "&FrequentFlyerNumber" + (i + 1) + passengers[i].FrequentFlyerNumber;
            }

            XmlDocument xDoc = new XmlDocument();
            string content = "";// makeHttpRequest(TKTS_service_uri + AWAD_CreateReservation + requestStr, 80);
            try
            {
                //content = ;
                xDoc.LoadXml(content);

                Logger.Logger.WriteToBookLog("SEND BookRequest " + requestStr + "Create Reservation response: " + content);
            }
            catch (Exception)
            {
                Logger.Logger.WriteToBookLog("EXCEPTION: BookRequest" + requestStr + "Create Reservation Error " + content);

                throw new SearchFlightException("BookRequest" + requestStr + "Create Reservation Error " + content);
            }

            XmlNodeList nList = xDoc.GetElementsByTagName("CreateReservation");

            if (nList.Count == 0) throw new SearchFlightException("BookRequest" + requestStr + "Create Reservation Error " + xDoc.InnerXml);

            if ((nList.Count == 1) && ((nList[0] as XmlElement).HasAttribute("OrderId")))
            {
                string orderId = (nList[0] as XmlElement).GetAttribute("OrderId").ToString();

                XmlDocument orderDoc = new XmlDocument();
            //    orderDoc.LoadXml(makeHttpRequest(TKTS_service_uri + AWAD_Order + orderId));
                XmlNodeList orderList = orderDoc.GetElementsByTagName("Order");

                if ((orderList[0] as XmlElement).GetAttribute("OrderAlreadyExists").ToString() == "true")
                {
                    Logger.Logger.WriteToBookLog("ALREADY EXISTS!!");
                    return "can not book";
                }
                //узнать таймлимит

                return "aw_" + (orderList[0] as XmlElement).GetAttribute("IdentifierNumber").ToString() + "@@@" + orderId;
            }
            else
                if ((nList[0] as XmlElement).GetAttribute("Error").ToString() == "CANT_CREATE_RESERVATION")
                    return "can not book";

            throw new SearchFlightException("BookRequest" + requestStr + "Create Reservation Error " + xDoc.InnerXml);
        }

        public int GetTimelimit(string order_number)
        {
            return 0;
        }

        public FlightDetails[] GetFlightDetails(string flightToken)
        {
            return null;
        }

        public FlightRules GetFareRules(string flightId)
        {
            //берем параметры тарифа и вариант перелета
            string[] arr = flightId.Split('_');
            var searchId = arr[1];
            var recId = arr[2];

            string request = TKTS_service_uri + TKTS_Rules + "key=" + TKTS_key ;

            request += "&session_id=" + searchId;
            request += "&recommendation_id=" + recId;


            //делаем запрос
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(makeHttpRequest(request));

            XmlNodeList nList = xDoc.GetElementsByTagName("fare-condition");

            bool CBD = true;
            bool CAD = true;
            bool RBD = true;
            bool RAD = true;

            string rulesText = "";

            foreach (XmlElement nodeItem in nList)
            {
                //маршрут
                string depCity = (nodeItem.GetElementsByTagName("departure")[0] as XmlElement).InnerText;
                string arrCity = (nodeItem.GetElementsByTagName("arrival")[0] as XmlElement).InnerText;

                var cancelHeader = nodeItem.GetElementsByTagName("cancellation")[0] as XmlElement;
                var changeHeader = nodeItem.GetElementsByTagName("change")[0] as XmlElement;

                CBD &= changeHeader.GetElementsByTagName("before")[0].InnerText == "true";
                CAD &= changeHeader.GetElementsByTagName("after")[0].InnerText == "true";
                RBD &= cancelHeader.GetElementsByTagName("before")[0].InnerText == "true";
                RAD &= cancelHeader.GetElementsByTagName("after")[0].InnerText == "true";

                rulesText += depCity + " - " + arrCity + "<br>\n<br>\n" + nodeItem.GetElementsByTagName("text")[0].InnerText + "<br>\n<br>\n<br>\n<br>\n";
            }

            return new FlightRules()
            {
                AllowedChangesAfter = CAD,
                AllowedChangesBefore = CBD,
                AllowedReturnAfter = RAD,
                AllowedReturnBefore = RBD,
                RulesText = rulesText
            };
        }
     
        #endregion

        #region Private methods
        private string AuthUser(string login, string password)
        {
            return "";
        }

        private class MyWebClient : WebClient
        {
            private int timer = 40;
            public MyWebClient(int _secs)
            {
                timer = _secs;
            }

            protected override WebRequest GetWebRequest(Uri uri)
            {
                WebRequest w = base.GetWebRequest(uri);
                w.Timeout = this.timer * 1000;
                return w;
            }
        }

        private string makeHttpRequest(string path, int timer)
        {
            try
            {
                MyWebClient wc = new MyWebClient(timer);
                // wc.webre
                return wc.DownloadString(path);
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("TKTS Exception for request: " + path + "  " + ex.Message + " " + ex.StackTrace);
                return "<root></root>";
            }
        }

        private string makeHttpRequest(string path)
        {
            try
            {
                MyWebClient wc = new MyWebClient(40);
                // wc.webre
                return wc.DownloadString(path);
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("TKTS Exception for request: " + path + "  " + ex.Message + " " + ex.StackTrace);
                return "<root></root>";
            }
        }

        private Flight ConvertXmlToFlight(XmlElement el, string sessionId)
        {
            Flight fl = new Flight(); //пустой билет

            //таймлимит
            fl.TimeLimit = DateTime.Now.AddMinutes(30).ToString("yyyy-MM-dd HH:mm");

            //авиакомпания
            fl.Airline = el.GetElementsByTagName("validating-supplier")[0].InnerText;
            fl.AirlineCode = fl.Airline;

            fl.Id = "tk_" + sessionId + "_" + el.GetElementsByTagName("id")[0].InnerText;
            fl.FullId = fl.Id;

            fl.Price = (int)Math.Ceiling(Convert.ToDecimal(el.GetElementsByTagName("RUR")[0].InnerText.Replace(".", ",")));

            fl.Parts = XmlToFlightParts(el.GetElementsByTagName("route"));

            return fl;
        }

        private FlightPart[] XmlToFlightParts(XmlNodeList routes) //формируем список частей маршрута
        {
            var list = new List<FlightPart>();

            foreach (XmlElement el in routes) //идем по списку -- конвертируем каждый элемент
                list.Add(XmlToFlightPart(el));

            return list.ToArray();
        }

        private FlightPart XmlToFlightPart(XmlElement el) //формируем элемент машрута
        {
            var part = new FlightPart();

            part.FlightLong = Convert.ToInt32(el.GetElementsByTagName("route-duration")[0].InnerText);

            part.Legs = XmlToLegs(el.GetElementsByTagName("segment"));

            return part;
        }

        private Leg[] XmlToLegs(XmlNodeList legs) //формируем список элементов
        {
            var list = new List<Leg>();

            foreach (XmlElement el in legs) //идем по списку -- конвертируем каждый элемент
                list.Add(XmlToLeg(el));

            return list.ToArray();
        }

        private Leg XmlToLeg(XmlElement el) //формируем элемент машрута
        {
            var leg = new Leg();

            leg.Airline = el.GetElementsByTagName("supplier")[0].InnerText;
            leg.AirportBeginName = el.GetElementsByTagName("departure-airport")[0].InnerText;
            leg.AirportEndName = el.GetElementsByTagName("arrival-airport")[0].InnerText;
            leg.Board = el.GetElementsByTagName("aircraft-code")[0].InnerText;
            leg.BoardName = el.GetElementsByTagName("aircraft")[0].InnerText;

            leg.BookingClass = el.GetElementsByTagName("service-class")[0].InnerText;
            leg.DateBegin = Convert.ToDateTime(el.GetElementsByTagName("departure-time")[0].InnerText);
            leg.DateEnd = Convert.ToDateTime(el.GetElementsByTagName("arrival-time")[0].InnerText);
            leg.Duration = Convert.ToInt32(el.GetElementsByTagName("flight-duration")[0].InnerText);

            leg.FlightNumber = el.GetElementsByTagName("flight-number")[0].InnerText;

            leg.LocationBegin = el.GetElementsByTagName("departure-city")[0].InnerText;
            leg.LocationBeginName = el.GetElementsByTagName("departure-city")[0].InnerText;

            leg.LocationEnd = el.GetElementsByTagName("arrival-city")[0].InnerText;
            leg.LocationEndName = el.GetElementsByTagName("arrival-city")[0].InnerText;
            leg.ServiceClass = leg.BookingClass;

            return leg;
        }
        #endregion
    }
}