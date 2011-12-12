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
        private int requestInterval;
        private string urlTracker;
        private byte[] infoHash;
        private PeerID peerID;
        private string ip;
        private int port;
        private double uploaded;
        private double downloaded;
        private double left;
        private TrackerEvents status;
        private string trackerGetRequest;
        private RequestState requestState;
        private byte[] trackerResponse;
        private bool trackerFailure;
        private string trackerFailureReason;
        private string path;
        Torrent torrent = new Torrent();
        
        public byte[] infoHash()
        {
            return InfoExtractor.ExtractInfoValue(path);
        }


        private void PrepareTrackerRequest()
        {

            urlTracker = torrent.Announce.ToString(); 
            // Prepare the Get string
            StringBuilder sb = new StringBuilder();
            sb.Append(urlTracker + "?");
            sb.Append("info_hash=" + Conversions.EscapeString(infoHash()));
            sb.Append("&peer_id=" + Conversions.EscapeString(peerID.ID));
            sb.Append("&port=" + port.ToString());
            sb.Append("&uploaded=" + uploaded.ToString());
            sb.Append("&downloaded=" + downloaded.ToString());
            sb.Append("&left=" + left.ToString());
            sb.Append("&event=" + status.ToString());
            sb.Append("&num_peers=0");
            sb.Append("&ip=" + ip.ToString());
            // Escape the String
            trackerGetRequest = sb.ToString();
        }

        private void SendTrackerGet()
        {
            PrepareTrackerRequest();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(trackerGetRequest);
            requestState = new RequestState();
            // To store the request
            requestState.request = request;
            try
            {
                // Start the Async request
                IAsyncResult result = request.BeginGetResponse(new AsyncCallback(EndGetTrackerResponse), requestState);

                // We need a TimeOut
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TrackerGetTimeOut), requestState, requestTimeOut, true);

                requestState.response.Close();
                trackerResponse = null;
                trackerFailure = false;
                trackerFailureReason = string.Empty;
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
                requestInterval = 2 * 60 * 1000;
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
