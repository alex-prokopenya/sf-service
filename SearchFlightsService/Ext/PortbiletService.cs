using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SearchFlightsService.Containers;
using SearchFlightsService.DB;
using System.Collections;
using SearchFlightsService.PortbiletWebService;
using System.Threading;
using System.Security.Cryptography;
using System.IO;
using System.Web.Configuration;
using System.Text.RegularExpressions;
using System.Configuration;

namespace SearchFlightsService.Ext
{
    public class PortbiletService : IExternalService
    {
        private string LOCALE = "ru";
        private string LOGIN_NAME = "info@clickandtravel.ru";
        private string PASSWORD = "maqt72c";
        private string SALES_POINT_CODE = "WEB_SITE";
        private string komisionAirlines = "A3,BT,AB,AZ,AA,CI,DL,EK,9W,AF,OS,BA,OK,AY,IB,KL,LO,LH,QR,SK,S7,LX,TP";

        private string ticket_folder = WebConfigurationManager.AppSettings["TempFolder"];

        private decimal TOTAL_COEF = Convert.ToDecimal(ConfigurationManager.AppSettings["TOTAL_COEF"]);

        #region Fields
        private Hashtable hashResults = new Hashtable();

        private FlightSearchResult result = null;

        private Route route;

        private int adult;

        private int children;

        private int inf;

        private string serviceClass;

        //    private string searchId;

        //  public string SearchId
        //  {
        //      get { return searchId; }
        //      set { searchId = value; }
        //  }

        private GDSWebServiceService sc = new GDSWebServiceService();

        private Dictionary<string, string> airportsLib = null;

        private Dictionary<string, string> airlinesLib = null;

        #endregion

        #region Constructors
        public PortbiletService()
        { }

        public PortbiletService(Route route, int adult, int children, int inf, string serviceClass)
        {
            this.route = route;
            this.adult = adult;
            this.children = children;
            this.inf = inf;
            this.serviceClass = serviceClass;
            //    this.searchId = route.ToString() + "-" + adult + "-" + children + "-" + inf + "-" + serviceClass;
        }
        #endregion

        #region Public methods

        public TicketInfo GetTicketInfo(string bookId)
        {
            BookingCreateResult bcr = this.sc.loadBooking(new InvocationContext()
            {
                locale = this.LOCALE,
                loginName = this.LOGIN_NAME,
                password = this.PASSWORD,
                salesPointCode = this.SALES_POINT_CODE
            },
                                                           new BookingLoadParameters()
                                                           {
                                                               bookingNumber = bookId.Replace("pb_", "")
                                                           });


            //получить стоимость
            decimal total_price = 0;
            string turist_name = "";
            string route_from = "";
            string route_to = "";
            int tst_count = 0;

            bool isBooking = true;

            int cnt = 0;

            foreach (PortbiletWebService.Product pc in bcr.bookingFile.reservations[0].products.Items)
            {
                tst_count++;
                foreach (PriceElement p_el in pc.price)
                    total_price += p_el.amount;

                isBooking = isBooking && (pc.status == productStatus.BOOKING);

                if (cnt++ == 0)
                {
                    turist_name = pc.passenger.passport.lastName;
                    route_from = pc.segments[0].locationBegin.code;
                    route_to = pc.segments[0].locationEnd.code;
                }
            }

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
            try
            {
                string bookingNumber = bookId.Replace("pb_", "");

                BookingCreateResult bcr = this.sc.selectFOP(new InvocationContext()
                {
                    locale = this.LOCALE,
                    loginName = this.LOGIN_NAME,
                    password = this.PASSWORD,
                    salesPointCode = this.SALES_POINT_CODE
                },
                new SelectFOPParameters()
                {
                    bookingNumber = bookingNumber,
                    paymentType = paymentType.INVOICE
                });

                if ((bcr.messages.Count() == 0) || (bcr.messages[0].type != messageType.ERROR))
                {
                    BookingCreateResult bcr2 = this.sc.issueTickets(new InvocationContext()
                    {
                        locale = this.LOCALE,
                        loginName = this.LOGIN_NAME,
                        password = this.PASSWORD,
                        salesPointCode = this.SALES_POINT_CODE
                    },
                    new IssueTicketsParameters()
                    {
                        bookingNumber = bookingNumber,
                    });

                    if ((bcr2.messages.Count() == 0) || (bcr2.messages[0].type != messageType.ERROR))
                        return "Success";
                    else
                        return "Error. While select payment type: " + bcr2.messages[0].message + ". cnt " + bcr2.messages.Count();
                }
                else
                    return "Error. While select payment type: " + bcr.messages[0].message + ". cnt " + bcr.messages.Count();
            }
            catch (Exception ex)
            {
                return "Error. " + ex.Message;
            }
        }

