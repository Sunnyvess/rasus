using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FairTorrent;
using System.IO;
using TorrentClient;

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

        public PieceSender(PWPConnection connection)
        {
            _connection = connection;
            _torrent = _connection.localClient.torrentMetaInfo;     
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

                var fileInfo = new System.IO.FileInfo(_connection.localClient.torrentRootFolderPath+"\\"+torrentInfo.File.Path);
                FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                int readingOffset = pieceIndex*torrentInfo.PieceLength + blockOffset;
                try{
                    fileStream.Seek(readingOffset, SeekOrigin.Begin);
                    totalBytesReaded = fileStream.Read(buffer, 0, blockLength);
                }
                finally{
                    fileStream.Close();
                }

                //ako citanje nije bilo uspjesno DEBUG!!!
                if (totalBytesReaded != blockLength)
                    _connection.closeConnection("Neuspjesno citanje podataka iz datoteke.");
            }
            else
            {
                var torrentInfo = (MultiFileTorrentInfo) _torrent.Info;


                //trazenje u kojem fileu se nalazi trazeni blok
                int offsetInTorrent = pieceIndex*torrentInfo.PieceLength + blockOffset;

                int fileIndex = 0;          //index filea u torrentu
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

                    var fileInfo = new System.IO.FileInfo(_connection.localClient.torrentRootFolderPath+"\\"+torrentInfo.Name+"\\"+torrentInfo.Files[fileIndex].Path);
                    FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                    int readingOffset = offsetInTorrent - fileOffset; //offset u fileu, ne u torrentu

                    try{
                        fileStream.Seek(readingOffset, SeekOrigin.Begin);
                        totalBytesReaded = fileStream.Read(buffer, 0, blockLength);
                    }
                    finally{
                        fileStream.Close();
                    }
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

                        var fileInfo = new System.IO.FileInfo(_connection.localClient.torrentRootFolderPath + "\\" + torrentInfo.Name + "\\" + torrentInfo.Files[fileIndex].Path);
                        FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                        int bytesReaded;
                        try{
                            fileStream.Seek(startRadingOffset, SeekOrigin.Begin);
                            bytesReaded = fileStream.Read(tempBuffer, 0, bytesToRead);
                        }
                        finally{
                            fileStream.Close();
                        }
                        
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

            //ako citanje nije bilo uspjesno
            if (totalBytesReaded != blockLength)
                _connection.closeConnection("Neuspjesno citanje podataka iz datoteke.");


            //generiranje Piece poruke
            //payload piece poruke = piece index + block offset + block data
            int messageId = 7;
            int messageLength = 1 + 4 + 4 + buffer.Length;
            byte[] blockData = buffer;

            byte[] pieceMessage = new byte[messageLength + 4];

            //prebaciti sve u byte[] i poslati - ovo je ana dodala XD
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, pieceMessage, 0, 4);
            pieceMessage[4] = (byte) messageId;
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(pieceIndex), 0, pieceMessage, 4 + 1, 4);
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockOffset), 0, pieceMessage, 4 + 1 + 4, 4);
            Buffer.BlockCopy(blockData, 0, pieceMessage, 4+1+4+4, blockData.Length);
    
            //svi prepuštaju konkretno slanje poruke connectionu 
            _connection.sendMessage(pieceMessage);

        }
    }
}
