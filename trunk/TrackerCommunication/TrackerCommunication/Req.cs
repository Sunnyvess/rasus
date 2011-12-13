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

    class Req
    {
        private int requestTimeOut = 1 * 60 * 1000;
        private byte[] response;
        private string urlTracker;
        private double uploaded;
        private double downloaded;
        private double left;
        //private TrackerEvents status;
        private string trackerGetRequest;
        private RequestState requestState;
        private Response res;
        private byte[] trackerResponse;
        private bool trackerFailure;
        private string trackerFailureReason;
        private int requestInterval;


        public static ManualResetEvent allDone = new ManualResetEvent(false);
        //private string path;
        Torrent torrent = new Torrent(@"D:\Downloads\Hurry_Up.torrent");

        //path = "D:\Downloads\Brave_New_World_1of5.torrent";

        public byte[] infoHash()
        {
            return SHA1.HashValue(InfoExtractor.ExtractInfoValue("D:\\Downloads\\Hurry_Up.torrent"));
        }


        Peer clientHost = new Peer();

        private void PrepareTrackerRequest()
        {
            uploaded = 0;
            downloaded = 0;
            left = torrent.Info.PieceLength;
            // Prepare the Get string
            StringBuilder sb = new StringBuilder();
            sb.Append(urlTracker + "?");
            sb.Append("info_hash=" + Conversions.EscapeString(infoHash()));
            sb.Append("&peer_id=" + Conversions.EscapeString(clientHost.PeerID));
            sb.Append("&port=" + clientHost.PeerPort.ToString());
            sb.Append("&uploaded=" + uploaded.ToString());
            sb.Append("&downloaded=" + downloaded.ToString());
            sb.Append("&left=" + left.ToString());
            //sb.Append("&ip=" + clientHost.PeerIP.ToString()); neobavezno
            //sb.Append("&numwant=0"); neobavezno
            //sb.Append("&event=" + status.ToString()); neobavezno
            sb.Append("&compact=0"); // kompaktni(binarni) način ili riječnik (dictionary) način rada
            sb.Append("&no_peer_id=1");
            //sb.Append("&key=123"); neobavezno
            //sb.Append("&trackerid=0"); neobavezno
            

            // Escape the String
            trackerGetRequest = sb.ToString();
        }

        public void SendTrackerGet()
        {
            PrepareTrackerRequest();
            //status = TrackerEvents.started;
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(trackerGetRequest);
            
            Console.WriteLine(trackerGetRequest + "\n");
            requestState = new RequestState();
            // To store the request
            requestState.request = request;
            try
                {
                    // Start the Async request
                    IAsyncResult result = request.BeginGetResponse(new AsyncCallback(EndGetTrackerResponse), requestState);
                    // We need a TimeOut
                    ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, new WaitOrTimerCallback(TrackerGetTimeOut), requestState, requestTimeOut, true);

                    allDone.WaitOne();
                   // requestState.response.Close();
                    trackerResponse = null;
                    trackerFailure = false;
                    trackerFailureReason = string.Empty;
                }
                catch (WebException we)
                {
                    Console.WriteLine("Exception 1:" + we.Message + "\n");
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception 2:" + e.Message + "\n");
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
                StreamReader sr = new StreamReader(requestState.streamResponse);
                
                // Tracker response must have less than 256 bytes.
                char[] bufferRead = new char[512];
                int responseLength = sr.Read(bufferRead, 0, 512);
                trackerResponse = new byte[responseLength];
                for (int i = 0; i < responseLength; i++)
                {
                    trackerResponse[i] = (byte)bufferRead[i];
                }

                ProcessTrackerResponse();
            }
            catch (WebException we)
            {
                Console.WriteLine("Exception 3:" + we.Message + "\n");
            }
            allDone.Set();
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
                ProcessTrackerResponse();
            }
        }


        private void ProcessTrackerResponse()
        {
            // The tracker response is a Dictionary
            if (trackerResponse != null)
            {
                // Create the dictionary
                try
                {
                    response = trackerResponse;
                    // Is there a failure reason ??
                    if (Conversions.ConvertByteArrayToString(response).Contains("failure reason"))
                    {
                        Console.WriteLine("\n" + Conversions.ConvertByteArrayToString(response) + "\n");
                        trackerFailure = true;
                        trackerFailureReason = response.ToString();
                        res.failureReason = trackerFailureReason;
                    }
                    else
                    {      // We have data from the Tracker
                        Console.WriteLine("\n Imamo pozitivan response! :) \n");
                        //requestInterval = ((BEncoder.Integer)response["interval"]).IntegerValue;
                        Console.WriteLine(Conversions.HexByteArrayToString(response) + "\n");
                        BEncoder.Decode(response); //ovo je za desifriranje intervala i liste peerova
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception 4:" + e.Message + "\n");
                }
            }
            
        }


        public void StartTrackerCommunication()
        {
            if (torrent.AnnounceList == null)
            {
                urlTracker = torrent.Announce.ToString();
                SendTrackerGet();
            }
            else
                foreach (string ann in (torrent.AnnounceList))
                {
                    urlTracker = ann.ToString();
                    Console.WriteLine(urlTracker);
                    SendTrackerGet();
                }

        }

       


    }
}
