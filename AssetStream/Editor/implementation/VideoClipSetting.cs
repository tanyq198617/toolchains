using System.IO;
using DG.Tweening.Core.Easing;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace AssetStream
{
    [CreateAssetMenu(fileName = Name, menuName = "资源导入配置/视频资源")]
    public class VideoClipSetting : AssetSetting
    {
        public const string Name = "___VideoClipSetting";

        [LabelText("检查视频尺寸小于1280*720")]
        private bool _checkVideoSize = true;
        
        public VideoClipSetting() : base("t:videoclip")
        {
             
        }
        
        protected override void RegisterAllCheckFunc()
        {
            RegisterCheckFunc(CheckVideoSize);
        }

        protected bool CheckVideoSize(AssetImporter assetImporter, out string error)
        {
            error = string.Empty;
            
            var videoClipImporter = assetImporter as VideoClipImporter;

            var width = videoClipImporter.defaultTargetSettings.customWidth;
            var height = videoClipImporter.defaultTargetSettings.customHeight;

            var maxSize = new Vector2(1280, 720);

            if (width > 1280 || height > 720)
            {
                error = "超出1280*720尺寸";
                return false;
            }

            return true;
        }
        

    }
}