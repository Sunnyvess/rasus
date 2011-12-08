using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Security.Cryptography;
using FairTorrent;

namespace TorrentClient
{
    public class PWPClient
    {
     
        public string metaInfo;
        public string peerId;

        private TcpListener tcpListener;
        private Thread listenThread;

        private int localPort;
        private const int maxConnections = 1;
        public int numConnections;

        public object lockerBrojaKonekcija = new Object();

        private List<Peer> peers = new List<Peer>();
        public List<string> connectedPeerNames = new List<string>();

        public PWPClient(int port, string peerId, string metaInfo)
        {
            this.localPort = port;
            this.metaInfo = metaInfo;
            this.peerId = peerId;
            this.numConnections = 0;

            this.tcpListener = new TcpListener(IPAddress.Any, localPort);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        public void refreshPeers( List<Peer> currentPeers ){

            List<Peer> newPeers = new List<Peer>();
            foreach(Peer peer in currentPeers)
            {
                if(!this.peers.Contains(peer))
                {   
                    newPeers.Add(peer);
                }
            }

            this.peers.AddRange(peers);

            foreach (Peer peer in newPeers)
            {
                lock (lockerBrojaKonekcija)
                {
                    if(numConnections == maxConnections){
                        break;
                    }
                }

                PWPConnection pwpConnection = new PWPConnection(this);

                Thread peerConection = new Thread(new ParameterizedThreadStart(pwpConnection.ConnectToPeer));

                lock (lockerBrojaKonekcija)
                {
                    numConnections++;
                }

                peerConection.Start(peer);

            }         
        }

        private void ListenForClients()
        {
            Console.WriteLine("Klijent sluša na portu " + this.localPort);
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();

                int ConnNum;
                lock (lockerBrojaKonekcija)
                {
                    ConnNum = numConnections;
                }
                if (ConnNum == maxConnections)
                {
                    Console.WriteLine("Odbijeno prihvacanje veze, konekcija: " + ConnNum);
                    client.Close();
                }
                else
                {
                    lock (lockerBrojaKonekcija)
                    {
                        numConnections++;
                    }

                    Console.WriteLine("Prihvaćena veza od nekog peera :)");
                    //create a thread to handle communication
                    //with connected client

                    PWPConnection pwpConnection = new PWPConnection(this);
                    Thread clientThread = new Thread(new ParameterizedThreadStart(pwpConnection.ListenForHandshake));
                    clientThread.Start(client);
                }
            }
        }

         
    }

}
