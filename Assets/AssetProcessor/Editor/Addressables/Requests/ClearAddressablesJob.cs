namespace Rhinox.AssetProcessor.Editor
{
    public class ClearAddressablesJob : BaseContentJob
    {
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            AddressableContentBuilder.ClearAddressables();
            
            TriggerCompleted();
        }
    }
}