using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Jayrock.Json;

namespace SearchFlightsService.Containers
{
    public class Leg
    {
        /*
            •	serviceClass – класс обслуживания (эконом, бизнес, первый);
            •	bookingClass – код класса бронирования;
            •	airline – перевозчик;
            •	flightNumber – номер рейса;
            •	locationBegin –пункт вылета;
            •	locationEnd –пункт прилёта;
            •	dateBegin – дата вылета;
            •	dateEnd – дата прилёта;
            •	board – воздушное судно;
            •	remarksSearchContext – контекст для получения подробной информации о тарифе;
            •	travelDuration – продолжительность перелёта в минутах, если <=0, то значение неопределено.
        */

        #region fields
        private string serviceClass;

        public string ServiceClass
        {
            get { return serviceClass; }
            set { serviceClass = value.Replace("\"", "'"); }
        }

        private string bookingClass;

        public string BookingClass
        {
            get { return bookingClass; }
            set { bookingClass = value.Replace("\"", "'"); }
        }

        private string airline;

        public string Airline
        {
            get { return airline; }
            set { airline = value.Replace("\"", "'"); }
        }

        private string flightNumber;

        public string FlightNumber
        {
            get { return flightNumber; }
            set { flightNumber = value.Replace("\"", "'"); }
        }

        private string airportBeginName;

        public string AirportBeginName
        {
            get { return airportBeginName; }
            set { airportBeginName = value.Replace("\"", "'"); }
        }

        private string airportEndName;

        public string AirportEndName
        {
            get { return airportEndName; }
            set { airportEndName = value.Replace("\"", "'"); }
        }

        private string locationBeginName;

        public string LocationBeginName
        {
            get { return locationBeginName; }
            set { locationBeginName = value.Replace("\"", "'"); }
        }

        private string locationBegin;

        public string LocationBegin
        {
            get { return locationBegin; }
            set { locationBegin = value.Replace("\"", "'"); }
        }

        private string locationEndName;

        public string LocationEndName
        {
            get { return locationEndName; }
            set { locationEndName = value.Replace("\"", "'"); }
        }

        private string locationEnd;

        public string LocationEnd
        {
            get { return locationEnd; }
            set { locationEnd = value.Replace("\"", "'"); }
        }

        private DateTime dateBegin;

        public DateTime DateBegin
        {
            get { return dateBegin; }
            set { dateBegin = value; }
        }

        private DateTime dateEnd;

        public DateTime DateEnd
        {
            get { return dateEnd; }
            set { dateEnd = value; }
        }

        private string board;

        public string Board
        {
            get { return board; }
            set { board = value.Replace("\"", "'"); }
        }

        private string boardName;

        public string BoardName
        {
            get { return boardName; }
            set { boardName = value.Replace("\"", "'"); }
        }

        private string remarksSearchContext;

        public string RemarksSearchContext
        {
            get { return remarksSearchContext; }
            set { remarksSearchContext = value.Replace("\"", "'"); }
        }

        private int duration;

        public int Duration
        {
            get { return duration; }
            set { duration = value; }
        }
        #endregion

        #region construct
        public Leg() { }

        public Leg( string serviceClass,
                    string bookingClass,
                    string airline,
                    string flightNumber,
                    string locationBegin,
                    string locationBeginName,
                    string locationEnd,
                    string locationEndName,
                    string airportBeginName,
                    string airportEndName,
                    DateTime dateBegin,
                    DateTime dateEnd,
                    string board,
                    string boardName,
                    string remarksSearchContext,
                    int duration)
        {
            this.serviceClass = serviceClass.Replace("\"", "'");
            this.bookingClass = bookingClass.Replace("\"", "'");
            this.airline = airline.Replace("\"", "'");
            this.flightNumber = flightNumber.Replace("\"", "'");
            this.locationBegin = locationBegin.Replace("\"", "'");
            this.locationBeginName = locationBeginName.Replace("\"", "'");
            this.locationEnd = locationEnd.Replace("\"", "'");
            this.locationEndName = locationEndName.Replace("\"", "'");

            this.airportBeginName = airportBeginName.Replace("\"", "'");
            this.airportEndName = airportEndName.Replace("\"", "'");

            this.dateBegin = dateBegin;
            this.dateEnd = dateEnd;
            this.board = board.Replace("\"", "'");
            this.boardName = boardName.Replace("\"", "'");
            this.remarksSearchContext = remarksSearchContext.Replace("\"", "'");
            this.duration = duration;
        }

        public Leg(JsonObject refl) 
        {
            this.airline = refl["a"] == null ? null : refl["a"].ToString();
            this.airportBeginName = refl["bn"] == null ? null : refl["bn"].ToString();
            this.airportEndName = refl["en"] == null ? null : refl["en"].ToString();
            this.board = refl["b"].ToString();
            this.boardName = refl["n"].ToString();
            this.bookingClass = refl["c"].ToString();
            this.dateBegin = refl["db"] == null ? DateTime.MinValue : Convert.ToDateTime(refl["db"]);
            this.dateEnd = refl["de"] == null ? DateTime.MinValue : Convert.ToDateTime(refl["de"]);
            this.duration = refl["d"] == null ? 0 : Convert.ToInt32(refl["d"]);

            this.flightNumber = refl["f"] == null ? null : refl["f"].ToString();
            this.locationBegin = refl["cb"] == null ? null : refl["cb"].ToString();
            this.locationBeginName = refl["nb"] == null ? null : refl["nb"].ToString();
            this.locationEnd = refl["ce"] == null ? null : refl["ce"].ToString();
            this.locationEndName = refl["cn"] == null ? null : refl["cn"].ToString();

            this.remarksSearchContext = refl["i"] == null? null: refl["i"].ToString();
            this.serviceClass = refl["s"] == null ? null : refl["s"].ToString();
        }
        #endregion

        #region convert
        public JsonObject ToJson()
        {
            JsonObject reflection = new JsonObject();

            reflection.Add("a", this.airline);
            reflection.Add("bn", this.airportBeginName);
            reflection.Add("en", this.airportEndName);
            reflection.Add("b", this.board);
            reflection.Add("n", this.boardName);
            reflection.Add("c", this.bookingClass);
            reflection.Add("db", this.dateBegin.ToString("yyyy-MM-dd HH:mm"));
            reflection.Add("de", this.dateEnd.ToString("yyyy-MM-dd HH:mm"));
            reflection.Add("d",  this.duration);
            reflection.Add("f", this.flightNumber);
            reflection.Add("cb", this.locationBegin);
            reflection.Add("nb", this.locationBeginName);
            reflection.Add("ce", this.locationEnd);
            reflection.Add("cn", this.locationEndName);

            reflection.Add("i", this.remarksSearchContext);
            reflection.Add("s", this.serviceClass);

            return reflection;
        }
        #endregion
    }
}