using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using FairTorrent;
using FileInfo = System.IO.FileInfo;

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
                    Request(payload);
                    break;
                case 7:
                    Piece(payload);
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

        private void Piece(byte[] payload)
        {
            throw new NotImplementedException();
        }

        private void Request(byte[] payload)
        {
            //payload = piece index + block offset + block length
            int pieceIndex = BitConverter.ToInt32(payload, 0);
            int blockOffset = BitConverter.ToInt32(payload, 4);
            int blockLength = BitConverter.ToInt32(payload, 8);

            int pieceLength = torrent.Info.PieceLength;

            //treba pronac iz kojeg fajla se cita
            //tražimo u kojem fajlu pocinje piece
            int position = 0;
            int piecePosition = pieceIndex * pieceLength;
            int i = 0;
            while (position < piecePosition)
            {
                position += torrent.Info.Files[i].Length;
            }
            //i-1 je indeks fajla u kojem pocinje piece
            int fileIndex = i - 1;
            


            //tocna pozicija od kuda pocinjemo citati podakte
            int blockPosition = piecePosition + blockOffset;

            byte[] fileBuffer = new byte[blockLength];
            //provjera da li citamo podatke iz jednog ili dva fajla
            //ako je blok na granici fajlova onda citamo iz dva (kraj jednog i pocetak drugog)
            //u position se nalazi pocetak slijedeceg fajla
            if(blockPosition + blockLength < position)
            {
                //cijeli blok je u jednom fajlu
                FairTorrent.FileInfo torrentFileInfo = torrent.Info.Files[i - 1];
                FileStream fileStream = new FileStream(torrentFileInfo.Path, FileMode.Open, FileAccess.Read);
                fileStream.Read(fileBuffer, blockPosition, blockLength);

                //posalji nekome ko ce izgenerirati piece poruku (podaci su u fileBuffer)
            }
            else
            {
                //duljina dijela u prvom i u drugom fajlu
                int secondLength = (blockPosition + blockLength) - position;
                int firstLength = blockLength - secondLength;

                //citanje dijela iz prvog fajla
                FairTorrent.FileInfo firstFileInfo = torrent.Info.Files[i - 1];
                FileStream firstFileStream = new FileStream(firstFileInfo.Path, FileMode.Open, FileAccess.Read);
                byte[] firstBuff = new byte[firstLength];
                firstFileStream.Read(firstBuff, blockPosition, firstLength);

                //citanje dijela iz drugog fajla
                FairTorrent.FileInfo secondFileInfo = torrent.Info.Files[i];
                FileStream secondFileStream = new FileStream(secondFileInfo.Path, FileMode.Open, FileAccess.Read);
                byte[] secondBuff = new byte[secondLength];
                secondFileStream.Read(secondBuff, 0, secondLength);

                Buffer.BlockCopy(firstBuff, 0, fileBuffer, 0, firstLength);
                Buffer.BlockCopy(secondBuff, 0, fileBuffer, firstLength, secondLength);

                //posalji nekome ko ce izgenerirati piece poruku (podaci su u fileBuffer)
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
