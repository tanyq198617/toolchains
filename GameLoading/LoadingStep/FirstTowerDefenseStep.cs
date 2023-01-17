using System;
using System.Collections;
using Config2;
using UI;
using UniRx;
using Voxels.TowerDefense;
using XTutorial;
using TDBattleDlg = UIOptimize.TowerDefenseBattleDlg.TowerDefenseBattleDlg.TowerDefenseBattleDlg;
using ReflexCLI.Profiling;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    public class FirstTowerDefenseStep : LoadingPipelineStep
    {
        private IDisposable _disposable = Disposable.Empty;
        private bool _isFirstTutorialDone;

        public FirstTowerDefenseStep(int step, string descriptionKey) : base(step, descriptionKey)
        {
        }
        
        public override void OnStart()
        {
            base.OnStart();
            UIManager.inst.Init();

            if (XTutorialMgr.IsOpenXTutorial)
                XTutorialMgr.Instance.Init();
            
            _isFirstTutorialDone = XTutorialSaver.Instance.IsDone(XTutorialMgr.TDFirstLevelDoneGroup);
            if (!_isFirstTutorialDone)
            {
                _disposable = Observable.FromCoroutine(_Run).Subscribe();
            }
            else
            {
                IsDone = true;
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
            _disposable.Dispose();
            OpeningVideoMgr.Instance.Dispose();
            //if (!_isFirstTutorialDone)
            //    XTutorialMgr.Instance.CurState = XTutorialMgr.State.Loading;
        }

        public override void OnCancel()
        {
            base.OnCancel();
            _disposable.Dispose();
            OpeningVideoMgr.Instance.Dispose();
            //if(!_isFirstTutorialDone)
            //    XTutorialMgr.Instance.CurState = XTutorialMgr.State.Loading;
        }

        IEnumerator _Run()
        {
            var plotVideoInfo = ConfigManager2.Instance.DB_PlotVideoGroup.GetById("tutorial_2021_cg_1");
            OpeningVideoMgr.Instance.PlayOpeningVideo("StoryVideo/tutorial_2021_cg_1", plotVideoInfo);
            while (OpeningVideoMgr.Instance.IsRunning)
                yield return null;
            
            bool inTd = true;
            var levelInfo = ConfigManager2.inst.DB_TowerDefenseLevels.GetById($"{Island.LevelPrefix}1");
            
            TowerDefenceMgr.Instance.EnterScene(levelInfo, showCloud: false,
                onEnter: () =>
                {
                    XTutorialMgr.Instance.CurState = XTutorialMgr.State.Started;
                    MessageBroker.Default.Publish(new SplashScreen.ActiveSplashEvt(false));
                }, onExit: () =>
                {
                    D.Log("#Tower# OnExitScene");
                    //XTutorialMgr.Instance.Dispose();
                    GameEngine.Instance.CurrentGameMode = GameEngine.GameMode.InitMode;
                    MessageBroker.Default.Publish(new SplashScreen.ActiveSplashEvt(true));
                    UIManager.inst.Cloud.PokeCloud(() =>
                    {
                        inTd = false;
                    });
                });
            
            while (inTd)
                yield return null;
            
            AudioManager.Instance.StopCurrentBGM();
            AudioManager.Instance.PlayLoadingBGM();
            
            plotVideoInfo = ConfigManager2.Instance.DB_PlotVideoGroup.GetById("tutorial_2021_cg_2");
            OpeningVideoMgr.Instance.PlayOpeningVideo("StoryVideo/tutorial_2021_cg_2", plotVideoInfo);
            while (OpeningVideoMgr.Instance.IsRunning)
                yield return null;
            
            //引导图面板
            UIManager.inst.OpenDlg(UIManager.DialogType.MapGuidanceBoatDlg);
            
            yield return null;
            yield return GameEngine.Instance.TryStartGame();

            //等待引导图面板关闭
            while (!UIManager.inst.IsDialogClear)
                yield return null;
            
            IsDone = true;
        }
    }
}