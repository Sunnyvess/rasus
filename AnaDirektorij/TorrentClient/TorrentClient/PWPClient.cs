﻿using System;
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
     
        public Torrent torrentMetaInfo;
        public byte[] infoBytes;
        public string clientName;

        private TcpListener tcpListener;
        private Thread listenThread;

        private int localPort;

        private const int maxConnections = 1;
        public int numConnections;

        public object lockerBrojaKonekcija = new Object();

        private List<Peer> peers = new List<Peer>();
        public List<string> connectedPeerNames = new List<string>();

        public Status[] pieceStatus;
        public object lockerStatusaDjelova = new Object();

        public PWPClient(int port, string name, Torrent metaInfo, byte[] infoBytes, Status[] imaFajlove)
        {
            this.localPort = port;
            this.clientName = name;
            this.torrentMetaInfo = metaInfo;
            this.infoBytes = infoBytes;
            this.numConnections = 0;

            this.pieceStatus = new Status[this.torrentMetaInfo.Info.Pieces.Length/20];
            this.pieceStatus = imaFajlove;

            this.tcpListener = new TcpListener(IPAddress.Any, localPort);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        //javno dostupna metoda kroz koju se Clientu dojavljuju trenutno dostupni peerovi
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

        //metoda koja prihvaća zahtjeve za kojekcijama od peerova
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
                    Handshaker handshaker = new Handshaker (pwpConnection);
                    Thread clientThread = new Thread(new ParameterizedThreadStart(handshaker.ListenForHandshake));
                    clientThread.Start(client);
                }
            }
        }       
    }

    //izmislit bolja imena oznaka !!!!
    public enum Status{
        Nema,
        Skidanje,
        Ima
    }
}
