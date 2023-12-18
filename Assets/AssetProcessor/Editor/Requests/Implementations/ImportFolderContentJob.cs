using System.IO;
using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    public class ImportFolderContentJob : BaseContentJob, IContentProcessorJob
    {
        private string _targetFolder;
        private string _group;
        
        protected ImportedContentCache _importedContent;
        public ImportedContentCache ImportedContent => _importedContent;

        public ImportFolderContentJob(string folder, string group)
        {
            _targetFolder = folder;
            _group = group;
        }
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            _importedContent = new ImportedContentCache();
            
            AssetDatabase.Refresh();
            
            var assetGuids = AssetDatabase.FindAssets("", new[] { _targetFolder });

            foreach (var guid in assetGuids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                
                // We don't care about folder assets
                if (Directory.Exists(assetPath))
                    continue;
                
                _importedContent.Add(_group, assetPath);
            }
            
            TriggerCompleted();
        }

    }
}