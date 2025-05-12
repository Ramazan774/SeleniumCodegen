using System;
using OpenQA.Selenium;
using WebDriverCdpRecorder.Utils;

namespace WebDriverCdpRecorder.Browser
{
    public class BrowserFactory
    {
        public static IWebDriver CreateBrowser(string browserType = "chrome")
        {
            Logger.Log($"Creating {browserType} browser...");
            
            try
            {
                WebDriverFactory.BrowserType type = WebDriverFactory.BrowserType.Chrome;
                
                // Parse the browser type (case insensitive)
                if (!string.IsNullOrEmpty(browserType))
                {
                    switch (browserType.ToLowerInvariant())
                    {
                        case "firefox":
                            type = WebDriverFactory.BrowserType.Firefox;
                            break;
                        case "edge":
                            type = WebDriverFactory.BrowserType.Edge;
                            break;
                        case "chrome":
                        default:
                            type = WebDriverFactory.BrowserType.Chrome;
                            break;
                    }
                }
                
                IWebDriver driver = WebDriverFactory.CreateDriver(type);
                Logger.Log($"SUCCESS: {browserType} browser initialized!");
                return driver;
            }
            catch (Exception ex)
            {
                Logger.Log($"FAIL: Failed to initialize {browserType} browser: {ex.Message}");
                return null;
            }
        }

        public static void SafeQuit(IWebDriver driver)
        {
            if (driver == null)
                return;

            Logger.Log("Attempting to quit driver...");
            try
            {
                driver.Quit();
                Logger.Log("Driver quit successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log($"Error quitting driver: {ex.Message}");
            }
        }
    }
}