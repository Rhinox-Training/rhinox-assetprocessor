using UnityEngine;

namespace Rhinox.AssetProcessor.Editor
{
    public class ConsumeContentJob : BaseChildContentJob<IContentProcessorJob>
    {
        
        protected override void OnStartChild(IContentProcessorJob parentJob)
        {
            var content = parentJob.ImportedContent;
            if (content != null)
            {
                var count = content.GetUnprocessedCount();
                Debug.Log($"Marking All ({count}) Asssets as processed.");
                content.MarkAllProcessed();
            }
            
            TriggerCompleted();
        }
    }
}