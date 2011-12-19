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
            Req TC = new Req();
            TC.StartTrackerCommunication();
            Console.ReadKey();
        }
    }
}
