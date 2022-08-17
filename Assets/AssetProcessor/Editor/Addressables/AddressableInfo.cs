using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed.Addressables;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Rhinox.AssetProcessor.Editor
{
    public class AddressableInfoItem
    {
        public string Path;
        public string ClientName;
    }
    
    public static class AddressableInfo
    {
        private class LoadContentCallbackFiltered
        {
            public string Label { get; }
            private Action<ICollection<string>> _callback;
            
            public LoadContentCallbackFiltered(string url, string label, Action<ICollection<string>> callback)
            {
                Addressables.LoadContentCatalogAsync(url).Completed += OnCompleted;
                Label = label;
                _callback = callback;
            }

            private void OnCompleted(AsyncOperationHandle<IResourceLocator> obj)
            {
                if (obj.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("Failed");
                    return;
                }

                if (!obj.Result.Locate(Label, typeof(UnityEngine.Object), out var locations))
                {
                    _callback?.Invoke(Array.Empty<string>());
                    return;
                }
                _callback?.Invoke(locations.Select(x => x.PrimaryKey).ToArray());
            }
        }
        
        private class LoadContentCallback
        {
            public string Label { get; }
            private Action<ICollection<AddressableInfoItem>> _callback;
            
            public LoadContentCallback(string url, Action<ICollection<AddressableInfoItem>> callback)
            {
                Addressables.LoadContentCatalogAsync(url).Completed += OnCompleted;
                _callback = callback;
            }

            private void OnCompleted(AsyncOperationHandle<IResourceLocator> obj)
            {
                if (obj.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("Failed");
                    return;
                }

                var resourceLocator = obj.Result;
                Debug.LogWarning(obj.Result.LocatorId);

                IResourceLocation[] filterSet = Array.Empty<IResourceLocation>();
                if (obj.Result.Locate("ALL", typeof(System.Object), out var locs))
                {
                    filterSet = locs.ToArray();
                    foreach (var entry in filterSet)
                    {
                        
                    }
                }

                foreach (var v in obj.Result.Keys)
                {
                    if (!(v is string vStr))
                        continue;
                    if (Guid.TryParse(vStr, out Guid result))
                        continue; // NO Asset GUIDS

                    if (filterSet.Any(x => x.PrimaryKey.Equals(vStr, StringComparison.InvariantCulture)))
                        continue;
                    Debug.LogError(vStr);
                }

                // if (!obj.Result.Locate(Label, typeof(UnityEngine.Object), out var locations))
                // {
                //     _callback?.Invoke(Array.Empty<string>()); 
                //     return;
                // }
                // _callback?.Invoke(locations.Select(x => x.PrimaryKey).ToArray());
            }
        }
        
        public static void FindAssetPathsRemote(string remoteUri, string label, Action<ICollection<string>> resourceCallback)
        {
            new LoadContentCallbackFiltered(remoteUri, label, resourceCallback);
        }

        public static void FindAssetPathsRemote(string remoteUri, Action<ICollection<AddressableInfoItem>> resourceCallback)
        {
            new LoadContentCallback(remoteUri, resourceCallback);
        }

        private static IEnumerator LoadStatic()
        {
            var handle = AddressablesExt.LoadCatalogLabelsAsync();
            var task = handle.Task;
            while (!task.IsCompleted)
                yield return null;
            Debug.LogWarning("LOADSTATIC LABELS:");
            foreach (var label in handle.Result)
                Debug.LogError($"LABEL: {label}");
        }
        
        public static IReadOnlyCollection<string> GetLoadedResourceKeys(string groupName, string addressableCatalogUrl, string extension = ".prefab")
        {
            if (Addressables.ResourceLocators == null)
                return Array.Empty<string>();

            // TODO: Should we go through remote IP or can we check this data locally?
            var addressableCache = Addressables.ResourceLocators.FirstOrDefault(x =>
                x.LocatorId.Equals(addressableCatalogUrl));

            
            if (addressableCache == null)
                return Array.Empty<string>();


            if (!addressableCache.Locate(groupName, typeof(UnityEngine.Object), out var locations))
                return Array.Empty<string>();
            
            
            List<string> keys = new List<string>();
            foreach (var l in addressableCache.Keys)
            {
                if (l is string s)
                {
                    if (System.Guid.TryParse(s, out System.Guid g))
                        continue;

                    if (extension != null && !s.EndsWith(extension))
                        continue;
                    keys.Add(s);
                }
            }
            return keys;
        }
    }
}