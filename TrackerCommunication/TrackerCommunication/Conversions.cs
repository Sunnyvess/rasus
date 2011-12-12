using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TrackerCommunication
{
    class Conversions
    {
        public static string EscapeString(byte[] str)
        {
            StringWriter sw = new StringWriter();
            foreach (byte chr in str)
            {
                if ((chr > 127) || (chr < 42))
                    sw.Write(Uri.HexEscape((char)chr));
                else
                    sw.Write((char)chr);
            }
            sw.Close();
            return sw.ToString();
        }

        public static byte[] ConvertStringToByteArray(string sourceString)
        {
            return System.Text.Encoding.ASCII.GetBytes(sourceString);
        }

        public static string ConvertByteArrayToString(byte[] byteArray)
        {
            return System.Text.ASCIIEncoding.ASCII.GetString(byteArray);
        }

        public static int GetByteCount(string hexString)
        {
            int numHexChars = 0;
            char c;
            for (int i = 0; i < hexString.Length; i++)
            {
                c = hexString[i];
                if (IsHexDigit(c))
                    numHexChars++;
            }
            if (numHexChars % 2 != 0)
            {
                numHexChars--;
            }
            return numHexChars / 2; 
        }

        public static byte[] GetBytes(string hexString, out int discarded)
        {
            discarded = 0;
            string newString = "";
            char c;

            for (int i = 0; i < hexString.Length; i++)
            {
                c = hexString[i];
                if (IsHexDigit(c))
                    newString += c;
                else
                    discarded++;
            }

            if (newString.Length % 2 != 0)
            {
                discarded++;
                newString = newString.Substring(0, newString.Length - 1);
            }

            int byteLength = newString.Length / 2;
            byte[] bytes = new byte[byteLength];
            string hex;
            int j = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                hex = new String(new Char[] { newString[j], newString[j + 1] });
                bytes[i] = StringHexToByte(hex);
                j = j + 2;
            }
            return bytes;
        }

        public static string HexByteArrayToString(byte[] bytes)
        {
            string hexString = "";
            for (int i = 0; i < bytes.Length; i++)
            {
                hexString += bytes[i].ToString("X2");
            }
            return hexString;
        }


        public static bool InHexFormat(string hexString)
        {
            bool hexFormat = true;

            foreach (char digit in hexString)
            {
                if (!IsHexDigit(digit))
                {
                    hexFormat = false;
                    break;
                }
            }
            return hexFormat;
        }

        public static bool IsHexDigit(Char c)
        {
            int numChar;
            int numA = Convert.ToInt32('A');
            int num1 = Convert.ToInt32('0');
            c = Char.ToUpper(c);
            numChar = Convert.ToInt32(c);
            if (numChar >= numA && numChar < (numA + 6))
                return true;
            if (numChar >= num1 && numChar < (num1 + 10))
                return true;
            return false;
        }

        private static byte StringHexToByte(string hex)
        {
            if (hex.Length > 2 || hex.Length <= 0)
                throw new ArgumentException("hex must be 1 or 2 characters in length");
            byte newByte = byte.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return newByte;
        }
    }
}
