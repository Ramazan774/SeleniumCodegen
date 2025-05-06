using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.RegularExpressions; // For sanitizing
using OpenQA.Selenium.DevTools.V136;
using DevToolsSessionDomains = OpenQA.Selenium.DevTools.V136.DevToolsSessionDomains;
using OpenQA.Selenium.DevTools.V136.Page;
using OpenQA.Selenium.DevTools.V136.Runtime;

namespace WebDriverCdpRecorder
{
    // Record definitions
    public record RecordedAction(string ActionType, string? SelectorType, string? SelectorValue, string? Value);
    public record ActionFromJs(string Type, string? Selector, string? Value, string? Key);

    class Program
    {
        private const string JsBindingName = "sendActionToCSharp";
        private static bool _isRecording = false;
        private static List<RecordedAction> _currentFeatureActions = new List<RecordedAction>();
        private static string _currentFeatureName = "DefaultFeature";
        private static readonly List<string> _consoleLogBuffer = new List<string>();

        static async Task Main(string[] args)
        {
            _currentFeatureName = "ToDoApp";
            ResetRecorderState();
            Console.Out.Flush();

            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            Log("ChromeOptions created.");

            IWebDriver? driver = null;
            IDevToolsSession? session = null;
            DevToolsSessionDomains? domains = null;
            Log("Attempting to initialize ChromeDriver...");

            try
            {
                 var service = ChromeDriverService.CreateDefaultService();
                 service.EnableVerboseLogging = true;
                 service.LogPath = "./chromedriver.log";
                 Log($"ChromeDriverService created. LogPath: {Path.GetFullPath(service.LogPath)}");

                driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(120));
                Log("SUCCESS: ChromeDriver initialized!");

                Log("Attempting to get DevTools instance...");
                var devTools = driver as IDevTools;
                if (devTools != null)
                {
                    Log("SUCCESS: Got IDevTools interface.");
                    Log("Attempting to get DevTools session...");
                    session = devTools.GetDevToolsSession();
                    if (session != null)
                    {
                         Log($"SUCCESS: Got DevTools Session!");
                         Log("Attempting to get V136 specific domains...");
                         domains = session.GetVersionSpecificDomains<DevToolsSessionDomains>();
                         if (domains != null)
                         {
                             Log("SUCCESS: Got V136 specific domains object!");
                             Log("Attempting to enable Page and Runtime domains...");
                             await domains.Page.Enable(new OpenQA.Selenium.DevTools.V136.Page.EnableCommandSettings());
                             await domains.Runtime.Enable(new OpenQA.Selenium.DevTools.V136.Runtime.EnableCommandSettings());
                             Log("SUCCESS: Enabled Page and Runtime domains.");

                             Log($"Attempting to add binding '{JsBindingName}'...");
                             await domains.Runtime.AddBinding(new OpenQA.Selenium.DevTools.V136.Runtime.AddBindingCommandSettings { Name = JsBindingName });
                             Log("SUCCESS: Added runtime binding.");

                             Log("Subscribing to events...");
                             domains.Page.FrameNavigated += HandleFrameNavigated;
                             domains.Runtime.BindingCalled += HandleBindingCalled;
                             Log("SUCCESS: Subscribed to events.");

                             Log("Injecting JavaScript listeners...");
                             await AddJsListeners(domains);
                             Log("SUCCESS: JavaScript injection attempted.");
                         } else { 
                            Log("FAIL: GetVersionSpecificDomains<V136> returned null.");
                        }
                    } else { 
                        Log("FAIL: devTools.GetDevToolsSession() returned null."); 
                    }
                } else { 
                    Log("FAIL: Driver could not be cast to IDevTools."); 
                }

                // --- Start Recording ---
                _isRecording = true;
                Log($"\n--- Recording Started (Feature: {_currentFeatureName}) ---");

                Log("Attempting initial navigation...");
                string targetUrl = "https://ramazanovdev.netlify.app/";
                Log($"Navigating to: {targetUrl}");
                driver.Navigate().GoToUrl(targetUrl);
                Log("SUCCESS: Navigation command sent.");
                await Task.Delay(1000);

                Console.WriteLine("\n--- Ready for Interaction ---");
                Console.WriteLine("Interact with the page (click, type, enter).");
                Console.WriteLine($"Type 'stop' to finish recording.");
                Console.WriteLine($"Type 'new feature <YourFeatureName>' to start a new feature file.");
                Console.Out.Flush();

                // Recording loop
                while (_isRecording)
                {
                    if (Console.KeyAvailable)
                    {
                        string? input = Console.ReadLine()?.Trim();
                        if (input?.ToLowerInvariant() == "stop") {
                            _isRecording = false;
                            Log("--- Stop command received ---");
                        }
                        else if (input != null && input.ToLowerInvariant().StartsWith("new feature "))
                        {
                            string newFeatureNameInput = input.Substring("new feature ".Length).Trim();
                            
                            string newFeatureName = SanitizeForFileName(newFeatureNameInput);
                             if (string.IsNullOrWhiteSpace(newFeatureName)) {
                                newFeatureName = $"Feature_{DateTime.Now:yyyyMMddHHmmss}";
                             }
                            Log($"--- Starting new feature command received: {newFeatureName} ---");
                            GenerateFilesForCurrentFeature();
                            _currentFeatureName = newFeatureName; 
                            ResetRecorderState(); 
                            Log($"--- Recording new feature: {_currentFeatureName} ---");
                        }
                    }
                    await Task.Delay(100);
                }
                 Console.WriteLine("\n--- Recording Stopped ---");
            }
            catch (Exception ex)
            {
                Log($"FAIL: An error occurred during setup/run: {ex.ToString()}");
                 _isRecording = false;
            }
            finally
            {
                 GenerateFilesForCurrentFeature(); 
                 Console.WriteLine("\n--- C# Event Handler Console Logs ---");
                 lock(_consoleLogBuffer) { _consoleLogBuffer.ForEach(Console.WriteLine); }
                 Console.WriteLine("------------------------------------\n");

                if (session != null && domains != null) { 
                    Log("Unsubscribing from events...");
                    domains.Page.FrameNavigated -= HandleFrameNavigated;
                    domains.Runtime.BindingCalled -= HandleBindingCalled;
                    Log($"Attempting to remove binding '{JsBindingName}'...");
                     try { 
                        await domains.Runtime.RemoveBinding(new OpenQA.Selenium.DevTools.V136.Runtime.RemoveBindingCommandSettings { Name = JsBindingName }); 
                        Log("SUCCESS: Binding removed.");
                    }
                     catch(Exception rbEx) { 
                        Log($"Info: Error removing binding: {rbEx.Message}");} 
                    }

                // Quit Driver
                Log("Attempting to quit driver...");
                try { 
                    driver?.Quit(); 
                    Log("Driver quit successfully."); 
                }
                catch (Exception qEx) { 
                    Log($"Error quitting driver: {qEx.Message}"); 
                }
            }

