using Rhinox.Perceptor;
using UnityEditor;
using UnityEditor.Compilation;

namespace Rhinox.AssetProcessor.Editor
{
    public class SwitchBuildTargetJob : BaseContentJob
    {
        private readonly BuildTarget _targetPlatform;

        public SwitchBuildTargetJob(BuildTarget buildTarget)
        {
            _targetPlatform = buildTarget;
        }
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            var group = BuildPipeline.GetBuildTargetGroup(_targetPlatform);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, _targetPlatform))
            {
                TriggerCompleted(true, $"Could not switch to platform {_targetPlatform}");
            }

            if (EditorApplication.isCompiling)
            {
                PLog.Info("Delaying until compilation is finished...");
                CompilationPipeline.compilationFinished += OnCompilationFinished;
            }
            else
            {
                PLog.Warn("Switched without compilation time...");
                TriggerCompleted();
            }
                
        }

        private void OnCompilationFinished(object obj)
        {
            CompilationPipeline.compilationFinished -= OnCompilationFinished;
            
            TriggerCompleted();
        }
    }
}