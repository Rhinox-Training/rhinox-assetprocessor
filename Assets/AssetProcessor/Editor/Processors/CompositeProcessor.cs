using System.Collections.Generic;
using System.Linq;

namespace Rhinox.AssetProcessor.Editor
{
    public class CompositeProcessor : IProcessor
    {
        private readonly IProcessor[] _childProcessors;
        
        public string FolderName { get; }
        public IReadOnlyCollection<string> Extensions { get; }

        public CompositeProcessor(params IProcessor[] processors)
        {
            _childProcessors = processors.ToArray();
        }
        
        public void Load(AssetProcessor manager)
        {
            if (_childProcessors != null)
            {
                foreach (var processor in _childProcessors)
                    processor.Load(manager);
            }
        }

        public bool CanParse(string clientName, string inputPath)
        {
            if (_childProcessors != null)
            {
                foreach (var processor in _childProcessors)
                {
                    if (!processor.CanParse(clientName, inputPath))
                        return false;
                }

                return true;
            }

            return false;
        }

        public bool ParseFile(string clientName, string inputPath, out string[] outputPaths, bool overwrite = false)
        {
            List<string> result = new List<string>();
            foreach (var processor in _childProcessors)
            {
                if (processor.ParseFile(clientName, inputPath, out string[] processedPaths, true))
                {
                    result.AddRange(processedPaths);
                }
                else
                {
                    outputPaths = result.ToArray();
                    return false;
                }
            }

            outputPaths = result.ToArray();
            return true;
        }
    }
}