            Console.WriteLine("\n--- Recorder Finished ---");
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }

        private static void ResetRecorderState() { 
             _consoleLogBuffer.Clear();
             lock(_currentFeatureActions) { _currentFeatureActions.Clear(); } }

        private static void GenerateFilesForCurrentFeature() {
             List<RecordedAction> actionsToGenerate;
             lock (_currentFeatureActions) { 
                if (!_currentFeatureActions.Any()) { 
                    Log($"Skipping file generation for '{_currentFeatureName}': No actions."); 
                    return; 
                } 
                actionsToGenerate = new List<RecordedAction>(_currentFeatureActions); 
            }
             Console.WriteLine($"\n--- Generating SpecFlow files for feature: {_currentFeatureName} ---");
             GenerateSpecFlowFiles(actionsToGenerate, _currentFeatureName); 
        }

        private static void Log(string message) { 
            string l=$"{DateTime.Now:HH:mm:ss.fff}-{message}"; 
            Console.WriteLine(l); 
            Console.Out.Flush(); 
        }
        private static void LogEventHandler(string message) {
            string l=$"{DateTime.Now:HH:mm:ss.fff}-{message}"; 
            lock(_consoleLogBuffer){_consoleLogBuffer.Add(l);}
        }

        private static void HandleFrameNavigated(object? sender, OpenQA.Selenium.DevTools.V136.Page.FrameNavigatedEventArgs e) {
            if (!_isRecording || e.Frame == null || e.Frame.ParentId != null) 
                return;
            string url = e.Frame.UrlFragment ?? e.Frame.Url;
            LogEventHandler($"---> EVENT: FrameNavigated to: {url}");
            lock(_currentFeatureActions) {
                var lastAction = _currentFeatureActions.LastOrDefault();
                if (!_currentFeatureActions.Any() || !(lastAction.ActionType == "Navigate" && lastAction.Value == url)) { 
                    AddAction_Locked("Navigate", null, null, url); 
                }
            }
        }
        private static void HandleBindingCalled(object? sender, OpenQA.Selenium.DevTools.V136.Runtime.BindingCalledEventArgs e) {
            if (!_isRecording || e.Name != JsBindingName) 
                return;
            LogEventHandler($"---> EVENT: JS Binding Called. Payload: {e.Payload}");
            try { 
                var action = JObject.Parse(e.Payload).ToObject<ActionFromJs>(); 
                if (action != null && !string.IsNullOrEmpty(action.Type)) { 
                    LogEventHandler($"   -> Parsed: Type='{action.Type}', Sel='{action.Selector}', Val='{action.Value}', Key='{action.Key}'"); 
                    string sType="Unknown", sVal=action.Selector??"N/A"; 
                    if (!string.IsNullOrEmpty(action.Selector)) { 
                        if (action.Selector.StartsWith("#")) { 
                            sType = "Id"; sVal = action.Selector.Substring(1); 
                        } else if (action.Selector.StartsWith(".")) { 
                            sType = "ClassName"; sVal = action.Selector.Substring(1).Split(new[] { '.', ' ' },StringSplitOptions.RemoveEmptyEntries)[0]; 
                        } else if (action.Selector.Contains(" ") || action.Selector.Contains(">") || action.Selector.Contains("[")) { 
                            sType = "CssSelector"; 
                        } else { 
                            sType = "TagName"; 
                        } 
                    } 
                    lock (_currentFeatureActions) { 
                        switch (action.Type.ToLowerInvariant()) { 
                            case "click": AddAction_Locked("Click", sType, sVal, null); 
                                break; 
                            case "change": AddAction_Locked("SendKeys", sType, sVal, action.Value); 
                                break; 
                            case "enterkey": AddAction_Locked("SendKeysEnter", sType, sVal, null); 
                                break; 
                        } 
                    } 
                } else { 
                    LogEventHandler($"   -> WARN: Bad payload parse."); } 
            } catch (Exception ex) { 
                LogEventHandler($"   -> FAIL: Processing binding: {ex.Message}"); 
            } 
        }

