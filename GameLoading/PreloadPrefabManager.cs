using System;
using System.Collections.Generic;
using ClientCore;
using ReflexCLI.Profiling;
using UI;
using UniRx;
using UnityEngine;

namespace GameLoading
{
    public class PreloadPrefabManager : IManager, ITicker
    {
        private string[] _allPrefabName = new[]
        {
            CitadelSystem.BG_PREFAB_PATH,
            CitadelSystem.CITY_MAP_PREFAB_PATH,
            UIManager.PUBLIC_HUD_PATH,
            UIManager.TIMER_HUD_PATH,
            UIManager.RELIC_BATTLE_HUD_PATH,
        };
            
        private Dictionary<string, GameObject> _allPreloadPrefab = new Dictionary<string, GameObject>();
        
        private CompositeDisposable _allLoadRequestDisposable = new CompositeDisposable();

        private List<KeyValuePair<string, Action<GameObject>>> _allWaitCallback =
            new List<KeyValuePair<string, Action<GameObject>>>();
        
        public void Initialize()
        {
            
        }
        
        public void StartLoadAsync()
        {
            for(int i = 0; i < _allPrefabName.Length; i++)
            {
                var prefabName = _allPrefabName[i];
                AssetManager.Instance.LoadAsObservable(prefabName).Subscribe(v =>
                {
                    var prefab = v as GameObject;
                    _allPreloadPrefab.Add(prefabName, prefab);
                }).AddTo(_allLoadRequestDisposable);
            }
        }

        public bool TryGetPreloadPrefab(string prefabName, out GameObject prefab)
        {
            return _allPreloadPrefab.TryGetValue(prefabName, out prefab);
        }

        public void WaitPrefabAsync(string prefabName, Action<GameObject> callback)
        {
            GameObject prefab = null;
            if(_allPreloadPrefab.TryGetValue(prefabName, out prefab))
            {
                callback.Invoke(prefab);
            }
            else
            {
                _allWaitCallback.Add(new KeyValuePair<string, Action<GameObject>>(prefabName, callback));
            }
        }
        
        public void Tick(float delta)
        {
            //using (new ScopedProfiler("PreloadPrefabMgr.Tick"))
            {
                for (int i = _allWaitCallback.Count - 1; i >= 0; i--)
                {
                    var prefabName = _allWaitCallback[i].Key;
                    var callback = _allWaitCallback[i].Value;
                
                    GameObject prefab = null;
                    if (_allPreloadPrefab.TryGetValue(prefabName, out prefab))
                    {
                        callback.Invoke(prefab);
                        _allWaitCallback.RemoveAt(i);
                    }
                }
            }
        }
        
        public void Dispose()
        {
            _allPreloadPrefab.Clear();
            _allPreloadPrefab.Clear();
            
            _allWaitCallback.Clear();
        }

       
    }
}