        public FileContainer[] SaveTicketToTempFolder(string bookId)
        {
            try
            {
                string bookingNumber = bookId.Replace("pb_", "");

                BookingCreateResult bcr2 = this.sc.loadBooking(new InvocationContext()
                {
                    locale = this.LOCALE,
                    loginName = this.LOGIN_NAME,
                    password = this.PASSWORD,
                    salesPointCode = this.SALES_POINT_CODE
                },
                new BookingLoadParameters()
                {
                    bookingNumber = bookingNumber,
                });

                int cnt = 1;

                FileContainer[] res = new FileContainer[bcr2.bookingFile.documents.Length];

                foreach (Document doc in bcr2.bookingFile.documents)
                {
                    string fileName = ticket_folder + "/" + cnt + "_" + doc.name;

                    FileStream fs = new FileStream(fileName, FileMode.Create);
                    fs.Write(doc.content, 0, doc.content.Length);
                    fs.Flush();
                    fs.Close();

                    res[(cnt++ - 1)] = new FileContainer() { FileTitle = doc.title.Replace("Маршрутная квитанция", "Авиабилет"), FilePath = fileName };
                }

                return res;
            }
            catch (Exception ex)
            {
                return new FileContainer[0];// "Error. " + ex.Message;
            }
        }

        public string InitSearch(Route route, int adult, int children, int inf, string serviceClass)
        {
            throw new SearchFlightException("InitSearch");
        }

        public void InitSearch() // returns searchId
        {
            try
            {

                FlightSearchParameters pars = new FlightSearchParameters();
                pars.eticketsOnly = true;
                pars.mixedVendors = true;

                #region Route
                pars.route = new PortbiletWebService.RouteSegment[this.route.Segments.Length];

                for (int i = 0; i < pars.route.Length; i++)
                {
                    Containers.RouteSegment rs = this.route.Segments[i];
                    pars.route[i] = new PortbiletWebService.RouteSegment();
                    pars.route[i].date = rs.Date;
                    pars.route[i].locationBegin = new DictionaryItem() { code = rs.LocationBegin };
                    pars.route[i].locationEnd = new DictionaryItem() { code = rs.LocationEnd };
                }
                #endregion

                #region Passangers
                int sp_count = 0;
                SeatsPreferences ad_sp = null;// new SeatsPreferences();
                if (adult > 0)
                {
                    ad_sp = new SeatsPreferences();
                    ad_sp.count = adult;
                    ad_sp.passengerType = passengerType.ADULT;
                    sp_count++;
                }

                SeatsPreferences ch_sp = null;// new SeatsPreferences();
                if (children > 0)
                {
                    ch_sp = new SeatsPreferences();
                    ch_sp.count = children;
                    ch_sp.passengerType = passengerType.CHILD;
                    sp_count++;
                }

                SeatsPreferences inf_sp = null;// new SeatsPreferences();

                if (inf > 0)
                {
                    inf_sp = new SeatsPreferences();
                    inf_sp.count = inf;
                    inf_sp.passengerType = passengerType.INFANT;
                    sp_count++;
                }

                SeatsPreferences[] seats = new SeatsPreferences[sp_count];
                int added = 0;
                if (ad_sp != null) seats[added++] = ad_sp;
                if (ch_sp != null) seats[added++] = ch_sp;
                if (inf_sp != null) seats[added++] = inf_sp;
                #endregion

                pars.seats = seats;
                pars.serviceClass = classOfService.ECONOMY;
                if (this.serviceClass == "E")
                    pars.serviceClass = classOfService.ECONOMY;
                else if (this.serviceClass == "B")
                    pars.serviceClass = classOfService.BUSINESS;

                pars.skipConnected = false;

                this.result = this.sc.searchFlights(new InvocationContext() { locale = this.LOCALE, loginName = this.LOGIN_NAME, password = this.PASSWORD, salesPointCode = this.SALES_POINT_CODE }, pars);


            }
            catch (Exception ex)
            {
                this.result = new FlightSearchResult();
                this.result.flights = null;
                Logger.Logger.WriteToLog("!!!PORTBILET INITSEARCH ERROR " + ex.Message + " " + ex.StackTrace);
            }
        }

