using System;
using System.Web.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.Sql;
using System.Data.SqlClient;
using SearchFlightsService.Containers;
using Jayrock.Json;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Net;
using MySql.Data.MySqlClient;
using System.Threading;
using SearchFlightsService.Core;
using System.Web.Configuration;

namespace SearchFlightsService.DB
{
    public class DataStore: IDisposable
    {
        #region Fields
        public string route_id = "";
        public string is_rambler = "false";
        private Route route;
        private int adult;
        private int children;
        private int inf;
        private string serviceClass;
        private string search_Id;
        private Fare[] fares;
        private Flight[] flights;
        private int isFinished;

        private  string ConnectionString = WebConfigurationManager.AppSettings["ClickConnectionString"];// ConfigurationManager.AppSettings["connectionString"];// "Data Source=192.168.0.105;Initial Catalog=avalon;User Id=sa;Password=asd321r";
        private static string ClickConnectionString = WebConfigurationManager.AppSettings["ClickConnectionString"];

        private int MySqlDbCount = 0;

        //private static string MySqlConnectionString = WebConfigurationManager.AppSettings["MySqlConnectionString"];
        //private static string DB_Name = WebConfigurationManager.AppSettings["db_user"];// "[avalon].[dbo].";



        private  string[] MySqlConnectionStrings = new string[0];
        private  MySqlConnection[] MySqlConnections = new MySqlConnection[0];//= new MySqlConnection(MySqlConnectionString);
        #endregion

        #region constructor
        public DataStore()
        {
            while (true) //читаем все конекшн стринг из конфигурации
            {
                if (WebConfigurationManager.AppSettings.AllKeys.Contains("MySqlConnectionString" + (MySqlDbCount + 1)))
                {
                    string[] _tempArr = new string[MySqlConnectionStrings.Length];

                    if (MySqlConnectionStrings.Length > 0)
                        MySqlConnectionStrings.CopyTo(_tempArr, 0);

                    MySqlConnectionStrings = new string[_tempArr.Length + 1];

                    if (_tempArr.Length > 0)
                        _tempArr.CopyTo(MySqlConnectionStrings, 0);

                    MySqlConnectionStrings[_tempArr.Length] = WebConfigurationManager.AppSettings["MySqlConnectionString" + (++MySqlDbCount)];
                }
                else
                    break;
            }

            MySqlDbCount = MySqlConnectionStrings.Length;
            MySqlConnections = new MySqlConnection[MySqlDbCount];

            //заполняем массив из коннекшн
            for (int i = 0; i < MySqlDbCount; i++)
                MySqlConnections[i] = new MySqlConnection(MySqlConnectionStrings[i]);
        }

        public DataStore(Route route, int adult, int children, int inf, string serviceClass, 
                         string search_Id, Fare[] fares, Flight[] flights, int isFinished)
        {
            while (true) //читаем все конекшн стринг из конфигурации
            {
                if (WebConfigurationManager.AppSettings.AllKeys.Contains("MySqlConnectionString" + (MySqlDbCount + 1)))
                { 
                    string [] _tempArr = new string[MySqlConnectionStrings.Length];

                    if (MySqlConnectionStrings.Length > 0)
                        MySqlConnectionStrings.CopyTo(_tempArr, 0);

                    MySqlConnectionStrings = new string[_tempArr.Length + 1];

                    if (_tempArr.Length > 0)
                        _tempArr.CopyTo(MySqlConnectionStrings, 0);
    
                    MySqlConnectionStrings[_tempArr.Length] = WebConfigurationManager.AppSettings["MySqlConnectionString" + (++MySqlDbCount)];
                }
                else
                    break;
            }

            MySqlDbCount = MySqlConnectionStrings.Length;
            MySqlConnections = new MySqlConnection[MySqlDbCount];

            //заполняем массив из коннекшн
            for (int i = 0; i < MySqlDbCount; i++ )
                MySqlConnections[i] = new MySqlConnection(MySqlConnectionStrings[i]);


            this.route = route;
            this.adult = adult;
            this.children = children;
            this.inf = inf;
            this.serviceClass = serviceClass;
            this.search_Id = search_Id;
            this.fares = fares;
            this.flights = flights;
            this.isFinished = isFinished;

            //clear old results
          ///  _openConnection();
        }
        #endregion

        #region private methods
        private void _openConnection(int i)
        {
        //    for (int i = 0; i < MySqlConnections.Length; i++)
            {
                try
                {
                    MySqlConnection con = MySqlConnections[i];
                    if ((con.State == ConnectionState.Closed) || (con.State == ConnectionState.Broken))
                        con.Open();
                }
                catch (Exception ex)
                {
                    Logger.Logger.WriteToLog("Open connection exception for DB"+  (i +1) + "\n" + ex.Message);
                }
            }
        }
        #endregion

        #region public methods

