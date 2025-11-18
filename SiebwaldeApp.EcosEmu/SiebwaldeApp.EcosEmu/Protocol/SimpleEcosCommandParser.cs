using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    public class SimpleEcosCommandParser : IEcosCommandParser
    {
        public EcosCommand? Parse(string line)
        {
            line = line.Trim();
            if (string.IsNullOrWhiteSpace(line))
                return null;

            int parenIndex = line.IndexOf('(');
            if (parenIndex < 0 || !line.EndsWith(")"))
                return new EcosCommand(line, line, null, Array.Empty<string>());

            string name = line[..parenIndex].Trim();
            string argsPart = line[(parenIndex + 1)..^1];

            string[] parts = argsPart.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            int? id = null;
            var options = new List<string>();

            if (parts.Length > 0 && int.TryParse(parts[0], out int parsedId))
            {
                id = parsedId;
                for (int i = 1; i < parts.Length; i++)
                    options.Add(parts[i]);
            }
            else
            {
                options.AddRange(parts);
            }

            return new EcosCommand(line, name, id, options.ToArray());
        }
    }
}