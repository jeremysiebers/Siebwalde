namespace SiebwaldeApp
{
    /// <summary>
    /// The severity of the log message
    /// </summary>
    public enum LogFactoryLevel
    {
        /// <summary>
        /// Logs everything
        /// </summary>
        Debug = 1,

        /// <summary>
        /// Logs all information except debug information
        /// </summary>
        Verbose = 2,

        /// <summary>
        /// Logs all informative message, ignoring any debug and verbose messages
        /// </summary>
        Informative = 3,

        /// <summary>
        /// Logs only warnings, errors and standard messages
        /// </summary>
        Normal = 4,

        /// <summary>
        /// Logs only critical errors and warnings, no general information
        /// </summary>
        Critical = 5,

        /// <summary>
        /// Logger outputs nothing
        /// </summary>
        Nothing = 6,
    }
}