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
                    ProcessReceivedChoke();
                    break;
                case 1:
                    ProcessReceivedUnchoke();
                    break;
                case 2:
                    ProcessReceivedInterested();
                    break;
                case 3:
                    ProcessReceivedUninterested();
                    break;
                case 4:
                    ProcessReceivedHave(_message);
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
                    ProcessReceivedCancel(_message);
                    break;
                default:
                    _connection.closeConnection("Pristigla poruka neodgovarajuceg Id-a");
                    break;
            }
        }

        private void ProcessReceivedBitfield(byte[] payload)
        {
            int numOfPieces = _connection.peerPiecesStatus.Length;
            
            //u payloadu je jedan piece reprezentian jednim bitom, a kod nas jednim bytom
            //prebacujemo bitArray to ByteArray
            var hisBitefield = new byte[numOfPieces];
            
            //za svaki bajt
            for (int i = 0; i < payload.Length; i++)
            {
                //za svaki bit u bajtu
                for(int j = 0; j < 8; j++)
                {
                    if(i*8 + j >= numOfPieces)
                        break;

                    if ((payload[i] & 1 << j) == 0)
                    {
                        hisBitefield[i*8 + j] = 0;
                    }
                    else
                    {
                        hisBitefield[i*8 + j] = 1;
                    }
                }
            }

            lock(_connection.piecesStatusLocker){
                for (int i = 0; i < numOfPieces ; i++)
                {
                    if (hisBitefield[i] == 0)
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



        private void ProcessReceivedCancel(byte[] payload)
        {
            //parsiranje isto kao za request
            var pieceIndexInBytes = new byte[4];
            Buffer.BlockCopy(payload, 0, pieceIndexInBytes, 0, 4);
            int pieceIndex = BitConverter.ToInt32(Convertor.ConvertToBigEndian(pieceIndexInBytes), 0);

            var blockOffsetInBytes = new byte[4];
            Buffer.BlockCopy(payload, 4, blockOffsetInBytes, 0, 4);
            int blockOffset = BitConverter.ToInt32(Convertor.ConvertToBigEndian(blockOffsetInBytes), 0);

            var blockLengthInBytes = new byte[4];
            Buffer.BlockCopy(payload, 8, blockLengthInBytes, 0, 4);
            int blockLength = BitConverter.ToInt32(Convertor.ConvertToBigEndian(blockLengthInBytes), 0);

            //trazenje requesta
            foreach (PieceSender request in _connection.pieceSendingList)
            {
                if (request.pieceIndex == pieceIndex && request.blockOffset == blockOffset && request.blockLength == blockLength)
                {
                    _connection.pieceSendingList.RemoveAll(p => p.Equals(request));
                    break;
                }
            }
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

            lock(_connection.localClient.lockerStatusaDjelova){
                if (_connection.localClient.pieceStatus[pieceIndex] == Status.Ima){
                    return;
                }
            }

            var blockOffsetInBytes = new byte[4];
            Buffer.BlockCopy(payload, 4, blockOffsetInBytes, 0, 4);
            int blockOffset = BitConverter.ToInt32(Convertor.ConvertToBigEndian(blockOffsetInBytes), 0);

            int blockLength = payload.Length - 8;

            var blockData = new byte[blockLength];
            Buffer.BlockCopy(payload, 8, blockData, 0, blockLength);

            bool zeroFound;
            lock(_connection.lockerDohvacenihPodataka){
                //sprema blok u piece
                Buffer.BlockCopy(blockData, 0, _connection.PieceData, blockOffset, blockLength);
                //oznacava koji djelovi piecea su stigli
                for (int i = 0 ; i < blockLength; i++)
                    _connection.HaveBytesInPiece[i + blockOffset] = 1;

                zeroFound = false;

                foreach (byte b in _connection.HaveBytesInPiece)
                {
                    if (b == 0) zeroFound = true;
                }

            }

            if (!zeroFound)
            {
                //skupljen je cijeli piece

                //provjera da li je piece dobar
                SHA1 sha1 = new SHA1Managed();
                byte[] recievedPieceHash = sha1.ComputeHash(_connection.PieceData);

                var localPieceHash = new byte[20];
                Buffer.BlockCopy(_torrent.Info.Pieces, 20 * pieceIndex, localPieceHash, 0, 20);

                bool areEqual = localPieceHash.SequenceEqual(recievedPieceHash);

                if (areEqual)
                {
                    //skinuti piece je dobar
                    StorePiece(pieceIndex, _connection.PieceData);

                    //upisi da je piece primljen
                    lock (_connection.localClient.lockerStatusaDjelova)
                    {
                        _connection.localClient.pieceStatus[pieceIndex] = Status.Ima;
                    }

                    logPieceRecival(pieceIndex);
                }
                else
                {
                    //piece nije dobar - odustajemo od skidanja za sada
                    lock(_connection.localClient.lockerStatusaDjelova){
                        _connection.localClient.pieceStatus[pieceIndex] = Status.Nema;
                    }      
                }

                //nakon primljenog citavog paketa, bio on ispravan ili ne pocinjemo ispocetka
                lock (_connection.lockerDohvacenihPodataka)
                {
                    _connection.PieceData = null;
                    _connection.HaveBytesInPiece = null;

                    _connection.pieceIndexDownloading = -1;
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
                lock(_connection.localClient.dataStoringLocker){
                    FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);

                    int writingOffset = pieceIndex * pieceLength;

                    try{
                        fileStream.Seek(writingOffset, SeekOrigin.Begin);
                        fileStream.Write(piece, 0, piece.Length);
                    }
                    finally{
                        fileStream.Close();
                    }
                }
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
                    FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Read);

                    int writingOffset = offsetInTorrent - fileOffset; //offset u fileu, ne u torrentu
                    lock(_connection.localClient.dataStoringLocker){
                        try
                        {
                            fileStream.Seek(writingOffset, SeekOrigin.Begin);
                            fileStream.Write(piece, 0, piece.Length);
                        }
                        finally
                        {
                            fileStream.Close();
                        }
                    }
                }
                else
                {
                    //piece je iz vise fileova

                    //granice od kud do kud se pise u koji file
                    int startWritingOffset = offsetInTorrent - fileOffset;
                    int endWritingOffset = nextFileOffset;
                    while (fileOffset < offsetInTorrent + pieceLength)
                    {
                        //citanje iz filea
                        int bytesToWrite = endWritingOffset - startWritingOffset;
                        var tempBuffer = new byte[bytesToWrite];

                        var fileInfo = new System.IO.FileInfo(torrentInfo.Files[fileIndex].Path);
                        FileStream fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);
                        lock(_connection.localClient.dataStoringLocker){
                            try
                            {
                                fileStream.Seek(startWritingOffset, SeekOrigin.Begin);
                                fileStream.Write(tempBuffer, 0, bytesToWrite);
                            }
                            finally
                            {
                                fileStream.Close();
                            }
                        }

                        //priprema za pisanje u  slijedeci file
                        fileIndex++;
                        fileOffset = nextFileOffset;
                        nextFileOffset += torrentInfo.Files[fileIndex].Length;

                        startWritingOffset = endWritingOffset;
                        if (nextFileOffset < offsetInTorrent + pieceLength)
                        {
                            endWritingOffset = nextFileOffset;
                        }
                        else
                        {
                            endWritingOffset = offsetInTorrent + pieceLength;
                        }
                    }
                }
            }
        }

        private void logPieceRecival(int pieceIndex){
            lock(_connection.localClient.logCreatingLocker){
                try{
                    TextWriter logWriter = File.AppendText(_connection.localClient.logFilePath);
                    logWriter.WriteLine(pieceIndex.ToString());
                    logWriter.Close();
                }catch{
                    //ozbiljni problemi! ovo bolje da se ne dogodi
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
            Buffer.BlockCopy(payload, 4, blockOffsetInBytes, 0, 4);
            int blockOffset = BitConverter.ToInt32(Convertor.ConvertToBigEndian(blockOffsetInBytes), 0);

            var blockLengthInBytes = new byte[4];
            Buffer.BlockCopy(payload, 8, blockLengthInBytes, 0, 4);
            int blockLength = BitConverter.ToInt32(Convertor.ConvertToBigEndian(blockLengthInBytes), 0);

            if (blockLength < Math.Pow(2, 17))
            {

                PieceSender pieceSender = new PieceSender(_connection);
                pieceSender.pieceIndex = pieceIndex;
                pieceSender.blockOffset = blockOffset;
                pieceSender.blockLength = blockLength;  
                
                lock(_connection.pieceSenderLocker){
                    _connection.pieceSendingList.Insert(0, pieceSender);
                }
            }
            else
            {
                _connection.closeConnection("Poslan je request sa duljinom bloka vecom od 2 na 17.");
            }

        }

        private void ProcessReceivedHave(byte[] payload)
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

        private void ProcessReceivedUninterested()
        {
            _connection.connectionState.peerInterested = false;

        }

        private void ProcessReceivedInterested()
        {
            _connection.connectionState.peerInterested = true;
        }

        private void ProcessReceivedUnchoke()
        {
            _connection.connectionState.peerChoking = false;
            
        }

        private void ProcessReceivedChoke()
        {
            _connection.connectionState.peerChoking = true;
        }

    }
}
