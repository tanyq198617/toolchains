using System;
using System.Collections.Generic;
using MoreLinq;
using MoreLinq.Extensions;
using UnityEngine;

namespace ClientCore
{
    /**
     * 异步请求容器
     */
    public class RequestContainer<T> : ITicker where T : class, IAsyncRequest
    {
        private int _limitCount = 5;
        
        private LinkedList<T> _allWaitingRequest = new LinkedList<T>();
        private List<T> _allRunningRequest = new List<T>();
        
        public LinkedList<T> AllWaitingRequest
        {
            get { return _allWaitingRequest; }
        }

        public List<T> AllRunningRequest
        {
            get { return _allRunningRequest; }
        }

        public int LimitCount
        {
            get { return _limitCount; }
        }
        
        private bool _isNewRequestPriorityIsHigh = true;
        
        public RequestContainer(int limitCount, bool isNewRequestPriorityIsHigh = true)
        {
            _limitCount = limitCount;
            _isNewRequestPriorityIsHigh = isNewRequestPriorityIsHigh;
        }
        
        public void SetRunningLimitCount(int limitCount)
        {
            _limitCount = limitCount;
        }
       
        public void AddRequest(T request)
        {
            var node = _allWaitingRequest.First;
            while (node != null)
            {
                if (request.Priority < node.Value.Priority)
                {
                    _allWaitingRequest.AddBefore(node, request);
                    return;
                }

                if (_isNewRequestPriorityIsHigh && request.Priority == node.Value.Priority)
                {
                    _allWaitingRequest.AddBefore(node, request);
                    return;
                }
                node = node.Next;
            }

            _allWaitingRequest.AddLast(request);
        }

        public void UpdateRequestPriority(T request, int priority)
        {
            var node = _allWaitingRequest.Find(request);
            if (node != null)
            {
                _allWaitingRequest.Remove(node);

                request.Priority = priority;
                AddRequest(request);
            }
        }

        public T TryGetRuningRequest(Predicate<T> match)
        {
            return _allRunningRequest.Find(match);
        }

        public T TryGetWaitingRequest(Predicate<T> match)
        {
            using (var enumerator = _allWaitingRequest.GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    if (match(enumerator.Current))
                    {
                        return enumerator.Current;
                    }
                }    
            }
            
            return null;
        }
        
        public virtual void Tick(float delta)
        {
            foreach (var request in _allRunningRequest)
            {
                request.Tick(delta);
            }
            
            var runningRequest = default(T);
            for (int i = _allRunningRequest.Count - 1; i >= 0; i--)
            {
                runningRequest = _allRunningRequest[i];
                if(runningRequest.IsDone)
                {
                    _allRunningRequest.RemoveAt(i);
                    runningRequest.AfterDone();
                    runningRequest.Dispose();
                }
            }
            
            while (_allRunningRequest.Count < _limitCount && _allWaitingRequest.Count > 0)
            {
                var request = _allWaitingRequest.First.Value;
                _allWaitingRequest.RemoveFirst();
                
                _allRunningRequest.Add(request);

                request.DoRequest();
            }
        }

        public void Dispose()
        {
            foreach (var request in _allWaitingRequest)
            {
                request.Dispose();
            }
            _allWaitingRequest.Clear();
            
            foreach (var request in _allRunningRequest)
            {
                request.Dispose();
            }
            _allRunningRequest.Clear();
        }
    }
}