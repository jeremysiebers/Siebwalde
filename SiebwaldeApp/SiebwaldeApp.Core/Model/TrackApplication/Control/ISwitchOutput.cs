namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The physical side of a switch: an accessory output that can be driven to a position.
    ///
    /// Implementations are mode specific (a simulator store, or a real accessory decoder
    /// bus), but the translation from an ECoS/Koploper switch command to this call is shared
    /// and lives in <c>SwitchController</c>.
    /// </summary>
    public interface ISwitchOutput
    {
        /// <summary>
        /// True when this output can actually drive a physical switch. False means the path is
        /// not wired yet; commands must then be reported as not applied (a known limitation),
        /// never as a confirmation.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Drives a physical switch output to a position.
        /// </summary>
        /// <param name="physicalAddress">Physical switch/output address on the accessory bus.</param>
        /// <param name="position">
        /// The physical position to drive to, already translated from the ECoS request
        /// (including any per-switch inversion).
        /// </param>
        /// <returns>True when the output was actually driven.</returns>
        bool SetPosition(int physicalAddress, SwitchPosition position);
    }
}
