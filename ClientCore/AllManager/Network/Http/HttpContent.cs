using System;
using System.Collections;
using UnityEngine;

namespace ClientCore
{
    public class HttpContent
    {
        public Action<bool, object> Callback;
        public string Action;
        public SendType SendType;
        public Hashtable RequestHeader = new Hashtable();
        public int RetryTimes;
        public int RetryInterval;
        public int HttpStatus;
        public bool BlockScreen;

        public bool DecodeSuccess;
        
        public bool IsSuccess => (HttpStatus == 200 && ResponseBytes != null);

        public int TimeOut
        {
            get { return Mathf.Max(RetryTimes * RetryInterval, 15000); }
        }
        
        public Hashtable RequestBody = null;
        public Hashtable ResponseBody = null;
        
        public byte[] RequestBytes { get; set; }
        public byte[] ResponseBytes { get; set; }

        public long SendUtcTime;
        public long ReceiveUtcTime;
        public long Latency;

        public string RequestJson;
        public string ResponseJson;
    }
}