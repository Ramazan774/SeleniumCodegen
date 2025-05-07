using System;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using OpenQA.Selenium.DevTools.V136.Page;
using OpenQA.Selenium.DevTools.V136.Runtime;
using WebDriverCdpRecorder.Models;
using WebDriverCdpRecorder.Utils;

namespace WebDriverCdpRecorder.Core
{
    /// <summary>
    /// Handles DevTools protocol events
    /// </summary>
    public class EventHandlers
    {
        private readonly RecorderState _state;

        /// <summary>
        /// Constructor
        /// </summary>
        public EventHandlers(RecorderState state)
        {
            _state = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// Handles frame navigation events
        /// </summary>
        public void HandleFrameNavigated(object? sender, FrameNavigatedEventArgs e)
        {
            if (!_state.IsRecording || e.Frame == null || e.Frame.ParentId != null)
                return;

            string url = e.Frame.UrlFragment ?? e.Frame.Url;
            if (url.Equals("about:blank", StringComparison.OrdinalIgnoreCase))
                return;

            Logger.LogEventHandler($"---> EVENT: FrameNavigated to: {url}");
            
            var lastAction = _state.GetLastAction();
            if (lastAction == null || !(lastAction.ActionType == "Navigate" && lastAction.Value == url))
            {
                _state.AddAction("Navigate", null, null, url);
            }
        }

        /// <summary>
        /// Handles JavaScript binding calls from the enhanced selector system
        /// </summary>
        public void HandleBindingCalled(object? sender, BindingCalledEventArgs e)
        {
            if (!_state.IsRecording || e.Name != "sendActionToCSharp")
                return;

            Logger.LogEventHandler($"---> EVENT: JS Binding Called. Payload: {e.Payload}");
            
            try
            {
                // Modified to handle the updated JS selector format
                var jsonObj = JObject.Parse(e.Payload);
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
                        }
                        else
                        {
                            // For custom elements, still record the change
                            _state.AddAction("SendKeys", selectorType, selectorValue, value, tagName, elementType);
                        }
                        break;
                        
                    case "enterkey":
                        // Pass value along, generation logic will decide if it's needed
                        _state.AddAction("SendKeysEnter", selectorType, selectorValue, value, tagName, elementType);
                        break;
                        
                    case "submit":
                        // Record form submissions
                        _state.AddAction("Submit", selectorType, selectorValue, value, tagName, elementType);
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.LogEventHandler($"   -> FAIL: Processing binding: {ex.Message}");
            }
        }
    }
}