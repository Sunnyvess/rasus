using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using FairTorrent;
using System.IO;

namespace TorrentClient
{
    class MessageHandler
    {

        private int _messageId;
        private byte[] _message;
        private PWPConnection _connection;
        private Torrent _torrent;

        public MessageHandler(PWPConnection connection, int messageId, byte[] message)
        {
            _connection = connection;
            _torrent = _connection.localClient.torrentMetaInfo;

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
                    have(_message);
                    break;
                case 5:
                    ProcessReceivedBitfield(_message);
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

        private void ProcessReceivedBitfield(byte[] payload)
        {
            int numOfPieces = payload.Length;

            if(_connection.peerPiecesStatus.Length != numOfPieces){
                _connection.closeConnection("Peer poslao bitfield neodgovarajuce duljine!");
            }

            lock(_connection.piecesStatusLocker){
                for (int i = 0; i < numOfPieces ; i++)
                {
                    if (payload[i] == 0)
                    {
                        _connection.peerPiecesStatus[i] = Status.Nema;
                    }
                    else
                    {
                        _connection.peerPiecesStatus[i] = Status.Ima;
                    }
                }
            }
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
            Buffer.BlockCopy(blockData, 0, _connection.PieceData, blockOffset, blockLength);
            //oznacava koji djelovi piecea su stigli
            for (int i = blockOffset; i < blockLength; i++)
                _connection.HaveBytesInPiece[i] = 1;

            bool zeroFound = false;
            foreach (byte b in _connection.HaveBytesInPiece)
            {
                if (b == 0) zeroFound = true;
            }

            if (!zeroFound)
            {
                //skupljen je cijeli piece

                //provjera da li je piece dobar
                SHA1 sha1 = new SHA1Managed();
                byte[] recievedPieceHash = sha1.ComputeHash(_connection.PieceData);
                var pieceHash = new byte[20];
                Buffer.BlockCopy(_torrent.Info.Pieces, 20 * pieceIndex, pieceHash, 0, 20);
                if (recievedPieceHash.Equals(pieceHash))
                {
                    //skinuti piece je dobar
                    //upisi da je piece primljen
                    lock(_connection.localClient.lockerStatusaDjelova){
                        _connection.localClient.pieceStatus[pieceIndex] = Status.Ima;
                    }

                    StorePiece(pieceIndex, _connection.PieceData);

                    //TODO resetirati oznake kaj imamo ?
                }
                else
                {
                    //piece nije dobar
                    //upisi da piece nije primljen
                    _connection.PieceData.Initialize();
                    _connection.HaveBytesInPiece.Initialize();
                }
            }
        }

        private void StorePiece(int pieceIndex, byte[] piece)
        {

            //provjera da li je jedan ili vise fileova u torrentu
            if (_connection.localClient.torrentMetaInfo.Info.GetType().Equals(typeof(SingleFileTorrentInfo)))
            {
                var torrentInfo = (SingleFileTorrentInfo)_torrent.Info;
                int pieceLength = torrentInfo.PieceLength;

                var fileInfo = new System.IO.FileInfo(torrentInfo.File.Path);
                FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);

                int writingOffset = pieceIndex * pieceLength;
                fileStream.Write(piece, writingOffset, pieceLength);
            }
            else
            {
                var torrentInfo = (MultiFileTorrentInfo)_torrent.Info;
                int pieceLength = torrentInfo.PieceLength;

                //trazenje u kojem fileu se nalazi trazeni piece
                int offsetInTorrent = pieceIndex * pieceLength;

                int fileIndex = 0; //index filea u torrentu
                int nextFileOffset = torrentInfo.Files[0].Length;
                while (nextFileOffset < offsetInTorrent)
                {
                    fileIndex++;
                    nextFileOffset += torrentInfo.Files[fileIndex].Length;
                }

                int fileOffset = nextFileOffset - torrentInfo.Files[fileIndex].Length;


                //provjera da li je piece iz jednog filea ili iz vise njih
                if (nextFileOffset > offsetInTorrent + pieceLength)
                {
                    //piece je iz jednog filea

                    var fileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                    FileStream fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read);

                    int writingOffset = offsetInTorrent - fileOffset; //offset u fileu, ne u torrentu

                    fileStream.Read(piece, writingOffset, pieceLength);
                }
                else
                {
                    //piece je iz vise fileova

                    //granice od kud do kud se cita iz kojeg filea
                    int startRadingOffset = offsetInTorrent - fileOffset;
                    int endReadingOffset = nextFileOffset;
                    while (fileOffset < offsetInTorrent + pieceLength)
                    {
                        //citanje iz filea
                        int bytesToWrite = endReadingOffset - startRadingOffset;
                        var tempBuffer = new byte[bytesToWrite];

                        var fileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                        FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);

                        fileStream.Read(tempBuffer, startRadingOffset, bytesToWrite);

                        //priprema za pisanje u  slijedeci file
                        fileIndex++;
                        fileOffset = nextFileOffset;
                        nextFileOffset += torrentInfo.Files[fileIndex].Length;

                        startRadingOffset = endReadingOffset;
                        if (nextFileOffset < offsetInTorrent + pieceLength)
                        {
                            endReadingOffset = nextFileOffset;
                        }
                        else
                        {
                            endReadingOffset = offsetInTorrent + pieceLength;
                        }
                    }
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

                PieceSender pieceSender = new PieceSender(_connection);
                pieceSender.pieceIndex = pieceIndex;
                pieceSender.blockOffset = blockOffset;
                pieceSender.blockLength = blockLength;  
                
                lock(_connection.pieceSenderLocker){
                    _connection.pieceSendingQueue.Enqueue(pieceSender);
                }
            }
            else
            {
                _connection.closeConnection("Poslan je request sa duljinom bloka vecom od 2 na 17.");
            }

        }

        private void have(byte[] payload)
        {
            //payload je index piecea
            int pieceIndex = BitConverter.ToInt32(Convertor.ConvertToBigEndian(payload), 0);

            lock(_connection.piecesStatusLocker){
                _connection.peerPiecesStatus[pieceIndex] = Status.Ima;
            }

            //nisam sigurna da kod ispod nije potreban
            //Status[] hisStatus = _connection.peerPiecesStatus;
            //hisStatus[pieceIndex] = Status.Ima;
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
