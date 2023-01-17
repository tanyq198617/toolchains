using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
    
namespace ClientCore
{
    
    public class FileDownloadHandler : DownloadHandlerScript
    {
        private string _url;
        private string _filePath;
        private string _md5;
        private FileStream _fileStream;
        private int _fileLength;
        private int _receivedFileLength;
        
        private bool _isDownloadSuccess;
        public bool IsDownloadSuccess
        {
            get { return _isDownloadSuccess; }
        }

        public bool IsDownloadBlocked
        {
            get
            {
                float blockTime = 15.0f;
                if (!IsDownloadSuccess)
                {
                    return Time.time - _lastReceiveTime > blockTime;
                }
                
                return false;
            }
        }
        
        private string TempFilePath
        {
            get { return _filePath + ".temp"; }
        }

        private Action<IOException> _ioExceptionCallback;

        private UnityWebRequest _unityWebRequest;
        public UnityWebRequest UnityWebRequest
        {
            set { _unityWebRequest = value; }
        }
        
        public FileDownloadHandler(string url, string filePath, string md5, byte[] buffer, Action<IOException> ioExceptionCallback) : base(buffer)
        {
            try
            {
                _isDownloadSuccess = false;
                _url = url;
                _filePath = filePath;
                _md5 = md5;
                _lastReceiveTime = Time.time;
                _ioExceptionCallback = ioExceptionCallback;

                _fileLength = -1;

                var fileInfo = new FileInfo(filePath);
                var directory = fileInfo.Directory;

                if (!directory.Exists)
                {
                    directory.Create();
                }

                if (fileInfo.Exists)
                {
                    fileInfo.Delete();
                }

                var tempFilePath = TempFilePath;
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                _fileStream = new FileStream(tempFilePath, FileMode.OpenOrCreate);
            }
            catch (IOException exception)
            {
                if (_ioExceptionCallback != null)
                {
                    _ioExceptionCallback.Invoke(exception);
                }
            }
        }

        public void CleanAndStopWrite()
        {
            try
            {
                if (_fileStream != null)
                {
                    _fileStream.Close();
                    _fileStream = null;

                    if (File.Exists(TempFilePath))
                    {
                        File.Delete(TempFilePath);
                    }
                }
            }
            catch (IOException exception)
            {
                if (_ioExceptionCallback != null)
                {
                    _ioExceptionCallback.Invoke(exception);
                }
            }
        }
        
        protected override void ReceiveContentLength(int contentLength)
        {
            _fileLength = contentLength;
        }
        
        protected float _lastReceiveTime = 0;
        
        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            _lastReceiveTime = Time.time;
            if (data != null && dataLength > 0)
            {
                try
                {
                    // 构造时创建文件流硬盘满异常
                    if (_fileStream == null)
                    {
                        return false;
                    }
                    
                    _fileStream.Write(data, 0, dataLength);

                    _receivedFileLength += dataLength;
                }
                catch (IOException exception)
                {
                    // 
                    if (_ioExceptionCallback != null)
                    {
                        _ioExceptionCallback.Invoke(exception);
                    }
                    
                    return false;
                }
            }
            
            return true;
        }
        
        protected override void CompleteContent()
        {
            try
            {
                _fileStream.Flush();
                _fileStream.Close();
                _fileStream = null;
                
                if (string.IsNullOrEmpty(_unityWebRequest.error))
                {
                    var tempFilePath = TempFilePath;
                    
                    if (string.IsNullOrEmpty(_md5) || MD5Utlity.CheckFileMd5(tempFilePath, _md5))
                    {
                        if (File.Exists(_filePath))
                        {
                            File.Delete(_filePath);
                        }

                        File.Move(tempFilePath, _filePath);
                        _isDownloadSuccess = true;
                    }
                    else
                    {
                        _isDownloadSuccess = false;
                        File.Delete(tempFilePath);
                        Debug.LogError($"Download File Error: CheckMd5 Failed {_md5} - {_url}");
                    }
                }
                else
                {
                    _isDownloadSuccess = false;
                    Debug.LogError(string.Format("Download File Error:{0} {1} {2}", _url, _filePath, _unityWebRequest.error));
                }
            }
            catch (IOException exception)
            {
                if (_ioExceptionCallback != null)
                {
                    _ioExceptionCallback.Invoke(exception);
                }
            }
        }
        
        protected override float GetProgress()
        {
            if (_fileLength <= 0)
            {
                return 0;
            }
            
            return _receivedFileLength / (float) _fileLength;
        }


    }
}