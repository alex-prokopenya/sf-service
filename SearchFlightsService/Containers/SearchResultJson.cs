using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Web;
using Jayrock.Json;

namespace SearchFlightsService.Containers
{
    public class SearchResultJson
    {
        private SortedSet<int> _changes = new SortedSet<int>();

        private void AddChanges(int ch)
        {
            _changes.Add(ch);
        }

        private string changes;

        private void ChangesToString()
        {
            JsonArray jArr = new JsonArray();

            foreach (int ch in _changes)
                jArr.Add(ch);

            this.changes = jArr.ToString();
        }

        public string Changes
        {
            set { }
            get {
                return changes;
            }
        }

        private Dictionary<string, string>[] _airports;

        private void AddAirport(int item, string city, string code, string name)
        {
            int current_length = _airports.Length;
            
            if (item+1 > current_length)
            {
                Dictionary<string, string>[] new_airports = new Dictionary<string, string>[item+1];

                for (int i = 0; i < current_length; i++)
                {
                    if (_airports[i] != null)
                        new_airports[i] = _airports[i];
                    else
                        new_airports[i] = new Dictionary<string, string>();
                }

                new_airports[item] = new Dictionary<string, string>();
                this._airports = new_airports;
            }
            if (!this._airports[item].Keys.Contains("city"))
                this._airports[item].Add("city", city);

            if (!this._airports[item].Keys.Contains(code))
                this._airports[item].Add(code, name);
        }

        private string airports;

        private void AirportsToString()
        {
            JsonArray jArr = new JsonArray();

            for (int i = 0; i < this._airports.Length; i++)
            {
                JsonObject city_arr = new JsonObject();

                Dictionary<string, string> city_dict = this._airports[i];

                ArrayList cdKeys = new ArrayList(city_dict.Keys.ToArray<string>());
                cdKeys.Sort(new AlKeysComparer(city_dict));

                if (city_dict != null)
                    foreach (string key in cdKeys)
                        city_arr.Add(key, city_dict[key]);

                jArr.Add(city_arr);
            }

            this.airports = jArr.ToString(); 
        }

        public string Airports
        {
            set { }
            get {
                return this.airports;
            }
        }


        private Fare[] _fares;

        private string fares;

        public void setFares(Fare[] fares, decimal course)
        {
            this._changes = new SortedSet<int>();
            this._airports = new Dictionary<string, string>[0];
            this._airlines = new Dictionary<string, string>();

            this._fares = fares;

            JsonArray arrFares = new JsonArray();

            foreach (Fare fr in fares)
            {
                JsonObject objFare = new JsonObject();
                objFare.Add("id", fr.Id);
                objFare.Add("price",  Math.Ceiling(fr.Price / course));

                objFare.Add("ac_name", fr.Airline);
                objFare.Add("ac_code", fr.AirlineCode);
                objFare.Add("ap_from", fr.Directions[0].Variants[0].Legs[0].LocationBegin);
                objFare.Add("ap_to", fr.Directions[0].Variants[0].Legs[fr.Directions[0].Variants[0].Legs.Length - 1].LocationEnd);

                this.AddAirline(fr.AirlineCode, fr.Airline);

                JsonArray arrDirections = new JsonArray();
                for (int i = 0; i < fr.Directions.Length; i++)
                {
                    Direction dir = fr.Directions[i];

                    JsonArray arrVars = new JsonArray();
                    for (int j = 0; j < dir.Variants.Length; j++)
                    {
                        Variant vrnt = dir.Variants[j];
                        JsonObject objVar = new JsonObject();
                        objVar.Add("vid", vrnt.Id);
                        objVar.Add("TT", vrnt.FlightTime);

                        //changes
                        this.AddChanges(vrnt.Legs.Length);
                        //addAirports_from
                        this.AddAirport(i, vrnt.Legs[0].LocationBeginName, vrnt.Legs[0].LocationBegin, vrnt.Legs[0].AirportBeginName);
                        //addAirports_return
                        Leg last_leg = vrnt.Legs[vrnt.Legs.Length - 1];
                        this.AddAirport(i+1, last_leg.LocationEndName, last_leg.LocationEnd, last_leg.AirportEndName);

                        JsonArray arrLegs = new JsonArray();
                        for (int k = 0; k < vrnt.Legs.Length; k++)
                        {
                            Leg leg = vrnt.Legs[k];

                            JsonObject objLeg = new JsonObject();

                            objLeg.Add("leg_time", leg.Duration);
                            objLeg.Add("car", leg.Airline);
                            objLeg.Add("plane_code", leg.Board);
                            objLeg.Add("plane_name", leg.Board);
                            objLeg.Add("from", leg.LocationBegin);
                            objLeg.Add("from_city", leg.LocationBeginName);
                            objLeg.Add("time_dep", leg.DateBegin.ToString("HH:mm"));
                            objLeg.Add("to", leg.LocationEnd);
                            objLeg.Add("to_city", leg.LocationEndName);
                            objLeg.Add("time_arr", leg.DateEnd.ToString("HH:mm"));

                            arrLegs.Add(objLeg);
                        }

                        objVar.Add("Legs", arrLegs);
                        arrVars.Add(objVar);
                    }
                    arrDirections.Add(arrVars);
                }

                objFare.Add("dirs", arrDirections);
                arrFares.Add(objFare);
            }
            this.fares  = arrFares.ToString();

            this.ChangesToString();
            this.AirportsToString();
        }

        public string Fares
        {
            set { }
            get {
                return this.fares;
            }
        }


        private Dictionary<string, string> _airlines = new Dictionary<string, string>();

        public void AddAirline(string code, string name)
        {
            if((!this._airlines.ContainsKey(code))&&(name != null))
                this._airlines.Add(code, name);
        }


        private class AlKeysComparer : IComparer 
        {
            private Dictionary<string, string> _airlines = null;

            public AlKeysComparer(Dictionary<string, string> al) { this._airlines = al; }

            public int Compare(object A, object B)
            {
                string keyA = A.ToString();
                string keyB = B.ToString();

                if (this._airlines[keyA] == this._airlines[keyB]) return 0;
                if (this._airlines[keyA] == null) return -1;
                if (this._airlines[keyB] == null) return 1;

                return this._airlines[keyA].CompareTo(this._airlines[keyB]);
            }
        }

        public string Airlines
        {
            set { }
            get {
                ArrayList alKeys = new ArrayList( _airlines.Keys.ToArray<string>() );
                alKeys.Sort(new AlKeysComparer(this._airlines));

                JsonObject jObj = new JsonObject();

                foreach (string key in alKeys)
                    jObj.Add(key, this._airlines[key]);

                return jObj.ToString();
            }
        }


        private string searchId;

        public string SearchId
        {
            get { return searchId; }
            set { searchId = value; }
        }


        private int isFinished;

        public int IsFinished
        {
            get { return isFinished; }
            set { isFinished = value; }
        }
    }
}