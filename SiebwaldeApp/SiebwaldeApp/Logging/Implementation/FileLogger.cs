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

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor
        /// <paramref name="filepath"/>The path to log to</param>
        /// </summary>
        public FileLogger(string filepath)
        {
            // Set the file property
            FilePath = filepath;
        }

        #endregion

        #region Logger methods

        public void Log(string message, LogFactoryLevel level)
        {
            IoC.File.WriteAllTextToFileAsync(message + Environment.NewLine, FilePath, append: true);
        }

        #endregion


    }
}
