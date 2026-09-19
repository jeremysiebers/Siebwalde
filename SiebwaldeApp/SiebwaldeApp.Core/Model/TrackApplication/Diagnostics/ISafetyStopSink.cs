namespace SiebwaldeApp.Core
{
    /// <summary>
    /// The existing locomotive-control mechanisms, exposed for safety reactions. Implementations
    /// must route through the same backend the ECoS path uses; no second control path may be
    /// introduced.
    /// </summary>
    public interface ISafetyStopSink
    {
        /// <summary>
        /// Controlled stop of one locomotive, through the existing loco speed command path
        /// (ECoS speed 0 = standstill). Returns false when the locomotive has no known position
        /// so nothing could be commanded.
        /// </summary>
        bool StopLoco(int address);

        /// <summary>
        /// Controlled stop of the whole layout, through the existing central power-off path
        /// (the same mechanism Koploper's <c>set(1,stop)</c> uses).
        /// </summary>
        void StopLayout();
    }

    /// <summary>
    /// Describes which feedback the current mode can actually observe. Used so that feedback
    /// which is unavailable by design is never reported as a fault or as a confirmation.
    /// </summary>
    public interface IObservability
    {
        /// <summary>True when the physical switch position can be read back.</summary>
        bool SwitchFeedbackAvailable { get; }

        /// <summary>True when block occupancy can be read reliably.</summary>
        bool OccupancyAvailable { get; }
    }

    /// <summary>
    /// Reads back the position a physical switch output actually has. Implementations that
    /// cannot observe return false, which must be treated as <c>unknown</c> and never as a
    /// successful confirmation.
    /// </summary>
    public interface ISwitchObserver
    {
        /// <summary>Reads the observed position of a physical switch output.</summary>
        bool TryGetObservedPosition(int physicalAddress, out SwitchPosition position);
    }
}
