using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ClientCore
{
    public class RedPointDebugWindow : EditorWindow
    {
        [MenuItem("Toolset/OtherTools/红点调试窗口")]
        private static void OpenRedPointDebugWindow()
        {
            GetWindow<RedPointDebugWindow>("红点调试窗口");
        }

        private Vector2 _scrollPosition;
        private int _sortFuncIndex = 0;
        private List<KeyValuePair<string, Comparison<RedPointNode>>> _allSortFunc =
            new List<KeyValuePair<string, Comparison<RedPointNode>>>()
            {
                new KeyValuePair<string, Comparison<RedPointNode>>("类型",
                    (a, b) => String.Compare(a.GetType().Name, b.GetType().Name, StringComparison.Ordinal)),
                new KeyValuePair<string, Comparison<RedPointNode>>("耗时", 
                    (a, b) => b.CalcTime.CompareTo(a.CalcTime)),
                
                new KeyValuePair<string, Comparison<RedPointNode>>("计算次数",
                    (a, b) => b.CalcTimes.CompareTo(a.CalcTimes)),
                
                new KeyValuePair<string, Comparison<RedPointNode>>("脏标记",
                    (a, b) => b.Dirty.CompareTo(a.Dirty))
            };

        private string[] _allSortFuncName = null;
        private string[] AllSortFuncName
        {
            get
            {
                if (_allSortFuncName == null)
                {
                    _allSortFuncName = new string[_allSortFunc.Count];
                    for(var i = 0; i < _allSortFunc.Count; i++)
                    {
                        _allSortFuncName[i] = _allSortFunc[i].Key;
                    }
                }

                return _allSortFuncName;
            }
        }

        bool showPosition  = true;
        string status = "Select a GameObject";
        
        Dictionary<long,bool> _showPositionDict = new Dictionary<long,bool>();

        private string _filter = string.Empty;
        
        private void OnGUI()
        {
            GUILayout.BeginVertical();
            _filter = EditorGUILayout.TextField("过滤", _filter, GUILayout.MaxWidth(300));
            
            EditorGUILayout.LabelField("排序方式:");
            _sortFuncIndex = GUILayout.SelectionGrid(_sortFuncIndex, AllSortFuncName, 5);
            
            if (Application.isPlaying && ManagerFacade.RedPointManager != null)
            {
                var redPointManager = ManagerFacade.RedPointManager;
                var allNode = redPointManager.AllNode4Editor;
                
                allNode.Sort(_allSortFunc[_sortFuncIndex].Value);
            
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, false, true);
                
                foreach (var node in allNode)
                {
                    GUILayout.BeginVertical();

                    if (string.IsNullOrEmpty(_filter) || node.GetType().Name.Contains(_filter))
                    {
                        DrawNode(node, 0);
                    }
                    GUILayout.EndVertical();
                }
                
                GUILayout.EndScrollView();
            }
            
            GUILayout.EndVertical();
            
            this.Repaint();
        }
        
        GUIStyle myFoldoutStyle = new GUIStyle(EditorStyles.foldout);
        void DrawNode(RedPointNode node, int recursive = 0)
        {
            GUILayout.BeginHorizontal();

            var nodeId = (node.Id != null ? node.Id.ToString() : "-");
            string space = "";
            for (int i = 0; i < recursive; i++)
            {
                space += "   ";
            }
            
                        
            EditorGUILayout.LabelField($"{space}类型:{node.GetType().Name}", GUILayout.MinWidth(400.0f));
            EditorGUILayout.LabelField($"枚举ID:{nodeId}", GUILayout.MinWidth(10.0f));
            EditorGUILayout.LabelField($"值:{node.Counter}", GUILayout.MinWidth(10.0f));
            EditorGUILayout.LabelField($"脏标记:{node.Dirty}", GUILayout.MinWidth(10.0f));
            EditorGUILayout.LabelField($"计算次数:{node.CalcTimes}", GUILayout.MinWidth(10.0f));
            EditorGUILayout.LabelField($"计算耗时:{node.CalcTime * 1000:f2}ms", GUILayout.MinWidth(10.0f));
            EditorGUILayout.LabelField($"时间戳:{node.ExecuteTimeStamp4Editor * 1000:f2}", GUILayout.MinWidth(10.0f));
            
            
            GUILayout.EndHorizontal();
            
            if (node is RedPointNodeCombination)
            {
                myFoldoutStyle.fontStyle = FontStyle.Bold;
//                myFoldoutStyle.fontSize = 10;
                
                RedPointNodeCombination nodeCombination = node as RedPointNodeCombination;
                if (!_showPositionDict.ContainsKey(nodeCombination.UniqueId4Editor))
                {
                    _showPositionDict.Add(nodeCombination.UniqueId4Editor,false);
                }
                int nextRecursive = recursive + 1;
                
//                _showPositionDict[nodeCombination.UniqueId4Editor] = EditorGUILayout.Foldout(_showPositionDict[nodeCombination.UniqueId4Editor], status,myFoldoutStyle);
                status = $"{space}类型:{node.GetType().Name} - child";
                _showPositionDict[nodeCombination.UniqueId4Editor] = EditorGUILayout.Foldout(_showPositionDict[nodeCombination.UniqueId4Editor], status);
                
                if (_showPositionDict[nodeCombination.UniqueId4Editor])
                {
                    for (int i = 0, len = nodeCombination.AllNode4Editor.Length; i < len; i++)
                    {
                        var subNode = nodeCombination.AllNode4Editor[i];
        
                        DrawNode(subNode, nextRecursive);
                   
                    }

                }
                else
                {
                    _showPositionDict[nodeCombination.UniqueId4Editor] = false;
                }
            }

        }

        
    }

}