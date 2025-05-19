using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using SpecFlowTestGenerator.Browser;
using SpecFlowTestGenerator.CodeGeneration;
using SpecFlowTestGenerator.Models;
using SpecFlowTestGenerator.Utils;

namespace SpecFlowTestGenerator.Core
{
    /// <summary>
    /// Main engine for the recorder application
    /// </summary>
    public class RecorderEngine
    {
        private readonly RecorderState _state;
        private readonly EventHandlers _eventHandlers;
        private readonly DevToolsSessionManager _sessionManager;
        private readonly JavaScriptInjector _jsInjector;
        private readonly SpecFlowGenerator _specFlowGenerator;
        private IWebDriver? _driver;
        private ChromeDriverService? _chromeService;
        
        /// <summary>
        /// Public property to check/set recording status
        /// </summary>
        public bool IsRecording
        {
            get { return _state.IsRecording; }
            set { _state.IsRecording = value; }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public RecorderEngine(string initialFeatureName = "DefaultFeature")
        {
            _state = new RecorderState(initialFeatureName);
            _eventHandlers = new EventHandlers(_state);
            
            // Create instances with proper initialization order to avoid circular dependency
            _jsInjector = new JavaScriptInjector(null!); // Temporarily pass null
            _sessionManager = new DevToolsSessionManager(_eventHandlers, _jsInjector);
            
            // Set the sessionManager on jsInjector to complete the dependency chain
            var field = _jsInjector.GetType().GetField("_sessionManager", 
                System.Reflection.BindingFlags.NonPublic | 
                System.Reflection.BindingFlags.Instance);
                
            if (field != null)
            {
                field.SetValue(_jsInjector, _sessionManager);
            }
            
            _specFlowGenerator = new SpecFlowGenerator();
        }

        /// <summary>
        /// Initialize the recorder engine
        /// </summary>
        public async Task<bool> Initialize()
        {
            try
            {
                // Create browser
                var (driver, service) = BrowserFactory.CreateChromeDriver();
                if (driver == null)
                {
                    Logger.Log("Failed to create Chrome driver.");
                    return false;
                }
                
                _driver = driver;
                _chromeService = service;

                // Initialize DevTools session with multi-version support
                if (!await _sessionManager.InitializeSession(_driver))
                {
                    Logger.Log("Failed to initialize DevTools session.");
                    return false;
                }

                // Inject JavaScript listeners
                await _jsInjector.InjectListeners();
                
                Logger.Log("SUCCESS: Recorder engine initialized.");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"FAIL: An error occurred during initialization: {ex.Message}");
                await CleanUp();
                return false;
            }
        }

        /// <summary>
        /// Start recording user actions
        /// </summary>
        public void StartRecording()
        {
            _state.IsRecording = true;
            Logger.Log($"\n--- Recording Started (Feature: {_state.CurrentFeatureName}) ---");
            Logger.Log("Ready for Interaction - Interact with the page (click, type, enter).");
            Logger.Log("Type 'stop' to finish recording.");
            Logger.Log("Type 'new feature <FeatureName>' to start a new feature.");
        }

        /// <summary>
        /// Stop recording user actions
        /// </summary>
        public void StopRecording()
        {
            _state.IsRecording = false;
            Logger.Log("--- Recording Stopped ---");
        }

        /// <summary>
        /// Process a command from the console
        /// </summary>
        public void ProcessCommand(string? command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return;

            string cmd = command.Trim().ToLowerInvariant();
            
            if (cmd == "stop")
            {
                StopRecording();
                GenerateCurrentFeatureFiles();
            }
            else if (cmd.StartsWith("new feature "))
            {
                string newFeatureNameInput = command.Substring("new feature ".Length).Trim();
                string newFeatureName = FileHelper.SanitizeForFileName(newFeatureNameInput);
                
                if (string.IsNullOrWhiteSpace(newFeatureName))
                {
                    newFeatureName = $"Feature_{DateTime.Now:yyyyMMddHHmmss}";
                }
                
                Logger.Log($"--- Starting new feature: {newFeatureName} ---");
                
                // Generate files for current feature before switching
                GenerateCurrentFeatureFiles();
                
                // Switch to new feature
                SwitchFeature(newFeatureName);
                
                Logger.Log($"--- Recording new feature: {_state.CurrentFeatureName} ---");
                Logger.Log("Navigate to the starting page for the new feature.");
            }
        }

        /// <summary>
        /// Switch to a new feature
        /// </summary>
        private void SwitchFeature(string featureName)
        {
            _state.CurrentFeatureName = featureName;
            _state.Reset();
        }

        /// <summary>
        /// Generate SpecFlow files for the current feature
        /// </summary>
        public void GenerateCurrentFeatureFiles()
        {
            if (!_state.HasActions())
            {
                Logger.Log($"INFO: Skipping file generation for '{_state.CurrentFeatureName}': No actions.");
                return;
            }

            List<RecordedAction> actions = _state.GetActions();
            Logger.Log($"\n--- Generating SpecFlow files for feature: {_state.CurrentFeatureName} ---");
            
            string outputDir = Environment.CurrentDirectory;
            _specFlowGenerator.GenerateFiles(actions, _state.CurrentFeatureName, outputDir);
        }

        /// <summary>
        /// Clean up resources
        /// </summary>
        public async Task CleanUp()
        {
            // Event handlers are now managed by the adapter pattern,
            // so we don't need to manually unsubscribe here
            
            // Clean up DevTools session
            await _sessionManager.CleanUpSession();

            // Quit driver
            BrowserFactory.SafeQuit(_driver);
            _driver = null;

            // Dispose Chrome service
            _chromeService?.Dispose();
            _chromeService = null;
            
            Logger.Log("Cleanup completed.");
        }
    }
}