using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.IO;

namespace FairTorrent.BEncoder
{
    public static class InfoExtractor
    {
        public static string Encode(Dictionary<string, object> inputDict)
        {
            throw new NotImplementedException();
        }

        private static void decodeInt(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            string integerRecord = "";
            while (message[posInMsg] != 'e')
            {
                integerRecord += (char)message[posInMsg];
                posInMsg++;
            }
            posInMsg++;
        }

        private static void decodeBytes(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            string declaredLength = "";
            while (message[posInMsg] != ':')
            {
                declaredLength += (char)message[posInMsg];
                posInMsg++;
            }
            posInMsg++;

            int intLength = int.Parse(declaredLength);
            if (intLength > 0)
            {
                posInMsg += intLength;
            }
        }

        private static string decodeString(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            string declaredLength = "";
            while (message[posInMsg] != ':')
            {
                declaredLength += (char)message[posInMsg];
                posInMsg++;
            }
            posInMsg++;

            Regex isInteger = new Regex("^(0|[1-9][0-9]*)$", RegexOptions.Compiled);

            if (!isInteger.IsMatch(declaredLength))
                throw new Exception("String length is not integer");

            int intLength = int.Parse(declaredLength);
            string returnString = "";
            if (intLength > 0)
            {
                for (int i = posInMsg; i < posInMsg + intLength; i++)
                    returnString += ((char)message[i]).ToString();
                posInMsg += intLength;
            }
            return returnString;
        }

        private static void decodeList(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            while (message[posInMsg] != 'e')
            {
                decodeRecord(message, ref posInMsg, ref infobuffer);
            }
            posInMsg += 1;
        }

        private static void decodeDict(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            string lastKey = null;
            while (message[posInMsg] != 'e')
            {
                string key = decodeString(message, ref posInMsg, ref infobuffer);
                int dictStartPos = 0;
                if (key == "info") dictStartPos = posInMsg;
                if (lastKey != null && lastKey.CompareTo(key) >= 0)
                    throw new Exception("Dictionary contains duplicate key or is incorrectly sorted");
                lastKey = key;

                // Hack - certain keys are read as byte[] to not corrupt data
                if ((key != "pieces") && (key != "peer id"))
                {
                    decodeRecord(message, ref posInMsg, ref infobuffer);
                }
                else
                {
                    decodeBytes(message, ref posInMsg, ref infobuffer);
                }
                if (key == "info")
                {
                    infobuffer = new byte[posInMsg-dictStartPos];
                    Buffer.BlockCopy(message, dictStartPos, infobuffer, 0, posInMsg - dictStartPos);
                }
            }
            posInMsg += 1;
        }

        private static void decodeRecord(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            byte typeChar = message[posInMsg];

            if (typeChar == 'i')
            {
                posInMsg += 1;
                decodeInt(message, ref posInMsg, ref infobuffer);
            }
            else if (typeChar == 'l')
            {
                posInMsg += 1;
                decodeList(message, ref posInMsg, ref infobuffer);
            }
            else if (typeChar == 'd')
            {
                posInMsg += 1;
                decodeDict(message, ref posInMsg, ref infobuffer);
            }
            else
                decodeString(message, ref posInMsg, ref infobuffer);
        }

        public static byte[] ExtractInfoValue(string path)
        {
            return ExtractInfoValue(File.ReadAllBytes(path));
        }
        
        public static byte[] ExtractInfoValue(byte[] message)
        {
            
            int posInMsg = 0;
            byte[] infobuffer = null;

            //decode message
            try
            {
                decodeRecord(message, ref posInMsg, ref infobuffer);
            }
            catch
            {
                throw new Exception("Error decoding message");
            }

            //check if something got left out
            if (posInMsg != message.Length)
            {
                throw new Exception("Decoder can't decode entire message");
            }

            return infobuffer;
        }
    }
}