        public void Dispose()
        {
            try
            {
                foreach (MySqlConnection con in this.MySqlConnections)
                    try{

                        if((con != null )&&( con.State == ConnectionState.Open))
                        con.Close();
                    }
                    catch(Exception e)
                    {
                        Logger.Logger.WriteToLog("DB dispose " + e.Message + e.StackTrace);
                    }
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("DB dispose " + ex.Message + ex.StackTrace);
            }
            //this.MySqlConnection.Close();
        }
        //thread Start Method
        public void SaveResults()
        {
            try
            {

                _openConnection(0);


                if (this.fares == null)
                    this.fares = FlightsToFareProcessor.FlightsToFares(this.flights, this.route.Segments.Length == 1);

                //fares To String 
                JsonArray faresArr = new JsonArray();

                foreach (Fare fare in fares)
                    faresArr.Add(fare.ToJson());

                //flights To String
                JsonArray flightsArr = new JsonArray();

                //ОГРАИЧИВАЕМ РАЗМЕР МАССИВА
                int flights_limit = 10000; //ограничение по количеству найденных билетов

                if (flights != null)
                    foreach (Flight flight in flights)
                        if (flights_limit-- >= 0) flightsArr.Add(flight.ToJson());

                string insert_query = "insert into `FlightSearches` (sr_date_time,sr_service_class,sr_route,sr_request_id,sr_ad,sr_ch,sr_inf,sr_isfinished,sr_flights,sr_fares) ";
                insert_query += "VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
                                       "'" + this.serviceClass + "', " +
                                       "'" + this.route.ToString() + "', " +
                                       "'" + this.search_Id + "', " +
                                       this.adult + ", " +
                                       this.children + ", " +
                                       this.inf + ", " +
                                       this.isFinished + ", " +
                                       "'', '')";



                MySqlCommand MySqlCommand = new MySqlCommand(insert_query, MySqlConnections[0]);

                bool OK = false;
                Random Rnd = new Random();

                while (!OK)
                {
                    try
                    {
                        MySqlCommand.ExecuteNonQuery();
                        OK = true;
                    }
                    catch (Exception exDead)
                    {
                        if (exDead.Message.ToLower().Contains("deadlock"))
                            System.Threading.Thread.Sleep(Rnd.Next(1000, 3000));
                        else
                            throw exDead;
                    }
                }

                long last_id = MySqlCommand.LastInsertedId;

                int db_index = Convert.ToInt32(last_id % MySqlDbCount);

                string insert_flights_query = "insert into `FlightSearchRes` (res_sr_id, res_flights, res_fares) VALUES(" +
                                                last_id + ",'" + flightsArr.ToString().Replace("'", "`") + "','" + faresArr.ToString().Replace("'", "`") + "')";

                MySqlCommand MySqlFlightsCommand = new MySqlCommand(insert_flights_query, MySqlConnections[db_index]);

                if (db_index != 0)
                    _openConnection(db_index);

                OK = false;
                Rnd = new Random();

                while (!OK)
                {
                    try
                    {
                        MySqlFlightsCommand.ExecuteNonQuery();
                        OK = true;
                    }
                    catch (Exception exDead)
                    {
                        if (exDead.Message.ToLower().Contains("deadlock"))
                            System.Threading.Thread.Sleep(Rnd.Next(1000, 3000));
                        else
                            throw exDead;
                    }
                }

                MySqlConnections[0].Close();

                if (db_index != 0)
                    MySqlConnections[db_index].Close();

                if (this.isFinished == 1) this.WriteToDataBase(db_index);

                this.fares = null;
                this.flights = null;
                this.Dispose();
                GC.Collect();
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("save res exception: " + ex.Message);
            }
        }

        //thread Start Method
        public void SaveEmpty()
        {
            try
            {
                _openConnection(0);

                //fares To String 
                JsonArray faresArr = new JsonArray();

                foreach (Fare fare in fares)
                    faresArr.Add(fare.ToJson());

                //flights To String
                JsonArray flightsArr = new JsonArray();

                foreach (Flight flight in flights)
                    flightsArr.Add(flight.ToJson());

                string insert_query = "insert into FlightSearches (sr_date_time,sr_service_class,sr_route,sr_request_id,sr_ad,sr_ch,sr_inf,sr_isfinished,sr_flights,sr_fares) ";
                insert_query += "VALUES('" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "', " +
                                       "'" + this.serviceClass + "', " +
                                       "'" + this.route.ToString() + "', " +
                                       "'" + this.search_Id + "', " +
                                       this.adult + ", " +
                                       this.children + ", " +
                                       this.inf + ", " +
                                       "2, " +
                                       "'[]', " +
                                       "'[]')";

                MySqlCommand MySqlCommand = new MySqlCommand(insert_query, MySqlConnections[0]);


                bool OK = false;
                Random Rnd = new Random();

                while (!OK)
                {
                    try
                    {
                        MySqlCommand.ExecuteNonQuery();
                        OK = true;
                    }
                    catch (Exception exDead)
                    {
                        if (exDead.Message.ToLower().Contains("deadlock"))
                            System.Threading.Thread.Sleep(Rnd.Next(1000, 3000));
                        else
                            throw exDead;
                    }
                }

                long last_id = MySqlCommand.LastInsertedId;

                int db_index = Convert.ToInt32(last_id % MySqlDbCount);

                string insert_flights_query = "insert into `FlightSearchRes` (res_sr_id, res_flights, res_fares) VALUES(" +
                                                last_id + ",'','')";

                if (db_index != 0)
                    _openConnection(db_index);
                MySqlCommand MySqlFlightsCommand = new MySqlCommand(insert_flights_query, MySqlConnections[db_index]);

                OK = false;
                Rnd = new Random();

                while (!OK)
                {
                    try
                    {
                        MySqlFlightsCommand.ExecuteNonQuery();
                        OK = true;
                    }
                    catch (Exception exDead)
                    {
                        if (exDead.Message.ToLower().Contains("deadlock"))
                            System.Threading.Thread.Sleep(Rnd.Next(1000, 3000));
                        else
                            throw exDead;
                    }
                }

                MySqlConnections[0].Close();

                if (db_index != 0)
                    MySqlConnections[db_index].Close();

                this.fares = null;
                this.flights = null;
                GC.Collect();
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("save empty exception: " + ex.Message);
            }
        }

