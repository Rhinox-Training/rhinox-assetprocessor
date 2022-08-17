using System.IO;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;

namespace Rhinox.AssetProcessor.Editor
{
    public class PostContentDeployJob : BaseChildContentJob<IContentDeployJob>, IContentDeployJob
    {
        public string TargetPath { get; }
        public bool WillCopyContent { get; }
        public bool OverwriteTarget { get; }

        public PostContentDeployJob(string targetPath, bool copyContent = true, bool overwriteTarget = false)
        {
            TargetPath = targetPath;
            WillCopyContent = copyContent;
            OverwriteTarget = overwriteTarget;
        }
        
        protected override void OnStartChild(IContentDeployJob parentJob)
        {
            PLog.Info($"Start Post Content to '{TargetPath}' (Overwrite: {OverwriteTarget})");
            var deployedPath = parentJob.TargetPath;
            if (OverwriteTarget)
            {
                PLog.Info($"Clearing Content in '{TargetPath}'");
                FileHelper.ClearDirectoryContentsIfExists(TargetPath);
            }

            FileHelper.CopyDirectory(deployedPath, TargetPath, OverwriteTarget);
            PLog.Info($"Posting Content from '{deployedPath}' at '{TargetPath}'");

            if (!WillCopyContent)
            {
                FileHelper.ClearDirectoryContentsIfExists(deployedPath);
                PLog.Info($"Clearing Content in '{deployedPath}'");
            }

            TriggerCompleted();
        }
    }
}