using ClientCore;

namespace GameLoading.LoadingStep
{
    public class WaitPrerequestLoaderStep : LoadingPipelineStep
    {
        public WaitPrerequestLoaderStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();
            
            CheckFinish();
        }
        
        public override void OnTick()
        {
            base.OnTick();
            
            CheckFinish();
        }

        private void CheckFinish()
        {
            var gameLoadingManager = ManagerFacade.GetManager<GameLoadingManager>();

            // 等待先决Loader请求完毕
            if (!gameLoadingManager.IsPrerequestLoaderDone)
            {
                return;
            }

            // 等待其他处理结束
            
            IsDone = true;
        }
        
    }
}