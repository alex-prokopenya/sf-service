using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SearchFlightsService.Containers;
using SearchFlightsService.Containers.JSON;
using Jayrock.Json;
using System.Net;
using System.IO;
using System.Text;
using System.IO.Compression;


namespace SearchFlightsService.Ext
{
    public class RamblerService
    {
        public static void SendSearchToRambler(Route route, int adult, int children, int inf, string serviceClass, Flight[] flights, string path)
        {
            #region generate Query object
            //сворачиваем параметры в объект "Query"
            QueryJson query = new QueryJson();
            query.Adults = adult;
            query.Children = children;
            query.Infants = 0;
            query.InfantsWithoutSeat = inf;
            query.CabinClass = serviceClass;

            query.QuerySegments = new QuerySegmentJson[route.Segments.Length];

            for (int i = 0; i < route.Segments.Length; i++)
            {
                QuerySegmentJson qs = new QuerySegmentJson();
                qs.date = new int[3] { route.Segments[i].Date.Year, route.Segments[i].Date.Month, route.Segments[i].Date.Day};
                qs.from = route.Segments[i].LocationBegin;
                qs.to = route.Segments[i].LocationEnd;

                query.QuerySegments[i] = qs;
            }

            #endregion

            #region Convert Flights to Tickets array

            TicketJson[] tickets = new TicketJson[flights.Length];

            for (int i = 0; i < flights.Length; i++ )
            {
                Flight flight = flights[i];
                tickets[i] = new TicketJson(flight);
            }
            #endregion

            #region send request to Rambler's server
            if (tickets.Length > 0)
            {
                JsonArray jArrDate = new JsonArray();
                jArrDate.Add(DateTime.UtcNow.Year);
                jArrDate.Add(DateTime.UtcNow.Month);
                jArrDate.Add(DateTime.UtcNow.Day);
                jArrDate.Add(DateTime.UtcNow.Hour);
                jArrDate.Add(DateTime.UtcNow.Minute);
                jArrDate.Add(DateTime.UtcNow.Second);

                Uri remoteUri = new Uri("path");

                WebRequest request = WebRequest.Create(remoteUri);

                request.Headers.Add("Content-Encoding", "gzip");

                System.IO.Stream reqStream = request.GetRequestStream();

                GZipStream gz = new GZipStream(reqStream, CompressionMode.Compress);

                System.IO.StreamWriter sw = new System.IO.StreamWriter(gz, Encoding.ASCII);

                JsonArray pack = new JsonArray(); //отправляем рамблеру объект вида [date, query, tickets]
                pack.Add(jArrDate);
                pack.Add(query.ToJsonArray());
                pack.Add(tickets);

                JsonArray result = new JsonArray();
                result.Add(pack);

                sw.Write(result.ToString());
                sw.Close();

                gz.Close();
                reqStream.Close();
            }

            query = null;
            tickets = null;
            flights = null;

            GC.Collect();
            #endregion
        }
    }
}