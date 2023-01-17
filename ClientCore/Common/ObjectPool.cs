//#define STRICT_MODE

using System;
using System.Collections.Generic;

namespace ClientCore
{
    public class ObjectPool<T> where T : new()
    {
        private Queue<T> _allInstance = new Queue<T>();
        
#if STRICT_MODE
        private List<T> _allInUseInstance = new List<T>();
#endif
        public T TakeOut()
        {
            if (_allInstance.Count > 0)
            {
                return _allInstance.Dequeue();
            }

            var instance = new T();
            
#if STRICT_MODE
            _allInUseInstance.Add(instance);
#endif
            return instance;
        }
        
        public void TakeBack(T instance)
        {
#if STRICT_MODE
            var index = _allInUseInstance.IndexOf(instance);
            if (index != -1)
            {
                _allInUseInstance.RemoveAt(index);
                _allInstance.Enqueue(instance);
            }
            else
            {
                throw new Exception("Unknown Instance.");
            }
#else
            _allInstance.Enqueue(instance);
#endif
        }
    }
    
}