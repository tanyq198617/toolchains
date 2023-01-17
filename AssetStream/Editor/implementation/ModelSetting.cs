using System.IO;
using NPOI.HSSF.Record;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AssetStream
{
    [CreateAssetMenu(fileName = Name, menuName = "资源导入配置/模型资源")]
    public class ModelSetting : AssetSetting
    {
        public const string Name = "___ModelSetting";
        
        [TitleGroup("检查选项")]
        [SerializeField] [LabelText("检查Read/Write选项")]
        private bool _checkReadWrite = true;
        
        public ModelSetting() : base("t:model")
        {
             
        }

        public override bool DoApply(AssetImporter assetImporter)

        {
            var modelImporter = assetImporter as ModelImporter;

            modelImporter.isReadable = false;
            modelImporter.materialImportMode = ModelImporterMaterialImportMode.None;
            
            return base.DoApply(assetImporter);
        }

        protected override void RegisterAllCheckFunc()
        {
            RegisterCheckFunc(CheckReadWrite);
        }

        private bool CheckReadWrite(AssetImporter importer, out string error)
        {
            error = string.Empty;
            
            if (!_checkReadWrite)
            {
                return true;
            }

            var modelImporter = importer as ModelImporter;
            if (modelImporter && modelImporter.isReadable)
            {
                error = "可读写";
                return false;
            }

            return true;
        }
    }
}