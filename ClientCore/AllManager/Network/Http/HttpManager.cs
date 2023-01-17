using System;
using System.Collections;
using System.Collections.Generic;

namespace ClientCore
{
    public delegate List<IHttpEncoder> HttpEncoderProvider();
    public delegate List<IHttpDecoder> HttpDecoderProvider();
    public delegate List<IHttpProcessor> HttpProcessorProvider();
    public delegate string HttpUrlProvider();
    
    public enum SendType
    {
        Sequential,
        Paralle,
    }

    public class HttpManager : IManager, ITicker
    {
        private string _serverUrl = string.Empty;

        private RequestContainer<AbstractHttpRequest> _sequentialContainer =
            new RequestContainer<AbstractHttpRequest>(1, false);

        private RequestContainer<AbstractHttpRequest> _paralleContainer = new RequestContainer<AbstractHttpRequest>(3, false);

        private List<IHttpEncoder> _allEncoder = new List<IHttpEncoder>();
        private List<IHttpDecoder> _allDecoder = new List<IHttpDecoder>();
        private List<IHttpProcessor> _allProcessor = new List<IHttpProcessor>();

        private string GetServerUrl()
        {
            return _serverUrl;
        }

        private List<IHttpEncoder> GetAllEncoder()
        {
            return _allEncoder;
        }

        private List<IHttpDecoder> GetAllDecoder()
        {
            return _allDecoder;
        }

        private List<IHttpProcessor> GetAllProcessor()
        {
            return _allProcessor;
        }

        public void Initialize()
        {

        }

        public void Dispose()
        {
            _sequentialContainer.Dispose();
            _sequentialContainer = null;

            _paralleContainer.Dispose();
            _paralleContainer = null;
        }

        public void Tick(float delta)
        {
            _sequentialContainer.Tick(delta);
            _paralleContainer.Tick(delta);
        }

        public void SetServerUrl(string serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public void SetEncoder(params IHttpEncoder[] allEncoder)
        {
            _allEncoder.Clear();
            _allEncoder.AddRange(allEncoder);
        }

        public void SetDecoder(params IHttpDecoder[] allDecoder)
        {
            _allDecoder.Clear();
            _allDecoder.AddRange(allDecoder);
        }

        public void SetProcessor(params IHttpProcessor[] allProcessor)
        {
            _allProcessor.Clear();
            _allProcessor.AddRange(allProcessor);
        }

        public AbstractHttpRequest FindRequest(Predicate<AbstractHttpRequest> match)
        {
            foreach (var request in _sequentialContainer.AllRunningRequest)
            {
                if (match.Invoke(request))
                {
                    return request;
                }
            }
            
            foreach (var request in _sequentialContainer.AllWaitingRequest)
            {
                if (match.Invoke(request))
                {
                    return request;
                }
            }
            
            foreach (var request in _paralleContainer.AllRunningRequest)
            {
                if (match.Invoke(request))
                {
                    return request;
                }
            }
            
            foreach (var request in _paralleContainer.AllWaitingRequest)
            {
                if (match.Invoke(request))
                {
                    return request;
                }
            }

            return null;
        }
        
        public void SendSequential(HttpContent httpContent, int priority)
        {
            var httpRequest = CreateHttpRequest(httpContent, GetServerUrl, GetAllEncoder, GetAllDecoder,
                GetAllProcessor);
            
            httpRequest.Priority = priority;
            
            _sequentialContainer.AddRequest(httpRequest);
        }

        public void SendParallel(HttpContent httpContent, int priority)
        {
            var httpRequest = CreateHttpRequest(httpContent, GetServerUrl, GetAllEncoder, GetAllDecoder,
                GetAllProcessor);

            httpRequest.Priority = priority;
            
            _paralleContainer.AddRequest(httpRequest);
        }

        // 消息串行发送, 保证依次按序到达
        public void SendSequential(Hashtable requestParameter, int retryTimes = 3, int retryInterval = 5000, int priority = 1)
        {
            var httpContent = new HttpContent();

            httpContent.SendType = SendType.Sequential;
            httpContent.RequestBody = requestParameter;
            httpContent.RetryTimes = retryTimes;
            httpContent.RetryInterval = retryInterval;

            var httpRequest = CreateHttpRequest(httpContent, GetServerUrl, GetAllEncoder, GetAllDecoder,
                GetAllProcessor);

            _sequentialContainer.AddRequest(httpRequest);
        }

        // 消息并行发送,不保证按序到达
        public void SendParallel(Hashtable requestParameter, int retryTimes = 3, int retryInterval = 5000, int priority = 1)
        {
            var httpContent = new HttpContent();

            httpContent.SendType = SendType.Paralle;
            httpContent.RequestBody = requestParameter;
            httpContent.RetryTimes = retryTimes;
            httpContent.RetryInterval = retryInterval;

            var httpRequest = CreateHttpRequest(httpContent, GetServerUrl, GetAllEncoder, GetAllDecoder,
                GetAllProcessor);

            _paralleContainer.AddRequest(httpRequest);
        }
        
        private AbstractHttpRequest CreateHttpRequest(HttpContent httpContent,
            HttpUrlProvider urlProvider, HttpEncoderProvider encoderProvider, HttpDecoderProvider decoderProvider,
            HttpProcessorProvider processorProvider)
        {
#if HTTP_DOTNET_ENABLED
            return new HttpDotNetBasedRequest(httpContent, urlProvider, encoderProvider, decoderProvider, processorProvider);
#else 
            return new HttpUnityBasedRequest(httpContent, urlProvider, encoderProvider, decoderProvider, processorProvider);
#endif
        }

    }
}