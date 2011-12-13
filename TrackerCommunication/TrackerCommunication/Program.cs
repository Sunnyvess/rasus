using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackerCommunication
{
    class Program
    {
        static void Main(string[] args)
        {
            Request getReq = new Request();
            getReq.SendTrackerGet();
        }
    }
}
