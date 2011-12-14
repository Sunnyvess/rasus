using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackerCommunication
{
    class Response
    {
        private string failureReason;
        private double interval;
        //public int complete; neobavezno
        //public int incomplete; neobavezno
        //public Peers peers; neobavezno
        //public string warning_message; neobavezno
        //public double min_interval; neobavezno
        //public byte[] trackerid; neobavezno

        public string FailureReason
        {
            get { return failureReason; }
            set { failureReason = value; }
        }

        public double Interval
        {
            get { return interval; }
            set { interval = value; }
        }
    }
}
