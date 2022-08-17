using System;
using System.Linq;
using Rhinox.Perceptor;

namespace Rhinox.AssetProcessor.Editor
{
    public class AddressableBuildJob : BaseContentJob, IContentDeployJob
    {
        private readonly string[] _defaultLabels;
        public string TargetPath { get; private set; }

        public AddressableBuildJob(params string[] defaultLabels)
        {
            _defaultLabels = defaultLabels.ToArray();
        }
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            var contentProcessorParent = GetParentOfType<IContentProcessorJob>();
            if (contentProcessorParent == null)
            {
                Log($"Job '{this}': Nothing to process no IContentProcessorJob found in parents");
                TriggerCompleted();
                return;
            }
            var importedAssets = contentProcessorParent.ImportedContent;

            AddressableContentBuilder.Clear();

            int totalAddedCount = 0;
            int totalCount = 0;
            string[] labelSet = new string[_defaultLabels.Length + 1];
            Array.Copy(_defaultLabels, 0, labelSet, 1, _defaultLabels.Length);
            foreach (var groupName in importedAssets.Groups)
            {
                labelSet[0] = groupName;
                PLog.Debug($"Loading Group: {groupName}");
                var assetPaths = importedAssets.GetAssets(groupName);
                int i = 0;
                foreach (var assetPath in assetPaths)
                {
                    PLog.Debug($"Adding Asset [{++i}/{assetPaths.Count}]: '{assetPath}'");
                }
                totalCount += assetPaths.Count;
                
                int addedCount = AddressableContentBuilder.AddAssets(assetPaths, labelSet);
                totalAddedCount += addedCount;
            }

            AddressableContentBuilder.Build((AddressableContentBuildResult result) =>
            {
                if (result.IsSuccessful)
                    PLog.Info($"Built {totalAddedCount} assets out of {totalCount} to: '{result.BuildFolder}'");
                else
                    PLog.Error($"Built at '{result.BuildFolder}' failed: {result.BuildInfo.Error}");
                
                // Set output folder
                TargetPath = result.BuildFolder;
                
                TriggerCompleted(!result.IsSuccessful, $"Built at '{result.BuildFolder}' failed: {result.BuildInfo.Error}");
            });
        }
    }
}