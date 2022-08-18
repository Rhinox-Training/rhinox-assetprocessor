using System.Collections.Generic;

namespace Rhinox.AssetProcessor.Editor
{
    public interface IProcessor
    {
        bool CanParse(string groupName, string inputPath);

        bool ParseFile(string groupName, string inputPath, string outputFolder, out string[] outputPaths, bool overwrite = false);
    }
}