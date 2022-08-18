using System;
using System.Collections;
using System.Collections.Generic;
using Rhinox.Perceptor;
using Unity.EditorCoroutines.Editor;

namespace Rhinox.AssetProcessor.Editor
{
    public class ContentRequestHub
    {
        private Queue<BaseContentJob> _requestQueue;
        
        private BaseContentJob _currentJob;
        private EditorCoroutine _queueProcessor;
        
        public bool IsActive { get; private set; }

        public bool IsBusy => IsActive && (_currentJob != null || (_requestQueue != null && _requestQueue.Count > 0));
        
        public void Run()
        {
            if (IsActive)
                return;
            
            // Initialize queue
            if (_requestQueue == null)
                _requestQueue = new Queue<BaseContentJob>();
            _requestQueue.Clear();
            _queueProcessor = EditorCoroutineUtility.StartCoroutineOwnerless(ParseQueue());
            IsActive = true;
        }

        public void Kill()
        {
            if (!IsActive)
                return;
            
            // Initialize queue
            if (_queueProcessor != null)
            {
                EditorCoroutineUtility.StopCoroutine(_queueProcessor);
                _queueProcessor = null;
            }

            if (_requestQueue != null)
                _requestQueue.Clear();
            IsActive = false;
        }
        
        public bool Enqueue(BaseContentJob job)
        {
            if (!IsActive || job == null)
            {
                PLog.Info($"Request {job} could not be enqueued (QueueState Active: {IsActive}).");
                return false;
            }

            PLog.Info($"Request {job} enqueued (QueueState Active: {IsActive}).");
            _requestQueue.Enqueue(job);
            return true;
        }
        
        private IEnumerator ParseQueue()
        {
            while (true)
            {
                if (_requestQueue == null)
                    yield return new EditorWaitForSeconds(0.05f);

                if (_currentJob != null)
                {
                    _currentJob.Update();
                    
                    if (_currentJob.IsCompleted)
                    {
                        PLog.Info($"Request {_currentJob} completed at {DateTime.Now.ToLocalTime()}.");
                        _currentJob = null;
                    }
                }
                else
                {
                    if (_requestQueue.Count > 0)
                    {
                        _currentJob = _requestQueue.Dequeue();
                        _currentJob.Start();
                        PLog.Info($"Request {_currentJob} started at {DateTime.Now.ToLocalTime()}.");
                    }
                }

                yield return new EditorWaitForSeconds(0.05f);
            }
        }
    }
}