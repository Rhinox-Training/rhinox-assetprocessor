using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.AssetProcessor.Editor
{
    public static class GroupFolderHandler
    {
        public static Dictionary<string, ICollection<string>> GetFilesByGroup(string assetsPath)
        {
            var dictionary = new Dictionary<string, ICollection<string>>();
            var groupImportFolders = GetGroupImportFolders(assetsPath);
            foreach (var groupImportFolder in groupImportFolders)
            {
                string clientName = groupImportFolder.Name.ToUpperInvariant();
                var files = GetFilesFromDirectoryInfo(groupImportFolder);

                dictionary[clientName] = files;
            }

            return dictionary;
        }

        private static ICollection<string> GetFilesFromDirectoryInfo(DirectoryInfo dir)
        {
            return FileHelper.GetFiles(dir.FullName, "*.*", SearchOption.AllDirectories);
        }
        
        public static IReadOnlyCollection<DirectoryInfo> GetGroupImportFolders(string assetsPath)
        {
            DirectoryInfo di = new DirectoryInfo(assetsPath);
            if (!di.Exists)
            {
                PLog.Error($"Directory '{assetsPath}' does not exist, exiting...");
                return Array.Empty<DirectoryInfo>();
            }

            var subDirectories = FileHelper.GetChildFolders(assetsPath);
            if (subDirectories.Count == 0)
            {
                PLog.Warn($"Directory '{assetsPath}' was empty (need folders by client name), exiting...");
                return Array.Empty<DirectoryInfo>();
            }

            return subDirectories;
        }
    }
}