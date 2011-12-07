using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using FairTorrent;

namespace MessageListener
{
    class MessageListener
    {
        private Torrent torrent;
        
        public MessageListener(PWPConnection connection)
        {
            torrent = connection.torrent;
        }

        public void Listen(object _stream)
        {
            NetworkStream stream = (NetworkStream) _stream;

            //izgled poruke => duljina poruke(4 bajta) + id poruke(4 bajta) + payload
            
            //citanje duljine poruke
            //duljina poruke je duljina id + payload
            int messageSize = stream.ReadByte();
            byte[] message = new byte[messageSize];

            //citanje poruke
            stream.Read(message, 0, messageSize);
            
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

            torrent.Info.PieceLength
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




        static void Main(string[] args)
        {

        }
    }

}
