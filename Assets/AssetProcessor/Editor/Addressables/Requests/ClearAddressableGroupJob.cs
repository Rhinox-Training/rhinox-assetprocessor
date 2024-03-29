using UnityEditor;
using UnityEditor.AddressableAssets.Settings;

namespace Rhinox.AssetProcessor.Editor
{
    public class ClearAddressableGroupJob : BaseContentJob
    {
        private AddressableAssetGroup _group;
        
        public ClearAddressableGroupJob(AddressableAssetGroup group)
        {
            _group = group;
        }
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            AssetDatabase.Refresh();

            AddressableContentBuilder.ClearGroup(_group);
            
            TriggerCompleted();
        }
    }
}