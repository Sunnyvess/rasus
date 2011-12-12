using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using FairTorrent;

namespace TorrentClient
{
    class MessageHandler
    {

        private int _messageId;
        private byte[] _message;
        private PWPConnection _connection;
        private Torrent _torrent;
        private static byte[] _piece;
        private static byte[] _partsInPiece;

        public MessageHandler(PWPConnection connection, int messageId, byte[] message)
        {
            _connection = connection;
            _torrent = _connection.localClient.torrentMetaInfo;
            _piece = new byte[_torrent.Info.PieceLength];
            _partsInPiece = new byte[_torrent.Info.PieceLength];
            _partsInPiece.Initialize();

            _messageId = messageId;
            _message = message;
        }

        public void HandleMessage()
        {

            switch (_messageId)
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
                case 5:
                    bitfield();
                    break;
                case 6:
                    ProcessReceivedRequest(_message);
                    break;
                case 7:
                    ProcessReceivedPiece(_message);
                    break;
                case 8:
                    cancle();
                    break;
                default:
                    _connection.closeConnection("Pristigla poruka neodgovarajuceg Id-a");
                    break;
            }
        }

        private void bitfield()
        {
            //throw new NotImplementedException();
        }



        private void cancle()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Obradjuje primljenu poruku piece
        /// </summary>
        /// <param name="payload">tijelo poruke je payload = piece index + block offset + block data</param>
        private void ProcessReceivedPiece(byte[] payload)
        {
            //parsiranje poruke
            var pieceIndexInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, pieceIndexInBytes, 0, 4);
            int pieceIndex = BitConverter.ToInt32(Convertor.ConvertToBigEndian(pieceIndexInBytes), 0);

            var blockOffsetInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, blockOffsetInBytes, 0, 4);
            int blockOffset = BitConverter.ToInt32(Convertor.ConvertToBigEndian(blockOffsetInBytes), 0);

            int blockLength = payload.Length - 8;

            var blockData = new byte[blockLength];
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

                //provjera da li je piece dobar
                SHA1 sha1 = new SHA1Managed();
                byte[] recievedPieceHash = sha1.ComputeHash(_piece);
                var pieceHash = new byte[20];
                Buffer.BlockCopy(_torrent.Info.Pieces, 20 * pieceIndex, pieceHash, 0, 20);
                if (recievedPieceHash.Equals(pieceHash))
                {
                    //skinuti piece je dobar

                    //upisi da je piece primljen

                    //spremi ga u datoteku -- isto kao za citanje u sendPiece
                }
                else
                {
                    //piece nije dobar

                    //upisi da piece nije primljen
                    _piece.Initialize();
                    _partsInPiece.Initialize();
                }

            }
        }


        /// <summary>
        /// Obradjuje primljenu poruku request
        /// </summary>
        /// <param name="payload">tijelo poruke je payload = piece index + block offset + block data</param>
        private void ProcessReceivedRequest(byte[] payload)
        {
            //parsiranje poruke
            //payload = piece index + block offset + block length

            var pieceIndexInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, pieceIndexInBytes, 0, 4);
            int pieceIndex = BitConverter.ToInt32(Convertor.ConvertToBigEndian(pieceIndexInBytes), 0);

            var blockOffsetInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, blockOffsetInBytes, 0, 4);
            int blockOffset = BitConverter.ToInt32(Convertor.ConvertToBigEndian(blockOffsetInBytes), 0);

            var blockLengthInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, blockLengthInBytes, 0, 4);
            int blockLength = BitConverter.ToInt32(Convertor.ConvertToBigEndian(blockLengthInBytes), 0);

            if (blockLength < Math.Pow(2, 17))
            {
                // MessageSender.Piece(pieceIndex, blockOffset, blockLength);
            }
            else
            {
                _connection.closeConnection("Poslan je request sa duljinom bloka vecom od 2 na 17.");
            }

        }

        private void have()
        {
            //throw new NotImplementedException();
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

    }
}
