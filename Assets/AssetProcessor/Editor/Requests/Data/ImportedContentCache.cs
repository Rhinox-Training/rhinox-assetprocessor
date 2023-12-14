using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEditor;

namespace Rhinox.AssetProcessor
{
    public class ImportedContentCache
    {
        public enum Filter
        {
            ExcludeProcessed,
            OnlyProcessed,
            All
        }
        
        private class ImportedContent
        {
            public string Group;
            public string Guid;
            public string Name;
            public bool Processed;
        }
        
        private List<ImportedContent> _content;

        public IReadOnlyCollection<string> Assets => _content.Select(x => AssetDatabase.GUIDToAssetPath(x.Guid)).ToArray();
        public int Count => _content.Count;
        public IReadOnlyCollection<string> Groups => _content.Select(x => x.Group).Distinct().ToArray();

        public ImportedContentCache()
        {
            _content = new List<ImportedContent>();
        }

        public ImportedContentCache(ImportedContentCache other)
        {
            _content = other._content.ToList();
        }

        public bool Add(string groupName, string assetPath)
        {
            if (groupName == null || string.IsNullOrWhiteSpace(assetPath))
                return false;
            
            // TODO: this was checked for duplicates... Is this needed?
            _content.Add(new ImportedContent
            {
                Group = groupName,
                Guid = AssetDatabase.AssetPathToGUID(assetPath),
                Name = Path.GetFileName(assetPath)
            });
            
            return true;
        }

        public bool AddRange(string groupName, ICollection<string> assetPaths)
        {
            if (groupName == null || assetPaths == null)
                return false;

            foreach (var path in assetPaths)
                Add(groupName, path);
            
            return true;
        }
        
        public bool AddRange(string groupName, IReadOnlyCollection<string> assetPaths)
        {
            if (groupName == null || assetPaths == null)
                return false;
            
            foreach (var path in assetPaths)
                Add(groupName, path);
            
            return true;
        }

        public void MarkAllProcessed()
        {
            for (var i = 0; i < _content.Count; i++)
            {
                var content = _content[i];
                content.Processed = true;
                _content[i] = content;
            }
        }
        
        public bool MarkProcessed(string assetPath)
        {
            var guid = AssetDatabase.AssetPathToGUID(assetPath);

            if (guid.IsNullOrEmpty())
                return false;
            
            for (var i = 0; i < _content.Count; i++)
            {
                if (_content[i].Guid != guid)
                    continue;
                
                var content = _content[i];
                content.Processed = true;
                _content[i] = content;
                return true;
            }

            return false;
        }
        
        public IReadOnlyCollection<string> GetAllAssetPaths(Filter filter = Filter.ExcludeProcessed)
        {
            var list = new List<string>();
            foreach (var content in _content)
            {
                if (!IsValidForFilter(filter, content))
                    continue;

                list.Add(AssetDatabase.GUIDToAssetPath(content.Guid));
            }
            return list;
        }

        public IReadOnlyCollection<string> GetAllAssetGuids(Filter filter = Filter.ExcludeProcessed)
        {
            var list = new List<string>();
            foreach (var content in _content)
            {
                if (!IsValidForFilter(filter, content))
                    continue;
                
                list.Add(content.Guid);
            }
            return list;
        }
        
        public IReadOnlyCollection<string> GetAssetGuids(string groupName, Filter filter = Filter.ExcludeProcessed)
        {
            var list = new List<string>();
            foreach (var content in _content)
            {
                if (content.Group != groupName)
                    continue;

                if (!IsValidForFilter(filter, content))
                    continue;
                
                list.Add(content.Guid);
            }
            return list;
        }
        
        public IReadOnlyCollection<string> GetAssetNames(string groupName, Filter filter = Filter.ExcludeProcessed)
        {
            var list = new List<string>();
            foreach (var content in _content)
            {
                if (content.Group != groupName)
                    continue;

                if (!IsValidForFilter(filter, content))
                    continue;
                
                list.Add(content.Name);
            }
            return list;
        }

        private static bool IsValidForFilter(Filter filter, ImportedContent content)
        {
            if (filter == Filter.ExcludeProcessed && content.Processed)
                return false;
            if (filter == Filter.OnlyProcessed && !content.Processed)
                return false;
            return true;
        }

        public int GetUnprocessedCount()
        {
            int count = 0;
            
            foreach (var content in _content)
            {
                if (!content.Processed)
                    count++;
            }

            return count;
        }
    }
}