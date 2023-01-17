using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ClientCore.Pipeline
{
    /** 
     * 串行流水线
     */
    public class Pipeline
    {
        private bool _run = false;
        
        private Action<PipelineStep, PipelineStep> _stepChangeCallback = null;
        private Action<bool> _finishCallback = null;
        private PipelineStep[] _allStep = null;
        
        private Queue<PipelineStep> _allWaitStep = new Queue<PipelineStep>();
        private PipelineStep _current = null;
        
        private float _currentStartTime = 0;
        private int _currentStartFrameCount;
        
        private PipelineStep Current
        {
            get { return _current; }
            set
            {
                var previous = _current;
                if (value == _current)
                {
                    return;
                }
                
                if(_current != null)
                {
                    _current.OnEnd();
                    
                    _current.CostTime = Time.realtimeSinceStartup - _currentStartTime;
                    _current.CostFrame = Time.frameCount - _currentStartFrameCount;
                }
                
                _current = value;
                
                if (_run)
                {
                    InvokeStepChangeCallback(previous, _current);
                }
                
                if (_current != null)
                {
                    _currentStartTime = Time.realtimeSinceStartup;
                    _currentStartFrameCount = Time.frameCount;
                    
                    _current.OnStart();
                }
            }
        }
        
        public PipelineStep GetCurrentPipelineStep()
        {
            return Current;
        }

        public T GetPipelineStep<T>() where T : PipelineStep
        {
            for (int i = 0; i < _allStep.Length; i++)
            {
                if (_allStep[i] is T)
                {
                    return _allStep[i] as T;
                }
            }
            
            return null;
        }
        
        public float GetProgress()
        {
            var progress = 0.0f;
            var progressPerStep = 1.0f / _allStep.Length;

            for (int i = 0; i < _allStep.Length; i++)
            {
                if (Current == _allStep[i])
                {
                    progress += Mathf.Clamp(Current.Progress, 0, 1) * progressPerStep;
                    break;
                }
                else
                {
                    progress += progressPerStep;
                }
            }

            return progress;
        }

        public void SetFinishCallback(Action<bool> finishCallback)
        {
            _finishCallback = finishCallback;
        }

        public void SetStepChangeCallback(Action<PipelineStep, PipelineStep> stepChangeCallback)
        {
            _stepChangeCallback = stepChangeCallback;
        }
        
        public Pipeline(params PipelineStep[] allStep)
        {
            _allStep = allStep;
            for (int i = 0; i < _allStep.Length; i++)
            {
                _allWaitStep.Enqueue(_allStep[i]);
            }
        }
        
        public void Start()
        {
            _run = true;
        }

        public void StartFrom(PipelineStep startStep)
        {
            if (startStep != null)
            {
                while (_allWaitStep.Count > 0 && startStep != _allWaitStep.Peek())
                {
                    _allWaitStep.Dequeue();
                }    
            }
            
            Start();
        }
        
        public void Cancel()
        {
            if (_run)
            {
                _run = false;

                if (Current != null)
                {
                    Current = null;
                }
            }
        }
        
        public void Tick()
        {
            if (_run)
            {
                if (!TryRunNextStep())
                {
                    InvokeFinishCallback(true);
                    _run = false;
                }

                if (Current != null && !Current.IsDone)
                {
                    Current.OnTick();
                }
            }
        }

        public bool IsDone
        {
            get
            {
                return _allWaitStep.Count <= 0;
            }
        }
        
        private bool TryRunNextStep()
        {
            if (Current == null || Current.IsDone)
            {
                if (_allWaitStep.Count > 0)
                {
                    Current = _allWaitStep.Dequeue();
                }
                else
                {
                    Current = null;
                }
                
                if (Current == null)
                {
                    return false;
                }
            }

            if (Current != null && Current.IsDone)
            {
                return TryRunNextStep();
            }
            
            return true;
        }

        private void InvokeFinishCallback(bool result)
        {
            if (_finishCallback != null)
            {
                _finishCallback.Invoke(result);
            }
        }

        private void InvokeStepChangeCallback(PipelineStep previous, PipelineStep current)
        {
            if (_stepChangeCallback != null)
            {
                _stepChangeCallback.Invoke(previous, current);
            }
        }

        public string DumpTimeInfo()
        {
            var builder = new StringBuilder();

            foreach (var step in _allStep)
            {
                builder.AppendLine($"{step.Name}, {step.IsDone}, {step.CostFrame}, {step.CostTime}");
            }

            return builder.ToString();
        }
    }
}