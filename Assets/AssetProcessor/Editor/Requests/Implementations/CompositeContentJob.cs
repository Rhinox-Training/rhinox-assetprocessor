using System;
using System.Collections.Generic;
using System.Linq;
using Rhinox.Lightspeed;
using UnityEditor.VersionControl;

namespace Rhinox.AssetProcessor.Editor
{
    public class CompositeContentJob : BaseContentJob
    {
        private BaseContentJob[] _stages;
        public IReadOnlyCollection<BaseContentJob> Stages => _stages ?? Array.Empty<BaseContentJob>();
        private BaseContentJob _activeStage;
        private int _activeStageIndex = -1;

        public CompositeContentJob(params BaseContentJob[] stages)
        {
            _activeStageIndex = -1;
            _stages = stages.Where(x => x != null).ToArray();
        }
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            AdvanceStage(parentJob);
        }

        public override void Update()
        {
            base.Update();
            if (_activeStage == null)
                return;

            _activeStage.Update();
            if (_activeStage.IsCompleted)
            {
                if (_activeStage.HasFailed)
                {
                    LogError($"Stage {_activeStageIndex} '{_activeStage}' failed, exiting...");
                    TriggerCompleted(true, _activeStage.ErrorString);
                }
                else
                {
                    Log($"Stage {_activeStageIndex} '{_activeStage}' completed.");
                    AdvanceStage(_activeStage);
                }
            }
        }

        public CompositeContentJob Then(BaseContentJob job)
        {
            if (_activeStageIndex != -1)
                return null;

            var stages = _stages.ToList();
            stages.Add(job);
            _stages = stages.ToArray();
            return this;
        }

        private void AdvanceStage(BaseContentJob parentJob)
        {
            if (IsCompleted)
            {
                Log($"Cannot advance stage beyond {_activeStageIndex}, CompositeJob ({_stages.Length} stages) has been completed.");
                return;
            }

            ++_activeStageIndex;
            if (_activeStageIndex >= _stages.Length)
            {
                Log($"CompositeContentJob.Completed {_stages.Length} stages");
                TriggerCompleted();
                return;
            }
            _activeStage = _stages[_activeStageIndex];
            Log($"Stage {_activeStageIndex} '{_activeStage}' started...");
            _activeStage.Start(parentJob);
        }
    }

    public static class CompositeContentJobExtensions
    {
        public static CompositeContentJob Then(this BaseContentJob job, BaseContentJob otherJob)
        {
            return new CompositeContentJob(job, otherJob);
        }
    }
}