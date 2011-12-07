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

namespace TorrentClient
{
    public class PWPConnection
    {

        PWPClient localClient;
        TcpClient peerClient;

        string peerName;

        public PWPConnection(PWPClient newLocalClient)
        {
            this.localClient = newLocalClient;
        }

        public void ConnectToPeer(object newPeer)
        {
            Peer peer = (Peer)newPeer;
            TcpClient client = new TcpClient();

            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(peer.IpAdress), peer.Port);

            client.Connect(serverEndPoint);
            peerClient = client;

            Console.WriteLine("Uspostavljena veza prema peeru na portu " + peer.Port);

            initiateComunication(client);
        }

        private void initiateComunication(TcpClient tcpClient)
        {
            sendHandshake(tcpClient);

            byte[] handshakeMessage;
            try
            {
                handshakeMessage = reciveHandshake(tcpClient);
            }
            catch
            {
                return;
            }
            
            if (!handshakeIsCorrect(handshakeMessage)){
                closeConnection( "saljem zahtjev - handshake : Primljen nevaljali hash metaInfoKey-a");
                return;
            }
            if(!peerNameIsUnique(handshakeMessage)){
                closeConnection( "primam zahtjev - handshake : Primljeno nevaljalo ime peera");
                return;
            }
            if (tcpClient.Connected){ 
                addPeerToActivePeers(handshakeMessage);
                handleClientComm(tcpClient);
            }
        }

        public void ListenForHandshake(object client)
        {
            TcpClient tcpClient = (TcpClient) client;
            peerClient = tcpClient;
            byte[] handshakeMessage;
            try{
                handshakeMessage = reciveHandshake(tcpClient);
            }catch{
                return;
            }
            
            if (!handshakeIsCorrect(handshakeMessage))
            {
                closeConnection( "primam zahtjev - handshake : Primljen nevaljali hash metaInfoKey-a");
                return;
            }
            sendHandshake(tcpClient);
            if (tcpClient.Connected)
            {
                if(!peerNameIsUnique(handshakeMessage)){
                    closeConnection( "primam zahtjev - handshake : Primljeno nevaljalo ime peera");
                    return;
                }
                addPeerToActivePeers(handshakeMessage);
                handleClientComm(tcpClient);
            }else{
                Console.WriteLine("Konekcija je izgleda prekinuta");
            }

        }

        private void sendHandshake(TcpClient client)
        {

            NetworkStream clientStream = client.GetStream();
            byte[] handshakeBytes = createHandshakeArray();
            clientStream.Write(handshakeBytes, 0, handshakeBytes.Length);
        }

        private byte[] createHandshakeArray()
        {
            byte[] message = new byte[68];

            message[0] = System.BitConverter.GetBytes(19)[0];

            byte[] protocolName = Convertor.strToByteArrayUTF8("BitTorrent protocol");
            Array.ConstrainedCopy(protocolName, 0, message, 1, 19);

            SHA1Managed SHA1hashing = new SHA1Managed();
            byte[] sha1Hash = SHA1hashing.ComputeHash(getMetaInfoKey());
         /*

            

            char[] array = info.ToArray();
             
            foreach(char ch in array){
                
            }


            

            byte[] test = new byte[]{84,124,15,255,155,171,156,168,91,46,204,24,249,116,110, 139,202,167,163,54};

            //string pretvoreno = Convertor.byteArrayToStringUTF8(test,0 , test.Length);

            */
         //   string info = "788f590f28a799cc1009a9b780b649fd6f0a2e91";
       //     byte[] sha1Hash = Convertor.strToByteArrayASCII(info);

            Array.ConstrainedCopy(sha1Hash, 0, message, 28, sha1Hash.Length);

            byte[] clientName = Convertor.strToByteArrayUTF8(localClient.peerId);
            Array.ConstrainedCopy(clientName, 0, message, 48, 20);

            Console.WriteLine("Poslani handshake string: ");
            foreach (byte digit in message)
            {
                Console.Write(digit);
            }
            Console.WriteLine();
            // Console.WriteLine("Duljina stringa:"+ message.Length);

            return message;
        }

        
        //ova metoda ne bi smjela bit u ovoj klasi?
        private byte[] getMetaInfoKey(){
            byte[] metaByteArray = File.ReadAllBytes("proba.torrent");
            //string metaString = File.ReadAllText("proba.torrent", new UTF8Encoding());
            string metaString = Convertor.byteArrayToStringUTF8(metaByteArray, 0, metaByteArray.Length);
            int beginIndex = metaString.IndexOf("infod") + "infod".Length;

            string interString = metaString.Substring(beginIndex, metaString.Length - beginIndex);
            int endIndex = interString.IndexOf("ee");
            int len = endIndex;
            string rez =  metaString.Substring(beginIndex, len);
            //Console.WriteLine(beginIndex +"  "+ endIndex);
            Console.WriteLine(rez.Substring(rez.Length-20,20));
            //Console.ReadLine();
            byte[] nekaj = Convertor.strToByteArrayUTF8(metaString);
            byte[] info = Convertor.strToByteArrayUTF8(rez);
            return info;
        }

        private byte[] reciveHandshake(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[68];
            int bytesRead = 0;

            bytesRead = clientStream.Read(message, 0, 68);

            if (bytesRead == 0)
            {
                //the client has disconnected from the server
                closeConnection("onaj s druge strane je prekinuo vezu");
                throw new Exception();
            }
            else
            {
                Console.WriteLine("Primljen handshake string: ");
                foreach (byte digit in message)
                {
                    Console.Write(digit);
                }
                Console.WriteLine();
            }
            
            return message;
        }

