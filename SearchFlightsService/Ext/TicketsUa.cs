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
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO;
using System.Text;

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

        private const string TKTS_balance = "/payment/balance?";
        private const string TKTS_confirm = "/payment/commit?";

        private const string TKTS_booking_details = "/avia/bookings_list.xml?";

        private const string TKTS_get_ticket = "/avia/eticket.json?";

        private const string TKTS_service = "avia";

        private const string userLogin = "al.prokopenya@gmail.com";

        private const string userPassword = "1loIbaR4";

        private const string TKTS_shop_api_key = "dbb95b76-4145-4e8b-88af-9d79cb63c96f";
        private const string TKTS_shop_secret_key = "HYP7O5kXRjGPRVRK";

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

        public FileContainer[] SaveTicketToTempFolder(string bookId)
        {
            try
            {
                string orderId = bookId.Replace("tk_", "");

                string path_template = "https://old.anywayanyday.com/en/order/receipt/?Compact=True&OrderId={order_id}";
                string url = path_template.Replace("{order_id}", orderId);

                string file_path = this.ticket_folder + "\\" + bookId + ".html";
                StreamWriter sw = new StreamWriter(file_path);

                WebClient wcl = new WebClient();
                string ticket = wcl.DownloadString(url);

                ticket = ticket.Replace("/images/logo_text.png", "http://clickandtravel.ru/rg_images/logo_clickandtravel.png")
                               .Replace("anywayanyday.com", "clickandtravel.ru")
                               .Replace("anywayanyday", "clickandtravel")
                               .Replace("width=\"288\"", "")
                               .Replace("class=\"for_print\" style=\"", "class=\"for_print\" style=\"font-size:260%; padding: 0 5px 5px 0; font-weight: bold; font-family: Tahoma, sans-serif; color#333\">www.clickandtravel.ru</div><div style=\"display:none;")
                               .Replace("/images/icoPrint_white.gif", "http://clickandtravel.ru/rg_images/icoPrint_white.gif")
                               .Replace("\">Print</", "font-size:150%\">Распечатать</")
                               .Replace("<span style=\"color:#F04B7D;\">any</span><span style=\"color:#323741\">way</span><span style=\"color:#F04B7D;\">any</span><span style=\"color:#323741\">day</span>", "<img src=\"http://clickandtravel.ru/rg_images/logo_clickandtravel.png\"/>")
                               .Replace("height=\"50\"", "");

                sw.Write(ticket);
                sw.Flush();
                sw.Close();

                return new FileContainer[] { new FileContainer() { FilePath = file_path, FileTitle = "Авиабилет" } };
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("save ticket exception: " + ex.Message + "\n" + ex.StackTrace);
                return new FileContainer[0];
            }
        }

        public TicketInfo GetTicketInfo(string bookId)
        {
            //авторизоваться
            var authToken = AuthUser(userLogin, userPassword);

            //получить инфо о заказах
            string orderId = bookId.Replace("tk_", "");

            var request = TKTS_service_uri + TKTS_booking_details + "key=" + TKTS_key;
            request += "&auth_key=" + authToken;
            request += "&locator=" + orderId;

            XmlDocument orderDoc = new XmlDocument();
            orderDoc.LoadXml(makeHttpRequest(request));

            XmlNodeList orderList = orderDoc.GetElementsByTagName("booking");

            if (orderList.Count == 0) return null;

            XmlElement orderElement = (orderList[0] as XmlElement);

            //получить стоимость
            decimal total_price = Convert.ToDecimal(orderElement.GetElementsByTagName("UAH")[0].InnerText.Replace(".", ","));

            string turist_name = orderElement.GetElementsByTagName("lastname")[0].InnerText;
            string route_from = orderElement.GetElementsByTagName("departure-location")[0].InnerText;
            string route_to = "/";

            //количество туристов
            int tst_count = orderElement.GetElementsByTagName("passenger").Count;

            //статус заказа
            bool isBooking = (orderElement.GetElementsByTagName("status")[0].InnerText == "W");

            return new TicketInfo()
            {
                MainTurist = turist_name,
                TuristCount = tst_count,
                RouteFrom = route_from,
                RouteTo = route_to,
                RubPrice = total_price,
                IsBooking = isBooking
            };
        }

        public decimal GetUsersBalance()
        {
             var request = TKTS_service_uri + TKTS_balance + "key=" + TKTS_key + "&service=avia";

             XmlDocument orderDoc = new XmlDocument();
             orderDoc.LoadXml(makeHttpRequest(request));

             string strAmount = (orderDoc.GetElementsByTagName("balance")[0] as XmlElement).GetAttribute("amount").Replace(".", ",");

             decimal amount = Convert.ToDecimal(strAmount);

             return amount;
        }

        public string BuyTicket(string bookId)
        {
            string orderId = bookId.Replace("tk_", "");

            //авторизоваться
            var authToken = AuthUser(userLogin, userPassword);

            //получить инфо о заказе
            var ticketInfo = GetTicketInfo(bookId);

            if (!ticketInfo.IsBooking)
                return "Error. Check order status ";

            var balance = GetUsersBalance();

            if (balance >= ticketInfo.RubPrice)
            {
                string request = TKTS_service_uri + TKTS_confirm + "key=" + TKTS_key;
                string amount = Math.Round(ticketInfo.RubPrice, 2).ToString().Replace(",", ".");

                string signCandidate = TKTS_shop_api_key + "avia" + orderId + amount + TKTS_shop_secret_key;
                string sign = CalculateMD5Hash(signCandidate).ToLower();

                request += "&service=avia";
                request += "&order_id=" + orderId;
                request += "&amount=" + amount;
                request += "&signature=" + sign;

                var response = makeHttpRequest(request);

                XmlDocument orderDoc = new XmlDocument();
                orderDoc.LoadXml(makeHttpRequest(request));

                var state = (orderDoc.GetElementsByTagName("order")[0] as XmlElement).GetAttribute("paid");

                if(state == "true")
                    return "Success";
                else
                    return "Error. " + response;
            }
            else
                return "Error. Check balance";
        }

        public string BookFlight(string flightId, Customer customer, Passenger[] passengers, DateTime tour_end_date)
        {
            var idParts = flightId.Split('_');

            if (idParts.Length < 3) 
                throw new SearchFlightException("BookRequest Error: wrong flight id " + flightId );

            var authToken = AuthUser(userLogin, userPassword);

            ///avia/book.xml?&passengers[0][type]=ххх&passengers[0][firstname]=хххххх&passengers[0][lastname]=хххххх&passengers[0][birthday]=хх-хх-хххх&passengers[0][gender]=х&passengers[0][citizenship]=хх&passengers[0][docnum]=ххххххххх&passengers[0][doc_expire_date]=ххххххххх&passengers[0][bonus_card]=ххххххххх
            ///
            string request = TKTS_service_uri + TKTS_book + "key=" + TKTS_key;

            request += "&session_id=" + idParts[1];
            request += "&recommendation_id=" + flightId.Replace("tk_" + idParts[1] + "_", "");
            request += "&auth_key=" + authToken;

            for (int i=0; i<passengers.Length; i++)
                request += ConvertPassengerToRequest(passengers[i], i, tour_end_date);

            var response = makeHttpRequest(request);

            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(response);

            try
            {
                var orderId = xDoc.GetElementsByTagName("locator")[0].InnerText;
                return "tk_" + orderId;
            }
            catch (Exception)
            {
                throw new SearchFlightException("BookRequest " + request + " Create Reservation Error " + xDoc.InnerXml);
            }
        }

        private static string ConvertPassengerToRequest(Passenger ps, int index, DateTime tour_end_date)
        {
            string request = "";

            request += "&passengers[" + index + "][type]=" + GetPasengerType(ps.Birth, tour_end_date);

            request += "&passengers[" + index + "][firstname]=" + ps.Name;
            request += "&passengers[" + index + "][lastname]=" + ps.Fname;
            request += "&passengers[" + index + "][birthday]=" + ps.Birth.ToString("dd-MM-yyyy");
            request += "&passengers[" + index + "][gender]=" + ps.Gender;
            request += "&passengers[" + index + "][citizenship]=" + ps.Citizen;
            request += "&passengers[" + index + "][docnum]=" + ps.Pasport;

            if (ps.PassportExpireDate > DateTime.Today)
            {
                request += "&passengers[" + index + "][doc_expire_date]=" + ps.PassportExpireDate.ToString("dd-MM-yyyy");
                request += "&passengers[" + index + "][doctype]=PSP";
            }
            else
                request += "&passengers[" + index + "][doctype]=PS";

            if ((ps.FrequentFlyerNumber.Length > 2) && (ps.FrequentFlyerNumber.Length < 41))
                request += "&passengers[" + index + "][bonus_card]=" + ps.FrequentFlyerNumber;

            //request += "type=" + GetPasengerType(ps.Birth, tour_end_date);


            return request;
        }

        private static string GetPasengerType(DateTime birthDate, DateTime tour_end_date)
        {
            string type = "ADT";

            if ((tour_end_date - birthDate).Days / 365.25 < 2)
                type = "INF";
            else if ((tour_end_date - birthDate).Days / 365.25 < 12)
                type = "CHD";

            return type;
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
        public string AuthUser(string login, string password)
        {
            string request = TKTS_service_uri + TKTS_signin + "key=" + TKTS_key;

            request += string.Format("&email={0}&password={1}", login, password);

            var resp = makeHttpRequest(request);

            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(resp);

            if (xDoc.GetElementsByTagName("auth_key").Count > 0)
                return xDoc.GetElementsByTagName("auth_key")[0].InnerText;

            throw new SearchFlightException("AuthRequest " + request + "  Error " + xDoc.InnerXml);
        }

        public  string CreateUser(string login)
        {
            string request = TKTS_service_uri + TKTS_signup + "key=" + TKTS_key;

            request += string.Format("&email={0}&phone={1}&name={2}", login,"375295579176","cat");

            var resp = makeHttpRequest(request);

            return resp;
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

        public static string CalculateMD5Hash(string input)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(input);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
    }
}