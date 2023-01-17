using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ClientCore;
using DB;
using DW.JLJQ.CityRepair;
using Funplus;
using I2.Loc;
using UnityEngine;

namespace GameLoading
{
    public class CallInitManager : IManager
    {
        private bool _alreadySendRequest = false;
        
        private Hashtable _response = null;

        public void Initialize()
        {
            MessageHub.inst.GetPortByAction(PortType.CALL_INIT).AddOriginalDataEvent(OnCallInitResponse);
        }
        
        void IManager.Dispose()
        {
            MessageHub.inst.GetPortByAction(PortType.CALL_INIT).RemoveOriginalDataEvent(OnCallInitResponse);
        }
        
        private void OnCallInitResponse(object data)
        {
            _response = data as Hashtable;
            if (_response == null)
            {
                throw new System.Exception("CallInit return null");
            }
        }
        

        public void SendCallInitRequest()
        {
            _alreadySendRequest = true;

            if (AccountManager.Instance.InDeleteAccountProcess)
            {
                D.Log("SendCallInitRequest return! beacuse of deleting account calm period");
                return;
            }
            
            Hashtable paramsHt = new Hashtable();
            paramsHt.Add("app_version", AppSetting.AppVersion);
            paramsHt.Add("fpid", AccountManager.Instance.AccountId);

    #if UNITY_EDITOR || UNITY_ANDROID
            paramsHt.Add("push_service", ThirdPartyPushManager.Instance.GetPushService());
    #endif
            
            if (GameSetting.Current.Channel == ChannelEnum.SQWAN || 
                GameSetting.Current.Channel == ChannelEnum.AndroidCn37Union)
            {
                paramsHt.Add("lang", "zh-CN");
            }
            else
            {
                paramsHt.Add("lang", Language.Instance.GetLocalizationLanguage());
            }
            
            paramsHt.Add("client_version", NativeManager.inst.AppMajorVersion);
            
            string cacheUID = PlayerPrefsEx.GetString(ComeBackGuideManager.CACHEUID,string.Empty);

            if (PlayerPrefsEx.HasKey(ComeBackGuideManager.GOTONEWSERVER + cacheUID))
            {
                paramsHt.Add("kingdom_id", 0);
                PlayerPrefsEx.DeleteKeyByUid(ComeBackGuideManager.GOTONEWSERVER);
                PlayerPrefsEx.DeleteKey(ComeBackGuideManager.CACHEUID);
            }
            else
            {
                paramsHt.Add("kingdom_id", AccountManager.Instance.KingdomID);
            }
            
            paramsHt.Add("cv", NetApi.inst.ConfigFilesVersion);

            paramsHt["os"] = NativeManager.inst.GetOS();
            paramsHt["os_version"] = FunplusSdkUtils.Instance.GetOsVersion();
            Hashtable hash = new Hashtable();
            hash.Add("channel_type", "aws");
            hash.Add("device_type", "phone");
    #if UNITY_EDITOR || UNITY_ANDROID
            hash.Add("platform_type", "gcm");
    #else
		    hash.Add("platform_type","apns");
    #endif
            hash.Add("device_token", AccountManager.Instance.GetPushToken());

            paramsHt["push_message"] = hash;

            if (!string.IsNullOrEmpty(SdkEntranceManager.KgGuard.SmDeviceId))
            {
                paramsHt["sm_device_id"] = SdkEntranceManager.KgGuard.SmDeviceId;
            }
            
            paramsHt["fp_device_id"] = SdkEntranceManager.KgGuard.FpDeviceId;
            
            if (!string.IsNullOrEmpty(NetApi.inst.AdjustTracker))
            {
                paramsHt.Add("adjust_tracker", NetApi.inst.AdjustTracker);
            }

            string channel = GameSetting.Current.ServerChannel;
            if (!string.IsNullOrEmpty(channel))
            {
                paramsHt.Add("channel", channel);
            }

            string imei = "";
            if (GameSetting.Current.Channel == ChannelEnum.AndroidCN ||
                GameSetting.Current.Channel == ChannelEnum.AndroidCN37)
            {
                if (Application.platform == RuntimePlatform.Android)
                {
                    imei = FunplusSdkUtils.Instance.GetAndroidIMEI();
                    if (!string.IsNullOrEmpty(imei))
                    {
                        paramsHt.Add("imei", imei);
                    }
                }
            }

            string language = NativeManager.inst.SysLanguage;
            paramsHt.Add("sys_lang", string.IsNullOrEmpty(language) ? "en_US" : language);

            if (!string.IsNullOrEmpty(AccountManager.Instance.FunplusID))
            {
                paramsHt["fpid"] = AccountManager.Instance.FunplusID;
                paramsHt.Add("session_key", AccountManager.Instance.SessionKey);
            }
            
            if (CustomDefine.IsCNUnionChannel())
            {
                string packageName = PZSDKManager.GetPackageName();
                paramsHt.Add("union_package", packageName);
            
                if (!PZSDKManager.Instance.IsInited)
                {
                    Debug.LogError("[cn37union] login error");

                    string title = ScriptLocalization.Get("network_error_title");
                    string content = ScriptLocalization.Get("network_error_login_fail_description");
                    if (!NetWorkDetector.Instance.IsReadyRestart())
                    {
                        NetWorkDetector.Instance.Send37LoginError2RUM("[InitUserDataState] [cn37union] Login ");
                    }

                    NetWorkDetector.Instance.RestartOrQuitGame(content, title);
                    return;
                }
            }
            
            if (GameSetting.Current.Channel == ChannelEnum.AndroidFlexion)
            {
                string packageName = FunplusSdk.Instance.GetSubPackageChannel();
                paramsHt.Add("union_package", packageName);
            }

            if (!Application.isEditor)
            {
                // cache information for bi events
                paramsHt["social_id"] = ""; // 加接口

                if (Application.platform == RuntimePlatform.IPhonePlayer)
                {
                    string tmp = Funplus.FunplusSdkUtils.Instance.GetIDFA();
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        paramsHt["idfa"] = tmp;
                    }
                    tmp = Funplus.FunplusSdkUtils.Instance.GetIDFV();
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        paramsHt["idfv"] = tmp;
                    }
                }
                paramsHt["currency_code"] = PaymentManager.Instance.GetCurrencyCode();
                paramsHt["time_zone"] = Funplus.FunplusSdkUtils.Instance.GetCurrentTimeZone();
                paramsHt["device_lang"] = Funplus.FunplusSdkUtils.Instance.GetLanguage();
                if (Application.platform == RuntimePlatform.Android)
                {
                    string tmp = Funplus.FunplusSdkUtils.Instance.GetAndroidID();
                    if (!string.IsNullOrEmpty(tmp))
                    {
                        paramsHt["android_id"] = tmp;
                    }
                }

