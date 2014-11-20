using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using SearchFlightsService.Containers;
using SearchFlightsService.Core;
using System.Net;
using System.Xml;
using System.Web.Configuration;
using System.Data.SqlClient;
using System.Data;
using Megatec.Common;
using Megatec.Common.BusinessRules;
using Megatec.Common.BusinessRules.Base;
using Megatec.Common.DataAccess;
using Megatec.MasterTour;
using Megatec.MasterTour.BusinessRules;
using Megatec.MasterTour.Model;

namespace SearchFlightsService.Ext
{
 public class VizitService : IExternalService
{
    // Fields
    private int adl;
    private int chd;
    private string ConnectionString;
    private Flight[] flights;
    private int FlightTypeTour;
    private int inf;
    private Route route;
    private string serviceClass;

    // Methods
    public VizitService()
    {
        this.ConnectionString = WebConfigurationManager.AppSettings["connectionString"];
        this.FlightTypeTour = Convert.ToInt32(WebConfigurationManager.AppSettings["VizitFlightTourType"]);
        this.serviceClass = "";
        this.adl = 0;
        this.chd = 0;
        this.inf = 0;
        this.route = null;
        Manager.ConnectionString = this.ConnectionString;
    }

    public VizitService(int adl, int chd, int inf, string servClass, Route route)
    {
        this.ConnectionString = WebConfigurationManager.AppSettings["connectionString"];
        this.FlightTypeTour = Convert.ToInt32(WebConfigurationManager.AppSettings["VizitFlightTourType"]);
        this.serviceClass = "";
        this.adl = 0;
        this.chd = 0;
        this.inf = 0;
        this.route = null;
        this.adl = adl;
        this.chd = chd;
        this.inf = inf;
        this.serviceClass = servClass;
        this.route = route;
        Manager.ConnectionString = this.ConnectionString;
    }

    public string BookFlight(string flightToken, Customer customer, Passenger[] passengers)
    {
        string[] str_params = flightToken.Split(new char[] { '_' });
        if (str_params.Length != 3)
        {
            return "vt_params_error";
        }
        int tour_key = Convert.ToInt32(str_params[1]);
        DateTime date = Convert.ToDateTime(str_params[2]);
        TurLists tls = new TurLists(new DataCache()) {
            RowFilter = "tl_key = " + tour_key
        };
        tls.Fill();
        if (tls.Count == 0)
        {
            return "vt_tour_not_exists";
        }
        

        TurList tl = tls[0];
        DupUsers users = new DupUsers(new DataCache()) {
            RowFilter = "us_id = 'ct01'"
        };
        users.Fill();
        if (users.Count == 0)
        {
            return "vt_user_not_found";
        }
        DupUser dUser = users[0];
        int CAT_CREATOR_KEY = 0x186a0;
        Dogovors dogs = new Dogovors(new DataCache());
        Dogovor dog = dogs.NewRow();
       
        try
        {
            dog.CountryKey = tl.CountryKey;
            dog.CityKey = 0;
            dog.TurDate = date;
            dog.NDays = tl.NDays;
            dog.MainMenEMail = "inflo@clickandtravel.ru";
            dog.MainMenPhone = "8-800-5555-4-33";
            dog.MainMen = "Алена Карпенко";
            dog.MainMenComment = "";
            dog.TourKey = tl.Key;
            dog.PartnerKey = dUser.PartnerKey;
            dog.DupUserKey = dUser.Key;
            dog.CreatorKey = CAT_CREATOR_KEY;
            dog.OwnerKey = dog.CreatorKey;
            dog.RateCode = "E";
            dogs.Add(dog);
            dogs.DataCache.Update();
            this.loadServices(dog.DogovorLists, tl.TurServices);
            this.loadTurists(dog, passengers);
            dog.CalculateCost();
            dog.NMen = (short) dog.Turists.Count;
            dog.DataCache.Update();
            SqlConnection conn = new SqlConnection(Manager.ConnectionString);
            conn.Open();
            SqlCommand com = conn.CreateCommand();
            com.CommandText = string.Concat(new object[] { "update tbl_dogovor set dg_creator=", CAT_CREATOR_KEY, ", dg_owner=", CAT_CREATOR_KEY, ", dg_filialkey = (select top 1 us_prkey from userlist where us_key = ", CAT_CREATOR_KEY, ") where dg_code='", dog.Code, "'" });
            com.ExecuteNonQuery();
            conn.Close();
        }
        catch (Exception ex)
        {
            return ("vt_Error " + ex.Message);
        }
        return ("vt_" + dog.Code);
    }

