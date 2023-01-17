using System.Collections.Generic;
using System.Linq;
using ClientCore;
using MoreLinq;

namespace GameLoading
{
    public class OtaBundleUpdateManager : IManager
    {
        private Queue<string> _allChangedOtaBundle = null;
        
        public void Initialize()
        {
            BundleManager.Instance.onBundleLoaded += OnBundleLoadFinished;
        }

        public void Dispose()
        {
            BundleManager.Instance.onBundleLoaded += OnBundleLoadFinished;
        }
        
        private string[] GetAllHighPriorityOtaBundle()
        {
            string[] abNames =
            {
                "ota+tex_6","ota+tex_36","ota+vfx","ota+holiday_carnival","ota+tex_140"
                ,"ota+tex_1","ota+vfx_zhucheng","ota+tex_28","dyna+citizen","dyna+cityeffect","ota+vfx_ui"
                ,"ota+bg_chapter","ota+tex_147","ota+tex_117","ota+tex_heroskill_1","ota+vfx_3"
                ,"ota+bg_recruit","ota+tex_12","ota+tex_1116","ota+tex_123_31","ota+image_unit_ranged_t413"
                ,"ota+tex_53","ota+tex_54","ota+tex_108","ota+tex_80","ota+tex_83","ota+tex_127","ota+tex_82"
                ,"ota+dragon_treasure_lv45","ota+tex_13","ota+tex_15","ota+tex_14","ota+tex_16","ota+tex_112","ota+vfx_4"
                ,"ota+portrait_0","ota+vfx","ota+holiday_carnival","ota+tex_1","ota+vfx_zhucheng","dyna+citizen","ota+tex_28",
                "ota+dragon_star_name_1_c","ota+dragon_emblem","dyna+cityeffect","ota+iap_package_super_speedup","ota+monster_10","ota+monster_7"
                ,"ota+monster_4","ota+monster_1","ota+tex_840","ota+vfx_hero","ota+vfx_ui","ota+tex_147","ota+troop_hero_bubble",
                "ota+dragon_star_name_1","ota+parlia_circle"
            };
            
            return abNames;
        }

        private List<string> GetAllChangedOtaBundleName()
        {
            var allChanged = VersionManager.Instance.Newversionlist;
            
            var allChangedOtaBundleName = new List<string>(allChanged.Count);

            foreach (var changed in allChanged)
            {
                var bundleName = changed.Key;

                if (bundleName.Contains(AssetConfig.BUNDLE_OTA_FLAG))
                {
                    bundleName = changed.Key.Substring(0, bundleName.IndexOf(AssetConfig.BUNDLE_EX));
                    
                    allChangedOtaBundleName.Add(bundleName);
                }
            }

            return allChangedOtaBundleName;
        }
        
        public void StartAutoDownloadOta()
        {
            if (!AssetManager.Instance.IsLoadAssetFromBundle)
            {
                return;
            }
            
            var allChangedOtaBundleName = GetAllChangedOtaBundleName();
            
            _allChangedOtaBundle = new Queue<string>(allChangedOtaBundleName.Count);
            
            // 优先填充高优先级ota bundle.
            var allHighPriorityBundle = GetAllHighPriorityOtaBundle();
            
            foreach (var bundleName in allHighPriorityBundle)
            {
                if (allChangedOtaBundleName.Contains(bundleName))
                {
                    _allChangedOtaBundle.Enqueue(bundleName);
                    allChangedOtaBundleName.Remove(bundleName);
                }
            }
            
            // 填充剩余ota bundle
            foreach (var bundleName in allChangedOtaBundleName)
            {
                _allChangedOtaBundle.Enqueue(bundleName);
            }
            
            // 开始缓存
            for (int i = 0; i < 5 && _allChangedOtaBundle.Count > 0; i++)
            {
                var bundleName = _allChangedOtaBundle.Dequeue();
                
                BundleManager.Instance.CacheBundle(bundleName);
            }
        }

        private void OnBundleLoadFinished(string bundleName, bool result)
        {
            if (_allChangedOtaBundle != null && _allChangedOtaBundle.Count > 0)
            {
                BundleManager.Instance.CacheBundle(_allChangedOtaBundle.Dequeue());
            }
        }
    }
}