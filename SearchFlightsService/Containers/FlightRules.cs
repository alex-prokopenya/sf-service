using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class FlightRules
    {
        //разрешен ли возврат до вылета
        private bool _allowedReturnBefore;

        public bool AllowedReturnBefore
        {
            get { return _allowedReturnBefore; }
            set { _allowedReturnBefore = value; }
        }

        //разрешен ли возврат после вылета
        private bool _allowedReturnAfter;

        public bool AllowedReturnAfter
        {
            get { return _allowedReturnAfter; }
            set { _allowedReturnAfter = value; }
        }

        //разрешен ли обмен до вылета
        private bool _allowedChangesBefore;

        public bool AllowedChangesBefore
        {
            get { return _allowedChangesBefore; }
            set { _allowedChangesBefore = value; }
        }

        //разрешен ли обмен после вылета
        private bool _allowedChangesAfter;

        public bool AllowedChangesAfter
        {
            get { return _allowedChangesAfter; }
            set { _allowedChangesAfter = value; }
        }

        //текст правил тарифа
        private string _rulesText;

        public string RulesText
        {
            get { return _rulesText; }
            set { _rulesText = value; }
        }
    }
}