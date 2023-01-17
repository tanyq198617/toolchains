using System;
using System.Collections;
using System.Text;
using Pathfinding.Serialization.JsonFx;
using UnityEngine;

namespace ClientCore
{
    public class HttpJsonDecoder : IHttpDecoder
    {
        private static JsonReaderSettings _jsonReadSetting = null;
        private static JsonReaderSettings JsonReadSetting
        {
            get
            {
                if (_jsonReadSetting == null)
                {
                    _jsonReadSetting = new JsonReaderSettings();
                    _jsonReadSetting.UseStringInsteadOfNumber = true;
                }

                return _jsonReadSetting;
            }
        }
        
        public bool Decode(HttpContent content)
        {
            try
            {
                if (content.IsSuccess)
                {
                    var json = Encoding.UTF8.GetString(content.ResponseBytes);

                    JsonReader jReader = new JsonReader(json, JsonReadSetting);

                    content.ResponseBody = jReader.Deserialize() as Hashtable;
                    
                    return content.ResponseBody != null;
                }
                else
                {
                    return false;
                }
            }
            catch(Exception exception)
            {
                Debug.LogError(exception);
                return false;
            }
        }
    }
}