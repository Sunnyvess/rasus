using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TorrentClient
{
    //information holder - zastavice ! uzimam tominu implementaciju kad bude gotova
    //pamćenje stanja veze
    public class ConnectionStatus
    {
        public bool localChoked;
        public bool localInterested;

        public bool peerChoked;
        public bool peerInterested;

        public ConnectionStatus(){
            this.localChoked = true;
            this.localInterested = false;

            this.peerChoked = true;
            this.peerInterested = false;
        }
    }
}
