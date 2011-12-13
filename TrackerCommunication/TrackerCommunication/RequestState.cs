﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

namespace TrackerCommunication
{
    class RequestState
    {
        const int BUFFER_SIZE = 1024;
        //public StringBuilder requestData;
        public byte[] bufferRead;
        public WebRequest request;
        public WebResponse response;
        public Stream streamResponse;

        public RequestState()
        {
            bufferRead = new byte[BUFFER_SIZE];
            //requestData = new StringBuilder();
            request = null;
            streamResponse = null;
        }
    }
}
