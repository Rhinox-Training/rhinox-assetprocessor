using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    public class ClearAddressablesJob : BaseContentJob
    {
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            AssetDatabase.Refresh();
            
            AddressableContentBuilder.ClearAddressables();
            
            TriggerCompleted();
        }
    }
}