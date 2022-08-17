using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Rhinox.Lightspeed.IO;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.AssetProcessor.Editor
{
    public class ProcessedAssetInfo
    {
        private Dictionary<string, string> _dict;

        public ProcessedAssetInfo()
        {
            _dict = new Dictionary<string, string>();
        }

        public void AddOrReplace(string inputPath, string outputPath)
        {
            
        }

        public void ToFile(string serializedPath)
        {
            var fileContent = new List<string>();
            foreach (var entry in _dict)
            {
                string line = $"\"{entry.Key}\"=>\"{entry.Value}\"";
                fileContent.Add(line);
            }
            
            File.WriteAllLines(serializedPath, fileContent);
        }

        public static ProcessedAssetInfo FromFile(string serializedPath)
        {
            var pai = new ProcessedAssetInfo();
            if (!File.Exists(serializedPath))
                return pai;
            var lines = File.ReadAllLines(serializedPath);
            const string searchStr = "\"([0-9aA-zZ.,\\/ :\\-_]+)\"=>\"([0-9aA-zZ.,\\/ :\\-_]+)\"";
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var m = Regex.Match(line, searchStr);
                if (!m.Success)
                    continue;
                string from = m.Groups[1].Value;
                string to = m.Groups[2].Value;
                pai.AddOrReplace(from, to);
            }

            return pai;
        }
        
    }
    
    public class AssetProcessor
    {
        public string InputPath { get; }
        public string OutputPath { get; }

        private List<IProcessor> _processors;
        
        private const string PREFAB_FOLDER_NAME = "Prefabs";
        private const string MATERIAL_FOLDER_NAME = "Materials";
        private const string TEXTURE_FOLDER_NAME = "Materials/Textures";
        private const string FLOOR_MATERIAL_FOLDER_NAME = "Materials/Floors";
        private const string FLOOR_TEXTURE_FOLDER_NAME = "Materials/Floors/Textures";

        private string PROCESSED_ASSET_INFO_PATH => FileHelper.GetFullPath("../processed_file.data", FileHelper.GetProjectPath());
        private ProcessedAssetInfo _processedAssetInfo;
        
        public AssetProcessor(string inputPath, string outputPath)
        {
            if (inputPath == null || !inputPath.StartsWith("Assets")) throw new ArgumentException(nameof(inputPath));
            if (outputPath == null || !outputPath.StartsWith("Assets")) throw new ArgumentException(nameof(outputPath));
            InputPath = inputPath;
            OutputPath = outputPath;

            _processedAssetInfo = ProcessedAssetInfo.FromFile(PROCESSED_ASSET_INFO_PATH);
            BaseFileProcessor.GlobalProcessed += OnProcessed;

            _processors = new List<IProcessor>();
        }

        public bool Initialize()
        {
            if (_processors == null)
                return false;
            
            foreach (var processor in _processors)
                processor.Load(this);
            return true;
        }

        private void OnProcessed(IProcessor sender, string inputpath, string outputpath)
        {
            _processedAssetInfo.AddOrReplace(inputpath, outputpath);
            _processedAssetInfo.ToFile(PROCESSED_ASSET_INFO_PATH);
        }

        public string[] ProcessAsset(string clientName, string inputPath)
        {
            string[] result = null;
            foreach (var processor in _processors)
            {
                if (!processor.CanParse(clientName, inputPath))
                    continue;
                
                if (!processor.ParseFile(clientName, inputPath, out string[] processedPaths, true))
                    PLog.Error($"Something went wrong during parsing of {clientName} - {inputPath} (processor: {processor.GetType().Name})");

                result = processedPaths;
                break;
            }

            return result ?? Array.Empty<string>();
        }
        
        public void Clear()
        {
            PLog.Info($"Clearing folder {OutputPath}");
#if UNITY_EDITOR
            // Remove dir (recursively)
            var fullPath = FileHelper.GetFullPath(OutputPath, GlobalData.ProjectPath);
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

        private static string GetFolderMetaPath(string folderPath)
        {
            folderPath = folderPath.Trim();
            if (folderPath.EndsWith("/") || folderPath.EndsWith("\\"))
                return folderPath.Substring(0, folderPath.Length - 1) + ".meta";
            return folderPath + ".meta";
        }
    }
}