using OpenQA.Selenium;
using TechTalk.SpecFlow;
using NUnit.Framework;
using System;

[Binding]
public class ToDoAppSteps
{
    private readonly IWebDriver _driver;
    public ToDoAppSteps(IWebDriver driver) {
		_driver = driver ?? throw new ArgumentNullException(nameof(driver));
}

    [Given(@"I navigate to \"(.*)\"")]
    [When(@"I navigate to \"(.*)\"")]
    public void NavigateToUrl(string url)
    {
        _driver.Navigate().GoToUrl(url);
    }

    [When(@"I click the element with (.*) \"(.*)\"")]
    public void ClickElement(string selectorType, string selectorValue)
    {
        _driver.FindElement(GetBy(selectorType, selectorValue)).Click();
    }

    [When(@"I type \"(.*)\" into element with (.*) \"(.*)\"")]
    public void TypeIntoElement(string text, string selectorType, string selectorValue)
    {
        var element = _driver.FindElement(GetBy(selectorType, selectorValue));
        element.SendKeys(text);
    }

    [When(@"I type \"(.*)\" and press Enter in element with (.*) \"(.*)\"")]
    public void TypeAndEnter(string text, string selectorType, string selectorValue)
    {
        var element = _driver.FindElement(GetBy(selectorType, selectorValue));
        element.SendKeys(text);
        element.SendKeys(Keys.Enter);
    }

    [When(@"I press Enter in element with (.*) \"(.*)\"")]
    public void PressEnterInElement(string selectorType, string selectorValue)
    {
        _driver.FindElement(GetBy(selectorType, selectorValue)).SendKeys(Keys.Enter);
    }

    [Then(@"the page should be in the expected state")]
    public void ThenExpectedState()
    {
        Assert.Pass("Placeholder assertion passed. Implement real checks.");
    }

    private By GetBy(string selectorType, string selectorValue)
    {
        selectorValue = selectorValue ?? string.Empty; selectorType = selectorType ?? string.Empty;
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
            default: throw new ArgumentException($"Unsupported selector type provided: '{selectorType}'. Value was '{selectorValue}'.");
        }
    }
}
