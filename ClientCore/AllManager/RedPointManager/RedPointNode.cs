using System;
using UnityEngine;
using UnityEngine.Profiling;

namespace ClientCore
{
    public abstract class RedPointNode
    {
        private bool _dirty = false;
        public bool Dirty => _dirty;

        private int _counter = 0;
        public int Counter => _counter;
        
        private float _calcTime = 0;
        public float CalcTime => _calcTime;

        private int _calcTimes = 0;
        public int CalcTimes => _calcTimes;

        private Enum _id;

        public Enum Id
        {
            get => _id;
            set => _id = value;
        }

        public Action<RedPointNode> OnCounterChanged;
        
        protected RedPointNode(Enum id = null)
        {    
            _id = id;
        }

        public void MarkDirty()
        {
            _dirty = true;
        }
        
        public float TryRecalculate()
        {
            if (_dirty)
            {
                
#if UNITY_EDITOR
                using (new CostTimePrinter(CachedProfilerName, 15))
                {
                    Profiler.BeginSample(CachedProfilerName);
#endif
                    _dirty = false;

                    _calcTimes++;
                
                    float startTime = Time.realtimeSinceStartup;
                
                    _counter = Calculate();

                    OnCounterChanged?.Invoke(this);
                
                    _calcTime = Time.realtimeSinceStartup - startTime;

#if UNITY_EDITOR
                    _executeTimeStamp4Editor = Time.realtimeSinceStartupAsDouble;
                    Profiler.EndSample();
#endif
                    return _calcTime;

#if UNITY_EDITOR
                }
#endif
            }

            return 0;
        }

        public abstract void Initialize();
        public abstract void Dispose();
        public abstract int Calculate();

#if UNITY_EDITOR
        private string _cachedProfilerName = null;
        private string CachedProfilerName
        {
            get
            {
                if (_cachedProfilerName == null)
                {
                    _cachedProfilerName = $"{this.GetType().Name}.Calculate";
                }

                return _cachedProfilerName;
            }
        }
        
        private double _executeTimeStamp4Editor;
        public double ExecuteTimeStamp4Editor
        {
            get { return _executeTimeStamp4Editor; }
        }
#endif

    }
}