using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text.RegularExpressions;

namespace SearchFlightsService.Containers
{
    public class Customer
    {
        private string name = "";

        public string Name
        {
            get { return name; }

            set
            {
                if (value.Trim().Length == 0)
                    throw new SearchFlightException("Invalid customer's name");

                name = value; 
            }
        }
        private string mail = "";

        public string Mail
        {
            get { return mail; }
            set {
                string pattern = "^([0-9a-zA-Z]([-\\.\\w]*[0-9a-zA-Z])*@([0-9a-zA-Z][-\\w]*[0-9a-zA-Z]\\.)+[a-zA-Z]{2,9})$";

                if (!Regex.IsMatch(value, pattern)) throw new SearchFlightException("Invalid customer's e-mail");
                mail = value; }
        }
        private string phone = "";

        public string Phone
        {
            get { return phone; }
            set
            {
                if (value.Trim().Length < 5)
                    throw new SearchFlightException("Invalid customer's phone");
    
                phone = value; 
            }
        }

        public Customer() { }

        public Customer(string name, string mail, string phone)
        {
            this.name = name;
            this.mail = mail;
            this.phone = phone;
        }
    }
}