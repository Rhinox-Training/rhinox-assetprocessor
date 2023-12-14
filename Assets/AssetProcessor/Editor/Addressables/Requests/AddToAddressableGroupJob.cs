using UnityEditor.AddressableAssets.Settings;

namespace Rhinox.AssetProcessor.Editor
{
    public class AddToAddressableGroupJob : BaseChildContentJob<IContentProcessorJob>
    {
        private AddressableAssetGroup _targetGroup;
        private string[] _tags;
        
        public AddToAddressableGroupJob(AddressableAssetGroup group, params string[] tags)
            : this(tags)
        {
            _targetGroup = group;
        }
        
        public AddToAddressableGroupJob(params string[] tags)
        {
            _tags = tags;
        }

        protected override void OnStartChild(IContentProcessorJob parentJob)
        {
            var guids = parentJob.ImportedContent.GetAllAssetGuids();

            AddressableContentBuilder.AddAssets(_targetGroup, guids, _tags);
            
            TriggerCompleted();
        }
    }
}