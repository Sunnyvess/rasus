using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TorrentClient
{
    //pretvaranje između stringa i byte[] uz različite encodinge
    class Convertor
    {
        public static byte[] strToByteArrayUTF8(string str)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetBytes(str);
        }

        public static byte[] strToByteArrayUTF16(string str)
        {
            System.Text.UnicodeEncoding encoding = new System.Text.UnicodeEncoding();
            return encoding.GetBytes(str);
        }

        public static byte[] strToByteArrayASCII(string str)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetBytes(str);
        }

        public static string byteArrayToStringUTF8(byte[] array, int startIndex, int length)
        {
            System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetString(array, startIndex, length);
        }

        public static string byteArrayToStringASCII(byte[] array, int startIndex, int length)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return encoding.GetString(array, startIndex, length);
        }

        public static byte[] ConvertToBigEndian(byte[] array)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(array);
            return array;
        }

        public static byte[] ConvertIntToBytes(int number)
        {
            byte[] numInBytes = BitConverter.GetBytes(number);
            ConvertToBigEndian(numInBytes);

            return numInBytes;
        }
    }
}
