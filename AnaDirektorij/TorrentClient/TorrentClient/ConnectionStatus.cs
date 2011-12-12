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
        public bool choked;
        public bool interested;

        public ConnectionStatus(){
            this.choked = true;
            this.interested = false;
        }
    }
}
