using System.IO;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    public class MoveImportedAssetsJob : BaseChildContentJob<IContentProcessorJob>
    {
        private readonly string _targetFolder;
        
        public MoveImportedAssetsJob(string targetFolder)
        {
            _targetFolder = targetFolder;
        }
        
        protected override void OnStartChild(IContentProcessorJob parentJob)
        {
            AssetDatabase.Refresh();
            
            // Get all the assets, any assets already in the target folder can be ignored
            var assets = parentJob.ImportedContent.GetAllAssetPaths(ImportedContentCache.Filter.All)
                .Where(x => !x.StartsWith(_targetFolder))
                .ToArray();
            
            var commonPath = Utility.GetLongestCommonPrefix(assets);

            if (!commonPath.IsNullOrEmpty() && !Directory.Exists(commonPath))
                commonPath = Path.GetDirectoryName(commonPath);
            
            foreach (var assetPath in assets)
            {
                string newPath = assetPath.RemoveFirst(commonPath);
                // Starting with a slash will make Path.Combine fail and just return the second part
                if (newPath.StartsWithOneOf("/", "\\"))
                    newPath = newPath.Substring(1);
                newPath = Path.Combine(_targetFolder, newPath).Replace("\\", "/");

                if (FileHelper.MoveAsset(assetPath, newPath))
                    PLog.Info($"Moved asset: {assetPath} -> {newPath}");
                else
                    PLog.Warn($"Failed to move asset '{assetPath}'");
            }
            
            AssetDatabase.Refresh();
            
            TriggerCompleted();
        }
    }
}