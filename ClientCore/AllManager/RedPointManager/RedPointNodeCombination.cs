using System;
using ClientCore.ReloadModeSupport;

namespace ClientCore
{
    public class RedPointNodeCombination : RedPointNode
    {
        protected RedPointNode[] _allNode;
        
        public RedPointNodeCombination(Enum id, params RedPointNode[] allNode):base(id)
        {
            _allNode = allNode;

#if UNITY_EDITOR
            SetUniqueId4Editor();
#endif
        }


        public override void Initialize()
        {
            foreach (var node in _allNode)
            {
                node.OnCounterChanged += OnSubNodeCounterChanged;
            }
        }

        public override void Dispose()
        {
            
            foreach (var node in _allNode)
            {
                node.OnCounterChanged -= OnSubNodeCounterChanged;
            }
        }

        public override int Calculate()
        {
            var counter = 0;
            foreach (var node in _allNode)
            {
                counter += node.Counter;
            }

            return counter;
        }
        
        private void OnSubNodeCounterChanged(RedPointNode node)
        {
            MarkDirty();
        }


        
#if UNITY_EDITOR
        [ReloadWithNewInstance(typeof(object))]
        private static readonly object _lock = new object();
        
        [ReloadWithValue(0)]
        private static long atomaticUniqueId = 0;
        
        private static long GetUniqueId4Editor()
        {
            lock (_lock)
            {
                return ++atomaticUniqueId;
            }    
        }

        private long _uniqueId4Editor;

        public RedPointNode[] AllNode4Editor
        {
            get { return _allNode; }
        }

        public long UniqueId4Editor
        {
            get { return _uniqueId4Editor; }
        }

        private void SetUniqueId4Editor()
        {
            _uniqueId4Editor = GetUniqueId4Editor();
        }
#endif
    }
}