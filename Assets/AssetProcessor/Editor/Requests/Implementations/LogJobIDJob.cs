using System;
using Rhinox.Perceptor;
using UnityEngine;

namespace Rhinox.AssetProcessor.Editor
{
    public class LogJobIDJob : BaseContentJob
    {
        public int ID { get; }

        public LogJobIDJob(int job_id)
        {
            ID = job_id;
        }
        
        protected override void OnStart(BaseContentJob parentJob = null)
        {
            PLog.Info($"Job with ID '{ID}' started at {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}...");
            TriggerCompleted();
        }
    }
}