using System;

namespace SpecFlowTestGenerator.Models
{
    /// <summary>
    /// Represents a recorded browser action
    /// </summary>
    public class RecordedAction
    {
        /// <summary>
        /// Gets or sets the type of action
        /// </summary>
        public string ActionType { get; set; }

        /// <summary>
        /// Gets or sets the selector type
        /// </summary>
        public string? SelectorType { get; set; }

        /// <summary>
        /// Gets or sets the selector value
        /// </summary>
        public string? SelectorValue { get; set; }

        /// <summary>
        /// Gets or sets the input value
        /// </summary>
        public string? Value { get; set; }

        /// <summary>
        /// Gets or sets the element tag name
        /// </summary>
        public string? TagName { get; set; }

        /// <summary>
        /// Gets or sets the element type
        /// </summary>
        public string? ElementType { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when this action was recorded
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public RecordedAction(string actionType, string? selectorType, string? selectorValue, string? value, string? tagName = null, string? elementType = null)
        {
            ActionType = actionType;
            SelectorType = selectorType;
            SelectorValue = selectorValue;
            Value = value;
            TagName = tagName;
            ElementType = elementType;
            Timestamp = DateTime.Now;
        }
    }
}