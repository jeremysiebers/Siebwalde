using System;
using System.Globalization;
using System.IO;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Production registration of the dedicated control trace.
    ///
    /// It follows the existing component-logger pattern: a <see cref="FileLogger"/> for the
    /// dedicated instance is added to the shared <see cref="ILogFactory"/> through
    /// <see cref="ILogFactory.AddLogger"/>, in the same directory and with the same date-based
    /// filename convention as the other component logs (for example <c>TrackAppLog</c>):
    ///
    ///   <c>{LogDirectory}{dd-M-yyyy}_{Component}.txt</c>, for example
    ///   <c>C:\Localdata\Siebwalde\Logging\20-9-2026_ControlTraceLog.txt</c>.
    ///
    /// The date and path are resolved at registration time; nothing is hard-coded.
    /// </summary>
    public static class ControlTraceLogging
    {
        /// <summary>
        /// Registers the control-trace file logger on the given factory and emits the
        /// session-start marker. Returns the trace the control path should use.
        /// </summary>
        public static IControlTrace Register(
            ILogFactory factory,
            string logDirectory,
            string application,
            string? mode = null,
            string? version = null)
        {
            if (factory is null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var filePath = BuildLogFilePath(logDirectory);
            factory.AddLogger(new FileLogger(filePath, ControlTraceLogger.LoggerInstance));

            var trace = new ControlTraceLogger(factory);
            trace.SessionStart(application, mode, version);
            return trace;
        }

        /// <summary>
        /// Builds the dedicated control-trace filename for the current day, using the existing
        /// component-log convention (<c>{dd-M-yyyy}_{Component}.txt</c>).
        /// </summary>
        public static string BuildLogFilePath(string logDirectory)
        {
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                throw new ArgumentException("A log directory is required.", nameof(logDirectory));
            }

            var now = DateTime.Now;
            var fileName =
                now.Day.ToString(CultureInfo.InvariantCulture) + "-" +
                now.Month.ToString(CultureInfo.InvariantCulture) + "-" +
                now.Year.ToString(CultureInfo.InvariantCulture) + "_" +
                ControlTraceLogger.LoggerInstance + ".txt";

            return Path.Combine(logDirectory, fileName);
        }
    }
}
