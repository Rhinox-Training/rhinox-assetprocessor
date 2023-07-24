using System;
using System.Linq;
using Rhinox.Perceptor;

namespace Rhinox.AssetProcessor.Editor
{
    public abstract class BaseContentJob
    {  
        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool HasFailed { get; private set; } 
        public string ErrorString { get; private set; }
        
        protected BaseContentJob ParentJob { get; private set; }

        protected T GetParentOfType<T>() where T : class
        {
            if (ParentJob == null)
                return default(T);

            var parent = ParentJob;
            while (parent != null)
            {
                if (parent is T job)
                    return job;
                parent = parent.ParentJob;
            }

            return default(T);
        }
        
        protected BaseContentJob()
        {
            
        }

        public bool Start(BaseContentJob parentJob = null)
        {
            if (IsRunning)
                return false;
                
            IsRunning = true;
            if (parentJob is CompositeContentJob compositeContentJob)
            {
                if (compositeContentJob.Stages.Count > 0)
                    parentJob = compositeContentJob.Stages.LastOrDefault();
            }
            ParentJob = parentJob;

            try
            {
                OnStart(parentJob);
            }
            catch (Exception e)
            {
                LogError($"Job {this} failed OnStart, reason: {e.ToString()}");
                TriggerCompleted();
                return false;
            }

            return true;
        }

        protected abstract void OnStart(BaseContentJob parentJob = null);

        public virtual void Update()
        {
            
        }

        protected void TriggerCompleted(bool failed = false, string errorString = "")
        {
            if (IsCompleted) // Can only fire once
                return;
            
            IsRunning = false;
            IsCompleted = true;
            HasFailed = failed;
            ErrorString = errorString;
            OnCompleted(failed, errorString);
        }
        
        protected virtual void OnCompleted(bool failed = false, string errorString = "")
        {
        }

        public override string ToString()
        {
            return GetType().Name;
        }

        protected virtual void Log(string line)
        {
            PLog.Info(line);
        }
        
        protected virtual void LogWarning(string line)
        {
            PLog.Warn(line);
        }

        protected virtual void LogError(string line)
        {
            PLog.Error(line);
        }
    }
}