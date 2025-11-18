using System;

namespace SiebwaldeApp.EcosEmu
{
    /// <summary>
    /// Persistent locomotive metadata synchronized between Koploper and the ECoS emulator.
    /// </summary>
    public sealed class LocoInfo
    {
        /// <summary>
        /// ECoS object id for this locomotive (e.g. 1000, 1001, ...).
        /// This is the id used in ECoS commands like set(1000,...) or request(1000,view).
        /// </summary>
        public int EcosId { get; set; }

        /// <summary>
        /// Decoder address as known by Koploper / the hardware.
        /// </summary>
        public int Address { get; set; }

        /// <summary>
        /// Optional extended address value (if used).
        /// </summary>
        public int? AddressExt { get; set; }

        /// <summary>
        /// Human readable locomotive name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Protocol name as seen by Koploper / ECoS (e.g. "DCC28").
        /// </summary>
        public string Protocol { get; set; } = "DCC28";

        /// <summary>
        /// Last known block number for this locomotive.
        /// This is updated from Koploper via the external info client.
        /// </summary>
        public int? Block { get; set; }
    }
}
