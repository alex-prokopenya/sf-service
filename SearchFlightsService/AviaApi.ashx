<%@ WebHandler Language="C#" Class="SearchFlightsService.AviaApi" debug="true" %>

namespace SearchFlightsService
{
    using System;
    using System.Web;
    using Jayrock.Json;
    using Jayrock.Json.Conversion;
    using Jayrock.JsonRpc;
    using Jayrock.JsonRpc.Web;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using SearchFlightsService.Containers;
    using SearchFlightsService.Core;
    using SearchFlightsService.Ext;
    using System.Xml;
    using System.Net;
    using System.IO;
    using System.IO.Compression;
    using System.Text;
    using SF_service;
    using Core;
    
    public class AviaApi: JsonRpcHandler
    {
        public static void WriteToLog(string message)
        {
            try
            {
                
                StreamWriter outfile = new StreamWriter("" + AppDomain.CurrentDomain.BaseDirectory + @"/check_log/" + DateTime.Today.ToString("yyyy-MM-dd") + ".log", true);
                {
		            outfile.WriteLine("");
		            outfile.WriteLine("_________________________");
                    outfile.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + message);
                }
                outfile.Close();
            }
            catch (Exception)
            { }
        }


	private static int  RAMBLER_MARGIN  = 10;
		
        private void checkGzip()
        {
            HttpResponse Response = HttpContext.Current.Response;

            if (HttpContext.Current.Request.Headers["Accept-Encoding"] == null) return;
            
            if (HttpContext.Current.Request.Headers["Accept-Encoding"].IndexOf("gzip") != -1)
            {
                Response.Filter = new System.IO.Compression.GZipStream(Response.Filter,
                                                    System.IO.Compression.CompressionMode.Compress);
                
                Response.AppendHeader("Content-Encoding", "gzip");
            }
        }

		private bool IsGlobalet()
		{
			string globaletHash = "Z2xvYmFsZXQ6QmVxYXphNGg=";
			if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + globaletHash) return true;
			