    public int GetTimelimit(string order_number)
    {
        return 24 * 60 * 60;
    }

    private double CalcPrice(TurServices tss, DateTime date)
    {
        double totalPrice = 0.0;
        Costs csts = new Costs(new DataCache());
        foreach (TurService ts in tss)
        {
            csts.RowFilter = string.Concat(new object[] { "cs_svkey = ", 1, " and cs_date<='", date.AddDays((double) (ts.Day - 1)).ToString("yyyy-MM-dd"), "' and cs_dateend >='", date.AddDays((double) (ts.Day - 1)).ToString("yyyy-MM-dd"), "' " });
           
            csts.RowFilter += string.Concat(new object[] { " and cs_pkkey = ", ts.PacketKey, " and cs_prkey = ", ts.PartnerKey, " and cs_code = ", ts.Code, " and cs_subcode1 = ", ts.SubCode1, " and cs_subcode2 = ", ts.SubCode2 });
            
            csts.RowFilter += string.Concat(new object[] { " and ((cs_long is NULL) or (cs_long >= ", ts.TurListTour.NDays, ")) and ((cs_longmin is NULL) or (cs_longmin<= ", ts.TurListTour.NDays, ")) " });
            
            csts.RowFilter += string.Concat(new object[] { " and ((cs_week is NULL) or (cs_week = '') or (cs_week like '%", this.GetDayOftWeek(date.AddDays((double) (ts.Day - 1)).DayOfWeek), "%')) " });
            csts.Sort = "CS_WEEK desc, CS_LONG desc, CS_LONGMIN desc";
            csts.Fill();
            if (csts.Count > 0)
            {
                totalPrice += csts[0].Brutto;
            }
        }
        return totalPrice;
    }

    private int CheckPlaces(TurServices tss, DateTime date)
    {
        SqlConnection conn = new SqlConnection(Manager.ConnectionString);
        conn.Open();
        int min_cnt = 10;
        foreach (TurService ts in tss)
        {
            SqlCommand com = new SqlCommand("zp_CheckQuota", conn) {
                CommandType = CommandType.StoredProcedure
            };
            com.Parameters.Add(new SqlParameter("@p_nServiceTypeID", ts.ServiceKey));
            com.Parameters.Add(new SqlParameter("@p_nCode", ts.Code));
            com.Parameters.Add(new SqlParameter("@p_nSubCode1", ts.SubCode1));
            com.Parameters.Add(new SqlParameter("@p_nSubCode2", SqlDbType.BigInt));
            com.Parameters.Add(new SqlParameter("@p_nAgentID", SqlDbType.BigInt));
            com.Parameters.Add(new SqlParameter("@p_dtDateBegin", date.AddDays((double) (ts.Day - 1)).ToString("yyyy-MM-dd")));
            com.Parameters.Add(new SqlParameter("@p_nDuration", ts.TurListTour.NDays));
            SqlParameter returnParameter = com.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;
            com.ExecuteNonQuery();
            int value = (int) returnParameter.Value;
            if (value <= 0)
            {
                return 0;
            }
            if (value < min_cnt)
            {
                min_cnt = value;
            }
        }
        conn.Close();
        return min_cnt;
    }