        //thread Start Method
        public void UpdateResults()
        {
            try
            {
                _openConnection(0);

                //fares To String 
                JsonArray faresArr = new JsonArray();

                foreach (Fare fare in fares)
                    faresArr.Add(fare.ToJson());

                //flights To String
                JsonArray flightsArr = new JsonArray();


                //ОГРАИЧИВАЕМ РАЗМЕР МАССИВА
                int flights_limit = 10000;

                foreach (Flight flight in flights)
                    if (flights_limit-- >= 0)  flightsArr.Add(flight.ToJson());

                MySqlCommand MySqlCommand = new MySqlCommand("select max(sr_id) from FlightSearches where sr_request_id = '" + this.search_Id + "'", MySqlConnections[0]);

                int sr_id = Convert.ToInt32(MySqlCommand.ExecuteScalar());

                //сделать update в нужной базе данных
                string update_query = "update FlightSearches set sr_isfinished = " + this.isFinished + 
                                      " where sr_id = " + sr_id;

                MySqlCommand = new MySqlCommand(update_query, MySqlConnections[0]);

                bool OK = false;
                Random Rnd = new Random();

                while (!OK)
                {
                    try
                    {
                        MySqlCommand.ExecuteNonQuery();
                        OK = true;
                    }
                    catch (Exception exDead)
                    {
                        if (exDead.Message.ToLower().Contains("deadlock"))
                            System.Threading.Thread.Sleep(Rnd.Next(1000, 3000));
                        else
                            throw exDead;
                    }
                }


                int db_index = sr_id % MySqlDbCount;//index of database

                //сделать update в нужной базе данных
                update_query = "update FlightSearchRes set res_flights = '" + flightsArr.ToString().Replace("'", "`") + "', res_fares = '" + faresArr.ToString().Replace("'", "`") + "'" +
                                      " where res_sr_id = " + sr_id;

                MySqlCommand = new MySqlCommand(update_query, MySqlConnections[db_index]);

                if (db_index != 0)
                    _openConnection(db_index);

                 OK = false;
                 Rnd = new Random();

                while (!OK)
                {
                    try
                    {
                        MySqlCommand.ExecuteNonQuery();
                        OK = true;
                    }
                    catch (Exception exDead)
                    {
                        if (exDead.Message.ToLower().Contains("deadlock"))
                            System.Threading.Thread.Sleep(Rnd.Next(1000, 3000));
                        else
                            throw exDead;
                    }
                }

                MySqlConnections[0].Close();

                if (db_index != 0) MySqlConnections[db_index].Close();


                if (this.isFinished == 1) this.WriteToDataBase(db_index);

                this.fares = null;
                this.flights = null;
                GC.Collect();
            }
            catch (Exception e)
            {
                Logger.Logger.WriteToLog("update res exception: " + e.Message + " " + e.StackTrace);
            }
        }

