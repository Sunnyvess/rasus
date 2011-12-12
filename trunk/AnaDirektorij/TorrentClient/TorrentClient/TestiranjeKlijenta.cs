using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FairTorrent;
using FairTorrent.BEncoder;

namespace TorrentClient
{
    
    class TestiranjeKlijenta
    {
        //parametri : 
        //     0.- port na kojem prihvacam konekcije
        //     1.- ime ovog klijenta (20 znakova)
        //     2.- lokacija torrent fajla
        //     3. nadalje - popis svih portova na kojima su klijenti na koje se mogu spojiti
        static void Main(string[] args)
        {
            List<Peer> peerovi = new List<Peer>();
            
            Status[] jelImaFile;
            if(Int32.Parse(args[3]) == 1 ){
                jelImaFile = new Status[]{Status.Ima};
            }else{
                jelImaFile = new Status[] { Status.Nema};
            }

            PWPClient client = new PWPClient(Int32.Parse(args[0]), args[1],new Torrent(args[2]), InfoExtractor.ExtractInfoValue(args[2]), jelImaFile);
            for(int i = 4; i < args.Length; i++)
            {
                peerovi.Add(new Peer("127.0.0.1",Int32.Parse(args[i])));
            }
                 
            client.refreshPeers(peerovi);
        }
    }
}
