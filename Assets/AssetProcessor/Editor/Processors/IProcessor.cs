using System.Collections.Generic;

namespace Rhinox.AssetProcessor.Editor
{
    public interface IProcessor
    {
        // string FolderName { get; }
        // IReadOnlyCollection<string> Extensions { get; }

        void Load(AssetProcessor manager);

        bool CanParse(string clientName, string inputPath);

        bool ParseFile(string clientName, string inputPath, out string[] outputPaths, bool overwrite = false);
    }
}