using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using ClientCore;
using ClientCore.RemoteSynchor;
using Config2;
using Newtonsoft.Json;

namespace GameLoading.LoadingStep
{
    /**
     * 更新配置表文件
     */
    public class UpdateConfigFileStep : LoadingPipelineStep, IRemoteSynchroListener
    {
        private string _configFileFolder;
        private string ConfigFileFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_configFileFolder))
                {
                    _configFileFolder = Path.Combine(Application.persistentDataPath, "GameAssets/Config");
                }

                return _configFileFolder;
            }
        }

        private string _configVersionFolder;
        private string ConfigVersionFolder
        {
            get
            {
                if (string.IsNullOrEmpty(_configVersionFolder))
                {
                    _configVersionFolder = Path.Combine(Application.persistentDataPath, "GameAssets/ConfigVersion");
                }

                return _configVersionFolder;
            }
        }

        private string _remoteVersion;
        private string RemoteVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_remoteVersion))
                {
                    _remoteVersion = NetApi.inst.ConfigFilesVersion;
                }

                return _remoteVersion;
            }
        }
        
        private string RemoteVersionCacheFileForJson
        {
            get { return Path.Combine(ConfigVersionFolder, $"json_{RemoteVersion}.json"); }
        }
        
        private string RemoteVersionCacheFileForBin
        {
            get { return Path.Combine(ConfigVersionFolder, $"bin_{RemoteVersion}.json"); }
        }
        
        private List<FileDownloadRequest> _allDownloadRequest = new List<FileDownloadRequest>();
        
        private string _description = String.Empty;
        
        public override string Description
        {
            get { return _description; }
        }
        
        public UpdateConfigFileStep(int step, string descriptionKey):base(step, descriptionKey)
        {
        }
        
        public override void OnStart()
        {
            base.OnStart();
            
            CrashReportCustomField.SetValue(CrashReportCustomField.Key.ConfigVersion, RemoteVersion);
            
            Utils.StartCoroutine(UpdateCoroutine());
        }

        private IEnumerator UpdateCoroutine()
        {
            _description = DescriptionLocalization;
            
            var gameLoadingManager = ManagerFacade.GetManager<GameLoadingManager>();
            yield return new WaitUntil(() => gameLoadingManager.IsConfigVersionListGenerated);
            
            var remoteVersionCacheFileForJson = RemoteVersionCacheFileForJson;
            var remoteVersionCacheFileForBin = RemoteVersionCacheFileForBin;
            
            if (!File.Exists(remoteVersionCacheFileForJson))
            {
                ClearObsoleteVersionFile("json_*.json");
                var request = ManagerFacade.FileDownLoadManager.DownloadFileAsync(NetApi.inst.BuildConfigVersionListURL(true), remoteVersionCacheFileForJson, FileDownloadPriority.High);
                _allDownloadRequest.Add(request);
                request.DownloadFinishCallback += OnVersionFileDownloaded;
            }

            if (!File.Exists(remoteVersionCacheFileForBin))
            {
                ClearObsoleteVersionFile("bin_*.json");
                var request = ManagerFacade.FileDownLoadManager.DownloadFileAsync(NetApi.inst.BuildConfigVersionListURL(false), remoteVersionCacheFileForBin, FileDownloadPriority.High);
                _allDownloadRequest.Add(request);
                request.DownloadFinishCallback += OnVersionFileDownloaded;
            }
            
            if (_allDownloadRequest.Count <= 0)
            {
                OnPrepareRemoteVersionFinished();
            }
        }

        private void ClearObsoleteVersionFile(string pattern)
        {
            if (Directory.Exists(ConfigVersionFolder))
            {
                var allFiles = Directory.GetFiles(ConfigVersionFolder, pattern);
                foreach (var file in allFiles)
                {
                    D.Log($"remove obsolete config:{file}");
                    File.Delete(file);
                }
            }
        }
        
        private void OnVersionFileDownloaded(FileDownloadRequest request)
        {
            if (request.IsSuccess)
            {
                _allDownloadRequest.Remove(request);
            }
            else
            {
                Utils.RestartGameWithErrorCode(ErrorCode.ErrorCodeDownloadConfigVersionList);
                return;
            }

            if (_allDownloadRequest.Count <= 0)
            {
                OnPrepareRemoteVersionFinished();
            }
        }

        private void OnPrepareRemoteVersionFinished()
        {
            Dictionary<string, string> allJsonFileVersion = null;
            Dictionary<string, string> allBinFileVersion = null;

            try
            {
                allJsonFileVersion =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(RemoteVersionCacheFileForJson));
                allBinFileVersion =
                    JsonConvert.DeserializeObject<Dictionary<string, string>>(
                        File.ReadAllText(RemoteVersionCacheFileForBin));
            }
            catch (System.Exception exception)
            {
               Debug.LogError("Parse Version List Error.");
            }
            
            if (allJsonFileVersion == null || allBinFileVersion == null)
            {
                File.Delete(RemoteVersionCacheFileForJson);
                File.Delete(RemoteVersionCacheFileForBin);
                
                Utils.RestartGameWithErrorCode(ErrorCode.ErrorCodeParseRemoteConfigListError);
                return;
            }
            
            var remoteSychro = new RemoteSynchro(GetConfigFileLocalPath, GetConfigFileRemoteUrl);
            remoteSychro.SetListener(this);
            
            var disableCheckMd5 = SwitchManager.Instance.GetSwitch(SwitchConst.DISABLE_CHECK_CONFIG_MD5);
            remoteSychro.SetCheckMd5Enabled(!disableCheckMd5);
            
            foreach (var configName in ConfigUpgrade.s_configFBInUse)
            {
                var jsonFileName = configName + ".json";
                var binFileName = configName + ".bin";

                if (allBinFileVersion.ContainsKey(binFileName))
                {
                    allJsonFileVersion.Remove(jsonFileName);
                    allJsonFileVersion.Add(binFileName, allBinFileVersion[binFileName]);
                }
                else
                {
                    D.Error($"找不到flatbuffer表版本信息: {binFileName}");
                }
            }

            foreach (var keyValuePair in allJsonFileVersion)
            {
                remoteSychro.AddRemoteFileVersion(keyValuePair.Key, keyValuePair.Value);
            }

            using (new CostTimePrinter("StartSync"))
            {
                remoteSychro.StartSync();
            }
        }
        
        private string GetConfigFileLocalPath(string fileName)
        {
            return Path.Combine(ConfigFileFolder, fileName);
        }
        
        private string GetConfigFileRemoteUrl(string fileName, string md5)
        {
            return NetApi.inst.BuildConfigUrl(fileName, RemoteVersion);
        }

        private float _progress = 0.0f;
        public void OnSyncProgressChanged(int successCount, int failedCount, int totalCount)
        {
            if (failedCount > 0)
            {
                _description = $"{DescriptionLocalization} {successCount}.{failedCount}/{totalCount}";
            }
            else
            {
                _description = $"{DescriptionLocalization} {successCount}/{totalCount}";
            }

            _progress = successCount / (float)totalCount;
        }

        public override float Progress => _progress;

        public void OnSyncFinished(int successCount, int failedCount, int totalCount)
        {
            if (failedCount <= 0)
            {
                IsDone = true;
            }
            else
            {
                Utils.RestartGameWithErrorCode(ErrorCode.ErrorCodeDownloadConfigFile);
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }
        
        
    }
}