                paramsHt["gaid"] = "";
            }
            
            if (Application.platform == RuntimePlatform.Android)
            {
                string gaid = NativeManager.inst.GAID;
                if (!string.IsNullOrEmpty(gaid))
                {
                    paramsHt["gaid"] = gaid;
                }
            }
            
            // app_instance_id
            {
                var appInstanceId = FirebaseManager.Instance.AppInstanceId;
                if (!string.IsNullOrEmpty(appInstanceId))
                {
                    paramsHt["app_instance_id"] = appInstanceId;
                    FirebaseManager.Instance.SetAppInstanceIdCallback(null);
                }
                else
                {
                    FirebaseManager.Instance.SetAppInstanceIdCallback(delegate(string id)
                    {
                        Utils.SendDeviceInfo(new Hashtable(){{"app_instance_id", id}});
                    });
                }
            }
            
            if (GameSetting.Current.AppsFlyerConfigData.AppsFlyerEnabled)
            {
                var oaid = SdkEntranceManager.KgTools.Oaid;
                if (!string.IsNullOrEmpty(oaid))
                {
                    paramsHt["appsflyer_oaid"] = oaid;
                }
                else
                {
#if  UNITY_ANDROID
                    SdkEntranceManager.KgTools.SetOaidUpdateCallback(delegate(string updatedOaid)
                        {
                            D.Log("[AFBI] SetOaidUpdateCallback");
                            Hashtable param = new Hashtable();
                            param.Add("bundle_id", Application.identifier);
                            param.Add("oaid", updatedOaid);
                            param.Add("att", "");

                            Utils.SendDeviceInfo(param);
                        }
                    );
                        
#elif UNITY_IPHONE
                    Hashtable param = new Hashtable();
                    param.Add("bundle_id", Application.identifier);

                    string idfa = FunplusSdkUtils.Instance.GetIDFA();
                    bool idfaValid = !string.IsNullOrEmpty(idfa) &&
                                     !idfa.Equals("0") &&
                                     !idfa.Equals("unknown") &&
                                     !idfa.Equals("error");
                    string osVersion = FunplusSdkUtils.Instance.GetOsVersion();
                    bool osVersionValid = Utils.GreaterThanTargetOSVersion(osVersion, "14.0");

                    if (idfaValid && osVersionValid)
                    {
                        param.Add("att", 3);
                    }

                    Utils.SendDeviceInfo(param);
#endif
                }
                
                var appsflyerEntrance = SdkEntranceManager.AppsFlyer;
                if (appsflyerEntrance != null)
                {
                    var appflyerId = appsflyerEntrance.GetAppsFlyerId();
                    
                    paramsHt["appsflyer_id"] = appflyerId;
                }
                
                var appstoreName = SdkEntranceManager.AppsFlyer.InstallAppStore;
                paramsHt["app_store_name"] = appstoreName;
            }

            if (GameSetting.Current.KgShumengConfigData.Enabled)
            {
                var kgShumeng = SdkEntranceManager.KgShumeng;
            
                if (kgShumeng.Inited)
                {
                    kgShumeng.AppendDataToHashtable(paramsHt);
                }
                else
                {
                    kgShumeng.SetInitCallback(delegate( )
                    {
                        var data = new Hashtable();
                
                        kgShumeng.AppendDataToHashtable(data);

                        Utils.SendDeviceInfo(data);
                    });
                }
            }
            
