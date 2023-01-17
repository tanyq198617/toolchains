#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ClientCore
{
    public class FileDownloadDebugWindow : EditorWindow
    {
        [MenuItem("Toolset/OtherTools/文件下载调试窗口")]
        private static void OpenFileDownloadDebugWindow()
        {
            GetWindow<FileDownloadDebugWindow>("文件下载调试窗口");
        }
        
        private RequestContainer<FileDownloadRequest> _requestContainer = null;
        
        public void SetRequestContainer(RequestContainer<FileDownloadRequest> requestContainer)
        {
            _requestContainer = requestContainer;
        }

        private string _traceKeyWord = string.Empty;

        private GUIStyle _traceKeywordStyle = null;
        private GUIStyle TraceKeywordStyle
        {
            get
            {
                if (_traceKeyWord == null)
                {
                    _traceKeywordStyle = new GUIStyle(EditorStyles.label);
                    _traceKeywordStyle.normal.textColor = Color.red;
                    _traceKeywordStyle.richText = true;
                }

                return _traceKeywordStyle;
            }
        }

        private string UrlToFileName(string url)
        {
            return url.Substring(url.LastIndexOf("/"));
        }

        private FileDownloadPriority PriorityToEnum(int priority)
        {
            return (FileDownloadPriority)(priority);
        }
        
        private void OnGUI()
        {
            GUILayout.BeginVertical();
            
            _traceKeyWord = EditorGUILayout.TextField("追踪关键字", _traceKeyWord, GUILayout.Width(400));

            var keyWord = WWW.EscapeURL(_traceKeyWord);
            
            var fileDownloadManager = ManagerFacade.FileDownLoadManager;
            if (!Application.isPlaying || fileDownloadManager == null)
            {
                GUILayout.Label("进入游戏模式后开始展示调试信息");
                return;
            }

            var requestContainer = fileDownloadManager.RequestContainer;
            
            GUILayout.BeginHorizontal();

            // 已下载
            
            // 下载中
            GUILayout.BeginVertical();   
            GUILayout.Label($"下载中:{requestContainer.AllRunningRequest.Count}/{requestContainer.LimitCount}");
            var allRunningRequest = requestContainer.AllRunningRequest;
            foreach (var request in allRunningRequest)
            {
                if (!string.IsNullOrEmpty(keyWord) && request.RemoteUrl.Contains(keyWord))
                {
                    DrawLabel($"<color=red>{PriorityToEnum(request.Priority)}\t{UrlToFileName(request.RemoteUrl)}\t{request.DownloadProgress}</color>", TraceKeywordStyle);
                }
                else
                {
                    DrawLabel($"{PriorityToEnum(request.Priority)}\t{UrlToFileName(request.RemoteUrl)}\t{request.DownloadProgress}");
                }
                
            }
            GUILayout.EndVertical();
            
            // 等待下载
            GUILayout.BeginVertical();
            GUILayout.Label($"排队中:{requestContainer.AllWaitingRequest.Count}");
            var allWaitingRequest = requestContainer.AllWaitingRequest;
            foreach (var request in allWaitingRequest)
            {
                if (!string.IsNullOrEmpty(keyWord) && request.RemoteUrl.Contains(keyWord))
                {
                    DrawLabel($"<color=red>{PriorityToEnum(request.Priority)}\t{UrlToFileName(request.RemoteUrl)}</color>", TraceKeywordStyle);
                }
                else
                {
                    DrawLabel($"{PriorityToEnum(request.Priority)}\t{UrlToFileName(request.RemoteUrl)}");
                }
            }
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            this.Repaint();
        }
        
        private void DrawLabel(string label, GUIStyle style)
        {
            GUILayout.Label(label, style);
        }

        private void DrawLabel(string label)
        {
            GUILayout.Label(label);
        }
    }
    
}

#endif