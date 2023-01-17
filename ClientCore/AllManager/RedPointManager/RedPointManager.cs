using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Profiling;

namespace ClientCore
{
    public class RedPointManager : IManager, ITicker
    {
        private float _maxCalcTimePerframe = 0.005f;
        
        private List<RedPointNode> _allNode = new List<RedPointNode>();
        public List<RedPointNode> AllNode => _allNode;
        
        private int _currentIndex = 0;
        
        public void Initialize()
        {
            
        }

        public void Dispose()
        {
            foreach (var node in _allNode)
            {
                node.Dispose();
            }
            _allNode.Clear();
        }
        
        public RedPointNode AddNode(RedPointNode node)
        {
            _allNode.Add(node);

            node.Initialize();
            node.MarkDirty();
            
            return node;
        }

        public void RemoveNode(RedPointNode node)
        {
            node.Dispose();
            _allNode.Remove(node);
        }

        public void RemoveNode<T>() where T:RedPointNode
        {
            var node = GetNode<T>();
            if (node != null)
            {
                RemoveNode(node);
            }
        }

        public void RemoveNode(Enum id)
        {
            var node = GetNode(id);
            if (node != null)
            {
                RemoveNode(node);
            }
        }
        
        public RedPointNode FindNode(Predicate<RedPointNode> match) 
        {
            return _allNode.Find(match);
        }
        
        public RedPointNode GetNode<T>() where T:RedPointNode
        {
            return _allNode.Find(p => p is T);
        }
        
        public RedPointNode GetNode(Enum id)
        {
            return _allNode.Find(p => p.Id != null && p.Id.Equals(id));
        }
        
        public void Tick(float delta)
        {
            Profiler.BeginSample("RedPointManager.Tick");
            
            var nodeCount = _allNode.Count;

            if (_currentIndex > nodeCount - 1)
            {
                _currentIndex = 0;
            }

            var calcTime = 0.0f;
            for (int i = 0; i < nodeCount; i++)
            {
                _currentIndex = (_currentIndex + 1) % nodeCount;
                calcTime += _allNode[_currentIndex].TryRecalculate();

                if (calcTime > _maxCalcTimePerframe)
                {
                    //Debug.LogFormat("[RedPointManager] {0}ms have been used ({1}/{2}), run other on next frame.",
                        //calcTime, _currentIndex, nodeCount);
                    break;
                }
            }
            
            Profiler.EndSample();
        }

#if UNITY_EDITOR
        private List<RedPointNode> _allNode4Editor = new List<RedPointNode>();
        public List<RedPointNode> AllNode4Editor
        {
            get
            {
                _allNode4Editor.Clear();
                _allNode4Editor.AddRange(_allNode);
                return _allNode4Editor;
            }
        }
#endif
        
    }
}