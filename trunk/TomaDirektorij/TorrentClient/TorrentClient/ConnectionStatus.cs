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
        /*  am_choking: this client is choking the peer
            am_interested: this client is interested in the peer
            peer_choking: peer is choking this client
            peer_interested: peer is interested in this client 
         */

        public bool amChoking;
        public bool amInterested;

        public bool peerChoking;
        public bool peerInterested;

        public ConnectionStatus(){
            this.amChoking = true;
            this.amInterested = false;

            this.peerChoking = true;
            this.peerInterested = false;
        }
    }
}
