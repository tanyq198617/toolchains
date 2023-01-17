using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using I2;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Spine;
using Spine.Unity;
using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Object = UnityEngine.Object;

namespace AssetStream
{
    public class AssetSettingWindow : OdinEditorWindow
    {
        [MenuItem("Toolset/资源流/资源配置窗口")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(AssetSettingWindow), false, "资源配置窗口");
        }
        
        public static bool CheckAndSaveReport(string reportFilePath, out string checkResultDesc)
        {
            var assetSettingWindow = (AssetSettingWindow)EditorWindow.GetWindow(typeof(AssetSettingWindow), false, "资源配置窗口");

            var result = assetSettingWindow.CheckAllSetting();

            checkResultDesc = assetSettingWindow.CheckResultDescription;
            
            assetSettingWindow.ExportCheckResultToCsv(reportFilePath);

            return result;
        }
        
        [PropertyOrder(2), LabelText("检查结果"), DictionaryDrawerSettings(KeyLabel = "资源", ValueLabel = "错误原因"), ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        protected Dictionary<Object, string> _textureCheckResult = new Dictionary<Object, string>();
        [PropertyOrder(2), LabelText("检查结果"), DictionaryDrawerSettings(KeyLabel = "资源", ValueLabel = "错误原因"), ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        protected Dictionary<Object, string> _audioCheckResult = new Dictionary<Object, string>();
        [PropertyOrder(2), LabelText("检查结果"), DictionaryDrawerSettings(KeyLabel = "资源", ValueLabel = "错误原因"), ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        protected Dictionary<Object, string> _modelCheckResult = new Dictionary<Object, string>();
        [PropertyOrder(2), LabelText("检查结果"), DictionaryDrawerSettings(KeyLabel = "资源", ValueLabel = "错误原因"), ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        protected Dictionary<Object, string> _videoCheckResult = new Dictionary<Object, string>();
        [PropertyOrder(2), LabelText("检查结果"), DictionaryDrawerSettings(KeyLabel = "资源", ValueLabel = "错误原因"), ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        protected Dictionary<Object, string> _materialCheckResult = new Dictionary<Object, string>();
        [PropertyOrder(2), LabelText("检查结果"), DictionaryDrawerSettings(KeyLabel = "资源", ValueLabel = "错误原因"), ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        protected Dictionary<Object, string> _spineCheckResult = new Dictionary<Object, string>();
        [PropertyOrder(2), LabelText("检查结果"), DictionaryDrawerSettings(KeyLabel = "资源", ValueLabel = "错误原因"), ShowInInspector, Sirenix.OdinInspector.ReadOnly]
        protected Dictionary<Object, string> _otherCheckResult = new Dictionary<Object, string>();

        protected string CheckResultDescription
        {
            get 
            { 
                return $@"
总错误数:{_textureCheckResult.Count+_audioCheckResult.Count+_modelCheckResult.Count+_videoCheckResult.Count+_materialCheckResult.Count}
贴图:{_textureCheckResult.Count} 
音频:{_audioCheckResult.Count} 
模型:{_modelCheckResult.Count} 
视频:{_videoCheckResult.Count} 
材质:{_materialCheckResult.Count}
Spine:{_spineCheckResult.Count}
其他:{_otherCheckResult.Count}";
            }
        }
        
        [PropertyOrder(1), HorizontalGroup("Functions"), Button("扫描检查资源配置", ButtonSizes.Small)]
        protected bool CheckAllSetting()
        {
            var result = true;
            if(!Application.isBatchMode && !EditorUtility.DisplayDialog("扫描检查资源配置", "耗时操作，是否确认执行", "确认", "取消"))
            {
                return result;
            }
            
            _textureCheckResult.Clear();
            _audioCheckResult.Clear();
            _modelCheckResult.Clear();
            _videoCheckResult.Clear();
            _materialCheckResult.Clear();
            _spineCheckResult.Clear();
            _otherCheckResult.Clear();
            
            var allGUID = AssetDatabase.FindAssets("t:AssetSetting");

            foreach (var guid in allGUID)
            {
                var assetSetting = AssetDatabase.LoadAssetAtPath<AssetSetting>(AssetDatabase.GUIDToAssetPath(guid));
                
                assetSetting.DoCheck();

                foreach (var keyValue in assetSetting.CheckResult)
                {
                    result = false;
                    
                    if (keyValue.Key is Texture)
                    {
                        _textureCheckResult.Add(keyValue.Key, keyValue.Value);
                    }
                    else if (keyValue.Key is AudioClip)
                    {
                        _audioCheckResult.Add(keyValue.Key, keyValue.Value);
                    }
                    else if (keyValue.Key is GameObject)
                    {
                        if (_modelCheckResult.ContainsKey(keyValue.Key))
                        {
                            _modelCheckResult[keyValue.Key] += keyValue.Value;
                        }
                        else
                        {
                            _modelCheckResult.Add(keyValue.Key, keyValue.Value);
                        }
                    }
                    else if (keyValue.Key is VideoClip)
                    {
                        _videoCheckResult.Add(keyValue.Key, keyValue.Value);
                    }
                    else if (keyValue.Key is Material)
                    {
                        _materialCheckResult.Add(keyValue.Key, keyValue.Value);
                    }
                    else if (keyValue.Key is SkeletonDataAsset)
                    {
                        _spineCheckResult.Add(keyValue.Key, keyValue.Value);
                    }
                    else
                    {
                        _otherCheckResult.Add(keyValue.Key, keyValue.Value);
                    }
                }
            }

            return result;
        }
        
        //[PropertyOrder(1), HorizontalGroup("Functions"), Button("扫描应用资源配置", ButtonSizes.Small)]
        protected void ApplyAllSetting()
        {
            if(!Application.isBatchMode && !EditorUtility.DisplayDialog("扫描应用资源配置", "耗时操作，是否确认执行", "确认", "取消"))
            {
                return ;
            }
            
            var allGUID = AssetDatabase.FindAssets("t:AssetSetting");

            foreach (var guid in allGUID)
            {
                var assetSetting = AssetDatabase.LoadAssetAtPath<AssetSetting>(AssetDatabase.GUIDToAssetPath(guid));
                assetSetting.DoApply();
            }
        }

        [PropertyOrder(1), HorizontalGroup("Functions"), Button("导出检查报告csv", ButtonSizes.Small)]
        protected void ExportCheckResult()
        {
            var path = EditorUtility.SaveFilePanel("导出路径", Application.dataPath, "资源检查报告", "csv");
            if (!string.IsNullOrEmpty(path))
            {
                ExportCheckResultToCsv(path);
            }
        }

        protected void ExportCheckResultToCsv(string path)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("类型,路径,错误原因");
            
            foreach (var keyValue in _textureCheckResult)
            {
                stringBuilder.AppendLine($"贴图,{AssetDatabase.GetAssetPath(keyValue.Key)},{keyValue.Value}");
            }
            
            foreach (var keyValue in _modelCheckResult)
            {
                stringBuilder.AppendLine($"模型,{AssetDatabase.GetAssetPath(keyValue.Key)},{keyValue.Value}");
            }
            
            foreach (var keyValue in _videoCheckResult)
            {
                stringBuilder.AppendLine($"视频,{AssetDatabase.GetAssetPath(keyValue.Key)},{keyValue.Value}");
            }
            
            foreach (var keyValue in _audioCheckResult)
            {
                stringBuilder.AppendLine($"音频,{AssetDatabase.GetAssetPath(keyValue.Key)},{keyValue.Value}");
            }
            
            foreach (var keyValue in _audioCheckResult)
            {
                stringBuilder.AppendLine($"材质,{AssetDatabase.GetAssetPath(keyValue.Key)},{keyValue.Value}");
            }
            
            foreach (var keyValue in _spineCheckResult)
            {
                stringBuilder.AppendLine($"Spine,{AssetDatabase.GetAssetPath(keyValue.Key)},{keyValue.Value}");
            }
            
            foreach (var keyValue in _otherCheckResult)
            {
                stringBuilder.AppendLine($"其他,{AssetDatabase.GetAssetPath(keyValue.Key)},{keyValue.Value}");
            }
            
                
            File.WriteAllText(path, stringBuilder.ToString());
        }

        [PropertyOrder(1), HorizontalGroup("Functions"), Button("预览资源配置规则", ButtonSizes.Small)]
        private void OpenAssetSettingOverView()
        {
            EditorWindow.GetWindow<AssetSettingOverViewWindow>("资源配置预览");
        }
        
        //[PropertyOrder(1), HorizontalGroup("Functions"), Button("更新资源配置命名", ButtonSizes.Small)]
        private void UpdateAllAssetSettingName()
        {
            var allGUID = AssetDatabase.FindAssets("t:AssetSetting");

            foreach (var guid in allGUID)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                var name = string.Empty;
                var setting = AssetDatabase.LoadAssetAtPath<AssetSetting>(assetPath);
                if (setting is AudioClipSetting)
                {
                    name = AudioClipSetting.Name;
                }

                if (setting is MaterialSetting)
                {
                    name = MaterialSetting.Name;
                }

                if (setting is ModelSetting)
                {
                    name = ModelSetting.Name;
                }
                
                if (setting is TextureSetting)
                {
                    name = TextureSetting.Name;
                }

                if (setting is VideoClipSetting)
                {
                    name = VideoClipSetting.Name;
                }

                var fileInfo = new FileInfo(AssetSetting.AssetPath2FullPath(assetPath));

                var newAssetPath = Path.Combine(fileInfo.Directory.FullName, $"{name}.asset");
                newAssetPath = AssetSetting.FullPath2AsstPath(newAssetPath);
                
                Debug.Log($"{assetPath} => {newAssetPath}");
                AssetDatabase.MoveAsset(assetPath, newAssetPath);

            }
            
            
        }
    }
}