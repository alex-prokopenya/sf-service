using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class Passenger
    {
        private string name = "";

        public string Name
        {
            get { return name; }
            set
            {
                if (value.Trim().Length == 0)
                    throw new SearchFlightException("Invalid passenger's name");

                name = value;
            }
        }
        private string fname = "";

        public string Fname
        {
            get { return fname; }
            set
            {
                if (value.Trim().Length == 0)
                    throw new SearchFlightException("Invalid passenger's fname");

                fname = value;
            }
        }

        private string otc = "";
        public string Otc
        {
            get { return otc; }
            set
            {
                otc = value;
            }
        }
        private string citizen = "";

        public string Citizen
        {
            get { return citizen; }
            set
            {
                if (value.Trim().Length < 2)
                    throw new SearchFlightException("Invalid passenger's citizenship");

                citizen = value;
            }
        }
        private DateTime birth = DateTime.MinValue;

        public DateTime Birth
        {
            get { return birth; }
            set
            {

                if (value > DateTime.Today)
                    throw new SearchFlightException("Invalid passenger's birthdate");

                birth = value;
            }
        }

        private string gender = "";

        public string Gender
        {
            get { return gender; }
            set
            {

                if ((value != "F") && (value != "M"))
                    throw new SearchFlightException("Invalid passenger's gender");

                gender = value;
            }
        }
        private string pasport = "";

        public string Pasport
        {
            get { return pasport; }
            set
            {
                if (value.Trim().Length < 2)
                    throw new SearchFlightException("Invalid passenger's pasport");

                pasport = value;
            }
        }
        private DateTime passport_expire_date = DateTime.MinValue;

        public DateTime Passport_expire_date
        {
            get { return passport_expire_date; }
            set
            {
                if (value < DateTime.Today)
                    throw new SearchFlightException("Invalid passenger's passport_expire_date");

                passport_expire_date = value;
            }
        }

        private string frequentFlyerAirline;

        public string FrequentFlyerAirline
        {
            get { return frequentFlyerAirline; }
            set { frequentFlyerAirline = value; }
        }

        private string frequentFlyerNumber;

        public string FrequentFlyerNumber
        {
            get { return frequentFlyerNumber; }
            set { frequentFlyerNumber = value; }
        }

        public Passenger()
        { }

        public Passenger(string name,
                           string fname,
                            string otc,
                           string citizenship,
                           DateTime birth,
                           string gender,
                           string pasport,
                           DateTime passport_expire_date,
                           string frequentFlyerNumber,
                           string frequentFlyerAirline)
        {
            this.name = name;
            this.fname = fname;
            this.Otc = otc;
            this.citizen = citizenship;
            this.birth = birth;
            this.gender = gender;
            this.pasport = pasport;
            this.passport_expire_date = passport_expire_date;
            this.frequentFlyerAirline = frequentFlyerAirline;
            this.frequentFlyerNumber = frequentFlyerNumber;
        }
    }
}