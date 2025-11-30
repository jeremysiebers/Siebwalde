using System.Diagnostics;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Logs the nmessages to the console
    /// </summary>
    public class DebugLogger : ILogger
    {
        /// <summary>
        /// Logs the given message to the system console
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The level of the message</param>
        /// <param name="loggerinstance">THe logger instance to decide if logging is applicable for this logger</param>
        public void Log(string message, LogLevel level, string loggerinstance)
        {
            // The default category
            var category = default(string);

            // Color console based on level
            switch (level)
            {
                // Debug is blue
                case LogLevel.Debug:
                    category = "information";
                    break;

                // Verbose is gray
                case LogLevel.Verbose:
                    category = "verbose";
                    break;

                // Warning is orange
                case LogLevel.Warning:
                    category = "warning";
                    break;

                // Error is red
                case LogLevel.Error:
                    category = "error";
                    break;

                // Success is green
                case LogLevel.Success:
                    category = "-----";
                    break;
            }

            // Check if loggerinstance is specified
            if (loggerinstance != "")
            {
                // Write message to console with loggerinstance
                Debug.WriteLine($"{loggerinstance} > {message}", category);
            }
            else
            {
                // Write message to console without loggerinstance
                Debug.WriteLine(message, category);
            }            
        }
    }
}
