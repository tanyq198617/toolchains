using ClientCore;
using ClientCore.Pipeline;
using GBlue;

namespace GameLoading.LoadingStep
{
    public abstract class LoadingPipelineStep : PipelineStep
    {
        private int _step = 0;
        private float _startTime;

        private string _descriptionKey;
        private string _descriptionLocalization;
        protected string DescriptionLocalization
        {
            get
            {
                if (string.IsNullOrEmpty(_descriptionLocalization))
                {
                    _descriptionLocalization = Utils.XLAT(_descriptionKey);
                }

                return _descriptionLocalization;
            }
        }
        
        public virtual string Description
        {
            get { return DescriptionLocalization; }
        }
        
        public LoadingPipelineStep(int step, string descriptionKey)
        {
            _step = step;
            _descriptionKey = descriptionKey;
        }

        public override void OnStart()
        {
            D.Log($"Loading {GetType()}.OnStart");
            
            _startTime = UnityEngine.Time.realtimeSinceStartup;
        }

        public override void OnEnd()
        {
            var costTime = (int) (1000 * (UnityEngine.Time.realtimeSinceStartup - _startTime));
            SendEndEventToBi(costTime);
        }
        
        public class BIStep
        {
            public string key = "";
            public string value;
        }
        
        public class BIDataFormat
        {
            public BIStep d_c1;
            public BIStep d_c2;
            public BIStep d_c3;
        }
        
        private void SendEndEventToBi(int costTimeMs)
        {
            BIStep bits = new BIStep();
            bits.value = _step.ToString();
            bits.key = "loading_step";
            
            BIStep info = new BIStep();
            info.value = GetType().Name;
            info.key = "loading_step";
	
            BIStep time = new BIStep();
            time.value = costTimeMs.ToString();
            time.key = "elapse_time";
            
            BIDataFormat bif = new BIDataFormat();
            bif.d_c1 = bits;
            bif.d_c2 = info;
            bif.d_c3 = time;
	        
            string bifjson = Utils.Object2Json(bif);
	        
            NetWorkDetector.Instance.PushBI("loading", bifjson);
            
            D.Log("LoadingPipelineStep: {0} Phase: {1} ElapseTime: {2}" , bits.value, info.value, time.value);
        }
    }
}