using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FairTorrent;

namespace BencoderTestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Torrent torrent = new Torrent(@"D:\Downloads\BossTest.torrent");

            Console.ReadLine();
        }
    }
}
