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
        public void Log(string message, LogFactoryLevel level)
        {
            // Store the current color
            var consoleOldColor = Console.ForegroundColor;

            // Default log color value
            var consoleColor = ConsoleColor.White;



            // Write message to console
            Console.WriteLine(message);

            // Set old color back
            Console.ForegroundColor = consoleOldColor;
        }
    }
}
