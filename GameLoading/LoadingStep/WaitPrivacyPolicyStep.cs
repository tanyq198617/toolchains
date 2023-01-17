using System.Collections;
using System;
using com.kingsgroup.sdk;
using DB;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLoading.LoadingStep
{
    public class WaitPrivacyPolicyStep : LoadingPipelineStep
    {
        public WaitPrivacyPolicyStep(int step, string descriptionKey):base(step, descriptionKey)
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
            
            if (CustomDefine.IsCNDefine())
            {
                UpdateCNOfflinePrivacy();
                IsDone = true;
            }
            else
            {
                Hashtable ht = new Hashtable();
                var lang = NexgenDragon.Localization.Instance.CurrentLanguage;
                if (string.IsNullOrEmpty(lang))
                {
                    lang = Language.Instance.GetSystemLanguage();
                }
                ht.Add("baseUrl", AccountManager.Instance.RecoverAccountUrl); // sdk 规定使用这个地址
                ht.Add("language", lang);
                ht.Add("gameId", GameSetting.Current.FunplusGameId);
                ht.Add("pkg_channel", GameSetting.Current.SdkChannel);
                
                string configJsonString = Utils.Object2Json(ht);
                D.Log("[OpenGlobalPrivacy] configJsonString:{0}", configJsonString);

                KGPrivacy.Instance().OpenGlobalPrivacy(configJsonString, OpenGlobalPrivacyCb);
            }
        }

        private void OpenGlobalPrivacyCb(string param)
        {
            if (string.IsNullOrEmpty(param))
            {
                D.Log("[OpenGlobalPrivacy] OpenGlobalPrivacyCb param is null");
                IsDone = true;
                return;
            }
            
            D.Log("[OpenGlobalPrivacy] OpenGlobalPrivacyCb param is {0}", param);

            var objParam = Utils.Json2Object(param);
            if (objParam == null)
            {
                D.Log("[OpenGlobalPrivacy] OpenGlobalPrivacyCb objParam is null");
                IsDone = true;
                return;
            }
            
            Hashtable ht = objParam as Hashtable;
            if (ht == null)
            {
                D.Log("[OpenGlobalPrivacy] OpenGlobalPrivacyCb ht is not hashtable");
                IsDone = true;
                return;
            }
            
            long code = 0;
            string type = string.Empty;

            DatabaseTools.UpdateData(ht, "code", ref code);
            DatabaseTools.UpdateData(ht, "type", ref type);
            
            if(code == -1)
            {
                Application.Quit();
                return;
            }

            if (!string.IsNullOrEmpty(type) && type.Equals("action"))
            {
                IsDone = true;
            }
        }

        private void OpenPrivacy()
        {
            KGPrivacy.Instance().OpenCNLocalPrivacy(PrivacyCb);
        }

        private void PrivacyCb(string result)
        {
            result = result == null ? " " : result;
            D.Log($"WaitPrivacyPolicyStep return value:{result}");

            try
            {
                object value = Utils.Json2Object(result);
                Hashtable ht = value as Hashtable;
                if (null == ht || ht.Count == 0)
                {
                    D.Error("隐私条款回调内容异常!");
                    UpdateCNOfflinePrivacy();
                    return;
                }

                long code = 0;
                string type = string.Empty;

                DatabaseTools.UpdateData(ht, "code", ref code);
                DatabaseTools.UpdateData(ht, "type", ref type);

                switch (type)
                {
                    case "bi":
                        // 暂时不处理
                        // HandlePrivacyBIEvent(ht);
                        break;
                    case "action":
                        switch (code)
                        {
                            case -1:
                                D.Error("隐私条款未能通过");
                                Application.Quit(); return;
                            case 0:
                            case 1:
                            case 2:
                                 IsDone = true;
                                 break;
                            default:
                                 IsDone = true;
                                 break;
                         }
                        break;
                    default:
                        break;
                }

                if (IsDone == true)
                {
                    UpdateCNOfflinePrivacy();
                }

            }
            catch (Exception e)
            {
                D.Error($"[PrivacyCb] 解析报错:{e.Message}");
            }
        }

        private void UpdateCNOfflinePrivacy()
        {
            Hashtable param = new Hashtable();
            
            string baseUrl = string.Empty;
            StringABCondtion condition = ABTestManager.Instance.GetCondition<StringABCondtion>(ABTestCase.PRIVACY_BASE_URL);
            if (condition != null)
                baseUrl = condition.ConditionValue();
            
            D.Log($"UpdatePrivacy. baseUrl:{baseUrl}");

            param.Add("baseUrl", baseUrl);
            param.Add("lang", "cn");
            param.Add("gameId", GameSetting.Current.FunplusGameId);
            param.Add("fpid", AccountManager.Instance.AccountId);
            string paramJson = Utils.Object2Json(param);
            KGPrivacy.Instance().UpdateCNPrivacy(paramJson);
        }
        
        public void HandlePrivacyBIEvent(Hashtable dataHt)
        {
            D.Log($"HandlePrivacyBIEvent");
            if (!dataHt.ContainsKey("event"))
            {
                D.Log($"HandlePrivacyBIEvent !ContainsKey event");
                return;
            }
            string eventName = dataHt["event"].ToString();

            Hashtable biEventProperties = new Hashtable();
            biEventProperties.Add("d_c1", this.buildBIValue("datafrom", "sdk"));
            biEventProperties.Add("d_c4", this.buildBIValue("sys_name", "privacy_protocol1.0"));

            if (dataHt.ContainsKey("position"))
            {
                biEventProperties.Add("d_c2", this.buildBIValue("position", dataHt["position"].ToString()));
            }
                    
            if (dataHt.ContainsKey("action"))
            {
                biEventProperties.Add("d_c3", this.buildBIValue("action", dataHt["action"].ToString()));
            }

            if (dataHt.ContainsKey("track_key"))
            {
                biEventProperties.Add("m5", this.buildBIValue("track_key", dataHt["track_key"].ToString()));
            }
            Funplus.FunplusBi.Instance.TraceEvent("privacy_protocol", Utils.Object2Json(biEventProperties));
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