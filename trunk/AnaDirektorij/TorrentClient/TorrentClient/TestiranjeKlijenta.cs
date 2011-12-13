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
        //     3.- path do log datoteke koristene u skidanju ovog filea (SAMO PATH, NE i naziv datoteke, npr c:\, datoteka je hardkodirana i zove se logg.txt)
        //     4. nadalje - popis svih portova na kojima su klijenti na koje se mogu spojiti
        static void Main(string[] args)
        {
            List<Peer> peerovi = new List<Peer>();

            PWPClient client = new PWPClient(Int32.Parse(args[0]), args[1], new Torrent(args[2]), InfoExtractor.ExtractInfoValue(args[2]), args[3]);
            for(int i = 4; i < args.Length; i++)
            {
                peerovi.Add(new Peer("127.0.0.1",Int32.Parse(args[i])));
            }
                 
            client.refreshPeers(peerovi);
        }
    }
}
