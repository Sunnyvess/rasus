using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackerCommunication
{
    class Peer
    {
        private PeerID peerID;
        private string peerIP;
        private int peerPort; 
        private double uploaded;
        private double downloaded;
        private double left;

        public Peer()
        {
            peerID = new PeerID();
            peerIP = "192.168.1.2";
            peerPort = 6968;
        }

        public byte[] PeerID
        {
            get { return peerID.ID; }
        }

        public string PeerIP
        {
            get { return peerIP; }
        }

        public int PeerPort
        {
            get { return peerPort; }
        }

        public double Uploaded
        {
            get { return uploaded; }
            set { uploaded = value; }
        }

        public double Downloaded
        {
            get { return downloaded; }
            set { downloaded = value; }
        }

        public double Left
        {
            get { return left; }
            set { left = value; }
        }
    }
}
