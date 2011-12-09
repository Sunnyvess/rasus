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
using FairTorrent.BEncoder;

namespace TorrentClient
{
    public class PWPConnection
    {

        PWPClient localClient; //ja
        TcpClient peerClient;  // onaj drugi

        string peerName; // ime onog s druge strane veze

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

            initiateComunication(client);
        }

        //zapocni komunikaciju s peerom - pošalji handshake, primi handshake i provjeri jel sve štima
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

        //ocekuj handshake - ako si prihvatio vezu od nekog peera, on prvi šalje handshake
        // ako njegov handshake valja, salji svoj
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

        //Slanje handshake poruke
        private void sendHandshake(TcpClient client)
        {

            NetworkStream clientStream = client.GetStream();
            byte[] handshakeBytes = createHandshakeArray();
            clientStream.Write(handshakeBytes, 0, handshakeBytes.Length);
        }

        //kreiranje handshake poruke
        private byte[] createHandshakeArray()
        {
            byte[] message = new byte[68];

            message[0] = System.BitConverter.GetBytes(19)[0];

            byte[] protocolName = Convertor.strToByteArrayUTF8("BitTorrent protocol");
            Array.ConstrainedCopy(protocolName, 0, message, 1, 19);

            SHA1Managed SHA1hashing = new SHA1Managed();
            byte[] sha1Hash = SHA1hashing.ComputeHash(this.localClient.infoBytes);

            Array.ConstrainedCopy(sha1Hash, 0, message, 28, sha1Hash.Length);

            byte[] clientName = Convertor.strToByteArrayUTF8(localClient.clientName);
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

        //primanje handskahe poruke
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
            byte[] localMetaInfoHash = SHA1hashing.ComputeHash(this.localClient.infoBytes);

            string hexa = BitConverter.ToString(localMetaInfoHash);

            //provjeri jel su primljeni i vlastiti hash jednaki
            for(int i = 0; i < 20; i++){
                if(localMetaInfoHash[i] != metaInfoHash[i]){
                    return false;
                }
            }

            return true;
        }

        //provjeri jel se peer indentificirao jedinstvenim imenom prema kojem već nemamo vezu
        private bool peerNameIsUnique(byte[] message){

            string peerName = Convertor.byteArrayToStringUTF8(message, 48, 20);

            if (localClient.clientName == peerName || localClient.connectedPeerNames.Contains(peerName))
            {
                return false;
            }else{
                return true;
            }
        }

        //dodaj ime peera u listu peerova s kojima imamo aktivnu vezu
        private void addPeerToActivePeers(byte[] message)
        {
            string peerName = Convertor.byteArrayToStringUTF8(message, 48, 20);
            this.peerName = peerName;
            localClient.connectedPeerNames.Add(peerName);
        }

        //slanje i primanje poruka - dok nije cijeli file prenesen ? (ko na kraju prekida vezu?)
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
                
             //   sendMessage(clientStream, new byte[]{11, 0, 0, 0, 2, 0, 0 , 0, 3, 3, 1, 3, 4, 3, 2});
            }

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
            NetworkStream stream = (NetworkStream) _stream;

            //izgled poruke => duljina poruke(4 bajta) + id poruke(4 bajta) + payload
            
            //citanje duljine poruke
            //duljina poruke je duljina id + payload
            byte[] messageSizeByte = new byte[4];
            
            //ako je duljina poruke nula, tcp konekcija se zatvara
            if (stream.Read(messageSizeByte, 0, 4) == 0)
            {
                this.connectionToPeer.closeConnection("Prilikom pokušaja čitanja poruke ustanovljeno je da je peer prekinuo vezu");
                //promjeniti u break kad se doda petlja
                return;
            }

            Console.WriteLine(BitConverter.ToString(messageSizeByte));

            if (BitConverter.IsLittleEndian)
                Array.Reverse(messageSizeByte);

            int messageSize = BitConverter.ToInt32(messageSizeByte, 0);

            byte[] message = new byte[messageSize];

            //citanje poruke
            stream.Read(message, 0, messageSize);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(message);

            //odvajanje id porke i payloada
            int messageId = BitConverter.ToInt32(message, 0);

            int payloadSize = messageSize - 4;
            byte[] payload = new byte[payloadSize];
            Array.Copy(message, 4, payload, 0, payloadSize);

            switch(messageId)
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