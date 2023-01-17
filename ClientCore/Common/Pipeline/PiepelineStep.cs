using System;

namespace ClientCore.Pipeline
{
    /**
     * 流水线执行步骤
     */
    public abstract class PipelineStep
    {
        public float CostTime = 0;
        public long CostFrame = 0;

        public virtual string Name
        {
            get { return this.GetType().Name; }
        }
        
        public bool IsDone { get; set; }
        
        public abstract void OnStart();
        public abstract void OnEnd();

        public virtual void OnTick()
        {
            
        }
        
        public virtual float Progress
        {
            get
            {
                return 0.0f;
            }
        }
        
        public virtual void OnCancel()
        {
            
        }
    }
}