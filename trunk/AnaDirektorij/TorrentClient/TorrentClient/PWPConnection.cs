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

        public int pieceIndexDownloading;
        public object pieceIndexDownloadingLocker = new Object();

        public object lockerDohvacenihPodataka = new Object();

        //TODO!!!!podaci o zahtjevanim piecovima, stalno se nadodaju - ako dođe choke : lista se briše :D
        public Queue<PieceSender> pieceSendingQueue;

        public object pieceSenderLocker = new Object();

        //koje piecove imam ja a koje peer
        public Status[] localPiecesStatus;
        public Status[] peerPiecesStatus;

        //jer jedna dretva handla pristigle poruke a druga provjerava kaj sve imamo
        public object piecesStatusLocker = new Object();

        public PWPConnection(PWPClient newLocalClient)
        {
            this.localClient = newLocalClient;
            this.connectionState = new ConnectionStatus();

            pieceIndexDownloading = -1;

            pieceSendingQueue = new Queue<PieceSender>();

            localPiecesStatus = new Status[newLocalClient.pieceStatus.Length];
            peerPiecesStatus = new Status[newLocalClient.pieceStatus.Length];
            peerPiecesStatus.Initialize();
            lock(newLocalClient.lockerStatusaDjelova){
            newLocalClient.pieceStatus.CopyTo(localPiecesStatus, 0);
            }
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

            //Console.WriteLine("Radimo nešto korisno zajedno!");

            lock(localClient.lockerStatusaDjelova){
                MessageSender.SendBitField(localClient.pieceStatus, this);
            }

            //za slanje keepalive poruka ako ništa nije poslano neko vrijeme
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            while (true)
            {
                if (!peerClient.Connected)
                {
                    break;
                }

                Dictionary<int, Status> changes = checkforChanges();
                foreach (int index in changes.Keys)
                {
                    localPiecesStatus[index] = changes[index];
                    if ( changes[index] == Status.Ima)
                    {
                        MessageSender.SendHave(index, this);
                    }
                }

                if(pieceSendingQueue.Count > 7 && !connectionState.peerChoked){
                    MessageSender.sendChoke(this);
                }else{
                    if(pieceSendingQueue.Count <= 7 && connectionState.peerChoked){
                    MessageSender.sendUnchoke(this);
                    }
                }
                
                //ovo smislit po nekom algoritmu!
                if(!this.connectionState.localChoked){
                    lock(lockerDohvacenihPodataka){
                        if(pieceIndexDownloading == -1){
                            lock(piecesStatusLocker){
                                lock(localClient.lockerStatusaDjelova){

                                    MessageSender.SendRequest(localClient.pieceStatus, this.peerPiecesStatus, this);
                                }
                            }
                        }
                    }

                    lock (pieceSenderLocker)
                    {

                        while (true)
                        {
                            PieceSender sender;
                            try
                            {
                                sender = pieceSendingQueue.Dequeue();

                            }
                            catch
                            {
                                break;
                            }

                            sender.sendPiece();
                        }
                    }
                }
  
                //ako dulje od 20 sec nije poslana nikakva poruka pošalji keepalive
                if(stopWatch.ElapsedMilliseconds > 20000){
                    sendMessage(new byte[] { 0, 0, 0, 0 });
                    stopWatch.Restart();  
                }             
            }

            messageListenerThread.Join();
            closeConnection("Redovan kraj rada");
        }


        //posalji peeru poruku
        public void sendMessage(byte[] message){
            
            try{
                this.clientStream.Write(message, 0, message.Length);
                this.clientStream.Flush();

                if(message.Length > 4){
                    Console.WriteLine("Poslana poruka s id-jem: "+message[4]);
                }
            }catch{}    
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
            lock (localClient.lockerStatusaDjelova)
            {
                lock(piecesStatusLocker){
                    for(int i = 0; i < localPiecesStatus.Length; i++){
                
                        if(localPiecesStatus[i] != localClient.pieceStatus[i]){
                            changeMapping.Add(i, localClient.pieceStatus[i]);
                        }
                    }
                }
            }
            
             return changeMapping;
        }
    }
}