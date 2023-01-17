using System.Collections;
using ClientCore;
using ClientCore.Pipeline;
using com.kingsgroup.sdk;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    /**
     * 加载funplus账号信息
     */
    public class LoadAccountStep : LoadingPipelineStep
    {
        public enum LoadingStatus
        {
            UnloadOrFailed = 0,
            LoadSuccess,
            AlreadDeleted,
        }
        
        public LoadAccountStep(int step, string descriptionKey):base(step, descriptionKey)
        {
        }
        
        public override void OnStart()
        {
            base.OnStart();
            
            if (Application.isEditor)
            {
                IsDone = true;
                return;
            }
            
            FirebaseManager.Instance.Initialize((bool result)=>
            {
                if (result)
                {
                    Utils.StartCoroutine(FirebaseManager.Instance.TryGetAppInstanceId());
                }
            });
            
            AccountManager.Instance.Initialize(OnLoadAccountCallback);
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }
        
        private void OnLoadAccountCallback(LoadingStatus status)
        {
            D.Log($"OnLoadAccountCallback isLogin : {status.ToString()}");
            
            if (status == LoadingStatus.LoadSuccess)
            {
                D.Log("Funplus ID="+AccountManager.Instance.FunplusID);
                D.Log("Session Key="+AccountManager.Instance.SessionKey);

                CrashReportCustomField.SetValue(CrashReportCustomField.Key.Fpid, AccountManager.Instance.FunplusID);
                
                PaymentManager.Instance.Initialize();
			
                SdkEntranceManager.HelpShift.SetMetaData(HelpShiftEntrance.MetaDataKey.FunplusId,
                    AccountManager.Instance.FunplusID);

                SdkEntranceManager.AppsFlyer.SetUserFpid(AccountManager.Instance.FunplusID);
                SdkEntranceManager.AppsFlyer.SendLoginEvent();
                
                SdkEntranceManager.UnityAds.TraceSessionEvent();
                
                NativeManager.inst.GetGaid();
                
                ReportATTStatusBI();
                
                IsDone = true;
            }
            else if (status == LoadingStatus.UnloadOrFailed)
            {
                AccountManager.Instance.Login();
            }
            else if (status == LoadingStatus.AlreadDeleted)
            {
                IsDone = true;
            }
        }
        
        private void ReportATTStatusBI()
        {
            if (KGPrivacy.Instance().availableiOS145())
            {
                if (KGPrivacy.Instance().isFirstRequestATTPermission())
                {
	                // 授权之前的状态
	                KGPermissionStatus statusBefore = KGPrivacy.Instance().QueryBeforeRequestPermissionStatus(KGPermissionType.IDFA_IOS);
	                string statusBeforeString = getAttStstus(statusBefore);
	                Hashtable biBeforeEventProperties = new Hashtable();
	                biBeforeEventProperties.Add("d_c1", buildBIValue("datafrom","sdk"));
	                biBeforeEventProperties.Add("d_c2", buildBIValue("position", "before_att"));
	                biBeforeEventProperties.Add("d_c3", buildBIValue("action","request"));
	                biBeforeEventProperties.Add("d_c4", buildBIValue("status",statusBeforeString));
	                string JsonBeforeStr = Utils.Object2Json(biBeforeEventProperties);
	                if (Funplus.FunplusSdk.Instance.IsSdkInstalled())
	                {
		                Debug.LogWarning("TraceUserAuthorizeResultBefore result: " + statusBeforeString + " JsonStr: " + JsonBeforeStr);
		                Funplus.FunplusBi.Instance.TraceEvent("track_auth", JsonBeforeStr);
	                }
	                
                    // 授权结果
                    KGPermissionStatus status = KGPrivacy.Instance().QueryPermissionStatus(KGPermissionType.IDFA_IOS);
                    string statusString = getAttStstus(status);
                    Hashtable biEventProperties = new Hashtable();
                    biEventProperties.Add("d_c1", buildBIValue("datafrom","sdk"));
                    biEventProperties.Add("d_c2", buildBIValue("position", "after_att"));
                    biEventProperties.Add("d_c3", buildBIValue("action","request"));
                    biEventProperties.Add("d_c4", buildBIValue("status",statusString));
                    string JsonStr = Utils.Object2Json(biEventProperties);
                    if (Funplus.FunplusSdk.Instance.IsSdkInstalled())
                    {
                        Debug.LogWarning("TraceUserAuthorizeResult result: " + statusString + " JsonStr: " + JsonStr);
                        Funplus.FunplusBi.Instance.TraceEvent("track_auth", JsonStr);
                    }
                }
            }
        }
        
        private string getAttStstus(KGPermissionStatus status)
        {
            string statusString = "Unknown";
            switch (status)
            {
                case KGPermissionStatus.NotDetermined:
                    statusString = "NotDetermined" ;
                    break;
                case KGPermissionStatus.Authorized:
                    statusString = "Authorized";
                    break;
                case KGPermissionStatus.Denied:
                    statusString = "Denied";
                    break;
                case KGPermissionStatus.Restricted:
                    statusString = "Restricted";
                    break;
                case KGPermissionStatus.UnderIOS14:
                    statusString = "UnderIOS14";
                    break;
                default:
                    statusString = "Unknown";
                    break;
            }

            return statusString;
        }
        
        private Hashtable buildBIValue(string key, object value)
        {
            Hashtable hashtable = new Hashtable();
            hashtable.Add("key", key);
            hashtable.Add("value", value);
            return hashtable;
        }
    }
}