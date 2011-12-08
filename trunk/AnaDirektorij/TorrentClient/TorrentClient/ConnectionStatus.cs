using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TorrentClient
{
    //information holder - zastavice ! uzimam tominu implementaciju kad bude gotova
    class ConnectionStatus
    {
        bool choked;
        bool interested;
        bool[] peerHasParts;
    }
}
