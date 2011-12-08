using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FairTorrent;
using FairTorrent.BEncoder;

namespace TorrentClient
{
    class KlijentPrvi
    {
        static void Main(string[] args)
        {
            List<Peer> peerovi = new List<Peer>();
            PWPClient client = new PWPClient(Int32.Parse(args[0]), args[1],new Torrent(args[2]), InfoExtractor.ExtractInfoValue(args[2]));
            for(int i = 3; i < args.Length; i++)
            {
                peerovi.Add(new Peer("127.0.0.1",Int32.Parse(args[i])));
            }
                 
            client.refreshPeers(peerovi);
        }
    }
}
