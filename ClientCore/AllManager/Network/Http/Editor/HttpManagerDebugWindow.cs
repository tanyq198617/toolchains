using System;
using System.Collections;
using Pathfinding.Serialization.JsonFx;
using UnityEditor;
using UnityEngine;

namespace ClientCore
{
    public class HttpManagerDebugWindow : EditorWindow
    {
        [MenuItem("Toolset/OtherTools/Http通讯调试窗口")]
        private static void OpenFileDownloadDebugWindow()
        {
            GetWindow<HttpManagerDebugWindow>("Http通讯调试窗口");
        }

        private string _httpUrl = "http://koa-qa.kingsgroup.cn/api//";
        private string _httpRequestJson = "{}";

        private Hashtable HttpRequestHashtable
        {
            get
            {
                try
                {
                    var jReader = new JsonReader(_httpRequestJson, new JsonReaderSettings());
                    return jReader.Deserialize() as Hashtable;
                }
                catch (Exception exception)
                {
                    return null;
                }
            }
        }
        private void OnGUI()
        {
            var httpManager = ManagerFacade.HttpManager;
            if (httpManager == null)
            {
                EditorGUILayout.LabelField("运行游戏,开始调试...");
                return;
            }
            
            GUILayout.BeginVertical();

            
            EditorGUILayout.BeginFoldoutHeaderGroup(true, "测试");
            _httpUrl = EditorGUILayout.TextField("服务器Url", _httpUrl);
            _httpRequestJson = EditorGUILayout.TextField("请求json", _httpRequestJson);
            
            if (GUILayout.Button("初始化"))
            {
                httpManager.SetServerUrl(_httpUrl);
                httpManager.SetEncoder(new HttpJsonEncoder());
                httpManager.SetDecoder(new HttpJsonDecoder());
                //httpManager.SetProcessor(new HttpSeqNumberProcessor(), new HttpDebugInfoProcessor());
            }
            
            if (GUILayout.Button("发送串行消息"))
            {
                httpManager.SendSequential(HttpRequestHashtable);
            }
            
            if (GUILayout.Button("发送并行消息"))
            {
                httpManager.SendParallel(HttpRequestHashtable);
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            
            GUILayout.EndVertical();
        }
    }
}