            if (NetApi.inst.PatchContent != null && NetApi.inst.PatchContent.Count > 0)
            {
                paramsHt["patch_version"] = NetApi.inst.PatchContent.Keys.ToArray();
            }

            paramsHt["session_key"] = AccountManager.Instance.SessionKey;

    #if UNITY_EDITOR
            paramsHt["session_key"] = "editor";
    #endif

#if !UNITY_EDITOR
            paramsHt["phone_model"] = SystemInfo.deviceModel.ToString();
#endif

            RequestManager.inst.SendLoader(PortType.CALL_INIT, paramsHt, delegate(bool result, object o) 
            {
                if (!result)
                {
                    Utils.RestartGameWithErrorCode(ErrorCode.ErrorCallInitFailed);
                }
            }, false);
        }

        public IEnumerator ProcessResponse(System.Action callback)
        {
            if (!_alreadySendRequest)
            {
                SendCallInitRequest();
            }
            
            while (_response == null)
            {
                yield return null;
            }
            
            yield return OnResponse(_response);
            
            if (callback != null)
            {
                callback();
            }

            _response = null;
        }

        private IEnumerator OnResponse(Hashtable response)
        {
            var preload = response["payload"] as Hashtable;

            ProcessVipLevel(preload);
            
            ProcessAlipay(preload);

            ProcessTileArea(preload);

            ProcessIsAccountUnbind(preload);

            ProcessBindReward(preload);

            ProcessIsWorldNewSearch(preload);

            ProcessIsCityForceLand(preload);

            ProcessToken(preload);

            ProcessChanceUrl(preload);

            ProcessAccountInfo(preload);

            ProcessAllianceTreasury(preload);

            ProcessGmContact(preload);

            ProcessCountry(preload);
            
            ProcessIsOpenTroopUpgrade(preload);

            ProcessThirdPartPush(preload);

            ProcessOpenTavernWheel(preload);

            ProcessFunctionSwitch(preload);

            ProcessABTestMapping(preload);

            ProcessOptUrl(preload);

            ProcessZipIcon(preload);
            
            ProcessCityDecoration(preload);
            
            Process37Union(preload);
            
            ProcessHealingCoin(preload);
            ProcessAllianceMineBoxRewards(preload);
            
            object data = response["data"] as Hashtable;
            
            var token = string.Empty;
            DatabaseTools.UpdateData(preload, "token", ref token);

            long time = 0;
            DatabaseTools.UpdateData(response, "time", ref time);
            
            if (time != 0 && token != null && data != null)
            {
                //time
                NetServerTime.inst.SetServerTime(time / 1000.0);
                NetServerTime.inst.StartSyncTime();

#if !UNITY_EDITOR
			        Funplus.FunplusPayment.Instance.SetCurrencyWhitelist(null);
#endif
                //token
                NetApi.inst.Token = token;
                AccountManager.Instance.CheckAccountType();

                long uid = 0;
                if (DatabaseTools.UpdateData(preload, "uid", ref uid))
                {
                    PlayerData.inst.hostPlayer.SetUid(uid);
                    PlayerPrefsEx.SetString(LocalPreferenceConst.SAVED_UID, uid.ToString());
                }
                else
                {
                    D.Error("token decode fail!!!");
                }
                
                //database
                using (new CostTimePrinter("PlayerData.LoadDatas"))
                {
                    yield return DBManager.inst.LoadDatas(data, time);
                }
                
                using(new CostTimePrinter("PlayerData.InitData"))
                {
                    yield return PlayerData.inst.InitData(preload);
                }
                
                PlayerData.inst.moderatorInfo.InitFromHashtable(data as Hashtable);
                
                ProcessCallback(preload);
                
                ProcessAnnvData(preload);

                ProcessAccount(preload);

                ProcessIOS14(preload);

                LoadKingdomList();

                ProcessIsHadKing(preload);
                
                ProcessRP(preload);
                
                ProcessUnionLogin(preload);

                ProcessAfterLogin(preload);

                ProcessOpenNewKgAccount(preload);

                LanguageSDK.Instance.SetBiLanguage(true);

                InitKingsGroupCMS();
                InitPayUrl(NetApi.inst.Manifest);

                InitPaymentDefaultIapList();
                InitCMSGiftStore();
                
                Hashtable saveData = null;
                DatabaseTools.UpdateData(preload, ServerConst.tutorial_v2,  ref saveData);
                
                using (new CostTimePrinter("CityRepairMgr.Initialize"))
                {
                    yield return CityRepairMgr.Instance.Initialize(saveData ?? new Hashtable());
                }

                if (GameSetting.Current.Channel == ChannelEnum.AndroidCN37 ||
                    GameSetting.Current.Channel == ChannelEnum.AndroidCn37Union)
                {
                    SdkEntranceManager.NHPSDK.SetRoleInfo();
                }
            }
            else
            {
                string message = "";
                if (response.Contains("msg"))
                {
                    message = response["msg"].ToString();
                }

                string title = ScriptLocalization.Get("data_error_title");
                string content = ScriptLocalization.Get("data_error_initialization3_description") + "[" +
                                 ErrorCode.ErrorInitUserData + "]";
                if (!string.IsNullOrEmpty(message))
                {
                    D.Log("[Request Error][Loading] Init user data fail!");
                    if (!NetWorkDetector.Instance.IsReadyRestart())
                    {
                        NetWorkDetector.Instance.SendRestartGameError2RUM(NetWorkDetector.RestartErrorType.Loading,
                            "InitUserDataFail", message);
                    }
                }

                NetWorkDetector.Instance.RestartOrQuitGame(content, title);
            }

            if (NexgenDragon.Localization.Instance.CurrentLanguage != PlayerData.inst.userData.language)
            {
                NexgenDragon.Localization.Instance.CurrentLanguage = PlayerData.inst.userData.language;
                NexgenDragon.Localization.Instance.ReloadLoadingLanguage();
                NexgenDragon.Localization.Instance.ReloadInGameLanguage();
            }
            
#if UNITY_IOS
            UserAuthorizationManager.Instance.TryForceOpenAutherization();
#endif
            ProcessPolicies(preload);
            
            TrainHelper.ResetNoExceedBaseCapacityTipsRecorder();
        }

        private void ProcessOpenNewKgAccount(Hashtable preload)
        {
            AccountManager.Instance.SetShowKGDissmiss(
                SwitchManager.Instance.GetSwitch(SwitchConst.KG_ACCOUNT_CAN_DISMISS));
        }

        private void ProcessVipLevel(Hashtable preload)
        {
            string vipLevel = string.Empty;
            DatabaseTools.UpdateData(preload, "vip_level", ref vipLevel);
            PlayerData.inst.paymentVipLevel = vipLevel;

        }
        private void ProcessIOS14(Hashtable preload)
        {
            if (preload.ContainsKey("ios14_idfa") && preload["ios14_idfa"] != null)
            {
                UserAuthorizationManager.Instance.IsIOSAuthorizationABTest =
                    preload["ios14_idfa"].ToString() == "2";
            }
        }
        
        private void ProcessAccount(Hashtable preload)
        {
            if (preload.ContainsKey("is_certifying_required") && preload["is_certifying_required"] != null)
            {
                AccountManager.Instance.SetCertifyingRequired(preload["is_certifying_required"].ToString());
            }

            if (preload.ContainsKey("patch_needed") && preload["patch_needed"] != null)
            {
                AccountManager.Instance.NeedPathCheck = preload["patch_needed"].ToString() == "1";
            }
                
            if (preload.ContainsKey("start_new_game") && preload["start_new_game"] != null)
            {
                AccountManager.Instance.StartNewGameFlag = Utils.TryParseLong(preload, "start_new_game");
            }
        }
        
        private void ProcessAfterLogin(Hashtable preload)
        {
            var isNewUser = false;
            DatabaseTools.UpdateData(preload, "is_new", ref isNewUser);
                
            CrashReportCustomField.SetValue(CrashReportCustomField.Key.IsNewUser, isNewUser.ToString());

            var cityManager = PlayerData.inst.CityData;
            if (cityManager != null)
            {
                var cityData = cityManager.MyCityData;
                if (cityData != null)
                {
                    CrashReportCustomField.SetValue(CrashReportCustomField.Key.UserKingdom, cityManager.Location.K.ToString());
                    CrashReportCustomField.SetValue(CrashReportCustomField.Key.UserCityLevel, cityData.CityLevel.ToString());
                }
                
            }
            
            if (isNewUser)
            {
                SdkEntranceManager.FPMarketUnion.TraceCreateRoleEvent(FPMarketUnionEntrance.Channel.QuickHand);
                SdkEntranceManager.FPMarketUnion.TraceRegisterEvent(FPMarketUnionEntrance.Channel.TencentADs);
            }
                
            if (GameSetting.Current.KGRangersAppConfigData.Enabled)
            {
                if (isNewUser)
                {
                    SdkEntranceManager.KGRangersApp.SendRegisterEvent();
                }

                SdkEntranceManager.KGRangersApp.SendLoginEvent();
            }

            if (GameSetting.Current.Channel == ChannelEnum.PCSim)
            {
                PCSimManager.Instance.ServerLogin();
            }
            
            SdkEntranceManager.KGHelpCenter.Init();
            SdkEntranceManager.FPAdNetwork.Initialize();
            SdkEntranceManager.FPAdNetwork.SendLoginEvent();
            SdkEntranceManager.FPMarketUnion.TraceEnterGameEvent(FPMarketUnionEntrance.Channel.TencentADs);
        }
        
        private void ProcessUnionLogin(Hashtable preload)
        {
#if UNITY_ANDROID
            if (CustomDefine.IsCNUnionChannel())
            {
                if (PZSDKManager.Instance.IsNewUser)
                {
                    
                    FunplusSdk.Instance.LogNewUser(AccountManager.Instance.AccountId);
                    FunplusSdk.Instance.LogUserLogin(AccountManager.Instance.AccountId);
                }
                else
                {
                    // 非新用户
                    FunplusSdk.Instance.LogUserLogin(AccountManager.Instance.AccountId);
                }
            }
#endif
        }
        
        private void ProcessRP(Hashtable preload)
        {
            //实名认证的认证状态 现在要在初始化玩家数据的时候 发过来
            if (preload.ContainsKey("rp") && preload["rp"] != null)
            {
                string objStr = preload["rp"].ToString();
                int rPrice = 0;
                DB.DatabaseTools.UpdateData(objStr, ref rPrice);
                AccountManager.Instance.RecentPrice = rPrice;
            }
        }
        
        private void ProcessIsHadKing(Hashtable preload)
        {
            //<国王喇叭屏蔽
            if (preload.ContainsKey("is_had_king") && preload["is_had_king"] != null)
            {
                string objStr = preload["is_had_king"].ToString();
                int hadKingInt = 0;
                if (DatabaseTools.UpdateData(objStr, ref hadKingInt))
                {
                    GameEngine.Instance.ChatManager.KingSpeakerStatus = 1 == hadKingInt;
                }
            }
        }
        
        private void LoadKingdomList()
        {
            if (PlayerData.inst.userData != null)
            {
                AccountManager.Instance.KingdomID = PlayerData.inst.userData.world_id;

                //加载kingdom list
                WorldMapData.Instance.LoadKingdomList(
                    WorldMapData.MapLoadType.All /*NewGroupManager.Instance.IsNewServer ? WorldMapData.MapLoadType.New : WorldMapData.MapLoadType.Old*/);
            }
        }
        
        private void ProcessAlipay(Hashtable preload)
        {
#if UNITY_ANDROID
            if (preload.ContainsKey("is_alipay_open") && preload["is_alipay_open"] != null)
            {
                bool aliEnabled = false;

                DB.DatabaseTools.UpdateData(preload, "is_alipay_open", ref aliEnabled);

                IapPaymentModePopup.AliEnabled = aliEnabled;
            }
#endif
        }

        private void ProcessAnnvData(Hashtable preload)
        {
            if (preload.ContainsKey("annv_data") && preload["annv_data"] != null)
            {
                AnniversaryManager.Instance.SetUp(preload["annv_data"]);
            }
            
            if (preload.ContainsKey("annv_conf") && preload["annv_conf"] != null)
            {
                AnniversaryIapPayload.Instance.SetTimeFromServer(preload["annv_conf"] as Hashtable);
            }
        }
        
        private void ProcessCallback(Hashtable preload)
        {
            //回流玩家赠礼
            if (preload.ContainsKey("call_back_rewards") && preload["call_back_rewards"] != null)
            {
                BackFlowManager.Instance.Decode(preload["call_back_rewards"]);
            }
        }
        
        private void ProcessTileArea(Hashtable preload)
        {
            //设置 tile大小映射关系
            if (preload.ContainsKey("tile_area"))
            {
                MapUtils.SetTileAreaData(preload["tile_area"] as Hashtable);
            }
        }

        private void ProcessIsAccountUnbind(Hashtable preload)
        {
            if (preload.ContainsKey("is_open_account_unbind") && preload["is_open_account_unbind"] != null)
            {
                string tmp = preload["is_open_account_unbind"].ToString();
                tmp = tmp.Trim();
                tmp = tmp.ToLower();
                if (tmp != "true")
                {
                    AccountManager.Instance.LockReplaceAccount();
                }
            }
        }

        private void ProcessBindReward(Hashtable preload)
        {
            if (preload.ContainsKey("kingsgroup_claim_bind_reward") &&
                preload["kingsgroup_claim_bind_reward"] != null)
            {
                string tmp = preload["kingsgroup_claim_bind_reward"].ToString();
                tmp = tmp.Trim();
                tmp = tmp.ToLower();
                AccountManager.Instance.HasRewards = false;
                if (tmp == "true")
                {
                    AccountManager.Instance.HasRewards = true;
                }
            }
        }

        private void ProcessIsCityForceLand(Hashtable preload)
        {
            if (preload.ContainsKey("is_city_force_land") && preload["is_city_force_land"] != null)
            {
                string tmp = preload["is_city_force_land"].ToString();
                tmp = tmp.Trim();
                tmp = tmp.ToLower();
                AccountManager.Instance.CanCover = false;
                if (tmp == "true")
                {
                    AccountManager.Instance.CanCover = true;
                }
            }

        }

        private void ProcessIsWorldNewSearch(Hashtable preload)
        {
            if (preload.ContainsKey("is_world_new_search") && preload["is_world_new_search"] != null)
            {
                string tmp = preload["is_world_new_search"].ToString();
                tmp = tmp.Trim();
                tmp = tmp.ToLower();
                AccountManager.Instance.IsNewQuickSearch = false;
                if (tmp == "true")
                {
                    AccountManager.Instance.IsNewQuickSearch = true;
                }
            }
        }

        private void ProcessAccountInfo(Hashtable preload)
        {
            for (int i = 0; i < AccountManager.MAX_ACCOUNT; i++)
            {
                string key = "account_name_" + (i + 1);
                string typeKey = "account_type_" + (i + 1);
                string timeKey = "account_mtime_" + (i + 1);
                if (preload.ContainsKey(key) && preload[key] != null
                                             && preload.ContainsKey(typeKey) && preload[typeKey] != null)
                {
                    string accountId = preload[key].ToString();
                    string accountType = preload[typeKey].ToString();
                    int ctime = 0;
                    if (preload[timeKey] != null)
                    {
                        string tmp = preload[timeKey].ToString();
                        int.TryParse(tmp, out ctime);
                    }

                    AccountManager.Instance.AddAccount(accountId, accountType, "");
                }
            }
        }

        private void ProcessCountry(Hashtable preload)
        {
            if (preload.ContainsKey("country") && preload["country"] != null)
            {
                string country = preload["country"].ToString();
                PlayerData.inst.Country = country;
            }
            else
            {
                PlayerData.inst.Country = null;
            }
        }

        private void ProcessIsOpenTroopUpgrade(Hashtable preload)
        {
            if (preload.ContainsKey("is_open_upgrade_troop") && preload["is_open_upgrade_troop"] != null)
            {
                string booleanStr = preload["is_open_upgrade_troop"].ToString();
                booleanStr = booleanStr.Trim();
                booleanStr = booleanStr.ToLower();

                if (booleanStr == "true")
                {
                    BarracksManager.Instance.IsUpgradeTroopOpen = true;
                }
                else
                {
                    BarracksManager.Instance.IsUpgradeTroopOpen = false;
                }
            }
        }

        private void ProcessThirdPartPush(Hashtable preload)
        {
#if UNITY_EDITOR || UNITY_ANDROID
            ThirdPartyPushManager.Instance.SetAlias(AccountManager.Instance.FunplusID);
#endif
        }

        private void ProcessOpenTavernWheel(Hashtable preload)
        {
            
            if (preload.ContainsKey("open_tavern_wheel") && preload["open_tavern_wheel"] != null)
            {
                RoulettePayload.Instance.IsOpen = true;
                Hashtable tavernWheel = preload["open_tavern_wheel"] as Hashtable;
                if (tavernWheel.ContainsKey("groupId") && tavernWheel["groupId"] != null)
                {
                    long groupId = Utils.TryParseLong(tavernWheel, "groupId");
                    RoulettePayload.Instance.GroupId = groupId;
                }

                if (tavernWheel.ContainsKey("end") && tavernWheel["end"] != null)
                {
                    long endTime = Utils.TryParseLong(tavernWheel, "end");
                    RoulettePayload.Instance.EndTime = endTime;
                }
            }
            else
            {
                RoulettePayload.Instance.IsOpen = false;
            }

        }

        private void ProcessFunctionSwitch(Hashtable preload)
        {
            if (preload.ContainsKey("func_switch"))
            {
                SwitchManager.Instance.Append(preload["func_switch"] as Hashtable);
            }
        }

        private void ProcessABTestMapping(Hashtable preload)
        {
            if (preload.Contains("abtest_mapping"))
            {
                Hashtable abtest_mapping = preload["abtest_mapping"] as Hashtable;
                if (abtest_mapping != null)
                {
                    ABTestManager.Instance.SetSplitRelateData(abtest_mapping);
                }
            }
        }

        private void ProcessCityDecoration(Hashtable preload)
        {
            CityDecorationData decoration  = new CityDecorationData();
            decoration.Decode(preload);
        }

        private void ProcessToken(Hashtable preload)
        {
            if (preload.ContainsKey("chat_token") && preload["chat_token"] != null)
            {
                //rtmToken
                NetApi.inst.RtmToken = preload["chat_token"].ToString();
            }

            if (preload.ContainsKey("kg_token") && preload["kg_token"] != null)
            {
                //Web Token
                NetApi.inst.WebToken = preload["kg_token"].ToString();
            }
            
            if (preload.Contains("anni_token") && preload["anni_token"] != null)
            {
                AnniversaryEntrancePayload.Instance.AnniversaryToken = preload["anni_token"].ToString();
            }
            else
            {
                AnniversaryEntrancePayload.Instance.AnniversaryToken = string.Empty;
            }
        }

        private void ProcessAllianceTreasury(Hashtable preload)
        {
            if (preload.ContainsKey("alliance_treasury_vault_count"))
            {
                int unOpenedChestCount = 0;
                if (int.TryParse(preload["alliance_treasury_vault_count"].ToString(), out unOpenedChestCount))
                {
                    AllianceTreasuryCountManager.Instance.SetUnopendChestCount(unOpenedChestCount);
                }
            }
        }

        private void ProcessGmContact(Hashtable preload)
        {
            if (preload.ContainsKey("gm_contact") && preload["gm_contact"] != null)
            {
                string value = preload["gm_contact"].ToString();
                AccountManager.Instance.SetGMContact(value);
            }

        }

        private void ProcessChanceUrl(Hashtable preload)
        {
            if (preload.ContainsKey("chance_url") && preload["chance_url"] != null)
            {
                ProbabilityManager.Instance.Decode(preload["chance_url"]);
            }
        }

        private void Process37Union(Hashtable preload)
        {
#if UNITY_ANDROID
            if (GameSetting.Current.Channel == ChannelEnum.AndroidCn37Union)
            {
                string preloadInfo = Utils.Object2Json(preload);
                Debug.Log(string.Format("[PZSDK] preloadInfo : {0}", preloadInfo));

                if (preload.ContainsKey("help_37"))
                {
                    PZSDKManager.Instance.Help37Url = preload["help_37"] as string;
                }

                if (preload.ContainsKey("update_37"))
                {
                    PZSDKManager.Instance.UpdateUrl = preload["update_37"] as string;
                }
            }
#endif
        }


        private void ProcessOptUrl(Hashtable preload)
        {
            if (preload.Contains("opt_url") && preload["opt_url"] != null)
            {
                AnniversaryEntrancePayload.Instance.OperationURL = preload["opt_url"].ToString();
            }
            else
            {
                AnniversaryEntrancePayload.Instance.OperationURL = string.Empty;
            }
        }

        private void ProcessZipIcon(Hashtable preload)
        {
            if (preload.Contains("zip_icon"))
            {
                IAPLimitManager.Instance.SetMergeValue(int.Parse(preload["zip_icon"].ToString())== 1);
            }
        }
        

        private void ProcessPolicies(Hashtable preload)
        {
            _policiesData = null;
            if (preload["policies"] != null)
            {
                _policiesData = preload["policies"] as Hashtable;
            }
        }

        private Hashtable _policiesData = null;
        public Hashtable PoliciesData
        {
            get { return _policiesData; }
        }

        #region 今日是否已经领取治疗币

        private void ProcessHealingCoin(Hashtable preload)
        {
            DatabaseTools.UpdateData(preload, "is_received_healing_coin", ref _healingCoin);
        }
        
        private long _healingCoin = 0;

        public long HealingCoin
        {
            get => _healingCoin;
            set => _healingCoin = value;
        }

        #endregion

        #region v14.1联盟矿宝箱奖励

        private void ProcessAllianceMineBoxRewards(Hashtable preload)
        {
            string allianceMineBoxRewardKey = "territory_mineral_info";
            if (preload.Contains(allianceMineBoxRewardKey))
            {
                AllianceMineManager.Instance.DecodeAllianceMineBoxReward(preload[allianceMineBoxRewardKey]);
            }
        }

        #endregion

        private void InitCMSGiftStore()
        {
            CMSGiftStoreManager.Instance.InitGiftStoreSdk();
        }
        
        private void InitPaymentDefaultIapList()
        {
            PaymentManager.Instance.SendDefaultIapListToPayment();
        }
        
        private void InitPayUrl(Hashtable payload)
        {
            if (payload != null)
            {
                string payUrl = string.Empty;

                if (DatabaseTools.UpdateData(payload, "pay_url", ref payUrl))
                {
                    var userData = PlayerData.inst.userData;

                    string gameId = GameSetting.Current.FunplusGameId;
                    string fpid = AccountManager.Instance.FunplusID;
                    string gameUid = PlayerData.inst.uid.ToString();
                    string lang = userData.language;
                    string paymentId = PaymentManager.Instance.PaymentServerId;

                    var userInfo = new Hashtable
                    {
                        {"gameId", gameId},
                        {"paymentId", paymentId},
                        {"gameUid", gameUid},
                        {"fpid", fpid},
                        {"lang", lang},
                        {"pkgChannel", userData.PkgChannel},
                        {"appotaCheckIdSecretKey", "1a98bdbf4e22c5f5a77d7191c0ddf1fd66b8a14e"}, // new add
                        {"appotaGameId", "koa"} // new add
                    };

                    var channelChannel = Utils.GetPayType();
                    var payChannelInfo = new ArrayList
                    {
                        new Hashtable
                        {
                            {"payChannel", channelChannel},
                            {"baseUrl", payUrl}
                        }
                    };

                    var parameter = new Hashtable();
                    parameter.Add("userInfo", userInfo);
                    parameter.Add("payChannelInfo", payChannelInfo);

                    var parameterJson = Utils.Object2Json(parameter);

                    D.Log("InitPayUrl:{0}", parameterJson);
                    FunplusSoutheastAsia.Instance.initPayment(parameterJson);
                }
            }
        }
        
        protected void InitKingsGroupCMS()
        {
            D.Log("<color=#00ff00> >>>>>>>>>>InitKingsGroupCMS<<<<<<<<<< </color>");

            try
            {
                string key = CMSManager.Instance.Key;
                ArrayList list = CMSManager.Instance.Params;
                List<string> ps = new List<string>();
                foreach (var p in list)
                {
                    ps.Add(Utils.XLAT(p.ToString()));
                }

                for (int i = 0; i < ps.Count; i++)
                {
                    string marker = "{" + i + "}";
                    key = key.Replace(marker, ps[i]);
                }

                FunplusSdkUtils.Instance.CMSParam = key;

                if (FunplusSdkUtils.Instance.CMSParam != null)
                {
                    D.Log("[InitKingsGroupCMS] FunplusSdkUtils.Instance.CMSParam = " + FunplusSdkUtils.Instance.CMSParam);

                    object cmsParam = Utils.Json2Object(FunplusSdkUtils.Instance.CMSParam);
                    Hashtable cmsParamHD = cmsParam as Hashtable;
                    if (cmsParamHD == null) cmsParamHD = new Hashtable();

                    var userData = PlayerData.inst.userData;
                    if (userData != null)
                    {
                        string gameId = FunplusSdk.Instance.GameId;
                        string fpid = AccountManager.Instance.FunplusID;
                        string gameUid = userData.uid.ToString();
                        string gameUserName = userData.userName;
                        string lang = userData.language;
                        string serverId = userData.world_id.ToString();
                        string pkgChannel = FunplusSdk.Instance.PackageChannel;
                        string anniToken = AnniversaryEntrancePayload.Instance.AnniversaryToken;
                        string cityLevel = PlayerData.inst.playerCityData.level.ToString();
                        string tagId = string.Empty;

                        Hashtable cmsUserInfo = new Hashtable();
                        cmsUserInfo.Add("gameId", gameId);
                        cmsUserInfo.Add("fpid", fpid);
                        cmsUserInfo.Add("gameUid", gameUid);
                        cmsUserInfo.Add("gameUserName", gameUserName);
                        cmsUserInfo.Add("lang", lang);
                        cmsUserInfo.Add("serverId", serverId);
                        cmsUserInfo.Add("pkgChannel", pkgChannel);
                        cmsUserInfo.Add("gameToken", NetApi.Instance.WebToken);
                        cmsUserInfo.Add("anniToken", anniToken);
                        cmsUserInfo.Add("cityLevel", cityLevel);
                        cmsUserInfo.Add("diamond", PlayerData.inst.userData.currency.diamond);
                        
                        var groupConfig = NewGroupManager.Instance.GetNewGroupConfig(userData.world_id);
                        tagId = groupConfig != null ? groupConfig.TagId : tagId; 
                        cmsUserInfo.Add("tagId", tagId);
                        

                        cmsParamHD.Add("userInfo", cmsUserInfo);


                        ArrayList payChannelList = new ArrayList();

    #if UNITY_ANDROID
                        if (GameSetting.Current.Channel == ChannelEnum.AndroidCN || GameSetting.Current.Channel == ChannelEnum.AndroidCN37)
                        {
                            payChannelList.Add("wechat");
                            if (IapPaymentModePopup.AliEnabled)
                                payChannelList.Add("alipay");
                        }
    #endif
                        cmsParamHD.Add("supportPayChannel", payChannelList);
                        cmsParamHD.Add("isUseDiamond", CustomDefine.IsDiamondEnabled ? 1 : 0);

                        string cmsJson = Utils.Object2Json(cmsParamHD);
                        D.Log("cmsUserInfo = " + cmsJson);
                        FunplusSdkUtils.Instance.InitCMS(cmsJson);

                        if (SwitchManager.Instance.GetSwitch(SwitchConst.USE_ACCOUNT_MANAGER_SDK) ||
                            AccountManager.Instance.UseFPUserCenter)
                        {
                            // {{key,value},{}}
                            string finalStr = "{{{0},{1}}}";
                            string thirdSocialInfo = AccountManager.Instance.GetSupportedThirdSocialPlatform();
                            string sdkAccoundOpenStatus = "\"is_sdk_account_manager\":{0}";
                            sdkAccoundOpenStatus = string.Format(sdkAccoundOpenStatus,
                                SwitchManager.Instance.GetSwitch(SwitchConst.USE_ACCOUNT_MANAGER_SDK).ToString());
                            sdkAccoundOpenStatus = sdkAccoundOpenStatus.ToLower();
                            finalStr = string.Format(finalStr, thirdSocialInfo, sdkAccoundOpenStatus);

                            string userName = PlayerData.inst.userData != null ? PlayerData.inst.userData.userName : "";
                            string avata = PlayerData.inst.userData != null ? PlayerData.inst.userData.Icon : "";;
                            finalStr = $"{{{thirdSocialInfo},{sdkAccoundOpenStatus},\"userName\":\"{userName}\", \"avatar\":\"{avata}\"}}";
                            D.Log($"[FpUpgrade] InitKingsGroupCMS finalStr:{finalStr}");

                            FunplusSdk.Instance.LogUserInfoUpdate(finalStr);
                        }

                        CMSManager.Instance.SetDefaultProductList();

                        CMSManager.Instance.SetGold();
                    }
                    else
                    {
                        D.Error("No user data to initialize KingsGroup CMS SDK");
                    }
                }
            }
            catch (System.Exception e)
            {
                D.Error("KingsGroup CMS SDK init error !!!");
            }

            if (GameSetting.Current.Channel == ChannelEnum.SQWAN)
            {
                D.Log("<color=#00ff00> >>>>>>>>>>InitKingsGroupCRM<<<<<<<<<< </color>");
                FunplusSdkUtils.Instance.InitCRM(null);
            }
        }
    }
}