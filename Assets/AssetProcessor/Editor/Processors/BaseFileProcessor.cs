using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Rhinox.Lightspeed;
using Rhinox.Lightspeed.IO;
using Rhinox.Lightspeed.Reflection;
using Rhinox.Perceptor;
using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    public abstract class BaseFileProcessor : IProcessor
    {
        public abstract string FolderName { get; }
        public IReadOnlyCollection<string> Extensions { get; }

        public delegate void ProcessEventHandler(IProcessor sender, string inputPath, string outputPath);
        public static event ProcessEventHandler GlobalProcessed;
        
        protected BaseFileProcessor(params string[] extensions)
        {
            if (extensions == null || extensions.Length == 0) throw new ArgumentNullException(nameof(extensions));

            Extensions = extensions.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x =>
            {
                if (x.StartsWith("."))
                    return x;
                else if (x.StartsWith("*."))
                    return x.Replace("*.", ".");
                else
                    return "." + x;
            }).ToArray();
        }
        
        public virtual bool CanParse(string groupName, string inputPath)
        {
            return MatchesExtensions(inputPath);
        }

        public abstract bool ParseFile(string groupName, string inputPath, string outputFolder, out string[] outputPaths, bool overwrite = false);

        protected virtual void TriggerProcessed(string inputPath, string outputPath)
        {
            GlobalProcessed?.Invoke(this, inputPath, outputPath);
        }

        protected bool MatchesExtensions(string path)
        {
            foreach (var ext in Extensions)
            {
                if (path.HasExtension(ext))
                    return true;
            }
            return false;
        }
    }


    public abstract class BaseFileProcessor<T> : BaseFileProcessor where T : UnityEngine.Object
    {
        protected BaseFileProcessor(params string[] extensions) 
            : base(extensions)
        {
        }

        public override bool CanParse(string groupName, string inputPath)
        {
            if (!base.CanParse(groupName, inputPath))
                return false;

            if (string.IsNullOrWhiteSpace(AssetDatabase.AssetPathToGUID(inputPath)))
                return false;

            return AssetDatabase.GetMainAssetTypeAtPath(inputPath).InheritsFrom(typeof(T));
        }

        protected virtual bool CanOverwrite(string outputPath, bool overwrite)
        {
            return !FileHelper.AssetExists(outputPath) || overwrite;
        }

        public override bool ParseFile(string groupName, string inputPath, string outputFolder, out string[] outputPaths, bool overwrite = false)
        {
            PLog.Info($"Parsing File '{inputPath}' with processor: {GetType().Name}");
            
            var objAsset = (T)AssetDatabase.LoadAssetAtPath(inputPath, typeof(T));

            if (objAsset == null)
            {
                PLog.Warn("Parse failed: Could not load asset");
                outputPaths = null;
                return false;
            }

            if (!ValidateInput(inputPath, objAsset))
            {
                PLog.Warn("Parse failed: Invalid asset");
                outputPaths = null;
                return false;
            }

            string outputFileName = GetOutputFileName(groupName, inputPath, objAsset);
            
            
            var outputPath = Path.Combine(outputFolder, $"{groupName}/{FolderName}/", outputFileName);

            if (!CanOverwrite(outputPath, overwrite))
            {
                outputPaths = null;
                return false;
            }

            PreprocessFile(inputPath);
            
            FileHelper.CreateAssetsDirectory(outputFolder);
            if (OnParseFile(objAsset, inputPath, outputPath, out string[] additionalPaths))
            {
                TriggerProcessed(inputPath, outputPath);
                List<string> result = new List<string>() {outputPath};
                if (additionalPaths != null)
                {
                    foreach (var p in additionalPaths)
                        result.AddUnique(p);
                }
                outputPaths = result.ToArray();
                return true;
            }

            outputPaths = null;
            return false;
        }

        protected virtual bool ValidateInput(string inputPath, T objAsset)
        {
            return true;
        }

        protected abstract string GetOutputFileName(string clientName, string inputPath, T gameObject);

        protected abstract bool OnParseFile(T asset, string inputPath, string outputPath, out string[] additionalPaths);

        protected virtual void PreprocessFile(string inputPath)
        {
            
        }
    }
}