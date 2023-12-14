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
                    foreach (var importedGuid in importedAssets.GetAssetGuids(client))
                    {
                        var path = AssetDatabase.GUIDToAssetPath(importedGuid);
                        var processedPaths = _processor.ProcessAsset(client, path, OutputFolder); // NOTE: Synchronous
                        foreach (var processedPath in processedPaths)
                        {
                            if (!string.IsNullOrWhiteSpace(processedPath))
                                _importedContent.Add(client, processedPath);
                            processedAssets.Add(processedPath);
                        }

                        Log($"Job '{this}' progress [{++count}/{importedAssets.Count}]: {client}#{path} -> '{string.Join(", ", processedPaths)}'");
                    }
                }
            }
            else
                Log($"Job '{this}': Processed 0 assets");

            // Ensure everything is saved...
            AssetDatabase.SaveAssets();
            
            AssetDatabase.Refresh();
            
            TriggerCompleted();
        }
    }
}