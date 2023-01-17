
using System.Collections.Generic;
using UI;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    /**
     * 处理用户数据状态
     */
    public class ProcessUserData : LoadingPipelineStep
    {
        private List<SubStatus> _subStatuses = new List<SubStatus>();
        
        public ProcessUserData(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();

            AddStatus(new AccountDelete());

            if (CheckFinish())
            {
                IsDone = true;
                D.Log("return ProcessUserData！！！");
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
                    D.Log("ProcessUserData OnTick return 2");
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
            {
                D.Log(" ProcessUserData  add  AccountDelete success ");
                _subStatuses.Add(status);
            }
            else
            {
                D.Log("ProcessUserData add AccountDelete failed!");
            }
        }
        
        private bool CheckFinish()
        {
            if (_subStatuses.Count == 0)
            {
                D.Log(" ProcessUserData  CheckFinish ");

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

        public class AccountDelete : SubStatus
        {
            public override bool CanSkip()
            {
                bool canSkip = !AccountManager.Instance.InDeleteAccountProcess;
                D.Log($"AccountDelete CanSkip : {canSkip.ToString()}");
                return canSkip;
            }

            public override void Start()
            {
                D.Log("start AccountDelete！！！");
                UIManager.inst.OpenPopup(UIManager.PopupType.AccountDeleteLoginPopup);
            }
            
            public override void OnTick()
            {
                
            }
        }

        #endregion
        
    }
}