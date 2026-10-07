using System;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Decodes the Koploper raw object graph against an injected memory layout, using the
    /// configured plausibility bounds. Offsets come exclusively from the layout profile. The
    /// walking itself is delegated to <see cref="KoploperRawObjectGraphReader"/>; this type only
    /// projects the raw observation down to the registry for the pre-KIS-04 public contract.
    /// </summary>
    public sealed class KoploperObjectGraphDecoder : IKoploperObjectGraphDecoder
    {
        private readonly IKoploperRawObjectGraphReader _graphReader;

        public KoploperObjectGraphDecoder(IKoploperMemoryLayout layout, KoploperDecodePlausibility plausibility)
        {
            ArgumentNullException.ThrowIfNull(layout);
            ArgumentNullException.ThrowIfNull(plausibility);
            _graphReader = new KoploperRawObjectGraphReader(layout, plausibility);
        }

        /// <inheritdoc />
        public KoploperObjectGraphDecodeResult Decode(
            IKoploperMemoryReader reader,
            nuint moduleBase,
            out KoploperRawRegistry? registry)
        {
            KoploperObjectGraphDecodeResult result = _graphReader.Read(reader, moduleBase, out KoploperRawObjectGraphObservation? observation);
            registry = observation?.Registry;
            return result;
        }
    }
}
