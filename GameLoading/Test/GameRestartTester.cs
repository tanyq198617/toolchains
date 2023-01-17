using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace GameLoading.Test
{
    public class GameRestartTester : MonoBehaviour
    {
        public float _time = 0;
        public GameEngine.LoadMode _loadMode;
        
        private float _nextTime = 0;
        
        private float GenerateNextTime()
        {
            _time = Random.Range(0, 15);
            _loadMode = Random.Range(0, 100) > 50 ? GameEngine.LoadMode.Deep : GameEngine.LoadMode.Lite;
                
            return Time.realtimeSinceStartup + _time;
        }
        
        private void Awake()
        {
            DontDestroyOnLoad(this.gameObject);
            
            _nextTime = GenerateNextTime();
        }

        void Update()
        {
            if (Time.realtimeSinceStartup > _nextTime)
            {
                GameEngine.Instance.MarkRestartGame(_loadMode);

                _nextTime = GenerateNextTime();

            }
        }
    }
}