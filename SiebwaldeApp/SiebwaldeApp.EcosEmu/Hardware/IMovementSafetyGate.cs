namespace SiebwaldeApp.EcosEmu
{
    /// <summary>
    /// Optional capability a hardware backend can expose so the ECoS backend can tell a real
    /// safety-interlock refusal apart from "there is no physical target for this command",
    /// without widening the <see cref="IHardwareBackend"/> contract itself.
    /// </summary>
    /// <remarks>
    /// <see cref="IHardwareBackend.SetLocoSpeed"/> returns a single boolean, so a refusal alone
    /// cannot say why the command was not applied. A backend that does not implement this
    /// interface is treated as "never blocked by safety": a refusal then means the command could
    /// not reach a physical target, which the ECoS layer may still accept as a logical request.
    /// </remarks>
    public interface IMovementSafetyGate
    {
        /// <summary>
        /// Returns whether non-zero movement for the locomotive is currently refused by a latched
        /// safety interlock.
        /// </summary>
        /// <param name="address">The locomotive address the movement command was issued for.</param>
        /// <returns>
        /// True when a latched safety interlock is blocking non-zero movement for this locomotive
        /// (or for the whole layout); otherwise false.
        /// </returns>
        bool IsMovementBlocked(int address);
    }
}
