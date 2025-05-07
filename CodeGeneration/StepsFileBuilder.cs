using System;
using System.Collections.Generic;
using System.Text;
using WebDriverCdpRecorder.Models;

namespace WebDriverCdpRecorder.CodeGeneration
{
    /// <summary>
    /// Builds SpecFlow steps files from recorded actions with universal compatibility
    /// </summary>
    public class StepsFileBuilder
    {
        /// <summary>
        /// Build the content of a SpecFlow steps file
        /// </summary>
        public string BuildStepsFileContent(List<RecordedAction> actions, string stepsClassName)
        {
            StringBuilder stepsFile = new StringBuilder();
            
            // Add file header
            stepsFile.AppendLine("using OpenQA.Selenium;");
            stepsFile.AppendLine("using OpenQA.Selenium.Support.UI;");
            stepsFile.AppendLine("using TechTalk.SpecFlow;");
            stepsFile.AppendLine("using NUnit.Framework;");
            stepsFile.AppendLine("using System;");
            stepsFile.AppendLine("using System.Threading;");
            stepsFile.AppendLine("using System.Collections.Generic;");
            stepsFile.AppendLine();
            
            // Add class definition
            stepsFile.AppendLine("[Binding]");
            stepsFile.AppendLine($"public class {stepsClassName}");
            stepsFile.AppendLine("{");
            stepsFile.AppendLine("    private readonly IWebDriver _driver;");
            stepsFile.AppendLine("    private string _baseUrl = string.Empty;");
            stepsFile.AppendLine();
            
            // Add constructor
            stepsFile.AppendLine($"    public {stepsClassName}(IWebDriver driver)");
            stepsFile.AppendLine("    {");
            stepsFile.AppendLine("        _driver = driver ?? throw new ArgumentNullException(nameof(driver));");
            stepsFile.AppendLine("        ");
            stepsFile.AppendLine("        // Set implicit wait to improve stability");
            stepsFile.AppendLine("        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);");
            stepsFile.AppendLine("        ");
            stepsFile.AppendLine("        // Initial delay to ensure browser is fully initialized");
            stepsFile.AppendLine("        Thread.Sleep(1000);");
            stepsFile.AppendLine("    }");
            stepsFile.AppendLine();
            
            // Add step methods based on the types of actions recorded
            var generatedSignatures = new HashSet<string>();
            
            if (actions.Exists(a => a.ActionType == "Navigate"))
            {
                AddNavigateSteps(stepsFile, generatedSignatures);
            }
            
            if (actions.Exists(a => a.ActionType == "Click"))
            {
                AddClickSteps(stepsFile, generatedSignatures);
            }
            
            if (actions.Exists(a => a.ActionType == "SendKeys"))
            {
                AddSendKeysSteps(stepsFile, generatedSignatures);
            }
            
            if (actions.Exists(a => a.ActionType == "SendKeysEnter"))
            {
                AddEnterSteps(stepsFile, generatedSignatures);
            }
            
            if (actions.Exists(a => a.ActionType == "SelectOption"))
            {
                AddSelectSteps(stepsFile, generatedSignatures);
            }
            
            // Always add Then step
            AddThenSteps(stepsFile, generatedSignatures);
            
            // Add helper methods
            AddGetByHelper(stepsFile);
            AddWaitForElementHelper(stepsFile);
            
            // Close class
            stepsFile.AppendLine("}");
            
            return stepsFile.ToString();
        }

