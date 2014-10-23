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

namespace SearchFlightsService.Ext
{
    public class BiletixService : IExternalService
    {
        private string LOCALE = "ru";
        private string LOGIN_NAME = "info@clickandtravel.ru";
        private string PASSWORD = "maqt72c";
        private string SALES_POINT_CODE = "WEB_SITE";
        private string komisionAirlines = "A3,BT,AB,AZ,AA,CI,DL,EK,9W,AF,OS,BA,OK,AY,IB,KL,LO,LH,QR,SK,S7,LX,TP";

        #region Fields 
        private Hashtable hashResults = new Hashtable();

        private FlightSearchResult result = null;

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

        private GDSWebServiceService sc = new GDSWebServiceService();

        private Dictionary<string, string> airportsLib = null;

        private Dictionary<string, string> airlinesLib = null;

        #endregion

        #region Constructors
        public BiletixService()
        { }

        public BiletixService(Route route, int adult, int children, int inf, string serviceClass)
        {
            this.route = route;
            this.adult = adult;
            this.children = children;
            this.inf = inf;
            this.serviceClass = serviceClass;
            this.searchId = route.ToString() + "-" + adult + "-" + children + "-" + inf + "-" + serviceClass;
        }
        #endregion

        #region Public methods
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

        public Containers.Flight[] Get_Flights(string search_id)
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
            pars.customer = new PortbiletWebService.Customer() { name = customer.Name, phone = "4957846256", email = "info@clickandtravel.ru" };

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
                    Logger.Logger.WriteToLog("pb_can not book:  " + ex.Message + "\n" + ex.StackTrace + " flight_id = " + flightId );
                }
            }

            //для проверки найденного токена, считаем контрольную сумму
            string check_short_id = "pb_" + CalculateMD5Hash(id);

       /*     //если суммы не верна, пишем в лог
            if (check_short_id != flightId)
            {
                Logger.Logger.WriteToLog("AHTUNG!!! wrong full id " + id + " for shortId " + flightId);
                return "can not book";
            }
            */


            pars.flightToken = GetBytes(id);

            pars.passengers = new PortbiletWebService.Passenger[passengers.Length];

            for(int i = 0; i < passengers.Length; i++)
            {
                passengerType type = passengerType.ADULT;

                if ((tour_end_date - passengers[i].Birth).Days / 365.25 < 2)
                    type = passengerType.INFANT;
                else if ((tour_end_date - passengers[i].Birth).Days / 365.25 < 12)
                    type = passengerType.CHILD;

                pars.passengers[i] = new PortbiletWebService.Passenger(){bonusCardAirline = new DictionaryItem(){code = passengers[i].FrequentFlyerAirline},
                                                                         bonusCardNumber = passengers[i].FrequentFlyerNumber,
                                                                         insurePassenger = false,
                                                                         countryCode = passengers[i].Citizen.Substring(2),
                                                                         type = type,
                                                                         passport = new Passport(){
                                                                                                    birthday = passengers[i].Birth,
                                                                                                    birthdaySpecified = true,
                                                                                                    citizenship = new DictionaryItem() { code = passengers[i].Citizen.Substring(2) },
                                                                                                    
                                                                                                    expired = passengers[i].Passport_expire_date,
                                                                                                    expiredSpecified = true,

                                                                                                    firstName = passengers[i].Name,
                                                                                                    gender = passengers[i].Gender == "M" ? gender.MALE: gender.FEMALE,
                                                                                                    genderSpecified = true,
                                                                                                    
                                                                                                    issuedSpecified = false,
                                                                                                    
                                                                                                    lastName = passengers[i].Fname,
                                                                                                    number = passengers[i].Pasport,
                                                                                                    type = passportType.PASSPORT,
                                                                                                    typeSpecified = true
                                                                         }
                };
            }

           //sc.
            this.sc.Timeout = 100000;

            BookingCreateResult bcr = sc.createBooking(new InvocationContext() { locale = this.LOCALE,
                                                                                 loginName = this.LOGIN_NAME,
                                                                                 password = this.PASSWORD,
                                                                                 salesPointCode = this.SALES_POINT_CODE},
                                                       pars);


            if (bcr.bookingFile != null)
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

            FareRemarksSearchResult remarksRes =  this.sc.searchRemarks(    new InvocationContext()
                                                                            {
                                                                                locale = this.LOCALE,
                                                                                loginName = this.LOGIN_NAME,
                                                                                password = this.PASSWORD,
                                                                                salesPointCode = this.SALES_POINT_CODE
                                                                            },
                                                                            new FareRemarksSearchParameters() { remarksSearchContext = GetBytes(flightToken) }
                                                    );

            if(remarksRes.remarks == null) return null;

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

        private string CalculateMD5Hash(string input)
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
            try
            {
                DB.DataStore ds = new DB.DataStore();

                if (this.airportsLib == null)
                    this.airportsLib = ds.AirportsLib();

                if (this.airlinesLib == null)
                    this.airlinesLib = ds.AirlinesLib();

                Containers.Flight[] result = new Containers.Flight[searchResults.flights.Length];

                for (int i = 0; i < result.Length; i++)
                {
                    PortbiletWebService.Flight rf = searchResults.flights[i];

                    Containers.Flight fl = new Containers.Flight();
                    fl.TimeLimit = (rf.timeLimit > DateTime.Now) ? rf.timeLimit.ToString("yyyy-MM-dd HH:mm") : DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                    fl.Airline = this.airlinesLib.ContainsKey(rf.carrier.code) ? this.airlinesLib[rf.carrier.code].Trim() : rf.carrier.name;
                    fl.AirlineCode = rf.carrier.code;

                    fl.FullId = BytesToString(rf.token);
                    fl.Id = "pb_" + CalculateMD5Hash(fl.FullId);

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

                   // fl.Price = Convert.ToInt32(Math.Ceiling(fl.Price ));// / 0.978));

                    fl.Parts = ConvertToFlightParts(rf.segments);

                    result[i] = fl;
                }
                return result;

            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("!!!!!!!PORTBILET ERROR " + ex.Message + " " + ex.StackTrace);
                return new Containers.Flight[0];
            }
        }

        private Containers.FlightPart[] ConvertToFlightParts(FlightSegment[] segments)
        {
            ArrayList partsList = new ArrayList();
            ArrayList currentSegments = new ArrayList();

            foreach (FlightSegment portSegment in segments)
            { 
                Leg leg = new Leg(ConvertToServiceClass(portSegment.serviceClass), 
                                   portSegment.bookingClass,
                                   portSegment.airline.code,
                                   portSegment.flightNumber,
                                   portSegment.locationBegin.code,
                                   this.airportsLib.ContainsKey(portSegment.locationBegin.code) ? this.airportsLib[portSegment.locationBegin.code] : portSegment.locationBegin.name,
                                   portSegment.locationEnd.code,
                                   this.airportsLib.ContainsKey(portSegment.locationEnd.code) ? this.airportsLib[portSegment.locationEnd.code] : portSegment.locationEnd.name,
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
                partsList.Add(new FlightPart() { Legs = currentSegments.ToArray(typeof(Leg)) as Leg[],
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
            switch(cl)
            {
                case classOfService.BUSINESS: return "B";
                case classOfService.ECONOMY:  return "E";
                case classOfService.FIRST:    return "F";
                case classOfService.PREMIUM:  return "P";
            }
            
            return "";
        }
        #endregion
    }
}