        private static void AddAction_Locked(string type, string? selType, string? selValue, string? value) {
            var a=new RecordedAction(type, selType, selValue, value); 
            _currentFeatureActions.Add(a); 
            LogEventHandler($"   -> Recorded: {type} Sel='{selType}={selValue}' Val='{value ?? "N/A"}'"); 
        }

        private static async Task AddJsListeners(DevToolsSessionDomains domains) { 
            string script = $@" (function() {{ /* ... same JS as before ... */ }})(); "; 
            script = $@" (function() {{ if (window.cdpRecorderListenersAttached) return; window.cdpRecorderListenersAttached = true; console.log('Injecting CDP Listeners...'); function getSel(el) {{ if (!el || !el.tagName) return null; try {{ if (el.id) return '#' + el.id; if (el.classList.contains('new-todo')) return '.new-todo'; if (el.className && typeof el.className === 'string') {{ const c = el.className.split(' ').filter(i => i && !i.includes(':') && !i.includes('(')); if (c.length > 0) return '.' + c[0]; }} return el.tagName.toLowerCase(); }} catch (e) {{ return el.tagName ? el.tagName.toLowerCase() : 'unknown'; }} }} function rec(ev) {{ const t = ev.target; if (!t || !t.tagName || t.tagName==='HTML' || t.tagName==='BODY') return; let a = {{ type: ev.type, selector: getSel(t), value: t.value, key: ev.key }}; console.log(`JS Send: type=${{a.type}}, sel=${{a.selector}}`); try {{ if (ev.type==='change' && (t.tagName==='INPUT'||t.tagName==='TEXTAREA'||t.tagName==='SELECT')) window.{JsBindingName}(JSON.stringify(a)); else if (ev.type==='click') {{ a.value=null; a.key=null; window.{JsBindingName}(JSON.stringify(a)); }} else if (ev.type==='keydown' && ev.key==='Enter' && t.tagName==='INPUT') {{ a.type='enterkey'; a.value=t.value; window.{JsBindingName}(JSON.stringify(a)); }} }} catch(e){{ console.error('JS Bind Err:', e); }} }} document.addEventListener('click', rec, {{ capture: true, passive: true }}); document.addEventListener('change', rec, {{ capture: true, passive: true }}); document.addEventListener('keydown', rec, {{ capture: true, passive: true }}); console.log('CDP Listeners Attached.'); }})();";
            try { 
                await domains.Page.AddScriptToEvaluateOnNewDocument(new AddScriptToEvaluateOnNewDocumentCommandSettings { Source = script }); 
                await domains.Runtime.Evaluate(new EvaluateCommandSettings { Expression = script, Silent = false }); 
            } catch (Exception ex) { 
                Log($"Error injecting script: {ex.Message}"); 
            }
        }

