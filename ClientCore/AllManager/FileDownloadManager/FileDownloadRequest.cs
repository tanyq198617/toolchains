using System;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;

namespace ClientCore
{
    public class FileDownloadRequest : IAsyncRequest
    {
        private bool _cachedIsDone = false;
        public bool IsDone
        {
            get
            {
                if (_requestOperation != null)
                {
                    return _requestOperation.isDone;
                }
                    
                return _cachedIsDone;
            }
        }

        private bool _cachedIsSuccess = false;
        public bool IsSuccess
        {
            get
            {
                if (_fileDownloadHandler != null)
                {
                    return _fileDownloadHandler.IsDownloadSuccess;
                }
                
                return _cachedIsSuccess;
            }
        }

        private string _cachedErrorMessage = "";
        public string ErrorMessage
        {
            get
            {
                if (_requestOperation != null)
                {
                    var webRequest = _requestOperation.webRequest;
                    return string.Format("Remote Url {0}, {1}", _remoteUrl, webRequest.error);
                }

                return _cachedErrorMessage;
            }
        }

        private float _cachedDownloadProgress = 0;
        public float DownloadProgress
        {
            get
            {
                if (_requestOperation != null)
                {
                    return _requestOperation.progress;
                }
                else
                {
                    return _cachedDownloadProgress;
                }
            }
        }

        private string _remoteUrl;
        public string RemoteUrl
        {
            get { return _remoteUrl; }
        }

        private string _filePath;
        public string FilePath
        {
            get { return _filePath; }
        }

        private string _md5;
        public string Md5
        {
            get { return _md5; }
        }
        
        private UnityWebRequestAsyncOperation _requestOperation;
        private FileDownloadHandler _fileDownloadHandler;
        private ObjectPool<FileDownLoadManager.ReceiveBuffer> _receiveBufferPool;
        private FileDownLoadManager.ReceiveBuffer _receiveBuffer;

        private int _priority;

        public int Priority
        {
            set { _priority = value; }
            get { return _priority; }
        }
        
        private Action<IOException> _ioExcetionHandler;
        
        public Action<FileDownloadRequest> DownloadFinishCallback;
        
        public FileDownloadRequest(string remoteUrl, string filePath, int fileDownloadPriority, string md5, ObjectPool<FileDownLoadManager.ReceiveBuffer> receiveBufferPool, Action<IOException> ioExcetionHandler)
        {
            _remoteUrl = remoteUrl;
            _filePath = filePath;
            _priority = fileDownloadPriority;
            _md5 = md5;
            _receiveBufferPool = receiveBufferPool;
            _ioExcetionHandler = ioExcetionHandler;
            
        }

        public void DoRequest()
        {
           //UnityWebRequest.Get(_remoteUrl);
            
            _receiveBuffer = _receiveBufferPool.TakeOut();
            
            var unityWebRequest = new UnityWebRequest(_remoteUrl, UnityWebRequest.kHttpVerbGET,
                _fileDownloadHandler = new FileDownloadHandler(_remoteUrl, _filePath, null, _receiveBuffer.Buffer, _ioExcetionHandler), null);
            
            _fileDownloadHandler.UnityWebRequest = unityWebRequest;
            
            _requestOperation = unityWebRequest.SendWebRequest();
        }
        
        public void Tick(float delta)
        {
            if (IsDone)
            {
                
            }
            else
            {
                if (_fileDownloadHandler != null && _fileDownloadHandler.IsDownloadBlocked)
                {
                    _requestOperation.webRequest.Abort();
                }
            }
        }
        
        public void AfterDone()
        {
            if (DownloadFinishCallback != null)
            {
                DownloadFinishCallback.Invoke(this);
            }
        }

        public void Dispose()
        {
            _cachedIsDone = IsDone;
            _cachedIsSuccess = IsSuccess;
            _cachedErrorMessage = ErrorMessage;
            _cachedDownloadProgress = DownloadProgress;
            
            D.Log("remoteUrl: {0}  filePath: {1}  error: {2}", _remoteUrl, _filePath, _cachedErrorMessage);

            if (_requestOperation != null)
            {
                var webRequest = _requestOperation.webRequest;
                if (webRequest != null)
                {
                    webRequest.Dispose();
                }

                _requestOperation = null;
            }

            if (_fileDownloadHandler != null)
            {
                _fileDownloadHandler.Dispose();
                _fileDownloadHandler.CleanAndStopWrite();
                _fileDownloadHandler = null;
            }

            if (_receiveBuffer != null)
            {
                _receiveBufferPool.TakeBack(_receiveBuffer);
                _receiveBuffer = null;
            }
        }

    }
}

