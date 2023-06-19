using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Rhinox.AssetProcessor
{
    public class ImportedContentCache
    { 
        private struct ImportedContent
        {
            public string Group;
            public string AssetPath;
            public bool Processed;
        }
        
        private List<ImportedContent> _content;

        public IReadOnlyCollection<string> Assets => _content.Select(x => x.AssetPath).ToArray();
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
                AssetPath = assetPath
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

        public IReadOnlyCollection<string> GetAssets(string groupName, bool excludeProcessed = true)
        {
            var list = new List<string>();
            foreach (var content in _content)
            {
                if (content.Group != groupName)
                    continue;

                if (excludeProcessed && content.Processed)
                    continue;
                
                list.Add(content.AssetPath);
            }
            return list;
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