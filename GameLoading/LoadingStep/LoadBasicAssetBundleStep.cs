using System.Collections;
using ClientCore;
using ClientCore.Pipeline;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    /**
     * 加载基础依赖的assetbundle
     */
    public class LoadBasicAssetBundleStep : LoadingPipelineStep
    {
        private float _progress = 0.0f;

        private string _description;
        public override string Description => _description;

        public LoadBasicAssetBundleStep(int step, string descriptionKey):base(step, descriptionKey)
        {
        }
        
        public override void OnStart()
        {
            base.OnStart();
            
            _description = DescriptionLocalization;
            
            if(AssetManager.Instance.IsLoadAssetFromBundle)
            {
                Utils.StartCoroutine(LoadCoroutine());
            }
            else
            {
                IsDone = true;
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        private IEnumerator LoadCoroutine()
        {
            var gameLoadingManager = ManagerFacade.GetManager<GameLoadingManager>();
            
            // 等待static+default解压完毕
            while(!gameLoadingManager.IsStaticDefaultABCached())
            {
                _progress = gameLoadingManager.GetStaticDefaultABCacheProgress();
                
                var percent = (_progress * 100).ToString("f2");
                
                _description = $"{DescriptionLocalization} {percent}%";

                yield return null;
            }
            
            // 加载static+default
            bool loadSuccess = false;
            yield return BundleManager.Instance.LoadBasicBundleCoroutine("static+default", () => { loadSuccess = true;});
            
            if (loadSuccess)
            {
                var svc = AssetManager.Instance.HandyLoad<ShaderVariantCollection>("_ShaderVariants");
                if (svc == null)
                    D.Error($"_ShaderVariants not found!");
                else
                    svc.WarmUp();
                
                IsDone = true;
            }
        }

        public override float Progress => _progress;
    }
}