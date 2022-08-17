#if UNITY_ADDRESSABLES
using Rhinox.Perceptor;

namespace Rhinox.AssetProcessor.Editor
{
    public class AddressableBuilderLogger : CustomLogger
    {
        protected override ILogTarget[] GetTargets()
        {
            return new ILogTarget[]
            {
                FileLogTarget.CreateByPath("addressable-builder.log"),
                new UnityLogTarget()
            };
        }
    }
}
#endif