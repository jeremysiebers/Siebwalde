using System;

namespace SiebwaldeApp
{
    /// <summary>
    /// Logs the nmessages to the console
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// Logs the given message to the system console
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The level of the message</param>
        /// <param name="loggerinstance">THe logger instance to decide if logging is applicable for this logger</param>
        public void Log(string message, LogLevel level, string loggerinstance)
        {
            // Store the current color
            var consoleOldColor = Console.ForegroundColor;

            // Default log color value
            var consoleColor = ConsoleColor.White;

            // Color console based on level
            switch (level)
            {
                // Debug is blue
                case LogLevel.Debug:
                    consoleColor = ConsoleColor.Blue;
                    break;

                // Verbose is gray
                case LogLevel.Verbose:
                    consoleColor = ConsoleColor.Gray;
                    break;

                // Warning is orange
                case LogLevel.Warning:
                    consoleColor = ConsoleColor.DarkYellow;
                    break;

                // Error is red
                case LogLevel.Error:
                    consoleColor = ConsoleColor.Red;
                    break;

                // Success is green
                case LogLevel.Success:
                    consoleColor = ConsoleColor.Green;
                    break;
            }

            // Set the desired color
            Console.ForegroundColor = consoleColor;

            // Check if loggerinstance is specified
            if (loggerinstance != "")
            {
                // Write message to console with loggerinstance
                Console.WriteLine($"{loggerinstance} > {message}");
            }
            else
            {
                // Write message to console without loggerinstance
                Console.WriteLine(message);
            }

            // Set old color back
            Console.ForegroundColor = consoleOldColor;
        }
    }
}
