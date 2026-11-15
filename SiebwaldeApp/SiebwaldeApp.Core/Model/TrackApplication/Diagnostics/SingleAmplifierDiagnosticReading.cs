using System;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Read-only diagnostic snapshot of a single track amplifier's diagnostic registers (HR1
    /// control/lease, HR8 state, HR9 fault, HR22 protocol), read via the existing 108 read + the
    /// SLAVEINFO cache. No second Modbus master, no competing UDP session, no backdoor.
    ///
    /// <see cref="Freshness"/> is the single honesty gate: only a <see cref="AmplifierRegisterFreshness.Fresh"/>
    /// reading may be treated as the live PIC18 state. <see cref="Hr8State"/>, <see cref="Hr9Fault"/>,
    /// <see cref="Hr1Control"/> and <see cref="Hr22Protocol"/> are always the current cache values;
    /// their trustworthiness is qualified by <see cref="Freshness"/>.
    /// </summary>
    public sealed record SingleAmplifierDiagnosticReading
    {
        /// <summary>The amplifier slave address this reading describes (1/3/4/6).</summary>
        public byte SlaveNumber { get; init; }

        /// <summary>Honesty label for the register values (fresh / cached / stale / unknown).</summary>
        public AmplifierRegisterFreshness Freshness { get; init; }

        /// <summary>HR1 (control/lease) register value.</summary>
        public ushort Hr1Control { get; init; }

        /// <summary>HR8 (state) register value (STANDBY=8, READY=1 in the low nibble).</summary>
        public ushort Hr8State { get; init; }

        /// <summary>HR9 (fault) register value.</summary>
        public ushort Hr9Fault { get; init; }

        /// <summary>HR22 (protocol) register value.</summary>
        public ushort Hr22Protocol { get; init; }

        /// <summary>The protocol readback sequence at the time of the reading.</summary>
        public long ProtocolReadbackSequence { get; init; }

        /// <summary>The last data-received timestamp at the time of the reading.</summary>
        public DateTimeOffset? LastDataReceivedUtc { get; init; }

        /// <summary>Whether the amplifier's Phase-1 configuration was verified (Stage 4 evidence).</summary>
        public bool ConfigurationVerified { get; init; }

        /// <summary>The Phase-1 failure reason, or null when compatible/configured.</summary>
        public string? Phase1Failure { get; init; }

        /// <summary>True when HR8's low nibble reflects the READY state (group-ready).</summary>
        public bool IsReady => (Hr8State & 0xF) == TrackAmplifierRegisters.ReadyState;

        /// <summary>True when HR8's low nibble reflects the STANDBY state (config applied, no GROUP_READY).</summary>
        public bool IsStandby => (Hr8State & 0xF) == TrackAmplifierRegisters.StandbyState;
    }
}
