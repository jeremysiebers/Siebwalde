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
        Real = 1
    }
}
