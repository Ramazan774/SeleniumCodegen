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

    // Version-specific implementations

    /// <summary>
    /// V136 specific event adapter
    /// </summary>
    public class V136EventAdapter : IDevToolsEventAdapter
    {
        private readonly OpenQA.Selenium.DevTools.V136.DevToolsSessionDomains _domains;
        private EventHandler<OpenQA.Selenium.DevTools.V136.Page.FrameNavigatedEventArgs>? _frameNavigatedHandler;
        private EventHandler<OpenQA.Selenium.DevTools.V136.Runtime.BindingCalledEventArgs>? _bindingCalledHandler;

        public V136EventAdapter(OpenQA.Selenium.DevTools.V136.DevToolsSessionDomains domains)
        {
            _domains = domains ?? throw new ArgumentNullException(nameof(domains));
        }

        public void RegisterEventHandlers(EventHandlers handler)
        {
            _frameNavigatedHandler = (sender, e) => 
                handler.HandleFrameNavigated(
                    e.Frame.Id, 
                    e.Frame.ParentId, 
                    e.Frame.Url, 
                    e.Frame.UrlFragment);
            
            _bindingCalledHandler = (sender, e) => 
                handler.HandleBindingCalled(e.Name, e.Payload);
            
            _domains.Page.FrameNavigated += _frameNavigatedHandler;
            _domains.Runtime.BindingCalled += _bindingCalledHandler;
            
            Logger.Log("SUCCESS: Registered V136 event handlers");
        }

        public void UnregisterEventHandlers()
        {
            if (_frameNavigatedHandler != null)
                _domains.Page.FrameNavigated -= _frameNavigatedHandler;
            
            if (_bindingCalledHandler != null)
                _domains.Runtime.BindingCalled -= _bindingCalledHandler;
            
            Logger.Log("V136 event handlers unregistered");
        }
    }

    /// <summary>
    /// V130 specific event adapter
    /// </summary>
    // public class V130EventAdapter : IDevToolsEventAdapter
    // {
    //     private readonly OpenQA.Selenium.DevTools.V130.DevToolsSessionDomains _domains;
    //     private EventHandler<OpenQA.Selenium.DevTools.V130.Page.FrameNavigatedEventArgs>? _frameNavigatedHandler;
    //     private EventHandler<OpenQA.Selenium.DevTools.V130.Runtime.BindingCalledEventArgs>? _bindingCalledHandler;

    //     public V130EventAdapter(OpenQA.Selenium.DevTools.V130.DevToolsSessionDomains domains)
    //     {
    //         _domains = domains ?? throw new ArgumentNullException(nameof(domains));
    //     }

    //     public void RegisterEventHandlers(EventHandlers handler)
    //     {
    //         _frameNavigatedHandler = (sender, e) => 
    //             handler.HandleFrameNavigated(
    //                 e.Frame.Id, 
    //                 e.Frame.ParentId, 
    //                 e.Frame.Url, 
    //                 e.Frame.UrlFragment);
            
    //         _bindingCalledHandler = (sender, e) => 
    //             handler.HandleBindingCalled(e.Name, e.Payload);
            
    //         _domains.Page.FrameNavigated += _frameNavigatedHandler;
    //         _domains.Runtime.BindingCalled += _bindingCalledHandler;
            
    //         Logger.Log("SUCCESS: Registered V130 event handlers");
    //     }

    //     public void UnregisterEventHandlers()
    //     {
    //         if (_frameNavigatedHandler != null)
    //             _domains.Page.FrameNavigated -= _frameNavigatedHandler;
            
    //         if (_bindingCalledHandler != null)
    //             _domains.Runtime.BindingCalled -= _bindingCalledHandler;
            
    //         Logger.Log("V130 event handlers unregistered");
    //     }
    // }

    // /// <summary>
    // /// V127 specific event adapter
    // /// </summary>
    // public class V127EventAdapter : IDevToolsEventAdapter
    // {
    //     private readonly OpenQA.Selenium.DevTools.V127.DevToolsSessionDomains _domains;
    //     private EventHandler<OpenQA.Selenium.DevTools.V127.Page.FrameNavigatedEventArgs>? _frameNavigatedHandler;
    //     private EventHandler<OpenQA.Selenium.DevTools.V127.Runtime.BindingCalledEventArgs>? _bindingCalledHandler;

    //     public V127EventAdapter(OpenQA.Selenium.DevTools.V127.DevToolsSessionDomains domains)
    //     {
    //         _domains = domains ?? throw new ArgumentNullException(nameof(domains));
    //     }

    //     public void RegisterEventHandlers(EventHandlers handler)
    //     {
    //         _frameNavigatedHandler = (sender, e) => 
    //             handler.HandleFrameNavigated(
    //                 e.Frame.Id, 
    //                 e.Frame.ParentId, 
    //                 e.Frame.Url, 
    //                 e.Frame.UrlFragment);
            
    //         _bindingCalledHandler = (sender, e) => 
    //             handler.HandleBindingCalled(e.Name, e.Payload);
            
    //         _domains.Page.FrameNavigated += _frameNavigatedHandler;
    //         _domains.Runtime.BindingCalled += _bindingCalledHandler;
            
    //         Logger.Log("SUCCESS: Registered V127 event handlers");
    //     }

    //     public void UnregisterEventHandlers()
    //     {
    //         if (_frameNavigatedHandler != null)
    //             _domains.Page.FrameNavigated -= _frameNavigatedHandler;
            
    //         if (_bindingCalledHandler != null)
    //             _domains.Runtime.BindingCalled -= _bindingCalledHandler;
            
    //         Logger.Log("V127 event handlers unregistered");
    //     }
    // }
}