
using System.IO;
using ClientCore;
using Facebook.Unity;
using Funplus.Internal;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    /**
     * 加载准备阶段
     */
    public class PreparationStep : LoadingPipelineStep
    {
        public PreparationStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();
            
            InitFaceBook();

            SetupBI();

            SetupRum();

            SdkEntranceManager.AppsFlyer.Init();
            
            NativeManager.inst.GetGaid();
        }

        public override void OnTick()
        {
            base.OnTick();
            
            if (ManagerFacade.GetManager<GameLoadingManager>().IsExtractResourceFinished)
            {
                NexgenDragon.Localization.Instance.ReloadLoadingLanguage();
                NexgenDragon.Localization.Instance.ReloadInGameLanguage();
                IsDone = true;
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        private void SetupBI()
        {
            LanguageSDK.Instance.SetBiLanguage();

            string channel = GameSetting.Current.SdkChannel;
            if (!string.IsNullOrEmpty(channel))
            {
                Funplus.FunplusSdk.Instance.setGamePackageChannel(channel);
            }
        }

        private void SetupRum()
        {
            // rum
            var rumConfigData = GameSetting.Current.RumConfigData;

            Funplus.FunplusRum.Instance.SetRUMDefaultConfig(
                rumConfigData.AppId, rumConfigData.AppKey, rumConfigData.AppTag, "1.0",
                "https://logagent-global.kingsgroupgames.com/log", 1);
        }

        private void InitFaceBook()
        {
            FunplusSettings.FunplusGameId = GameSetting.Current.FunplusGameId;
            FunplusSettings.FunplusGameKey = GameSetting.Current.FunplusGameKey;

#if SERVER_DW_GLOBAL
        FunplusSettings.Environment = "production";
#else
            FunplusSettings.Environment = "sandbox";
#endif

            FunplusSettings.FacebookAppId = GameSetting.Current.FacebookConfigData.FacebookAppId;
            FunplusSettings.FacebookAppName = GameSetting.Current.FacebookConfigData.FacebookAppName;
            FunplusSettings.FacebookEnabled = GameSetting.Current.FacebookConfigData.FacebookEnabled;

            D.Warn("#FaceBookDebug# FacebookAppId: {0}  FacebookAppName: {1}", FunplusSettings.FacebookAppId,
                FunplusSettings.FacebookAppName);

            if (FunplusSettings.FacebookEnabled)
            {
                if (FB.IsInitialized)
                {
                    FB.ActivateApp();
                }
                else
                {
                    D.Warn("#FaceBookDebug# FB.Init GameVersionState.ValidGameSetting");
                    FB.Init(appId: FunplusSettings.FacebookAppId, onInitComplete: () =>
                    {
                        D.Warn("#FaceBookDebug# FB.Init GameVersionState.ValidGameSetting CB");
                        FB.ActivateApp();
                    });
                }
            }
        }
    }
}