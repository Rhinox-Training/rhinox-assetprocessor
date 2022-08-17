using System;
using System.Collections.Generic;
using System.IO;
using Rhinox.Lightspeed.Addressables;
using Rhinox.Perceptor;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Rhinox.AssetProcessor.Editor
{
    public class AddressableContentBuildResult
    {
        public string BuildFolder { get; }
        public AddressablesPlayerBuildResult BuildInfo { get; }
        public bool IsSuccessful { get; }

        public AddressableContentBuildResult(string folder, AddressablesPlayerBuildResult result)
        {
            BuildFolder = folder;
            BuildInfo = result;
            IsSuccessful = string.IsNullOrEmpty(result.Error);
        }
    }
    
    public static class AddressableContentBuilder
    {
        public const string LOG_NAME = "addressable-builder";

        public static string BuildFolder => AddressablesExt.GetTargetBuildPath();
        
        public static void Build(Action<AddressableContentBuildResult> callback = null)
        {
            var result = AddressablesExt.BuildOrUpdatePlayerContent();
            callback?.Invoke(new AddressableContentBuildResult(BuildFolder, result));
        }
        
        public static int AddAssets(IReadOnlyCollection<string> assetPaths, params string[] labels)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var entriesAdded = new List<AddressableAssetEntry>();
            foreach (var assetPath in assetPaths)
            {
                if (TryCreateEntry(settings, assetPath, out AddressableAssetEntry assetEntry, labels))
                    entriesAdded.Add(assetEntry);
            }

            if (entriesAdded.Count > 0)
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryMoved, entriesAdded, true);
            return entriesAdded.Count;
        }

        [MenuItem("Modulab/Clear Asset Entries from DefaultGroup")]
        public static void Clear()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var assetEntries = new List<AddressableAssetEntry>();
            settings.GetAllAssets(assetEntries, false, groupFilter: x => x.Default);
            int oldCount = assetEntries.Count;
            
            foreach (var entry in assetEntries)
                settings.RemoveAssetEntry(entry.guid);
            
            assetEntries.Clear();
            settings.GetAllAssets(assetEntries, false, groupFilter: x => x.Default);
            int newCount = assetEntries.Count;
            
            PLog.Info<AddressableBuilderLogger>($"Cleared {oldCount - newCount} assets, remaining {newCount}");
        }
        
        private static bool TryCreateEntry(AddressableAssetSettings settings, string assetPath, out AddressableAssetEntry assetEntry, params string[] labels)
        {
            if (assetPath == null)
            {
                assetEntry = null;
                return false;
            }

            var guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                assetEntry = null;
                return false;
            }

            var group = settings.DefaultGroup;
            var entry = settings.CreateOrMoveEntry(guid, group);

            entry.address = assetPath;
            foreach (var label in labels)
                entry.labels.Add(label);
            assetEntry = entry;
            return true;
        }
    }
}