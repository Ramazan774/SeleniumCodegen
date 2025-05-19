using System;
using System.Collections.Generic;

namespace SpecFlowTestGenerator.Utils
{
    /// <summary>
    /// Provides logging functionality for the application
    /// </summary>
    public class Logger
    {
        private static readonly List<string> _consoleLogBuffer = new List<string>();
        private static readonly object _lockObject = new object();

        /// <summary>
        /// Logs a message to console and flushes immediately
        /// </summary>
        public static void Log(string message)
        {
            string formattedMessage = $"{DateTime.Now:HH:mm:ss.fff}-{message}";
            Console.WriteLine(formattedMessage);
            Console.Out.Flush();
        }

        /// <summary>
        /// Logs a message to the internal buffer for event handlers
        /// </summary>
        public static void LogEventHandler(string message)
        {
            string formattedMessage = $"{DateTime.Now:HH:mm:ss.fff}-{message}";
            lock (_lockObject)
            {
                _consoleLogBuffer.Add(formattedMessage);
            }
            System.Diagnostics.Debug.WriteLine(formattedMessage);
        }

        /// <summary>
        /// Gets the current log buffer and clears it
        /// </summary>
        public static List<string> GetAndClearLogBuffer()
        {
            lock (_lockObject)
            {
                var buffer = new List<string>(_consoleLogBuffer);
                _consoleLogBuffer.Clear();
                return buffer;
            }
        }

        /// <summary>
        /// Gets all logs without clearing the buffer
        /// </summary>
        public static List<string> GetLogBuffer()
        {
            lock (_lockObject)
            {
                return new List<string>(_consoleLogBuffer);
            }
        }
    }
}
