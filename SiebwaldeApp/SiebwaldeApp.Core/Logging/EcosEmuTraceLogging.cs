using System;
using System.Globalization;
using System.IO;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Shared helper for the file-backed diagnostic traces of the EcosEmu &lt;-&gt; Koploper traffic
    /// and the occupancy read path. It follows the same date-based filename convention as
    /// <see cref="ControlTraceLogging"/>:
    ///
    ///   <c>{LogDirectory}{dd-M-yyyy}_{Component}.txt</c>.
    ///
    /// The component name is also the logger instance used when calling
    /// <see cref="ILogFactory.Log"/>, so a single <see cref="FileLogger"/> bound to that instance
    /// captures exactly that trace. Nothing is hard-coded: the date and path are resolved at
    /// registration time, exactly like the existing component logs.
    /// </summary>
    public static class EcosEmuTraceLogging
    {
        /// <summary>
        /// Builds the date-based diagnostic trace filename for the given component, using the
        /// same day/month/year invariant-culture format as <see cref="ControlTraceLogging"/>.
        /// </summary>
        public static string BuildLogFilePath(string logDirectory, string component)
        {
            if (string.IsNullOrWhiteSpace(logDirectory))
            {
                throw new ArgumentException("A log directory is required.", nameof(logDirectory));
            }

            if (string.IsNullOrWhiteSpace(component))
            {
                throw new ArgumentException("A component name is required.", nameof(component));
            }

            var now = DateTime.Now;
            var fileName =
                now.Day.ToString(CultureInfo.InvariantCulture) + "-" +
                now.Month.ToString(CultureInfo.InvariantCulture) + "-" +
                now.Year.ToString(CultureInfo.InvariantCulture) + "_" +
                component + ".txt";

            return Path.Combine(logDirectory, fileName);
        }
    }
}