        static void GenerateSpecFlowFiles(List<RecordedAction> actions, string featureName) { 
            string safeFeatureName = SanitizeForFileName(featureName);
            string stepsClassName = $"{safeFeatureName}Steps";

            StringBuilder featureFile = new StringBuilder(); StringBuilder stepsFile = new StringBuilder();
            featureFile.AppendLine($"Feature: {safeFeatureName}"); 
            featureFile.AppendLine(); 
            featureFile.AppendLine($"Scenario: Perform recorded actions on {safeFeatureName}");
            string? lastValue = null; 
            bool lastActionWasSendKeys = false;
            var actionsCopy = new List<RecordedAction>(actions);
            for(int i = 0; i < actionsCopy.Count; i++) { 
                var action = actionsCopy[i]; 
                bool isLastActionInLoop = (i == actionsCopy.Count - 1); 
                if (lastValue != null && (action.ActionType != "SendKeysEnter" || isLastActionInLoop)) { 
                    int prevIndex = i - 1; 
                    if(prevIndex >= 0 && actionsCopy[prevIndex].ActionType == "SendKeys" && actionsCopy[prevIndex].Value == lastValue) { 
                        var sendKeysAction = actionsCopy[prevIndex]; 
                        featureFile.AppendLine($"\tAnd I type \"{lastValue}\" into element with {sendKeysAction.SelectorType} \"{sendKeysAction.SelectorValue}\""); 
                        lastValue = null; 
                    } else if (isLastActionInLoop && action.ActionType == "SendKeys" && action.Value == lastValue) { 
                        featureFile.AppendLine($"\tAnd I type \"{lastValue}\" into element with {action.SelectorType} \"{action.SelectorValue}\""); lastValue = null; 
                        lastActionWasSendKeys = false; } 
                }
                switch (action.ActionType) { 
                    case "Navigate": featureFile.AppendLine($"\tGiven I navigate to \"{action.Value}\""); lastActionWasSendKeys = false; 
                        break; 
                    case "Click": featureFile.AppendLine($"\tWhen I click the element with {action.SelectorType} \"{action.SelectorValue}\""); lastActionWasSendKeys = false; 
                        break; 
                    case "SendKeys": lastValue = action.Value; lastActionWasSendKeys = true; 
                        break; 
                    case "SendKeysEnter": 
                    if (lastValue != null) { 
                        featureFile.AppendLine($"\tAnd I type \"{lastValue}\" and press Enter in element with {action.SelectorType} \"{action.SelectorValue}\""); 
                        lastValue = null; 
                    } else { 
                        featureFile.AppendLine($"\tAnd I press Enter in element with {action.SelectorType} \"{action.SelectorValue}\""); 
                    } 
                    lastActionWasSendKeys = false; 
                        break; 
                }
                if (isLastActionInLoop && lastActionWasSendKeys && lastValue != null) { 
                    featureFile.AppendLine($"\tAnd I type \"{lastValue}\" into element with {action.SelectorType} \"{action.SelectorValue}\""); 
                } 
            }
            featureFile.AppendLine("\tThen the page should be in the expected state");

            stepsFile.AppendLine("using OpenQA.Selenium;\nusing TechTalk.SpecFlow;\nusing NUnit.Framework;\nusing System;\n\n[Binding]\npublic class " + stepsClassName + "\n{");
            stepsFile.AppendLine("    private readonly IWebDriver _driver;"); 
            stepsFile.AppendLine($"    public {stepsClassName}(IWebDriver driver) {{\n\t\t_driver = driver ?? throw new ArgumentNullException(nameof(driver));\n}}\n");
            var generatedSignatures = new HashSet<string>();
            if (actions.Any(a => a.ActionType == "Navigate")) { 
                AddNavigateSteps(stepsFile, generatedSignatures); 
            }
            if (actions.Any(a => a.ActionType == "Click")) { 
                AddClickSteps(stepsFile, generatedSignatures); 
            }
            if (actions.Any(a => a.ActionType == "SendKeys")) { 
                AddSendKeysSteps(stepsFile, generatedSignatures); 
            }
            if (actions.Any(a => a.ActionType == "SendKeysEnter")) { 
                AddEnterSteps(stepsFile, generatedSignatures); 
            }
            AddThenSteps(stepsFile, generatedSignatures); 
            AddGetByHelper(stepsFile);
            stepsFile.AppendLine("}"); 

            string featureFilePath = $"{safeFeatureName}.feature"; 
            string stepsFilePath = $"{stepsClassName}.cs";
            File.WriteAllText(featureFilePath, featureFile.ToString()); 
            File.WriteAllText(stepsFilePath, stepsFile.ToString());
            Console.WriteLine($"\nGenerated: {featureFilePath}\nGenerated: {stepsFilePath}"); }

