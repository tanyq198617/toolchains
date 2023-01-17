#if HTTP_DOTNET_ENABLED
using System;
using System.Collections;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ClientCore.ReloadModeSupport;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace ClientCore
{
    public class HttpDotNetBasedRequest : AbstractHttpRequest
    {
        [ReloadWithValue(null)] 
        private static HttpClient _httpClient;
        private static HttpClient HttpClient
        {
            get
            {
                if (_httpClient == null)
                {
                    var handler = new HttpClientHandler
                    {
                        Proxy = WebRequest.DefaultWebProxy,
                        UseProxy = true,
                    };

                    _httpClient = new HttpClient(handler);
                }

                return _httpClient;
            }
        }
        
        private ByteArrayContent _byteArrayContent;
        private HttpResponseMessage _httpResponseMessage;
        
        private CancellationTokenSource _cancellationTokenSource;
        
        private float _timeOutTime = -1;
        private float _nextRetryTime = -1;
        private int _sendCounter = 0;
        
        public HttpDotNetBasedRequest(HttpContent httpContent, 
            HttpUrlProvider urlProvider,
            HttpEncoderProvider encoderProvider, HttpDecoderProvider decoderProvider, 
            HttpProcessorProvider processorProvider):base(httpContent, urlProvider, encoderProvider, decoderProvider, processorProvider)
        {
            _httpContent = httpContent;

            _urlProvider = urlProvider;
            
            _encoderProvider = encoderProvider;
            _decoderProvider = decoderProvider;
            
            _processorProvider = processorProvider;
        }
        
        public override void DoRequestImplement(Hashtable headers, byte[] bytes)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            
            _byteArrayContent = new ByteArrayContent(_httpContent.RequestBytes);

            var enumerator = _httpContent.RequestHeader.GetEnumerator();
            while (enumerator.MoveNext())
            {
                _byteArrayContent.Headers.Add(enumerator.Key.ToString(), enumerator.Value.ToString());    
            }

            var currentTime = Time.realtimeSinceStartup;

            _timeOutTime = currentTime + _httpContent.TimeOut / 1000.0f;
            
            RunSendTask();
        }

        private void RunSendTask()
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
            
            Task.Run(SendAsync);
        }
        
        private object _responseLock = new object();
        private bool _responsed = false;
        
        private async Task SendAsync()
        {
            var sendCounter = _sendCounter;

            try
            {
                HttpLogUtil.Log($"[HttpManager] {_httpContent.Action} SendAsync CounterId:{sendCounter}");

                var url = _urlProvider.Invoke();

                HttpResponseMessage httpResponseMessage =
                    await HttpClient.PostAsync(url, _byteArrayContent, _cancellationTokenSource.Token);

                var needProcess = false;

                HttpLogUtil.Log(
                    $"[HttpManager] {_httpContent.Action} ReceivedAsync CounterId:{sendCounter} HttpStatus:{httpResponseMessage.StatusCode}");

                lock (_responseLock)
                {
                    if (!_responsed)
                    {
                        HttpLogUtil.Log($"[HttpManager] {_httpContent.Action} UseAsync CounterId:{sendCounter}");
                        _responsed = true;
                        needProcess = true;
                    }
                }

                if (needProcess)
                {
                    var responseByte = await httpResponseMessage.Content.ReadAsByteArrayAsync();
                    SetResponseData(true, responseByte, (int)httpResponseMessage.StatusCode);
                    MarkRequestDone();
                }
            }
            catch (TaskCanceledException exception)
            {
                HttpLogUtil.Log(
                    $"[HttpManager] {_httpContent.Action} Cancelled CounterId:{sendCounter} {exception.Message} {exception.StackTrace}");

                SetResponseData(false, null, 0);
                MarkRequestDone();
            }
            catch (Exception exception)
            {
                HttpLogUtil.Error(
                    $"[HttpManager] {_httpContent.Action} Exception CounterId:{sendCounter} {exception.Message} {exception.StackTrace}");
                
                SetResponseData(false, null, 0);
                MarkRequestDone();
            }
        }
        
        public override void Tick(float delta)
        {
            var currentTime = Time.realtimeSinceStartup;
            
            if (_nextRetryTime > 0 && currentTime > _nextRetryTime)
            {
                RunSendTask();
            }
            
            // 请求超时
            if (_timeOutTime > 0 && currentTime > _timeOutTime && !_isDone)
            {
                HttpClient.CancelPendingRequests();
                
                SetResponseData(false, null, 0);
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
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public string ErrorMessage { get; }
        public int Priority { get; set; }
    }
}
#endif