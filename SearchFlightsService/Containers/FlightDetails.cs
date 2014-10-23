using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class FlightDetails
    {
        private string code;

        [System.Xml.Serialization.XmlAttribute("Code")]
        public string Code
        {
            get { return code; }
            set { code = value; }
        }

        private string number;

        [System.Xml.Serialization.XmlAttribute("Number")]
        public string Number
        {
            get { return number; }
            set { number = value; }
        }

        private string title;

         [System.Xml.Serialization.XmlAttribute("Title")]
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        private string content;

        [System.Xml.Serialization.XmlText]
        public string Content
        {
            get { return content; }
            set { content = value; }
        }

        public FlightDetails()
        { }

        public FlightDetails(string code, string number, string title, string content)
        {
            this.code = code;
            this.number = number;
            this.title = title;
            this.content = content;
        }
    }
}