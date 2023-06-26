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
            if (EditorUserBuildSettings.activeBuildTarget == _targetPlatform)
            {
                TriggerCompleted();
                return;
            }
        
            var group = BuildPipeline.GetBuildTargetGroup(_targetPlatform);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, _targetPlatform))
            {
                TriggerCompleted(true, $"Could not switch to platform {_targetPlatform}");
            }

            if (EditorApplication.isCompiling)
            {
                PLog.Info("Delaying until compilation is finished...");
                // Note: this never finished; most likely state is cleared
                // CompilationPipeline.compilationFinished += OnCompilationFinished;
            }
            else
            {
                PLog.Warn("Switched without compilation time...");
                TriggerCompleted();
            }
                
        }

        public override void Update()
        {
            if (EditorApplication.isCompiling)
                return;

            TriggerCompleted();
        }

        // private void OnCompilationFinished(object obj)
        // {
        //     CompilationPipeline.compilationFinished -= OnCompilationFinished;
        //     
        //     TriggerCompleted();
        // }
    }
}