        private static void AddNavigateSteps(StringBuilder s, HashSet<string> sig) { 
            if (sig.Add("NavigateToUrl")) { 
                s.AppendLine("    [Given(@\"I navigate to \"\"(.*)\"\"\")]"); 
                s.AppendLine("    [When(@\"I navigate to \"\"(.*)\"\"\")]"); 
                s.AppendLine("    public void NavigateToUrl(string url)"); 
                s.AppendLine("    {"); 
                s.AppendLine("        _driver.Navigate().GoToUrl(url);"); 
                s.AppendLine("    }"); 
                s.AppendLine(); 
            }
        }

        private static void AddClickSteps(StringBuilder s, HashSet<string> sig) { 
            if (sig.Add("ClickElement")) { 
                s.AppendLine("    [When(@\"I click the element with (.*) \"\"(.*)\"\"\")]"); 
                s.AppendLine("    public void ClickElement(string selectorType, string selectorValue)"); 
                s.AppendLine("    {"); 
                s.AppendLine("        _driver.FindElement(GetBy(selectorType, selectorValue)).Click();"); 
                s.AppendLine("    }"); 
                s.AppendLine(); 
            }
        }

        private static void AddSendKeysSteps(StringBuilder s, HashSet<string> sig) { 
            if (sig.Add("TypeIntoElement")) { 
                s.AppendLine("    [When(@\"I type \"\"(.*)\"\" into element with (.*) \"\"(.*)\"\"\")]"); 
                s.AppendLine("    public void TypeIntoElement(string text, string selectorType, string selectorValue)"); 
                s.AppendLine("    {"); 
                s.AppendLine("        var element = _driver.FindElement(GetBy(selectorType, selectorValue));"); 
                s.AppendLine("        element.SendKeys(text);"); 
                s.AppendLine("    }"); 
                s.AppendLine(); 
            }
        }

