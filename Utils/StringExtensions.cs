using System;

namespace WebDriverCdpRecorder.Utils
{
    /// <summary>
    /// Extension methods for string operations
    /// </summary>
    public static class StringExtensions
    {
        /// <summary>
        /// Safely trims a string that might be null
        /// </summary>
        public static string SafeTrim(this string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        /// <summary>
        /// Safely converts a string to lowercase
        /// </summary>
        public static string SafeToLower(this string? value)
        {
            return value?.ToLowerInvariant() ?? string.Empty;
        }

        /// <summary>
        /// Check if string starts with value (case insensitive)
        /// </summary>
        public static bool StartsWithIgnoreCase(this string? value, string prefix)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(prefix))
                return false;
                
            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}