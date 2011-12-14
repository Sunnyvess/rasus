using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace FairTorrent
{
    public class FailureInfo
    {
        public string failureReason { get; set; }
    }

    public class Response
    {
        public string interval { get; set; }
        public string complete { get; set; }
        public string incomplete { get; set; }
        public string min_interval { get; set; }
        public byte[] Peers { get; set; }

        public FailureInfo Failure { get; set; }

        public Response()
        {
        }

        public Response(byte[] bencodedData)
        {
            DecodeResponse(bencodedData);
        }

        public void DecodeResponse(byte[] bencodedData)
        {
            Dictionary<string, object> dict = BEncoder.BEncoder.Decode(bencodedData);

            this.interval = (string)dict["interval"];
            this.complete = (string)dict["complete"];
            this.incomplete = (string)dict["incomplete"];
            this.min_interval = (string)dict["min_interval"];
            this.Peers = (byte[])dict["peers"];
         

        }

    }
}
