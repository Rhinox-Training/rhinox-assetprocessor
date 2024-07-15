using System.IO;
using Rhinox.Lightspeed.IO;
using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    public class MoveFileProcessor<T> : BaseFileProcessor<T> where T : UnityEngine.Object
    {
        public override string FolderName { get; }
        
        public MoveFileProcessor(string targetFolderName, params string[] extensions) : base(extensions)
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
            FileHelper.EnsureDirectoryForFile(outputPath);
            AssetDatabase.MoveAsset(inputPath, outputPath);
            AssetDatabase.Refresh();
            //File.Copy(inputPath, outputPath, true);
            additionalPaths = null;
            return true;
        }
    }
}