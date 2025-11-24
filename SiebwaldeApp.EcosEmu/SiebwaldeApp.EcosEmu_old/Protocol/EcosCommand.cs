using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    public class EcosCommand
    {
        public string RawLine { get; }
        public string Name { get; }
        public int? ObjectId { get; }
        public string[] Options { get; }

        public EcosCommand(string raw, string name, int? objectId, string[] options)
        {
            RawLine = raw;
            Name = name;
            ObjectId = objectId;
            Options = options ?? Array.Empty<string>();
        }
    }
}
