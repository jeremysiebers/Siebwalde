using System.Collections.Generic;

namespace SiebwaldeApp.Core.TrackApplication.Simulator
{
    /// <summary>
    /// Narrow injection surface for the controllable simulated-amplifier I/O used by the
    /// <see cref="TrackControlMode.FullSimulation"/> runtime. It lets a caller (tests, and the
    /// WPF amplifier page) drive occupancy/status and observe the simulated holding registers
    /// without exposing the transport's full protocol surface.
    /// </summary>
    public interface ISimulatedTrackIo
    {
        /// <summary>Detected ModBus slave addresses (defensive copy).</summary>
        IReadOnlyList<byte> DetectedSlaves { get; }

        /// <summary>Sets or clears the occupied bit of the slave's status register (HR2).</summary>
        void SetSlaveOccupancy(byte slaveNumber, bool occupied);

        /// <summary>Sets the whole status register (HR2) of the slave to the given raw value.</summary>
        void SetSlaveStatus(byte slaveNumber, ushort status);

        /// <summary>Returns a defensive copy of the simulated holding registers for a slave.</summary>
        ushort[] GetRegisters(byte slaveNumber);
    }
}
