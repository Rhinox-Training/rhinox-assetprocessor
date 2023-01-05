using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Rhinox.Lightspeed.IO;
using Rhinox.Utilities.Editor;
using Rhinox.Perceptor;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using EditorCoroutine = Unity.EditorCoroutines.Editor.EditorCoroutine;

namespace Rhinox.AssetProcessor.Editor
{
    public class AddOrReplaceContentJob : BaseContentJob, IContentProcessorJob
    {
        private readonly string _newAssetsPath;
        private readonly string _importTargetDir;
        private readonly bool _replaceContent;
        
        private EditorCoroutine _start;
        private List<string> _clientImportedFolders;
        private IReadOnlyCollection<DirectoryInfo> _clientImportFolders;

        protected ImportedContentCache _importedContent;
        public ImportedContentCache ImportedContent => _importedContent;

        public AddOrReplaceContentJob(string assetsSourceDir, string importTargetDir, bool replace = false)
        {
            if (assetsSourceDir == null) throw new ArgumentNullException(nameof(assetsSourceDir));
            if (importTargetDir == null) throw new ArgumentNullException(nameof(importTargetDir));
            
            _newAssetsPath = assetsSourceDir;
            _importTargetDir = importTargetDir;
            _replaceContent = replace;
        }

        protected override void OnStart(BaseContentJob parentJob = null)
        {
            var parentContentProcessor = GetParentOfType<IContentProcessorJob>();
            if (parentContentProcessor != null)
                _importedContent = new ImportedContentCache(parentContentProcessor.ImportedContent);
            else
                _importedContent = new ImportedContentCache();
            
            _start = EditorCoroutineUtility.StartCoroutineOwnerless(ProcessRequest());

            AssetDatabaseExt.JobStarted += OnImportJobStarted;
            AssetDatabaseExt.JobFinished += OnImportJobFinished;
        }

        protected override void OnCompleted(bool failed = false, string errorString = "")
        {
            base.OnCompleted(failed, errorString);
            
            AssetDatabaseExt.JobFinished -= OnImportJobFinished;
            AssetDatabaseExt.JobStarted -= OnImportJobStarted;
            
            if (_start != null)
                EditorCoroutineUtility.StopCoroutine(_start);
        }

        private IEnumerator ProcessRequest()
        {
            _clientImportFolders = GetClientImportFolders();
            if (_clientImportFolders.Count == 0)
            {
                TriggerCompleted();
                yield break;
            }

            _clientImportedFolders = new List<string>();
            foreach (var clientImportFolder in _clientImportFolders)
            {
                string clientName = clientImportFolder.Name.ToUpperInvariant();
                
                Log($"Parsing Client '{clientName}'");

                if (_replaceContent)
                {
                    ClearTargetFolder(clientName); // TODO: should we support clearing the full folder structure as well, or only on a per customer basis for the requests?
                    yield return new WaitForSeconds(2.0f);
                }

                try
                {
                    ProcessClientData(clientName, clientImportFolder, _clientImportedFolders);
                }
                catch (Exception e)
                {
                    LogError($"Import process {clientName} failed and cancelled, reason: {e.ToString()}");
                    TriggerCompleted(); // TODO: mark failed
                    yield break;
                }

                yield return new WaitForSeconds(1.0f);
            }
        }

        public override void Update()
        {
            base.Update();
            if (_clientImportFolders != null && _clientImportedFolders != null)
            {
                bool containEqual = true;
                foreach (var target in _clientImportFolders)
                {
                    string clientName = target.Name.ToUpperInvariant();
                    if (!_clientImportedFolders.Contains(clientName))
                        containEqual = false;
                }
                
                if (containEqual)
                    TriggerCompleted();
            }
        }

        private IReadOnlyCollection<DirectoryInfo> GetClientImportFolders()
        {
            DirectoryInfo di = new DirectoryInfo(_newAssetsPath);
            if (!di.Exists)
            {
                PLog.Error($"Directory '{_newAssetsPath}' does not exist, exiting...");
                return Array.Empty<DirectoryInfo>();
            }

            var subDirectories = FileHelper.GetChildFolders(_newAssetsPath);
            if (subDirectories.Count == 0)
            {
                PLog.Warn($"Directory '{_newAssetsPath}' was empty (need folders by client name), exiting...");
                return Array.Empty<DirectoryInfo>();
            }

            return subDirectories;
        }

        private void ClearTargetFolder(string clientName = null)
        {
            DirectoryInfo di = string.IsNullOrWhiteSpace(clientName)
                ? new DirectoryInfo(_importTargetDir)
                : new DirectoryInfo(Path.Combine(_importTargetDir, clientName));
            if (!di.Exists)
                return;

            Log($"Clearing folder {di.FullName}");
            
            foreach (FileInfo file in di.EnumerateFiles())
                file.Delete();
            
            foreach (DirectoryInfo dir in di.EnumerateDirectories())
                dir.Delete(true); 
        }
        
        private void ProcessClientData(string clientName, DirectoryInfo dir, List<string> clientImportTargets, bool verbose = false)
        {
            // TODO: Parse FBX vs UnityPackage?
            string targetDir = Path.Combine(_importTargetDir, clientName);
            var files = FileHelper.GetFiles(dir.FullName, "*.*", SearchOption.AllDirectories);

            if (verbose)
            {
                foreach (var file in files)
                {
                    var content = BetterUnityPackageImporter.ListPackageContent(file,
                        mode: BetterUnityPackageImporter.ImportMode.CommonParent);
                    int index = 0;
                    foreach (var entry in content)
                        Log($"Content of '{file}' [{++index}/{content.Count}]: {entry}");
                }
            }

            Log($"Import Started of: {string.Join(", ", files)}");
            bool result = AssetDatabaseExt.ImportAssets(files, targetDir, (importedAssets) =>
            {
                _importedContent.AddRange(clientName, importedAssets.ImportedAssets);
                clientImportTargets?.Add(clientName);
                Log($"Import completed for '{clientName}' (added: {importedAssets.ImportedAssets.Count} - total asset count: {_importedContent.Count})");
            });

            // NOTE: Job failed
            if (!result)
                TriggerCompleted(true, $"Import failed: {string.Join(", ", files)}");
        }

        // Logging
        //==============================================================================================================
        private void OnImportJobFinished(IImportJob job)
        {
            Log($"Job '{job.Name}' completed...");
        }

        private void OnImportJobStarted(IImportJob job)
        {
            Log($"Job '{job.Name}' started...");
        }
    }
}