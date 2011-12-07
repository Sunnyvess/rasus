using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace FairTorrent.BEncoder
{
    public static class InfoExtractor
    {
        public static string Encode(Dictionary<string, object> inputDict)
        {
            throw new NotImplementedException();
        }

        private static int decodeInt(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            string integerRecord = "";
            while (message[posInMsg] != 'e')
            {
                integerRecord += (char)message[posInMsg];
                posInMsg++;
            }
            posInMsg++;

            Regex isInteger = new Regex("^(0|-?[1-9][0-9]*)$", RegexOptions.Compiled);

            if (!isInteger.IsMatch(integerRecord))
                throw new Exception("Integer entry is not integer");

            return int.Parse(integerRecord);
        }

        private static byte[] decodeBytes(byte[] message, ref int posInMsg, ref byte[] infobuffer)
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
            if (intLength > 0)
            {
                byte[] returnBytes = new byte[intLength];
                Buffer.BlockCopy(message, posInMsg, returnBytes, 0, intLength);
                posInMsg += intLength;
                return returnBytes;
            }
            return null;
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

        private static List<object> decodeList(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            List<object> returnList = new List<object>();
            while (message[posInMsg] != 'e')
            {
                object entry = decodeRecord(message, ref posInMsg, ref infobuffer);
                returnList.Add(entry);
            }
            posInMsg += 1;
            return returnList;
        }

        private static Dictionary<string, object> decodeDict(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            Dictionary<string, object> returnDict = new Dictionary<string, object>();
            string lastKey = null;

            while (message[posInMsg] != 'e')
            {
                string key = decodeString(message, ref posInMsg, ref infobuffer);
                int dictStartPos = 0;
                if (key == "info") dictStartPos = posInMsg;
                if (lastKey != null && lastKey.CompareTo(key) >= 0)
                    throw new Exception("Dictionary contains duplicate key or is incorrectly sorted");
                lastKey = key;

                object val;
                // Hack - certain keys are read as byte[] to not corrupt data
                if ((key != "pieces") && (key != "peer id"))
                {
                    val = decodeRecord(message, ref posInMsg, ref infobuffer);
                }
                else
                {
                    val = decodeBytes(message, ref posInMsg, ref infobuffer);
                }
                returnDict.Add(key, val);
                if (key == "info")
                {
                    Buffer.BlockCopy(message, dictStartPos, infobuffer, 0, posInMsg - dictStartPos);
                }
            }
            posInMsg += 1;
            return returnDict;
        }

        private static object decodeRecord(byte[] message, ref int posInMsg, ref byte[] infobuffer)
        {
            byte typeChar = message[posInMsg];

            if (typeChar == 'i')
            {
                posInMsg += 1;
                return decodeInt(message, ref posInMsg, ref infobuffer);
            }
            else if (typeChar == 'l')
            {
                posInMsg += 1;
                return decodeList(message, ref posInMsg, ref infobuffer);
            }
            else if (typeChar == 'd')
            {
                posInMsg += 1;
                return decodeDict(message, ref posInMsg, ref infobuffer);
            }
            else
                return decodeString(message, ref posInMsg, ref infobuffer);
        }

        public static byte[] ExtractInfoValue(byte[] message)
        {
            throw new NotImplementedException();
            
            int posInMsg = 0;
            object decodeResult;
            byte[] infobuffer;

            //decode message
            try
            {
                decodeResult = decodeRecord(message, ref posInMsg, ref infobuffer);
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
        }
    }
}

