﻿using System;
using System.Linq;
using Rhinox.Perceptor;

namespace Rhinox.AssetProcessor.Editor
{
    public class AddressableBuildJob : BaseChildContentJob<IContentProcessorJob>, IContentDeployJob
    {
        private readonly string[] _defaultLabels;
        private bool _allowUpdate;
        public string TargetPath { get; private set; }

        public AddressableBuildJob(bool allowUpdate, params string[] defaultLabels)
        {
            _defaultLabels = defaultLabels.ToArray();
            _allowUpdate = allowUpdate;
        }

        protected override void OnStartChild(IContentProcessorJob parentJob)
        {
            PLog.Debug($"Fetching ImportedContent from {parentJob.GetType().Name}");
            var importedAssets = parentJob.ImportedContent;

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
            }, _allowUpdate);
        }
    }
}