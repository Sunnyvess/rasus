using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FairTorrent;
using System.IO;

namespace TorrentClient
{
    //klasa koja kada se primi zahtjev za pieceom salje odgovor        
    public class PieceSender
    {
        private static Torrent _torrent;
        private static PWPConnection _connection;

        public int pieceIndex;
        public int blockOffset;
        public int blockLength;

        public bool readyForSend;

        public object sendPieceDataLocker = new Object();

        public PieceSender(PWPConnection connection)
        {
            _connection = connection;
            _torrent = _connection.localClient.torrentMetaInfo;   
            readyForSend = false;     
        }

        public void sendPiece()
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


                }
            }

            //sto napravit ako nismo uspjeli procitat podatke iz filea (totalBytesReaded != blockLength)?


            //generiranje Piece poruke
            //payload piece poruke = piece index + block offset + block data
            int messageId = 7;
            int messageLength = 1 + 4 + 4 + buffer.Length;
            byte[] blockData = buffer;

            byte[] pieceMessage = new byte[messageLength + 4];

            //prebaciti sve u byte[] i poslati - ovo je ana dodala XD
            Buffer.BlockCopy(BitConverter.GetBytes(messageLength), 0, pieceMessage, 0, 4);
            pieceMessage[5] = (byte) messageId;
            Buffer.BlockCopy(BitConverter.GetBytes(pieceIndex), 0, pieceMessage, 4+1, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(blockOffset), 0, pieceMessage, 4+1+4, 4);
            Buffer.BlockCopy(blockData, 0, pieceMessage, 4+1+4+4, blockData.Length);
    
            //svi prepuštaju konkretno slanje poruke connectionu 
            _connection.sendMessage( pieceMessage);

            readyForSend = false; 
        }
    }
}
