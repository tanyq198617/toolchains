using System;
using System.Collections.Generic;
using System.IO;
using I2;
using NPOI.HSSF.Record;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AssetStream
{
    public abstract class AssetSetting : ScriptableObject
    {
        [SerializeField] protected Preset _preset;

        [LabelText("忽略资源列表")]
        [SerializeField] protected List<Object> _ignoreList;
        
        [LabelText("检查结果"), DictionaryDrawerSettings(KeyLabel = "资源", ValueLabel = "错误原因"), ShowInInspector, ReadOnly]
        protected Dictionary<Object, string> _checkResult = new Dictionary<Object, string>();

        public Dictionary<Object, string> CheckResult
        {
            get { return _checkResult; }
        }
        
        protected string _searchPattern;
        protected List<CheckFunc> _allCheckFunc = null;
        
        protected List<CheckFunc> AllCheckFunc
        {
            get
            {
                if (_allCheckFunc == null)
                {
                    _allCheckFunc = new List<CheckFunc>();
                    RegisterAllCheckFunc();
                }

                return _allCheckFunc;
            }
        }
            
        protected AssetSetting(string searchParttern)
        {
            _searchPattern = searchParttern;
        }
        
        public string PresetDescription
        {
            get
            {
                var presetName =  _preset ? _preset.name : "忽略";
                return $"{this.GetType().Name}({presetName})";
            }
        }
        
        protected abstract void RegisterAllCheckFunc();

        protected delegate bool CheckFunc(AssetImporter assetImporter, out string error);

        //[Button("应用资源导入配置", ButtonSizes.Large)]
        public void DoApply()
        {
            try
            {
                var allAssetImporter = new List<AssetImporter>();
                
                var path = AssetDatabase.GetAssetPath(this);

                var customImporterFile = new FileInfo(AssetPath2FullPath(path));
                var customImporterDirectory = customImporterFile.Directory.FullName;
                var customImporterFileName = customImporterFile.Name;

                var allAssetGUID = AssetDatabase.FindAssets(_searchPattern,
                    new string[] {FullPath2AsstPath(customImporterDirectory)});

                var current = 0;
                foreach (var assetGUI in allAssetGUID)
                {
                    current++;
                    if (current % 20 == 0)
                    {
                        EditorUtility.DisplayProgressBar("应用中", $"{current}/{allAssetGUID.Length}",
                            current / (float) allAssetGUID.Length);
                    }

                    var assetPath = AssetDatabase.GUIDToAssetPath(assetGUI);
                    var fullAssetPath = AssetPath2FullPath(assetPath);

                    if (assetPath.Contains("/Editor/") || assetPath.Contains("/Plugins/"))
                    {
                        continue;
                    }
                    
                    if (ExistOverrideSetting(fullAssetPath, customImporterDirectory, customImporterFileName))
                    {
                        //Debug.Log($"Override {assetPath}");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var assetImporter = AssetImporter.GetAtPath(assetPath);

                        if (DoApply(assetImporter))
                        {
                            allAssetImporter.Add(assetImporter);
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.Refresh();
                EditorUtility.ClearProgressBar();
            }
        }
        
        public virtual bool DoApply(AssetImporter assetImporter)
        {
            if (assetImporter)
            {
                if (_preset)
                {
                    var assetPath = assetImporter.assetPath;

                    if (_preset.CanBeAppliedTo(assetImporter))
                    {
                        _preset.ApplyTo(assetImporter);
                    }
                    else
                    {
                        Debug.LogError("Preset Error.");
                    }
                }
                else
                {
                    Debug.Log("忽略 Preset");
                }
            }
            
            if (AssetDatabase.WriteImportSettingsIfDirty(assetImporter.assetPath))
            {
                return true;
            }
            
            return false;
        }
        
        [Button("执行资源检查", ButtonSizes.Large)]
        public void DoCheck()
        {
            _checkResult.Clear();

            var ignorePathList = new List<string>(_ignoreList.Count);
            foreach (var ignoreObject in _ignoreList)
            {
                if (ignoreObject)
                {
                    var path = AssetDatabase.GetAssetPath(ignoreObject);
                    ignorePathList.Add(path);
                }
            }
            
            try
            {
                var path = AssetDatabase.GetAssetPath(this);

                var customImporterFile = new FileInfo(AssetPath2FullPath(path));
                var customImporterDirectory = customImporterFile.Directory.FullName;
                var customImporterFileName = customImporterFile.Name;

                var allAssetGUID = AssetDatabase.FindAssets(_searchPattern,
                    new string[] {FullPath2AsstPath(customImporterDirectory)});

                var current = 0;
                
                foreach (var assetGUI in allAssetGUID)
                {
                    current++;

                    if (current % 20 == 0)
                    {
                        EditorUtility.DisplayProgressBar("检查中", $"{current}/{allAssetGUID.Length}", current / (float)allAssetGUID.Length);
                    }
                    
                    var assetPath = AssetDatabase.GUIDToAssetPath(assetGUI);
                    
                    if (assetPath.Contains("/Editor/") || assetPath.Contains("/Plugins/"))
                    {
                        continue;
                    }

                    if (ignorePathList.Contains(assetPath))
                    {
                        continue;
                    }
                    
                    var fullAssetPath = AssetPath2FullPath(assetPath);

                    if (ExistOverrideSetting(fullAssetPath, customImporterDirectory, customImporterFileName))
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        var assetImporter = AssetImporter.GetAtPath(assetPath);
                        if (assetImporter)
                        {
                            var allError = "";
                            foreach (var checkFunc in AllCheckFunc)
                            {
                                if (!checkFunc.Invoke(assetImporter, out var error))
                                {
                                    allError += $"{error};";
                                }
                            }

                            if (allError.Length > 0)
                            {
                                var asset = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
                                _checkResult.Add(asset, allError);
                            }
                        }
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
        }
        
        protected void RegisterCheckFunc(CheckFunc checkFunc)
        {
            _allCheckFunc.Add(checkFunc);
        }
        
        public static string FullPath2AsstPath(string fullPath)
        {
            {
                var index = fullPath.IndexOf("Assets/", StringComparison.Ordinal);
                if (index > 0)
                {
                    return fullPath.Substring(index);
                }
            }

            {
                var index = fullPath.IndexOf("Assets", StringComparison.Ordinal);
                if (index > 0)
                {
                    return fullPath.Substring(index);
                }
            }
            
            throw new System.Exception($"FullPath2AsstPath: {fullPath}");
        }

        public static string AssetPath2FullPath(string projectPath)
        {
            return Path.Combine(Application.dataPath, $"../{projectPath}");
        }

        protected void ForeachSubFolderConditionDo(DirectoryInfo directoryInfo,  Predicate<DirectoryInfo> conditionFunc, Action<DirectoryInfo> doFunc)
        {
            doFunc?.Invoke(directoryInfo);
            
            var allChildDirectory = directoryInfo.GetDirectories("", SearchOption.TopDirectoryOnly);

            foreach (var childDirectory in allChildDirectory)
            {
                ForeachSubFolderConditionDo(childDirectory, conditionFunc, doFunc);
            }
        }

        private bool ExistOverrideSetting(string fullAssetPath, string customImporterDirectory, string customImporterFileName)
        {
            var assetFileInfo = new FileInfo(fullAssetPath);

            var parentDirectory = assetFileInfo.Directory;
            while (parentDirectory != null)
            {
                if (parentDirectory.FullName == customImporterDirectory)
                {
                    return false;
                }

                var overrideImporterFileInfo =
                    new FileInfo(Path.Combine(parentDirectory.FullName, customImporterFileName));

                if (overrideImporterFileInfo.Exists)
                {
                    return true;
                }

                parentDirectory = parentDirectory.Parent;
            }

            return false;
        }
    }
}
