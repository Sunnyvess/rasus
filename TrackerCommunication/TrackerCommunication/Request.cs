using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FairTorrent.BEncoder;
using System.Net;
using System.Web;
using System.Threading;
using FairTorrent;
using System.IO;

namespace TrackerCommunication
{
    public enum TrackerEvents { started, completed, stopped, empty };

    class Request
    {
        private int requestTimeOut = 1 * 60 * 1000;
        //private int requestInterval;
        private string urlTracker;
        private double uploaded;
        private double downloaded;
        private double left;
        private TrackerEvents status;
        private string trackerGetRequest;
        private RequestState requestState;
        private byte[] trackerResponse;
        //private string path;
        Torrent torrent = new Torrent(@"D:\Downloads\Brave_New_World_1of5.torrent");
        
        //path = "D:\Downloads\Brave_New_World_1of5.torrent";

        public byte[] infoHash()
        {
            return InfoExtractor.ExtractInfoValue("D:\\Downloads\\Brave_New_World_1of5.torrent");
        }

        
        Peer clientHost = new Peer();

        private void PrepareTrackerRequest()
        {
            status = TrackerEvents.started;
            urlTracker = torrent.Announce.ToString();
            uploaded = 0;
            downloaded = 0;
            left = 100;
            Console.WriteLine(status);
            Console.WriteLine(urlTracker);
            // Prepare the Get string
            StringBuilder sb = new StringBuilder();
            sb.Append(urlTracker + "?");
            sb.Append("info_hash=" + Conversions.EscapeString(infoHash()));
            sb.Append("&peer_id=" + Conversions.EscapeString(clientHost.PeerID));
            sb.Append("&port=" + clientHost.PeerPort.ToString());
            sb.Append("&uploaded=" + uploaded.ToString());
            sb.Append("&downloaded=" + downloaded.ToString());
            sb.Append("&left=" + left.ToString());
            sb.Append("&event=" + status.ToString());
            sb.Append("&num_peers=0");
            sb.Append("&ip=" + clientHost.PeerIP.ToString());

            // Escape the String
            trackerGetRequest = sb.ToString();
            
        }

        public void SendTrackerGet()
        {
            PrepareTrackerRequest();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(trackerGetRequest);
            requestState = new RequestState();
            // To store the request
            requestState.request = request;
            WebResponse response = request.GetResponse();
            try
            {
                // Start the Async request
                IAsyncResult result = request.BeginGetResponse(new AsyncCallback(EndGetTrackerResponse), requestState);

                // We need a TimeOut
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TrackerGetTimeOut), requestState, requestTimeOut, true);

                requestState.streamResponse.Close();
                trackerResponse = null;
            }
            catch (WebException we)
            {
                throw new Exception("Error in GET to tracker. [" + we.Message + "].", we);
            }
        }

        private void TrackerGetTimeOut(object state, bool timedOut)
        {
            if (timedOut)
            {
                // Abort the Get
                WebRequest request = (state as RequestState).request;
                if (request != null)
                    request.Abort();
                // To create the timer
                //requestInterval = 2 * 60 * 1000;
                //ProcessTrackerResponse();
            }
        }

        private void EndGetTrackerResponse(IAsyncResult result)
        {
            requestState = (RequestState)result.AsyncState;
            try
            {
                WebRequest webRequest = requestState.request;
                requestState.response = webRequest.EndGetResponse(result);

                // Get the response
                requestState.streamResponse = requestState.response.GetResponseStream();

                Encoding encode = System.Text.Encoding.GetEncoding("utf-8");
                StreamReader sr = new StreamReader(requestState.streamResponse, encode);
                // Tracker response must have less than 256 bytes.
                char[] bufferRead = new char[512];
                int responseLength = sr.Read(bufferRead, 0, 512);
                trackerResponse = new byte[responseLength];
                for (int i = 0; i < responseLength; i++)
                {
                    trackerResponse[i] = (byte)bufferRead[i];
                }
                //ProcessTrackerResponse();
            }
            catch (WebException we)
            {
                throw new Exception("Error getting response from tracker. [" + we.Message + "].", we);
            }
        }


    }
}
