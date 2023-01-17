using System.Collections;
using ClientCore;
using Config2;
using DB;
using ReflexCLI.Profiling;
using UI;
using UniRx;
using UnityEngine;
using Voxels.TowerDefense;
using XTutorial;

namespace GameLoading.LoadingStep
{
    public class EnterGameStep : LoadingPipelineStep
    {
        public EnterGameStep(int step, string descriptionKey) : base(step, descriptionKey)
        {
        }

        private bool _tutorialLoaded;

        public override void OnStart()
        {
            base.OnStart();
            Utils.StartCoroutine(LoadGameCoroutine());
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        private IEnumerator LoadGameCoroutine()
        {
            if (XTutorialMgr.Instance.CurState == XTutorialMgr.State.Closed && XTutorialMgr.IsOpenXTutorial)
                XTutorialMgr.Instance.Init();
            
            yield return GameEngine.Instance.TryStartGame();

            //第一次进游戏的时候，需要触发建筑的升级引导
            foreach (var building in CityManager.inst.Buildings)
            {
                //升级了，查一下有没有解锁单位
                var buildInfo = ConfigManager2.inst.DB_Buildings.GetData(building.Type, building.Level);
                if (buildInfo == null)
                {
                    D.Error("CitadelSystem.InitCity->BuildingInfo is null,mType:{0},mLevel:{1}", building.Type,
                        building.Level);
                    continue;
                }

                var units = ConfigManager2.inst.DB_UnitStats.GetUnitsUnlockedByBuilding(building.Type,
                    building.Level);
                MessageBroker.Default.Publish(new BuildingLevelUpEvt(building.BuildingId, buildInfo, units, false));
            }

            D.Log($"#Tutorial# Enter Game OK! {Time.frameCount}");
            IsDone = true;
        }
    }
}