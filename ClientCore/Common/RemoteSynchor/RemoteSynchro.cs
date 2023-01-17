using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Debug = UnityEngine.Debug;

namespace ClientCore.RemoteSynchor
{
    public interface IRemoteSynchroListener
    {
        void OnSyncProgressChanged(int successCount, int failedCount, int totalCount);
        void OnSyncFinished(int successCount, int failedCount, int totalCount);
    }
    
    /**
     * 远端文件同步工具
     */
    public class RemoteSynchro
    {
        private IRemoteSynchroListener _listener;

        private bool _localFileMd5Changed = false;
        private Dictionary<string, string> _allLocalFileMd5 = new Dictionary<string, string>();
        private Dictionary<string, string> _allRemoteFileMd5 = new Dictionary<string, string>();
        
        private List<FileDownloadRequest> _allDownloadRequest = new List<FileDownloadRequest>();
        private List<FileDownloadRequest> _allFailedDownloadRequest = new List<FileDownloadRequest>();
        private List<FileDownloadRequest> _allSuccessDownloadRequest = new List<FileDownloadRequest>();
        
        public delegate string LocalPathFunc(string fileName);
        public delegate string RemoteUrlFunc(string fileName, string md5);

        private LocalPathFunc _localPathProvider;
        private RemoteUrlFunc _remoteUrlFuncProvider;

        private WaitForAsyncOperation _saveLocalVersion;
        
        private string LocalVersionFilePath { get { return _localPathProvider("_local_version.json"); }}

        private bool _checkMd5Enabled = false;
        
        public RemoteSynchro(LocalPathFunc localPathProvider, RemoteUrlFunc remoteUrlProvider)
        {
            _localPathProvider = localPathProvider;
            _remoteUrlFuncProvider = remoteUrlProvider;
        }

        public void SetListener(IRemoteSynchroListener listener)
        {
            _listener = listener;
        }
        
        public void AddRemoteFileVersion(string fileName, string md5)
        {
            _allRemoteFileMd5.Add(fileName, md5);
        }
        
        public void StartSync()
        {
            using (new CostTimePrinter("LoadLocalVersionList"))
            {
                LoadLocalVersionList();
            }

            using (new CostTimePrinter("TryRemoveAllObsoletedFile"))
            {
                TryRemoveAllObsoletedFile();
            }

            using (new CostTimePrinter("DownloadAllChangedFile"))
            {
                DownloadAllChangedFile();
            }
        }

        public void SetCheckMd5Enabled(bool checkMd5Enabled)
        {
            _checkMd5Enabled = checkMd5Enabled;
        }
        
        public void GenerateLocalVersion()
        {
            var allLocalVersion = new Dictionary<string, string>();
            
            var localVersionFileInfo = new FileInfo(LocalVersionFilePath);
            
            IOUtility.CreateDirectory(localVersionFileInfo.Directory.FullName);
            
            var allFileInfo = localVersionFileInfo.Directory.GetFiles();

            for (int i = 0; i < allFileInfo.Length; i++)
            {
                var fileInfo = allFileInfo[i];
                
                var md5 = MD5Utlity.ComputeFileMd5(fileInfo.FullName);
                allLocalVersion.Add(fileInfo.Name, md5);
            }
            
            var json = JsonConvert.SerializeObject(allLocalVersion);
            File.WriteAllText(LocalVersionFilePath, json);
        }

        public void GenerateLocalVersionCancelable(CancellationToken cancelToken)
        {
            var allLocalVersion = new Dictionary<string, string>();
            
            var localVersionFileInfo = new FileInfo(LocalVersionFilePath);
            
            IOUtility.CreateDirectory(localVersionFileInfo.Directory.FullName);
            
            var allFileInfo = localVersionFileInfo.Directory.GetFiles();

            for (int i = 0; i < allFileInfo.Length; i++)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    D.Log($"Cancelled {i}/{allFileInfo.Length}");
                    return;
                }
                
                var fileInfo = allFileInfo[i];
                var md5 = MD5Utlity.ComputeFileMd5(fileInfo.FullName);
                allLocalVersion.Add(fileInfo.Name, md5);
            }
            
