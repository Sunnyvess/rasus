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

        public static void SendHave(int pieceIndex, PWPConnection connection)
        {
            //MessageLength(4) + MessageId(1) + PieceIndex(4)
            int messageLength = 1 + 4;
            byte messageId = 4;
            var message = new byte[messageLength + 4];

            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = messageId;
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(pieceIndex), 0, message, 5, 4);
        }

        public static void SendBitField(Status[] myStatus, PWPConnection connection)
        {
            //MessageLength(4) + MessageId(1) + StatusList
            int numOfPieces = myStatus.Length;
            var statusList = new byte[numOfPieces];

            for (int i = 0; i < numOfPieces; i++)
            {
                if (myStatus[i] == Status.Ima)
                {
                    statusList[i] = 1;
                }
                else 
                {
                    statusList[i] = 0;
                }
            }

            byte messageId = (byte) 5;
            int messageLength = 1 + numOfPieces;
            var message = new byte[messageLength + 4];
            
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = messageId;
            Buffer.BlockCopy(statusList, 0, message, 5, numOfPieces);

            connection.sendMessage(message);
        }
        
        public static void SendRequest(Status[] myStatus, Status[] hisStatus, PWPConnection connection)
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
                if (index == firstIndex )
                {
                    return;
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
