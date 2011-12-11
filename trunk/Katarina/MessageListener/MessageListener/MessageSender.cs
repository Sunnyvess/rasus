using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FairTorrent;
using TorrentClient;

namespace MessageCommunication
{
    internal class MessageSender
    {
        private static Torrent _torrent;
        private static PWPConnection _connection;

        public MessageSender(PWPConnection connection)
        {
            _torrent = _connection.localClient.torrentMetaInfo;
            _connection = connection;
        }

        public static void Piece(int pieceIndex, int blockOffset, int blockLength)
        {
            //citanje trazenih podataka
            var buffer = new byte[blockLength]; //procitani podaci
            int totalBytesReaded;

            //provjera da li je jedan ili vise fileova u torrentu
            if (_torrent.Info.GetType().Equals(typeof (SingleFileTorrentInfo)))
            {
                var torrentInfo = (SingleFileTorrentInfo) _torrent.Info;

                var fileInfo = new System.IO.FileInfo(torrentInfo.File.Path);
                FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                int readingOffset = pieceIndex*torrentInfo.PieceLength + blockOffset;
                totalBytesReaded = fileStream.Read(buffer, readingOffset, blockLength);
            }
            else
            {
                var torrentInfo = (MultiFileTorrentInfo) _torrent.Info;


                //trazenje u kojem fileu se nalazi trazeni blok
                int offsetInTorrent = pieceIndex*torrentInfo.PieceLength + blockOffset;

                int fileIndex = 0; //index filea u torrentu
                int nextFileOffset = torrentInfo.Files[0].Length;
                while (nextFileOffset < offsetInTorrent)
                {
                    fileIndex++;
                    nextFileOffset += torrentInfo.Files[fileIndex].Length;
                }

                int fileOffset = nextFileOffset - torrentInfo.Files[fileIndex].Length;


                //provjera da li je blok iz jednog filea ili iz vise njih
                if (nextFileOffset > offsetInTorrent + blockLength)
                {
                    //blok je iz jednog filea

                    var fileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                    FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                    int readingOffset = offsetInTorrent - fileOffset; //offset u fileu, ne u torrentu

                    totalBytesReaded = fileStream.Read(buffer, readingOffset, blockLength);
                }
                else
                {
                    //blok je iz vise fileova

                    //gradince od kud do kud se cita iz kojeg filea
                    int startRadingOffset = offsetInTorrent - fileOffset;
                    int endReadingOffset = nextFileOffset;
                    totalBytesReaded = 0;
                    while (fileOffset < offsetInTorrent + blockLength)
                    {
                        //citanje iz filea
                        int bytesToRead = endReadingOffset - startRadingOffset;
                        var tempBuffer = new byte[bytesToRead];

                        var fileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                        FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                        int bytesReaded = fileStream.Read(tempBuffer, startRadingOffset, bytesToRead);
                        if (bytesReaded == 0) _connection.closeConnection("Nemogu procitati podatke iz datoteke.");

                        //spajanje do sada procitanog
                        Buffer.BlockCopy(tempBuffer, 0, buffer, totalBytesReaded, bytesReaded);
                        totalBytesReaded += bytesReaded;

                        //priprema za citanje slijedeceg filea
                        fileIndex++;
                        fileOffset = nextFileOffset;
                        nextFileOffset += torrentInfo.Files[fileIndex].Length;

                        startRadingOffset = endReadingOffset;
                        if (nextFileOffset < offsetInTorrent + blockLength)
                        {
                            endReadingOffset = nextFileOffset;
                        }
                        else
                        {
                            endReadingOffset = offsetInTorrent + blockLength;
                        }
                    }

                    if (totalBytesReaded == 0) _connection.closeConnection("Nemogu procitati podatke iz datoteke.");
                }
            }

            //sto napravit ako nismo uspjeli procitat podatke iz filea (totalBytesReaded != blockLength)?


            //generiranje Piece poruke
            //payload piece poruke = piece index + block offset + block data
            int messageId = 7;
            int messageLength = 4 + 4 + 4 + buffer.Length;
            byte[] blockData = buffer;

            //prebaciti sve u byte[] i poslati
        }

        public static void Request()
        {
        }

        public static byte[] convertIntToByte()
        {
            return new byte[30];
        }
    }
}