    private bool CheckRoute(TurServices tss)
    {
        string tmpStr;
        int nDays = 1;
        if (this.route.Segments.Length > 1)
        {
            TimeSpan ts = (TimeSpan) (this.route.Segments[1].Date - this.route.Segments[0].Date);
            nDays = ts.Days + 1;
        }
        if (tss[0].TurListTour.NDays != nDays)
        {
            return false;
        }
        string needRoute = "start(" + this.route.Segments[0].LocationBegin + ")+";
        if (this.route.Segments.Length == 1)
        {
            needRoute = needRoute + "finish(" + this.route.Segments[0].LocationEnd + ")";
        }
        else
        {
            tmpStr = needRoute;
            needRoute = tmpStr + "middle(" + this.route.Segments[0].LocationEnd + ")+finish(" + this.route.Segments[1].LocationEnd + ")";
        }
        string[] checkArray = needRoute.Split(new char[] { '+' });
        if (tss.Count == 0)
        {
            return false;
        }
        Charter ch = tss[0].SubService as Charter;
        string findedRoute = "start(" + ch.FromAirport.Code + ")start(" + ch.FromCity.Code + ")";
        if (tss.Count == 1)
        {
            tmpStr = findedRoute;
            findedRoute =tmpStr + "finish(" + ch.ToAirport.Code + ")finish(" + ch.ToCity.Code + ")";
        }
        else
        {
            tmpStr= findedRoute;
            findedRoute = tmpStr + "middle(" + ch.ToAirport.Code + ")middle(" + ch.ToCity.Code + ")";
            ch = tss[1].SubService as Charter;
            tmpStr = findedRoute;
            findedRoute = tmpStr+ "finish(" + ch.ToAirport.Code + ")finish(" + ch.ToCity.Code + ")";
        }
        foreach (string checkItem in checkArray)
        {
            if (!findedRoute.Contains(checkItem))
            {
                return false;
            }
        }
        SearchFlightsService.Logger.Logger.WriteToLog("success route");
        return true;
    }

    private Flight CreateFlight(TurServices tss, DateTime date, double price)
    {
        FlightPart[] parts = this.GetParts(tss, date);
        if (parts == null)
        {
            return null;
        }
        return new Flight { Airline = (tss[0].SubService as Charter).Airline.Name, AirlineCode = parts[0].Legs[0].Airline, Id = string.Concat(new object[] { "vt_", tss[0].TurListTourKey, "_", date.ToString("yyyy-MM-dd") }), Key = tss[0].TurListTourKey.ToString(), Parts = parts, Price = Convert.ToInt32(Math.Ceiling(price)), TimeLimit = DateTime.Now.AddHours(24.1).ToString("yyyy-MM-dd HH:00:00") };
    }