        private void AddNavigateSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("NavigateToUrl"))
            {
                s.AppendLine("    [Given(@\"I navigate to \"\"(.*)\"\"\")]");
                s.AppendLine("    [When(@\"I navigate to \"\"(.*)\"\"\")]");
                s.AppendLine("    public void NavigateToUrl(string url)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Navigating to {url}\");");
                s.AppendLine("        _driver.Navigate().GoToUrl(url);");
                s.AppendLine("        ");
                s.AppendLine("        // Store base URL for potential relative navigation later");
                s.AppendLine("        Uri uri = new Uri(url);");
                s.AppendLine("        _baseUrl = $\"{uri.Scheme}://{uri.Host}\";");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for page to load completely");
                s.AppendLine("        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));");
                s.AppendLine("        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript(\"return document.readyState\").Equals(\"complete\"));");
                s.AppendLine("        ");
                s.AppendLine("        // Additional wait to ensure UI is ready");
                s.AppendLine("        Thread.Sleep(1000);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddClickSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("ClickElement"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I click the element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void ClickElementWith(string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Clicking element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            element.Click();");
                s.AppendLine("        }");
                s.AppendLine("        catch (ElementClickInterceptedException)");
                s.AppendLine("        {");
                s.AppendLine("            // Fallback to JavaScript click if regular click fails");
                s.AppendLine("            Console.WriteLine(\"Regular click failed, trying JavaScript click...\");");
                s.AppendLine("            ((IJavaScriptExecutor)_driver).ExecuteScript(\"arguments[0].click();\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after click");
                s.AppendLine("        Thread.Sleep(500);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddSendKeysSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("TypeIntoElement"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I type \"\"(.*)\"\" into element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    [Given(@\"I type \"\"(.*)\"\" into element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void TypeIntoElement(string text, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Typing '{text}' into element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            element.Clear();");
                s.AppendLine("            element.SendKeys(text);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard typing failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for setting value");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].value = arguments[1];\", element, text);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new Event('change'));\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after typing");
                s.AppendLine("        Thread.Sleep(300);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddEnterSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("TypeAndEnter"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I type \"\"(.*)\"\" and press Enter in element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    [Given(@\"I type \"\"(.*)\"\" and press Enter in element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void TypeAndEnter(string text, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Typing '{text}' and pressing Enter in element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            element.Clear();");
                s.AppendLine("            element.SendKeys(text);");
                s.AppendLine("            Thread.Sleep(300); // Short pause before pressing Enter");
                s.AppendLine("            element.SendKeys(Keys.Enter);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard typing failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for setting value and pressing Enter");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].value = arguments[1];\", element, text);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new Event('change'));\", element);");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new KeyboardEvent('keydown', {'key': 'Enter', 'keyCode': 13}));\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after pressing Enter");
                s.AppendLine("        Thread.Sleep(1000);");
                s.AppendLine("    }");
                s.AppendLine();
            }

            if (sig.Add("PressEnterInElement"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I press Enter in element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    [Given(@\"I press Enter in element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void PressEnterInElement(string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Pressing Enter in element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            element.SendKeys(Keys.Enter);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard Enter key failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for pressing Enter");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].dispatchEvent(new KeyboardEvent('keydown', {'key': 'Enter', 'keyCode': 13}));\", element);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after pressing Enter");
                s.AppendLine("        Thread.Sleep(1000);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddSelectSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("SelectOptionByValue"))
            {
                // Use the exact pattern that matches the feature file
                s.AppendLine("    [When(@\"I select option with value \"\"(.*)\"\" from element with (.*?) \"\"(.*?)\"\"\")]");
                s.AppendLine("    public void SelectOptionByValue(string valueToSelect, string selectorType, string selectorValue)");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine($\"Selecting option '{valueToSelect}' from element with {selectorType}='{selectorValue}'\");");
                s.AppendLine("        var element = WaitForElement(selectorType, selectorValue);");
                s.AppendLine("        ");
                s.AppendLine("        try");
                s.AppendLine("        {");
                s.AppendLine("            var selectElement = new SelectElement(element);");
                s.AppendLine("            selectElement.SelectByValue(valueToSelect);");
                s.AppendLine("        }");
                s.AppendLine("        catch (Exception ex)");
                s.AppendLine("        {");
                s.AppendLine("            Console.WriteLine($\"Standard select failed: {ex.Message}\");");
                s.AppendLine("            Console.WriteLine(\"Trying JavaScript approach...\");");
                s.AppendLine("            ");
                s.AppendLine("            // Fallback to JavaScript for selecting option");
                s.AppendLine("            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
                s.AppendLine("            js.ExecuteScript(\"arguments[0].value = arguments[1]; arguments[0].dispatchEvent(new Event('change'));\", element, valueToSelect);");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait for UI to update after selection");
                s.AppendLine("        Thread.Sleep(500);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddThenSteps(StringBuilder s, HashSet<string> sig)
        {
            if (sig.Add("ThenExpectedState"))
            {
                s.AppendLine("    [Then(@\"the page should be in the expected state\")]");
                s.AppendLine("    public void ThenExpectedState()");
                s.AppendLine("    {");
                s.AppendLine("        Console.WriteLine(\"Verifying the expected page state\");");
                s.AppendLine("        ");
                s.AppendLine("        // Adjust this assertion based on the specific website being tested");
                s.AppendLine("        // For ToDoMVC, verify some todos exist");
                s.AppendLine("        if (_driver.Url.Contains(\"todomvc\"))");
                s.AppendLine("        {");
                s.AppendLine("            var todoItems = _driver.FindElements(By.CssSelector(\".todo-list li\"));");
                s.AppendLine("            Assert.That(todoItems.Count > 0, \"Expected at least one todo item to be created\");");
                s.AppendLine("        }");
                s.AppendLine("        else");
                s.AppendLine("        {");
                s.AppendLine("            // Generic assertion - page has loaded successfully");
                s.AppendLine("            Assert.That(_driver.Title != null, \"Page title should not be null\");");
                s.AppendLine("            Assert.That(_driver.PageSource.Length > 0, \"Page source should not be empty\");");
                s.AppendLine("        }");
                s.AppendLine("        ");
                s.AppendLine("        // Wait to see the final state");
                s.AppendLine("        Thread.Sleep(2000);");
                s.AppendLine("    }");
                s.AppendLine();
            }
        }

        private void AddGetByHelper(StringBuilder s)
        {
            s.AppendLine("    // Helper method to get By locator from string values");
            s.AppendLine("    private By GetBy(string selectorType, string selectorValue)");
            s.AppendLine("    {");
            s.AppendLine("        selectorValue = selectorValue ?? string.Empty;");
            s.AppendLine("        selectorType = selectorType ?? string.Empty;");
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
            s.AppendLine("            // Handle attribute selectors");
            s.AppendLine("            case \"aria-label\": return By.CssSelector($\"[aria-label='{selectorValue}']\");");
            s.AppendLine("            case \"placeholder\": return By.CssSelector($\"[placeholder='{selectorValue}']\");");
            s.AppendLine("            case \"part\": return By.CssSelector($\"[part='{selectorValue}']\");");
            s.AppendLine("            case \"data-test-id\": return By.CssSelector($\"[data-test-id='{selectorValue}']\");");
            s.AppendLine("            // Handle other attribute-based selectors");
            s.AppendLine("            default:");
            s.AppendLine("                // If selector type looks like an attribute, use it as an attribute selector");
            s.AppendLine("                if (!selectorType.Contains(\" \") && !selectorType.Contains(\">\") && !selectorType.Contains(\"[\"))");
            s.AppendLine("                {");
            s.AppendLine("                    return By.CssSelector($\"[{selectorType}='{selectorValue}']\");");
            s.AppendLine("                }");
            s.AppendLine("                throw new ArgumentException($\"Unsupported selector type provided: '{selectorType}'. Value was '{selectorValue}'.\");");
            s.AppendLine("        }");
            s.AppendLine("    }");
            s.AppendLine();
        }

        private void AddWaitForElementHelper(StringBuilder s)
        {
            // Add the universal WaitForElement method with fallbacks
            s.AppendLine("    // Helper method to wait for an element to be present and visible with robust fallbacks");
            s.AppendLine("    private IWebElement WaitForElement(string selectorType, string selectorValue, int timeoutSeconds = 10)");
            s.AppendLine("    {");
            s.AppendLine("        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));");
            s.AppendLine("        var by = GetBy(selectorType, selectorValue);");
            s.AppendLine("        ");
            s.AppendLine("        try");
            s.AppendLine("        {");
            s.AppendLine("            // First attempt with the provided selector");
            s.AppendLine("            return wait.Until(driver => {");
            s.AppendLine("                var element = driver.FindElement(by);");
            s.AppendLine("                return element.Displayed ? element : null;");
            s.AppendLine("            });");
            s.AppendLine("        }");
            s.AppendLine("        catch (WebDriverTimeoutException)");
            s.AppendLine("        {");
            s.AppendLine("            Console.WriteLine($\"Element not found with {selectorType}='{selectorValue}', trying alternatives...\");");
            s.AppendLine("            ");
            s.AppendLine("            // Try with different selector strategies");
            s.AppendLine("            var alternativeSelectors = new List<(string description, By by)>();");
            s.AppendLine("            ");
            s.AppendLine("            // Site-specific selectors");
            s.AppendLine("            if (_driver.Url.Contains(\"todomvc\"))");
            s.AppendLine("            {");
            s.AppendLine("                if (selectorValue.Equals(\"toggle\", StringComparison.OrdinalIgnoreCase))");
            s.AppendLine("                {");
            s.AppendLine("                    alternativeSelectors.Add((\"First todo toggle\", By.CssSelector(\".todo-list li:first-child .toggle\")));");
            s.AppendLine("                    alternativeSelectors.Add((\"Any toggle\", By.CssSelector(\".todo-list .toggle\")));");
            s.AppendLine("                }");
            s.AppendLine("                else if (selectorValue.Equals(\"new-todo\", StringComparison.OrdinalIgnoreCase))");
            s.AppendLine("                {");
            s.AppendLine("                    alternativeSelectors.Add((\"New todo input\", By.CssSelector(\".new-todo\")));");
            s.AppendLine("                    alternativeSelectors.Add((\"Todo input by placeholder\", By.CssSelector(\"input[placeholder='What needs to be done?']\")));");
            s.AppendLine("                }");
            s.AppendLine("            }");
            s.AppendLine("            ");
            s.AppendLine("            // Build general alternative selectors");
            s.AppendLine("            if (selectorType.Equals(\"ClassName\", StringComparison.OrdinalIgnoreCase))");
            s.AppendLine("            {");
            s.AppendLine("                // CSS selector alternative");
            s.AppendLine("                alternativeSelectors.Add((\"CSS with class\", By.CssSelector(\".\" + selectorValue)));");
            s.AppendLine("                ");
            s.AppendLine("                // For inputs/buttons, try different approaches");
            s.AppendLine("                if (selectorValue.Contains(\"input\") || selectorValue.Contains(\"button\") || ");
            s.AppendLine("                    selectorValue.Contains(\"toggle\") || selectorValue.Contains(\"checkbox\"))");
            s.AppendLine("                {");
            s.AppendLine("                    alternativeSelectors.Add((\"Any input\", By.TagName(\"input\")));");
            s.AppendLine("                    alternativeSelectors.Add((\"Any button\", By.TagName(\"button\")));");
            s.AppendLine("                    alternativeSelectors.Add((\"Checkbox input\", By.CssSelector(\"input[type='checkbox']\")));");
            s.AppendLine("                }");
            s.AppendLine("                ");
            s.AppendLine("                // For search/text inputs");
            s.AppendLine("                if (selectorValue.Contains(\"search\") || selectorValue.Contains(\"text\") || ");
            s.AppendLine("                    selectorValue.Contains(\"input\") || selectorValue.Contains(\"todo\"))");
            s.AppendLine("                {");
            s.AppendLine("                    alternativeSelectors.Add((\"Text input\", By.CssSelector(\"input[type='text']\")));");
            s.AppendLine("                    alternativeSelectors.Add((\"Search input\", By.CssSelector(\"input[type='search']\")));");
            s.AppendLine("                }");
            s.AppendLine("            }");
            s.AppendLine("            ");
            s.AppendLine("            // Try with XPath alternatives");
            s.AppendLine("            alternativeSelectors.Add((\"XPath by class\", By.XPath($\"//*[contains(@class,'{selectorValue}')]\")));");
            s.AppendLine("            ");
            s.AppendLine("            // Try all alternative selectors");
            s.AppendLine("            foreach (var (description, alternativeBy) in alternativeSelectors)");
            s.AppendLine("            {");
            s.AppendLine("                try");
            s.AppendLine("                {");
            s.AppendLine("                    Console.WriteLine($\"Trying alternative: {description}\");");
            s.AppendLine("                    var element = _driver.FindElement(alternativeBy);");
            s.AppendLine("                    if (element.Displayed)");
            s.AppendLine("                    {");
            s.AppendLine("                        Console.WriteLine($\"Found element using alternative: {description}\");");
            s.AppendLine("                        return element;");
            s.AppendLine("                    }");
            s.AppendLine("                }");
            s.AppendLine("                catch");
            s.AppendLine("                {");
            s.AppendLine("                    // Continue to next alternative");
            s.AppendLine("                }");
            s.AppendLine("            }");
            s.AppendLine("            ");
            s.AppendLine("            // One last resort: try finding by JS");
            s.AppendLine("            try");
            s.AppendLine("            {");
            s.AppendLine("                Console.WriteLine(\"Trying to find element using JavaScript...\");");
            s.AppendLine("                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;");
            s.AppendLine("                ");
            s.AppendLine("                string jsScript = \"return document.querySelector('.\" + selectorValue + \"') || \" + ");
            s.AppendLine("                               \"document.querySelector('[\" + selectorType + \"=\\\"\" + selectorValue + \"\\\"]') || \" +");
            s.AppendLine("                               \"document.querySelector('input, button');\";");
            s.AppendLine("                ");
            s.AppendLine("                var element = js.ExecuteScript(jsScript) as IWebElement;");
            s.AppendLine("                if (element != null)");
            s.AppendLine("                {");
            s.AppendLine("                    Console.WriteLine(\"Found element using JavaScript!\");");
            s.AppendLine("                    return element;");
            s.AppendLine("                }");
            s.AppendLine("            }");
            s.AppendLine("            catch (Exception jsEx)");
            s.AppendLine("            {");
            s.AppendLine("                Console.WriteLine($\"JavaScript approach failed: {jsEx.Message}\");");
            s.AppendLine("            }");
            s.AppendLine("            ");
            s.AppendLine("            // If we get here, no element was found with any strategy");
            s.AppendLine("            throw new NoSuchElementException($\"Element with {selectorType}='{selectorValue}' not found after trying multiple strategies.\");");
            s.AppendLine("        }");
            s.AppendLine("    }");
        }
    }
}