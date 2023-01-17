using System.Collections.Generic;

namespace GameLoading.LoadingStep
{
    public class UpdatePrerequestOtaBundleStep : LoadingPipelineStep
    {
        private List<string> _allPrerequestOtaBundle = new List<string>();
        private List<string> _allSucessOtaBundle = new List<string>();
        private List<string> _allFailedOtaBundle = new List<string>();
        
        public override float Progress
        {
            get { return _allSucessOtaBundle.Count / (float)_allPrerequestOtaBundle.Count; }
        }

        private string _description = string.Empty;
        
        public override string Description
        {
            get { return _description; }
        }
        
        public UpdatePrerequestOtaBundleStep(int step, string descriptionKey) : base(step, descriptionKey)
        {
        }

        public override void OnStart()
        {
            base.OnStart();

            if (!AssetManager.Instance.IsLoadAssetFromBundle)
            {
                IsDone = true;
                return;
            }
            
            _description = DescriptionLocalization;
            
            BundleManager.Instance.onBundleLoaded += OnBundleLoaded;
            
            _allPrerequestOtaBundle.Clear();
            
            // 1v1
            TryAppendAlliance1v1Bundle(ref _allPrerequestOtaBundle);

            //
            TryRemoveInvalidBundleName(ref _allPrerequestOtaBundle);

            if (_allPrerequestOtaBundle.Count > 0)
            {
                foreach (var bundleName in _allPrerequestOtaBundle)
                {
                    BundleManager.Instance.CacheBundle(bundleName);
                }    
            }
            else
            {
                IsDone = true;
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
            
            BundleManager.Instance.onBundleLoaded -= OnBundleLoaded;
        }

        private void TryAppendAlliance1v1Bundle(ref List<string> bundleList)
        {
            if (PlayerData.inst.IsInAlliance1v1) 
            {
                string[] bundleNames = Alliance1v1MapManager.Instance.BundleNames;
                foreach (string bundleName in bundleNames)
                {
                    bundleList.Add(bundleName);
                }
            }
        }

        private void TryRemoveInvalidBundleName(ref List<string> bundleList)
        {
            for (int i = bundleList.Count - 1; i >= 0; i--)
            {
                var bundleName = bundleList[i];
                
                if (!BundleManager.Instance.IsBundleNameValid(bundleName))
                {
                    D.Error($"remove invalid bundle name {bundleName}");
                    bundleList.RemoveAt(i);
                }
            }
        }
        
        private void OnBundleLoaded(string bundleName, bool result)
        {
            if (_allPrerequestOtaBundle.Contains(bundleName))
            {
                if(result)
                {
                    if (!_allSucessOtaBundle.Contains(bundleName))
                    {
                        _allSucessOtaBundle.Add(bundleName);
                    }
                }
                else
                {
                    if (!_allFailedOtaBundle.Contains(bundleName))
                    {
                        _allFailedOtaBundle.Add(bundleName);
                    }
                }
            }

            if (_allSucessOtaBundle.Count + _allFailedOtaBundle.Count >= _allPrerequestOtaBundle.Count)
            {
                if (_allFailedOtaBundle.Count <= 0)
                {
                    IsDone = true;
                }
                else
                {
                    Utils.RestartGameWithErrorCode(ErrorCode.ErrorDownloadAssetBundleFailed);
                }
            }
            
            if (_allFailedOtaBundle.Count > 0)
            {
                _description = $"{DescriptionLocalization} {_allSucessOtaBundle.Count}.{_allFailedOtaBundle.Count}/{_allPrerequestOtaBundle.Count}";
            }
            else
            {
                _description = $"{DescriptionLocalization} {_allSucessOtaBundle.Count}/{_allPrerequestOtaBundle.Count}";
            }
        }
    }
}