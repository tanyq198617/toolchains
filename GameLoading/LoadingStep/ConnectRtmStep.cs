using System;
using ClientCore;
using ClientCore.Pipeline;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    /**
     * 和rtm建立链接
     */
    public class ConnectRtmStep : LoadingPipelineStep
    {
        private RtmConnectManager _rtmConnectManager = null;
        
        public ConnectRtmStep(int step, string descriptionKey):base(step, descriptionKey)
        {
        }
        
        public override void OnStart()
        {
            base.OnStart();

            _rtmConnectManager = ManagerFacade.GetManager<RtmConnectManager>();
            
            _rtmConnectManager.StartConnectRtm();

            if (!SwitchManager.Instance.GetSwitch(SwitchConst.LOADING_WAIT_RTM))
            {
                IsDone = true;
            }
        }
        
        public override void OnEnd()
        {
            base.OnEnd();
        }
        
        public override void OnTick()
        {
            if (_rtmConnectManager.Connected)
            {
                IsDone = true;
            }
        }
    }
}