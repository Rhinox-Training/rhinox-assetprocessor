using System;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    public class MoveFolderJob : BaseContentJob
    {
        private readonly string _sourceFolder;

        private readonly string _targetFolder;

        public MoveFolderJob(string source, string target)
        {
            _sourceFolder = source.ToLinuxSafePath();
            _targetFolder = target.ToLinuxSafePath();
        }
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            FileHelper.MoveFolder(_sourceFolder, _targetFolder);
            
            AssetDatabase.Refresh();
            
            TriggerCompleted();
        }
    }
}