    public Flight Get_Flight_Info(string flightToken)
    {
        string[] arr = flightToken.Split(new char[] { '_' });
        int tour = Convert.ToInt32(arr[1]);
        DateTime date = Convert.ToDateTime(arr[2]);
        TurLists tls = new TurLists(new DataCache()) {
            RowFilter = string.Concat(new object[] { "tl_tip = ", this.FlightTypeTour, " and tl_key = ", tour })
        };
        tls.Fill();
        if (tls.Count == 0)
        {
            SearchFlightsService.Logger.Logger.WriteToLog("");
            SearchFlightsService.Logger.Logger.WriteToLog("VIZIT: Тур не найден для token " + flightToken);
            return null;
        }
        TurList tl = tls[0];
        tl.TurDates.Fill();
        tl.TurDates.RowFilter = string.Concat(new object[] { "td_trkey = ", tl.Key, " and td_date = '", date.ToString("yyyy-MM-dd"), "'" });
        if (tl.TurDates.Count == 0)
        {
            SearchFlightsService.Logger.Logger.WriteToLog("");
            SearchFlightsService.Logger.Logger.WriteToLog("VIZIT: Дата тура не найдена для token " + flightToken);
            return null;
        }
        tl.TurServices.Fill();
        tl.TurServices.RowFilter = string.Concat(new object[] { "ts_trkey =", tl.Key, " and TS_SVKEY = ", 1 });
        tl.TurServices.Sort = "TS_day asc ";
        TurServices tss = tl.TurServices;
        double price = this.CalcPrice(tss, date);
        RealCourses rcs = new RealCourses(new DataCache()) {
            RowFilter = "rc_rcod1 = 'рб' and rc_rcod2 = '" + tl.RateCode + "' and rc_datebeg > '" + DateTime.Today.AddDays(-5.0).ToString("yyyy-MM-dd") + "' and rc_datebeg < '" + DateTime.Today.AddDays(1.0).ToString("yyyy-MM-dd") + "'",
            Sort = "rc_datebeg desc"
        };
        rcs.Fill();
        double course = 41.0;
        if (rcs.Count > 0)
        {
            course = rcs[0].Course;
        }
        SearchFlightsService.Logger.Logger.WriteToLog("course " + course);
        price *= course;
        if (price == 0.0)
        {
            SearchFlightsService.Logger.Logger.WriteToLog("");
            SearchFlightsService.Logger.Logger.WriteToLog("VIZIT: Не удалось расчитать стоимость для token " + flightToken);
            return null;
        } 
        price += 10.0;
        int places = this.CheckPlaces(tss, date);
        FlightPart[] parts = this.GetParts(tss, date);
        if (parts == null)
        {
            SearchFlightsService.Logger.Logger.WriteToLog("");
            SearchFlightsService.Logger.Logger.WriteToLog("VIZIT: Не удалось сформировать маршрут " + flightToken);
            return null;
        }
        return new Flight { Airline = (tss[0].SubService as Charter).Airline.Name, AirlineCode = parts[0].Legs[0].Airline, Id = flightToken, Key = places.ToString(), Parts = parts, Price = Convert.ToInt32(Math.Ceiling(price)), TimeLimit = DateTime.Now.AddHours(4).ToString("yyyy-MM-dd HH:00:00") };
    }

    public Flight[] GetFlights(string search_id)
    {
        return this.flights;
    }

    private int GetDayOftWeek(DayOfWeek dw)
    {
        if (dw == DayOfWeek.Monday)
        {
            return 1;
        }
        if (dw == DayOfWeek.Tuesday)
        {
            return 2;
        }
        if (dw == DayOfWeek.Wednesday)
        {
            return 3;
        }
        if (dw == DayOfWeek.Thursday)
        {
            return 4;
        }
        if (dw == DayOfWeek.Friday)
        {
            return 5;
        }
        if (dw == DayOfWeek.Saturday)
        {
            return 6;
        }
        return 7;
    }

    public FlightDetails[] GetFlightDetails(string flightToken)
    {
        return null;
    }

    private FlightPart[] GetParts(TurServices tss, DateTime date)
    {
        ArrayList parts = new ArrayList();
        foreach (TurService ts in tss)
        {
            Charter ch = ts.SubService as Charter;
            AirSeason airSsn = ch.GetAirSeason(date.AddDays((double) (ts.Day - 1)));
            int duration = 0;
            try
            {
                duration = Convert.ToInt32(airSsn.Remark);
            }
            catch (Exception)
            {
            }
            Leg leg1 = new Leg {
                Airline = ch.AirlineCode,
                AirportBeginName = ch.FromAirport.Name,
                AirportEndName = ch.ToAirport.Name,
                Board = ch.AircraftCode,
                BoardName = ch.Aircraft.Name,
                BookingClass = "V",
                DateBegin = date.AddDays((double) (ts.Day - 1)).AddHours((double) airSsn.TimeFrom.Hour).AddMinutes((double) airSsn.TimeFrom.Minute),
                DateEnd = date.AddDays((double) (ts.Day - 1)).AddHours((double) airSsn.TimeTo.Hour).AddMinutes((double) airSsn.TimeTo.Minute),
                Duration = duration,
                FlightNumber = ch.Flight,
                LocationBegin = ch.FromAirport.Code,
                LocationBeginName = ch.FromCity.Name.Replace(".", "").Replace(" Вары","-Вары"),
                LocationEnd = ch.ToAirport.Code,
                LocationEndName = ch.ToCity.Name.Replace(".", "").Replace(" Вары", "-Вары"),
                ServiceClass = "E"
            };
            Leg leg = leg1;
            FlightPart fp = new FlightPart {
                FlightLong = duration,
                Legs = new Leg[] { leg }
            };
            FlightPart part =fp;
            parts.Add(part);
            if (airSsn == null)
            {
                return null;
            }
        }
        return (parts.ToArray(typeof(FlightPart)) as FlightPart[]);
    }

