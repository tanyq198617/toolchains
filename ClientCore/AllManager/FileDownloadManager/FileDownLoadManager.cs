using System;
using System.IO;

namespace ClientCore
{
    public enum FileDownloadPriority
    {
        VeryHigh = 0,
        High = 1,
        Normal = 2,
        Low = 3
    }
    
    public class FileDownLoadManager : IManager, ITicker
    {
        private const int DefaultMaxDownloadCount = 3;
        
        private RequestContainer<FileDownloadRequest> _requestContainer = new RequestContainer<FileDownloadRequest>(DefaultMaxDownloadCount);
        public RequestContainer<FileDownloadRequest> RequestContainer
        {
            get { return _requestContainer; }
        }
        
        public class ReceiveBuffer
        {
            private byte[] _buffer = new byte[50*1024];
            public byte[] Buffer { get { return _buffer; }}
        }
        
        private ObjectPool<ReceiveBuffer> _receiveBufferPool = new ObjectPool<ReceiveBuffer>();

        public Action<IOException> IOExceptionCallback = null;

        public void SetRunningLimitCount(int maxCount)
        {
            _requestContainer.SetRunningLimitCount(maxCount);
        }
        
        public void Initialize()
        {
            
        }

        public void Dispose()
        {
            _requestContainer.Dispose();
            IOExceptionCallback = null;
        }

        public void Tick(float delta)
        {
            _requestContainer.Tick(delta);
        }
        
        public FileDownloadRequest DownloadFileAsync(string remoteUrl, string filePath, FileDownloadPriority fileDownloadPriority, string md5 = null)
        {
            D.Log("FileDownloadManager-> {0} remoteUrl: {1}  filePath: {2}", fileDownloadPriority, remoteUrl, filePath);
            var downloadRequest = _requestContainer.TryGetRuningRequest(p => p.RemoteUrl == remoteUrl && p.FilePath == filePath);
            
            if (downloadRequest == null)
            {
                downloadRequest =  _requestContainer.TryGetWaitingRequest(p => p.RemoteUrl == remoteUrl && p.FilePath == filePath);

                if (downloadRequest == null)
                {
                    downloadRequest = new FileDownloadRequest(remoteUrl, filePath, (int)fileDownloadPriority, md5, _receiveBufferPool, OnIOExceptionHappened);
                    _requestContainer.AddRequest(downloadRequest);
                }
                else
                {
                    if ((int)fileDownloadPriority < downloadRequest.Priority)
                    {
                        _requestContainer.UpdateRequestPriority(downloadRequest, (int)fileDownloadPriority);
                    }
                }
            }
            
            return downloadRequest;
        }
        
        public void UpdateDownloadPriority(string remoteUrl, string filePath, FileDownloadPriority priority)
        {
            var downloadRequest = _requestContainer.TryGetWaitingRequest(p => p.RemoteUrl == remoteUrl && p.FilePath == filePath);
            if(downloadRequest != null)
            {
                D.Log($"FileDownloadManager.UpdateDownloadPriority: ${remoteUrl} {filePath} {priority}");
                
                _requestContainer.UpdateRequestPriority(downloadRequest, (int)(priority));
            }
        }
        
        private void OnIOExceptionHappened(IOException exception)
        {
            if (IOExceptionCallback != null)
            {
                IOExceptionCallback.Invoke(exception);
            }
        }
    }
}