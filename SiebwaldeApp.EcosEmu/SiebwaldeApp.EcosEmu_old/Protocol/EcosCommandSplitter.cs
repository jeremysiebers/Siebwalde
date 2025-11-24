using System;
using System.Collections.Generic;

namespace SiebwaldeApp.EcosEmu
{
    /// <summary>
    /// Splitst een chunk zoals:
    /// "request(1,view)request(10,view)queryObjects(26)..."
    /// in: "request(1,view)", "request(10,view)", ...
    /// door simpelweg op ')' te splitsen.
    /// </summary>
    public static class EcosCommandSplitter
    {
        public static IReadOnlyList<string> SplitCommands(string chunk)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(chunk))
                return result;

            var parts = chunk.Split(')', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            foreach (var part in parts)
            {
                var cmd = part.Trim();
                if (cmd.Length == 0)
                    continue;

                // voeg de ) weer toe
                cmd += ")";

                result.Add(cmd);
            }

            return result;
        }
    }
}
