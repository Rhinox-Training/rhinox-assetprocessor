using System;
using System.Linq;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEditor.AddressableAssets;

namespace Rhinox.AssetProcessor.Editor
{
    public class AddressableBuildJob : BaseContentJob, IContentDeployJob
    {
        private bool _allowUpdate;
        public string TargetPath { get; private set; }

        public AddressableBuildJob(bool allowUpdate)
        {
            _allowUpdate = allowUpdate;
        }

        protected override void OnStart(BaseContentJob parentJob = null)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var totalCount = settings.groups.Sum(x => x.entries.Count);
            
            AddressableContentBuilder.Build((AddressableContentBuildResult result) =>
            {
                if (result.IsSuccessful)
                    PLog.Info($"Built {totalCount} assets to: '{result.BuildFolder}'");
                else
                    PLog.Error($"Built at '{result.BuildFolder}' failed: {result.BuildInfo.Error}");
                
                // Set output folder
                TargetPath = result.BuildFolder;
                
                TriggerCompleted(!result.IsSuccessful, $"Built at '{result.BuildFolder}' failed: {result.BuildInfo.Error}");
            }, _allowUpdate);
        }
    }
}