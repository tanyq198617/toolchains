using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace ClientCore
{
    public class CoroutineHelper : MonoBehaviour
    {
    }
    
    public class HttpUnityBasedRequest : AbstractHttpRequest
    {
        private float _timeOutTime = -1;
        private float _nextRetryTime = -1;
        private int _sendCounter = 0;
        private long _sendStartTime = 0;
        
        private static CoroutineHelper _coroutineHelper;
        public static CoroutineHelper CoroutineHelper
        {
            get
            {
                if (!_coroutineHelper)
                {
                    var go = new GameObject("HttpUnityBasedRequest_Helper");
                    _coroutineHelper = go.AddComponent<CoroutineHelper>();
                    
                    Object.DontDestroyOnLoad(go);
                }
                return _coroutineHelper;
            }
        }

        private List<Coroutine> _allCoroutine = new List<Coroutine>();
        private List<UnityWebRequestAsyncOperation> _allRequestOperation = new List<UnityWebRequestAsyncOperation>();
        
        public HttpUnityBasedRequest(HttpContent httpContent,
            HttpUrlProvider urlProvider,
            HttpEncoderProvider encoderProvider, HttpDecoderProvider decoderProvider,
            HttpProcessorProvider processorProvider) : base(httpContent, urlProvider, encoderProvider,
            decoderProvider, processorProvider)
        {
            
        }
        
        public override void DoRequestImplement(Hashtable headers, byte[] requestBytes)
        {
            _sendStartTime = CurrentUtcTime;
            Send();
        }

        private void Send()
        {
            _sendCounter++;
            
            if (_sendCounter < _httpContent.RetryTimes)
            {
                _nextRetryTime = Time.realtimeSinceStartup + _httpContent.RetryInterval / 1000.0f;
            }
            else
            {
                _nextRetryTime = -1;
            }
            
            _allCoroutine.Add(CoroutineHelper.StartCoroutine(SendCoroutine()));
        }
        
        private IEnumerator SendCoroutine()
        {
            var sendTime = CurrentUtcTime;
            
            var counter = _sendCounter;
            
            HttpLogUtil.Log($"[HttpManager] {_httpContent.Action} SendAsync CounterId:{counter}");
            
            var url = _urlProvider.Invoke();
            var requestBytes = _httpContent.RequestBytes;
            
            var webRequest = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST, new DownloadHandlerBuffer(), new UploadHandlerRaw(requestBytes));
            
            if (_httpContent.RequestHeader != null)
            {
                var enumerator = _httpContent.RequestHeader.GetEnumerator();
           
                while (enumerator.MoveNext())
                {
                    try
                    {
                        webRequest.SetRequestHeader(enumerator.Key.ToString(), enumerator.Value.ToString());
                    }
                    catch (Exception e)
                    {
                        HttpLogUtil.Log($"Http Header Not Support: {enumerator.Key} {enumerator.Value}");
                    }
                }
            }
            
            var requestOperation = webRequest.SendWebRequest();
            _allRequestOperation.Add(requestOperation);
            
            var currentProgress = 0.0f;
            while (!requestOperation.isDone)
            {
                if (Math.Abs(requestOperation.progress - currentProgress) < 0.001f)
                {
                    if (_timeOutTime < 0)
                    {
                        _timeOutTime = Time.realtimeSinceStartup + _httpContent.TimeOut / 1000.0f;
                    }
                }
                else
                {
                    _timeOutTime = -1;
                }

                currentProgress = requestOperation.progress;
                
                yield return null;
            }

            var receivedTime = CurrentUtcTime;

            HttpLogUtil.Log($"[HttpManager] {_httpContent.Action} ReceivedAsync CounterId:{counter}");

            var responseCode = (int)requestOperation.webRequest.responseCode;
            var data = requestOperation.webRequest.downloadHandler.data;
                        
            // 兼容通讯结果200，但返回内容为空情况，跳过通过重试机制获取结果。
            if (responseCode == 200 && (data == null || data.Length <= 0))
            {
                yield break;
            }
                
            if (!_isDone && !_waitDecode)
            {
                _waitDecode = true;
                
                HttpLogUtil.Log($"[HttpManager] {_httpContent.Action} UseAsync CounterId:{counter}");

                SetResponseData(true, data, responseCode, sendTime, receivedTime);
                
                var allProcessor = _processorProvider();
                foreach (IHttpProcessor processor in allProcessor)
                {
                    if (!processor.ProcessBeforeDecoding(this._httpContent))
                    {
                        break;
                    }
                }
                
                var allDecoder = _decoderProvider();

                var decodeResult = true;
                
                yield return new WaitForAsyncOperation(delegate(object o)
                {
                    //using (new CostTimePrinter($"decode_{_httpContent.Action}", 5))
                    {
                        foreach (IHttpDecoder decoder in allDecoder)
                        {
                            if (!decoder.Decode(this._httpContent))
                            {
                                decodeResult = false;
                                break;
                            }
                        }    
                    }
                    
                    return null;
                }, null);
                
                SetDecodeResult(decodeResult);
                
                MarkRequestDone();
            }
            
            _allRequestOperation.Remove(requestOperation);
        }

        private bool _waitDecode = false;
        
        public override void Tick(float delta)
        {
            if(_waitDecode)
            {
                return;
            }
            
            var currentTime = Time.realtimeSinceStartup;
            
            if (_nextRetryTime > 0 && currentTime > _nextRetryTime)
            {
                Send();
            }
            
            // 请求超时
            if (_timeOutTime > 0 && currentTime > _timeOutTime && !_isDone)
            {
                SetResponseData(false, null, 0, _sendStartTime, 0);
                MarkRequestDone();
                
                var allPostProcessor = _processorProvider.Invoke();
                
                foreach (var processor in allPostProcessor)
                {
                    if (!processor.ProcessAfterTimeout(_httpContent))
                    {
                        break;
                    }
                }
            }
        }

        public override void Dispose()
        {
            foreach (var requestOperation in _allRequestOperation)
            {
                requestOperation.webRequest.Dispose();
            }
            _allRequestOperation.Clear();
            
            foreach (var coroutine in _allCoroutine)
            {
                CoroutineHelper.StopCoroutine(coroutine);
            }
            _allCoroutine.Clear();
        }
    }
}