using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClientCore;
using ClientCore.Pipeline;
using ClientCore.RemoteSynchor;
using GameLoading.LoadingStep;
using ReflexCLI.Profiling;
using UI;
using UnityEngine;
using XTutorial;
using Debug = UnityEngine.Debug;

namespace GameLoading
{
    /**
     * 游戏loading管理
     */
    public class GameLoadingManager : IManager, ITicker
    {
        private Pipeline _loadingPipeline = null;

        public bool IsLoading => _loadingPipeline == null || !_loadingPipeline.IsDone;

        public void Initialize()
        {
            AddLoaderListener();
            StartExtractResource();
        }

        public void Dispose()
        {
            RemoveLoaderListener();
            DestroyCurrentPipeline();

            if (_generateLocalVersionOperation != null)
            {
                _generateLocalVersionOperation.Dispose();
                _generateLocalVersionOperation = null;
            }

            if (_extractResourceOperation != null)
            {
                _extractResourceOperation.Dispose();
                _extractResourceOperation = null;
            }
        }

        public void StartLoading(GameEngine.LoadMode loadMode)
        {
            ManagerFacade.FileDownLoadManager.SetRunningLimitCount(6);
            DestroyCurrentPipeline();

            _loadingPipeline = CreatePipeline();
            _loadingPipeline.SetStepChangeCallback(OnLoadingStepChanged);
            _loadingPipeline.SetFinishCallback(OnLoadingFinished);

            if (loadMode == GameEngine.LoadMode.Deep)
            {
                _loadingPipeline.Start();
            }
            else
            {
                var loadUserDataStep = _loadingPipeline.GetPipelineStep<LoadUserDataStep>();
                _loadingPipeline.StartFrom(loadUserDataStep);
            }

            ShowSplash();
        }

        private bool _stopLoading = false;

        public void StopLoading()
        {
            _stopLoading = true;
        }

        private Pipeline CreatePipeline()
        {
            int step = 1;

            var pipeline = new Pipeline(
                new FixClientStep(step++, " "),
                new PreparationStep(step++, " "),
                new LoadManifestStep(step++, "load_setup_tip"),
                new LoadInjectFixStep(step++, " "),
                new WaitPrivacyPolicyStep(step++, " "),
                new UpdateAssetBundleVersionListStep(step++, "load_bundleversion_tip"), // 结束后开始解压static+default
                new LoadAccountStep(step++, "load_set_up_sdk_tip"), // 结束后开始发送call init请求
                new UpdateAssetBundleStep(step++, "load_cache_bundle"),
                new UpdateConfigFileStep(step++, "load_parse_config"),
                new LoadBasicAssetBundleStep(step++, "load_cache_bundle"), // 等待解压static+default结束，并加载

                new LoadUserDataStep(step++, "load_data_tip"), // 结束后播放开场视频
                
                new ProcessUserData(step++, " "), // 处理玩家数据状态：删号冷静期、等

                new WaitUserConfirmationStep(step++, "user_confirmation"),
                new UpdatePrerequestOtaBundleStep(step++, "load_cache_bundle"), // 等待下载必须ota
                new UpdateLanguageStep(step++, " "),
                new ConnectRtmStep(step++, "load_connect_rtm"),
                //new WaitLoadingVideoStep(step++, "load_connect_rtm"),  // 等待loading视频播放结束;
                new WaitPrerequestLoaderStep(step++, "load_connect_rtm"), // 等待loading必要loader请求完毕;
                new FirstTowerDefenseStep(step++, "load_scene_tip"),
                new SendLoaderStep(step++, " "),
                new EnterGameStep(step++, "load_scene_tip")
            );

            return pipeline;
        }

        public void Tick(float delta)
        {
            //using (new ScopedProfiler("GameLoadingMgr.Tick"))
            {
                if (_stopLoading)
                {
                    return;
                }

                if (_loadingPipeline != null)
                {
                    _loadingPipeline.Tick();

                    var currentStep = _loadingPipeline.GetCurrentPipelineStep();
                    if (currentStep != null)
                    {
                        var loadingStep = currentStep as LoadingPipelineStep;

                        var desription = loadingStep.Description;

                        if (string.IsNullOrEmpty(desription))
                        {
                            desription = currentStep.GetType().ToString();
                            D.Error($"{desription} Description should not return empty!");
                        }

                        UpdateLoadingTips(desription);
                    }
                }
            }
        }

