using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private List<IProcessor> _processors;

        private string PROCESSED_ASSET_INFO_PATH => FileHelper.GetFullPath("../processed_file.data", GlobalData.ProjectPath);
        private ProcessedAssetInfo _processedAssetInfo;
        
        public AssetProcessor(ICollection<IProcessor> processors)
        {
            _processors = processors.ToList();
            _processedAssetInfo = ProcessedAssetInfo.FromFile(PROCESSED_ASSET_INFO_PATH);
            BaseFileProcessor.GlobalProcessed += OnProcessed;
        }
        
        public AssetProcessor(params IProcessor[] processors)
        {
            _processors = processors.ToList();
            _processedAssetInfo = ProcessedAssetInfo.FromFile(PROCESSED_ASSET_INFO_PATH);
            BaseFileProcessor.GlobalProcessed += OnProcessed;
        }

        private void OnProcessed(IProcessor sender, string inputpath, string outputpath)
        {
            _processedAssetInfo.AddOrReplace(inputpath, outputpath);
            _processedAssetInfo.ToFile(PROCESSED_ASSET_INFO_PATH);
        }

        public string[] ProcessAsset(string clientName, string inputPath, string outputFolder)
        {
            string[] result = null;
            foreach (var processor in _processors)
            {
                if (!processor.CanParse(clientName, inputPath))
                    continue;

                var timer = new Stopwatch();
                timer.Start();
                
                if (!processor.ParseFile(clientName, inputPath, outputFolder, out string[] processedPaths, true))
                    PLog.Error($"Something went wrong during parsing of {clientName} - {inputPath} (processor: {processor.GetType().Name})");

                timer.Stop();
                PLog.Debug($"Time taken to process asset: {timer.ElapsedMilliseconds}ms");
                
                result = processedPaths;
                break;
            }

            return result ?? Array.Empty<string>();
        }
    }
}