using System;
using System.Collections.Generic;
using System.Web;
using System.Xml;
using Jayrock.Json;

/// <summary>
/// Summary description for Route
/// </summary>

namespace SearchFlightsService.Containers.JSON
{
    public class TicketJson
    {
        public TicketJson()
        {
        }

        public TicketJson(XmlNode[] input, int price, int fareId, int adults, int children, int infants)
        {
            this.Adults = adults;

            this.Children = children;

            this.Infants = infants;

            this.FareId = fareId;

            this.Price = price;

            this.Route = new RouteJson(input);
        }

        public TicketJson(Flight flight) ///преобразуем объект flight для отправки рамблеру
        {
            this.Price = flight.Price;
            this.Route = new RouteJson();
            this.Route.Segments = new SegmentJson[flight.Parts.Length];

            //копируем участки маршрута
            for (int i = 0; i < flight.Parts.Length; i++)
            {
                FlightPart flightPart = flight.Parts[i];
                SegmentJson segment = new SegmentJson();
                segment.Flights = new FlightJson[flightPart.Legs.Length];

                //копируем перелеты, входящие в участок
                for (int j = 0; j < flightPart.Legs.Length; j++)
                {
                    Leg leg = flightPart.Legs[j];
                    FlightJson flightJson = new FlightJson();

                    flightJson.AirlineCode = leg.Airline;
                    flightJson.ArrivalAirport = leg.LocationEnd;
                    flightJson.DepartureAirport = leg.LocationBegin;

                    flightJson.ArrivalDateTime = new int[] { leg.DateEnd.Year, leg.DateEnd.Month, leg.DateEnd.Day, leg.DateEnd.Hour, leg.DateEnd.Minute};
                    flightJson.DepartureDateTime = new int[] { leg.DateBegin.Year, leg.DateBegin.Month, leg.DateBegin.Day, leg.DateBegin.Hour, leg.DateBegin.Minute };

                    flightJson.BookingClass = leg.BookingClass;
                    flightJson.CabinClass = leg.ServiceClass;

                    flightJson.Duration = leg.Duration;
                    flightJson.FlightNumber = leg.FlightNumber;
                    flightJson.PlaneCode = leg.Board;

                    segment.Flights[j] = flightJson;
                }
                this.Route.Segments[i] = segment;
            }
        }

        public JsonArray ToJsonArray()
        {
            JsonArray jArr = new JsonArray();

            jArr.Add(this.Route.ToJsonArray());
            jArr.Add(this.Price);

            return jArr;
        }

        public int Adults = 0;
        public int Children = 0;
        public int Infants = 0;

        public RouteJson Route = null;
        public int Price = 0;
        public int FareId = 0;
    }
}