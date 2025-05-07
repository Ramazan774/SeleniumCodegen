using System.Collections.Generic;
using WebDriverCdpRecorder.Models;
using WebDriverCdpRecorder.Utils;

namespace WebDriverCdpRecorder.Core
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
            }
            Logger.LogEventHandler($"Recorder state reset for feature: {CurrentFeatureName}");
        }

        /// <summary>
        /// Add an action to the recorded sequence
        /// </summary>
        public void AddAction(string type, string? selType, string? selValue, string? value, string? tagName = null, string? elementType = null)
        {
            var action = new RecordedAction(type, selType, selValue, value, tagName, elementType);
            lock (_lockObject)
            {
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
    }
}