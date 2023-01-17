using System.Collections;
using _Script.XPerf;
using ClientCore;
using ClientCore.Pipeline;
using DB;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    /**
     * 向服务器请求版本信息(Manifest)
     */
    public class LoadManifestStep : LoadingPipelineStep
    {
        public LoadManifestStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
	        base.OnStart();
	        
            MessageHub.Instance.GetPortByAction(PortType.MANIFEST).AddOriginalDataEvent(OnManifestResponse);
            
            SendManifestRequest();

            var gameLoadingManager = ManagerFacade.GetManager<GameLoadingManager>();
            gameLoadingManager.CleanObsoleteFiles();
        }

        public override void OnEnd()
        {
	        base.OnEnd();
	        
            MessageHub.Instance.GetPortByAction(PortType.MANIFEST).RemoveOriginalDataEvent(OnManifestResponse);
        }
        
  
        private void OnManifestResponse(object data)
        {
            var response = data as Hashtable;
			
            var htManifest = response["payload"] as Hashtable;
            
            NetApi.Instance.Manifest = htManifest;
			
            if(CheckRedirect(htManifest))
            {
                return;
            }
            
            if (!CheckVersion())
            {
	            return;
            }

            if (htManifest != null)
            {
	            NetServerUpdater.Decode(htManifest["server_addr"] as ArrayList);

				DecodeDeleteAccountInfo();

	            ABTestManager.Instance.Init(htManifest["abtest"] as Hashtable);

	            if (htManifest.ContainsKey("payment"))
	            {
		            string channel = htManifest["payment"].ToString();
		            PaymentManager.Instance.PaymentServerId = channel;
	            }

	            if (htManifest.ContainsKey("v"))
	            {
		            string channel = htManifest["v"].ToString();
		            CustomDefine.SERVER_ENV = channel;
	            }
	            else
	            {
		            CustomDefine.SERVER_ENV = CustomDefine.ServerEnvironment.Production;
	            }

	            if (htManifest.ContainsKey("loadingAnnounce") && htManifest["loadingAnnounce"] != null)
	            {
		            string content = htManifest["loadingAnnounce"].ToString();
		            GameLoadingAnnouncementPayload.Instance.AnnouncementContent = content;
	            }
	            else
	            {
		            GameLoadingAnnouncementPayload.Instance.AnnouncementContent = null;
	            }

	            CheckSDK();

	            if (htManifest.ContainsKey("app_id") && htManifest["app_id"] != null)
	            {
		            PaymentManager.Instance.PaymentAppId = htManifest["app_id"].ToString();
	            }

	            if (htManifest.ContainsKey("client_switch"))
	            {
		            SwitchManager.Instance.Init(htManifest["client_switch"] as Hashtable);
		            if (SwitchManager.Instance.GetSwitch("device_log"))
			            GameEngine.Instance.LogSwitch(true);
	            }

	            if (htManifest.ContainsKey("ai_service_url") && htManifest["ai_service_url"] != null)
	            {
		            SdkEntranceManager.KGHelpCenter.FAQBaseUrl = htManifest["ai_service_url"].ToString();
	            }
	            if (htManifest.ContainsKey("ticket_service_url") && htManifest["ticket_service_url"] != null)
	            {
		            SdkEntranceManager.KGHelpCenter.TicketsBaseUrl = htManifest["ticket_service_url"].ToString();
	            }
	            
	            if (htManifest.ContainsKey("sdk"))
	            {
		            var sdkSetting = htManifest["sdk"] as Hashtable;
		            if (sdkSetting != null)
		            {
			            string cvBaseUrl = null;
			            DatabaseTools.UpdateData(sdkSetting, "cv_log", ref cvBaseUrl);
			            SdkEntranceManager.FPAdNetwork.SetBaseUrl(cvBaseUrl);
		            
			            string sdkSettingJson = Utils.Object2Json(htManifest["sdk"]);

			            int rightBraceIndex = 0;
			            if (htManifest.Contains("cdn"))
			            {
				            ArrayList oriCdnData = htManifest["cdn"] as ArrayList;
				            if (oriCdnData != null && oriCdnData.Count > 0)
				            {
					            string cdnUrl = oriCdnData[0].ToString();
					            rightBraceIndex = sdkSettingJson.IndexOf("}");
					            string cdnInfo = ",\"cdnSource\":\"" + cdnUrl + "\"";
					            sdkSettingJson = sdkSettingJson.Insert(rightBraceIndex, cdnInfo);
				            }
			            }

			            if (htManifest.Contains("bundle_cdn"))
			            {
				            NetApi.AssetBundleCdnUrl = htManifest["bundle_cdn"].ToString();
			            }
			            
			            rightBraceIndex = sdkSettingJson.IndexOf("}");
			            string fpUserCenterInfo = AccountManager.Instance.GetFPUserCenterParam(htManifest);
			            fpUserCenterInfo = $",\"fpUserCenter\":{fpUserCenterInfo}";
			            sdkSettingJson = sdkSettingJson.Insert(rightBraceIndex, fpUserCenterInfo);   

			            // funplusSDKInit
			            Funplus.FunplusSdk.Instance.FunplusSDKInit(sdkSettingJson,
				            (1000 * NetServerTime.Instance.ServerTimestamp).ToString());
		            }

	            }


	            if (htManifest.ContainsKey("prod"))
	            {
		            string product_platform = htManifest["prod"] as string;
		            string[] array = product_platform.Split('_');
		            if (array != null && array.Length > 0)
		            {
			            PaymentManager.Instance.Project = array[0];
		            }
		            else
		            {
			            PaymentManager.Instance.Project = string.Empty;
		            }
	            }
	            
				if (htManifest.ContainsKey("micro_community_url") && htManifest["micro_community_url"] != null)
				{
					SdkEntranceManager.MicroCommunity.VerticalViewUrl = htManifest["micro_community_url"] as string;
				}

	            if (DB.DatabaseTools.CheckHashtableKey(htManifest, "cms"))
	            {
		            Hashtable ht = htManifest["cms"] as Hashtable;
		            if (ht != null)
		            {
			            CMSManager.Instance.Key = ht["key"].ToString();
			            CMSManager.Instance.Params = ht["params"] as ArrayList;
		            }
		            else
		            {
			            CMSManager.Instance.Key = string.Empty;
			            CMSManager.Instance.Params = new ArrayList();
		            }

		            var keyParameters = Utils.Json2Object(CMSManager.Instance.Key) as Hashtable;
		            if (keyParameters != null)
		            {
			            string cmsBaseUrl = string.Empty;

			            DatabaseTools.UpdateData(keyParameters, "cmsBaseUrl", ref cmsBaseUrl);

			            NetApi.Instance.BaseCMSUrl = cmsBaseUrl;
		            }
	            }

	            if (htManifest.ContainsKey("kg_gift_mall_server_url") &&
	                htManifest["kg_gift_mall_server_url"] != null)
	            {
		            CMSGiftStoreManager.Instance.BaseUrl = htManifest["kg_gift_mall_server_url"].ToString();
	            }
            }
            if (NetApi.Instance.Manifest != null)
            {
	            bool isOpen = false;
	            DatabaseTools.UpdateData(NetApi.Instance.Manifest, "xperf", ref isOpen);
	            XPerfManager.Instance.IsOpen = isOpen;
            }
            if (NetApi.Instance.Manifest != null)
            {
	            Hashtable sdkData = null;
	            DatabaseTools.UpdateData(NetApi.Instance.Manifest, "sdk", ref sdkData);
	            if (sdkData != null)
	            {
		            string tag = "",url = "",key = "";
		            DatabaseTools.UpdateData(sdkData, "bi_app_tag", ref tag);
		            DatabaseTools.UpdateData(sdkData, "bi_log_server_url", ref url);
		            DatabaseTools.UpdateData(sdkData, "bi_app_key", ref key);
		            XPerfManager.Instance.AppId = tag;
		            XPerfManager.Instance.BaseUrl = url;
		            XPerfManager.Instance.Key = key;
	            }
            }
            if (NetApi.Instance.Encrypted)
            {
	            if (!ParseAesKeyCoder(response))
	            {
		            throw new System.Exception("not support!");
	            }
            }

           
            IsDone = true;
        }
        
        private void SendManifestRequest()
        {
            Hashtable param = new Hashtable();
            
	        param.Add("os", NativeManager.inst.GetOS());
                
            string channel = GameSetting.Current.ServerChannel;
            if (!string.IsNullOrEmpty(channel))
            {
                param.Add("channel",channel);	
            }
            param.Add("client_version", NativeManager.inst.AppMajorVersion);
            param.Add("client_full_version", NativeManager.inst.AppVersion);

            param.Add("client_lang", NexgenDragon.Localization.Instance.CurrentLanguage);
	        
            string language = NativeManager.inst.SysLanguage;
            if (string.IsNullOrEmpty(language)){
                language = "en-US";
            }
            param.Add("sys_lang",language);

            if (CustomDefine.IsCNUnionChannel())
            {
	            string packageName = Funplus.FunplusSdk.Instance.GetSubPackageChannel();
	            param.Add("union_package", packageName);
            }
            else if (GameSetting.Current.Channel == ChannelEnum.AndroidFlexion)
            {
	            string packageName = Funplus.FunplusSdk.Instance.GetSubPackageChannel();
	            param.Add("union_package", packageName);
            }

            string paramJson = Utils.Object2Json(param);
            D.Log($"manifest param:{paramJson}");
            RequestManager.inst.SendLoader(PortType.MANIFEST, param, delegate(bool result, object o)
            {
	            if (!result)
	            {
		            NetServerUpdater.UpdateGroup(NetApi.ServerUrl, true);
		            Utils.RestartGameWithErrorCode(ErrorCode.ErrorRequestManifastFaield);
	            }
            });
        }

        private bool CheckRedirect(Hashtable payload)
        {
	        string redirectUrl = string.Empty;

	        DatabaseTools.UpdateData(payload, "redirect", ref redirectUrl);

	        if (!string.IsNullOrEmpty(redirectUrl) && redirectUrl != NetApi.ServerUrl)
	        {
		        NetApi.RedirectUrl = redirectUrl;
		        NetApi.ServerUrl = redirectUrl;
		        
		        RequestManager.Instance.SetServerUrl(NetApi.ServerUrl);
		        
		        SendManifestRequest();
		        return true;
	        }

	        return false;
        }
        
        private bool CheckVersion()
        {
	        if (Application.isEditor && GameSetting.Current.AssetMode == AssetModeEnum.Default)
	        {
		        return true;
	        }
	        
	        switch (NetApi.inst.State)
	        {
		        case NetApi.AppVersioState.Normal:
		        case NetApi.AppVersioState.Update:
			        return true;
			        break;
		        case NetApi.AppVersioState.NotSupported:
			        UpgradeApp();
			        break;
		        default:
			        string title = I2.Loc.ScriptLocalization.Get("data_error_title");
			        string content =  I2.Loc.ScriptLocalization.Get("data_error_file_parse_description")+"["+ErrorCode.ErrorDownloadGameVersion+"]";
			        if (!NetWorkDetector.Instance.IsReadyRestart())
			        {
				        NetWorkDetector.Instance.SendRestartGameError2RUM(NetWorkDetector.RestartErrorType.Loading,"Manifest", "no state found in app_ver");
			        }
			        NetWorkDetector.Instance.RestartOrQuitGame(content, title);
			        break;
	        }

	        return false;
        }

        private void UpgradeApp()
        {
	        ChooseConfirmationBox.ButtonState buttonState = ChooseConfirmationBox.ButtonState.OK_CENTER;
	        string rightLabel = I2.Loc.ScriptLocalization.Get("update_official_site_button");

	        string warningContent = I2.Loc.ScriptLocalization.Get("update_no_choice_description");
	        string okbt = I2.Loc.ScriptLocalization.Get("update_choice_yes");
	        string title = I2.Loc.ScriptLocalization.Get("update_title");
			
	        UI.UIManager.inst.ShowConfirmationBox(title, warningContent, okbt, rightLabel,
		        buttonState,
		        ConfirmUpdateCallBack, ConfirmUpdateCallBack, ConfirmUpdateCallBack);
        }
        
        private void ConfirmUpdateCallBack()
        {
	        if (!string.IsNullOrEmpty(NetApi.inst.UpdateURL))
	        {
		        Application.OpenURL(NetApi.inst.UpdateURL);
	        }
        }

        private void CheckSDK()
		{
			if (Application.isEditor)
			{
				return;
			}
			if (ABTestManager.Instance.IsTestRunning(ABTestCase.CONFIG_SERVER))
			{
				StringABCondtion condition = ABTestManager.Instance.GetCondition<StringABCondtion>(ABTestCase.CONFIG_SERVER);
				string tmp = condition.ConditionValue();
				if (!string.IsNullOrEmpty(tmp))
				{
	                Funplus.FunplusSdk.Instance.setConfigServerEndpoint(condition.ConditionValue());
				}
			}
	        if (ABTestManager.Instance.IsTestRunning(ABTestCase.PAYMENT_SERVER))
	        {
	            StringABCondtion condition = ABTestManager.Instance.GetCondition<StringABCondtion>(ABTestCase.PAYMENT_SERVER);
	            string tmp = "";
	            if (condition != null)
	            {
	                tmp = condition.ConditionValue();
	                if (!string.IsNullOrEmpty(tmp))
	                {
	                    Funplus.FunplusSdk.Instance.setPaymentServerEndpoint(tmp);
		                PaymentManager.Instance.PaymentUrl = tmp;
	                }
	            }
	        }
	        
	        Funplus.FunplusSdk.Instance.setPassportServerEndpoint(NetServerUpdater.CurGroup.PassportUrl);

	        if (ABTestManager.Instance.IsTestRunning(ABTestCase.IDENSERVER))
	        {
		        StringABCondtion condition = ABTestManager.Instance.GetCondition<StringABCondtion>(ABTestCase.IDENSERVER);
		        string tmp = condition.ConditionValue();
		        if (!string.IsNullOrEmpty(tmp))
		        {
			        UserVerifyManager.Instance.BaseUrl = tmp;
		        }
	        }
		}

        private bool ParseAesKeyCoder(Hashtable resHt)
        {
	        NetApi.Instance.IsRandomKeyAesOn = false;
	    
	        long ai = 0;
	        if (!DatabaseTools.UpdateData(resHt, "ai", ref ai))
	        {
		        return false;
	        }

	        string ak = null;
	        if (!DatabaseTools.UpdateData(resHt, "ak", ref ak))
	        {
		        return false;

	        }
	    
	        NetApi.inst.IsRandomKeyAesOn = ai != 0 && !string.IsNullOrEmpty(ak);

	        if (NetApi.inst.IsRandomKeyAesOn)
	        {
		        AesCoderWithRandomKey.Initialize(ak,ai);
	        }

	        return NetApi.inst.IsRandomKeyAesOn;
        }


        private void DecodeDeleteAccountInfo()
        {
	        if (NetApi.inst.Manifest != null && NetApi.inst.Manifest.ContainsKey("logoff")
	                                         && NetApi.inst.Manifest["logoff"] != null)
	        {
		        Hashtable ht = NetApi.inst.Manifest["logoff"] as Hashtable;
		        if (ht != null)
		        {
			        // AccountManager.Instance.DeleteAccountUrl
			        string deleteAccountUrl = string.Empty;
			        if (DatabaseTools.UpdateData(ht, "url", ref deleteAccountUrl))
			        {
				        AccountManager.Instance.DeleteAccountUrl = deleteAccountUrl;
			        }
			        else
			        {
				        D.Warn("解析删除账号信息失败");
			        }
			        
			        string recoverAccountUrl = string.Empty;
			        if (DatabaseTools.UpdateData(ht, "restore_url", ref recoverAccountUrl))
			        {
				        AccountManager.Instance.RecoverAccountUrl = recoverAccountUrl;
			        }
			        else
			        {
				        D.Warn("解析删除账号信息失败");
			        }
		        }
	        }
        }
    }
}