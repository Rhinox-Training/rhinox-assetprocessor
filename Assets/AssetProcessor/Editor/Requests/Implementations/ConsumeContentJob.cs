namespace Rhinox.AssetProcessor.Editor
{
    public class ConsumeContentJob : BaseChildContentJob<IContentProcessorJob>
    {
        protected override void OnStartChild(IContentProcessorJob parentJob)
        {
            parentJob.ImportedContent.MarkAllProcessed();
            
            TriggerCompleted();
        }
    }
}