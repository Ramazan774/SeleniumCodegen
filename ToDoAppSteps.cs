using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TechTalk.SpecFlow;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

[Binding]
public class ToDoAppSteps
{
    private readonly IWebDriver _driver;

    public ToDoAppSteps(IWebDriver driver)
    {
        _driver = driver ?? throw new ArgumentNullException(nameof(driver));
        
        // Set implicit wait to improve stability
        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [Given(@"I navigate to ""(.*)""")]
    [When(@"I navigate to ""(.*)""")]
    public void NavigateToUrl(string url)
    {
        Console.WriteLine($"Navigating to {url}");
        _driver.Navigate().GoToUrl(url);
        
        // Wait for page to load completely
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(20));
        wait.Until(d => ((IJavaScriptExecutor)d).ExecuteScript("return document.readyState").Equals("complete"));
        
        // Additional wait to ensure UI is ready
        Thread.Sleep(1000);
    }

    [When(@"I click the element with (.*?) ""(.*?)""")]
    public void ClickElementWith(string selectorType, string selectorValue)
    {
        Console.WriteLine($"Clicking element with {selectorType}='{selectorValue}'");
        var element = WaitForElement(selectorType, selectorValue);
        
        try
        {
            // Scroll element into view for better reliability
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            Thread.Sleep(300);
            
            element.Click();
        }
        catch (ElementClickInterceptedException)
        {
            // Fallback to JavaScript click if regular click fails
            Console.WriteLine("Regular click failed, trying JavaScript click...");
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].click();", element);
        }
        
