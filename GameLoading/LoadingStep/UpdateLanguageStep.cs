using System.Collections;
using System.IO;
using NexgenDragon;
using UnityEngine.Profiling;

namespace GameLoading.LoadingStep
{
    /**
     * 更新游戏多语言
     */
    public class UpdateLanguageStep : LoadingPipelineStep
    {
        private LocalizationSynchro _localizationSycSynchro;
        
        public UpdateLanguageStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();
            
            string prefixUrl = string.Empty;
            
            string gameLanguageRegion = string.Empty;
            string gameLanguageVersion = string.Empty;
            
            string loadingLanguageRegion = string.Empty;
            string loadingLanguageVersion = string.Empty;
            
            ParseLanguageInfoInManifest(ref prefixUrl,
                ref gameLanguageRegion, ref gameLanguageVersion,
                ref loadingLanguageRegion, ref loadingLanguageVersion);

            NexgenDragon.Localization.Instance.RemoteLoadingVersion = loadingLanguageVersion;
            NexgenDragon.Localization.Instance.RemoteInGameVersion = gameLanguageVersion;
            
            _localizationSycSynchro = new LocalizationSynchro();
            _localizationSycSynchro.SetLocalizationInfo(prefixUrl,
                gameLanguageRegion, loadingLanguageRegion, 
                loadingLanguageVersion, gameLanguageVersion);

            // 本地有多语言，直接进入游戏，后台更新
            if (NexgenDragon.Localization.Instance.IsInGameLanguageExist())
            {
                IsDone = true;
            }

            _localizationSycSynchro.StartSync(OnLocalizationSyncComplete);
        }
        

        void OnLocalizationSyncComplete(bool loadingUpdated, bool ingameUpdated)
        {
            IsDone = true;
        }
        
        public override void OnTick()
        {
            base.OnTick();
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        public override float Progress { get; }
        
        private void ParseLanguageInfoInManifest(ref string prefixUrl, 
            ref string gameLanguageRegion, ref string gameLanguageVersion, 
            ref string loadingLanguageRegion, ref string loadingLanguageVersion)
        {
            var manifest = NetApi.inst.Manifest;
            if (manifest != null && manifest.Contains("translate"))
            {
                var translateInfo = manifest["translate"] as Hashtable;
                DB.DatabaseTools.UpdateData(translateInfo, "prefix_url", ref prefixUrl);
                DB.DatabaseTools.UpdateData(translateInfo, "region", ref gameLanguageRegion);
                DB.DatabaseTools.UpdateData(translateInfo, "region_loading", ref loadingLanguageRegion);
                loadingLanguageVersion = translateInfo["loading_version"].ToString();
                gameLanguageVersion =  translateInfo["game_version"].ToString();
            }
        }
    }
}