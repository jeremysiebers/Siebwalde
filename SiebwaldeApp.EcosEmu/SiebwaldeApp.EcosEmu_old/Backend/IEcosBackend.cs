using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    public interface IEcosBackend
    {
        //Action LocoListSynced { get; set; }

        Task HandleAsync(EcosCommand cmd, TextWriter writer, CancellationToken ct);
    }
}
