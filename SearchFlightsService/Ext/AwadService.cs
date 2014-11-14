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
using System.IO;
using SearchFlightsService;
using System.Configuration;

namespace SearchFlightsService.Ext
{
    public class AwadService : IExternalService
    {
        private string ticket_folder = WebConfigurationManager.AppSettings["TempFolder"];


        #region Constants
        private const string AWAD_ApiPath = "http://api.anywayanyday.com";
        private const string AWAD_InitSearch = "/api/NewRequest/?Route=";
        private const string AWAD_RequestState = "/api/RequestState/?R=";
        private const string AWAD_Fares = "/api/Fares/?V=Matrix&VB=true&L=RU&PS=100&PN=1&R=";
        private const string AWAD_Rules = "/api/FareRules/?R=";
        private const string AWAD_Details = "/api/Fare/?C=RUR&R=";
        private const string AWAD_Confirmed = "/api/ConfirmFare/?R=";
        private const string AWAD_CreateReservation = "/api/CreateReservation/?";
        private const string AWAD_Pay_Order = "/api/PayReservation/?OrderId=";
        private const string AWAD_Order = "/api/GetOrder/?OrderId=";

        private static string AWAD_PartnerKey = "&Partner=clickandtravel76";//clickandtravel76";

        private decimal TOTAL_COEF = Convert.ToDecimal(ConfigurationManager.AppSettings["TOTAL_COEF"]);

        #endregion

        private string request_id = "";

        #region Public methods

        public TicketInfo GetTicketInfo(string bookId)
        {
            string orderId = bookId.Replace("aw_", "");

            XmlDocument orderDoc = new XmlDocument();
            orderDoc.LoadXml(makeHttpRequest(AWAD_ApiPath + AWAD_Order + orderId));
            XmlNodeList orderList = orderDoc.GetElementsByTagName("Order");

            if (orderList.Count == 0) return null;

            XmlElement orderElement = (orderList[0] as XmlElement);

            //получить стоимость
            decimal total_price = Convert.ToDecimal(orderElement.GetAttribute("Amount").ToString().Replace(".", ","));

            string turist_name = orderElement.GetElementsByTagName("LastName")[0].InnerText;
            string route_from = (orderElement.GetElementsByTagName("Leg")[0] as XmlElement).GetAttribute("FN").ToString();
            string route_to = "/";

            //количество туристов
            int tst_count = orderElement.GetElementsByTagName("Ticket").Count;

            //статус заказа
            bool isBooking = (orderElement.GetAttribute("Status").ToString() == "WaitingCustomerConfirm");


            return new TicketInfo()
            {
                MainTurist = turist_name,
                TuristCount = tst_count,
                RouteFrom = route_from,
                RouteTo = route_to,
                RubPrice = Convert.ToInt32(total_price),
                IsBooking = isBooking
            };
        }

        public string BuyTicket(string bookId)
        {
            string orderId = bookId.Replace("aw_", "");

            XmlDocument orderDoc = new XmlDocument();
            string response = makeHttpRequest(AWAD_ApiPath + AWAD_Pay_Order + orderId);
            orderDoc.LoadXml(response);

            Logger.Logger.WriteToLog("AWAD_PAY_RESPONE:" + response);

            XmlNodeList orderList = orderDoc.GetElementsByTagName("PayReservation");

            if (orderList.Count == 0) return null;

            XmlElement orderElement = (orderList[0] as XmlElement);

            if (orderElement.HasAttribute("Status") && (orderElement.GetAttribute("Status") == "SUCCESSFUL"))
                return "Success";

            return "Error. " + response;
        }

