using System;
using System.Collections.Generic;

namespace GameLoading.LoadingStep
{
    /**
     * 更新asset bundle
     */
    public class UpdateAssetBundleStep : LoadingPipelineStep
    {
        private List<string> _allChangedAssetBundle = new List<string>();
        private List<string> _allSuccessAssetBundle = new List<string>();
        private List<string> _allFailedAssetBundle = new List<string>();
        
        public UpdateAssetBundleStep(int step, string descriptionKey):base(step, descriptionKey)
        {
        }
        
        public override string Description
        {
            get
            {
                if (_allChangedAssetBundle.Count > 0)
                {
                    return DescriptionLocalization + $" {_allSuccessAssetBundle.Count}/{_allChangedAssetBundle.Count}";
                }
                
                return DescriptionLocalization;
            }
        }

        public override float Progress
        {
            get { return _allSuccessAssetBundle.Count / (float) _allChangedAssetBundle.Count; }
        }

        public override void OnStart()
        {
            base.OnStart();
            
            AddCachedBundleCallback();
            
            if (!AssetManager.Instance.IsLoadAssetFromBundle)
            {
                IsDone = true;
                return;
            }
            
            // 收集要更新的assetbundle列表
            foreach (var assetBundle in VersionManager.Instance.Newversionlist.Keys)
            {
                if (assetBundle.StartsWith(AssetConfig.BUNDLE_STATIC_FLAG))
                {
                    string assetBundleName = assetBundle.Replace(AssetConfig.BUNDLE_EX, string.Empty);

                    if (assetBundleName != "static+default")
                    {
                        _allChangedAssetBundle.Add(assetBundleName);
                    }
                }
            }
            // 开始更新
            foreach (var assetBundle in _allChangedAssetBundle)
            {
                BundleManager.Instance.CacheBundle(assetBundle, true);
            }
            
            if (_allChangedAssetBundle.Count <= 0)
            {
                IsDone = true;
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
            
            RemoveCacheBundleCallback();
        }

        private void AddCachedBundleCallback()
        {
            BundleManager.Instance.onBundleLoaded += OnBundleLoadFinished;
        }

        private void RemoveCacheBundleCallback()
        {
            BundleManager.Instance.onBundleLoaded -= OnBundleLoadFinished;
        }
        
        private void OnBundleLoadFinished(string name, bool success)
        {
            var assetBundleName = name.Replace(AssetConfig.BUNDLE_EX, String.Empty);

            if (success && !_allSuccessAssetBundle.Contains(assetBundleName))
            {
                _allSuccessAssetBundle.Add(assetBundleName);
            }
            else 
            {
                if (!_allFailedAssetBundle.Contains(assetBundleName))
                {
                    _allFailedAssetBundle.Add(assetBundleName);
                }
            }

            CheckFinish();
        }

        private void CheckFinish()
        {
            if (_allFailedAssetBundle.Count + _allSuccessAssetBundle.Count >= _allChangedAssetBundle.Count)
            {
                if (_allFailedAssetBundle.Count <= 0)
                {
                    IsDone = true;
                }
                else
                {
                    ShowRetryPopup();
                }
            }
        }
        
        private void ShowRetryPopup()
        {
            string title = I2.Loc.ScriptLocalization.Get("network_error_try_again_button");
            string content = I2.Loc.ScriptLocalization.Get("network_error_description");
            NetWorkDetector.Instance.RetryTip(RetryAllFailed, title, content);
        }
        
        private void RetryAllFailed()
        {
            var allFailedAssetBundle = _allFailedAssetBundle.ToArray();
            
            _allFailedAssetBundle.Clear();
            
            foreach (var assetBundle in allFailedAssetBundle)
            {
                BundleManager.Instance.CacheBundle(assetBundle, true);
            }
        }
    }
}