namespace WebDriverCdpRecorder.Models
{
    /// <summary>
    /// Represents an action sent from JavaScript to C#
    /// </summary>
    public record ActionFromJs(
        string Type, 
        string? Selector, 
        string? Value, 
        string? Key, 
        string? TagName, 
        string? ElementType);
}