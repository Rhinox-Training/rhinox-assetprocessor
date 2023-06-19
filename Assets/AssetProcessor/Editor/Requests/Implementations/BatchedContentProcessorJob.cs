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
    /// <summary>
    ///  Compiles the given assets and transforms them into 1 or more new assets
    ///  Replaces the upper ImportedContentCache with its own serverdata one
    /// </summary>
    public class BatchedContentProcessorJob : BaseChildContentJob<IContentProcessorJob>, IContentProcessorJob
    {
        private readonly AssetProcessor _processor;
        
        protected ImportedContentCache _importedContent;
        public ImportedContentCache ImportedContent => _importedContent;
        
        public string OutputFolder { get; }
        
        private BatchedContentProcessorJob(AssetProcessor assetProcessor, string outputFolder)
        {
            _processor = assetProcessor;
            // Do not copy outer one, this replaces it
            _importedContent = new ImportedContentCache();
            OutputFolder = outputFolder;
        }
        
        public static BatchedContentProcessorJob Create(ICollection<IProcessor> processors, string outputFolder)
        {
            if (outputFolder == null || !outputFolder.StartsWith("Assets")) throw new ArgumentException(nameof(outputFolder));

            var assetProcessor = new AssetProcessor(processors);
            var job = new BatchedContentProcessorJob(assetProcessor, outputFolder);
            return job;
        }

        protected override void OnStartChild(IContentProcessorJob parentJob)
        {
            PLog.Debug($"Fetching ImportedContent from {parentJob.GetType().Name}");
            var importedAssets = parentJob.ImportedContent;
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
            // File copies / processors may be slightly delayed...
            yield return new EditorWaitForSeconds(2.0f);
            
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
            
            yield return new EditorWaitForSeconds(0.5f);

            TriggerCompleted();
        }
    }
}