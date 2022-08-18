using System.IO;
using Rhinox.Lightspeed.IO;
using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    public class CopyFileProcessor<T> : BaseFileProcessor<T> where T : UnityEngine.Object
    {
        public override string FolderName { get; }
        
        public CopyFileProcessor(string targetFolderName, params string[] extensions) : base(extensions)
        {
            FolderName = targetFolderName;
        }
        
        protected override string GetOutputFileName(string clientName, string inputPath, T gameObject)
        {
            return Path.GetFileName(inputPath);
        }

        protected override bool OnParseFile(T asset, string inputPath, string outputPath, out string[] additionalPaths)
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
            EnsureDirectoryForFile(outputPath);
            AssetDatabase.CopyAsset(inputPath, outputPath);
            AssetDatabase.Refresh();
            //File.Copy(inputPath, outputPath, true);
            additionalPaths = null;
            return true;
        }
        
        private static void EnsureDirectoryForFile(string filePath)
        {
            string containingFolder = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(containingFolder))
                FileHelper.CreateAssetsDirectory(containingFolder);
        }
    }
}