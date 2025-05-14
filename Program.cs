using System;
using System.Threading.Tasks;
using WebDriverCdpRecorder.Core;
using WebDriverCdpRecorder.Utils;

namespace WebDriverCdpRecorder
{
    /// <summary>
    /// Main entry point for the WebDriverCdpRecorder application
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("===== WebDriverCdpRecorder =====");
            Console.WriteLine("Starting application...");

            string featureName = args.Length > 0 ? args[0] : "MyFeature";
            
            // Create and initialize recorder engine
            var recorder = new RecorderEngine(featureName);
            
            try
            {
                // Initialize the recorder
                if (!await recorder.Initialize())
                {
                    Console.WriteLine("Failed to initialize. Exiting...");
                    return;
                }

                // Start recording
                recorder.StartRecording();
                
                // Main recording loop
                while (recorder.IsRecording)
                {
                    if (Console.KeyAvailable)
                    {
                        string? input = Console.ReadLine()?.Trim();
                        recorder.ProcessCommand(input);
                    }
                    
                    await Task.Delay(100);
                }
                
                // Generate files for recorded actions
                recorder.GenerateCurrentFeatureFiles();
            }
            catch (Exception ex)
            {
                Logger.Log($"FAIL: An error occurred: {ex.Message}");
                Logger.Log($"Details: {ex}");
            }
            finally
            {
                // Print event handler logs
                Console.WriteLine("\n--- C# Event Handler Console Logs ---");
                foreach (var log in Logger.GetLogBuffer())
                {
                    Console.WriteLine(log);
                }
                Console.WriteLine("------------------------------------\n");
                
                // Clean up resources
                await recorder.CleanUp();
            }
            
            Console.WriteLine("\n--- Recorder Finished ---");
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
    }
}