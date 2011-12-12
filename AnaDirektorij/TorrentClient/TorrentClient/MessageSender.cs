using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace TorrentClient
{
    //za generiranje svih poruka osim piecea
    class MessageSender
    {
        public void SendReqzest(Status[] myStatus, Status[] hisStatus, PWPConnection connection)
        {
            //izgled request poruke:
            //MessageLength(4) + MessageId(1) + PieceIndex(4) + BlockOffset(4) + BlockLength(4)


            //TODO: dodaj lock za pristupanje statusima
            int numOfPieces = myStatus.Length;
            int pieceLength = connection.localClient.torrentMetaInfo.Info.PieceLength;
            NetworkStream stream = connection.clientStream;
            //int blockLength = client.lockLength;

            //odabir koji piece cemo traziti
            Random rand = new Random();
            int firstIndex = rand.Next(numOfPieces);
            int index = firstIndex;
            while (!(myStatus[index] == Status.Nema && hisStatus[index] == Status.Ima))
            {
                index = (index + 1) % numOfPieces;
                
                //nije potrebno al nek ima
                if (index == firstIndex - 1)
                {
                    break;
                }
            }

            int messageLength = 13;
            int messageId = 6;
            int pieceIndex = index;
            //TODO: int blockLength = client.lockLength;
            int blockLength = (int)Math.Pow(2, 14);
            var message = new byte[messageLength + 4];

            int blockOffset = 0;
            while(blockOffset + blockLength < pieceLength)
            {
                //generiraj poruku
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageId), 0, message, 4, 1);
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(pieceIndex), 0, message, 5, 4);
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockOffset), 0, message, 9, 4);
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockLength), 0, message, 13, 4);

                //posajli
                connection.sendMessage(message);
                
                blockOffset += blockLength;
            }

            //izracunaj blockLength
            blockLength = pieceLength - blockOffset;
            //generiraj poruku
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageId), 0, message, 4, 1);
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(pieceIndex), 0, message, 5, 4);
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockOffset), 0, message, 9, 4);
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockLength), 0, message, 13, 4);
            
            //posalji
            connection.sendMessage(message);
        }

    }

}
