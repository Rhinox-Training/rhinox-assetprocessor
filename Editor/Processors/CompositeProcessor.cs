using System.Collections.Generic;
using System.Linq;

namespace Rhinox.AssetProcessor.Editor
{
    public class CompositeProcessor : IProcessor
    {
        private readonly IProcessor[] _childProcessors;
        
        public CompositeProcessor(params IProcessor[] processors)
        {
            _childProcessors = processors.ToArray();
        }
        
        public bool CanParse(string groupName, string inputPath)
        {
            if (_childProcessors != null)
            {
                foreach (var processor in _childProcessors)
                {
                    if (!processor.CanParse(groupName, inputPath))
                        return false;
                }

                return true;
            }

            return false;
        }

        public bool ParseFile(string clientName, string inputPath, string outputFolder, out string[] outputPaths, bool overwrite = false)
        {
            List<string> result = new List<string>();
            foreach (var processor in _childProcessors)
            {
                if (processor.ParseFile(clientName, inputPath, outputFolder, out string[] processedPaths, true))
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