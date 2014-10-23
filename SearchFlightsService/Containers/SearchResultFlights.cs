using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class SearchResultFlights
    {
        #region //fields
        private int isFinished;

        public int IsFinished
        {
            get { return isFinished; }
            set { isFinished = value; }
        }

        private string requestId;

        public string RequestId
        {
            get { return requestId; }
            set { requestId = value; }
        }

        private Flight[] flights;

        public Flight[] Flights
        {
            get { 
                return flights; 
            }
            set {

                if (value.Length > 10000)
                { 
                    Flight[] new_arr = new Flight[10000];
                    Array.Copy(value,new_arr, 10000);
                    flights = new_arr;
                }
                else
                    flights = value; 
            }
        }

        private int searchId;

        public int SearchId
        {
            get { return searchId; }
            set { searchId = value; }
        }


        #endregion

        #region //constructor
        public SearchResultFlights()
        { }

        public SearchResultFlights(string requestId, Flight[] _flights, int isFinished)
        {
            this.requestId = requestId;

            if (_flights.Length > 10000)
            {
                Flight[] new_arr = new Flight[10000];
                Array.Copy(_flights, new_arr, 10000);
                this.flights = new_arr;
            }
            else
                this.flights = _flights;

            this.isFinished = isFinished;
        }

        public SearchResultFlights(string _requestId, Flight[] _flights, int _isFinished, int _searchId)
        {
            this.requestId = _requestId;

            if (_flights.Length > 10000)
            {
                Flight[] new_arr = new Flight[10000];
                Array.Copy(_flights, new_arr, 10000);
                this.flights = new_arr;
            }
            else
                this.flights = _flights;

            this.isFinished = _isFinished;
            this.searchId = _searchId;
        }
        #endregion
    }
}