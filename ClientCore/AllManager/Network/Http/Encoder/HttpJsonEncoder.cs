using System;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

namespace ClientCore
{
    public class HttpJsonEncoder : IHttpEncoder
    {
        public bool Encode(HttpContent content)
        {
            try
            {
                content.RequestHeader["Content-Type"] = "application/json";
                
                var json = JsonConvert.SerializeObject(content.RequestBody);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                content.RequestBytes = bytes;
                return content.RequestBytes != null;
            }
            catch (Exception exception)
            {
                Debug.LogError(exception);
                return false;
            }
        }
    }
}