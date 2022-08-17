using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rhinox.AssetProcessor
{
    public class ImportedContentCache
    {
        public Dictionary<string, List<string>> _map;

        public IReadOnlyCollection<string> Assets => _map.Values.SelectMany(x => x).ToArray();
        public int Count => Assets.Count;
        public IReadOnlyCollection<string> Groups => _map.Keys;

        public ImportedContentCache()
        {
            _map = new Dictionary<string, List<string>>();
        }

        public ImportedContentCache(ImportedContentCache other)
        {
            _map = other._map.ToDictionary(x => x.Key, x => x.Value);
        }

        public bool Add(string groupName, string assetPath)
        {
            if (groupName == null || string.IsNullOrWhiteSpace(assetPath))
                return false;
            
            if (!_map.ContainsKey(groupName))
                _map.Add(groupName, new List<string>());

            var list = _map[groupName];
            if (!list.Contains(assetPath))
                list.Add(assetPath);
            _map[groupName] = list;
            return true;
        }

        public bool AddRange(string groupName, ICollection<string> assetPaths)
        {
            if (groupName == null || assetPaths == null)
                return false;
            
            if (!_map.ContainsKey(groupName))
                _map.Add(groupName, new List<string>());
            
            var list = _map[groupName];
            foreach (var assetPath in assetPaths)
            {
                if (string.IsNullOrWhiteSpace(assetPath))
                    continue;
                
                if (!list.Contains(assetPath))
                    list.Add(assetPath);
            }
            _map[groupName] = list;
            return true;
        }
        
        public bool AddRange(string groupName, IReadOnlyCollection<string> assetPaths)
        {
            if (groupName == null || assetPaths == null)
                return false;
            
            if (!_map.ContainsKey(groupName))
                _map.Add(groupName, new List<string>());
            
            var list = _map[groupName];
            foreach (var assetPath in assetPaths)
            {
                if (string.IsNullOrWhiteSpace(assetPath))
                    continue;
                
                if (!list.Contains(assetPath))
                    list.Add(assetPath);
            }
            _map[groupName] = list;
            return true;
        }

        public IReadOnlyCollection<string> GetAssets(string groupName)
        {
            if (!_map.ContainsKey(groupName))
                return Array.Empty<string>();
            return _map[groupName];
        }
    }
}