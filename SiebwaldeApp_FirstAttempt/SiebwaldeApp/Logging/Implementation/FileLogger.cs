using System;

namespace SiebwaldeApp
{ 
    /// <summary>
    /// Logs to a specific file
    /// </summary>
    public class FileLogger : ILogger
    {
        #region Public properties

        /// <summary>
        /// The path to write the log file to
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// The LoggerInstance to write the log file to
        /// </summary>
        public string LoggerInstance { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <paramref name="filepath"/>The path to log to</param>
        /// <param name="loggerinstance">THe logger instance to decide if logging is applicable for this logger</param>
        /// </summary>
        public FileLogger(string filepath, string loggerinstance)
        {
            // Set the file property
            FilePath = filepath;

            // Set the file logger instance property
            LoggerInstance = loggerinstance;
        }

        #endregion

        #region Logger methods

        /// <summary>
        /// Logs the given message to the log file
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The log level</param>
        /// <param name="loggerinstance">THe logger instance to decide if logging is applicable for this logger</param>
        public void Log(string message, LogLevel level, string loggerinstance)
        {
            // If the log is meant for this instance of file logger, log it
            if (loggerinstance != LoggerInstance)
                return;

            IoC.File.WriteAllTextToFileAsync(message + Environment.NewLine, FilePath, append: true);
        }

        #endregion


    }
}