        public Containers.Flight[] GetFlights(string search_id)
        {
            if (this.result == null)
                return null;

            if (this.result.flights != null)
                return ConvertToFlights(this.result);//convert portbilet flights to containers.flights
            else
                return new Containers.Flight[0];// throw new SearchFlightException("get_Flights_Portbilet " + this.result.messages[0].message);
        }

        static byte[] GetBytes(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                                        .Where(x => x % 2 == 0)
                                        .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                                        .ToArray();
        }

        public string BookFlight(string flightId, Containers.Customer customer, Containers.Passenger[] passengers, DateTime tour_end_date)
        {
            //return "pb_0375901";
            BookingCreateParameters pars = new BookingCreateParameters();
            pars.customer = new PortbiletWebService.Customer() { name = customer.Name, phone = "74957846256", email = "info@clickandtravel.ru" };

            //!!!!!!!!!
            string id = "";
            using (DB.DataStore ds = new DB.DataStore())
            {
                try
                {
                    id = ds.GetFullId(flightId);
                }
                catch (Exception ex)
                {
                    Logger.Logger.WriteToLog("pb_can not book:  " + ex.Message + "\n" + ex.StackTrace + " flight_id = " + flightId);
                }
            }

            //для проверки найденного токена, считаем контрольную сумму
            string check_short_id = "pb_" + CalculateMD5Hash(id);

            //если суммы не верна, пишем в лог
            if (check_short_id != flightId)
            {
                Logger.Logger.WriteToLog("AHTUNG!!! wrong full id " + id + " for shortId " + flightId);
                return "can not book";
            }


            pars.flightToken = GetBytes(id);

            pars.passengers = new PortbiletWebService.Passenger[passengers.Length];

            for (int i = 0; i < passengers.Length; i++)
            {
                passengerType type = passengerType.ADULT;

                if ((tour_end_date - passengers[i].Birth).Days / 365.25 < 2)
                    type = passengerType.INFANT;
                else if ((tour_end_date - passengers[i].Birth).Days / 365.25 < 12)
                    type = passengerType.CHILD;

                pars.passengers[i] = new PortbiletWebService.Passenger()
                {
                    bonusCardAirline = new DictionaryItem() { code = passengers[i].FrequentFlyerAirline },
                    bonusCardNumber = passengers[i].FrequentFlyerNumber,
                    insurePassenger = false,
                    countryCode = passengers[i].Citizen.Substring(2),
                    type = type,

                    phoneType = communicationType.AGENCY,

                    phoneNumber = "84957846256",
                    phoneTypeSpecified = true,
                    passport = new Passport()
                    {
                        birthday = passengers[i].Birth,
                        birthdaySpecified = true,
                        citizenship = new DictionaryItem() { code = passengers[i].Citizen.Substring(2) },

                        expired = passengers[i].Passport_expire_date,
                        expiredSpecified = true,

                        firstName = passengers[i].Name,
                        gender = passengers[i].Gender == "M" ? gender.MALE : gender.FEMALE,
                        genderSpecified = true,

                        issuedSpecified = false,

                        lastName = passengers[i].Fname,

                        middleName = Translit(passengers[i].Otc),

                        number = passengers[i].Pasport,
                        type = passengers[i].Citizen.Substring(2).ToUpper() == "RU" ?
                                    (passengers[i].Pasport.Length == 9 ?
                                            passportType.FOREIGN
                                            : passportType.INTERNAL)
                                    : passportType.PASSPORT,

                        typeSpecified = true
                    }
                };

                Logger.Logger.WriteToBookLog(passengers[i].Name);
                Logger.Logger.WriteToBookLog(passengers[i].Fname);
                Logger.Logger.WriteToBookLog(passengers[i].Otc);


                if (Regex.IsMatch(pars.passengers[i].passport.number, @"\p{IsCyrillic}"))
                {
                    pars.passengers[i].passport.type = passportType.BIRTHDAY_NOTIFICATION;
                    pars.passengers[i].passport.number = Translit(pars.passengers[i].passport.number);

                    Logger.Logger.WriteToBookLog("BIRTHDAY_NOTIFICATION applied");
                }
            }

            this.sc.Timeout = 100000;

            BookingCreateResult bcr = sc.createBooking(new InvocationContext()
            {
                locale = this.LOCALE,
                loginName = this.LOGIN_NAME,
                password = this.PASSWORD,
                salesPointCode = this.SALES_POINT_CODE
            },
                                                       pars);



            if ((bcr.bookingFile != null) && (bcr.bookingFile.status != workflowStatus.ERROR))
            {

                Logger.Logger.WriteToLog("id=" + flightId + " full_id=" + id + " booking = " + bcr.bookingFile.number);

                Logger.Logger.WriteToBookLog("id=" + flightId + " full_id=" + id + " booking = " + bcr.bookingFile.number);
                //узнать таймлимит
                return "pb_" + bcr.bookingFile.number;
            }

            if (bcr.messages[0].message == "не удалось подтвердить места в авиакомпании")
            {
                Logger.Logger.WriteToLog("pb_can not book: flightId " + flightId);
                Logger.Logger.WriteToBookLog("pb_can not book: flightId " + flightId);
                return "can not book";
            }
            else
            {
                foreach (Message message in bcr.messages)
                {
                    Logger.Logger.WriteToLog("pb_exception_message: for flightId " + flightId + " id= " + id + " text: " + message.message + " " + message.details);
                    Logger.Logger.WriteToBookLog("pb_exception_message: for flightId " + flightId + " id= " + id + " text: " + message.message + " " + message.details);
                }
            }

            throw new SearchFlightException("BookFlight");
        }

