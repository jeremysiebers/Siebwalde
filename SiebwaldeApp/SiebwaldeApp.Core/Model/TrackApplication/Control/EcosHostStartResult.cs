namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Outcome of a request to start the ECoS host. Callers use this instead of assuming
    /// that a start request succeeded, so the UI can show the mode that is actually active.
    /// </summary>
    public enum EcosHostStartResult
    {
        /// <summary>No ECoS host is available in this application instance.</summary>
        NotAvailable = 0,

        /// <summary>The host was not running and is now running in the requested mode.</summary>
        Started = 1,

        /// <summary>The requested mode was already active; nothing changed.</summary>
        AlreadyActive = 2,

        /// <summary>
        /// The host was running in another mode and was stopped and restarted in the
        /// requested mode. Used when the real track application becomes available.
        /// </summary>
        Transitioned = 3,

        /// <summary>
        /// The request was refused because a more authoritative mode is active. Real mode
        /// outranks the simulator, so a simulator request is rejected while real mode runs.
        /// </summary>
        Rejected = 4,

        /// <summary>The start attempt threw; the host is not running.</summary>
        Failed = 5
    }
}
