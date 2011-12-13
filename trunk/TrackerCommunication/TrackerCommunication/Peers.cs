using System;
using System.Collections;
using System.Linq;
using System.Text;
using FairTorrent.BEncoder;

namespace TrackerCommunication
{
    public class Peers : IEnumerable, IEnumerator
    {
        private ArrayList peers;
        private int index = -1;


        public Peers() : this(0) {
		}
        
        public Peers(int numElements) {
            if (numElements == 0)
                peers = new ArrayList();
            else
                peers = new ArrayList(numElements);
        }

       /* public Peers(BEncoder peers) {
            try {
                // Create the peers from the dictionary.

            }
            catch (Exception) {
                /// TODO log error
                
                this.peers = new ArrayList();
            }
        }*/


        IEnumerator IEnumerable.GetEnumerator() {
            throw new NotImplementedException();
        }

        public object Current {
            get {
                if ((index == -1) || (index >= peers.Count))
                    throw new InvalidOperationException("Index out of bounds.");
                else
                    return peers[index];
            }
        }

        public bool MoveNext() {
            index++;
            return (index < peers.Count);
        }

        public void Reset() {
            index = -1;
        }

        public int PeersNumber {
            get { return peers.Count; }
        }

      /*  public Peer this[int index] {
            get {
                if ((index < 0) || (index >= peers.Count))
                    throw new System.InvalidOperationException("Index before/after elements on container.");
                else
                    return (Peer)peers[index];
            }
        } */
    }
}