        private static string Translit(string inp)
        {
            string outp = inp;
            Dictionary<string, string> words = new Dictionary<string, string>();
            words.Add("а", "a");
            words.Add("б", "b");
            words.Add("в", "v");
            words.Add("г", "g");
            words.Add("д", "d");
            words.Add("е", "e");
            words.Add("ё", "yo");
            words.Add("ж", "zh");
            words.Add("з", "z");
            words.Add("и", "i");
            words.Add("й", "j");
            words.Add("к", "k");
            words.Add("л", "l");
            words.Add("м", "m");
            words.Add("н", "n");
            words.Add("о", "o");
            words.Add("п", "p");
            words.Add("р", "r");
            words.Add("с", "s");
            words.Add("т", "t");
            words.Add("у", "u");
            words.Add("ф", "f");
            words.Add("х", "h");
            words.Add("ц", "c");
            words.Add("ч", "ch");
            words.Add("ш", "sh");
            words.Add("щ", "sch");
            words.Add("ъ", "j");
            words.Add("ы", "i");
            words.Add("ь", "j");
            words.Add("э", "e");
            words.Add("ю", "yu");
            words.Add("я", "ya");
            words.Add("А", "A");
            words.Add("Б", "B");
            words.Add("В", "V");
            words.Add("Г", "G");
            words.Add("Д", "D");
            words.Add("Е", "E");
            words.Add("Ё", "Yo");
            words.Add("Ж", "Zh");
            words.Add("З", "Z");
            words.Add("И", "I");
            words.Add("Й", "J");
            words.Add("К", "K");
            words.Add("Л", "L");
            words.Add("М", "M");
            words.Add("Н", "N");
            words.Add("О", "O");
            words.Add("П", "P");
            words.Add("Р", "R");
            words.Add("С", "S");
            words.Add("Т", "T");
            words.Add("У", "U");
            words.Add("Ф", "F");
            words.Add("Х", "H");
            words.Add("Ц", "C");
            words.Add("Ч", "Ch");
            words.Add("Ш", "Sh");
            words.Add("Щ", "Sch");
            words.Add("Ъ", "J");
            words.Add("Ы", "I");
            words.Add("Ь", "J");
            words.Add("Э", "E");
            words.Add("Ю", "Yu");
            words.Add("Я", "Ya");

            foreach (string key in words.Keys)
                outp = outp.Replace(key, words[key]);

            return outp;
        }

