using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetStream
{
    [CreateAssetMenu(fileName = Name, menuName = "资源导入配置/音频资源")]
    public class AudioClipSetting : AssetSetting
    {
        public const string Name = "___AudioClipSetting";
        
        public AudioClipSetting() : base("t:audioclip")
        {
             
        }
        
        protected override void RegisterAllCheckFunc()
        {
            
        }
    }
}