/*
using System;
using System.IO;
using System.Net;
using System.Reflection.Emit;
using LZ4;
using UnityEngine;
using UnityEngine.Networking;

namespace ClientCore
{
    public class FileDownloadRequest : IAsyncRequest
    {
        private bool _cachedIsDone = false;

        public bool IsDone
        {
            get
            {
                if (_webRequest != null)
                {
                    return _webRequest.isDone;
                }

                return _cachedIsDone;
            }
        }

        private bool _cachedIsSuccess = false;

        public bool IsSuccess
        {
            get
            {
                if (_downloadHandlerFile != null && _downloadHandlerFile.isDone)
                {
                    //在判断一下文件是否存在
                    if (File.Exists(_filePath))
                        return true;
                }

                return _cachedIsSuccess;
            }
        }

        private string _cachedErrorMessage = "";

        public string ErrorMessage
        {
            get
            {
                if (_webRequest != null && (_webRequest.isHttpError || _webRequest.isNetworkError))
                {
                    return string.Format("ErrorMessage Remote Url {0}, {1}", _remoteUrl, _webRequest.error);
                }

                return _cachedErrorMessage;
            }
        }

        private float _cachedDownloadProgress = 0;

        public float DownloadProgress
        {
            get
            {
                if (_requestOperation != null)
                {
                    return _requestOperation.progress;
                }
                else
                {
                    return _cachedDownloadProgress;
                }
            }
        }

        private string _remoteUrl;

        public string RemoteUrl
        {
            get { return _remoteUrl; }
        }

        private string _filePath;

        public string FilePath
        {
            get { return _filePath; }
        }

        private string _md5;

        public string Md5
        {
            get { return _md5; }
        }

        private UnityWebRequestAsyncOperation _requestOperation;

        private UnityWebRequest _webRequest;

        private DownloadHandlerFile _downloadHandlerFile;

        //private FileDownloadHandler _fileDownloadHandler;
        //private ObjectPool<FileDownLoadManager.ReceiveBuffer> _receiveBufferPool;
        //private FileDownLoadManager.ReceiveBuffer _receiveBuffer;
        private Action _ioExcetionHandler;

        public Action<FileDownloadRequest> DownloadFinishCallback;

        public FileDownloadRequest(string remoteUrl, string filePath, string md5, Action ioExcetionHandler)
            // ObjectPool<FileDownLoadManager.ReceiveBuffer> receiveBufferPool,
        {
            _remoteUrl = remoteUrl;
            _filePath = filePath;
            _md5 = md5;
            //_receiveBufferPool = receiveBufferPool;
            _ioExcetionHandler = ioExcetionHandler;
        }

        public void DoRequest()
        {
            var fileInfo = new FileInfo(_filePath);
            var directory = fileInfo.Directory;

            if (!directory.Exists)
                directory.Create();

            if (fileInfo.Exists)
                fileInfo.Delete();
            //UnityWebRequest.Get(_remoteUrl);
            //_receiveBuffer = _receiveBufferPool.TakeOut();

            //_fileDownloadHandler = new FileDownloadHandler(_remoteUrl, _filePath, null, _receiveBuffer.Buffer, _ioExcetionHandler);
            _webRequest = new UnityWebRequest(_remoteUrl, UnityWebRequest.kHttpVerbGET);

            var dlh = new DownloadHandlerFile(_filePath);
            dlh.removeFileOnAbort = true;

            _downloadHandlerFile = dlh;

            _webRequest.downloadHandler = dlh;

            //_fileDownloadHandler;

            //_fileDownloadHandler.UnityWebRequest = unityWebRequest;

            _requestOperation = _webRequest.SendWebRequest();

            _requestOperation.completed += OnCompleted;


            D.Log("DoRequest " + _remoteUrl + " " + _filePath);
        }

        private void OnCompleted(AsyncOperation op)
        {
            D.Log(
                string.Format("FileDownloadRequest OnComplete url: {0}, isDonw: {1}, isHttpError: {2}, isNetworkError: {3}, error: {4}", 
                    _remoteUrl, _webRequest.isDone, _webRequest.isHttpError, _webRequest.isNetworkError, _webRequest.error));


            if (DownloadFinishCallback != null)
            {
                DownloadFinishCallback.Invoke(this);
            }
            
            //TODO: 如果磁盘空间不足，要调用 _ioExcetionHandler
            if (_webRequest.isHttpError || _webRequest.isNetworkError)
            {
                var error = _webRequest.error;
                if (error != null && error.ToLower().Contains("disk"))
                {
                    if (_ioExcetionHandler != null)
                        _ioExcetionHandler();
                }
            }
            
            _cachedIsDone = IsDone;
            _cachedIsSuccess = IsSuccess;
            _cachedErrorMessage = ErrorMessage;
            _cachedDownloadProgress = DownloadProgress;
            
            if (_webRequest != null)
            {
                _webRequest.Dispose();
                _webRequest = null;
            }

            if (_downloadHandlerFile != null)
            {
                _downloadHandlerFile.Dispose();
                _downloadHandlerFile = null;
            }
        }

        public void Tick(float delta)
        {
            int percent = (int) (DownloadProgress * 100.0);
            
            //if(_requestOperation.)
            
            //D.Log(_remoteUrl + " Progress: " + percent + "%");
            // if (IsDone)
            // {
            //     D.Log("Tick IsDone " + _remoteUrl);
            //     //TODO: 如果磁盘空间不足，要调用_ioExcetionHandler
            //     
            //     
            //     if (DownloadFinishCallback != null)
            //     {
            //         DownloadFinishCallback.Invoke(this);
            //     }
            // }
            // else
            // {
            //     if (_webRequest.isHttpError || _webRequest.isNetworkError)
            //     {
            //         D.Error("Tick Error " + _remoteUrl + " " + _webRequest.error);
            //         _requestOperation.webRequest.Abort();
            //     }
            //     // if (_fileDownloadHandler != null && _fileDownloadHandler.IsDownloadBlocked)
            //     // {
            //     //     D.Error("Tick IsDownloadBlocked " + _remoteUrl);
            //     //     _requestOperation.webRequest.Abort();
            //     // }
            // }
        }


        public void Dispose()
        {
            

            //D.Log("Dispose remoteUrl: {0}  filePath: {1}  error: {2}", _remoteUrl, _filePath, _cachedErrorMessage);



            // if (_fileDownloadHandler != null)
            // {
            //     _fileDownloadHandler.Dispose();
            //     _fileDownloadHandler.CleanAndStopWrite();
            //     _fileDownloadHandler = null;
            // }

            // if (_receiveBuffer != null)
            // {
            //     _receiveBufferPool.TakeBack(_receiveBuffer);
            //     _receiveBuffer = null;
            // }
        }
    }
}
*/