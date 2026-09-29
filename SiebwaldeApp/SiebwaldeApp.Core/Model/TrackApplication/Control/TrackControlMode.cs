namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Selects which hardware backend the ECoS host drives.
    /// </summary>
    public enum TrackControlMode
    {
        /// <summary>
        /// Software-only simulation. No track controller or physical hardware is required,
        /// so this mode can be exercised on a development machine.
        /// </summary>
        Simulator = 0,

        /// <summary>
        /// Real track amplifiers, driven through the track controller.
        /// Requires a running track application.
        /// </summary>
        Real = 1,

        /// <summary>
        /// Full software simulation: the real track-control chain (comm, init pipeline, runtime
        /// write loop and ECoS host) driven against the deterministic transport, plus a
        /// controllable simulated-amplifier I/O surface. No track controller or physical
        /// hardware is required.
        /// </summary>
        FullSimulation = 2
    }

    /// <summary>
    /// Helpers shared by the Integration-layer hosts when a mode must be treated as the full
    /// track-control chain.
    /// </summary>
    public static class TrackControlModeExtensions
    {
        /// <summary>
        /// True when the mode drives the real track-control chain (real hardware or the full
        /// software simulation); false for the lightweight ECoS simulator.
        /// </summary>
        public static bool IsFullTrackChain(this TrackControlMode mode)
            => mode is TrackControlMode.Real or TrackControlMode.FullSimulation;
    }
}
