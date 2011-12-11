using System;
using System.IO;
using System.Net.Sockets;
using FairTorrent;
using TorrentClient;


namespace MessageListener
{
    internal class MessageListener
    {
        private readonly Torrent _torrent;
        private readonly PWPConnection _connection;

        public MessageListener(PWPConnection connection)
        {
            _torrent = _connection.localClient.torrentMetaInfo;
            _connection = connection;
        }

        public void Listen(object _stream)
        {
            var stream = (NetworkStream) _stream;

            //izgled poruke => duljina poruke(4 bajta) + id poruke(4 bajta) + payload

            //citanje duljine poruke
            //duljina poruke je duljina id + payload
            var messageSizeByte = new byte[4];

            //ako je duljina poruke nula, tcp konekcija se zatvara
            if (stream.Read(messageSizeByte, 0, 4) == 0)
            {
                _connection.closeConnection("Primljena je poruka duljine nula");
                //promjeniti u break kad se doda petlja
                return;
            }

            int messageSize = BitConverter.ToInt32(ConvertToBigEndian(messageSizeByte), 0);

            var message = new byte[messageSize];

            //citanje poruke
            stream.Read(message, 0, messageSize);

            //odvajanje id porke i payloada
            var messageIdInBytes = new byte[4];
            Buffer.BlockCopy(message, 0, messageIdInBytes, 0, 4);
            int messageId = BitConverter.ToInt32(ConvertToBigEndian(messageIdInBytes), 0);

            int payloadSize = messageSize - 4;
            var payload = new byte[payloadSize];
            Buffer.BlockCopy(message, 4, payload, 0, payloadSize);

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

        private byte[] ConvertToBigEndian(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return array;
        }

        private void cancle()
        {
            throw new NotImplementedException();
        }

        private void Piece(byte[] payload)
        {
            //payload = piece index + block offset + block data

            //parsiranje poruke
            var pieceIndexInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, pieceIndexInBytes, 0, 4);
            int pieceIndex = BitConverter.ToInt32(ConvertToBigEndian(pieceIndexInBytes), 0);

            var blockOffsetInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, blockOffsetInBytes, 0, 4);
            int blockOffset = BitConverter.ToInt32(ConvertToBigEndian(blockOffsetInBytes), 0);

            int blockLength = payload.Length - 8;

            byte[] blockData = new byte[blockLength];
            Buffer.BlockCopy(payload, 8, blockData, 0, blockLength);


            if (_torrent.Info.GetType().Equals(typeof(SingleFileTorrentInfo)))
            {
                var torrentInfo = (SingleFileTorrentInfo)_torrent.Info;

                var fileInfo = new System.IO.FileInfo(torrentInfo.File.Path);
                FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);

                int writingOffset = pieceIndex * torrentInfo.PieceLength + blockOffset;
                fileStream.Write(blockData, writingOffset, blockLength);
            }
            else
            {
                var torrentInfo = (MultiFileTorrentInfo)_torrent.Info;

                //trazenje u kojem fileu se nalazi trazeni blok
                int offsetInTorrent = pieceIndex * torrentInfo.PieceLength + blockOffset;
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
                    FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);

                    int writingOffset = offsetInTorrent - filePosition; //offset u fileu, ne u torrentu

                    fileStream.Write(blockData, writingOffset, blockLength);
                }
                else
                {
                    //blok je na kraju jednog i pocetku drugog filea

                    //od kojeg dijela filea pocinjemo pisati
                    int firstWritingOffset = offsetInTorrent - filePosition;
                    int secondWritingOffset = 0;

                    //koliko je velicina pojedinog dijela
                    int firstPartLength = nextFilePosition - offsetInTorrent;
                    int secondPartLength = (offsetInTorrent + blockLength) - nextFilePosition;

                    var firstBuff = new byte[firstPartLength];
                    var secondBuff = new byte[secondPartLength];
                    Buffer.BlockCopy(blockData, 0, firstBuff, 0, firstPartLength);
                    Buffer.BlockCopy(blockData, firstPartLength, secondBuff, 0, secondPartLength);

                    //otvaramo fileove
                    var secondFileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex + 1].Path);
                    FileStream secondFileStream = secondFileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);
                    var firstFileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                    FileStream firstFileStream = firstFileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);

                    //pisemo podatke
                    firstFileStream.Write(firstBuff, firstWritingOffset, firstPartLength);
                    secondFileStream.Write(secondBuff, secondWritingOffset, secondPartLength);
                }
            }
        }

        private void Request(byte[] payload)
        {
            //parsiranje poruke
            //payload = piece index + block offset + block length

            var pieceIndexInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, pieceIndexInBytes, 0, 4);
            int pieceIndex = BitConverter.ToInt32(ConvertToBigEndian(pieceIndexInBytes), 0);

            var blockOffsetInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, blockOffsetInBytes, 0, 4);
            int blockOffset = BitConverter.ToInt32(ConvertToBigEndian(blockOffsetInBytes), 0);

            var blockLengthInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, blockLengthInBytes, 0, 4);
            int blockLength = BitConverter.ToInt32(ConvertToBigEndian(blockLengthInBytes), 0);

            //provjera da li je jedan ili vise fileova u torrentu
            if (_torrent.Info.GetType().Equals(typeof (SingleFileTorrentInfo)))
            {
                var torrentInfo = (SingleFileTorrentInfo) _torrent.Info;

                var fileInfo = new System.IO.FileInfo(torrentInfo.File.Path);
                FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                int readingOffset = pieceIndex*torrentInfo.PieceLength + blockOffset;
                var buffer = new byte[blockLength];
                int bytesReaded = fileStream.Read(buffer, readingOffset, blockLength);
            }
            else
            {
                var torrentInfo = (MultiFileTorrentInfo) _torrent.Info;


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
                    FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

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
                    FileStream secondFileStream = secondFileInfo.Open(FileMode.Open, FileAccess.Read);
                    var firstFileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                    FileStream firstFileStream = firstFileInfo.Open(FileMode.Open, FileAccess.Read);

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


        private static void Main()
        {

        }

    }
}
