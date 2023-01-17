using System.Collections;
using System;
using System.Collections.Generic;
using ClientCore;
using DB;
using MoreLinq.Extensions;
using UnityEngine;
using XTutorial;
using Object = UnityEngine.Object;

namespace GameLoading.LoadingStep
{
    /** 
     * 加载玩家数据(callinit)
     */
    public class LoadUserDataStep : LoadingPipelineStep
    {
        public LoadUserDataStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();

            if (AccountManager.Instance.InDeleteAccountProcess)
            {
                D.Log("LoadUserDataStep return! beacuse of deleting account calm period");
                IsDone = true;
                return;
            }
            
            NetApi.Instance.Token = String.Empty;
            TrySwitchSpeedupUrl();
            
            Utils.StartCoroutine(ProcessCoroutine());
        }

        // 原组尝试切回加速组
        private void TrySwitchSpeedupUrl()
        {
            var serverUrl = NetApi.ServerUrl;
            var isBaseGroup = NetServerUpdater.IsServerEquals(serverUrl, GameSetting.Current.ServerUrl);

            if (isBaseGroup && NetServerUpdater.IsGroupAvailable)
            {
                NetServerUpdater.UpdateGroup(serverUrl, false);
            }
        }
        
        private IEnumerator ProcessCoroutine()
        {
            var callinitManager = ManagerFacade.GetManager<CallInitManager>();

            bool processSuccess = false;
            
            yield return callinitManager.ProcessResponse(() => { processSuccess = true;});
            
            Debug.Log($"LoadUserDataStep processSuccess : {processSuccess.ToString()}");
            if (processSuccess)
            {
                AfterLoginSuccess();
                
                if (callinitManager.PoliciesData != null)
                {
                    Debug.Log($"LoadUserDataStep processSuccess 2");

                    UI.UIManager.inst.OpenPopup(UI.UIManager.PopupType.NoticePopup, new NoticePopup.Parameter
                    {
                        ht = callinitManager.PoliciesData
                    });
                }
                else
                {
                    Debug.Log($"LoadUserDataStep processSuccess 3");

                    IsDone = true;
                }    
            }
            else
            {
                Debug.Log($"LoadUserDataStep processSuccess failed,but continue");
                IsDone = true;
            }
        }

        private void AfterLoginSuccess()
        {
            TraceFirebaseLogin();

            AddAllRedPointNode();
            if (!CustomDefine.IsCNDefine())
            {
                SdkEntranceManager.HelpShift.HelpshiftLogin();
            }

            ConnectWather();
        }

        private void ConnectWather()
        {
            string remoteAddress = "";
            if (DatabaseTools.UpdateData(NetApi.Instance.Manifest, "watcher_gate", ref remoteAddress))
            {
                var tcpManager = ManagerFacade.GetManager<TcpManager>();

                var addressParams = remoteAddress.Split(':');
                if (addressParams.Length >= 2)
                {
                    var port = 0;
                    if (int.TryParse(addressParams[1], out port))
                    {
                        tcpManager.SetRemoteAddress(addressParams[0], port);
                        tcpManager.Connect();

                    }
                }
            }
        }

        private void TraceFirebaseLogin()
        {
            FirebaseSDK.TraceLogin();

            FirebaseSDK.CheckUserCTime();
            if (null != CityManager.inst && null != PlayerData.inst && null != PlayerData.inst.heroData)
            {
                int strongholdLevel = CityManager.inst.GetHighestBuildingLevelFor(BuildingType.STRONGHOLD);


                FirebaseSDK.SetUserProperty(FirebaseBIKey.UserId.ToString(), PlayerData.inst.uid.ToString()); //用户id
                FirebaseSDK.SetUserProperty(FirebaseBIKey.City_Level.ToString(), strongholdLevel.ToString()); //城堡等级
                FirebaseSDK.SetUserProperty(FirebaseBIKey.Hero_Level.ToString(),
                    PlayerData.inst.heroData.level.ToString()); //领主等级
                FirebaseSDK.SetUserProperty(FirebaseBIKey.Dragon_Level.ToString(),
                    null != PlayerData.inst.dragonData ? PlayerData.inst.dragonData.Level.ToString() : "0"); //龙等级
            
                //记录用户ID
                FirebaseSDK.TraceLog(FirebaseBIKey.UserId.ToString() + ": " + PlayerData.inst.uid.ToString());
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        private void AddAllRedPointNode()
        {
            var redPointManager = ManagerFacade.RedPointManager;

            var talentPointNode = redPointManager.AddNode(new RPN_TalentPoint());
            var equipmentReplaceable = HeroMainDlgEquipReplacePointCreator.Create(RedPointId.PlayerIconEquipReplaceable);

            redPointManager.AddNode(new RedPointNodeCombination(RedPointId.PlayerIconEntrance, talentPointNode, equipmentReplaceable));
            
            
            var allParliamentHeroInfo = ConfigManager.inst.DB_ParliamentHero.ParliamentHeroInfoList;
            var allHeroCardTip = new RedPointNode[allParliamentHeroInfo.Count];
            
            for(var i = 0; i < allParliamentHeroInfo.Count; i++)
            {
                var parliamentHeroInfo = allParliamentHeroInfo[i];
                allHeroCardTip[i] = ManagerFacade.RedPointManager.AddNode(new RPN_HeroCardTip(parliamentHeroInfo));
                
            }

            var heroPackageEntrance = ManagerFacade.RedPointManager.AddNode(new RedPointNodeCombination(RedPointId.HeroPackageEntrance, allHeroCardTip));
        }

    }
}