    public void InitSearch()
    {
        try
        {
            Exception ex;
            TurLists tls = new TurLists(new DataCache());
            DateTime turdate = DateTime.Now;
            int total_tickets = 0;
            try
            {
                if ((this.route == null) || (this.route.Segments == null))
                {
                    this.flights = new Flight[0];
                }
                if (this.route.Segments.Length > 2)
                {
                    this.flights = new Flight[0];
                }
            }
            catch (Exception exception1)
            {
                ex = exception1;
                throw new Exception("block 0 " + ex.Message);
            }
            try
            {
                total_tickets = this.adl + this.chd;
            }
            catch (Exception exception2)
            {
                ex = exception2;
                throw new Exception("block 1 " + ex.Message);
            }
            try
            {
                turdate = this.route.Segments[0].Date;
                int flLong = 1;
                if (this.route.Segments.Length == 2)
                {
                    TimeSpan ts = (TimeSpan) (this.route.Segments[1].Date - this.route.Segments[0].Date);
                    flLong +=ts.Days;
                }
            }
            catch (Exception exception3)
            {
                ex = exception3;
                throw new Exception("block 2 " + ex.Message);
            }
            try
            {
                int tourTip = 0x15;
                tls.RowFilter = string.Concat(new object[] { "tl_tip = ", tourTip, " and 0<(select count(*) from [turDATE] where tl_key = td_trkey and td_date = '", turdate.ToString("yyyy-MM-dd"), "') " });
                tls.Fill();
            }
            catch (Exception exception4)
            {
                ex = exception4;
                throw new Exception("block 3 " + ex.Message);
            }
            try
            {
                double course = 41.0;
                if (tls.Count > 0)
                {
                    RealCourses rcs = new RealCourses(new DataCache()) {
                        RowFilter = "rc_rcod1 = 'рб' and rc_rcod2 = '" + tls[0].RateCode + "' and rc_datebeg > '" + DateTime.Today.AddDays(-5.0).ToString("yyyy-MM-dd") + "' and rc_datebeg <= '" + DateTime.Today.ToString("yyyy-MM-dd") + "'",
                        Sort = "rc_datebeg desc"
                    };
                    rcs.Fill();
                    if (rcs.Count > 0)
                    {
                        course = rcs[0].Course;
                    }
                }
                ArrayList flights = new ArrayList();
                foreach (TurList tl in tls)
                {
                    tl.TurServices.Fill();
                    tl.TurServices.RowFilter = string.Concat(new object[] { "ts_trkey =", tl.Key, " and TS_SVKEY = ", 1 });
                    tl.TurServices.Sort = "TS_day asc ";
                    if (this.CheckRoute(tl.TurServices))
                    {
                        int havePlaces = this.CheckPlaces(tl.TurServices, turdate);
                        if (total_tickets <= havePlaces)
                        {
                            double price = this.CalcPrice(tl.TurServices, turdate);
                            if (price >= 1.0)
                            {
                                Flight flight = this.CreateFlight(tl.TurServices, turdate, ((price * course) * total_tickets) + 10.0);
                                if (flight != null)
                                {
                                    flights.Add(flight);
                                }
                            }
                        }
                    }
                }
                this.flights = flights.ToArray(typeof(Flight)) as Flight[];
            }
            catch (Exception exception5)
            {
                ex = exception5;
                throw new Exception("block 4 " + ex.Message);
            }
        }
        catch (Exception e)
        {
            SearchFlightsService.Logger.Logger.WriteToLog("vizit block total " + e.Message);
            this.flights = new Flight[0];
        }
    }

