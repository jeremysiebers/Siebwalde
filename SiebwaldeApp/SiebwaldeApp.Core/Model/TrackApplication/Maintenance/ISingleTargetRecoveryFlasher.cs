namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Flashes exactly one, explicitly identified PIC18 track amplifier that is already in
    /// bootloader mode, via the single-target bootloader-only primitive (command 132). This is the
    /// per-target control the legacy un-targeted recovery FSM lacks: the target amplifier address
    /// is honoured, so a multi-bootloader ambiguity can never flash the wrong amplifier.
    /// </summary>
    public interface ISingleTargetRecoveryFlasher
    {
        /// <summary>Flashes <paramref name="targetAmplifier"/> (1..50) bootloader-only and returns the outcome.</summary>
        Task<FlashOneAmplifierResult> ExecuteBootloaderOnlyAsync(
            byte targetAmplifier,
            CancellationToken cancellationToken = default);
    }
}
