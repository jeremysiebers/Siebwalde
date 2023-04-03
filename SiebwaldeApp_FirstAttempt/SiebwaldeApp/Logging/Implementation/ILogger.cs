namespace SiebwaldeApp
{
    /// <summary>
    /// A logger that will output or handle log messages from a <see cref="ILogFactory"/>
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Handles the logged message being passed in
        /// </summary>
        /// <param name="message">The message being logged</param>
        /// <param name="level">Log level of the log message</param>
        /// <param name="loggerinstance">The logger instance to log to</param>
        void Log(string message, LogLevel level, string loggerinstance);
    }
}