            var json = JsonConvert.SerializeObject(allLocalVersion);
            File.WriteAllText(LocalVersionFilePath, json);
        }

        private void LoadLocalVersionList()
        {
            if (!File.Exists(LocalVersionFilePath))
            {
                GenerateLocalVersion();
            }

            var json = File.ReadAllText(LocalVersionFilePath);

            _allLocalFileMd5.Clear();
            
            JsonConvert.PopulateObject(json, _allLocalFileMd5);
        }
        
        private void SaveLocalVersionListAsync()
        {
            if (_localFileMd5Changed)
            {
                var localVersionFilePath = LocalVersionFilePath;
                _saveLocalVersion = new WaitForAsyncOperation(delegate(object param)
                {
                    var allLocalFileMd5 = param as Dictionary<string, string>;
                    var json = JsonConvert.SerializeObject(allLocalFileMd5);
                    File.WriteAllText(localVersionFilePath, json);
                    return null;
                }, _allLocalFileMd5);
            }
        }
        
        private void TryRemoveAllObsoletedFile()
        {
            //
            
        }

        private void DownloadAllChangedFile()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            foreach (var keyValuePair in _allRemoteFileMd5)
            {
                var fileName = keyValuePair.Key;
                var fileMd5 = keyValuePair.Value;

                var configFileLocalPath = _localPathProvider(fileName);
                if (!File.Exists(configFileLocalPath) || !CheckLocalFileMd5(fileName, fileMd5))
                {
                    var fileUrl = _remoteUrlFuncProvider(fileName, fileMd5);
                    var filePath = _localPathProvider(fileName);

                    var checkMd5 = _checkMd5Enabled ? fileMd5 : null;
                    var fileDownloadRequest = ManagerFacade.FileDownLoadManager.DownloadFileAsync(fileUrl, filePath, FileDownloadPriority.Normal, checkMd5);
                    
                    fileDownloadRequest.DownloadFinishCallback += delegate(FileDownloadRequest request)
                    {
                        OnDownloadFileFinished(request, fileMd5); 
                    };
                    
                    _allDownloadRequest.Add(fileDownloadRequest);
                }
            }
            
            stopwatch.Stop();
            
            D.Log($"Check Md5 Cost Time: {stopwatch.Elapsed.TotalSeconds}");

            if (_allDownloadRequest.Count <= 0)
            {
                SaveLocalVersionListAsync();
                _listener.OnSyncFinished(0, 0, 0);
            }
        }

        private bool CheckLocalFileMd5(string fileName, string md5)
        {
            string localMd5 = string.Empty;
            if (_allLocalFileMd5.TryGetValue(fileName, out localMd5))
            {
                if (localMd5 == md5)
                {
                    return true;
                }
            }

            if (MD5Utlity.CheckFileMd5(_localPathProvider(fileName), md5))
            {
                UpdateLocalMd5(fileName, md5);
                return true;
            }

            return false;
        }
        
        private void OnDownloadFileFinished(FileDownloadRequest fileDownloadRequest, string md5)
        {
            if (fileDownloadRequest.IsSuccess)
            {
                _allSuccessDownloadRequest.Add(fileDownloadRequest);

                var fileInfo = new FileInfo(fileDownloadRequest.FilePath);
                
                UpdateLocalMd5(fileInfo.Name, md5);
            }
            else
            {
                Debug.LogError($"download failed: {fileDownloadRequest.RemoteUrl}");
                _allFailedDownloadRequest.Add(fileDownloadRequest);
            }

            var successCount = _allSuccessDownloadRequest.Count;
            var failedCount = _allFailedDownloadRequest.Count;
            var count = _allDownloadRequest.Count;
            
            if (_listener != null)
            {
                _listener.OnSyncProgressChanged(successCount, failedCount, count);
                
                if (_allFailedDownloadRequest.Count + _allSuccessDownloadRequest.Count >= _allDownloadRequest.Count)
                {
                    SaveLocalVersionListAsync(); // 异步存储避免主线程卡顿
                    _listener.OnSyncFinished(successCount, failedCount, count);
                }
            }
        }

        private void UpdateLocalMd5(string configFile, string md5)
        {
            if (_allLocalFileMd5.ContainsKey(configFile))
            {
                _allLocalFileMd5[configFile] = md5;
            }
            else
            {
                _allLocalFileMd5.Add(configFile, md5);
            }

            _localFileMd5Changed = true;
        }
    }
    
}