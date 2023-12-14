using System;
using System.IO;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEngine.WSA;

namespace Rhinox.AssetProcessor.Editor
{
    public class CopyFolderJob : BaseContentJob
    {
        private readonly string _sourcePath;

        private readonly string _targetPath;

        private bool _clearTarget;
        
        public CopyFolderJob(string source, string target, bool clearTarget)
        {
            _clearTarget = clearTarget;
            
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (target == null) throw new ArgumentNullException(nameof(target));

            _sourcePath = source;
            _targetPath = target;
        }

        protected override void OnStart(BaseContentJob parentJob = null)
        {
            if (_clearTarget)
                FileHelper.DeleteDirectoryIfExists(_targetPath);
            
            if (!Directory.Exists(_sourcePath))
            {
                PLog.Info($"Copying folder {_sourcePath} canceled. No folder found.");
                TriggerCompleted();
                return;
            }
            
            PLog.Info($"Copying folder {_sourcePath} -> {_targetPath}");
            
            FileHelper.CopyDirectory(_sourcePath, _targetPath, true);
            
            TriggerCompleted();
        }
    }
}