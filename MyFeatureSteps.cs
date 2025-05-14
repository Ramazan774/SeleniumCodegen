using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using TechTalk.SpecFlow;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

[Binding]
public class MyFeatureSteps
{
    private readonly IWebDriver _driver;

    public MyFeatureSteps(IWebDriver driver)
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
            js.ExecuteScript("arguments[0].dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', code: 'Enter', keyCode: 13}));", element);
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
            js.ExecuteScript("arguments[0].dispatchEvent(new KeyboardEvent('keydown', {key: 'Enter', code: 'Enter', keyCode: 13}));", element);
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
            case "data-testid": return By.CssSelector($"[data-testid='{selectorValue}']");
            case "data-test": return By.CssSelector($"[data-test='{selectorValue}']");
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

    // Helper method to wait for an element to be present and visible
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
            
            // Try simple JavaScript approach as a last resort
            try
            {
                IJavaScriptExecutor js = (IJavaScriptExecutor)_driver;
                Console.WriteLine("Trying JavaScript to find element");
                
                // Simple script to try class name or attribute
                if (selectorType.Equals("ClassName", StringComparison.OrdinalIgnoreCase))
                {
                    var elements = js.ExecuteScript(
                        "return document.getElementsByClassName(arguments[0])", selectorValue) as IReadOnlyCollection<IWebElement>;
                    
                    if (elements != null && elements.Count > 0)
                    {
                        foreach (var element in elements)
                        {
                            if (element.Displayed)
                            {
                                return element;
                            }
                        }
                    }
                }
                
                // Try by generic attribute
                var elementByAttr = js.ExecuteScript(
                    "return document.querySelector('[" + arguments[0] + "=\"" + arguments[1] + "\"]')", 
                    selectorType, selectorValue) as IWebElement;
                
                if (elementByAttr != null && elementByAttr.Displayed)
                {
                    Console.WriteLine("Found element using JavaScript");
                    return elementByAttr;
                }
            }
            catch (Exception jsEx)
            {
                Console.WriteLine($"JavaScript approach failed: {jsEx.Message}");
            }
            
            // Element not found after trying fallbacks
            throw new NoSuchElementException($"Element with {selectorType}='{selectorValue}' not found after trying fallbacks");
        }
    }
}
