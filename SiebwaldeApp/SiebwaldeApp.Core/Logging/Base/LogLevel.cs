namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The severity of the log message
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Developer specific
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Verbose information
        /// </summary>
        Verbose = 2,

        /// <summary>
        /// General information
        /// </summary>
        Informative = 3,

        /// <summary>
        /// A warning
        /// </summary>
        Warning = 4,

        /// <summary>
        /// A warning
        /// </summary>
        Error = 5,

        /// <summary>
        /// A warning
        /// </summary>
        Success = 6,
    }
}