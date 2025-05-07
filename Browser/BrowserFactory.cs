using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using WebDriverCdpRecorder.Utils;

namespace WebDriverCdpRecorder.Browser
{
    /// <summary>
    /// Factory for creating and configuring browser instances
    /// </summary>
    public class BrowserFactory
    {
        /// <summary>
        /// Creates a new Chrome driver instance with the specified options
        /// </summary>
        public static (IWebDriver? Driver, ChromeDriverService? Service) CreateChromeDriver()
        {
            ChromeOptions options = new ChromeOptions();
            options.AddArgument("--start-maximized");
            Logger.Log("ChromeOptions created.");

            Logger.Log("Attempting to initialize ChromeDriver...");
            try
            {
                var service = ChromeDriverService.CreateDefaultService();
                service.EnableVerboseLogging = true;
                service.LogPath = "./chromedriver.log";
                Logger.Log($"ChromeDriverService created. LogPath: {System.IO.Path.GetFullPath(service.LogPath)}");

                var driver = new ChromeDriver(service, options, TimeSpan.FromSeconds(120));
                Logger.Log("SUCCESS: ChromeDriver initialized!");
                
                return (driver, service);
            }
            catch (Exception ex)
            {
                Logger.Log($"FAIL: Failed to initialize ChromeDriver: {ex.Message}");
                return (null, null);
            }
        }

        /// <summary>
        /// Safely quits the driver if it exists
        /// </summary>
        public static void SafeQuit(IWebDriver? driver)
        {
            if (driver == null)
                return;

            Logger.Log("Attempting to quit driver reference...");
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
