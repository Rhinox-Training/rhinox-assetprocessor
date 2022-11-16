using System;
using System.Collections;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using Unity.EditorCoroutines.Editor;

namespace Rhinox.AssetProcessor.Editor
{
    public class IncludeStaticAssetFolderJob : BaseContentJob, IContentProcessorJob
    {
        protected ImportedContentCache _importedContent;
        public ImportedContentCache ImportedContent => _importedContent;

        public string AssetFolder { get; }
        public string ExtensionFilter { get; }

        public IncludeStaticAssetFolderJob(string assetFolder, string extensionFilter = "*.*")
        {
            if (assetFolder == null) throw new ArgumentNullException(nameof(assetFolder));
            AssetFolder = assetFolder;
            ExtensionFilter = extensionFilter;
        }

        protected override void OnStart(BaseContentJob parentJob = null)
        {
            var parentContentProcessor = GetParentOfType<IContentProcessorJob>();
            if (parentContentProcessor != null)
                _importedContent = new ImportedContentCache(parentContentProcessor.ImportedContent);
            else
                _importedContent = new ImportedContentCache();
            
            PLog.Info($"Starting IncludeStaticAssetFolderJob for '{AssetFolder}' with extensionFilter '{ExtensionFilter}'");
            EditorCoroutineUtility.StartCoroutineOwnerless(Run());
        }

        private IEnumerator Run()
        {
            try
            {
                string path = AssetFolder;
                if (!Path.IsPathRooted(path))
                    path = Path.GetFullPath(Path.Combine(GlobalData.ProjectPath, path)); // TODO: cache this ProjectPath before, since this theses jobs get called from outside the main Unity thread

                var files = FileHelper.GetFiles(path, ExtensionFilter, SearchOption.AllDirectories);
                files = files.Where(x => x != null && !x.EndsWith(".meta")).ToArray();

                foreach (var file in files)
                {
                    string strippedFilePath = file.Replace(GlobalData.ProjectPath + "\\", "");
                    strippedFilePath = strippedFilePath.Replace(GlobalData.ProjectPath + "/", "");
                    strippedFilePath = strippedFilePath.Replace(GlobalData.ProjectPath, "");
                    // TODO: this copies all prior groups only, this implies the IncludeStaticAssetFolderJob should be last in a contentprocessorpipe
                    var keyCache = _importedContent.Groups.ToArray();
                    foreach (var group in keyCache)
                        _importedContent.Add(group, strippedFilePath);
                }
                PLog.Info($"Finished IncludeStaticAssetFolderJob for '{AssetFolder}' with extensionFilter '{ExtensionFilter}' - {files.Count} files imported");
            }
            catch (Exception e)
            {
                PLog.Error($"Error on IncludeStaticAssets '{AssetFolder}': {e.ToString()}");
            }
            finally
            {
                TriggerCompleted();
            }

            yield break;
        }
    }
}