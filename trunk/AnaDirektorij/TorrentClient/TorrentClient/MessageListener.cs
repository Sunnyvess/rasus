using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using FairTorrent;
using TorrentClient;
using System.Threading;


namespace MessageCommunication
{
    internal class MessageListener
    {
        private static PWPConnection _connection;

        public MessageListener(PWPConnection connection)
        {
            _connection = connection;
        }

        public void Listen(object _stream)
        {
            while(true){
                var stream = (NetworkStream) _stream;

                //izgled poruke => duljina poruke(4 bajta) + id poruke(4 bajta) + payload

                //citanje duljine poruke
                //duljina poruke je duljina id + payload
                var messageSizeByte = new byte[4];

                //ako je duljina poruke nula, tcp konekcija se zatvara
                if(!stream.CanRead){
                    return;
                }
                if (stream.Read(messageSizeByte, 0, 4) == 0)
                {
                    _connection.closeConnection("Primljena je poruka duljine nula");
                    //promjeniti u break kad se doda petlja
                    return;
                }

                int messageSize = BitConverter.ToInt32(Convertor.ConvertToBigEndian(messageSizeByte), 0);
                Console.WriteLine("Primio sam poruku duljine {0}", messageSize);

                var message = new byte[messageSize];

                //citanje poruke
                stream.Read(message, 0, messageSize);

                //odvajanje id porke i payloada
                var messageIdInBytes = new byte[] { 0, 0, 0, message[0] };
                int messageId = BitConverter.ToInt32(Convertor.ConvertToBigEndian(messageIdInBytes), 0);
                Console.WriteLine("Id primljene poruke je {0}", messageId);

                int payloadSize = messageSize - 1;
                var payload = new byte[payloadSize];
                Buffer.BlockCopy(message, 1, payload, 0, payloadSize);

                if(messageId != 4){
                    int a;
                }

                MessageHandler messageHandler = new MessageHandler(_connection, messageId, payload);

                Thread messageHandlerThread = new Thread(new ThreadStart(messageHandler.HandleMessage));
                messageHandlerThread.Start();
             }
        }     
    }
}
