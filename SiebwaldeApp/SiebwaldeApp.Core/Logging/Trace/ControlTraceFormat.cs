using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The one deterministic formatting convention for the production control trace.
    ///
    /// Convention (stable, machine-parseable, locale-independent):
    /// <list type="bullet">
    ///   <item><description>Payload starts with <c>EVENT=&lt;NAME&gt;</c>.</description></item>
    ///   <item><description>Fields are <c>key=value</c> separated by a single space.</description></item>
    ///   <item><description>Unknown/null is the literal token <c>&lt;none&gt;</c>.</description></item>
    ///   <item><description>Lists (for example amplifier sets) are comma-separated without spaces, ordered ascending; an empty list is <c>&lt;none&gt;</c>.</description></item>
    ///   <item><description>Booleans are <c>true</c>/<c>false</c> (lower case).</description></item>
    ///   <item><description>Enums use their stable C# member name (for example <c>MountainRailway</c>).</description></item>
    ///   <item><description>Numbers always use the invariant culture, so no locale can change the output.</description></item>
    ///   <item><description>Values never contain a line break; embedded CR/LF are replaced by a space.</description></item>
    /// </list>
    ///
    /// The existing <c>FileLogger</c> prefix (timestamp/source/method/line) still precedes the
    /// payload, so the payload itself stays a single stable line.
    /// </summary>
    public static class ControlTraceFormat
    {
        /// <summary>Token used for an unknown or absent value.</summary>
        public const string None = "<none>";

        /// <summary>Field separator between key/value pairs.</summary>
        public const string Separator = " ";

        /// <summary>Separator inside a list value.</summary>
        public const string ListSeparator = ",";

        /// <summary>Builds the leading <c>EVENT=</c> token.</summary>
        public static string Event(string eventName) => "EVENT=" + Sanitize(eventName);

        /// <summary>Formats a string field; null/empty becomes <see cref="None"/>.</summary>
        public static string Field(string key, string? value)
            => key + "=" + (string.IsNullOrEmpty(value) ? None : Sanitize(value));

        /// <summary>Formats an integer field with the invariant culture.</summary>
        public static string Field(string key, int value)
            => key + "=" + value.ToString(CultureInfo.InvariantCulture);

        /// <summary>Formats a nullable integer field; null becomes <see cref="None"/>.</summary>
        public static string Field(string key, int? value)
            => key + "=" + (value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : None);

        /// <summary>Formats a boolean field as <c>true</c>/<c>false</c>.</summary>
        public static string Field(string key, bool value)
            => key + "=" + (value ? "true" : "false");

        /// <summary>Formats an enum field using its stable member name.</summary>
        public static string Field(string key, Enum value)
            => key + "=" + Sanitize(value.ToString());

        /// <summary>Formats an ordered amplifier list; null/empty becomes <see cref="None"/>.</summary>
        public static string List(IEnumerable<ushort>? values)
            => List(values?.Select(v => (int)v));

        /// <summary>Formats an ordered integer list; null/empty becomes <see cref="None"/>.</summary>
        public static string List(IEnumerable<int>? values)
        {
            if (values is null)
            {
                return None;
            }

            var items = values
                .Select(v => v.ToString(CultureInfo.InvariantCulture))
                .ToArray();

            return items.Length == 0 ? None : string.Join(ListSeparator, items);
        }

        /// <summary>Joins already-formatted parts, dropping empty ones.</summary>
        public static string Join(params string?[] parts)
            => string.Join(Separator, parts.Where(p => !string.IsNullOrEmpty(p)));

        /// <summary>Removes line breaks and trims, so a payload stays one line.</summary>
        public static string Sanitize(string value)
            => value.Replace('\r', ' ').Replace('\n', ' ').Trim();
    }
}
