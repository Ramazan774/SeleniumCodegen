using System;
using System.IO;
using System.Text.RegularExpressions;

namespace SpecFlowTestGenerator.Utils
{
    /// <summary>
    /// Provides file operations helper methods
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        /// Sanitizes a string to be used as a filename
        /// </summary>
        public static string SanitizeForFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "InvalidFeatureName";

            string invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            string regex = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            string sanitized = Regex.Replace(input, regex, "_");
            sanitized = sanitized.Trim('_', ' ');

            if (string.IsNullOrWhiteSpace(sanitized))
                return "SanitizedFeatureName";

            if (!char.IsLetter(sanitized[0]) && sanitized[0] != '_')
                sanitized = "_" + sanitized;

            sanitized = Regex.Replace(sanitized, @"_+", "_");
            return sanitized;
        }

        /// <summary>
        /// Ensures a directory exists, creating it if necessary
        /// </summary>
        public static bool EnsureDirectoryExists(string path)
        {
            try
            {
                Directory.CreateDirectory(path);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error creating directory '{path}': {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Writes text to a file, creating directories as needed
        /// </summary>
        public static bool WriteTextFile(string filePath, string content)
        {
            try
            {
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                File.WriteAllText(filePath, content);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log($"Error writing file '{filePath}': {ex.Message}");
                return false;
            }
        }
    }
}
