using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SearchFlightsService.Core
{
        public class RamblerAviaException : Exception
        {

            public RamblerAviaException()
            { }

            public RamblerAviaException(string Message)
                : base(Message)
            { }

            public RamblerAviaException(string Message, int Code)
                : base("" + Code + "~" + Message)
            {
                this.Code = Code;
            }

            public int Code;
        }
}