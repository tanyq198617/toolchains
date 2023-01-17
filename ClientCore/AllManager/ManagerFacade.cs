using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ClientCore.ReloadModeSupport;
using UnityEngine;

namespace ClientCore
{
    public static class ManagerFacade
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void _RuntimeInitializeOnLoadMethod()
        {
            _allManager.Clear();
            _allTicker.Clear();

            _fileDownloadManager = null;
        }

        [ReloadCallClear]
        private static List<IManager> _allManager = new List<IManager>();
        [ReloadCallClear]
        private static List<ITicker> _allTicker = new List<ITicker>();

        [ReloadWithValue(null)]
        private static HttpManager _httpManager = null;
        public static HttpManager HttpManager => _httpManager;
        
        [ReloadWithValue(null)]
        private static FileDownLoadManager _fileDownloadManager = null;
        public static FileDownLoadManager FileDownLoadManager => _fileDownloadManager;
        
        [ReloadWithValue(null)] 
        private static RedPointManager _redPointManager = null;
        public static RedPointManager RedPointManager => _redPointManager;

        public static T GetManager<T>() where T : class, IManager
        {
            foreach (var manager in _allManager)
            {
                if (manager is T)
                {
                    return (T)manager;
                }
            }
            
            return null;
        }
        
        public static T RegisterCustomManager<T>(T manager) where T : IManager
        {
            RegisterManager(manager);
            return manager;
        }
        
        public static void InitializeAllManager()
        {
            // register builtin managers.
            RegisterManager(_fileDownloadManager = new FileDownLoadManager());
            
            RegisterManager(_httpManager = new HttpManager());
            
            RegisterManager(_redPointManager = new RedPointManager());
            
            foreach (var manager in _allManager)
            {
                manager.Initialize();   
            }
        }
        
        private static void RegisterManager(IManager manager)
        {
            _allManager.Add(manager);
            
            var ticker = manager as ITicker;
            if (ticker != null)
            {
                RegisterTicker(ticker);
            }
        }

        private static void RegisterTicker(ITicker ticker)
        {
            _allTicker.Add(ticker);    
        }
        
        public static void DisposeAllManager()
        {
            foreach (var manager in _allManager)
            {
                manager.Dispose();   
            }
            
            _allManager.Clear();
            _allTicker.Clear();
        }

        public static void Tick(float delta)
        {
            foreach (var ticker in _allTicker)
            {
                ticker.Tick(delta);
            }
        }
    }
}