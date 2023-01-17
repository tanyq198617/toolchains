using System.IO;
using I2;
using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace AssetStream
{
    public class AssetSettingOverViewWindow : OdinMenuEditorWindow
    {
        
        protected override OdinMenuTree BuildMenuTree()
        {
            var tree = new OdinMenuTree(true);
            tree.Config.DrawSearchToolbar = true;
            
            var allGUID = AssetDatabase.FindAssets("t:AssetSetting");

            foreach (var guid in allGUID)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var assetSetting = AssetDatabase.LoadAssetAtPath<AssetSetting>(path);

                var fileInfo = new FileInfo(path);
                
                tree.Add($"{fileInfo.Directory}/{assetSetting.PresetDescription}", assetSetting);
                
            }
            return tree;
        }
    }
}