        public FileContainer[] SaveTicketToTempFolder(string bookId)
        {
            try
            {
                string orderId = bookId.Replace("aw_", "");

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

        public string InitSearch(Route route, int adult, int children, int inf, string serviceClass)
        {
            return awadInitSearch(route, adult, children, inf, serviceClass);
        }

        public Flight[] GetFlights(string search_id)
        {
            return FaresToFlights(Get_Fares(this.request_id));
        }

        public Fare[] Get_Fares(string search_id)
        {
            //проверяем, завершился ли поиск. отдаем null если поиск не завершен
            //в случае чего, awadGetState сгенерирует исключение
            if (awadGetState(this.request_id) < 100) return null;

            if (this.request_id == "") return new Fare[0];

            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(makeHttpRequest(AWAD_ApiPath + AWAD_Fares + this.request_id));

            XmlNodeList fares_list = xDoc.GetElementsByTagName("FareVerbose");

            Fare[] result = new Fare[fares_list.Count];

            for (int i = 0; i < result.Length; i++)
            {
                XmlElement el = fares_list[i] as XmlElement;

                Fare fare = new Fare();
                fare.Id = "aw_" + this.request_id + "_" + el.GetAttribute("F").ToString();

                ///PAYU!!!!
                fare.Price = Convert.ToInt32(el.GetAttribute("TotalAmount"));
                fare.Price = Convert.ToInt32(Math.Ceiling(fare.Price * TOTAL_COEF));

                //применяем наценку, в зависимости от стоимость билета
                // if (fare.Price < 15500)
                //  fare.Price = Convert.ToInt32(Math.Ceiling(fare.Price * 1.02));
                //else if (fare.Price < 24800)
                //    fare.Price = Convert.ToInt32(Math.Ceiling(fare.Price * 1.005));


                XmlNodeList als = el.GetElementsByTagName("Airline");

                fare.Airline = (als[0] as XmlElement).GetAttribute("N");
                fare.AirlineCode = (als[0] as XmlElement).GetAttribute("C");

                XmlNodeList dirs = el.GetElementsByTagName("Dir");

                fare.Directions = new Direction[dirs.Count];

                for (int j = 0; j < dirs.Count; j++)
                    fare.Directions[j] = dirToDirection(dirs[j] as XmlElement);

                result[i] = fare;
            }
            return result;
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
            string content = makeHttpRequest(AWAD_ApiPath + AWAD_CreateReservation + requestStr, 80);
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
                orderDoc.LoadXml(makeHttpRequest(AWAD_ApiPath + AWAD_Order + orderId));
                XmlNodeList orderList = orderDoc.GetElementsByTagName("Order");

                if ((orderList[0] as XmlElement).GetAttribute("OrderAlreadyExists").ToString() == "true")
                {
                    Logger.Logger.WriteToBookLog("ALREADY EXISTS!!");
                    return "can not book";
                }
                //узнать таймлимит

                return "aw_" + orderId;//+ (orderList[0] as XmlElement).GetAttribute("IdentifierNumber").ToString();
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

        public Flight[] FaresToFlights(Fare[] fares)
        {
            if (fares == null) return null;

            ArrayList flights_arrays = new ArrayList();

            //все тарифы разбиваем на перелеты (тикеты)
            foreach (Fare fare in fares)
                flights_arrays.AddRange(fareToFlights(fare));

            flights_arrays.Sort(new FlightsComparer());

            return flights_arrays.ToArray(typeof(Flight)) as Flight[];
        }

        public FlightRules GetFareRules(string flightId)
        {
            //берем параметры тарифа и вариант перелета
            string[] arr = flightId.Replace("~", "_").Split('_');
            string requestStr = arr[1] + "&F=" + arr[2] + "&V=" + arr[3].Replace("^", ";").TrimEnd(new char[] { ';' });

            //делаем запрос
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(makeHttpRequest(AWAD_ApiPath + AWAD_Rules + requestStr));

            XmlNodeList nList = xDoc.GetElementsByTagName("Direction");

            bool CBD = true;
            bool CAD = true;
            bool RBD = true;
            bool RAD = true;

            string rulesText = "";

            foreach (XmlElement nodeItem in nList)
            {
                //маршрут
                string depCity = (nodeItem.GetElementsByTagName("Dep")[0] as XmlElement).GetAttribute("City");
                string arrCity = (nodeItem.GetElementsByTagName("Arr")[0] as XmlElement).GetAttribute("City");

                var header = nodeItem.GetElementsByTagName("Header")[0] as XmlElement;

                CBD &= header.GetAttribute("CBD") == "true";
                CAD &= header.GetAttribute("CAD") == "true";
                RBD &= header.GetAttribute("RBD") == "true";
                RAD &= header.GetAttribute("RAD") == "true";

                rulesText += depCity + " - " + arrCity + "<br>\n<br>\n" + nodeItem.GetElementsByTagName("Rules")[0].InnerText + "<br>\n<br>\n<br>\n<br>\n";
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
        public string Get_Fare_TimeLimit(string flightId)
        {
            //берем параметры тарифа и вариант перелета
            string[] arr = flightId.Replace("~", "_").Split('_');
            string requestStr = arr[1] + "&F=" + arr[2] + "&V=" + arr[3].Replace("^", ";").TrimEnd(new char[] { ';' });

            //делаем запрос
            XmlDocument xDoc = new XmlDocument();
            xDoc.LoadXml(makeHttpRequest(AWAD_ApiPath + AWAD_Details + requestStr));

            XmlNodeList nList = xDoc.GetElementsByTagName("ReservationTimeLimit");

            if (nList.Count == 1)
                return (nList[0] as XmlElement).GetAttribute("Date").ToString() + " " + (nList[0] as XmlElement).GetAttribute("Time").ToString();
            else
                return "";
        }
        #endregion

        #region xmlToClass
        private Direction dirToDirection(XmlElement dir)
        {
            Direction direction = new Direction();

            XmlNodeList variants = dir.GetElementsByTagName("Variant");

            direction.Variants = new Variant[variants.Count];

            for (int i = 0; i < variants.Count; i++)
                direction.Variants[i] = varToVariant(variants[i] as XmlElement);

            return direction;
        }

        private Variant varToVariant(XmlElement _var)
        {
            Variant variant = new Variant();
            variant.FlightTime = _var.GetAttribute("TT").ToString();

            string time = variant.FlightTime.TrimStart(new char[] { '0' });
            time = time.Replace(":", " ч. ") + " мин.";

            variant.FlightTime = time;

            variant.Id = _var.GetAttribute("Id").ToString();

            XmlNodeList legs = _var.GetElementsByTagName("Leg");
            variant.Legs = new Leg[legs.Count];

            for (int i = 0; i < legs.Count; i++)
                variant.Legs[i] = legFromXmlElement(legs[i] as XmlElement);

            return variant;
        }

        private Leg legFromXmlElement(XmlElement _leg)
        {
            Leg leg = new Leg();

            leg.FlightNumber = _leg.GetAttribute("FN").ToString();
            leg.Airline = leg.FlightNumber.Substring(0, 2);
            leg.FlightNumber = leg.FlightNumber.Substring(3);

            XmlNodeList planes = _leg.GetElementsByTagName("Plane");
            XmlNodeList departure = _leg.GetElementsByTagName("Departure");
            XmlNodeList arrival = _leg.GetElementsByTagName("Arrival");

            leg.Board = (planes[0] as XmlElement).GetAttribute("C");
            leg.BoardName = (planes[0] as XmlElement).GetAttribute("N");
            leg.BookingClass = _leg.GetAttribute("BC").ToString();
            leg.ServiceClass = _leg.GetAttribute("SC").ToString();

            leg.DateBegin = Convert.ToDateTime((departure[0] as XmlElement).GetAttribute("Date") + " " + (departure[0] as XmlElement).GetAttribute("Time"));
            leg.DateEnd = Convert.ToDateTime((arrival[0] as XmlElement).GetAttribute("Date") + " " + (arrival[0] as XmlElement).GetAttribute("Time"));

            leg.Duration = getFlightTime(_leg.GetAttribute("FT"));
            leg.LocationBegin = (departure[0] as XmlElement).GetAttribute("Code");
            leg.LocationBeginName = (departure[0] as XmlElement).GetAttribute("City");
            leg.LocationEnd = (arrival[0] as XmlElement).GetAttribute("Code");
            leg.LocationEndName = (arrival[0] as XmlElement).GetAttribute("City");
            leg.AirportBeginName = (departure[0] as XmlElement).GetAttribute("Airport");
            leg.AirportEndName = (arrival[0] as XmlElement).GetAttribute("Airport");

            return leg;
        }
        #endregion

        #region Private methods
        private int getFlightTime(string fl_time)
        {
            string[] time_arr = fl_time.Split(':');

            return Convert.ToInt32(time_arr[0]) * 60 + Convert.ToInt32(time_arr[1]);
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
                Logger.Logger.WriteToLog("AWAD Exception for request: " + path + "  " + ex.Message + " " + ex.StackTrace);
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
                Logger.Logger.WriteToLog("AWAD Exception for request: " + path + "  " + ex.Message + " " + ex.StackTrace);
                return "<root></root>";
            }
        }

        private string awadInitSearch(Route route, int adult, int children, int inf, string serviceClass) //returns search_id
        {
            if (WebConfigurationManager.AppSettings.AllKeys.Contains("AwadPartnerId"))
            {
                AWAD_PartnerKey = "&Partner=" + WebConfigurationManager.AppSettings["AwadPartnerId"];
            }

            string request = route.ToString();
            request += "&AD=" + adult + "&CN=" + children + "&IN=" + inf + "&SC=" + serviceClass + AWAD_PartnerKey;

            XmlDocument xDoc = new XmlDocument();
            // отправляем запрос на поиск
            xDoc.LoadXml(makeHttpRequest(AWAD_ApiPath + AWAD_InitSearch + request, 45));

            try
            {
                // получаем request_id
                XmlNodeList el = xDoc.GetElementsByTagName("NewRequest");
                XmlElement result = el[0] as XmlElement;

                this.request_id = result.GetAttribute("Id").ToString();
                return this.request_id;//request_id
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("AWAD INIT Exception for request: " + AWAD_ApiPath + AWAD_InitSearch + request + "  " + ex.Message + " " + ex.StackTrace);
                this.request_id = "";
                return "";
                // throw new Exception("Exception while init search " + ex.Message + "\n" + ex.StackTrace + "\n\n" + xDoc.InnerXml);
            }
        }

        private int awadGetState(string search_id)
        {
            XmlDocument xDoc = new XmlDocument();

            if (search_id.Length == 0) return 100;

            // делаем запрос о соостоянии поиска
            xDoc.LoadXml(makeHttpRequest(AWAD_ApiPath + AWAD_RequestState + search_id, 15));

            try
            {
                // получаем процент выполнения
                XmlNodeList el = xDoc.GetElementsByTagName("RequestState");
                XmlElement result = el[0] as XmlElement;

                return Convert.ToInt32(result.GetAttribute("Completed"));
            }
            catch (Exception ex)
            {
                return 0;
                throw new Exception("Exception while getting request state " + ex.Message + "\n" + ex.StackTrace + "\n\n" + xDoc.InnerXml);
            }
        }

        //разбивает тариф на "тикеты" (Flight)
        private ArrayList fareToFlights(Fare fare)
        {
            //для сложного маршрута нужно смешать все варианты перелетов на каждом участке
            Variant[][] variants = mixVariants(fare.Directions);

            //из одного тарифа получится а1*а2*..*an разных перелетов, где ax количество вариантов перелета на участле "х"
            ArrayList flights = new ArrayList();

            //создаем объект Flight
            for (int i = 0; i < variants.Length; i++)
            {
                Variant[] vars = variants[i];
                Flight fl = new Flight();

                //копируем авикомпанию
                fl.Airline = fare.Airline;
                fl.AirlineCode = fare.AirlineCode;
                fl.TimeLimit = "";
                //цена из тарифа
                fl.Price = fare.Price;

                //задаем количество участков маршрута
                fl.Parts = new FlightPart[vars.Length];

                //айдишник формируется из айдишника тарифа + айдишников используемых вариантов
                string id = fare.Id + "~";
                for (int j = 0; j < vars.Length; j++)
                {
                    id += vars[j].Id + ";";
                    fl.Parts[j] = new FlightPart(vars[j].Legs, CalcDuration(vars[j].FlightTime));
                }
                fl.Id = id;
                flights.Add(fl);
            }
            return flights;
        }

        private int CalcDuration(string flightTime)
        {
            flightTime = flightTime.Replace(" ч. ", ":").Replace(" мин.", "");

            string[] arr_str = flightTime.Split(':');

            int res = 0;

            if (arr_str.Length > 1)
            {
                if (arr_str[0].TrimStart(new char[] { '0' }).Length > 0)
                    res += Convert.ToInt32(arr_str[0].TrimStart(new char[] { '0' })) * 60;

                if (arr_str[1].TrimStart(new char[] { '0' }).Length > 0)
                    res += Convert.ToInt32(arr_str[1].TrimStart(new char[] { '0' }));

                return res;
            }

            return 0;
        }

        private Variant[][] mixVariants(Direction[] directions)
        {
            if (directions.Length == 1)
            {
                Direction dir = directions[0];
                Variant[][] mix = new Variant[dir.Variants.Length][];

                for (int i = 0; i < dir.Variants.Length; i++)
                    mix[i] = new Variant[1] { dir.Variants[i] };

                return mix;
            }
            else
            {
                //shift directions
                Direction[] shift_dirs = new Direction[directions.Length - 1];

                for (int i = 0; i < shift_dirs.Length; i++)
                    shift_dirs[i] = directions[i + 1];

                Variant[][] down_mix = mixVariants(shift_dirs);

                Variant[] current_variants = directions[0].Variants;

                Variant[][] new_mix = new Variant[down_mix.Length * current_variants.Length][];

                for (int i = 0; i < current_variants.Length; i++)
                {
                    Variant variant = current_variants[i];

                    for (int j = 0; j < down_mix.Length; j++)
                    {
                        int index = i * down_mix.Length + j;

                        Variant[] down_variant = down_mix[j];
                        Variant[] new_variant = new Variant[down_variant.Length + 1];
                        new_variant[0] = variant;

                        for (int k = 0; k < down_variant.Length; k++)
                            new_variant[k + 1] = down_variant[k];

                        new_mix[index] = new_variant;
                    }
                }
                return new_mix;
            }
        }
        #endregion
    }
}