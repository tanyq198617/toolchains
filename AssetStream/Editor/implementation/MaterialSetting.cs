using System.IO;
using CodeStage.Maintainer.Core;
using I2;
using NPOI.HSSF.Record;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AssetStream
{
    [CreateAssetMenu(fileName = Name, menuName = "资源导入配置/材质资源")]
    public class MaterialSetting : AssetSetting
    {
        public const string Name = "___MaterialSetting";

        [TitleGroup("检查选项")]
        [SerializeField] [LabelText("检查对StandardShader引用")]
        private bool _checkStandardShader = true;
        
        public MaterialSetting() : base("t:material")
        {
             
        }

        protected override void RegisterAllCheckFunc()
        {
            RegisterCheckFunc(CheckStandardShader);
        }

        private bool CheckStandardShader(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkStandardShader)
            {
                return true;
            }
            
            var allAsset = AssetDatabase.LoadAllAssetsAtPath(assetImporter.assetPath);
            foreach (var asset in allAsset)
            {
                if (asset is Material)
                {
                    var material = asset as Material;
                    if (material && material.shader.name.StartsWith("Standard"))
                    {
                        error = "引用StandardShader";
                        return false;
                    }
                }
            }

            return true;
        }

    }
}