using System;
using System.IO;
using System.Net.Sockets;
using FairTorrent;
using TorrentClient;


namespace MessageCommunication
{
    internal class MessageListener
    {
        private static Torrent _torrent;
        private static PWPConnection _connection;
        private static byte[] _piece;
        private static byte[] _partsInPiece;

        public MessageListener(PWPConnection connection)
        {
            _torrent = _connection.localClient.torrentMetaInfo;
            _connection = connection;
            _piece = new byte[_torrent.Info.PieceLength];
            _partsInPiece = new byte[_torrent.Info.PieceLength];
            _partsInPiece.Initialize();
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
            Console.WriteLine("Primio sam poruku duljine {0}", messageSize);

            var message = new byte[messageSize];

            //citanje poruke
            stream.Read(message, 0, messageSize);

            //odvajanje id porke i payloada
            byte[] messageIdInBytes = new byte[] { 0, 0, 0, message[0] };
            int messageId = BitConverter.ToInt32(ConvertToBigEndian(messageIdInBytes), 0);
            Console.WriteLine("Id primljene poruke je {0}", messageId);

            int payloadSize = messageSize - 1;
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
                    ProcessReceivedRequest(payload);
                    break;
                case 7:
                    ProcessReceivedPiece(payload);
                    break;
                case 8:
                    cancle();
                    break;
            }
        }

        private static byte[] ConvertToBigEndian(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return array;
        }

        private void cancle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obradjuje primljenu poruku piece
        /// </summary>
        /// <param name="payload">tijelo poruke je payload = piece index + block offset + block data</param>
        private static void ProcessReceivedPiece(byte[] payload)
        {
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
            
            //sprema blok u piece
            Buffer.BlockCopy(blockData, 0, _piece, blockOffset, blockLength);
            //oznacava koji djelovi piecea su stigli
            for (int i = blockOffset; i < blockLength; i++)
                _partsInPiece[i] = 1;

            
            //oznaci da se piece skida


            //provjera jel skupljen cijeli piece
            bool zeroFound = false;
            foreach (byte b in _partsInPiece)
            {
                if (b == 0) zeroFound = true;
            }

            if (!zeroFound)
            {
                //skupljen je cijeli piece

                //provjeri jel dobar
                //upisi da je primljen piece
                //spremi ga u file
            }
        }


        /// <summary>
        /// Obradjuje primljenu poruku request
        /// </summary>
        /// <param name="payload">tijelo poruke je payload = piece index + block offset + block data</param>
        private static void ProcessReceivedRequest(byte[] payload)
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

            if (blockLength < Math.Pow(2, 17))
            {
                MessageSender.Piece(pieceIndex, blockOffset, blockLength);
            }
            else
            {
                _connection.closeConnection("Poslan je request sa duljinom bloka vecom od 2 na 17.");
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


        private static void Main()
        {

        }

    }
}
