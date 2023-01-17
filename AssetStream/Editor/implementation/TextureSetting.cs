using System.IO;
using NPOI.SS.Formula.Functions;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace AssetStream
{
    [CreateAssetMenu(fileName = TextureSetting.Name, menuName = "资源导入配置/贴图资源")]
    public class TextureSetting : AssetSetting
    {
        public const string Name = "___TextureSetting";
        
        public TextureSetting() : base("t:texture")
        {
             
        }

        [TitleGroup("检查选项")]
        [SerializeField] [LabelText("检查文件类型:png/tga/psd其他格式不支持")]
        private bool _checkFileType = true;
        [SerializeField] [LabelText("检查超大图片文件")]
        private bool _checkFileSize = true;
        [SerializeField] [LabelText("检查图片宽高满足4x")]
        private bool _checkSize4N = true;
        [SerializeField] [LabelText("检查读写属性关闭")]
        private bool _checkReadWrite = true;
        [SerializeField] [LabelText("检查Mipmap属性关闭")]
        private bool _checkMipmap = true;
        [SerializeField] [LabelText("检查iOS使用Astc4x4")]
        private bool _checkIOSAstc4x4Usage = false;
        [SerializeField] [LabelText("检查iOS使用Astc5x5")]
        private bool _checkIOSAstc5x5Usage = false;
        [SerializeField] [LabelText("检查iOS使用Astc6x6")]
        private bool _checkIOSAstc6x6Usage = false;
        [SerializeField] [LabelText("检查Android使用etc2")]
        private bool _chedkAndroidEtc2 = true;
        
        protected override void RegisterAllCheckFunc()
        {
            RegisterCheckFunc(CheckFileSize);
            RegisterCheckFunc(CheckFileType);
            RegisterCheckFunc(CheckSize4N);
            RegisterCheckFunc(CheckReadWrite);
            RegisterCheckFunc(CheckMipmap);
            RegisterCheckFunc(CheckIOSAstc4x4Usage);
            RegisterCheckFunc(CheckIOSAstc5x5Usage);
            RegisterCheckFunc(CheckIOSAstc6x6Usage);
            RegisterCheckFunc(CheckAndroidEtc2);
        }
        
        public override bool DoApply(AssetImporter assetImporter)
        {
            var textureImporter = assetImporter as TextureImporter;

            if (textureImporter)
            {
                if (assetImporter.assetPath.EndsWith("exr"))
                {
                    return false;
                }
                
                var iosSetting = textureImporter.GetPlatformTextureSettings("iPhone");
                iosSetting.overridden = true;
                {
                    if (_checkIOSAstc4x4Usage)
                    {
                        iosSetting.format = TextureImporterFormat.ASTC_4x4;
                        iosSetting.compressionQuality = 100;
                    }
                    else if(_checkIOSAstc5x5Usage)
                    {
                        iosSetting.format = TextureImporterFormat.ASTC_5x5;
                        iosSetting.compressionQuality = 100;
                    }
                    else if(_checkIOSAstc6x6Usage)
                    {
                        iosSetting.format = TextureImporterFormat.ASTC_6x6;
                        iosSetting.compressionQuality = 100;
                    }
                    textureImporter.SetPlatformTextureSettings(iosSetting);
                }
                
                if (AssetDatabase.WriteImportSettingsIfDirty(assetImporter.assetPath))
                {
                    return true;
                }
            }

            return true;
        }

        private bool CheckFileSize(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkFileSize)
            {
                return true;
            }

            if (assetImporter.assetPath.EndsWith(".png"))
            {
                var fullPath = AssetPath2FullPath(assetImporter.assetPath);

                var fileInfo = new FileInfo(fullPath);
                
                if (fileInfo.Exists && fileInfo.Length  >= (8 * 1024 * 1024))
                {
                    error = "图片文件超大";
                    return false;
                }    
            }

            return true;
        }

        private bool CheckSize4N(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkSize4N)
            {
                return true;
            }
            
            var texture = AssetDatabase.LoadAssetAtPath<Texture>(assetImporter.assetPath);
            if (texture && texture.width > 1 && texture.height > 1)
            {
                if (texture.width % 4 != 0 || texture.height % 4 != 0)
                {
                    error = "图片宽高不满足4的倍数";
                    return false;

                }
            }
            
            return true;
        }
        
        private bool CheckReadWrite(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkReadWrite)
            {
                return true;
            }
            
            var textureImporter = assetImporter as TextureImporter;
            if (textureImporter != null && textureImporter.isReadable)
            {
                error = "读写属性开启";
                return false;
            }
            
            return true;
        }
        private bool CheckMipmap(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkMipmap)
            {
                return true;
            }

            if (assetImporter.assetPath.EndsWith("exr"))
            {
                return true;
            }
            
            var textureImporter = assetImporter as TextureImporter;

            if (textureImporter != null && textureImporter.mipmapEnabled)
            {
                error = "Mipmap属性开启";
                return false;
            }
            
            return true;
        }
        
        private bool CheckFileType(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkFileType)
            {
                return true;
            }

            var allValidExtension = new string[] {".exr", ".tga", ".png", ".psd"};
            
            var fileInfo = new FileInfo(assetImporter.assetPath);
            var extension = fileInfo.Extension.ToLower();

            foreach (var validExtension in allValidExtension)
            {
                if (validExtension == extension)
                {
                    return true;
                }
            }
            
            error = "贴图文件类型不支持";
            
            return true;
        }
        
        private bool CheckIOSAstc4x4Usage(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkIOSAstc4x4Usage)
            {
                return true;
            }

            if (assetImporter.assetPath.EndsWith("exr"))
            {
                return true;
            }
            
            var textureImporter = assetImporter as TextureImporter;

            
            if (textureImporter != null)
            {
                var iosSetting = textureImporter.GetPlatformTextureSettings("iPhone");
                if (!iosSetting.overridden || (iosSetting.format != TextureImporterFormat.ASTC_4x4))
                {
                    error = "iOS平台没有使用Astc4x4";
                    return false;
                }
            }
            
            return true;
        }
        
        private bool CheckIOSAstc5x5Usage(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkIOSAstc5x5Usage)
            if (!_checkIOSAstc5x5Usage)
            {
                return true;
            }

            if (assetImporter.assetPath.EndsWith("exr"))
            {
                return true;
            }
            
            var textureImporter = assetImporter as TextureImporter;

            
            if (textureImporter != null)
            {
                var iosSetting = textureImporter.GetPlatformTextureSettings("iPhone");
                if (!iosSetting.overridden || (iosSetting.format != TextureImporterFormat.ASTC_5x5))
                {
                    error = "iOS平台没有使用Astc5x5";
                    return false;
                }
            }
            
            return true;
        }
        
        private bool CheckIOSAstc6x6Usage(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_checkIOSAstc6x6Usage)
            {
                return true;
            }

            if (assetImporter.assetPath.EndsWith("exr"))
            {
                return true;
            }
            
            var textureImporter = assetImporter as TextureImporter;

            
            if (textureImporter != null)
            {
                var iosSetting = textureImporter.GetPlatformTextureSettings("iPhone");
                if (!iosSetting.overridden || (iosSetting.format != TextureImporterFormat.ASTC_6x6))
                {
                    error = "iOS平台没有使用Astc6x6";
                    return false;
                }
            }
            
            return true;
        }

        private bool CheckAndroidEtc2(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;

            if (!_chedkAndroidEtc2)
            {
                return true;
            }

            if (assetImporter.assetPath.EndsWith("exr"))
            {
                return true;
            }
            
            var textureImporter = assetImporter as TextureImporter;

            
            if (textureImporter != null)
            {
                var androidSetting = textureImporter.GetPlatformTextureSettings("Android");
                if (androidSetting.overridden)
                {
                    if(androidSetting.format != TextureImporterFormat.ETC2_RGB4 &&
                       androidSetting.format != TextureImporterFormat.ETC2_RGBA8 &&
                       androidSetting.format != TextureImporterFormat.ETC_RGB4)
                    {
                        error = "Android平台没有使用Etc2";
                        return false;
                    } 
                }
                else
                {
                    var format = textureImporter.GetAutomaticFormat("Android");
                    if (format != TextureImporterFormat.ETC2_RGB4 &&
                        format != TextureImporterFormat.ETC2_RGBA8 &&
                        androidSetting.format != TextureImporterFormat.ETC_RGB4)
                    {
                        error = "Android平台没有使用Etc2";
                        return false;
                    }
                }
            }
            
            return true;
        }
    }
}