        //returns saved in DB fares
        public SearchResult GetFaresFromDB(string searchId, DateTime timeLimit)
        {
            SearchResult result = null;
            try
            {
                _openConnection(0);

                string select_query = "select sr_isfinished, sr_id from FlightSearches where sr_request_id = '" + searchId +
                                      "' and sr_date_time > '" + timeLimit.ToString("yyyy-MM-dd HH:mm:ss") + "' and not sr_isfinished = 2  order by sr_date_time desc limit 0, 1";


                Logger.Logger.WriteToLog("db_count " + MySqlConnections.Length);

                MySqlCommand MyCommand = new MySqlCommand(select_query, MySqlConnections[0]);
                MySqlDataReader reader = MyCommand.ExecuteReader();


                int sr_id = -1;
                int isFinished = -1;
                if (reader.Read())
                {
                    sr_id = Convert.ToInt32(reader["sr_id"].ToString());
                    isFinished = Convert.ToInt32(reader["sr_isfinished"]);

                    Logger.Logger.WriteToLog("sr_id " + sr_id + " isFinished = " + isFinished);



                    /*    if (fares_string.Length > 0)
                        {
                            JsonArray faresArr = Jayrock.Json.Conversion.JsonConvert.Import(typeof(JsonArray), fares_string) as JsonArray;
                            Fare[] fares = new Fare[faresArr.Length];

                            for (int i = 0; i < fares.Length; i++)
                                fares[i] = new Fare(faresArr[i] as JsonObject);

                            reader.Close();
                            return new SearchResult(searchId, fares, isFinished);
                        }
                     */

                }
                reader.Close();

                if (sr_id > 0)
                {
                    int db_index = sr_id % MySqlDbCount;

                    Logger.Logger.WriteToLog("db_index " + db_index);

                    string select_fares_query = "select res_fares from FlightSearchRes where res_sr_id = " + sr_id +
                                                 " limit 0, 1";

                    if (db_index != 0)
                        _openConnection(db_index);

                    MyCommand = new MySqlCommand(select_fares_query, MySqlConnections[db_index]);
                    reader = MyCommand.ExecuteReader();

                    if (reader.Read())
                    {
                        string fares_string = reader["res_fares"].ToString();
                        JsonArray faresArr = Jayrock.Json.Conversion.JsonConvert.Import(typeof(JsonArray), fares_string) as JsonArray;
                        Fare[] fares = new Fare[faresArr.Length];

                        for (int i = 0; i < fares.Length; i++)
                            fares[i] = new Fare(faresArr[i] as JsonObject);

                        reader.Close();
                        this.Dispose();

                        return new SearchResult(searchId, fares, isFinished);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("get fares exception: " + ex.Message);
            }

            return result;
        }

        //returns saved in DB flights
        public SearchResultFlights GetFlightsFromDB(string searchId, DateTime timeLimit)
        {
            SearchResultFlights result = null;
            try
            {
                _openConnection(0);

                string select_query = "select sr_id, sr_isfinished from FlightSearches where sr_request_id = '" + searchId +
                                      "' and sr_date_time > '" + timeLimit.ToString("yyyy-MM-dd HH:mm:ss") + "' and not sr_isfinished = 2  order by sr_date_time desc limit 0, 1";

                MySqlCommand MyCommand = new MySqlCommand(select_query, MySqlConnections[0]);
                MySqlDataReader reader = MyCommand.ExecuteReader();


                int sr_id = -1;
                int isFinished = -1;

                if (reader.Read())
                {
                    sr_id = Convert.ToInt32(reader["sr_id"]);
                    isFinished = Convert.ToInt32(reader["sr_isfinished"]);

                    /* if (flights_string.Length > 0)
                     {
                         JsonArray flightsArr = Jayrock.Json.Conversion.JsonConvert.Import(typeof(JsonArray), flights_string) as JsonArray;
                         Flight[] flights = new Flight[flightsArr.Length];

                         for (int i = 0; i < flights.Length; i++)
                             flights[i] = new Flight(flightsArr[i] as JsonObject);

                         reader.Close();
                         return new SearchResultFlights(searchId, flights, isFinished);
                     }
                     */
                }
                else
                {
                    reader.Close();

                    select_query = "select sr_id, sr_isfinished from FlightSearches where sr_request_id = '" + searchId +
                                      "' and sr_date_time > '" + timeLimit.ToString("yyyy-MM-dd HH:mm:ss") + "'  order by sr_date_time desc limit 0, 1";

                    MyCommand = new MySqlCommand(select_query, MySqlConnections[0]);
                    reader = MyCommand.ExecuteReader();

                    if (reader.Read())
                    {
                        sr_id = Convert.ToInt32(reader["sr_id"]);
                        isFinished = Convert.ToInt32(reader["sr_isfinished"]);
                    }

                    return new SearchResultFlights(searchId, new Flight[0], isFinished, sr_id);
                }
                reader.Close();


                Logger.Logger.WriteToLog("GetFlightsFromDB sr_id  = " + sr_id);

                if (sr_id > 0)
                {
                    int db_index = sr_id % MySqlDbCount;

                    Logger.Logger.WriteToLog("GetFlightsFromDB db_index  = " + db_index);

                    string select_fares_query = "select res_flights from FlightSearchRes where res_sr_id = " + sr_id +
                                                 " limit 0, 1";

                    if (db_index != 0)
                        _openConnection(db_index);

                    MyCommand = new MySqlCommand(select_fares_query, MySqlConnections[db_index]);
                    reader = MyCommand.ExecuteReader();

                    if (reader.Read())
                    {
                        string flights_string = reader["res_flights"].ToString();

                        JsonArray flightsArr = Jayrock.Json.Conversion.JsonConvert.Import(typeof(JsonArray), flights_string) as JsonArray;
                        Flight[] flights = new Flight[Math.Min(flightsArr.Length, 10000)];

                        for (int i = 0; i < flights.Length; i++)
                            flights[i] = new Flight(flightsArr[i] as JsonObject);

                        reader.Close();

                        Logger.Logger.WriteToLog("GetFlightsFromDB fl_count  = " + flights.Length);

                        return new SearchResultFlights(searchId, flights, isFinished, sr_id);
                    }
                    else
                    {
                        Logger.Logger.WriteToLog("rescue from unknown searchId");
                        return new SearchResultFlights(searchId, new Flight[0], 0, sr_id);
                    }

                }
                this.Dispose();

            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("get flights exception: " + ex.Message);
            }

            return result;
        }

        public SearchResultFlights GetFlightsFromDB(int search_key)
        {
            SearchResultFlights result = null;
            try
            {
                int sr_id = search_key;
             

                Logger.Logger.WriteToLog("GetFlightsFromDB sr_id  = " + sr_id);

                if (sr_id > 0)
                {
                    int db_index = sr_id % MySqlDbCount;

                    Logger.Logger.WriteToLog("GetFlightsFromDB db_index  = " + db_index);

                    string select_fares_query = "select res_flights from FlightSearchRes where res_sr_id = " + sr_id +
                                                 " limit 0, 1";

                    //if (db_index != 0)
                    _openConnection(db_index);

                    MySqlCommand MyCommand = new MySqlCommand(select_fares_query, MySqlConnections[db_index]);
                    MySqlDataReader reader = MyCommand.ExecuteReader();

                    if (reader.Read())
                    {
                        string flights_string = reader["res_flights"].ToString();

                        JsonArray flightsArr = Jayrock.Json.Conversion.JsonConvert.Import(typeof(JsonArray), flights_string) as JsonArray;
                        Flight[] flights = new Flight[Math.Min(flightsArr.Length, 10000)];

                        for (int i = 0; i < flights.Length; i++)
                            flights[i] = new Flight(flightsArr[i] as JsonObject);

                        reader.Close();
                        // return new SearchResultFlights(searchId, flights, isFinished);
                        //  this.Dispose();

                        Logger.Logger.WriteToLog("GetFlightsFromDB fl_count  = " + flights.Length);

                        return new SearchResultFlights("fake", flights, isFinished, sr_id);
                    }
                }
                this.Dispose();

            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("get flights exception: " + ex.Message);
            }

            return result;
        }

        public Int32 GetRequestState(string searchId, DateTime timeLimit)
        {
            int isFinished = -1;
            _openConnection(0);
            try
            {
                string select_query = "select sr_isfinished from " +
                                        "FlightSearches  where sr_request_id = '" + searchId +
                                        "' and sr_date_time > '" + timeLimit.ToString("yyyy-MM-dd HH:mm:ss") + "' order by sr_date_time desc limit 0, 1";

                MySqlCommand MyCommand = new MySqlCommand(select_query, MySqlConnections[0]);
                MySqlDataReader reader = MyCommand.ExecuteReader();


                if (reader.Read())
                    isFinished = Convert.ToInt32(reader["sr_isfinished"]);

                reader.Close();
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("get state exception: " + ex.Message);

            }
            this.MySqlConnections[0].Close();
            return isFinished;
        }

        public void UpdateRequestState(string searchId, Int32 isFinished, DateTime timeLimit)
        {
            try
            {

                _openConnection(0);

                string update_query = "update " +
                                        "FlightSearches set sr_isfinished = " + isFinished + " where sr_request_id = '" + searchId +
                                        "' and sr_date_time > '" + timeLimit.ToString("yyyy-MM-dd HH:mm:ss") + "'";

                MySqlCommand MyCommand = new MySqlCommand(update_query, MySqlConnections[0]);
                ///MyCommand.ExecuteNonQuery();

                bool OK = false;
                Random Rnd = new Random();

                while (!OK)
                {
                    try
                    {
                        MyCommand.ExecuteNonQuery();
                        OK = true;
                    }
                    catch (Exception exDead)
                    {
                        if (exDead.Message.ToLower().Contains("deadlock"))
                            System.Threading.Thread.Sleep(Rnd.Next(100, 300));
                        else
                            throw exDead;
                    }
                }

                MySqlConnections[0].Close();

            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("update state exception: " + ex.Message);       
            }
        }

        public Dictionary<string, string> AirportsLib()
        {
            Dictionary<string, string> lib = new Dictionary<string, string>();
            try
            {


                _openConnection(0);

                string select_query = "select ap_code, ct_name from  ap_to_city_code, cities_lib  where city_code = ct_code";

                MySqlCommand MyCommand = new MySqlCommand(select_query, MySqlConnections[0]);
                MySqlDataReader reader = MyCommand.ExecuteReader();

                while (reader.Read())
                    lib.Add(reader["ap_code"].ToString(), reader["ct_name"].ToString());

                reader.Close();
                MySqlConnections[0].Close();
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("airports lib exception: " + ex.Message);    
            }

            return lib;
        }

        public Dictionary<string, string> AirlinesLib()
        {

            Dictionary<string, string> lib = new Dictionary<string, string>();

            try
            {

                _openConnection(0);

                string select_query = "select al_code, al_name from airlines_lib";

                MySqlCommand MyCommand = new MySqlCommand(select_query, MySqlConnections[0]);
                MySqlDataReader reader = MyCommand.ExecuteReader();

                while (reader.Read())
                    lib.Add(reader["al_code"].ToString(), reader["al_name"].ToString());

                reader.Close();
                MySqlConnections[0].Close();

            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("airlines lib exception: " + ex.Message);
            }

            return lib;
        }

        private static Dictionary<string, string> temp_fullId = new Dictionary<string,string>();

        private  string temp_shortId = "";

        public string GetFullId(string shortId)
        {
            try
            {
                temp_shortId = shortId;

                Thread[] threads = new Thread[MySqlDbCount];

                for (int i = 0; i < MySqlDbCount; i++)
                {
                    Thread thr = new Thread(new ParameterizedThreadStart(GetFullId));

                    thr.Start(i);

                    threads[i] = thr;
                }

                while (true)
                {
                    if (temp_fullId.ContainsKey(shortId))
                    {
                        temp_shortId = "";
                        Logger.Logger.WriteToLog("for short_id temp_shortId founded full Id " + temp_fullId[shortId]);
                        return temp_fullId[shortId];
                    }

                    bool all_is_dead = true;

                    for (int i = 0; (i < MySqlDbCount && all_is_dead); i++)
                    {
                        all_is_dead = all_is_dead && !threads[i].IsAlive;
                    }

                    if (all_is_dead) break;

                    Thread.Sleep(50);
                }

                temp_shortId = "";
                Logger.Logger.WriteToLog("founded full Id " + temp_fullId[shortId]);

                //string return_id = temp_fullId;
                //temp_fullId = "";
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("get full id exception: " + ex.Message);
            }

            return temp_fullId[shortId];
        }

        public void GetFullId(object db_index)
        {
            try
            {
                _openConnection(Convert.ToInt32(db_index));
                string select_query = "select ft_link_full from FlightTickets where ft_link like '%" + temp_shortId + "'";
                MySqlCommand MyCommand = new MySqlCommand(select_query, MySqlConnections[Convert.ToInt32(db_index)]);

                string full_id = MyCommand.ExecuteScalar().ToString();

                MySqlConnections[Convert.ToInt32(db_index)].Close();
                if (full_id.Length > 0)
                    temp_fullId[temp_shortId] = full_id;
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("GetFullId for db" + db_index + " Exception:" + ex.Message + " " + ex.StackTrace);
            }

        }

        //записывает найденные тикеты в базу
        //запись нужна для проверки перелетов сервисом рамблера
        public void WriteToDataBase(int db_index)
        {
            DataTable flightsTable = null;
            try
            {
                if ((this.flights == null) || (this.flights.Length == 0))
                    return;

                Containers.Flight[] flights = this.flights;
                int adl = this.adult;
                int ch = this.children;
                int inf = this.inf;

                //create Temp Table with flights
                flightsTable = FlightsToTicketsTable(flights, adl, ch, inf);
                _openConnection(db_index);

                string fileName =  WebConfigurationManager.AppSettings["TempFolder"] + "load_" + Thread.CurrentThread.ManagedThreadId + DateTime.Now.Ticks + ".txt";

                DumpTable(flightsTable, fileName);


                MySqlBulkLoader bl = new MySqlBulkLoader(MySqlConnections[db_index]);

                bl.FieldTerminator = "~`~";
                bl.LineTerminator = "#!#";
                bl.FileName = fileName;
                bl.NumberOfLinesToSkip = 0;
               // bl

                // set the destination table name
                bl.TableName = flightsTable.TableName;

                // write the data in the "dataTable"
                int cnt = bl.Load();

                MySqlConnections[db_index].Close();
                flightsTable.Clear();

                //File.Delete(fileName);

           //     if(this.is_rambler == "false") SendToRambler(this.route_id);
            }
            catch (Exception ex)
            {
                MySqlConnections[db_index].Close();
                if ((this.flights != null) && (this.flights.Length > 0))
                    Logger.Logger.WriteToLog("DB " + ex.Message + " " + ex.StackTrace + "--->" + this.flights[0].Parts[0].Legs[0].LocationBeginName + " - " + this.flights[0].Parts[0].Legs[this.flights[0].Parts[0].Legs.Length-1].LocationEndName + " " + this.flights[0].Parts[0].Legs[0].DateBegin.ToString("yy.MM.dd"));

             
             //   Logger.Logger.WriteToLog("dump: " + dump);
            }
        }

        private static void DumpTable(DataTable table , string fileName)
        {
            try
            {
                StreamWriter file = new StreamWriter(fileName);

                string rows_separator = "";
                string cells_separator = "";
                foreach (DataRow row in table.Rows)
                {
                    file.Write(rows_separator);
                    rows_separator = "#!#";

                    cells_separator = "";
                    foreach (object item in row.ItemArray)
                    {
                        file.Write(cells_separator);
                        cells_separator = "~`~";
                        file.Write(item);
                    }
                }

                file.Close();
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("dump table exception: "+ ex.Message);
            }
        }

        private static string EncodeTo64(string toEncode)
        {
            byte[] toEncodeAsBytes

                  = System.Text.ASCIIEncoding.ASCII.GetBytes(toEncode);

            string returnValue

                  = System.Convert.ToBase64String(toEncodeAsBytes);

            return returnValue;

        }

        private void SendToRambler(string searchId)
        {
            try
            {
                var postData = "{\"jsonrpc\": \"2.0\", \"id\":\"l1HjbG9d\", \"method\": \"send_request\", \"params\": [\"" + searchId + "\"] }";

                Uri remoteUri = new Uri("http://online.clickandtravel.ru/rambler_json_rpc/v2/AviaApi.ashx");//"/"

                WebRequest request = WebRequest.Create(remoteUri);
                request.ContentType = "application/x-www-form-urlencoded";

                string login = "rambler";
                string password = "rambler%^ty&*";
                string hash = EncodeTo64(login + ":" + password);//.to
                request.Headers.Add("Authorization", "Basic " + hash);

                request.Method = "POST";

                byte[] bytes = System.Text.Encoding.ASCII.GetBytes(postData);
                request.ContentLength = bytes.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                WebResponse response = request.GetResponse();
                StreamReader reader = new StreamReader(response.GetResponseStream());
                var result = reader.ReadToEnd().Trim();

                Logger.Logger.WriteToLog("send to rambler " + result + " route: "+ this.route_id);
            }
            catch(Exception ex)
            { Logger.Logger.WriteToLog("Send rembler exc " + ex.Message + " " +ex.StackTrace); }
        }

        private string SqlInjectionProtect(string inp)
        {
            inp = inp.ToUpper();

            string[] words = new string[] {"UPDATE","EXECUTE","--","DROP","ALTER","CREATE","INSERT","DELETE","SELECT"};

            foreach(string word in words)
                inp = inp.Replace(word, "");

            return inp;
        }

        private static string temp_check_tickets = null;
        private static Dictionary<string, Containers.PriceLink> temp_dict = new Dictionary<string,PriceLink>();

        public void CheckTicketsInDatabase(object db_index_obj)
        {
            try
            {
                int db_index = Convert.ToInt32(db_index_obj);

                this._openConnection(db_index);

                Logger.Logger.WriteToLog("in check db " + db_index_obj);
                MySqlCommand myCommmand = new MySqlCommand("", MySqlConnections[db_index]);
                //сделать запрос в базу по хэш-кодам
                myCommmand.CommandText = "select ft_price, ft_link, ft_hash from FlightTickets where ft_hash in (" + temp_check_tickets + ")";
                MySqlDataReader reader = null;
                try
                {
                    //Dictionary<string, Containers.PriceLink> dict = new Dictionary<string, PriceLink>();
                    reader = myCommmand.ExecuteReader();

                    //выгрузили результаты
                    while (reader.Read())
                        if (!temp_dict.Keys.Contains(reader["ft_hash"].ToString()))
                            temp_dict.Add(reader["ft_hash"].ToString(), new Containers.PriceLink(Convert.ToInt32(reader["ft_price"]),
                                                                                        reader["ft_link"].ToString()));

                }
                catch (Exception ex)
                {
                    Logger.Logger.WriteToLog("DB exception  " + (db_index + 1) + " " + ex.Message);
                }
                reader.Close();
                MySqlConnections[db_index].Close();
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("check tickets exception: " + ex.Message);
            }
        }

        public Containers.PriceLink[] CheckTickets(string[] ticketsHashes)
        {
            string keys_arr = "'nonono'";
            for (int i = 0; i < ticketsHashes.Length; i++)
            {
                ticketsHashes[i] = SqlInjectionProtect(ticketsHashes[i]);
                keys_arr += ",'" + ticketsHashes[i] + "'";
            }

            temp_check_tickets = keys_arr;
            temp_dict = new Dictionary<string, PriceLink>();

            //this._openConnection();
            //удаляем записи, которым больше 40 минут
         //   MySqlCommand myCommmand = new MySqlCommand("delete from FlightTickets  where ft_request_date < '" + DateTime.Now.AddMinutes(-40).ToString("yyyy-MM-dd HH:mm:ss") + "'", this.MySqlConnection);
         //   myCommmand.ExecuteNonQuery();
            Logger.Logger.WriteToLog("in check wait finish");
            try{
                // run threads

                Thread[] threads = new Thread[MySqlDbCount];

                for (int i = 0; i < MySqlDbCount; i++)
                {
                    Thread thr = new Thread(new ParameterizedThreadStart(CheckTicketsInDatabase));

                    thr.Start(i);

                    threads[i] = thr;
                }

                //check threads
                while (true)
                {
                    if (temp_dict.Count > 0) break;

                    bool all_is_dead = true;

                    for (int i = 0; i < MySqlDbCount; i++)
                    {
                        all_is_dead = all_is_dead && !threads[i].IsAlive;
                    }

                    if (all_is_dead) break;

                    Thread.Sleep(50);
                    Logger.Logger.WriteToLog("check wait finish");
                }


                Logger.Logger.WriteToLog("check founded " + temp_dict.Count);

                Containers.PriceLink[] results = new Containers.PriceLink[ticketsHashes.Length];
                for (int i = 0; i < ticketsHashes.Length; i++)
                {
                    results[i] = temp_dict.Keys.Contains(ticketsHashes[i]) ? temp_dict[ticketsHashes[i]] : null;

                    if(results[i] == null)
                        try {
                            Logger.Logger.WriteToLog("ticket not founded " + ticketsHashes[i]); 
                        }
                        catch (Exception) { }
                }

                //run clearing
                Random rand = new Random(100);
                if (rand.Next(100) < 50)
                {
                    Logger.Logger.WriteToLog("clearing started");
                    Thread starter = new Thread(Clearing);
                    starter.Start();
                }

                this.Dispose();
                return results;
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("DB exception  " + ex.Message + " " + ex.StackTrace);
               this.Dispose();
                
            }   
            return null;
        }

        #endregion

        private DataTable FlightsToTicketsTable(Containers.Flight[] flights,  int adl, int ch, int inf)
        {
            string hash_postfix =  "," + adl + "," + ch +","+ inf;

            DataTable ticketsTable = CreateTicketsTable();

            foreach(Containers.Flight fl in flights)
            {
                ticketsTable.Rows.Add(
                        new object[]
                        {
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                            fl.Price,
                            this.route_id+'`'+fl.Id,
                            fl.FullId,
                            fl.FlightHash + hash_postfix
                        }
                    );
            }
            return ticketsTable;
        }

        private DataTable CreateTicketsTable()
        {
            DataTable ticketsTable = new DataTable("FlightTickets");
            ticketsTable.Columns.Add("ft_request_date", typeof(string));
            ticketsTable.Columns.Add("ft_price", typeof(Int32));
            ticketsTable.Columns.Add("ft_link", typeof(string));
            ticketsTable.Columns.Add("ft_link_full", typeof(string));
            ticketsTable.Columns.Add("ft_hash", typeof(string));
           
            return ticketsTable;
        }

        public void Clearing()
        { 
            //clear FlightSearchRes
            try
            {
                string delete_query = "delete from FlightTickets where ft_request_date < '" + DateTime.Now.AddMinutes(-40).ToString("yyyy-MM-dd HH:mm:ss") + "'";

                for (int i = 0; i < MySqlDbCount; i++)
                {
                    _openConnection(i);
                    MySqlCommand command = new MySqlCommand(delete_query, MySqlConnections[i]);

                    Random Rnd = new Random();

                    //  bool OK = false;
                    // while (!OK)
                    {
                        try
                        {
                            command.ExecuteNonQuery();
                            //OK = true;
                        }
                        catch (Exception exDead)
                        {
                            if (exDead.Message.ToLower().Contains("deadlock"))
                                System.Threading.Thread.Sleep(Rnd.Next(20, 100));
                        }
                    }
                }

                //clear FlightTickets

                //select 

                string select_query = "select max(sr_id) from FlightSearches where sr_date_time <'" + DateTime.Now.AddMinutes(-40).ToString("yyyy-MM-dd HH:mm:ss") + "'";

                MySqlCommand get_max_command = new MySqlCommand(select_query, MySqlConnections[0]);

                int max_sr_id = Convert.ToInt32(get_max_command.ExecuteScalar());

                string update_query = "delete from FlightSearchRes where res_sr_id < " + max_sr_id;

                for (int i = 0; i < MySqlDbCount; i++)
                {
                    MySqlCommand command = new MySqlCommand(delete_query, MySqlConnections[i]);

                    bool OK = false;
                    Random Rnd = new Random();

                    while (!OK)
                    {
                        try
                        {
                            command.ExecuteNonQuery();
                            OK = true;
                        }
                        catch (Exception exDead)
                        {
                            if (exDead.Message.ToLower().Contains("deadlock"))
                                System.Threading.Thread.Sleep(Rnd.Next(20, 100));
                        }
                    }
                }

                //clear Temp folder

                string[] filePaths = Directory.GetFiles(WebConfigurationManager.AppSettings["TempFolder"]);

                foreach (string filePath in filePaths)
                {
                    if (File.GetCreationTime(filePath) < DateTime.Now.AddMinutes(-5))
                        File.Delete(filePath);
                }

                Logger.Logger.WriteToLog("!!!! Clearing done");
                this.Dispose();
            }
            catch (Exception ex)
            {
                Logger.Logger.WriteToLog("clearing exception: " + ex.Message);   
            }
        }


        public static string ExecuteQueryToClick(string query)
        {
            SqlConnection con = new SqlConnection(ClickConnectionString);
            con.Open();

            SqlCommand com = new SqlCommand(query, con);

            string value = com.ExecuteScalar().ToString();

            con.Close();

            return value;
        }
    }
}