        private void DestroyCurrentPipeline()
        {
            if (_loadingPipeline != null && !_loadingPipeline.IsDone)
            {
                _loadingPipeline.Cancel();
            }

            _loadingPipeline = null;
        }

        private void OnLoadingStepChanged(PipelineStep previous, PipelineStep current)
        {
            D.Log($"GameLoadingManager: LoadingStepChanged:{previous} -> {current}");

            if (current != null)
            {
                CrashReportCustomField.SetValue(CrashReportCustomField.Key.LoadingState, current.GetType().ToString());
            }
            else
            {
                CrashReportCustomField.SetValue(CrashReportCustomField.Key.LoadingState, "");
            }

            // 加载patch后本地更新配置表MD5列表
            if (previous is LoadInjectFixStep)
            {
                GenerateConfigVersionList();
            }

            // 更新好资源版本信息后
            if (previous is UpdateAssetBundleVersionListStep)
            {
                CacheStaticDefaultAB();
            }

            // 加载fpid成功后并行发送callinit
            if (previous is LoadAccountStep)
            {
                ManagerFacade.GetManager<CallInitManager>().SendCallInitRequest();
            }

            // 加载用户数据时并行加载游戏资源
            if (current is LoadUserDataStep)
            {
                ManagerFacade.GetManager<PreloadPrefabManager>().StartLoadAsync();
            }

            // 加载用户数据后并行检查播放视频
            if (previous is LoadUserDataStep)
            {
                //PlayLoadingVideo();
                SendPrerequestLoader();
                SdkEntranceManager.MicroCommunity.Init();
            }
        }

        private void OnLoadingFinished(bool result)
        {
            D.Log("GameLoadingManager: LoadingFinished({0})", result);

            HideSplash();
            Debug.Log(_loadingPipeline.DumpTimeInfo());

            var otaBundleUpdateManager = ManagerFacade.GetManager<OtaBundleUpdateManager>();
            otaBundleUpdateManager.StartAutoDownloadOta();

            //挪个位置，根据是不是新前期给不同的设置
            //ManagerFacade.FileDownLoadManager.SetRunningLimitCount(3);
        }

        #region generate config local md5

        private bool _configVersionListGenerated = false;

        public bool IsConfigVersionListGenerated
        {
            get { return _configVersionListGenerated; }
        }

        private void GenerateConfigVersionList()
        {
            GameEngine.Instance.StartCoroutine(GenerateConfigVersionListCoroutine());
        }

        private WaitForAsyncOperation _generateLocalVersionOperation = null;

        private IEnumerator GenerateConfigVersionListCoroutine()
        {
            _configVersionListGenerated = false;

            var configFileFolder = Path.Combine(Application.persistentDataPath, "GameAssets/Config");

            var remoteSychro = new RemoteSynchro(
                (string configFile) => { return Path.Combine(configFileFolder, configFile); },
                (string configFile, string md5) => { return string.Empty; }
            );

            IOException ioException = null;
            _generateLocalVersionOperation = new WaitForAsyncOperation((parameter, cancelToken) =>
            {
                try
                {
                    remoteSychro.GenerateLocalVersionCancelable(cancelToken);
                }
                catch (IOException exception)
                {
                    ioException = exception;
                }

                return null;
            }, null);

            yield return _generateLocalVersionOperation;
            _generateLocalVersionOperation = null;

            if (ioException != null)
            {
                GameEngine.Instance.OnIOExceptionHanppend(ioException);
                yield break;
            }

            _configVersionListGenerated = true;
        }

        #endregion

        // 解压包内资源

        #region ExtractPackageResource

        private bool _extractResourceFinished = false;

        public bool IsExtractResourceFinished
        {
            get { return _extractResourceFinished; }
        }

        private WaitForAsyncOperation _extractResourceOperation = null;

        private void StartExtractResource()
        {
            Utils.StartCoroutine(ExtractFilesInPackage());
        }

