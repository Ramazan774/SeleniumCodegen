using System;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using SpecFlowTestGenerator.Core;
using SpecFlowTestGenerator.Utils;

namespace SpecFlowTestGenerator.Browser
{
    /// <summary>
    /// Manages DevTools sessions for interacting with browser CDP with multi-version support
    /// </summary>
    public class DevToolsSessionManager
    {
        private IDevToolsSession? _session;
        private object? _domains;
        private const string JsBindingName = "sendActionToCSharp";
        private EventHandlers? _eventHandlers;
        private JavaScriptInjector? _jsInjector;
        private bool _isInitialized = false;

        /// <summary>
        /// Gets the JS binding name used for communication
        /// </summary>
        public string BindingName => JsBindingName;

        /// <summary>
        /// Creates a new instance of DevToolsSessionManager with no dependencies (for use in circular dependency scenarios)
        /// </summary>
        public DevToolsSessionManager()
        {
            // Empty constructor for circular dependency resolution
        }

        /// <summary>
        /// Creates a new instance of DevToolsSessionManager
        /// </summary>
        public DevToolsSessionManager(EventHandlers eventHandlers, JavaScriptInjector jsInjector)
        {
            _eventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
            _jsInjector = jsInjector ?? throw new ArgumentNullException(nameof(jsInjector));
            _isInitialized = true;
        }

        /// <summary>
        /// Sets the dependencies after construction (for breaking circular dependencies)
        /// </summary>
        public void SetDependencies(EventHandlers eventHandlers, JavaScriptInjector jsInjector)
        {
            _eventHandlers = eventHandlers ?? throw new ArgumentNullException(nameof(eventHandlers));
            _jsInjector = jsInjector ?? throw new ArgumentNullException(nameof(jsInjector));
            _isInitialized = true;
        }

        /// <summary>
        /// Initializes the DevTools session from the driver
        /// </summary>
        public async Task<bool> InitializeSession(IWebDriver driver)
        {
            if (!_isInitialized)
            {
                Logger.Log("ERROR: DevToolsSessionManager not properly initialized. Call SetDependencies first.");
                return false;
            }

            if (driver == null)
                return false;

            try
            {
                Logger.Log("Attempting to get DevTools instance...");
                var devTools = driver as IDevTools;
                if (devTools == null)
                {
                    Logger.Log("FAIL: Driver could not be cast to IDevTools.");
                    return false;
                }

                Logger.Log("SUCCESS: Got IDevTools interface.");
                Logger.Log("Attempting to get DevTools session...");
                _session = devTools.GetDevToolsSession();
                if (_session == null)
                {
                    Logger.Log("FAIL: devTools.GetDevToolsSession() returned null.");
                    return false;
                }

                Logger.Log($"SUCCESS: Got DevTools Session!");
                
                // Try to initialize with different versions
                return await InitializeBasedOnVersion();
            }
            catch (Exception ex)
            {
                Logger.Log($"FAIL: An error occurred during DevTools session setup: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tries to initialize using available CDP versions
        /// </summary>
        private async Task<bool> InitializeBasedOnVersion()
        {
            // Try V136 first (newest)
            if (await TryInitializeV136())
                return true;
                
            // No supported version found
            Logger.Log("FAIL: No supported DevTools version found.");
            return false;
        }

        /// <summary>
        /// Tries to initialize with Chrome DevTools Protocol version 136
        /// </summary>
        private async Task<bool> TryInitializeV136()
        {
            try
            {
                Logger.Log("Attempting to get V136 specific domains...");
                var domains = _session!.GetVersionSpecificDomains<OpenQA.Selenium.DevTools.V136.DevToolsSessionDomains>();
                if (domains == null)
                    return false;

                Logger.Log("SUCCESS: Got V136 specific domains object!");
                Logger.Log("Attempting to enable Page and Runtime domains...");
                await domains.Page.Enable(new OpenQA.Selenium.DevTools.V136.Page.EnableCommandSettings());
                await domains.Runtime.Enable(new OpenQA.Selenium.DevTools.V136.Runtime.EnableCommandSettings());
                Logger.Log("SUCCESS: Enabled Page and Runtime domains.");

                Logger.Log($"Attempting to add binding '{JsBindingName}'...");
                await domains.Runtime.AddBinding(new OpenQA.Selenium.DevTools.V136.Runtime.AddBindingCommandSettings { Name = JsBindingName });
                Logger.Log("SUCCESS: Added runtime binding.");

                // Use null-forgiving operator since we already checked _isInitialized
                _eventHandlers!.SetAdapter(new V136EventAdapter(domains));
                _jsInjector!.SetAdapter(new V136JavaScriptInjectionAdapter(domains));

                _domains = domains;
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"INFO: V136 not supported: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Cleans up the DevTools session
        /// </summary>
        public async Task CleanUpSession()
        {
            if (_session == null || _domains == null)
            {
                Logger.Log("INFO: Session or Domains object was null during cleanup.");
                return;
            }

            Logger.Log($"Attempting to remove binding '{JsBindingName}'...");
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                
                // Clean up based on which version is active
                if (_domains is OpenQA.Selenium.DevTools.V136.DevToolsSessionDomains v136)
                {
                    await v136.Runtime.RemoveBinding(new OpenQA.Selenium.DevTools.V136.Runtime.RemoveBindingCommandSettings { Name = JsBindingName }, cts.Token);
                }
                
                Logger.Log("SUCCESS: Binding removed.");
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is TimeoutException)
            {
                Logger.Log($"Info: Timeout/Cancel removing binding: {ex.Message}");
            }
            catch (Exception ex)
            {
                Logger.Log($"Info: Error removing binding: {ex.Message}");
            }

            _session = null;
            _domains = null;
        }
    }
}