using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using FairTorrent;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace FairTorrent.BEncoder
{
    public static class BEncoder
    {
        public static string Encode(Dictionary<string,object> inputDict)
        {
            throw new NotImplementedException();
        }
        
        private static int decodeInt(byte[] message, ref int posInMsg)
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

        private static byte[] decodeBytes(byte[] message, ref int posInMsg)
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

        private static string decodeString(byte[] message, ref int posInMsg)
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

        private static List<object> decodeList(byte[] message, ref int posInMsg)
        {
            List<object> returnList = new List<object>();
            while (message[posInMsg] != 'e')
            {
                object entry = decodeRecord(message, ref posInMsg);
                returnList.Add(entry);
            }
            posInMsg += 1;
            return returnList;
        }

        private static Dictionary<string,object> decodeDict(byte[] message, ref int posInMsg)
        {
            Dictionary<string, object> returnDict = new Dictionary<string, object>();
            string lastKey = null;

            while (message[posInMsg] != 'e')
            {
                string key = decodeString(message, ref posInMsg);
                if (lastKey != null && lastKey.CompareTo(key) >= 0)
                    throw new Exception("Dictionary contains duplicate key or is incorrectly sorted");
                lastKey = key;

                object val;
                // Hack - certain keys are read as byte[] to not corrupt data
                if ((key != "pieces") && (key != "peer id"))
                {
                    val = decodeRecord(message, ref posInMsg);
                }
                else
                {
                    val = decodeBytes(message, ref posInMsg);
                }
                returnDict.Add(key, val);
            }
            posInMsg += 1;
            return returnDict;
        }

        private static object decodeRecord(byte[] message, ref int posInMsg)
        {
            byte typeChar = message[posInMsg];

            if (typeChar == 'i')
            {
                posInMsg += 1;
                return decodeInt(message, ref posInMsg);
            }
            else if (typeChar == 'l')
            {
                posInMsg += 1;
                return decodeList(message, ref posInMsg);
            }
            else if (typeChar == 'd')
            {
                posInMsg += 1;
                return decodeDict(message, ref posInMsg);
            }
            else
                return decodeString(message, ref posInMsg);
        }

        public static Dictionary<string,object> Decode(string message)
        {
            return Decode(System.Text.Encoding.ASCII.GetBytes(message));
        }

        public static Dictionary<string,object> Decode(byte[] message)
        {
            int posInMsg = 0;
            Dictionary<string, object> resultDict;
            object decodeResult;

            //decode message
            try
            {
                decodeResult = decodeRecord(message, ref posInMsg);
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

            // attempt to cast resulting object as dictionary; if it isn't one, create dictionary and add object as entry
            try
            {
                resultDict = (Dictionary<string, object>)decodeResult;
            }
            catch
            {
                resultDict = new Dictionary<string, object>();
                resultDict.Add("content", decodeResult);
            }

            return resultDict;
        }
    }
}
