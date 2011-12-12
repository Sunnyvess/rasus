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
        public NetworkStream clientStream;

        public string peerName; // ime onog s druge strane veze

        //zastavice - stanje veze
        public ConnectionStatus connectionState;

        //dosada dohvaćeni djelovi piecea
        public byte[] PieceData;
        public byte[] HaveBytesInPiece;

        public object lockerDohvacenihPodataka = new Object();

        //TODO!!!!podaci o zahtjevanim piecovima, stalno se nadodaju - ako dođe choke : lista se briše :D
        public PieceSender pieceSender;

        public object lockerPrimljenihZahtjeva = new Object();

        //koje piecove imam ja a koje peer
        public Status[] localPiecesStatus;
        public Status[] peerPiecesStatus;

        public PWPConnection(PWPClient newLocalClient)
        {
            this.localClient = newLocalClient;
            this.connectionState = new ConnectionStatus();

            PieceData = new byte[newLocalClient.torrentMetaInfo.Info.PieceLength];
            HaveBytesInPiece = new byte[newLocalClient.torrentMetaInfo.Info.PieceLength];
            HaveBytesInPiece.Initialize();

            localPiecesStatus = new Status[newLocalClient.pieceStatus.Length];
            peerPiecesStatus = new Status[newLocalClient.pieceStatus.Length];
            peerPiecesStatus.Initialize();
            newLocalClient.pieceStatus.CopyTo(localPiecesStatus, 0);
        }

        //probaj se spojiti na peera
        public void ConnectToPeer(object newPeer)
        {
            Peer peer = (Peer)newPeer;
            TcpClient client = new TcpClient();

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(peer.IpAdress), peer.Port);

            try{
            client.Connect(serverEndPoint);
            peerClient = client;
            }catch{}

            Console.WriteLine("Uspostavljena veza prema peeru na portu " + peer.Port);

            Handshaker handshaker = new Handshaker (this);

            //zapocni komunikaciju s peerom - pošalji handshake, primi handshake i provjeri jel sve štima
            handshaker.InitiateComunication(client);
        }

        
        

        //slanje i primanje poruka - dok nije cijeli file prenesen ? (ko na kraju prekida vezu?)
        public void HandleClientComm(TcpClient tcpClient)
        {
            this.clientStream = tcpClient.GetStream();

            MessageListener messageListener = new MessageListener(this);
            Thread messageListenerThread = new Thread(new ParameterizedThreadStart(messageListener.Listen));
            messageListenerThread.Start(clientStream);

            Console.WriteLine("Radimo nešto korisno zajedno!");

            try
            {
                Random rand = new Random(231234);

                Thread.Sleep(rand.Next(10000));

              //  sendMessage( new byte[] { 0, 0, 0, 1, 3 });
              //  sendMessage( new byte[] { 0, 0, 0, 1, 1 });
              //  sendMessage( new byte[] { 0, 0, 0, 13, 6, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 2, 0, 64, 0, 0 });
            }
            catch (IOException e)
            {
                closeConnection("Tokom izmjene poruka, peer je prekinuo vezu.");
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
           while (true)
            {
                if (!peerClient.Connected)
                {
                    break;
                }
                //provjeri stanje zastavica :)
                //ako je za napraviti nešto pametno - poslat zahtjev npr, pošalji ga :D  
                
                //if(!this.connectionState.choked && this.connectionState.interested){
                    //pošalji request  !!!!
                //}
                   
                Dictionary<int, Status> changes = checkforChanges();
                foreach(int index in changes.Keys){
                    localPiecesStatus[index] = localClient.pieceStatus[index];
                    if (localClient.pieceStatus[index] == Status.Ima){
                        //pošalji have s oznakom pieca :D !!!!!!!!!!!!
                    }
                }



                lock(pieceSender.sendPieceDataLocker){
                    if(pieceSender.readyForSend){
                        pieceSender.sendPiece();
                        stopWatch.Restart();
                    }
                }

                //ako dulje od 20 sec nije poslana nikakva poruka pošalji keepalive
                if(stopWatch.ElapsedMilliseconds > 20000){
                    stopWatch.Stop();
                    sendMessage( new byte[]{0,0,0,0});
                }             
            }

            messageListenerThread.Join();
            closeConnection("Redovan kraj rada");
        }


        //posalji peeru poruku
        public void sendMessage(byte[] message){
            
            this.clientStream.Write(message, 0, message.Length);
            this.clientStream.Flush();
            
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

        private Dictionary<int, Status> checkforChanges()
        {
            Dictionary<int, Status> changeMapping = new Dictionary<int, Status>();
            for(int i = 0; i < localPiecesStatus.Length; i++){
                if(localPiecesStatus[i] != localClient.pieceStatus[i]){
                    changeMapping.Add(i, localClient.pieceStatus[i]);
                }
            }
            
             return changeMapping;
        }
    }
}