        private IEnumerator ExtractFilesInPackage()
        {
            _extractResourceFinished = false;

            var gameSetting = GameSetting.Current;

            var gameVersion = $"{gameSetting.VersionName}.{gameSetting.VersionCode}";

            var versionKey = "extrace_version";

            var currentVersion = PlayerPrefs.GetString(versionKey);

            if (currentVersion != gameVersion)
            {
                // Config
                var configFolder = NetApi.inst.ConfigFolder;
                var languageFolder = NetApi.inst.LanguageFolder;

                // 移除老目录
                if (Directory.Exists(configFolder))
                {
                    Directory.Delete(configFolder, true);
                }

                if (Directory.Exists(languageFolder))
                {
                    Directory.Delete(languageFolder, true);
                }

                // 释放包内新资源
                var zipFileName = $"GameAssets.zip";
                var targetDirectory = Path.Combine(Application.persistentDataPath, "GameAssets");
                var tempZipFilePath = Path.Combine(Application.persistentDataPath, zipFileName);

                var error = "";
                string buildfolderpath = Path.Combine(Application.streamingAssetsPath + "/build", zipFileName);

                _extractResourceOperation = new WaitForAsyncOperation((object state) =>
                {
#if UNITY_EDITOR
                    D.Log("skip in editor mode");
#elif UNITY_IOS
                {

                    SharpZipUtil.UnZipFile(buildfolderpath, targetDirectory, out error);
                }
#elif UNITY_ANDROID
                {
                    if (AndroidUtility_Threaded.UnzipAssets_Threaded($"build/{zipFileName}", tempZipFilePath, 10 * 1024))
                    {
                        SharpZipUtil.UnZipFile(tempZipFilePath, targetDirectory,  out error);
                        File.Delete(tempZipFilePath);
                    }
                    else
                    {
                        error = "unzip config.zip from apk error";
                    }
                }
#endif
                    return null;
                }, null);

                yield return _extractResourceOperation;
                _extractResourceOperation = null;

                if (!string.IsNullOrEmpty(error))
                {
                    D.Error($"Unzip GameAsset.zip Error: {error}");
                    //Utils.RestartGameWithErrorCode(ErrorCode.ErrorUnzipGameAssetFailed);
                    //yield break;
                }
            }

            PlayerPrefs.SetString(versionKey, gameVersion);
            PlayerPrefs.Save();

            _extractResourceFinished = true;
        }

        #endregion

        // 缓存AssetBundle

        #region CachedAssetBundle

        private void CacheStaticDefaultAB()
        {
            if (AssetManager.Instance.IsLoadAssetFromBundle)
            {
                BundleManager.Instance.CacheBundle("static+default");
            }
        }

        public bool IsStaticDefaultABCached()
        {
            return !AssetManager.Instance.IsLoadAssetFromBundle ||
                   BundleManager.Instance.IsBundleCached("static+default");
        }

        public float GetStaticDefaultABCacheProgress()
        {
            if (AssetManager.Instance.IsLoadAssetFromBundle)
            {
                return BundleManager.Instance.GetAssetBundleDecompressProgress("static+default");
            }

            return 1.0f;
        }

        #endregion

        // Loading视频

        #region LoadingVideo

        private void PlayLoadingVideo()
        {
            if (Application.isEditor)
            {
                return;
            }

            // VideoPlayer不支持的设备名单
            if (Application.platform == RuntimePlatform.Android)
            {
                var whiteList = new string[]
                {
                    "Pixel 4a"
                };

                var deviceName = Funplus.FunplusSdkUtils.Instance.GetDeviceName();

                if (whiteList.Contains(deviceName))
                {
                    return;
                }
            }

            //switch (OpeningVideoMgr.Instance.ab_opening_cg)
            //{
            //    case 2:
            //        StepBase.AddStepTouch("cg_2_start");
            //        OpeningVideoMgr.Instance.Startup();
            //        break;
            //}
        }

        public bool IsLoadingVideoFinished
        {
            get { return !OpeningVideoMgr.Instance.IsRunning; }
        }

        #endregion

        // Loading刷新

        #region LoadingRefresh

        private CostTimePrinter _costTime = null;

