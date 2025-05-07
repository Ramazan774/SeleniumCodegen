namespace WebDriverCdpRecorder.Models
{
    /// <summary>
    /// Represents a user action captured during recording
    /// </summary>
    public record RecordedAction(
        string ActionType, 
        string? SelectorType, 
        string? SelectorValue, 
        string? Value, 
        string? TagName = null, 
        string? ElementType = null);
}