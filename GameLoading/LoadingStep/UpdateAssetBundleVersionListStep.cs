namespace GameLoading.LoadingStep
{
    public class UpdateAssetBundleVersionListStep : LoadingPipelineStep
    {
        public UpdateAssetBundleVersionListStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();
            
            AddVersionListLoadCallback();

            if (!AssetManager.Instance.IsLoadAssetFromBundle)
            {
                IsDone = true;
                return;
            }

            VersionManager.Instance.Init();
        }

        public override void OnEnd()
        {
            base.OnEnd();

            RemoveVersionListLoadCallback();
        }
        
        private void AddVersionListLoadCallback()
        {
            VersionManager.Instance.onVersionLoadFinished += OnVersionListLoadCallback;
        }

        private void RemoveVersionListLoadCallback()
        {
            VersionManager.Instance.onVersionLoadFinished -= OnVersionListLoadCallback;
        }

        private void OnVersionListLoadCallback(bool success)
        {
            if (success)
            {
                IsDone = true;
            }
            else
            {
                ShowRetryPopup();
            }
        }
        
        private void ShowRetryPopup()
        {
            string title = I2.Loc.ScriptLocalization.Get("network_error_try_again_button");
            string content = I2.Loc.ScriptLocalization.Get("network_error_description");
            NetWorkDetector.Instance.RetryTip(()=>
            {
                GameEngine.Instance.MarkRestartGame(GameEngine.LoadMode.Deep);
            }, title, content);
        }
    }
}