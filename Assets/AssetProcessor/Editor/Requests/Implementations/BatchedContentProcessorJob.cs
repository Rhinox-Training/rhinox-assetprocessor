using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using Unity.EditorCoroutines.Editor;
using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    public class BatchedContentProcessorJob : BaseContentJob, IContentProcessorJob
    {
        private readonly AssetProcessor _processor;
        private readonly bool _clear;
        
        protected ImportedContentCache _importedContent;
        public ImportedContentCache ImportedContent => _importedContent;
        
        public string OutputFolder { get; }
        
        private BatchedContentProcessorJob(AssetProcessor assetProcessor, string outputFolder)
        {
            _processor = assetProcessor;
            _importedContent = new ImportedContentCache();
            OutputFolder = outputFolder;
        }

        public static BatchedContentProcessorJob Create(ICollection<IProcessor> processors, string outputFolder, bool clear = false)
        {
            if (outputFolder == null || !outputFolder.StartsWith("Assets")) throw new ArgumentException(nameof(outputFolder));

            var assetProcessor = new AssetProcessor(processors);
            var job = new BatchedContentProcessorJob(assetProcessor, outputFolder);
            if (clear)
                job.ClearOutputFolder();
            return job;
        }
        
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            var contentProcessorParent = GetParentOfType<IContentProcessorJob>();
            if (contentProcessorParent == null)
            {
                Log($"Job '{this}': Nothing to process no IContentProcessorJob found in parents");
                TriggerCompleted();
                return;
            }
            var importedAssets = contentProcessorParent.ImportedContent;
            var processedAssets = new List<string>();
            if (importedAssets != null)
            {
                int count = 0;
                foreach (var client in importedAssets.Groups)
                {
                    foreach (var importedAsset in importedAssets.GetAssets(client))
                    {
                        var processedPaths = _processor.ProcessAsset(client, importedAsset, OutputFolder); // NOTE: Synchronous
                        foreach (var processedPath in processedPaths)
                        {
                            if (!string.IsNullOrWhiteSpace(processedPath))
                                _importedContent.Add(client, processedPath);
                            processedAssets.Add(processedPath);
                        }

                        Log($"Job '{this}' progress [{++count}/{importedAssets.Count}]: {client}#{importedAsset} -> '{string.Join(", ", processedPaths)}'");
                    }
                }
            }
            else
                Log($"Job '{this}': Processed 0 assets");

            EditorCoroutineUtility.StartCoroutineOwnerless(CreateMetaFilesAndFinish(processedAssets));
        }

        private IEnumerator CreateMetaFilesAndFinish(ICollection<string> processedAssets)
        {
            if (processedAssets != null && processedAssets.Count > 0)
                AssetDatabase.ForceReserializeAssets(processedAssets, ForceReserializeAssetsOptions.ReserializeMetadata);
            const int count = 10;
            for (int i = 0; i < count; ++i)
            {
                yield return new EditorWaitForSeconds(0.5f);
                AssetDatabase.SaveAssets();
                yield return new EditorWaitForSeconds(0.5f);
                AssetDatabase.Refresh();
            }
            AssetDatabase.Refresh();
            TriggerCompleted();
        }
        
        private void ClearOutputFolder()
        {
            PLog.Info($"Clearing folder {OutputFolder}");
#if UNITY_EDITOR
            // Remove dir (recursively)
            var fullPath = FileHelper.GetFullPath(OutputFolder, GlobalData.ProjectPath);
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
                var metaPath = GetFolderMetaPath(fullPath);
                if (File.Exists(metaPath))
                    File.Delete(metaPath);
            }
#else
            FileHelper.ClearAssetDirectory(OutputPath);
#endif
        }

        private string GetFolderMetaPath(string folderPath)
        {
            folderPath = folderPath.Trim();
            if (folderPath.EndsWith("/") || folderPath.EndsWith("\\"))
                return folderPath.Substring(0, folderPath.Length - 1) + ".meta";
            return folderPath + ".meta";
        }
    }
}