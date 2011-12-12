using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Threading;
using System.IO;
using System.Web;
using System.Diagnostics;
using FairTorrent.BEncoder;
using MessageCommunication;

namespace TorrentClient
{
    public class PWPConnection
    {

        public PWPClient localClient; //ja
        public TcpClient peerClient;  // onaj drugi

        public string peerName; // ime onog s druge strane veze

        public PWPConnection(PWPClient newLocalClient)
        {
            this.localClient = newLocalClient;
        }

        //probaj se spojiti na peera
        public void ConnectToPeer(object newPeer)
        {
            Peer peer = (Peer)newPeer;
            TcpClient client = new TcpClient();

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(peer.IpAdress), peer.Port);

            client.Connect(serverEndPoint);
            peerClient = client;

            Console.WriteLine("Uspostavljena veza prema peeru na portu " + peer.Port);

            Handshaker handshaker = new Handshaker (this);

            //zapocni komunikaciju s peerom - pošalji handshake, primi handshake i provjeri jel sve štima
            handshaker.InitiateComunication(client);
        }

        
        

        //slanje i primanje poruka - dok nije cijeli file prenesen ? (ko na kraju prekida vezu?)
        public void HandleClientComm(TcpClient tcpClient)
        {
            NetworkStream clientStream = tcpClient.GetStream();

            MessageListener messageListener = new MessageListener(this);
            Thread messageListenerThread = new Thread(new ParameterizedThreadStart(messageListener.Listen));
            messageListenerThread.Start(clientStream);

            Console.WriteLine("Radimo nešto korisno zajedno!");

            try
            {
          //      sendMessage(clientStream, new byte[] { 0, 0, 0, 1, 3 });
                //     sendMessage(clientStream, new byte[] { 0, 0, 0, 1, 1 });
           //     sendMessage(clientStream, new byte[] { 0, 0, 0, 13, 6, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 2, 0, 64, 0, 0 });
            }
            catch (IOException e)
            {
                closeConnection("Tokom izmjene poruka, peer je prekinuo vezu.");
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
           while (true)
            {

                if(stopWatch.ElapsedMilliseconds > 10000000){
                //ako dulje od 10 sec nije poslana nikakva poruka pošalji keepalive
                    stopWatch.Stop();
                    sendMessage(clientStream, new byte[]{0,0,0,0});
                }
                //provjeri stanje zastavica :)

                //ako je za napraviti nešto pametno - poslat zahtjev npr, pošalji ga :D    
            }

            messageListenerThread.Join();
            closeConnection("Redovan kraj rada");
        }


        //posalji peeru poruku
        private void sendMessage(NetworkStream stream, byte[] message){
            
            stream.Write(message, 0, message.Length);
            stream.Flush();
            
        }

        //zatvaranje konekcije prema peeru - smanjenje broja konekcija, micanje peera iz aktivnih peerova
        public void closeConnection(string desc)
        {
            lock(localClient.lockerBrojaKonekcija){
                localClient.numConnections--;
            }
            
            //ovo mozda ne radi zbog usporedbe stringova
            localClient.connectedPeerNames.Remove(this.peerName);

            Console.WriteLine("Konekcija se zatvara!: "+desc);
            peerClient.Close();
        }

    }
}