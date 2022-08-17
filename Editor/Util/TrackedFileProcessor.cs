using System;
using System.Collections;
using System.IO;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using Unity.EditorCoroutines.Editor;

namespace Rhinox.AssetProcessor.Editor
{
    public class TrackedFileProcessor
    {
        public string TargetFolder { get; }
        public string Filter { get; }
        public bool Consume { get; }

        public delegate void FileProcessorEventHandler(TrackedFileProcessor sender, string fullPath);

        public event FileProcessorEventHandler FileProcessTriggered;
        
        private FileSystemWatcher _watcher;
        private EditorCoroutine _coroutine;
        private readonly bool _checkSubdirs;

        public TrackedFileProcessor(string folderToTrack, string extension, bool consume = false, bool checkSubdirs = false)
        {
            if (string.IsNullOrWhiteSpace(folderToTrack))
                throw new ArgumentNullException(nameof(folderToTrack));
            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentNullException(nameof(extension));
                
            TargetFolder = folderToTrack;
            extension = extension.Trim();
            if (extension.StartsWith("*."))
                Filter = extension;
            else if (extension.StartsWith("."))
                Filter = "*" + extension;
            else if (extension[0] == '*' && extension[1] != '.')
                Filter = extension.Replace("*", "*.");
            else
                Filter = extension;
            _checkSubdirs = checkSubdirs;
            Consume = consume;
        }

        public void Enable()
        {
            if (_coroutine != null)
                return;
            
            PLog.Debug($"Starting FileWatcher at '{TargetFolder}' (filter: {Filter})");
            _coroutine = EditorCoroutineUtility.StartCoroutineOwnerless(RunCoroutine());
        }

        public void Disable()
        {
            if (_coroutine == null)
                return;
            EditorCoroutineUtility.StopCoroutine(_coroutine);

            if (_watcher != null)
            {
                _watcher.Created -= OnCreated;
                _watcher.Renamed -= OnRenamed;
                _watcher.Deleted -= OnCreated;
                _watcher.Error -= OnError;

                _watcher = null;
            }

            _coroutine = null;
        }

        private IEnumerator RunCoroutine()
        {
            if (_watcher != null)
                yield break;

            _watcher = new FileSystemWatcher(TargetFolder);
            
            PLog.Debug<FileWatcherLogger>($"Start watch at '{TargetFolder}' with subdirs ({(_checkSubdirs ? "yes" : "no")})");
            _watcher.IncludeSubdirectories = _checkSubdirs;
            
            _watcher.NotifyFilter = NotifyFilters.CreationTime | 
                                    NotifyFilters.DirectoryName | 
                                    NotifyFilters.FileName | 
                                    NotifyFilters.LastAccess |
                                    NotifyFilters.LastWrite | 
                                    NotifyFilters.Size;
            _watcher.Filter = Filter;
            _watcher.Created += OnCreated;
            _watcher.Renamed += OnRenamed;
            _watcher.Deleted += OnDeleted;
            _watcher.Error += OnError;

            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;
            
            yield return null;
        }

        private void OnCreated(object source, FileSystemEventArgs e)
        {
            if (!AllowFileCheckSubDirectory(e.FullPath))
                return;
            TriggerProcess(e.FullPath);
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            if (!AllowFileCheckSubDirectory(e.FullPath))
                return;
            WatcherChangeTypes wct = e.ChangeType;
            Log("File " + e.OldFullPath + " renamed to " + e.FullPath + ": " + wct);
            TriggerProcess(e.FullPath);
        }

        private bool AllowFileCheckSubDirectory(string filePath)
        {
            if (_checkSubdirs)
                return true;
            
            var fi = new FileInfo(filePath);
            string relativePath = FileHelper.GetRelativePath(fi.Directory.FullName, _watcher.Path);
            bool result = relativePath.IsNullOrEmpty();
            if (!result)
                PLog.Trace<FileWatcherLogger>($"Filepath '{filePath}' failed, GetRelativePath({fi.Directory.FullName}, {_watcher.Path}) -> relativePath: {relativePath}");
            return result;
        }

        private void TriggerProcess(string fullPath)
        {
            if (!File.Exists(fullPath))
            {
                Log($"Failed to find '{fullPath}', skipping process...");
                return;
            }

            Log($"Process triggered for '{fullPath}'");
            FileProcessTriggered?.Invoke(this, fullPath);

            if (Consume)
            {
                string fileName = Path.GetFileName(fullPath);
                string containingDirectory = fullPath.Replace(fileName, "");
                string directory = Path.Combine(containingDirectory, "__processed__");
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);
                string targetPath = Path.Combine(directory, fileName);
                if (File.Exists(targetPath))
                    File.Delete(targetPath);
                File.Move(fullPath, targetPath);
                Log($"Consumed file at '{fullPath}' (moved to '{targetPath}')");
            }
        }

        private void OnDeleted(object sender, FileSystemEventArgs e)
        {
            if (!AllowFileCheckSubDirectory(e.FullPath))
                return;
            Log($"File removed '{e.FullPath}'");
        }

        private void OnError(object source, ErrorEventArgs e)
        {
            Log("Error detected: " + e.GetException().GetType());
        }

        private void Log(string logLine)
        {
            PLog.Info<FileWatcherLogger>(logLine);
        }
    }
}