        public int GetTimelimit(string order_number)
        {
            return 0;
        }

        public FlightDetails[] GetFlightDetails(string flightToken)
        {

            /*  //!!!!!!!!!
              string id = "";
              using (DB.DataStore ds = new DB.DataStore())
              {
                  try
                  {
                      id = ds.GetFullId(flightToken);
                  }
                  catch (Exception ex)
                  {
                      Logger.Logger.WriteToLog("pb_can not book:  " + ex.Message + "\n" + ex.StackTrace);
                  }
              }
              */

            FareRemarksSearchResult remarksRes = this.sc.searchRemarks(new InvocationContext()
            {
                locale = this.LOCALE,
                loginName = this.LOGIN_NAME,
                password = this.PASSWORD,
                salesPointCode = this.SALES_POINT_CODE
            },
                                                                            new FareRemarksSearchParameters() { remarksSearchContext = GetBytes(flightToken) }
                                                    );

            if (remarksRes.messages.Length > 0)
            {
                string res = "";
                foreach (Message ms in remarksRes.messages)
                    res += " + " + ms.message;

                Logger.Logger.WriteToLog("get details fail:" + res);
            }

            if (remarksRes.remarks == null) return new FlightDetails[0];

            FlightDetails[] arr = new FlightDetails[remarksRes.remarks.Length];

            for (int i = 0; i < remarksRes.remarks.Length; i++)
                arr[i] = new FlightDetails()
                {
                    Code = remarksRes.remarks[i].code,
                    Content = remarksRes.remarks[i].content,
                    Number = remarksRes.remarks[i].number,
                    Title = remarksRes.remarks[i].title
                };

            return arr;
        }

        public string BytesToString(byte[] arr)
        {
            string hex = BitConverter.ToString(arr);
            return hex.Replace("-", "");
        }

        #endregion

        #region Private methods

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


        private Containers.Flight[] ConvertToFlights(FlightSearchResult searchResults)
        {
            Logger.Logger.WriteToLog("port try convert flights");

            try
            {
                DB.DataStore ds = new DB.DataStore();

                if (this.airportsLib == null)
                    this.airportsLib = ds.AirportsLib();

                if (this.airlinesLib == null)
                    this.airlinesLib = ds.AirlinesLib();

                ArrayList result = new ArrayList();

                for (int i = 0; i < searchResults.flights.Length; i++)
                {
                    PortbiletWebService.Flight rf = searchResults.flights[i];

                    try
                    {
                        Containers.Flight fl = new Containers.Flight();
                        fl.TimeLimit = (rf.timeLimit > DateTime.Now) ? rf.timeLimit.ToString("yyyy-MM-dd HH:mm") : DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                        fl.Airline = this.airlinesLib.ContainsKey(rf.carrier.code) ? this.airlinesLib[rf.carrier.code].Trim() : rf.carrier.name;
                        fl.AirlineCode = rf.carrier.code;
                        fl.FullId = BytesToString(rf.token);
                        fl.Id = "pb_" + CalculateMD5Hash(fl.FullId);
                        //   fl.Gds = rf.gds == gdsName.si ? "sabre":"";
                        int price = 0;

                        for (int j = 0; j < rf.price.Length; j++)
                        {
                            switch (rf.price[j].passengerType)
                            {
                                case passengerType.ADULT:
                                    price += Convert.ToInt32(Math.Ceiling(rf.price[j].amount) * this.adult);
                                    break;

                                case passengerType.CHILD:
                                    price += Convert.ToInt32(Math.Ceiling(rf.price[j].amount) * this.children);
                                    break;

                                case passengerType.INFANT:
                                    price += Convert.ToInt32(Math.Ceiling(rf.price[j].amount) * this.inf);
                                    break;

                                default:
                                    price += Convert.ToInt32(Math.Ceiling(rf.price[j].amount));
                                    break;
                            }
                        }

                        fl.Price = price;

                        ///PAYU!!!! 
                        if (this.komisionAirlines.Contains(fl.AirlineCode))
                            fl.Price = Convert.ToInt32(Math.Ceiling(fl.Price * 1.03));

                        fl.Price = Convert.ToInt32(Math.Ceiling(fl.Price * TOTAL_COEF));

                        fl.Parts = ConvertToFlightParts(rf.segments);

                        if (fl.Parts != null) result.Add(fl);
                    }
                    catch (Exception ex)
                    {
                        Logger.Logger.WriteToLog("port exc try convert flights: " + ex.Message + ' ' + ex.StackTrace + " ");
                    }
                }

                return result.ToArray(typeof(Containers.Flight)) as Containers.Flight[];

            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("!!PORTBILET ERROR " + ex.Message + " " + ex.StackTrace);
                return new Containers.Flight[0];
            }
        }

