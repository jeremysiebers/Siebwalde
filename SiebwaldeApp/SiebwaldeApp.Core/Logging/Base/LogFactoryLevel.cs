namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The level of details to output for a logger
    /// </summary>
    public enum LogOutputLevel
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
        /// Logs only critical errors and warnings and success, no general information
        /// </summary>
        Critical = 4,

        /// <summary>
        /// Logger outputs nothing
        /// </summary>
        Nothing = 7,
    }
}