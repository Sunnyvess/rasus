﻿using System;
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

                int readedBytes, readedBytesUkupni = 0, messageSize = 0;
                try
                {
                    readedBytes = stream.Read(messageSizeByte, 0, 4);
                    //if (readedBytes != 4) _connection.closeConnection("Primljeno != 4 bytea message size");
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

                messageSize = BitConverter.ToInt32(Convertor.ConvertToBigEndian(messageSizeByte), 0);
                //Console.WriteLine("Primio sam poruku duljine {0}", messageSize);

                //hvatanje keap alive poruke
                if (messageSize == 0)
                    continue;

                var message = new byte[messageSize];

                try
                {
                    //stream.Read ne garantira da će ukupni messageSize biti pročitan, 
                    //nego u readedBytes vrati onoliko koliko do sada ima u bufferu. U najvećem 
                    //broju slučajeva readedBytes == messageSize, no ponekad se dogodi da jedostavno
                    //nisu pristigli svi paketi, pa vrati "koliko ima". Zbog toga je potrebno stalno u
                    //while petlji provjeravati jel sve pristiglo što očekujemo...
                    while (readedBytesUkupni != messageSize)
                    {
                        readedBytes = stream.Read(message, readedBytesUkupni, messageSize - readedBytesUkupni);
                        readedBytesUkupni = readedBytesUkupni + readedBytes;
                        /*if (readedBytesUkupni != messageSize)
                        {
                            Console.WriteLine("Pročitano manje nego očekivano {0} < {1}", readedBytes, messageSize);
                            continue;
                            //readedBytes = stream.Read(message, readedBytes, messageSize - readedBytes);
                            //messageSize = readedBytes; //Ako je primljeno manje nego veličina punog piecea, onda je messageSize = pročitana veličina! NEVALJA!
                        }*/
                        if (readedBytes == 0)
                        {
                            _connection.closeConnection("Primljena je poruka duljine nula");
                            break;
                        }
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

                //int payloadSize = messageSize - 1;
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
