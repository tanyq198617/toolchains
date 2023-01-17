
using System.Collections.Generic;
using UI;

namespace GameLoading.LoadingStep
{
    /**
     * 用户确认信息阶段
     */
    public class WaitUserConfirmationStep : LoadingPipelineStep
    {
        private List<SubStatus> _subStatuses = new List<SubStatus>();
        
        public WaitUserConfirmationStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();

            AddStatus(new UserVerify());

            if (CheckFinish())
            {
                IsDone = true;
                return;
            }
            
            _subStatuses[0].Start();
        }

        public override void OnTick()
        {
            base.OnTick();
            
            if (_subStatuses.Count == 0)
            {
                IsDone = true;
                return;
            }
            
            if (_subStatuses[0].Done())
            {
                _subStatuses.RemoveAt(0);
                
                if (_subStatuses.Count == 0)
                {
                    IsDone = true;
                    return;
                }
                
                _subStatuses[0].Start();
            }
                
            _subStatuses[0].OnTick();
        }

        private void AddStatus(SubStatus status)
        {
            if (!status.CanSkip())
                _subStatuses.Add(status);
        }

        private bool CheckFinish()
        {
            if (_subStatuses.Count == 0)
            {
                return true;
            }

            return false;
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }


        #region 子状态
        
        public abstract class SubStatus
        {
            protected bool _done = false;
            public abstract bool CanSkip();
            public abstract void Start();
            public abstract void OnTick();
            public bool Done()
            {
                return _done;
            }
        }

        public class UserVerify : SubStatus
        {
            public override bool CanSkip()
            {
                bool canSkip = !UserVerifyManager.inst.CanOpenUserVerify || !UserVerifyManager.inst.IsOpen;
                
                return canSkip;
            }

            public override void Start()
            {
                UserVerifyManager.Instance.Initialize();
            }
            
            public override void OnTick()
            {
                if (!UserVerifyManager.Instance.IsVerify && !UserVerifyManager.Instance.IsOpenedVerify)
                {
                    D.Log("启动实名认证");
                    UserVerifyManager.Instance.CheckUserVerify();
                    return;
                }
            
                if (!_done && UserVerifyManager.inst.IsVerify)
                {
                    _done = true;
                }
            }
        }

        #endregion
        
    }
}