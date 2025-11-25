using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Low-level byte-oriented transport for the track bus.
    /// This can be UDP, EcosEmu, or any other implementation.
    /// </summary>
    public interface ITrackTransport : IAsyncDisposable
    {
        Task OpenAsync(CancellationToken cancellationToken = default);
        Task CloseAsync(CancellationToken cancellationToken = default);

        Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

        /// <summary>
        /// Asynchronously yields raw datagrams from the underlying transport.
        /// </summary>
        IAsyncEnumerable<byte[]> ReceiveAsync(CancellationToken cancellationToken = default);
    }
}