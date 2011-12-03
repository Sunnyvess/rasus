using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Threading;

namespace TorrentClient
{
    public class PWPConnection
    {

        PWPClient localClient;

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
                closeConnection(tcpClient, "saljem zahtjev - handshake : Primljen nevaljali hash metaInfoKey-a");
                return;
            }
            if(!peerNameIsUnique(handshakeMessage)){
                closeConnection(tcpClient, "primam zahtjev - handshake : Primljeno nevaljalo ime peera");
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
            byte[] handshakeMessage;
            try{
                handshakeMessage = reciveHandshake(tcpClient);
            }catch{
                return;
            }
            
            if (!handshakeIsCorrect(handshakeMessage))
            {
                closeConnection(tcpClient, "primam zahtjev - handshake : Primljen nevaljali hash metaInfoKey-a");
                return;
            }
            sendHandshake(tcpClient);
            if (tcpClient.Connected)
            {
                if(!peerNameIsUnique(handshakeMessage)){
                    closeConnection(tcpClient, "primam zahtjev - handshake : Primljeno nevaljalo ime peera");
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

            byte[] protocolName = Convertor.strToByteArray("BitTorrent protocol");
            Array.ConstrainedCopy(protocolName, 0, message, 1, 19);

            SHA1Managed SHA1hashing = new SHA1Managed();
            byte[] sha1Hash = SHA1hashing.ComputeHash(Convertor.strToByteArray(localClient.metainfoKey));
            Array.ConstrainedCopy(sha1Hash, 0, message, 28, sha1Hash.Length);

            byte[] clientName = Convertor.strToByteArray(localClient.peerId);
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
                closeConnection(tcpClient, "onaj s druge strane je prekinuo vezu");
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

            string protocolName = Convertor.byteArrayToString(message, 1, protocolNameLength);

            if (protocolName.Length != protocolNameLength)
            {
                return false;
            }

            byte[] metaInfoHash = new byte[20];
            Array.ConstrainedCopy(message, 28, metaInfoHash, 0, 20);

            SHA1Managed SHA1hashing = new SHA1Managed();
            byte[] localMetaInfoHash = SHA1hashing.ComputeHash(Convertor.strToByteArray(localClient.metainfoKey));

            for(int i = 0; i < 20; i++){
                if(localMetaInfoHash[i] != metaInfoHash[i]){
                    return false;
                }
            }

            return true;
        }

        private bool peerNameIsUnique(byte[] message){

            string clientName = Convertor.byteArrayToString(message, 48, 20);

            if (localClient.peerId == clientName || localClient.connectedPeerNames.Contains(clientName))
            {
                return false;
            }else{
                return true;
            }
        }

        private void addPeerToActivePeers(byte[] message)
        {
            string clientName = Convertor.byteArrayToString(message, 48, 20);
            this.peerName = clientName;
            localClient.connectedPeerNames.Add(clientName);
        }


        private void handleClientComm(TcpClient tcpClient)
        {

            Console.WriteLine("Radimo nešto korisno zajedno!");
            while (true)
            {
                NetworkStream clientStream = tcpClient.GetStream();
                byte[] buffer = new byte[1];
                int bytesRead = 0;
                try{
                    bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                }
                catch{
                    break;
                }
                if (bytesRead == 0) //kako ovo napravit u asinkronom modu rada?
                {
                    break;
                }
            }
            /*
            */
            closeConnection(tcpClient, "Redovan kraj rada");
        }

        private void closeConnection(TcpClient tcpClient, string desc)
        {
            lock(localClient.lockerBrojaKonekcija){
                localClient.numConnections--;
            }
            
            //ovo mozda ne radi zbog usporedbe stringova
            localClient.connectedPeerNames.Remove(this.peerName);

            Console.WriteLine("Konekcija se zatvara!: "+desc);
            tcpClient.Close();

        }

    }

    public class Convertor{

        public static byte[] strToByteArray(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        public static string byteArrayToString(byte[] array, int startIndex, int length)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetString(array, startIndex, length);
        }
    }
}
