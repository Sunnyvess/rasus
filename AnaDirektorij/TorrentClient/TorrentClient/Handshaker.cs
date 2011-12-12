using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Net.Sockets;

namespace TorrentClient
{
    class Handshaker
    {
        PWPConnection _connection;

        public Handshaker (PWPConnection connection){
            this._connection = connection;
        }

        public void InitiateComunication(TcpClient tcpClient)
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

            if (!handshakeIsCorrect(handshakeMessage))
            {
                _connection.closeConnection("saljem zahtjev - handshake : Primljen nevaljali hash metaInfoKey-a");
                return;
            }
            if (!peerNameIsUnique(handshakeMessage))
            {
                _connection.closeConnection("primam zahtjev - handshake : Primljeno nevaljalo ime peera");
                return;
            }
            if (tcpClient.Connected)
            {
                addPeerToActivePeers(handshakeMessage);
                _connection.HandleClientComm(tcpClient);
            }
        }

        //ocekuj handshake - ako si prihvatio vezu od nekog peera, on prvi šalje handshake
        // ako njegov handshake valja, salji svoj
        public void ListenForHandshake(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            _connection.peerClient = tcpClient;
            byte[] handshakeMessage;
            try
            {
                handshakeMessage = reciveHandshake(tcpClient);
            }
            catch
            {
                return;
            }

            if (!handshakeIsCorrect(handshakeMessage))
            {
                _connection.closeConnection("primam zahtjev - handshake : Primljen nevaljali hash metaInfoKey-a");
                return;
            }
            sendHandshake(tcpClient);
            if (tcpClient.Connected)
            {
                if (!peerNameIsUnique(handshakeMessage))
                {
                    _connection.closeConnection("primam zahtjev - handshake : Primljeno nevaljalo ime peera");
                    return;
                }
                addPeerToActivePeers(handshakeMessage);
                _connection.HandleClientComm(tcpClient);
            }
            else
            {
                _connection.closeConnection("Peer prekinuo konekciju za vrijeme handshakea!");
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
            byte[] sha1Hash = SHA1hashing.ComputeHash(_connection.localClient.infoBytes);

            Array.ConstrainedCopy(sha1Hash, 0, message, 28, sha1Hash.Length);

            byte[] clientName = Convertor.strToByteArrayUTF8(_connection.localClient.clientName);
            Array.ConstrainedCopy(clientName, 0, message, 48, 20);

           /*Console.WriteLine("Poslani handshake string: ");
            foreach (byte digit in message)
            {
                Console.Write(digit);
            }
            Console.WriteLine();*/
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
                _connection.closeConnection("onaj s druge strane je prekinuo vezu");
                throw new Exception();
            }
            else
            {
            /*    Console.WriteLine("Primljen handshake string: ");
                foreach (byte digit in message)
                {
                    Console.Write(digit);
                }
                Console.WriteLine();*/
            }

            return message;
        }

        private bool handshakeIsCorrect(byte[] message)
        {

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
            byte[] localMetaInfoHash = SHA1hashing.ComputeHash(_connection.localClient.infoBytes);

            string hexa = BitConverter.ToString(localMetaInfoHash);

            //provjeri jel su primljeni i vlastiti hash jednaki
            for (int i = 0; i < 20; i++)
            {
                if (localMetaInfoHash[i] != metaInfoHash[i])
                {
                    return false;
                }
            }

            return true;
        }

        //provjeri jel se peer indentificirao jedinstvenim imenom prema kojem već nemamo vezu
        private bool peerNameIsUnique(byte[] message)
        {

            string peerName = Convertor.byteArrayToStringUTF8(message, 48, 20);

            if (_connection.localClient.clientName == peerName || _connection.localClient.connectedPeerNames.Contains(peerName))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        //dodaj ime peera u listu peerova s kojima imamo aktivnu vezu
        private void addPeerToActivePeers(byte[] message)
        {
            string peerName = Convertor.byteArrayToStringUTF8(message, 48, 20);
            _connection.peerName = peerName;
            _connection.localClient.connectedPeerNames.Add(peerName);
        }
    }
}