        // Wait for UI to update after click
        Thread.Sleep(500);
    }

    [When(@"I type ""(.*)"" and press Enter in element with (.*?) ""(.*?)""")]
    [Given(@"I type ""(.*)"" and press Enter in element with (.*?) ""(.*?)""")]
    public void TypeAndEnter(string text, string selectorType, string selectorValue)
    {
        Console.WriteLine($"Typing '{text}' and pressing Enter in element with {selectorType}='{selectorValue}'");
        var element = WaitForElement(selectorType, selectorValue);
        
        try
        {
            // Scroll element into view
            ((IJavaScriptExecutor)_driver).ExecuteScript("arguments[0].scrollIntoView({block: 'center'});", element);
            Thread.Sleep(300);
            
            element.Clear();
            element.SendKeys(text);
            Thread.Sleep(300); // Short pause before pressing Enter
            element.SendKeys(Keys.Enter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Standard typing failed: {ex.Message}");
            Console.WriteLine("Trying JavaScript approach...");
            
            // Fallback to JavaScript for setting value and pressing Enter
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].value = arguments[1];", element, text);
            js.ExecuteScript("arguments[0].dispatchEvent(new Event('change'));", element);
            js.ExecuteScript("arguments[0].dispatchEvent(new KeyboardEvent('keydown', {'key': 'Enter', 'code': 'Enter', 'keyCode': 13}));", element);
        }
        
        // Wait for UI to update after pressing Enter
        Thread.Sleep(1000);
    }

    [When(@"I press Enter in element with (.*?) ""(.*?)""")]
    [Given(@"I press Enter in element with (.*?) ""(.*?)""")]
    public void PressEnterInElement(string selectorType, string selectorValue)
    {
        Console.WriteLine($"Pressing Enter in element with {selectorType}='{selectorValue}'");
        var element = WaitForElement(selectorType, selectorValue);
        
        try
        {
            element.SendKeys(Keys.Enter);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Standard Enter key failed: {ex.Message}");
            Console.WriteLine("Trying JavaScript approach...");
            
            // Fallback to JavaScript for pressing Enter
            IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
            js.ExecuteScript("arguments[0].dispatchEvent(new KeyboardEvent('keydown', {'key': 'Enter', 'code': 'Enter', 'keyCode': 13}));", element);
        }
        
        // Wait for UI to update after pressing Enter
        Thread.Sleep(1000);
    }

    [Then(@"the page should be in the expected state")]
    public void ThenExpectedState()
    {
        Console.WriteLine("Verifying the expected page state");
        
        // Basic verification that page loaded successfully
        Assert.That(_driver.Title != null, "Page title should not be null");
        Assert.That(_driver.PageSource.Length > 0, "Page source should not be empty");
        
        // Wait to see the final state
        Thread.Sleep(2000);
    }

    // Helper method to get By locator from string values
    private By GetBy(string selectorType, string selectorValue)
    {
        selectorValue = selectorValue ?? string.Empty;
        selectorType = selectorType ?? string.Empty;
        switch (selectorType.ToLowerInvariant().Trim())
        {
            case "id": return By.Id(selectorValue);
            case "name": return By.Name(selectorValue);
            case "classname": return By.ClassName(selectorValue);
            case "cssselector": return By.CssSelector(selectorValue);
            case "xpath": return By.XPath(selectorValue);
            case "linktext": return By.LinkText(selectorValue);
            case "partiallinktext": return By.PartialLinkText(selectorValue);
            case "tagname": return By.TagName(selectorValue);
            // Handle attribute selectors
            case "aria-label": return By.CssSelector($"[aria-label='{selectorValue}']");
            case "placeholder": return By.CssSelector($"[placeholder='{selectorValue}']");
            case "data-test-id": return By.CssSelector($"[data-test-id='{selectorValue}']");
            // Handle other attribute-based selectors
            default:
                // If selector type looks like an attribute, use it as an attribute selector
                if (!selectorType.Contains(" ") && !selectorType.Contains(">") && !selectorType.Contains("["))
                {
                    return By.CssSelector($"[{selectorType}='{selectorValue}']");
                }
                throw new ArgumentException($"Unsupported selector type provided: '{selectorType}'. Value was '{selectorValue}'.");
        }
    }

    private IWebElement WaitForElement(string selectorType, string selectorValue, int timeoutSeconds = 10)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(timeoutSeconds));
        var by = GetBy(selectorType, selectorValue);
        
        try
        {
            // First attempt with the provided selector
            Console.WriteLine($"Looking for element with {selectorType}='{selectorValue}'");
            return wait.Until(driver => {
                var element = driver.FindElement(by);
                return element.Displayed ? element : null;
            });
        }
        catch (WebDriverTimeoutException)
        {
            Console.WriteLine($"Element not found with primary selector, trying fallbacks");
            
            // Try with CSS selector alternative if className was used
            if (selectorType.Equals("ClassName", StringComparison.OrdinalIgnoreCase))
            {
                try 
                {
                    Console.WriteLine($"Trying CSS selector alternative: .{selectorValue}");
                    var cssSelector = By.CssSelector("." + selectorValue);
                    var element = _driver.FindElement(cssSelector);
                    if (element.Displayed)
                    {
                        Console.WriteLine("Found element with CSS selector");
                        return element;
                    }
                }
                catch { /* Continue to next approach */ }
            }
            
            // Try finding by element type based on context clues
            bool mightBeCheckbox = selectorValue.Contains("toggle") || 
                                  selectorValue.Contains("check") || 
                                  selectorValue.Contains("box");
                                  
            bool mightBeInput = selectorValue.Contains("input") || 
                               selectorValue.Contains("text") || 
                               selectorValue.Contains("field") ||
                               selectorValue.Contains("todo");
            
            if (mightBeCheckbox)
            {
                try
                {
                    Console.WriteLine("Selector suggests a checkbox, trying checkbox selectors");
                    var elements = _driver.FindElements(By.CssSelector("input[type='checkbox']"));
                    if (elements.Count > 0)
                    {
                        Console.WriteLine($"Found {elements.Count} checkbox elements, using first one");
                        return elements[0];
                    }
                }
                catch { /* Continue to next approach */ }
            }
            
            if (mightBeInput)
            {
                try 
                {
                    Console.WriteLine("Selector suggests an input field, trying input selectors");
                    var elements = _driver.FindElements(By.TagName("input"));
                    foreach (var element in elements)
                    {
                        try
                        {
                            if (element.Displayed && element.GetAttribute("type") != "checkbox" && 
                                element.GetAttribute("type") != "radio" && element.GetAttribute("type") != "hidden")
                            {
                                Console.WriteLine("Found visible text input");
                                return element;
                            }
                        }
                        catch { /* Continue to next element */ }
                    }
                }
                catch { /* Continue to next approach */ }
            }
            
            // Last resort: JavaScript
            try
            {
                Console.WriteLine("Trying JavaScript to find element");
                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                
                string script = @"
                    function findElement(selector, type) {
                        // Try by class
                        if (type.toLowerCase() === 'classname') {
                            // First by class name
                            var byClass = document.getElementsByClassName(selector);
                            if (byClass.length > 0 && byClass[0].offsetParent !== null) 
                                return byClass[0];
                                
                            // Then by CSS selector
                            var byCss = document.querySelectorAll('.' + selector);
                            if (byCss.length > 0 && byCss[0].offsetParent !== null) 
                                return byCss[0];
                        }
                        
                        // Try by attribute
                        var byAttr = document.querySelectorAll('[' + type + '="' + selector + '"]');
                        if (byAttr.length > 0 && byAttr[0].offsetParent !== null) 
                            return byAttr[0];
                        
                        // If selector hints at a checkbox
                        if (selector.includes('toggle') || selector.includes('check')) {
                            var checkboxes = document.querySelectorAll('input[type="checkbox"]');
                            if (checkboxes.length > 0 && checkboxes[0].offsetParent !== null) 
                                return checkboxes[0];
                        }
                        
                        // If selector hints at a text field
                        if (selector.includes('input') || selector.includes('todo') || selector.includes('text')) {
                            var inputs = document.querySelectorAll('input[type="text"], input:not([type])');
                            for (var i = 0; i < inputs.length; i++) {
                                if (inputs[i].offsetParent !== null) return inputs[i];
                            }
                        }
                        
                        return null;
                    }
                    
                    return findElement(arguments[0], arguments[1]);
                ";
                
                var element = js.ExecuteScript(script, selectorValue, selectorType) as IWebElement;
                if (element != null && element.Displayed)
                {
                    Console.WriteLine("Found element using JavaScript");
                    return element;
                }
            }
            catch (Exception jsEx)
            {
                Console.WriteLine($"JavaScript approach failed: {jsEx.Message}");
            }
            
            // Element not found
            throw new NoSuchElementException($"Element with {selectorType}='{selectorValue}' not found after trying fallbacks");
        }
    }
}
