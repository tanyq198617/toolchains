using System;
using System.Threading;
using UnityEngine;
using Object = System.Object;

namespace ClientCore
{
    public class WaitForAsyncOperation : CustomYieldInstruction, IDisposable
    {
        private bool _isDone = false;
        
        private object _result;
        private bool _success = false;
        
        public bool Success
        {
            get => _success;
        }

        public Object Result
        {
            get { return _result; }
        }

        private CancellationTokenSource _cancelSource = null;
        
        public delegate Object AsyncFunction(Object param);
        public delegate Object AsyncFunctionCancelable(Object param, CancellationToken cancelToken);

        public WaitForAsyncOperation(AsyncFunction asyncFunc, Object param)
        {
            _isDone = false;
            
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    _result = asyncFunc.Invoke(state);
                    _success = true;
                    _isDone = true;
                }
                catch (ThreadInterruptedException exception)
                {
                    _success = false;
                    _result = null;
                    _isDone = true;
                }
            }, param);
            
        }
        
        public WaitForAsyncOperation(AsyncFunctionCancelable asyncFunc, Object param)
        {
            _isDone = false;
            
            _cancelSource = new CancellationTokenSource();
            
            ThreadPool.QueueUserWorkItem((state) =>
            {
                try
                {
                    _result = asyncFunc.Invoke(state, _cancelSource.Token);
                    _success = true;
                    _isDone = true;
                }
                catch (ThreadInterruptedException exception)
                {
                    _success = false;
                    _result = null;
                    _isDone = true;
                }
            }, param);
            
        }
        
        public override bool keepWaiting
        {
            get { return !_isDone; }
        }

        public void Dispose()
        {
            if (_cancelSource != null)
            {
                _cancelSource.Cancel();
                _cancelSource.Dispose();
                _cancelSource = null;
            }
            
        }
    }
}