        private bool handshakeIsCorrect(byte[] message){

            byte[] protocolNameLengthBytes = new byte[2];
            Array.ConstrainedCopy(message, 0, protocolNameLengthBytes, 0, 1);
            int protocolNameLength = System.BitConverter.ToInt16(protocolNameLengthBytes, 0);

            string protocolName = Convertor.byteArrayToStringUTF8(message, 1, protocolNameLength);

            if (protocolName.Length != protocolNameLength)
            {
                return false;
            }

            //procitaj primljeni info
            byte[] metaInfoHash = new byte[20];
            Array.ConstrainedCopy(message, 28, metaInfoHash, 0, 20);

            //izracunaj vlastiti
            SHA1Managed SHA1hashing = new SHA1Managed();
            byte[] localMetaInfoHash = SHA1hashing.ComputeHash(getMetaInfoKey());

            for(int i = 0; i < 20; i++){
                if(localMetaInfoHash[i] != metaInfoHash[i]){
                    return false;
                }
            }

            return true;
        }

        private bool peerNameIsUnique(byte[] message){

            string clientName = Convertor.byteArrayToStringUTF8(message, 48, 20);

            if (localClient.peerId == clientName || localClient.connectedPeerNames.Contains(clientName))
            {
                return false;
            }else{
                return true;
            }
        }

        private void addPeerToActivePeers(byte[] message)
        {
            string clientName = Convertor.byteArrayToStringUTF8(message, 48, 20);
            this.peerName = clientName;
            localClient.connectedPeerNames.Add(clientName);
        }


        private void handleClientComm(TcpClient tcpClient)
        {
            NetworkStream clientStream = tcpClient.GetStream();

            MessageListener messageListener = new MessageListener(this);
            Thread messageListenerThread = new Thread(new ParameterizedThreadStart(messageListener.Listen));
            messageListenerThread.Start(clientStream);

            
            Console.WriteLine("Radimo nešto korisno zajedno!");


            while (true)
            {
                //probvjeri stanje zastavica :)

                //ako je za napraviti nešto pametno - poslat zahtjev npr, pošalji ga :D
                
                sendMessage(clientStream, new byte[]{11, 2, 0, 0 , 0, 3, 3, 1, 3, 4, 3, 2});
            }

            closeConnection("Redovan kraj rada");
        }

        private void sendMessage(NetworkStream stream, byte[] message){

           stream.Write(message, 0, message.Length);
           stream.Flush();
        }

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

    public class Convertor{

        public static byte[] strToByteArrayUTF8(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        public static byte[] strToByteArrayUTF16(string str)
        {
            System.Text.UnicodeEncoding encoding = new System.Text.UnicodeEncoding();
            return encoding.GetBytes(str);
        }

        public static byte[] strToByteArrayASCII(string str)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        public static string byteArrayToStringUTF8(byte[] array, int startIndex, int length)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetString(array, startIndex, length);
        }

        public static string byteArrayToStringASCII(byte[] array, int startIndex, int length)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetString(array, startIndex, length);
        } 
    }

    //klasa koja čeka konkretnu implementaciju 
    public class MessageListener{
        
        PWPConnection connectionToPeer;

        public MessageListener(PWPConnection connection){
            this.connectionToPeer = connection;
        }

        public void listenForMessages(object clientStream){
            //slušam !! clientStream.BeginRead();

            connectionToPeer.closeConnection("Redovno ubijam vezu!");
        }

        public void Listen(object _stream)
        {
            NetworkStream stream = (NetworkStream)_stream;

            //izgled poruke => duljina poruke(4 bajta) + id poruke(4 bajta) + payload

            //citanje duljine poruke
            //duljina poruke je duljina id + payload
            while (!stream.DataAvailable) ;
            int messageSize = stream.ReadByte();
            byte[] message = new byte[messageSize];

            //citanje poruke

            while(!stream.DataAvailable);
            int bytesRead = stream.Read(message, 0, messageSize);

            //odvajanje id porke i payloada
            int messageId = BitConverter.ToInt32(message, 0);

            int payloadSize = messageSize - 4;
            byte[] payload = new byte[payloadSize];
            Array.Copy(message, 4, payload, 0, payloadSize);

            switch (messageId)
            {
                case 0:
                    choke();
                    break;
                case 1:
                    unchoke();
                    break;
                case 2:
                    interested();
                    break;
                case 3:
                    uninterested();
                    break;
                case 4:
                    have();
                    break;
                case 6:
                    request(payload);
                    break;
                case 7:
                    peace(payload);
                    break;
                case 8:
                    cancle();
                    break;
            }
        }

        private void cancle()
        {
            throw new NotImplementedException();
        }

        private void peace(byte[] payload)
        {
            //payload = piece index + block offset + block length
            byte pieceIndex = payload[0];
            byte blocOffset = payload[1];
            byte blockLength = payload[2];

        }

        private void request(byte[] payload)
        {
            throw new NotImplementedException();
        }

        private void have()
        {
            throw new NotImplementedException();
        }

        private void uninterested()
        {
            throw new NotImplementedException();
        }

        private void interested()
        {
            throw new NotImplementedException();
        }

        private void unchoke()
        {
            throw new NotImplementedException();
        }

        private void choke()
        {
            throw new NotImplementedException();
        }
    }
}