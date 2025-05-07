using System;
using System.Collections.Generic;
using System.IO;
using WebDriverCdpRecorder.Models;
using WebDriverCdpRecorder.Utils;

namespace WebDriverCdpRecorder.CodeGeneration
{
    /// <summary>
    /// Generates SpecFlow feature and steps files from recorded actions
    /// </summary>
    public class SpecFlowGenerator
    {
        private readonly FeatureFileBuilder _featureBuilder;
        private readonly StepsFileBuilder _stepsBuilder;

        /// <summary>
        /// Constructor
        /// </summary>
        public SpecFlowGenerator()
        {
            _featureBuilder = new FeatureFileBuilder();
            _stepsBuilder = new StepsFileBuilder();
        }

        /// <summary>
        /// Generate SpecFlow files from recorded actions
        /// </summary>
        public void GenerateFiles(List<RecordedAction> actions, string featureName, string outputDir)
        {
            if (actions == null || actions.Count == 0)
            {
                Logger.Log($"No actions to generate files for feature '{featureName}'.");
                return;
            }

            string safeFeatureName = FileHelper.SanitizeForFileName(featureName);
            string stepsClassName = $"{safeFeatureName}Steps";

            try
            {
                // Create feature file content
                string featureContent = _featureBuilder.BuildFeatureFileContent(actions, safeFeatureName);
                
                // Create steps file content
                string stepsContent = _stepsBuilder.BuildStepsFileContent(actions, stepsClassName);

                // Ensure directory exists
                FileHelper.EnsureDirectoryExists(outputDir);
                
                // Write files
                string featureFilePath = Path.Combine(outputDir, $"{safeFeatureName}.feature");
                string stepsFilePath = Path.Combine(outputDir, $"{stepsClassName}.cs");
                
                File.WriteAllText(featureFilePath, featureContent);
                File.WriteAllText(stepsFilePath, stepsContent);
                
                Logger.Log($"Generated: {featureFilePath}");
                Logger.Log($"Generated: {stepsFilePath}");
            }
            catch (Exception ex)
            {
                Logger.Log($"ERROR generating files: {ex.Message}");
            }
        }
    }
}
