using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    /// <summary>
    /// Abstraction for loading and saving locomotive metadata.
    /// </summary>
    public interface ILocoRepository
    {
        Task LoadAsync(CancellationToken ct = default);
        Task SaveAsync(CancellationToken ct = default);

        IReadOnlyCollection<LocoInfo> GetAll();

        LocoInfo? GetByEcosId(int ecosId);
        LocoInfo? GetByAddress(int address, int? addressExt = null);

        void AddOrUpdate(LocoInfo loco);
        void RemoveByEcosId(int ecosId);
    }
}
