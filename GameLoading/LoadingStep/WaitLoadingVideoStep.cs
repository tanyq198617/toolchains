using ClientCore;

#if false

namespace GameLoading.LoadingStep
{
    public class WaitLoadingVideoStep : LoadingPipelineStep
    {
        public WaitLoadingVideoStep(int step, string descriptionKey):base(step, descriptionKey)
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
            if (!gameLoadingManager.IsLoadingVideoFinished)
            {
                return;
            }
            
            IsDone = true;
        }
        
    }
}

#endif
