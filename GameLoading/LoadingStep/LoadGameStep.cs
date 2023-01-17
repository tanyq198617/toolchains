using System;
using System.Collections;
using ClientCore;
using ClientCore.Pipeline;
using UI;
using UnityEngine;
using UnityEngine.Profiling;

namespace GameLoading.LoadingStep
{
    /**
     * 加载游戏内容city/kingdom/1v1 ..
    public class LoadGameStep : LoadingPipelineStep
    {
        public LoadGameStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();

            Utils.StartCoroutine(LoadGameCoroutine());
        }

        public override void OnEnd()
        {
            Debug.DebugBreak();
            
            base.OnEnd();
        }

        public override void OnTick()
        {
            base.OnTick();

            if (GameEngine.IsReady())
            {
                if (GameEngine.Instance.CurrentGameMode == GameEngine.GameMode.CityMode)
                {
                    if (CitadelSystem.inst.IsPreloadFinished)
                    {
                        IsDone = true;
                    }
                }
                else
                {
                    IsDone = true;
                }
            }
        }

        private IEnumerator LoadGameCoroutine()
        {
            using (new CostTimePrinter("InitAllHud"))
            {
                yield return UIManager.inst.InitAllHud();
            }

            using (new CostTimePrinter("StartGame"))
            {
                GameEngine.Instance.TryStartGame();
            }
        }
    }
    */
}