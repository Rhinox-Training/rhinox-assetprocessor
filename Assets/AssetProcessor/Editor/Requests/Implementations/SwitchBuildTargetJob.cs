using Rhinox.Perceptor;
using UnityEditor;

namespace Rhinox.AssetProcessor.Editor
{
    /// <summary>
    /// NOTE: EditorUserBuildSettings.SwitchActiveBuildTarget is not available when running the Editor in batch mode.
    /// This is because changing the build target requires recompiling script code for the given target which cannot
    /// be done while script code is executing (not a problem in in the editor as the operation is simply deferred
    /// but batch mode will immediately exit after having executed the designated script code).
    /// To set the build target to use in batch mode, use the buildTarget command-line switch.
    /// </summary>
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
            base.Update();
            
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