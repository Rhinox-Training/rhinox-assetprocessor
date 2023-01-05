namespace Rhinox.AssetProcessor.Editor
{
    public abstract class BaseChildContentJob<T> : BaseContentJob where T : class
    {
        protected bool _shouldFailOnParentNotfound = false; 
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            var contentDeployParent = GetParentOfType<T>();
            if (contentDeployParent == null)
            {
                string errorText = $"Job '{this}': Nothing to process no {typeof(T).Name} found in parents";
                LogError(errorText);
                TriggerCompleted(_shouldFailOnParentNotfound, errorText);
                return;
            }
            
            OnStartChild(contentDeployParent);
        }

        protected abstract void OnStartChild(T parentJob);
    }
}