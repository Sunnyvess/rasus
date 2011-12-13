using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using FairTorrent;
using FairTorrent.BEncoder;

namespace TrackerCommunication
{
    public enum TrackerEvents { started, completed, stopped, empty };
    public class TrackerCommunication
    {
        //private Request theRequest = new Request();
        private Response theResponse = new Response();
        private HttpWebRequest request;
        private WebResponse response;
        private bool Stopped = false;
        private string urlTracker;
        private double uploaded;
        private double downloaded;
        private double left;
        private TrackerEvents status;
        private string GetRequest;

        Torrent torrent = new Torrent("D:\\Downloads\\NekiTorrent.torrent");
        public byte[] infoSHA1()
        {
            return SHA1.HashValue(InfoExtractor.ExtractInfoValue("D:\\Downloads\\NekiTorrent.torrent"));
        }
        Peer clientHost = new Peer();

        private void PrepareCommunication()
        {
            urlTracker = torrent.Announce.ToString();
            uploaded = 0;
            downloaded = 0;
            left = torrent.Info.PieceLength;
            // Prepare the Get string
            StringBuilder sb = new StringBuilder();
            sb.Append(urlTracker + "?");
            sb.Append("info_hash=" + Conversions.EscapeString(infoSHA1()));
            sb.Append("&peer_id=" + Conversions.EscapeString(clientHost.PeerID));
            sb.Append("&port=" + clientHost.PeerPort.ToString());
            sb.Append("&uploaded=" + uploaded.ToString());
            sb.Append("&downloaded=" + downloaded.ToString());
            sb.Append("&left=" + left.ToString());
            sb.Append("&event=" + status.ToString());
            sb.Append("&num_peers=0");
            sb.Append("&ip=" + clientHost.PeerIP.ToString());

            GetRequest = sb.ToString();
        }

        private void SendGetToTracker()
        {
            PrepareCommunication();
            if (Stopped)
            {
                status = TrackerEvents.started;
                return;
            }
            status = TrackerEvents.started;
            request = (HttpWebRequest)WebRequest.Create(GetRequest);
            Console.WriteLine(GetRequest);
            try
            {
                    response = request.GetResponse();
                    Stream receiveStream = response.GetResponseStream();
                    Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                    StreamReader sr = new StreamReader(receiveStream, encode);
                    char[] bufferRead = new char[256];
                    Int32 responseLength = sr.Read(bufferRead, 0, 256);
                    byte[] buffer = new byte[responseLength];
                    System.Diagnostics.Debug.Write("Debug:");
                    for (int i = 0; i < responseLength; i++)
                    {
                        buffer[i] = (byte)bufferRead[i];
                        System.Diagnostics.Debug.Write(bufferRead[i]);
                    }
                   // theResponse.ProcessResponse(buffer);
                }
                catch (WebException we)
                {
                    throw new Exception("Pogreška! " + we.Message);
                }
        }


        public void StartTrackerCommunication()
        {
            // Start talking to the Tracker
            SendGetToTracker();
        }

        public void StopTrackerCommunication()
        {
            Stopped = true;
        }

        public Peers GetPeers
        {
            get { return theResponse.peers; }
        }

        public Int32 PeersCount
        {
            get { return theResponse.interval; }
        }

    }
}
