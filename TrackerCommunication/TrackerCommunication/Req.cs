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
        private string trackerGetRequest;
        private RequestState requestState;
        private byte[] trackerResponse;


        public static ManualResetEvent allDone = new ManualResetEvent(false);
        Torrent torrent = new Torrent("C:\\Users\\Toma\\Downloads\\Money_as_Debt_(2006)__DVDR(xvid)__NL_Subs__DMT.6763327.TPB.torrent");

        public byte[] infoHash()
        {
            return SHA1.HashValue(InfoExtractor.ExtractInfoValue("C:\\Users\\Toma\\Downloads\\Money_as_Debt_(2006)__DVDR(xvid)__NL_Subs__DMT.6763327.TPB.torrent"));
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
            //sb.Append("&ip=" + clientHost.PeerIP.ToString()); optional
            //sb.Append("&numwant=0"); optional
            //sb.Append("&event=" + status.ToString()); optional
            sb.Append("&compact=1"); // compact(binary) or dictionary mode
            //sb.Append("&no_peer_id=1");
            //sb.Append("&key=123"); optional
            //sb.Append("&trackerid=0"); optional
            

            // Escape the String
            trackerGetRequest = sb.ToString();
        }

        public void SendTrackerGet()
        {
            PrepareTrackerRequest();
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
                    trackerResponse = null;
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
                ProcessTrackerResponse();
            }
        }


        private void ProcessTrackerResponse()
        {

            if (trackerResponse != null)
            {
                try
                {
                    response = trackerResponse;
                    // Is there a failure reason ??
                    if (Conversions.ConvertByteArrayToString(response).Contains("failure reason"))
                    {
                        Response res = new Response();
                        Console.WriteLine("\n" + Conversions.ConvertByteArrayToString(response) + "\n");
                        res.Failure.failureReason = response.ToString();
                    }
                    else
                    {      // We have data from the Tracker
                        Console.WriteLine("\n Imamo pozitivan response! :) \n");
                        Console.WriteLine(Conversions.ConvertByteArrayToString(response));
                        string re = Conversions.ConvertByteArrayToString(response);
                        BEncoder.Decode(re);
                        Response res = new Response(response);//decode and get interval and peer list
                        Console.WriteLine(res + "\n");
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
                Console.WriteLine(urlTracker);
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
