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
                if (stream.CanTimeout)
                    stream.ReadTimeout = 2 * 60 * 1000; //ceka dvije minute
                
                //izgled poruke => duljina poruke(4 bajta) + id poruke(4 bajta) + payload

                //citanje duljine poruke
                //duljina poruke je duljina id + payload
                var messageSizeByte = new byte[4];

                //ako je duljina poruke nula, tcp konekcija se zatvara
                if(!stream.CanRead){
                    return;
                }

                int readedBytes;
                try
                {
                    readedBytes = stream.Read(messageSizeByte, 0, 4);
                    if (readedBytes == 0)
                    {
                        _connection.closeConnection("Primljena je poruka duljine nula");
                        break;
                    }
                }
                catch(Exception ex)
                {
                    _connection.closeConnection("PogreŠka kod čitanja sa streama");
                }

                int messageSize = BitConverter.ToInt32(Convertor.ConvertToBigEndian(messageSizeByte), 0);
                //Console.WriteLine("Primio sam poruku duljine {0}", messageSize);

                //hvatanje keap alive poruke
                if (messageSize == 0)
                    continue;

                var message = new byte[messageSize];

                try
                {
                    readedBytes = stream.Read(message, 0, messageSize);
                    if (readedBytes == 0)
                    {
                        _connection.closeConnection("Primljena je poruka duljine nula");
                        break;
                    }

                }
                catch (Exception ex)
                {
                    _connection.closeConnection("PogreŠka kod čitanja sa streama");
                }

                //odvajanje id porke i payloada
                var messageIdInBytes = new byte[] { 0, 0, 0, message[0] };
                int messageId = BitConverter.ToInt32(Convertor.ConvertToBigEndian(messageIdInBytes), 0);
                Console.WriteLine("Id primljene poruke je {0}", messageId);

                int payloadSize = messageSize - 1;
                var payload = new byte[payloadSize];
                Buffer.BlockCopy(message, 1, payload, 0, payloadSize);

                MessageHandler messageHandler = new MessageHandler(_connection, messageId, payload);

                Thread messageHandlerThread = new Thread(new ThreadStart(messageHandler.HandleMessage));
                messageHandlerThread.Start();
             }
        }     
    }
}
