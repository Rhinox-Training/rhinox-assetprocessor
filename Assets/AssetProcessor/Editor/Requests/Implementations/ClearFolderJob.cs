using System;
using System.Collections;
using System.IO;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Rhinox.AssetProcessor.Editor
{
    public class ClearFolderJob : BaseContentJob
    {
        private readonly string _targetFolder;
        
        private EditorCoroutine _start;

        public ClearFolderJob(string folder)
        {
            if (folder == null) throw new ArgumentNullException(nameof(folder));

            _targetFolder = folder;
        }

        protected override void OnStart(BaseContentJob parentJob = null)
        {
            _start = EditorCoroutineUtility.StartCoroutineOwnerless(ProcessRequest());
        }

        private IEnumerator ProcessRequest()
        {
            PLog.Info($"Clearing folder {_targetFolder}");
#if UNITY_EDITOR
            // Remove dir (recursively)
            var fullPath = FileHelper.GetFullPath(_targetFolder, GlobalData.ProjectPath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                var metaPath = GetFolderMetaPath(fullPath);
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
            }
#else
            FileHelper.ClearAssetDirectory(OutputPath);
#endif

            // Wait a bit, the operation can take some time
            yield return new WaitForSeconds(.2f);
            
            TriggerCompleted();
        }
        
        private string GetFolderMetaPath(string folderPath)
        {
            folderPath = folderPath.Trim();
            if (folderPath.EndsWith("/") || folderPath.EndsWith("\\"))
                return folderPath.Substring(0, folderPath.Length - 1) + ".meta";
            return folderPath + ".meta";
        }
    }
}