using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using FairTorrent;

namespace TorrentClient
{
    //za generiranje svih poruka osim piecea
    class MessageSender
    {

		public static void sendChoke(PWPConnection connection)
        {
            //MessageLength(4) + MessageId(1)
            int messageLength = 1;
            byte messageId = 0;

            var message = new byte[messageLength + 4];
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = messageId;

            connection.sendMessage(message);

            connection.connectionState.amChoking = true;
            
            //Brisanje svih zahtjeva iz liste zahtjeva peera
            connection.pieceSendingList.Clear();
        }

        public static void sendUnchoke(PWPConnection connection)
        {
            //MessageLength(4) + MessageId(1)
            int messageLength = 1;
            byte messageId = 1;

            var message = new byte[messageLength + 4];
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = messageId;

            connection.sendMessage(message);

            connection.connectionState.amChoking = false;
        }

        public static void sendInterested(PWPConnection connection)
        {
            //MessageLength(4) + MessageId(1)
            int messageLength = 1;
            byte messageId = 2;

            var message = new byte[messageLength + 4];
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = messageId;

            connection.sendMessage(message);

            connection.connectionState.amInterested = true;
        }

        public static void sendUninterested(PWPConnection connection)
        {
            //MessageLength(4) + MessageId(1)
            int messageLength = 1;
            byte messageId = 3;

            var message = new byte[messageLength + 4];
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = messageId;

            connection.sendMessage(message);

            connection.connectionState.peerInterested = false;
        }
		
        public static void SendHave(int pieceIndex, PWPConnection connection)
        {
            //MessageLength(4) + MessageId(1) + PieceIndex(4)
            int messageLength = 1 + 4;
            byte messageId = 4;
            var message = new byte[messageLength + 4];

            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = messageId;
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(pieceIndex), 0, message, 5, 4);

            connection.sendMessage(message);
        }

        public static void SendBitField(Status[] myStatus, PWPConnection connection)
        {
            //MessageLength(4) + MessageId(1) + StatusList
            int numOfPieces = myStatus.Length;
            int bitfieldLength = numOfPieces / 8 + 1;
            var statusList = new byte[bitfieldLength];

            for (int i = 0; i < bitfieldLength; i++)
            {
                byte bit;
                statusList[i] = 0;
                for (int j = 0; j < 8; j++)
                {
                    if (i * 8 + j < numOfPieces)
                    {
                        bit = myStatus[i] == Status.Ima ? (byte)1 : (byte)0;
                    }
                    else
                    {
                        //na kraj upisuj nule
                        bit = 0;
                    }

                    //upisi bit
                    statusList[i] = (byte)((statusList[i] << 1) | bit);
                }
            }

            byte messageId = 5;
            int messageLength = 1 + bitfieldLength;
            var message = new byte[messageLength + 4];

            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = messageId;
            Buffer.BlockCopy(statusList, 0, message, 5, bitfieldLength);

            connection.sendMessage(message);
        }
        
        public static void SendRequest(Status[] myStatus, Status[] hisStatus, PWPConnection connection)
        {
            //izgled request poruke:
            //MessageLength(4) + MessageId(1) + PieceIndex(4) + BlockOffset(4) + BlockLength(4)

            //provjeri jel smo već u procesu skidanja nekog pieca :)
            //ako jesmo, šibaj s njim dalje, ako nismo traži novi

            int numOfPieces = myStatus.Length;
            int pieceLength = connection.localClient.torrentMetaInfo.Info.PieceLength;
            NetworkStream stream = connection.clientStream;
            //int blockLength = client.lockLength;

            //odabir koji piece cemo traziti
            Random rand = new Random();
            int firstIndex = rand.Next(numOfPieces);
            int index = firstIndex;

            //cijela metoda se poziva pod lockom nad statusima
            while (!(myStatus[index] == Status.Nema && hisStatus[index] == Status.Ima))
            {
                
                index = (index + 1) % numOfPieces;
                
                //nije potrebno al nek ima
                if (index == firstIndex )
                {
                    return;
                }
            }



            //označi da si si bezeciral piece :D
            lock(connection.localClient.lockerStatusaDjelova){
                connection.localClient.pieceStatus[index] = Status.Skidanje;          
            }

            lock(connection.lockerDohvacenihPodataka){
                connection.pieceIndexDownloading = index;
            }

            //zadnji piece monje biti i manji!!
            if(index == numOfPieces-1){
                int fileLength = ((SingleFileTorrentInfo) connection.localClient.torrentMetaInfo.Info).File.Length;
                int newPieceLength = fileLength - (numOfPieces - 1) * pieceLength;
                if(newPieceLength != 0){
                    pieceLength = newPieceLength;
                }
            }

            lock(connection.lockerDohvacenihPodataka){
                connection.PieceData = new byte[pieceLength];
                connection.HaveBytesInPiece = new byte[pieceLength];
                connection.HaveBytesInPiece.Initialize();
            }
 
            int messageLength = 13;
            int messageId = 6;
            int pieceIndex = index;

            //TODO: int blockLength = client.lockLength; //ani nije jasno kaj ovo znaci, samo da se duljina bloka uzima izvana?

            int blockLength = (int)Math.Pow(2, 14); 
            var message = new byte[messageLength + 4];

            int blockOffset = 0;
            while(blockOffset + blockLength < pieceLength)
            {
                //generiraj poruku
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
                message[4]= (byte) messageId;
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(pieceIndex), 0, message, 5, 4);
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockOffset), 0, message, 9, 4);
                Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockLength), 0, message, 13, 4);

                //posajli
                connection.sendMessage(message);
                
                blockOffset += blockLength;
            }

            //izracunaj blockLength zadnji!
            blockLength = pieceLength - blockOffset;

            //generiraj poruku
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(messageLength), 0, message, 0, 4);
            message[4] = (byte)messageId;
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(pieceIndex), 0, message, 5, 4);
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockOffset), 0, message, 9, 4);
            Buffer.BlockCopy(Convertor.ConvertIntToBytes(blockLength), 0, message, 13, 4);
            
            //posalji
            connection.sendMessage(message);
        }

    }

}
