using System;
using System.Collections;
using ClientCore;
using ReflexCLI.Profiling;

namespace GameLoading
{
    public class RtmConnectManager : IManager, ITicker
    {
        private bool _checkConnectionStatus = false;
        private bool _connected = false;

        private DateTime _startTime;

        public bool Connected
        {
            get { return _connected; }
        }

        public void Initialize()
        {
            MessageHub.inst.GetPortByAction(PortType.PLAYER_LOGIN_ACK).AddEvent(OnSendLoginAckResponse);
        }

        public void Dispose()
        {
            MessageHub.inst.GetPortByAction(PortType.PLAYER_LOGIN_ACK).RemoveEvent(OnSendLoginAckResponse);
        }

        public void StartConnectRtm()
        {
            _checkConnectionStatus = true;

            NetWorkDetector.Instance.StopMonitor();

            GameEngine.Instance.ChatManager.Init();

            Connect();
        }

        public void Tick(float deltaTime)
        {
            //using (new ScopedProfiler("RTM.Tick"))
            {
                // 检查首次连接状态发送ack给后端，后面就不检查了
                if (!_checkConnectionStatus)
                {
                    return;
                }

                if (NetWorkDetector.Instance.ConnectionInited)
                {
                    if (NetWorkDetector.Instance.IsConnectionAvailable)
                    {
                        NetWorkDetector.Instance.StartMonitor();

                        _connected = true;

                        _checkConnectionStatus = false;

                        SendLoginAck();
                    }
                    else
                    {
                        if (NetApi.inst.RTM_Status == NetApi.RTMSTATE.AuthFail)
                        {
                            _checkConnectionStatus = false;

                            string title = I2.Loc.ScriptLocalization.Get("data_error_title");
                            string content = I2.Loc.ScriptLocalization.Get("data_error_initialization3_description") +
                                             "[" + ErrorCode.ErrorRtmAuthorFailed + "]";
                            if (!NetWorkDetector.Instance.IsReadyRestart())
                            {
                                NetWorkDetector.Instance.SendRestartGameError2RUM(NetWorkDetector.RestartErrorType.RTM,
                                    "AuthFail", "token=" + NetApi.inst.RtmToken);
                            }

                            NetWorkDetector.Instance.RestartOrQuitGame(content, title);
                        }
                    }
                }
                else
                {
                    var delta = DateTime.Now - _startTime;
                    if (delta.TotalSeconds > 10)
                    {
                        Reconnect();
                    }
                }
            }
        }

        private void Connect()
        {
            _startTime = DateTime.Now;
            GameEngine.Instance.ChatManager.Connect();
        }

        private void Reconnect()
        {
            _startTime = DateTime.Now;
            GameEngine.Instance.ChatManager.ReConnect();
        }

        private void SendLoginAck()
        {
            var htRequest = new Hashtable();
            RequestManager.Instance.SendLoader(PortType.PLAYER_LOGIN_ACK, htRequest);
        }

        private void OnSendLoginAckResponse(object orgData)
        {
            D.Log("Login Ack Rsp");

            //check data update
            if (orgData is Hashtable data)
            {
                Hashtable htAllianceTech = null;
                DB.DatabaseTools.UpdateData(data, "alliance_tech", ref htAllianceTech);
                PlayerData.inst.allianceTechManager.Update(htAllianceTech);
            }
        }
    }
}