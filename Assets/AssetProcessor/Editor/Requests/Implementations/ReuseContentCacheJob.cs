namespace Rhinox.AssetProcessor.Editor
{
    public class ReuseContentCacheJob : BaseContentJob, IContentProcessorJob
    {
        private IContentProcessorJob _job;
        
        public ImportedContentCache ImportedContent { get; private set; }

        
        public ReuseContentCacheJob(IContentProcessorJob otherJob)
        {
            _job = otherJob;
        }
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            ImportedContent = new ImportedContentCache(_job.ImportedContent);
            
            TriggerCompleted();
        }
    }
}