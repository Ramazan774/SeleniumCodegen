using System;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.DevTools;
using OpenQA.Selenium.DevTools.V136;
using DevToolsSessionDomains = OpenQA.Selenium.DevTools.V136.DevToolsSessionDomains;
using OpenQA.Selenium.DevTools.V136.Page;
using OpenQA.Selenium.DevTools.V136.Runtime;
using WebDriverCdpRecorder.Utils;

namespace WebDriverCdpRecorder.Browser
{
    /// <summary>
    /// Manages DevTools sessions for interacting with browser CDP
    /// </summary>
    public class DevToolsSessionManager
    {
        private IDevToolsSession? _session;
        private DevToolsSessionDomains? _domains;
        private const string JsBindingName = "sendActionToCSharp";

        /// <summary>
        /// Gets the current DevTools session domains
        /// </summary>
        public DevToolsSessionDomains? Domains => _domains;

        /// <summary>
        /// Gets the JS binding name used for communication
        /// </summary>
        public string BindingName => JsBindingName;

        /// <summary>
        /// Initializes the DevTools session from the driver
        /// </summary>
        public async Task<bool> InitializeSession(IWebDriver driver)
        {
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
                Logger.Log("Attempting to get V136 specific domains...");
                _domains = _session.GetVersionSpecificDomains<DevToolsSessionDomains>();
                if (_domains == null)
                {
                    Logger.Log("FAIL: GetVersionSpecificDomains<V136> returned null.");
                    return false;
                }

                Logger.Log("SUCCESS: Got V136 specific domains object!");
                Logger.Log("Attempting to enable Page and Runtime domains...");
                await _domains.Page.Enable(new OpenQA.Selenium.DevTools.V136.Page.EnableCommandSettings());
                await _domains.Runtime.Enable(new OpenQA.Selenium.DevTools.V136.Runtime.EnableCommandSettings());
                Logger.Log("SUCCESS: Enabled Page and Runtime domains.");

                Logger.Log($"Attempting to add binding '{JsBindingName}'...");
                await _domains.Runtime.AddBinding(new AddBindingCommandSettings { Name = JsBindingName });
                Logger.Log("SUCCESS: Added runtime binding.");

                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"FAIL: An error occurred during DevTools session setup: {ex.Message}");
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
                await _domains.Runtime.RemoveBinding(new RemoveBindingCommandSettings { Name = JsBindingName }, cts.Token);
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