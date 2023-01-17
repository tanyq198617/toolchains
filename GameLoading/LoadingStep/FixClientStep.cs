using System.IO;
using ClientCore;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    public class FixClientStep : LoadingPipelineStep
    {
        public FixClientStep(int step, string descriptionKey) : base(step, descriptionKey)
        {
            
        }

        public override string Description
        {
            get
            {
                if (_failedCounter > 0)
                {
                    return $"{DescriptionLocalization} {_removeIndex}+{_failedCounter}/{_allGameFiles.Length}";
                }
                else
                {
                    return $"{DescriptionLocalization} {_removeIndex}/{_allGameFiles.Length}";
                }
            }
        }

        public override float Progress
        {
            get
            {
                if (_allGameFiles.Length > 0)
                {
                    return _removeIndex / (float)_allGameFiles.Length;
                }

                return 1.0f;
            }
        }

        private string[] _allGameFiles = null;
        private int _removeIndex = 0;
        private int _failedCounter = 0;
        
        public override void OnStart()
        {
            base.OnStart();

            var gameLoadingManager = ManagerFacade.GetManager<GameLoadingManager>();
            if (gameLoadingManager.RequestFixClient)
            {
                DoCollectGameAssetFiles();
            }
            else
            {
                IsDone = true;
            }
        }

        public override void OnTick()
        {
            base.OnTick();

            var removePerFrame = 30;
            
            for (var i = 0; i < removePerFrame && _removeIndex < _allGameFiles.Length; i++, _removeIndex++)
            {
                try
                {
                    File.Delete(_allGameFiles[_removeIndex]);
                }
                catch(System.Exception exception)
                {
                    _failedCounter++;
                }
            }

            if (_removeIndex >= _allGameFiles.Length)
            {
                PlayerPrefs.DeleteAll();
                PlayerPrefs.Save();
                
                IsDone = true;
            }
        }
        
        // low performance
        private void DoCollectGameAssetFiles()
        {
            var gameAssetFolder = Path.Combine(Application.persistentDataPath, "GameAssets");
            
            _allGameFiles = Directory.GetFiles(gameAssetFolder, "*.*", SearchOption.AllDirectories);
        }
    }
}