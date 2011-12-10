using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using FairTorrent;
using TorrentClient;
using FileInfo = System.IO.FileInfo;


namespace MessageListener
{
    internal class MessageListener
    {
        private Torrent torrent;
        private PWPConnection connection;

        public MessageListener(PWPConnection _connection)
        {
            torrent = connection.localClient.torrentMetaInfo;
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
                connection.closeConnection("Primljena je poruka duljine nula");
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
            //parsiranje poruke
            //payload = piece index + block offset + block length
            int pieceIndex = BitConverter.ToInt32(payload, 0);
            int blockOffset = BitConverter.ToInt32(payload, 4);
            int blockLength = BitConverter.ToInt32(payload, 8);

            //provjera da li je jedan ili vise fileova u torrentu
            if (torrent.Info.GetType().Equals(typeof (SingleFileTorrentInfo)))
            {
                var torrentInfo = (SingleFileTorrentInfo) torrent.Info;

                var fileInfo = new System.IO.FileInfo(torrentInfo.File.Path);
                System.IO.FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                int readingOffset = pieceIndex*torrentInfo.PieceLength + blockOffset;
                var buffer = new byte[blockLength];
                int bytesReaded = fileStream.Read(buffer, readingOffset, blockLength);
            }
            else
            {
                var torrentInfo = (MultiFileTorrentInfo) torrent.Info;


                //trazenje u kojem fileu se nalazi trazeni blok
                int offsetInTorrent = pieceIndex*torrentInfo.PieceLength + blockOffset;
                //udaljenost dijela koji se trazi od pocetka torrenta

                int fileIndex = 0; //index filea u torrentu
                int nextFilePosition = torrentInfo.Files[0].Length; //udaljenost flea od pocetka torrenta
                while (nextFilePosition < offsetInTorrent)
                {
                    fileIndex++;
                    nextFilePosition += torrentInfo.Files[fileIndex].Length;
                }

                int filePosition = nextFilePosition - torrentInfo.Files[fileIndex].Length;


                //provjera da li je blok iz jednog filea ili se proteze u dva
                if (nextFilePosition > offsetInTorrent + blockLength)
                {
                    //blok je iz jednog filea

                    var fileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                    System.IO.FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                    int readingOffset = offsetInTorrent - filePosition; //offset u fileu, ne u torrentu
                    var buffer = new byte[blockLength];

                    int bytesReaded = fileStream.Read(buffer, readingOffset, blockLength);
                }
                else
                {
                    //blok je na kraju jednog i pocetku drugog filea

                    //od kojeg dijela filea pocinjemo citati
                    int firstReadingOffset = offsetInTorrent - filePosition;
                    int secondReadingOffset = 0;

                    //koliko je velicina pojedinog dijela
                    int firstPartLength = nextFilePosition - offsetInTorrent;
                    int secondPartLength = (offsetInTorrent + blockLength) - nextFilePosition;

                    //bufferi u koje cemo spremati podatke
                    var firstBuff = new byte[firstPartLength];
                    var secondBuff = new byte[secondPartLength];

                    //otvaramo fileove
                    var secondFileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex + 1].Path);
                    System.IO.FileStream secondFileStream = secondFileInfo.Open(FileMode.Open, FileAccess.Read);
                    var firstFileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                    System.IO.FileStream firstFileStream = firstFileInfo.Open(FileMode.Open, FileAccess.Read);

                    //citamo podatke
                    int firstBytesReaded = firstFileStream.Read(firstBuff, firstReadingOffset, firstPartLength);
                    int secondBytesReaded = secondFileStream.Read(secondBuff, secondReadingOffset, secondPartLength);

                    //spajanje procitanih dijelova
                    var buffer = new byte[blockLength];
                    Buffer.BlockCopy(firstBuff, 0, buffer, 0, firstPartLength);
                    Buffer.BlockCopy(secondBuff, 0, buffer, firstPartLength, secondPartLength);
                }

            }

            //sto napravit ako nismo uspjeli procitat podatke iz filea (bytesReaded != blockLength)?

            //return buffer;
            //nekome predaj procitane podatke (buffer)


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


        private static void Main(string[] args)
        {

        }
    }
}
