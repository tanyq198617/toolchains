using System.IO;
using Sirenix.OdinInspector;
using Toolset;
using UnityEditor;
using UnityEngine;

namespace AssetStream
{
    [CreateAssetMenu(fileName = Name, menuName = "资源导入配置/AssetBundleName")]
    public class AssetBundleNameSetting : AssetSetting
    {
        public const string Name = "___AssetBundleNameSetting";
        
        [TitleGroup("检查选项")]
        [SerializeField] [LabelText("检查AssetBundleName有效性")]
        private bool _checkAssetBundleName = true;
        
        private AssetBundleSettingHelper _abSettingHelper = new AssetBundleSettingHelper();
        
        public AssetBundleNameSetting() : base("*")
        {
             
        }
        
        protected override void RegisterAllCheckFunc()
        {
            RegisterCheckFunc(CheckAssetBundleName);
        }
        
        private bool CheckAssetBundleName(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkAssetBundleName)
            {
                return true;
            }

            if (!_abSettingHelper.DoCheckAssetBundleName(assetImporter.assetPath))
            {
                error = $"BundleName错误:({assetImporter.assetBundleName})";
                return false;
            }

            return true;
        }
    }
}