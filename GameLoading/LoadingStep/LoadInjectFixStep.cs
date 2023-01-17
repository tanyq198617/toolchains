using System.IO;
using ClientCore;
using UnityEngine;

namespace GameLoading.LoadingStep
{
    /**
     * 加载InjectFix
     */
    public class LoadInjectFixStep : LoadingPipelineStep
    {
        private FileDownloadRequest _downloadRequest = null;
        
        public LoadInjectFixStep(int step, string descriptionKey):base(step, descriptionKey)
        {
            
        }
        
        public override void OnStart()
        {
            base.OnStart();

            if (Application.isEditor)
            {
                IsDone = true;
                return;
            }
            
            //
            if (NetApi.inst.PatchContent != null && NetApi.inst.PatchContent.Count > 0)
            {
                foreach (var patch in NetApi.inst.PatchContent)
                {
                    var patchVersion = patch.Key;
                    var patchFileName = patch.Value;
                    
                    var patchFilePath = Path.Combine(IFixManager.PATCH_FOLDER_PATH, patchFileName);
                    
                    // 
                    if (!File.Exists(patchFilePath))
                    {
                        var url = NetApi.inst.BuildPatchUrl(patchVersion, patchFileName);
                        // patchFileName  md5.bytes
                        var md5 = patchFileName.Substring(0, patchFileName.IndexOf("."));
                        
                        _downloadRequest = ManagerFacade.FileDownLoadManager.DownloadFileAsync(url, patchFilePath, FileDownloadPriority.High, md5);
                        _downloadRequest.DownloadFinishCallback += OnDownloadPatchFileCallback;
                    }
                    else
                    {
                        LoadInjectFixFile();
                    }
                }
            }
            else
            {
                IsDone = true;
            }
        }

        public override void OnEnd()
        {
            base.OnEnd();
        }

        public override float Progress
        {
            get
            {
                if (_downloadRequest != null)
                {
                    return _downloadRequest.DownloadProgress;
                }

                return 0;
            }
        }

        private void OnDownloadPatchFileCallback(FileDownloadRequest downloadRequest)
        {
            if (downloadRequest.IsSuccess)
            {
                LoadInjectFixFile();
            }
            else
            {
                Utils.RestartGameWithErrorCode(ErrorCode.ErrorDownloadPatchFailed);
            }
        }
        
        private void LoadInjectFixFile()
        {
            var patchVersion = IFixManager.LoadPatches();
            
            CrashReportCustomField.SetValue(CrashReportCustomField.Key.PatchVersion, patchVersion);
            
            IsDone = true;
        }
    }
}