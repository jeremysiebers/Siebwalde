using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace SiebwaldeApp
{
    /// <summary>
    /// The standard log factory for SiebwaldeApp
    /// Logs details to the Debug, Console, Trace and log files
    /// </summary>
    public class BaseLogFactory : ILogFactory
    {
        #region Protected Methods

        /// <summary>
        /// THe lost of loggers inthis factory
        /// </summary>
        protected List<ILogger> mLoggers = new List<ILogger>();

        /// <summary>
        /// A lock for the loggers list to keep it thread safe
        /// </summary>
        protected object mLoggersLock = new object();

        #endregion

        #region Public properties

        /// <summary>
        /// The level of logging to output
        /// </summary>
        public LogFactoryLevel LogOutputLevel { get; set; }

        /// <summary>
        /// If true, includes the origin of where the log message was logged from
        /// such as the class name, line number and file name
        /// </summary>
        public bool IncludeLogOriginDetails { get; set; } = true;

        #endregion

        #region Events

        /// <summary>
        /// Fires whenever a new log arrives
        /// </summary>
        public event Action<(string Message, LogFactoryLevel Level)> NewLog = (details) => { };

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// </summary>
        public BaseLogFactory()
        {
            // Add console logger
            AddLogger(new ConsoleLogger());
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Adds the specific logger to this factory
        /// </summary>
        /// <param name="logger">The logger</param>
        public void AddLogger(ILogger logger)
        {
            // Lock the list so it is thread-safe
            lock (mLoggersLock)
            {
                if (!mLoggers.Contains(logger))
                    // Add the logger to the list
                    mLoggers.Add(logger);
            }
        }

        /// <summary>
        /// Removes the specified logger from this factory
        /// </summary>
        /// <param name="logger">The logger</param>
        public void RemoveLogger(ILogger logger)
        {
            // Lock the list so it is thread-safe
            lock (mLoggersLock)
            {
                if (mLoggers.Contains(logger))
                    // Remove the logger to the list
                    mLoggers.Remove(logger);
            }
        }

        /// <summary>
        /// Logs the specific message to all loggers in this factory
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="level">The level of the message being logged</param>
        /// <param name="origin">The method/function this message was logged in</param>
        /// <param name="filepath">The code filename that this message was logged from</param>
        /// <param name="linenumber">The line of code in the filename this message was logged from</param>
        public void Log(
            string message, 
            LogFactoryLevel level = LogFactoryLevel.Informative, 
            [CallerMemberName]string origin = "", 
            [CallerFilePath]string filepath = "", 
            [CallerLineNumber]int linenumber = 0)
        {
            // If the user wants to know where the log originated from...
            if (IncludeLogOriginDetails)
                message = $"[{Path.GetFileName(filepath)} > {origin}() > line {linenumber}] {message}";

            // Log to all loggers
            mLoggers.ForEach(logger => logger.Log(message, level));

            // Informs listeners
            NewLog.Invoke((message, level));
        }

        #endregion
    }
}
