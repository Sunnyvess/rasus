using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackerCommunication
{
    class SHA1
    {
        public const int SHA1SIZE = 20;
        private static System.Security.Cryptography.SHA1 sha = new System.Security.Cryptography.SHA1Managed();

        public static byte[] HashValue(byte[] buffer)
        {
            return sha.ComputeHash(buffer);
        }
    }
}
