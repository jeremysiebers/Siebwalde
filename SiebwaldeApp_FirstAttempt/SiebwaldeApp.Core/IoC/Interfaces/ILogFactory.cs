﻿using System;
using System.Runtime.CompilerServices;

namespace SiebwaldeApp.Core
{ 
    /// <summary>
    /// Holds a bunch of loggers to log messages for the user
    /// </summary>
    public interface ILogFactory
    {
        #region Properties

        /// <summary>
        /// The level of logging to output
        /// </summary>
        LogOutputLevel LogOutputLevel { get; set; }

        /// <summary>
        /// If true, includes the origin of where the log message was logged from
        /// such as the class name, line number and file name
        /// </summary>
        bool IncludeLogOriginDetails { get; set; }

        #endregion

        #region Events

        /// <summary>
        /// Fires whenever a new log arrives
        /// </summary>
        event Action<(string Message, LogLevel Level, string loggerinstance)> NewLog;

        #endregion       

        #region Methods

        /// <summary>
        /// Adds the specific logger to this factory
        /// </summary>
        /// <param name="logger">The logger</param>
        void AddLogger(ILogger logger);

        /// <summary>
        /// Removes the specified logger from this factory
        /// </summary>
        /// <param name="logger">The logger</param>
        void RemoveLogger(ILogger logger);

        /// <summary>
        /// Logs the specific message to all loggers in this factory
        /// </summary>
        /// <param name="message">The message to log</param>
        /// <param name="loggerinstance">The logger instance</param>
        /// <param name="level">The level of the message being logged</param>
        /// <param name="origin">The method/function this message was logged in</param>
        /// <param name="filepath">The code filename that this message was logged from</param>
        /// <param name="linenumber">The line of code in the filename this message was logged from</param>
        void Log(
            string message,
            string loggerinstance,
            LogLevel level = LogLevel.Informative,            
            [CallerMemberName]string origin = "",
            [CallerFilePath]string filepath = "",
            [CallerLineNumber]int linenumber = 0);

        #endregion
    }
}