        private void ShowSplash()
        {
            _costTime = new CostTimePrinter("ShowLoading");
            UIManager.inst.SplashControl.Show();
        }

        private void HideSplash()
        {
            UIManager.inst.SplashControl.Hide();
            _costTime.Dispose();
            _costTime = null;
        }

        public bool IsShowingSplash => _costTime != null;

        public void UpdateLoadingTips(string description)
        {
            var progress = _loadingPipeline.GetProgress();
            UIManager.inst.SplashControl.SetMessage(description);
            UIManager.inst.SplashControl.SetValue(progress);
        }

        #endregion

        // 先决Loader
        #region PrerequestLoader

        private string[] _allPrerequestLoader =
        {
            PortType.ACTIVITY_LOAD_DATA,
            PortType.PLAYER_GET_PACKAGE,
            PortType.IAP_CENTER_GET_WELFARECENTER_ACTIVITIES
        };

        private List<string> _allReceivePrerequestLoader = new List<string>();

        private void AddLoaderListener()
        {
            foreach (var action in _allPrerequestLoader)
            {
                RequestManager.inst.AddObserver(action, OnPrerequestLoadCallBack);
            }
        }

        private void RemoveLoaderListener()
        {
            foreach (var action in _allPrerequestLoader)
            {
                RequestManager.inst.RemoveObserver(action, OnPrerequestLoadCallBack);
            }
        }

        private void OnPrerequestLoadCallBack(string action, bool result)
        {
            if (!_allReceivePrerequestLoader.Contains(action))
            {
                _allReceivePrerequestLoader.Add(action);
            }
        }
        
        public void SendPrerequestLoader()
        {
            if (AccountManager.Instance.InDeleteAccountProcess)
            {
                D.Log("SendPrerequestLoader return! beacuse of deleting account calm period");
                return;
            }
            
            // 先决Loader，需要在loading内请求完成才可以进入游戏
            ActivityManager.Instance.OnGameModeReady();
            IAPStorePackagePayload.Instance.Initialize();
            IAPCenterManager.Instance.OnGetActivityData(true, null);
            HeroCardUtils.LoadHeroCardDataByLoader();
        }

        public bool IsPrerequestLoaderDone
        {
            get
            {
                var playerCityData = PlayerData.inst.playerCityData;
                if (playerCityData != null &&
                    playerCityData.level < 3 &&
                    !PlayerData.inst.IsInAlliance1v1)
                {
                    return true;
                }

                return _allReceivePrerequestLoader.Count >= _allPrerequestLoader.Length;
            }
        }

        #endregion

        // 修复客户端

        #region fix client

        private const string FixClientKey = "FixClientKey";

        public bool RequestFixClient
        {
            get { return PlayerPrefs.GetInt(FixClientKey, 0) > 0; }
            set
            {
                PlayerPrefs.SetInt(FixClientKey, value ? 1 : 0);
                PlayerPrefs.Save();
                if (value)
                {
                    GameEngine.Instance.MarkRestartGame(GameEngine.LoadMode.Deep);
                }
            }
        }

        #endregion

        // 清理老版本文件

        #region clear obsolete files

        public void CleanObsoleteFiles()
        {
            var key = "clean_mask";
            var cleanMask = PlayerPrefs.GetInt(key, 0);
            if (cleanMask <= 0)
            {
                using (new CostTimePrinter("CleanObsoleteFiles"))
                {
                    var rootPath = Application.persistentDataPath;

                    CleanFilesUnderFolder(rootPath, "*.bin");
                    CleanFilesUnderFolder(rootPath, "*.json");
                    CleanFilesUnderFolder(rootPath, "*.assetbundle");
                }

                PlayerPrefs.SetInt(key, 1);
                PlayerPrefs.Save();
            }
        }

        private void CleanFilesUnderFolder(string directory, string searchPattern)
        {
            try
            {
                var allFiles = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
                for (int i = 0; i < allFiles.Length; i++)
                {
                    File.Delete(allFiles[i]);
                }
            }
            catch (System.Exception exception)
            {
                D.Error($"CleanFilesUnderFolder({directory}, {searchPattern}) Exception.");
            }
        }

        #endregion
    }
}