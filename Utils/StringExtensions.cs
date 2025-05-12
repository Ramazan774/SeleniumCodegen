using System;

namespace WebDriverCdpRecorder.Utils
{
    public static class StringExtensions
    {
        public static string SafeTrim(this string? value)
        {
            return value?.Trim() ?? string.Empty;
        }

        public static string SafeToLower(this string? value)
        {
            return value?.ToLowerInvariant() ?? string.Empty;
        }

        public static bool StartsWithIgnoreCase(this string? value, string prefix)
        {
            if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(prefix))
                return false;
                
            return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
        }
    }
}