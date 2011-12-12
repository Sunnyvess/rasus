using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackerCommunication
{
    class PeerID
    {
        private byte[] peerID;

        public PeerID()
        {
            string ferTorrent = "FerTorrent-";
            int index = 0;

            peerID = new byte[20];
            foreach (byte letter in ferTorrent)
                peerID[index++] = letter;
            // Random ID
            for (; index < 20; index++)
                /// TODO: Generar random byte
                peerID[index] = (byte)'A';
        }

        public PeerID(PeerID peerID)
        {
            this.peerID = new byte[20];
            // Copy Id
            this.peerID = (byte[])peerID.peerID.Clone();
        }

        public override string ToString()
        {
            // Return the (20 byte) ID as a string
            return Conversions.ConvertByteArrayToString(peerID);
        }

        public byte[] ID
        {
            get { return this.peerID; }
        }
    }
}
