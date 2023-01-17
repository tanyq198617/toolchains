using Sirenix.OdinInspector;
using Spine.Unity;
using UnityEditor;
using UnityEngine;

namespace AssetStream
{
    [CreateAssetMenu(fileName = Name, menuName = "资源导入配置/Spine资源")]
    public class SpineSetting : AssetSetting
    {
        public const string Name = "___SpineSetting";
                
        public SpineSetting() : base("t:SkeletonDataAsset")
        {
             
        }

        [TitleGroup("检查选项")]
        [SerializeField] [LabelText("检查转化使用二进制")]
        private bool _checkUseBinary = true;
        
        protected override void RegisterAllCheckFunc()
        {
            RegisterCheckFunc(CheckReadWrite);
        }

        private bool CheckReadWrite(AssetImporter importer, out string error)
        {
            error = "";
            
            var skeletonDataAsset = AssetDatabase.LoadAssetAtPath<SkeletonDataAsset>(importer.assetPath);
            if (skeletonDataAsset && skeletonDataAsset.skeletonJSON)
            {
                var currentJsonPath = AssetDatabase.GetAssetPath(skeletonDataAsset.skeletonJSON);
                if (currentJsonPath.Contains("json"))
                {
                    error = "Spine未转化为二进制";
                    return false;
                }
            }

            return true;
        }

    }
}