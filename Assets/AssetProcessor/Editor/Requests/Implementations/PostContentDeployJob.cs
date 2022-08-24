using System.Collections.Generic;
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

            CopyDirectory(deployedPath, TargetPath, OverwriteTarget);
            PLog.Info($"Posting Content from '{deployedPath}' at '{TargetPath}'");

            if (!WillCopyContent)
            {
                FileHelper.ClearDirectoryContentsIfExists(deployedPath);
                PLog.Info($"Clearing Content in '{deployedPath}'");
            }

            TriggerCompleted();
        }
        
        private static void CopyDirectory(string source, string target, bool overwrite = false)
        {
            var stack = new Stack<FileHelper.Folders>();
            stack.Push(new FileHelper.Folders(source, target));

            while (stack.Count > 0)
            {
                var folders = stack.Pop();
                Directory.CreateDirectory(folders.Target);
                foreach (var file in Directory.GetFiles(folders.Source, "*.*"))
                {
                    string destFileName = Path.Combine(folders.Target, Path.GetFileName(file));
                    File.Copy(file, destFileName, overwrite);
                    PLog.Debug($"Copy file '{file}' to '{destFileName}' (overwrite: {overwrite})");
                }

                foreach (var folder in Directory.GetDirectories(folders.Source))
                {
                    stack.Push(new FileHelper.Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                }
            }
        }
    }
}