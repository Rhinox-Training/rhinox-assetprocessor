using Rhinox.Lightspeed.IO;

namespace Rhinox.AssetProcessor.Editor
{
    public static class GlobalData
    {
        public static string ProjectPath { get; private set; }

        public static void Initialize()
        {
            ProjectPath = FileHelper.GetProjectPath();
        }
    }
}