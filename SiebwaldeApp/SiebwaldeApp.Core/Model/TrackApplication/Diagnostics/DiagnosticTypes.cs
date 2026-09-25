namespace SiebwaldeApp.Core
{
    /// <summary>
    /// How serious a diagnostic is, and therefore what the control path does about it.
    /// </summary>
    public enum DiagnosticSeverity
    {
        /// <summary>Informational; nothing is wrong.</summary>
        Info = 0,

        /// <summary>
        /// Worth telling the operator, but the control path continues. Used for conditions
        /// that are unavailable by design rather than broken.
        /// </summary>
        Warning = 1,

        /// <summary>The operation is refused; the control path keeps running.</summary>
        Rejected = 2,

        /// <summary>Movement is unsafe; a controlled stop is requested.</summary>
        StopRequired = 3
    }

    /// <summary>
    /// The meaningful ways the requested/logical state can stop agreeing with the state the
    /// control system can actually guarantee. Deliberately free of protocol and UI wording.
    /// </summary>
    public enum DiagnosticCode
    {
        /// <summary>A command or address is not present in the mapping, so it is ignored.</summary>
        UnmappedAddress = 0,

        /// <summary>The configuration is invalid or contradicts itself.</summary>
        InvalidConfiguration = 1,

        /// <summary>
        /// A route needs a switch position that differs from the known logical position.
        /// </summary>
        RouteSwitchMismatch = 2,

        /// <summary>The backend accepted the command but could not apply it.</summary>
        CommandNotApplied = 3,

        /// <summary>The commanded state and the observed state differ, where observation exists.</summary>
        CommandedObservedMismatch = 4,

        /// <summary>Occupancy relevant to the selected route does not match expectations.</summary>
        OccupancyMismatch = 5,

        /// <summary>The backend or communication path is not available.</summary>
        BackendUnavailable = 6,

        /// <summary>
        /// The state is unknown while a safe operation requires knowing it. Distinct from
        /// <see cref="UnmappedAddress"/>: the element exists, but its state cannot be confirmed.
        /// </summary>
        StateUnknown = 7,

        /// <summary>
        /// A movement command was refused because a safety fault is latched. Reported once per
        /// affected locomotive while the latch holds, so repeated commands cannot flood the
        /// diagnostics.
        /// </summary>
        MovementRejectedBySafety = 8,

        /// <summary>
        /// An explicit safety reset was refused because the underlying condition is still not
        /// resolved.
        /// </summary>
        ResetRefused = 9,

        /// <summary>
        /// Observed neutral could not be established for every configured amplifier within the
        /// bounded window, so movement permission was not granted.
        /// </summary>
        NeutralNotEstablished = 10
    }

    /// <summary>The safety reaction that was actually taken for a diagnostic.</summary>
    public enum SafetyAction
    {
        /// <summary>Nothing was done.</summary>
        None = 0,

        /// <summary>The affected locomotive was stopped through the existing loco-command path.</summary>
        StopLoco = 1,

        /// <summary>
        /// The whole layout was stopped through the existing central power-off path, used when
        /// no single locomotive can be held responsible.
        /// </summary>
        StopLayout = 2,

        /// <summary>
        /// A locomotive-scoped stop could not neutralize every required physical amplifier, so
        /// the amplifier-centric layout neutralization was invoked instead. The accompanying
        /// diagnostics record whether that escalation was complete.
        /// </summary>
        StopLayoutEscalated = 3
    }
}