        private Containers.FlightPart[] ConvertToFlightParts(FlightSegment[] segments)
        {
            ArrayList partsList = new ArrayList();
            ArrayList currentSegments = new ArrayList();

            foreach (FlightSegment portSegment in segments)
            {
                if (portSegment.locationBegin == null) throw new Exception("locationBegin");
                if (portSegment.locationEnd == null) throw new Exception("locationEnd");
                if (portSegment.board == null) throw new Exception("board");
                if (portSegment.airline == null) throw new Exception("airline");

                Leg leg = new Leg(ConvertToServiceClass(portSegment.serviceClass),
                                   portSegment.bookingClass,
                                   portSegment.airline.code,
                                   portSegment.flightNumber,
                                   portSegment.locationBegin.code,
                                   this.airportsLib.ContainsKey(portSegment.locationBegin.code) ? this.airportsLib[portSegment.locationBegin.code] : portSegment.cityBegin.name,
                                   portSegment.locationEnd.code,
                                   this.airportsLib.ContainsKey(portSegment.locationEnd.code) ? this.airportsLib[portSegment.locationEnd.code] : portSegment.cityEnd.name,
                                   portSegment.locationBegin.name,
                                   portSegment.locationEnd.name,
                                   portSegment.dateBegin,
                                   portSegment.dateEnd,
                                   portSegment.board.code,
                                   portSegment.board.name,
                                   BytesToString(portSegment.remarksSearchContext),
                                   portSegment.travelDuration);

                if ((portSegment.starting) && (currentSegments.Count > 0))
                {
                    FlightPart fp = new FlightPart();
                    fp.Legs = currentSegments.ToArray(typeof(Leg)) as Leg[];
                    fp.FlightLong = CalculatePartDuration(fp.Legs);

                    partsList.Add(fp);
                    currentSegments = new ArrayList();
                }
                currentSegments.Add(leg);
            }

            if (currentSegments.Count > 0)
                partsList.Add(new FlightPart()
                {
                    Legs = currentSegments.ToArray(typeof(Leg)) as Leg[],
                    FlightLong = CalculatePartDuration(currentSegments.ToArray(typeof(Leg)) as Leg[])
                });

            return partsList.ToArray(typeof(Containers.FlightPart)) as Containers.FlightPart[];
        }

        private int CalculatePartDuration(Leg[] legs)
        {
            int res = 0;
            for (int i = 0; i < legs.Length; i++)
            {
                if (i > 0)//calculate waitTime
                    res += Convert.ToInt32((legs[i].DateBegin - legs[i - 1].DateEnd).TotalMinutes);

                res += legs[i].Duration;
            }
            return res;
        }

        private string ConvertToServiceClass(classOfService cl)
        {
            switch (cl)
            {
                case classOfService.BUSINESS: return "B";
                case classOfService.ECONOMY: return "E";
                case classOfService.FIRST: return "F";
                case classOfService.PREMIUM: return "P";
            }

            return "";
        }
        #endregion
    }
}