using System;
using System.Collections.Generic;
using UnityEngine;

namespace ClientCore.Singleton
{
    public class SingletonResetHelper
    {
        private static List<ISingleton> _allSingleton = new List<ISingleton>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _RuntimeInitializeOnLoadMethod()
        {
            lock (_allSingleton)
            {
                foreach (var singleton in _allSingleton)
                {
                    singleton.DestroyInstance();
                }

                _allSingleton.Clear();
            }
        }
        
        public static void Register(ISingleton singleton)
        {
            lock (_allSingleton)
            {
                _allSingleton.Add(singleton);
            }
        }
        
        public static void ReInstance()
        {
            lock (_allSingleton)
            {
                for (int i = _allSingleton.Count - 1; i >= 0; i--)
                {
                    var singleton = _allSingleton[i];
                    if (singleton.ReInstance)
                    {
                        //Debug.Log($"------------{singleton.GetType().FullName}-------------");
                        singleton.DestroyInstance();
                        _allSingleton.RemoveAt(i);
                    }
                }
            }
        }
    }

    public interface ISingleton
    {
        // 每次重启，是否重新生成实例
        bool ReInstance { get; }
        // 销毁实例
        void DestroyInstance();
    }
    
    public class SingletonReInstance<T> : Singleton<T> where T : class, new()
    {
        public override bool ReInstance => true;
    }
    
    public class Singleton<T> : ISingleton where T : class, new()
    {
        static object _lock = new object();
        
        protected static T _instance;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                            SingletonResetHelper.Register((ISingleton)(_instance));
                        }
                    }
                }

                return _instance;
            }
        }

        [Obsolete("use Instance instead.")]
        public static T inst
        {
            get { return Instance; }
        }
        
        public virtual bool ReInstance => false;

        public void DestroyInstance()
        {
            _instance = null;
        }
    }

    public class MonoSingletonReInstance<T> : MonoSingleton<T> where T : MonoBehaviour
    {
        public override bool ReInstance => true;
    }
    
    public class MonoSingleton<T> : MonoBehaviour, ISingleton where T : MonoBehaviour
    {
        protected static T _instance;
        
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject($"{typeof(T).Name}_singleton");

                    go.AddComponent<DontDestroy>();
                    _instance = go.AddComponent<T>();
                    
                    SingletonResetHelper.Register((ISingleton)(_instance));
                }
                
                return _instance;
            }
        }
        
        [Obsolete("use Instance instead.")]
        public static T inst
        {
            get { return Instance; }
        }
        
        public virtual bool ReInstance => false;

        public void DestroyInstance()
        {
            if (_instance)
            {
                Destroy(_instance.gameObject);
                _instance = null;
            }
        }
    }
}