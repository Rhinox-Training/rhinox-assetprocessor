﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
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
                
                DirectoryInfo  di = new DirectoryInfo(TargetPath);
                while (!IsEmpty(di)) // wait until the OS actually cleared the folder
                {
                    Thread.Sleep(250);
                    di = new DirectoryInfo(TargetPath);
                }
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

        private static bool IsEmpty(DirectoryInfo di)
        {
            if (!di.Exists)
                return true;
            if (di.EnumerateFiles().Any())
                return false;
            if (di.EnumerateDirectories().Any())
                return false;
            return true;
        }
        
        private static void CopyDirectory(string source, string target, bool overwrite = false)
        {
            var stack = new Stack<FileHelper.Folders>();
            stack.Push(new FileHelper.Folders(source, target));

            int totalFiles = 0;
            while (stack.Count > 0)
            {
                var folders = stack.Pop();
                Directory.CreateDirectory(folders.Target);
                var files = Directory.GetFiles(folders.Source, "*.*");
                totalFiles += files.Length;
                foreach (var file in files)
                {
                    string destFileName = Path.Combine(folders.Target, Path.GetFileName(file));
                    PLog.Debug($"Copying file '{file}' to '{destFileName}' (overwrite: {overwrite})");
                    try
                    {
                        File.Copy(file, destFileName, overwrite);
                        PLog.Debug($"Copy completed, Exists: {FileHelper.Exists(destFileName)}");
                    }
                    catch (Exception e)
                    {
                        PLog.Error($"Failed copy file '{file}' to '{destFileName}', {e.ToString()}");
                        throw;
                    }
                }

                foreach (var folder in Directory.GetDirectories(folders.Source))
                {
                    stack.Push(new FileHelper.Folders(folder, Path.Combine(folders.Target, Path.GetFileName(folder))));
                }
            }
            
            PLog.Info($"Copied {totalFiles} files from '{source}' to '{target}'");
        }
    }
}