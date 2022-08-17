using Rhinox.Perceptor;

namespace Rhinox.AssetProcessor.Editor
{
    public class FileWatcherLogger : CustomLogger
    {
        protected override ILogTarget[] GetTargets()
        {
            return new ILogTarget[]
            {
                FileLogTarget.CreateByPath("filewatcher.log"),
                new UnityLogTarget()
            };
        }
    }
}