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
        private EventHandlers _eventHandlers;
        private JavaScriptInjector _jsInjector;
        private DevToolsSessionManager _sessionManager;
        private IWebDriver? _driver;
        private ChromeDriverService? _chromeService;
        private readonly SpecFlowGenerator _specFlowGenerator;
        
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
            
            // Break the circular dependency by using empty constructors
            _jsInjector = new JavaScriptInjector();
            _sessionManager = new DevToolsSessionManager();
            
            // Set up the dependencies
            _jsInjector.SetSessionManager(_sessionManager);
            _sessionManager.SetDependencies(_eventHandlers, _jsInjector);
            
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

                // Initialize DevTools session
                if (!await _sessionManager.InitializeSession(_driver))
                {
                    Logger.Log("Failed to initialize DevTools session.");
                    return false;
                }

                // Inject JavaScript listeners
                await _jsInjector.InjectListeners();
                
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
            // Clean up DevTools session
            await _sessionManager.CleanUpSession();

            // Quit driver
            if (_driver != null)
            {
                try
                {
                    _driver.Quit();
                    _driver.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Log($"INFO: Error during driver cleanup: {ex.Message}");
                }
                _driver = null;
            }

            // Dispose Chrome service
            _chromeService?.Dispose();
            _chromeService = null;
            
            Logger.Log("Cleanup completed.");
        }
    }
}