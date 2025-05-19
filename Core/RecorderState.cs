using System;
using System.Collections.Generic;
using SpecFlowTestGenerator.Models;
using SpecFlowTestGenerator.Utils;

namespace SpecFlowTestGenerator.Core
{
    /// <summary>
    /// Manages state for the recording process
    /// </summary>
    public class RecorderState
    {
        private readonly object _lockObject = new object();
        private List<RecordedAction> _currentFeatureActions = new List<RecordedAction>();
        
        /// <summary>
        /// Gets or sets the name of the current feature being recorded
        /// </summary>
        public string CurrentFeatureName { get; set; } = "DefaultFeature";
        
        /// <summary>
        /// Gets or sets whether recording is currently active
        /// </summary>
        public bool IsRecording { get; set; } = false;

        /// <summary>
        /// Gets or sets the time when recording started
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Gets or sets the time when recording ended
        /// </summary>
        public DateTime? EndTime { get; set; }

        /// <summary>
        /// Constructor with optional feature name
        /// </summary>
        public RecorderState(string featureName = "DefaultFeature")
        {
            CurrentFeatureName = featureName;
        }

        /// <summary>
        /// Reset recorder state (clear actions)
        /// </summary>
        public void Reset()
        {
            lock (_lockObject)
            {
                _currentFeatureActions.Clear();
                StartTime = null;
                EndTime = null;
            }
            Logger.LogEventHandler($"Recorder state reset for feature: {CurrentFeatureName}");
        }

        /// <summary>
        /// Add an action to the recorded sequence
        /// </summary>
        public void AddAction(string type, string? selType, string? selValue, string? value, string? tagName = null, string? elementType = null)
        {
            var action = new RecordedAction(type, selType, selValue, value, tagName, elementType);
            
            // Set the timestamp for the action
            action.Timestamp = DateTime.Now;
            
            lock (_lockObject)
            {
                // If this is the first action, set the start time
                if (_currentFeatureActions.Count == 0 && StartTime == null)
                {
                    StartTime = DateTime.Now;
                }
                
                _currentFeatureActions.Add(action);
            }
            
            Logger.LogEventHandler($"   -> Recorded: {type} Tag='{tagName}' Type='{elementType}' Sel='{selType}={selValue}' Val='{value ?? "N/A"}'");
        }

        /// <summary>
        /// Gets a copy of the current actions
        /// </summary>
        public List<RecordedAction> GetActions()
        {
            lock (_lockObject)
            {
                return new List<RecordedAction>(_currentFeatureActions);
            }
        }

        /// <summary>
        /// Gets the last recorded action if any
        /// </summary>
        public RecordedAction? GetLastAction()
        {
            lock (_lockObject)
            {
                return _currentFeatureActions.Count > 0 ? _currentFeatureActions[^1] : null;
            }
        }

        /// <summary>
        /// Checks if any actions have been recorded
        /// </summary>
        public bool HasActions()
        {
            lock (_lockObject)
            {
                return _currentFeatureActions.Count > 0;
            }
        }

        /// <summary>
        /// Sets the recording as stopped and records the end time
        /// </summary>
        public void StopRecording()
        {
            lock (_lockObject)
            {
                IsRecording = false;
                EndTime = DateTime.Now;
            }
        }

        /// <summary>
        /// Sets the recording as started and records the start time
        /// </summary>
        public void StartRecording()
        {
            lock (_lockObject)
            {
                IsRecording = true;
                StartTime = DateTime.Now;
                EndTime = null;
            }
        }

        /// <summary>
        /// Clears all actions and resets timestamps
        /// </summary>
        public void Clear()
        {
            Reset();
        }
    }
}