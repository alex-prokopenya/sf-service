using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;

/// <summary>
/// Summary description for Route
/// </summary>

namespace SearchFlightsService
{
    public class Ticket_reduced
    {
        public Ticket_reduced()
        {

        }

        public Ticket_reduced(JsonArray inp)
        {
            this.Route = new Route_reduced(inp[0] as JsonArray);
            this.Adults = Convert.ToInt32(inp[1]);
            this.Children = Convert.ToInt32(inp[2]);
            this.Infants = Convert.ToInt32(inp[3]);
            this.InfantsWithoutSeat = Convert.ToInt32(inp[4]);

            if (inp[5] != null)
                this.Price = Convert.ToInt32(inp[5]);

            foreach (Segment_reduced sr in this.Route.Segments)
            {
                //дата вылета
                string date = sr.Flights[0].DepartureDateTime[2].ToString();

                if (date.Length == 1) this.Request += "0";

                this.Request += date;

                //месяц вылета
                string month = sr.Flights[0].DepartureDateTime[1].ToString();

                if (month.Length == 1) this.Request += "0";

                this.Request += month;

                string ap_from = sr.Flights[0].DepartureAirport;

            //    if (ap_from.Length != 3) ap_from = Core.DataStore.GetIataCodeByIcao(ap_from);

                this.Request += ap_from;

                string ap_to = sr.Flights[sr.Flights.Length - 1].ArrivalAirport;

             //   if (ap_to.Length != 3) ap_to = Core.DataStore.GetIataCodeByIcao(ap_to);

                this.Request += ap_to;
            }

            this.Request += "&AD=" + this.Adults + "&CN=" + (this.Children + this.Infants);

            //класс перелета
            if ((this.Route.Segments[0].Flights[0].CabinClass == "E") || (this.Route.Segments[0].Flights[0].CabinClass == "B"))
                this.Request += "&SC=" + this.Route.Segments[0].Flights[0].CabinClass;
            else
                this.Request += "&SC=A";
        }

        public JsonArray ToJsonArray()
        {
            JsonArray jArr = new JsonArray();

            jArr.Add(this.Route.ToJsonArray());
            jArr.Add(this.Adults);
            jArr.Add(this.Children);
            jArr.Add(this.Infants);
            jArr.Add(this.InfantsWithoutSeat);
            jArr.Add(this.Price);

            return jArr;
        }

    /*    public int CompareWithTicket(Ticket ticket)
        {
            //сравниваем количество сегментов
            if (this.Route.Segments.Length != ticket.Route.Segments.Length) return 1;

            for (int i = 0; i < this.Route.Segments.Length; i++)
            {
                if (this.Route.Segments[i].Flights.Length != ticket.Route.Segments[i].Flights.Length)
                    return 2;

                for (int j = 0; j < this.Route.Segments[i].Flights.Length; j++)
                {
                    Flight flight = ticket.Route.Segments[i].Flights[j];
                    Flight_reduced flight_this = this.Route.Segments[i].Flights[j];

                    if (flight_this.DepartureAirport != flight.DepartureAirport) return 3;

                    if (flight_this.ArrivalAirport != flight.ArrivalAirport) return 4;

                    if ((flight_this.DepartureDateTime[4] != flight.DepartureDateTime[4])
                        || (flight_this.DepartureDateTime[3] != flight.DepartureDateTime[3])
                        || (flight_this.DepartureDateTime[2] != flight.DepartureDateTime[2])
                        || (flight_this.DepartureDateTime[1] != flight.DepartureDateTime[1])
                        || (flight_this.DepartureDateTime[0] != flight.DepartureDateTime[0]))
                        return 5;

                    if ((flight_this.ArrivalDateTime[4] != flight.ArrivalDateTime[4])
                        || (flight_this.ArrivalDateTime[3] != flight.ArrivalDateTime[3])
                        || (flight_this.ArrivalDateTime[2] != flight.ArrivalDateTime[2])
                        || (flight_this.ArrivalDateTime[1] != flight.ArrivalDateTime[1])
                        || (flight_this.ArrivalDateTime[0] != flight.ArrivalDateTime[0]))
                        return 6;

                    if (flight_this.AirlineCode != flight.AirlineCode) return 7;

                    if (flight_this.BookingClass != flight.BookingClass) return 8;

                    if (flight_this.CabinClass != flight.CabinClass) return 9;

                    if (flight_this.FlightNumber != flight.FlightNumber) return 10;
                }
            }
            return 0;
        }
        */
        public Route_reduced Route = null;
        public int Adults = 0;
        public int Children = 0;
        public int Infants = 0;
        public int InfantsWithoutSeat = 0;
        public int Price = 0;

        public string Request = "";
    }
}