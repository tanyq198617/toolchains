using System.Collections.Generic;
using Activity.GuardianTrail.Core;
using ClientCore.Pipeline;
using DW.XRandomQuest;
using UI.Activity.ArthurGuard;
using UI.Intelligence;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    /**
     * 发送所有Loader阶段，不会等待loader返回
     */
    public class SendLoaderStep : LoadingPipelineStep
    {
        private List<string> _allPrerequestLoader = new List<string>();
        private List<string> _allReceivePrerequestLoader = new List<string>();
        
        public SendLoaderStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();

            SendLoader();
            
            IsDone = true;
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        void SendLoader()
        {
            // 其他Loader
            RequestManager.inst.SendLoader(PortType.PVP_GET_WATCH_TOWER,
                Utils.Hash("uid", PlayerData.inst.uid),
                delegate(bool arg1, object arg2)
                {
                    if (GameEngine.IsReady() && arg1)
                    {
                        WatchtowerUtilities.Init();
                    }
                });
            RequestManager.inst.SendLoader(PortType.SEVEN_DAYDS_LOAD_DATA);

            ActivityManager.Instance.CheckPersonalActivity();

            PlayerData.inst.allianceWarManager.OnGameModeReady();
            PlayerData.inst.allianceTechManager.OnGameModeReady();
            PaymentManager.Instance.GetPayStatus();
            MarksmanPayload.Instance.Initialize();
            IapRebatePayload.Instance.Initialize();

            RebateByRechargePayload.Instance.Initialize();
            SuperLoginPayload.Instance.Initialize();
            AnniversaryEntrancePayload.Instance.Initialize();
            MutualAidPayload.Instance.Initialize();
            KingdomBuffPayload.Instance.Initialize();
            LotteryDiyPayload.Instance.Initialize();
            DicePayload.Instance.Initialize();
            TradingHallPayload.Instance.Initialize();
            AnniversaryIapPayload.Instance.Initialize();
            AccountManager.Instance.LogUserInfo();
            AccountManager.Instance.SyncAccountBindData();

            AllianceManager.Instance.OnGameModeReady();
            HeroReformManager.Instance.Initialize();
            PitExplorePayload.Instance.SyncAbyssInfo();
            SubscriptionSystem.Instance.RequestSubscribeDataNonBlock();
            IAPDailyRechargePackagePayload.Instance.Initialize();
            AnniversaryManager.Instance.Initialize();
            Anniversary2YearKingsPayload.Instance.Initialize();
            AuctionHelper.Instance.Initialize();
            ChatAndMailConstraint.Instance.Initialize();
            MerlinTrialsPayload.Instance.Initialize();
            RoulettePayload.Instance.Initialize();
            AllianceWarPayload.Instance.Initialize();
            WonderPayload.Instance.Initialize();
            MerlinTowerPayload.Instance.Initialize();
            PeakednessBattlePayload.Instance.Initialize();
            ValhallaPayload.Instance.Initialize();
            OfflineVerificationPayload.Instance.Initialize();
            GloryCallbackActivityPayload.Instance.Initialize();
            ChapelPayload.Instance.Initialize();
            GrowUpPayload.Instance.Initialize();
            TinyAssistantProxy.Instance.Initialize();
            SuperDefendPayload.Instance.Initialize();
            TimeLimitedGoalPayload.Instance.Initialize();
            DemonInvasionManager.Instance.Initialize();
            MagicLadderManager.Instance.Initialize();
            DailyTaskManager.Instance.Initialize();
            Alliance1v1ActivityPayload.Instance.Initialize();
            SkyHospitalManager.Instance.Initialize();
            RaidShopManager.Instance.Initialize();
            GoldActivityHateManager.Instance.Initialize();
            GoldActivityChestManager.Instance.Initialize();

            RequestManager.inst.SendLoader(PortType.KING_LOAD_HALL_OF_KING_DATA);
            
            TacticsPayload.Instance.Initialize();
            SecretTerritoryPayload.Instance.Initialize();
            LuckyCardManager.Instance.Initialize();
            SingleRebateManager.Instance.Initialize();
            HolidayCarnivalManager.Instance.Initialize();
            GoldActivityRepositoryManager.Instance.Initialize();
            GameLoggedonAnnouncementPayload.Instance.Initialize();
            HuntBossManager.Instance.Initialize();
            IAPLimitManager.Instance.Initialize();

            GoToManager.Instance.Init();
            GradeManager.Instance.Initialize();
            VirtueManager.Instance.Initialize();
            ThirdPartyEventManager.Instance.Initialize();

            Alliance1v1Manager.Instance.Initialize();
            WorldTrendManager.Instance.Initialize();

            CMSGiftStoreManager.Instance.Initialize();
            CakeEventManager.Instance.Initialize();
            FamousCityManager.Instance.Initialize();
            FamousCityNewManager.Instance.Initialize();
            AllianceProtectionManager.Instance.Initialize();
            SeasonRecruitManager.Instance.Initialize();
            IntelManager.Instance.Initialize();
            AmazonPrimeManager.Instance.Initialize();
            HeroStoreManager.Instance.Initialize();
            AllianceShopManager.Instance.Initialize();
            DestinySealManager.Instance.Init();
            MonopolyManager.Instance.Initialize();
            WeekendPrayManager.Instance.Initialize();
            FireworkManager.Instance.Initialize();
            EventCardManager.Instance.Initialize();
#if UNITY_ANDROID
            GoogleActivityManager.Instance.Initialize();
#endif
            UserAuthorizationManager.Instance.Initialize();

            RandomQuestMgr.Instance.Initialize();
            AllianceWarfareManager.Instance.Initialize();
            BarracksManager.Instance.Initialize();
            GuardianTrailMgr.Instance.Initialize();
            //获取成就数据
            RequestManager.inst.SendLoader(PortType.ACHIEVEMENT_LODA_DATA, Utils.Hash("uid",PlayerData.inst.uid));

            SeasonNisaEveManager.Instance.Initialize();
            SeasonNisaStoryManager.Instance.Initialize();
            LimitShopManager.Instance.Initialize();

            SeasonOverviewManager.Instance.Initialize();
            ArthurGuardManager.Instance.Initialize();
            IntelligenceManager.Instance.Initialize();
            FakeMonsterManager.Instance.Initialize();
            KnightHoodManager.Instance.Initialize();
            RTSTokenManager.Instance.Initialize();
            
            DataConfigAutoReset.Instance.Initialize();

            RequestManager.Instance.SendLoader(PortType.ALLIANCE_GET_ALLIANCE_MEMBER_INFO, Utils.Hash("alliance_id", PlayerData.inst.allianceId));

            RequestManager.Instance.SendLoader(PortType.SOCIAL_GET_ALLIANCE_RED_PACKET, Utils.Hash("uid", PlayerData.inst.uid));
        }
    }
}