			return false;
		}

		private bool IsTest()
		{
			string testHash = "dGVzdDp0ZXN0cGFzc3dvcmQ=";
			if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + testHash) return true;
			
			return false;
		}

		
		private bool IsPlus()
		{
			string testHash = "YmlsZXR5cGx1czpkZmFzQCNXRQ==";
			if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + testHash) return true;
			
			return false;
		}


		private bool IsBuruki()
		{
			string testHash = "YnVydWtpOmtpM3J1MmJ1MQ==";
			if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + testHash) return true;
			
			return false;
		}

        private bool CheckAuth()
        {
            HttpResponse Response = HttpContext.Current.Response;

            string ramblerHash = "cmFtYmxlcjpyYW1ibGVyJV50eSYq";
            string globaletHash = "Z2xvYmFsZXQ6QmVxYXphNGg=";
	        string testHash = "dGVzdDp0ZXN0cGFzc3dvcmQ=";
	        string plusHash = "YmlsZXR5cGx1czpkZmFzQCNXRQ==";
	        string burukiHash = "YnVydWtpOmtpM3J1MmJ1MQ=="; 	//buruki:ki3ru2bu1
			
            string port = HttpContext.Current.Request.ServerVariables["SERVER_PORT"];

            if (port != "443") return false;
                
            if (HttpContext.Current.Request.Headers["Authorization"] == null) return false;
            if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + ramblerHash) return true;
            if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + globaletHash) return true;
	        if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + plusHash) return true;
	        if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + burukiHash) return true;
	        if (HttpContext.Current.Request.Headers["Authorization"] == "Basic " + testHash) return true;

            return false;
        }
        
        [ JsonRpcMethod("ping") ]
        public JsonObject ping()
        {
            string headers = "";
            foreach (string str in HttpContext.Current.Request.Headers.AllKeys)
                headers += ", " + str;

            JsonObject jo = new JsonObject(new string[] { "api_version", "profile", "version", "contact" }, new string[] { "2", "dev", "1", "it@viziteurope.eu" });
            return jo;
        }
        
        [JsonRpcMethod("search_tickets")]
        public object search_tickets(params object[] args)
        {
        
		    SF_service sf_service = new SF_service();

		    Random rand = new Random(100);

            JsonArray inp = new JsonArray();
            foreach(object item in args)
                inp.Add(item);

            //преобразуем входной параметр в объект Query
            Query query = new Query(inp);

            //проверить даты
            if (Convert.ToDateTime("" + query.QuerySegments[0].date[0] + "-" + query.QuerySegments[0].date[1] + "-" + query.QuerySegments[0].date[2]) < DateTime.Today)
                throw new Core.RamblerAviaException("некорректные даты или сочетание дат", 32010);

            if (query.QuerySegments.Length > 1)
            { 
                DateTime date1 = Convert.ToDateTime("" + query.QuerySegments[0].date[0] + "-" + query.QuerySegments[0].date[1] + "-" + query.QuerySegments[0].date[2]);
                DateTime date2 = Convert.ToDateTime("" + query.QuerySegments[1].date[0] + "-" + query.QuerySegments[1].date[1] + "-" + query.QuerySegments[1].date[2]);
                
                if(date1 > date2)
                    throw new Core.RamblerAviaException("некорректные даты или сочетание дат", 32010);
            }   
            
            //проверить маршрут
            if(query.QuerySegments.Length > 2)
                throw new Core.RamblerAviaException("маршрут невозможен" , 32012);
            
            if((query.QuerySegments.Length == 2)&&((query.QuerySegments[0].from != query.QuerySegments[1].to) || (query.QuerySegments[1].from != query.QuerySegments[0].to)))
                throw new Core.RamblerAviaException("маршрут невозможен" , 32012);

            //готовим запрос на поиск
            Route route = new Route();
            route.Segments = new RouteSegment[query.QuerySegments.Length];
            
            //добавляем маршрут перелета
            for(int i=0; i < query.QuerySegments.Length; i++)
            {
                QuerySegment qs =  query.QuerySegments[i];
                    
                string ap_from = qs.from;

               // if (ap_from.Length != 3) 
              //      ap_from = DataStore.GetIataCodeByIcao(ap_from);
              //  else
              //      ap_from = DataStore.CheckPoint(ap_from);

               // if (ap_from == "Not Found") throw new RamblerAviaException("неизвестные коды точек маршрута " + ap_from  + qs.from, 32011);

                //..аэропорт прилета
                string ap_to = qs.to;

               // if (ap_to.Length != 3) 
               //     ap_to = DataStore.GetIataCodeByIcao(ap_to);
               // else
                //    ap_to = DataStore.CheckPoint(ap_to);

              //  if (ap_to == "Not Found") throw new Core.RamblerAviaException("неизвестные коды точек маршрута" + ap_from +" - " + ap_to, 32011);
                
                 route.Segments[i] = new RouteSegment(){Date = Convert.ToDateTime("" + qs.date[0] + "-" + qs.date[1] + "-" + qs.date[2]),
                                                        LocationBegin = ap_from,
                                                        LocationEnd = ap_to};
            }
            DateTime beg = DateTime.Now;
            SearchResultFlights srf = sf_service.Search_Full_Momondo(route, query.Adults, (query.Children + query.Infants), query.InfantsWithoutSeat, query.CabinClass, "true");
            DateTime end = DateTime.Now;


            decimal apply_margin = 1M;
            
            if(HttpContext.Current.Request.Url.AbsolutePath.Contains("buruki"))
                apply_margin = 0.98M;
            
            JsonArray json_tickets = new JsonArray();
            foreach (Flight fl in srf.Flights)
            {
               // Ticket ticket = FlightToTicket(fl);
                fl.Price = Convert.ToInt32(Math.Round(fl.Price / apply_margin));
                JsonArray ticket_arr = fl.ToJsonArray();

                
                
                ticket_arr.Add("/api/avia/Base.php?Link=" + srf.RequestId + "`" + fl.Id + "&ravia={mark}");
                json_tickets.Add(ticket_arr);
            }
	//
        //    DateTime end = DateTime.Now;
		
	        try{
		         WriteToLog("started at " +beg.ToString("mm:ss fff") + " ended at " + end.ToString("mm:ss fff"));		
	        }
	        catch(Exception)
	        {}

            return "ok";//json_tickets;
        }

        private Ticket FlightToTicket(global::SF_service.Flight flight)
        {
            Ticket ticket = new Ticket();
            //ticket.Price = flight.Price;
	    	ticket.Price = flight.Price - RAMBLER_MARGIN;
            ticket.Route = new Route();
            ticket.Route.Segments = new Segment[flight.Parts.Length];

            for (int i = 0; i < flight.Parts.Length; i++)
            {
                FlightPart part = flight.Parts[i];
                Segment segment = new Segment();
                segment.Flights = new Flight[part.Legs.Length];
                
                for (int j = 0; j < part.Legs.Length; j++)
                {
                    Leg leg = part.Legs[j];
                    Flight ticketFlight = new Flight();
                    ticketFlight.AirlineCode = leg.Airline;
                    ticketFlight.ArrivalAirport = leg.LocationEnd;
                    ticketFlight.BookingClass = leg.BookingClass;
                    ticketFlight.CabinClass = leg.ServiceClass;
                    ticketFlight.DepartureAirport = leg.LocationBegin;
                    ticketFlight.Duration = leg.Duration;
                    ticketFlight.FlightNumber = leg.FlightNumber;
                    ticketFlight.PlaneCode = leg.Board;
                    ticketFlight.ArrivalDateTime = DateToIntArray(leg.DateEnd);
                    ticketFlight.DepartureDateTime = DateToIntArray(leg.DateBegin);

                    segment.Flights[j] = ticketFlight;
                }
                ticket.Route.Segments[i] = segment;
            }
            return ticket;
        }

        private int[] DateToIntArray(DateTime date)
        {
            string strDate = date.ToString("yyyy MM dd HH mm");
            string[] strArray = strDate.Split(' ');

            int[] res = new int[strArray.Length];
            
            for(int i=0; i<strArray.Length; i++)
                res[i] = Convert.ToInt32(strArray[i]);

            return res;
        }

    [JsonRpcMethod("check_availability")]
    public object check_availability(params object[] args)
    {

       // HttpContext.Current.Request.Url.AbsolutePath;
        
	    //	throw new Exception("stop");
	    try{
                Ticket_reduced[] tickets = new Ticket_reduced[args.Length];

                int cnt = 0;

                foreach(object item in args)
                    tickets[cnt++] = new Ticket_reduced(item as JsonArray);

                string[] hashes = new string[tickets.Length];
                for (int i = 0; i < tickets.Length; i++ )
                    hashes[i] = GetTicketHash(tickets[i]);

                SF_service sf_service = new SF_service();

                PriceLink[] links = sf_service.CheckFlights(hashes);


                decimal apply_margin = 1M;

                if (HttpContext.Current.Request.Url.AbsolutePath.Contains("buruki"))
                    apply_margin = 0.98M;
            
            
                JsonArray res = new JsonArray();
                foreach (PriceLink link in links)
                    if (link == null)
                        res.Add(JsonNull.Value);
                    else
                        res.Add(new object[] { Math.Round(link.Price / apply_margin) - RAMBLER_MARGIN, "http://www.clickandtravel.ru/api/avia/Base.php?Link=" + link.Link + "&ravia={mark}" });

	            WriteToLog(res.ToString());

	            if(res.ToString()=="null")
	            {
		            WriteToLog(tickets[0].ToJsonArray().ToString());
		            WriteToLog(hashes[0]);
	            }

                return res;
	        }
	        catch(Exception ex)
	        {
	 	        WriteToLog(ex.Message);
		        throw ex;
	        }
        }
		
        private static string GetTicketHash(Ticket_reduced ticket)
        {
            //DME,2012,12,13,1,5,DXB,2012,12,13,6,20,EK,132,E,U/DXB,2012,12,13,11,20,PEK,2012,12,13,22,30,EK,308,E,U/|PEK,2012,12,20,22,45,WUH,2012,12,21,0,40,MU,2462,E,Y/WUH,2012,12,21,8,0,PVG,2012,12,21,9,30,MU,517,E,K/PVG,2012,12,21,12,20,SVO,2012,12,21,18,5,MU,591,E,V/|60094,2,0,0

            string res = "";
            foreach (Segment_reduced segment in ticket.Route.Segments)
            {
                foreach (Flight_reduced flight in segment.Flights)
                {
                    res += flight.DepartureAirport + "," +
                           flight.DepartureDateTime[0] + "," + flight.DepartureDateTime[1] + "," + flight.DepartureDateTime[2] + "," + flight.DepartureDateTime[3] + "," + flight.DepartureDateTime[4] + "," +
                           flight.ArrivalAirport + "," +
                           flight.ArrivalDateTime[0] + "," + flight.ArrivalDateTime[1] + "," + flight.ArrivalDateTime[2] + "," + flight.ArrivalDateTime[3] + "," + flight.ArrivalDateTime[4] + ","+
                           flight.AirlineCode+","+flight.FlightNumber+","+flight.CabinClass + "," + flight.BookingClass;
                    
                    res += "/";
                }
                res += "|";
            }
            res += ","+ticket.Adults + "," + (ticket.Children + ticket.Infants) + "," + ticket.InfantsWithoutSeat;

            return res;
        }

        [JsonRpcMethod("add_mark")]
        public void add_mark(params object[] args)
        {
            string mark = Convert.ToString(args[0]);
            string dogovor = Convert.ToString(args[1]);
            string request_id = Convert.ToString(args[2]);
            string fare = Convert.ToString(args[3]);
            string variants = Convert.ToString(args[4]);

      //      Core.DataStore.FixMarkToDB(mark, dogovor, request_id, fare, variants); 
        } 
        
        [JsonRpcMethod("list_bookings")]
        public object list_bookings(object[] date1, object[] date2)
        {
            if (!CheckAuth())
            {
                HttpResponse Response = HttpContext.Current.Response;
                Response.StatusCode = 401;
                Response.Headers.Add("WWW-Authenticate","Basic realm=\"type your password\"");

                return null;
            }
			
			//получить префикс для фильтрации путевок
			string service_filter = "not mark like 'buruki%' and not mark like 'plus%' and not mark like 'globalet%' and  not mark like 'test%' and ";

			
			if(IsGlobalet())
				service_filter = " mark like 'globalet%' and ";


			if(IsPlus())
				service_filter = " mark like 'plus%' and ";

			if(IsBuruki())
				service_filter = " mark like 'buruki%' and ";

			if(IsTest())
				service_filter = " mark like 'test%' and ";
				
            DateTime date_from = Convert.ToDateTime("" + date1[0] + "-" + date1[1] + "-" + date1[2]);
            DateTime date_to   = Convert.ToDateTime("" + date2[0] + "-" + date2[1] + "-" + date2[2]);

          //  return Core.DataStore.GetListOfBooks(date_from, date_to, service_filter);
        }

        [JsonRpcMethod("send_request")]
        public object send_request(object searchId)
        {
            return null;
            ////парсим строку в query
            //string[] input_query = searchId.ToString().Split('-');

            //if (input_query.Length < 5) return null;

            //string route = input_query[0];

            //Query qr = new Query();
            //qr.Adults = Convert.ToInt32(input_query[1]);
            //qr.Children = Convert.ToInt32(input_query[2]);
            //qr.Infants = 0;
            //qr.InfantsWithoutSeat = Convert.ToInt32(input_query[3]);
            //qr.CabinClass = input_query[4];
            //int route_parts_cnt = route.Length / 10;
            
            //qr.QuerySegments = new QuerySegment[route_parts_cnt];

            //for (int i = 0; i < route_parts_cnt; i++)
            //{
            //    string route_part = route.Substring(0, 10);
            //    route = route.Remove(0, 10);
                
            //    QuerySegment qs = new QuerySegment();
            //    qs.from = route_part.Substring(4,3);
            //    qs.to   = route_part.Substring(7,3);

            //    DateTime date = Convert.ToDateTime("" + DateTime.Today.Year + "-" + route_part.Substring(2, 2) + "-" + route_part.Substring(0, 2));

            //    if (date < DateTime.Today) date = date.AddYears(1);

            //    qs.date = new int[] {date.Year, date.Month, date.Day};

            //    qr.QuerySegments[i] = qs;
            //}
        
            //JsonArray inp = qr.ToJsonArray();

            //Query query = qr;
            ////проверить даты
            //if (Convert.ToDateTime("" + query.QuerySegments[0].date[0] + "-" + query.QuerySegments[0].date[1] + "-" + query.QuerySegments[0].date[2]) < DateTime.Today)
            //    return null;

            //if (query.QuerySegments.Length > 1)
            //{
            //    DateTime date1 = Convert.ToDateTime("" + query.QuerySegments[0].date[0] + "-" + query.QuerySegments[0].date[1] + "-" + query.QuerySegments[0].date[2]);
            //    DateTime date2 = Convert.ToDateTime("" + query.QuerySegments[1].date[0] + "-" + query.QuerySegments[1].date[1] + "-" + query.QuerySegments[1].date[2]);

            //    if (date1 > date2)
            //        return "некорректные даты или сочетание дат";
            //}

            ////проверить маршрут
            //if (query.QuerySegments.Length > 2)
            //    return "маршрут невозможен";

            //if ((query.QuerySegments.Length == 2) && ((query.QuerySegments[0].from != query.QuerySegments[1].to) || (query.QuerySegments[1].from != query.QuerySegments[0].to)))
            //    return "маршрут невозможен";

            //SF_service sf_service = new SF_service();
            //SearchResultFlights sr = sf_service.GetCurrentResultsFlights(searchId.ToString());

            //JsonArray json_tickets = new JsonArray();

            //foreach (global::SF_service.Flight fl in sr.Flights)
            //{
            //    Ticket ticket = FlightToTicket(fl);
            //    json_tickets.Add(ticket.ToJsonArray());
            //}

       /*     //делаем поиск, получаем элемент Fares
            XmlElement fares = Core.Awad_Proxy.DoSearch(query);

            //извлекаем коллекцию элементов Fare
            XmlNodeList fare_list = fares.GetElementsByTagName("Fare");

            //из каждого fare получаем набор ticket-ов
            JsonArray json_tickets = new JsonArray();

            foreach (XmlElement fare in fare_list)
            {
                Fare json_fare = new Fare(fare, query.QuerySegments.Length);

                foreach (Ticket ticket in json_fare.Tickets)
                    json_tickets.Add(ticket.ToJsonArray());
            }
        */
            
            //if (json_tickets.Count > 0)
            //{                
            //    JsonArray jArrDate = new JsonArray();
            //    jArrDate.Add(DateTime.UtcNow.Year);
            //    jArrDate.Add(DateTime.UtcNow.Month);
            //    jArrDate.Add(DateTime.UtcNow.Day);
            //    jArrDate.Add(DateTime.UtcNow.Hour);
            //    jArrDate.Add(DateTime.UtcNow.Minute);
            //    jArrDate.Add(DateTime.UtcNow.Second);

            //    Uri remoteUri = new Uri("http://in.avia.rambler.ru/clickandtravel/");

            //    WebRequest request = WebRequest.Create(remoteUri);

            //    request.Method = "POST";
            //    request.Headers.Add("Content-Encoding", "gzip");

            //    System.IO.Stream reqStream = request.GetRequestStream();

            //    GZipStream gz = new GZipStream(reqStream, CompressionMode.Compress);

            //    System.IO.StreamWriter sw = new System.IO.StreamWriter(gz, Encoding.ASCII);

            //    JsonArray pack = new JsonArray();
            //    pack.Add(jArrDate);
            //    pack.Add(inp);
            //    pack.Add(json_tickets);

            //    JsonArray result = new JsonArray();
            //    result.Add(pack);

            //    sw.Write(result.ToString());
            //    sw.Close();

            //    gz.Close();
            //    reqStream.Close();
				
            //    //System.IO.Stream respStream = ;
            //    string resp = new StreamReader(request.GetResponse().GetResponseStream()).ReadToEnd();

            //    return "success" + resp;
				
            //    return "success" + result.ToString();
            //}
            //return "tickets 0";
        }
    }
}
