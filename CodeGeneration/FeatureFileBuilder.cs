using System;
using System.Collections.Generic;
using System.Text;
using SpecFlowTestGenerator.Models;

namespace SpecFlowTestGenerator.CodeGeneration
{
    /// <summary>
    /// Builds SpecFlow feature files from recorded actions
    /// </summary>
    public class FeatureFileBuilder
    {
        /// <summary>
        /// Build the content of a SpecFlow feature file
        /// </summary>
        public string BuildFeatureFileContent(List<RecordedAction> actions, string featureName)
        {
            StringBuilder featureFile = new StringBuilder();
            
            // Add feature header
            featureFile.AppendLine($"Feature: {featureName}");
            featureFile.AppendLine();
            featureFile.AppendLine($"Scenario: Perform recorded actions on {featureName}");

            string? lastValue = null;
            bool lastActionWasSendKeys = false;
            
            // Process actions
            var actionsCopy = new List<RecordedAction>(actions);
            for (int i = 0; i < actionsCopy.Count; i++)
            {
                var action = actionsCopy[i];
                bool isLastActionInLoop = (i == actionsCopy.Count - 1);

                // Handle pending SendKeys before processing current action
                if (lastValue != null && action.ActionType != "SendKeysEnter")
                {
                    int prevIndex = i - 1;
                    if (prevIndex >= 0 && actionsCopy[prevIndex].ActionType == "SendKeys" && actionsCopy[prevIndex].Value == lastValue)
                    {
                        var sendKeysAction = actionsCopy[prevIndex];
                        featureFile.AppendLine($"\tAnd I type \"{lastValue}\" into element with {sendKeysAction.SelectorType} \"{sendKeysAction.SelectorValue}\"");
                        lastValue = null; // Consumed
                    }
                }

                switch (action.ActionType)
                {
                    case "Navigate":
                        featureFile.AppendLine($"\tGiven I navigate to \"{action.Value}\"");
                        lastActionWasSendKeys = false;
                        break;
                        
                    case "Click":
                        featureFile.AppendLine($"\tWhen I click the element with {action.SelectorType} \"{action.SelectorValue}\"");
                        lastActionWasSendKeys = false;
                        break;
                        
                    case "SendKeys":
                        lastValue = action.Value;
                        lastActionWasSendKeys = true;
                        break;
                        
                    case "SendKeysEnter":
                        string valueToUse = lastValue ?? action.Value ?? "";
                        if (!string.IsNullOrEmpty(valueToUse))
                        {
                            featureFile.AppendLine($"\tAnd I type \"{valueToUse}\" and press Enter in element with {action.SelectorType} \"{action.SelectorValue}\"");
                        }
                        else
                        {
                            featureFile.AppendLine($"\tAnd I press Enter in element with {action.SelectorType} \"{action.SelectorValue}\"");
                        }
                        lastValue = null; 
                        lastActionWasSendKeys = false;
                        break;
                        
                    case "SelectOption":
                        featureFile.AppendLine($"\tAnd I select option with value \"{action.Value}\" from element with {action.SelectorType} \"{action.SelectorValue}\"");
                        lastActionWasSendKeys = false;
                        break;
                }

                // Handle last action if it was SendKeys and wasn't followed by Enter
                if (isLastActionInLoop && lastActionWasSendKeys && lastValue != null)
                {
                    featureFile.AppendLine($"\tAnd I type \"{lastValue}\" into element with {action.SelectorType} \"{action.SelectorValue}\"");
                }
            }
            
            featureFile.AppendLine("\tThen the page should be in the expected state");
            
            return featureFile.ToString();
        }
    }
}