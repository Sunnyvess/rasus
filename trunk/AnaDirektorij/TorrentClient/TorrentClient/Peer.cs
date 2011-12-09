using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TorrentClient
{
    //information holder
    public class Peer
    {
        private string ipAdress;
        private int port;
        private string clientName;

        public Peer(string newIpAdress, int newPort)
        {
            this.ipAdress = newIpAdress;
            this.port = newPort;
        }

        public string IpAdress
        {
            get
            {
                return this.ipAdress;
            }
        }

        public int Port
        {
            get
            {
                return this.port;
            }
        }

        public string PeerName
        {
            get
            {
                return this.PeerName;
            }
            set
            {
                this.PeerName = value;
            }
        }
    }
}
