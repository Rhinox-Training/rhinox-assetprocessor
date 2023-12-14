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
        
        public static AddressableAssetGroup FindGroup(Func<AddressableAssetGroup, bool> predicate)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            foreach (var group in settings.groups)
            {
                if (predicate(group))
                    return group;
            }

            return null;
        }
        
        public static void Build(Action<AddressableContentBuildResult> callback = null, bool allowUpdate = true)
        {
            AddressablesPlayerBuildResult result;
            if (allowUpdate)
                result = AddressablesExt.BuildOrUpdatePlayerContent();
            else
                AddressableAssetSettings.BuildPlayerContent(out result);
            callback?.Invoke(new AddressableContentBuildResult(BuildFolder, result));
        }
        
        public static int AddAssets(IReadOnlyCollection<string> assetGuids, params string[] labels)
        {
            return AddAssets(null, assetGuids, labels);
        }
        
        public static int AddAssets(AddressableAssetGroup group, IReadOnlyCollection<string> assetGuids, params string[] labels)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (group == null)
                group = settings.DefaultGroup;
                    
            var entriesAdded = new List<AddressableAssetEntry>();
            foreach (var guid in assetGuids)
            {
                if (TryCreateEntry(settings, group, guid, out AddressableAssetEntry assetEntry, labels))
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
            ClearGroup(settings.DefaultGroup);
        }
        
        [MenuItem("Modulab/Clear Addressables")]
        public static void ClearAddressables()
        {
            foreach (var group in AddressableAssetSettingsDefaultObject.Settings.groups)
                ClearGroup(group);
        }

        public static void ClearGroup(AddressableAssetGroup group)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var assetEntries = new List<AddressableAssetEntry>();
            settings.GetAllAssets(assetEntries, false, groupFilter: x => x == group);
            int oldCount = assetEntries.Count;
            
            foreach (var entry in assetEntries)
                settings.RemoveAssetEntry(entry.guid);
            
            assetEntries.Clear();
            settings.GetAllAssets(assetEntries, false, groupFilter: x => x.Default);
            int newCount = assetEntries.Count;
            
            PLog.Info<AddressableBuilderLogger>($"Cleared {oldCount - newCount} assets, remaining {newCount}");
        }
        
        private static bool TryCreateEntry(AddressableAssetSettings settings, AddressableAssetGroup group, string assetGuid, out AddressableAssetEntry assetEntry, params string[] labels)
        {
            if (assetGuid == null)
            {
                assetEntry = null;
                return false;
            }

            if (string.IsNullOrEmpty(assetGuid))
            {
                assetEntry = null;
                return false;
            }

            var entry = settings.CreateOrMoveEntry(assetGuid, group);

            entry.address = AssetDatabase.GUIDToAssetPath(assetGuid);
            foreach (var label in labels)
                entry.labels.Add(label);
            assetEntry = entry;
            return true;
        }

        private static bool TryCreateEntry(AddressableAssetSettings settings, string assetPath, out AddressableAssetEntry assetEntry, params string[] labels)
            => TryCreateEntry(settings, settings.DefaultGroup, assetPath, out assetEntry, labels);
    }
}