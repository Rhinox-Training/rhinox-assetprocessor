using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Utilities.Editor;
using Rhinox.Perceptor;
using Unity.EditorCoroutines.Editor;
using UnityEngine;
using EditorCoroutine = Unity.EditorCoroutines.Editor.EditorCoroutine;

namespace Rhinox.AssetProcessor.Editor
{
    public class ImportPackageContentJob : BaseContentJob, IContentProcessorJob
    {
        private readonly string _unityPackagePath;
        private readonly string _importTargetDir;
        private readonly string _groupDir;
        private readonly bool _replaceContent;

        public string PackageName => Path.GetFileNameWithoutExtension(_unityPackagePath);
        
        private EditorCoroutine _start;

        protected ImportedContentCache _importedContent;
        public ImportedContentCache ImportedContent => _importedContent;

        public ImportPackageContentJob(string unityPackagePath, string importTargetDir, string group, bool replace = false)
        {
            if (unityPackagePath == null) throw new ArgumentNullException(nameof(unityPackagePath));
            if (importTargetDir == null) throw new ArgumentNullException(nameof(importTargetDir));

            _unityPackagePath = unityPackagePath;
            _importTargetDir = importTargetDir;
            _groupDir = group;
            _replaceContent = replace;
        }

        protected override void OnStart(BaseContentJob parentJob = null)
        {
            var parentContentProcessor = GetParentOfType<IContentProcessorJob>();
            if (parentContentProcessor != null)
            {
                PLog.Debug($"Fetching ImportedContent from {parentContentProcessor.GetType().Name}");
                _importedContent = new ImportedContentCache(parentContentProcessor.ImportedContent);
            }
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
            Log($"Parsing '{_unityPackagePath}'");

            if (_replaceContent)
            {
                var folder = _importTargetDir;
                if (!_groupDir.IsNullOrEmpty())
                    folder = Path.Combine(folder, _groupDir);
                
                Log($"Operation is replace: ensuring folder '{folder}' is clean");

                ClearTargetFolder(folder);
                yield return new EditorWaitForSeconds(2.0f);
            }

            try
            {
                ProcessData(_unityPackagePath);
            }
            catch (Exception e)
            {
                LogError($"Import process {_unityPackagePath} failed and cancelled, reason: {e.ToString()}");
                TriggerCompleted(true, e.Message);
            }
        }

        private void ClearTargetFolder(string folder)
        {
            DirectoryInfo di = new DirectoryInfo(folder);

            if (!di.Exists)
            {
                Log($"Folder '{di.FullName}' does not exist yet... We can continue.");
                return;
            }

            Log($"Clearing folder {di.FullName}");

            foreach (FileInfo file in di.EnumerateFiles())
                file.Delete();

            foreach (DirectoryInfo dir in di.EnumerateDirectories())
                dir.Delete(true);
        }

        private void ProcessData(string packagePath, bool verbose = false)
        {
            string targetDir = Path.Combine(_importTargetDir, _groupDir);
            if (verbose)
            {
                var content = BetterUnityPackageImporter.ListPackageContent(packagePath, mode: BetterUnityPackageImporter.ImportMode.CommonParent);
                int index = 0;
                foreach (var entry in content)
                    Log($"Content of '{packagePath}' [{++index}/{content.Count}]: {entry}");
            }

            Log($"Import Started of: {packagePath}");
            bool result = AssetDatabaseExt.CreateAndRunImportAssetJob(packagePath, targetDir, (importedAssets, failed) =>
            {
                if (failed)
                {
                    TriggerCompleted(true, $"Import failed: {packagePath}");
                }
                else
                {
                    _importedContent.AddRange(_groupDir, importedAssets.ImportedAssets);
                    Log($"Import completed for '{packagePath}' (added: {importedAssets.ImportedAssets.Count} - total asset count: {_importedContent.Count})");
                    TriggerCompleted();
                }
            });

            // NOTE: Job failed
            if (!result)
                TriggerCompleted(true, $"Import failed: {packagePath}");
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