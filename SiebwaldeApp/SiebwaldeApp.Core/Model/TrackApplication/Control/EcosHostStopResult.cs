namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Outcome of a request to stop the ECoS host. Callers use this to decide whether a stop is
    /// proven clean, instead of assuming that a stop request succeeded.
    /// </summary>
    public enum EcosHostStopResult
    {
        /// <summary>The host was running and stopped cleanly within the bound.</summary>
        Stopped = 0,

        /// <summary>The host was already stopped; nothing changed.</summary>
        AlreadyStopped = 1,

        /// <summary>At least one owned task did not complete within the bound.</summary>
        Timeout = 2,

        /// <summary>A stop operation threw; the clean-stop guarantee is not established.</summary>
        Faulted = 3
    }
}
