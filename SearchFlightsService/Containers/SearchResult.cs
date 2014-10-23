using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Containers
{
    public class SearchResult
    {
        #region //fields
        private int isFinished;

        public int IsFinished
        {
            get { return isFinished; }
            set { isFinished = value; }
        }

        private string searchtId;

        public string SearchId
        {
            get { return searchtId; }
            set { searchtId = value; }
        }

        private Fare[] fares;

        public Fare[] Fares
        {
            get { return fares; }
            set { fares = value; }
        }



        #endregion

        #region //constructor
        public SearchResult()
        { }

        public SearchResult(string requestId, Fare[] fares, int isFinished)
        {
            this.searchtId = requestId;
            this.fares = fares;
            this.isFinished = isFinished;
        }
        #endregion
    }
}