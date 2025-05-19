using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using SpecFlowTestGenerator.Models;
using SpecFlowTestGenerator.Utils;

namespace SpecFlowTestGenerator.Core
{
    /// <summary>
    /// Interface for version-specific event handling adapters
    /// </summary>
    public interface IDevToolsEventAdapter
    {
        void RegisterEventHandlers(EventHandlers handler);
        void UnregisterEventHandlers();
    }

    /// <summary>
    /// Handles DevTools protocol events
    /// </summary>
    public class EventHandlers
    {
        private readonly RecorderState _state;
        private IDevToolsEventAdapter? _eventAdapter;

        /// <summary>
        /// Constructor
        /// </summary>
        public EventHandlers(RecorderState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// Sets the version-specific adapter
        /// </summary>
        public void SetAdapter(IDevToolsEventAdapter adapter)
        {
            _eventAdapter?.UnregisterEventHandlers();
            _eventAdapter = adapter;
            _eventAdapter.RegisterEventHandlers(this);
        }

        /// <summary>
        /// Handles frame navigation events generically
        /// </summary>
        public void HandleFrameNavigated(string frameId, string? parentId, string url, string? urlFragment)
        {
            if (!_state.IsRecording || parentId != null)
                return;

            string navigatedUrl = urlFragment ?? url;
            if (navigatedUrl.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                return;

            Logger.LogEventHandler($"---> EVENT: FrameNavigated to: {navigatedUrl}");
            
            var lastAction = _state.GetLastAction();
            if (lastAction == null || !(lastAction.ActionType == "Navigate" && lastAction.Value == navigatedUrl))
            {
                _state.AddAction("Navigate", null, null, navigatedUrl);
            }
        }

        /// <summary>
        /// Handles JavaScript binding calls from the enhanced selector system
        /// </summary>
        public void HandleBindingCalled(string name, string payload)
        {
            if (!_state.IsRecording || name != "sendActionToCSharp")
                return;

            Logger.LogEventHandler($"---> EVENT: JS Binding Called. Payload: {payload}");
            
            try
            {
                // Modified to handle the updated JS selector format
                var jsonObj = JObject.Parse(payload);
                string actionType = jsonObj["type"]?.ToString() ?? string.Empty;
                string selectorType = jsonObj["selector"]?.ToString() ?? string.Empty;
                string selectorValue = jsonObj["selectorValue"]?.ToString() ?? string.Empty;
                string? value = jsonObj["value"]?.ToString();
                string? tagName = jsonObj["tagName"]?.ToString();
                string? elementType = jsonObj["elementType"]?.ToString();
                
                Logger.LogEventHandler($"   -> Parsed: Type='{actionType}', Tag='{tagName}', ElType='{elementType}', SelType='{selectorType}', SelVal='{selectorValue}', Val='{value}'");
                
                // We already have the selector type and value directly from the smart selector script
                
                switch (actionType.ToLowerInvariant())
                {
                    case "click":
                        _state.AddAction("Click", selectorType, selectorValue, null, tagName, elementType);
                        break;
                        
                    case "change":
                        // Ignore change on checkbox/radio - click is sufficient
                        if (elementType?.ToLowerInvariant() == "checkbox" || elementType?.ToLowerInvariant() == "radio")
                        {
                            Logger.LogEventHandler($"   -> Ignored: 'change' event on element type '{elementType}'");
                        }
                        // Record change on SELECT as SelectOption
                        else if (tagName?.ToUpperInvariant() == "SELECT")
                        {
                            _state.AddAction("SelectOption", selectorType, selectorValue, value, tagName, elementType);
                        }
                        // Record change on other inputs as SendKeys (captures final value)
                        else if (tagName?.ToUpperInvariant() == "INPUT" || tagName?.ToUpperInvariant() == "TEXTAREA")
                        {
                            _state.AddAction("SendKeys", selectorType, selectorValue, value, tagName, elementType);