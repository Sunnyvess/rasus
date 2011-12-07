using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using FairTorrent;

namespace MessageListener
{
    class MessageListener
    {
        private Torrent torrent;
        private PWPConnection connection;
        
        public MessageListener(PWPConnection _connection)
        {
            torrent = connection.torrent;
            connection = _connection;
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
                connection.CloseConnection();
                //promjeniti u break kad se doda petlja
                return;
            }

            int messageSize = BitConverter.ToInt32(messageSizeByte, 0);

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
                    Peace(payload);
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

        private void Peace(byte[] payload)
        {
            throw new NotImplementedException();
        }

        private void request(byte[] payload)
        {
            //payload = piece index + block offset + block length
            int pieceIndex = BitConverter.ToInt32(payload, 0);
            int blocOffset = BitConverter.ToInt32(payload, 4);
            int blockLength = BitConverter.ToInt32(payload, 8);

            int pieceLength = torrent.Info.PieceLength;

            //treba pronac iz kojeg fajla se cita
            //znamo kolika je velicina kojeg fajla, velicinu piecea i koji piece nam treba
            int position = 0;
            int piecePosition = pieceIndex * pieceLength;
            int i = 0;
            while (position < piecePosition)
            {
                position += torrent.Info.Files[i].Length;
            }
            //i-1 je indeks fajla u kojem se piece nalazi
            FairTorrent.FileInfo fileInfo = torrent.Info.Files[i - 1];
            int fileIndex = i - 1;
            
            //byte[] pieceBuffer = 
            
            //provjera da piece sadrži samo jedan file ili dva
            //ako je pocetak piecea u jednom fileu a kraj u drugom
            //pocetak slijedeceg filea se nalazi u varijabli position
            if (piecePosition + torrent.Info.PieceLength < position)
            {
                //citamo iz jednog filea
                var fileStream = File.Open(fileInfo.Path, FileMode.Open, FileAccess.Read);
                //fileStream.Read()

            }
            else
            {
                //citamo iz dva filea
            }
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
