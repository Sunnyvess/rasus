using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using FairTorrent;

namespace TrackerCommunication
{
    class URLencode
    {
        private string theString;
        private byte[] beEncoded;

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            byte[] beEncode = this.Encode();
            // Remove the length
            int actualPos = 0;
            while (beEncode[actualPos] != (char)':')
                actualPos++;
            // Remove ':'
            actualPos++;
            for (int i = actualPos; i < beEncode.Length; i++)
                sb.Append((char)beEncode[i]);
            return sb.ToString();
        }

        public byte[] Encode()
        {
            if (theString == null)
                throw new Exception("The string is null.");
            int stringLength = theString.Length.ToString().Length;
            int index = 0;
            beEncoded = new byte[theString.Length + stringLength + 1];
            for (; index < stringLength; index++)
                beEncoded[index] = (byte)theString.Length.ToString()[index];
            beEncoded[index++] = (byte)':';
            for (int i = 0; i < theString.Length; i++)
                beEncoded[index++] = (byte)theString[i];
            return beEncoded;
        }
   
    }
}