        private static void AddEnterSteps(StringBuilder s, HashSet<string> sig) { 
            if (sig.Add("TypeAndEnter")) { 
                s.AppendLine("    [When(@\"I type \"\"(.*)\"\" and press Enter in element with (.*) \"\"(.*)\"\"\")]"); 
                s.AppendLine("    public void TypeAndEnter(string text, string selectorType, string selectorValue)"); 
                s.AppendLine("    {"); 
                s.AppendLine("        var element = _driver.FindElement(GetBy(selectorType, selectorValue));"); 
                s.AppendLine("        element.SendKeys(text);"); 
                s.AppendLine("        element.SendKeys(Keys.Enter);"); 
                s.AppendLine("    }"); 
                s.AppendLine(); 
            } 
            if (sig.Add("PressEnterInElement")) { 
                s.AppendLine("    [When(@\"I press Enter in element with (.*) \"\"(.*)\"\"\")]"); 
                s.AppendLine("    public void PressEnterInElement(string selectorType, string selectorValue)"); 
                s.AppendLine("    {"); 
                s.AppendLine("        _driver.FindElement(GetBy(selectorType, selectorValue)).SendKeys(Keys.Enter);"); 
                s.AppendLine("    }"); 
                s.AppendLine(); 
            }
        }

        private static void AddThenSteps(StringBuilder s, HashSet<string> sig) { 
            if (sig.Add("ThenExpectedState")){ 
                s.AppendLine("    [Then(@\"the page should be in the expected state\")]"); 
                s.AppendLine("    public void ThenExpectedState()"); 
                s.AppendLine("    {"); 
                s.AppendLine("        Assert.Pass(\"Placeholder assertion passed. Implement real checks.\");"); 
                s.AppendLine("    }"); 
                s.AppendLine(); 
            }}
        private static void AddGetByHelper(StringBuilder s) { 
            s.AppendLine("    private By GetBy(string selectorType, string selectorValue)"); 
            s.AppendLine("    {"); 
            s.AppendLine("        selectorValue = selectorValue ?? string.Empty; selectorType = selectorType ?? string.Empty;"); 
            s.AppendLine("        switch (selectorType.ToLowerInvariant().Trim())"); 
            s.AppendLine("        {"); 
            s.AppendLine("            case \"id\": return By.Id(selectorValue);");
            s.AppendLine("            case \"name\": return By.Name(selectorValue);");
            s.AppendLine("            case \"classname\": return By.ClassName(selectorValue);");
            s.AppendLine("            case \"cssselector\": return By.CssSelector(selectorValue);");
            s.AppendLine("            case \"xpath\": return By.XPath(selectorValue);");
            s.AppendLine("            case \"linktext\": return By.LinkText(selectorValue);");
            s.AppendLine("            case \"partiallinktext\": return By.PartialLinkText(selectorValue);");
            s.AppendLine("            case \"tagname\": return By.TagName(selectorValue);");
            s.AppendLine("            default: throw new ArgumentException($\"Unsupported selector type provided: '{selectorType}'. Value was '{selectorValue}'.\");"); 
            s.AppendLine("        }"); 
            s.AppendLine("    }"); 
        }

        private static string SanitizeForFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) 
                return "InvalidFeatureName";
            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string regex = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            string sanitized = Regex.Replace(input, regex, "_");
            sanitized = sanitized.Trim('_', ' ');
             if (string.IsNullOrWhiteSpace(sanitized)) 
                return "SanitizedFeatureName";
            if (!char.IsLetter(sanitized[0]) && sanitized[0] != '_') sanitized = "_" + sanitized;
            sanitized = Regex.Replace(sanitized, @"_+", "_");
            return sanitized;
        }

    } 
} 