    public string InitSearch(Route route, int adult, int children, int inf, string serviceClass)
    {
        throw new SearchFlightException("InitSearch");
    }

    private void loadServices(DogovorLists dls, TurServices tss)
    {
        foreach (TurService ts in tss)
        {
            DogovorList dl = dls.NewRow();
            dl.ServiceKey = ts.ServiceKey;
            dl.NMen = 0;
            dl.Code = ts.Code;
            dl.SubCode1 = ts.SubCode1;
            dl.SubCode2 = ts.SubCode2;
            dl.TurDate = dl.Dogovor.TurDate;
            dl.TourKey = dl.Dogovor.TourKey;
            dl.PacketKey = ts.PacketKey;
            dl.CreatorKey = dl.Dogovor.CreatorKey;
            dl.OwnerKey = dl.Dogovor.OwnerKey;
            dl.DateBegin = dl.Dogovor.TurDate.AddDays((double) (ts.Day - 1));
            dl.Name = ts.Name;
            dl.AgentKey = dl.Dogovor.PartnerKey;
            if (dl.ServiceKey == 3)
            {
                dl.Comment = "1";
            }
            dl.CountryKey = ts.CountryKey;
            dl.CityKey = ts.CityKey;
            if ((dl.Dogovor.CityKey < 1) && (dl.ServiceKey == 1))
            {
                City ct = (ts.SubService as Charter).ToCity;
                if (ct.CountryKey == dl.Dogovor.CountryKey)
                {
                    dl.Dogovor.CityKey = ct.Key;
                    dl.Dogovor.DataContainer.Update();
                }
            }
            dl.PartnerKey = ts.PartnerKey;
            if (ts.Days > 0)
            {
                dl.NDays = ts.Days;
            }
            dl.Day = ts.Day;
            dl.BuildName();
            dl.DataContainer.Update();
            dls.Add(dl);
        }
        dls.DataContainer.Update();
    }

    private void loadTurists(Dogovor dog, Passenger[] pss)
    {
        int CAT_CREATOR_KEY = 0x186a0;
        string FEMALE_CODE = "F";
        int CHILD_AGE = 14;
        TuristServices tServices = new TuristServices(new DataCache());
        int man = 0;
        foreach (Passenger iTst in pss)
        {
            man++;
            Turist tst = dog.Turists.NewRow();
            tst.NameRus = iTst.Fname.ToUpper();
            tst.NameLat = iTst.Fname.ToUpper();
            tst.FNameRus = iTst.Name.ToUpper();
            tst.FNameLat = iTst.Name.ToUpper();
            tst.SNameRus = "";
            tst.SNameLat = "";
            tst.Birthday = iTst.Birth;
            tst.CreatorKey = CAT_CREATOR_KEY;

            tst.PasportNum = iTst.Pasport.Substring(2);
            tst.PasportType = iTst.Pasport.Substring(0, 2);
            tst.PasportDateEnd = iTst.PassportExpireDate;

            tst.DogovorCode = dog.Code;
            tst.DogovorKey = dog.Key;
            tst.Citizen = iTst.Citizen.Substring(2);
            if (iTst.Gender == FEMALE_CODE)
            {
                tst.RealSex = 1;
                if (tst.Age > CHILD_AGE)
                {
                    tst.Sex = 1;
                }
                else
                {
                    tst.Sex = 2;
                }
            }
            else
            {
                tst.RealSex = 0;
                if (tst.Age > CHILD_AGE)
                {
                    tst.Sex = 0;
                }
                else
                {
                    tst.Sex = 2;
                }
            }
            dog.Turists.Add(tst);
            dog.Turists.DataCache.Update();
            foreach (DogovorList dl in dog.DogovorLists)
            {
                dl.NMen = (short) (dl.NMen + 1);
                TuristService ts = tServices.NewRow();
                ts.Turist = tst;
                ts.DogovorList = dl;
                tServices.Add(ts);
                tServices.DataCache.Update();
            }
            dog.DogovorLists.DataCache.Update();
        }
    }
}

}