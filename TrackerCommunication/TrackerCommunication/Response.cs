using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackerCommunication
{
    class Response
    {

        public int interval
        {
            get;
            set;
        }

        public Peers peers
        {
            get;
            set;
        }
    }
}
