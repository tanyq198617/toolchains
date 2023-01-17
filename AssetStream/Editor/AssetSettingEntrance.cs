using System.Collections.Generic;
using System.IO;
using UnityEditor;

namespace AssetStream
{
    public class AssetSettingEntrance : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            // 相关资源配置首次导入时使用，后续允许手动修改配置，通过检查功能检查手动可能产生的错误；
            if (assetImporter.importSettingsMissing)
            {
                ApplyToAssetImporter(assetImporter);
            }
        }

        private static bool _processing = false;

        /*
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_processing)
            {
                return;
            }

            _processing = true;

            foreach (string assetPath in importedAssets)
            {
                if (assetPath.Contains("/Editor/") || assetPath.Contains("/Plugins/"))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(assetPath);
                ApplyToAssetImporter(importer);
            }

            _processing = false;
        }
        */

        static void ApplyToAssetImporter(AssetImporter importer)
        {
            AssetSetting setting = null;
            var assetPath = importer.assetPath;
            
            if (importer is TextureImporter)
            {
                setting = FindAssetSetting(assetPath, TextureSetting.Name);
            }

            if (importer is AudioImporter)
            {
                setting= FindAssetSetting(assetPath, AudioClipSetting.Name);
            }

            if (importer is VideoClipImporter)
            {
                setting = FindAssetSetting(assetPath, VideoClipSetting.Name);
            }

            if (importer is ModelImporter)
            {
                setting= FindAssetSetting(assetPath, ModelSetting.Name);
            }
                
            if (setting != null)
            {
                setting.DoApply(AssetImporter.GetAtPath(assetPath));
            }
        }

    static AssetSetting FindAssetSetting(string assetPath, string settingName)
        {
            var fileInfo = new FileInfo(AssetSetting.AssetPath2FullPath(assetPath));

            var directoryInfo = fileInfo.Directory;

            while (directoryInfo != null)
            {
                var settingFile = new FileInfo(Path.Combine(directoryInfo.FullName, $"{settingName}.asset"));

                if (settingFile.Exists)
                {
                    var settingFileAssetPath = AssetSetting.FullPath2AsstPath(settingFile.FullName);

                    return AssetDatabase.LoadAssetAtPath<AssetSetting>(settingFileAssetPath);
                }
                directoryInfo = directoryInfo.Parent;
            }
            
            return null;
        }
    }
}
