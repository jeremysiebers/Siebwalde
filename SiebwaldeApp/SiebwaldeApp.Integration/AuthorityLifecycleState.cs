namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// The four authority-lifecycle states of the ownership-aware carrier.
    ///
    /// The carrier's drive authority comes from the KIS-07 logical-section shadow. A single boolean
    /// <c>ShadowValid</c> is not enough: whether an invalid shadow is "startup not ready", a
    /// "transient suspect" or "operational authority loss" depends on whether a valid shadow has
    /// EVER been observed and on how long the invalidity has persisted (the N+X qualification model).
    ///
    /// <list type="bullet">
    /// <item><description>
    /// <see cref="AuthorityNotYetObserved"/> — the application started and no valid authoritative KIS
    /// shadow has been observed yet. Movement is denied (fail closed) but this MUST NOT create a
    /// latched layout safety fault: it is "startup not ready", not "layout stop latched".
    /// </description></item>
    /// <item><description>
    /// <see cref="AuthorityValid"/> — the first valid KIS shadow has been established.
    /// </description></item>
    /// <item><description>
    /// <see cref="AuthoritySuspect"/> — a valid shadow has been observed, then the shadow became
    /// invalid. This is a TRANSIENT, non-latching state: the established footprint keeps driving
    /// from the last-known-good snapshot while the carrier qualifies the loss (N consecutive
    /// invalid observations AND a minimum invalid duration). A single bad read does NOT latch.
    /// </description></item>
    /// <item><description>
    /// <see cref="AuthorityLostAfterValid"/> — authority was valid, became suspect, and then
    /// QUALIFIED as lost (consecutive-invalid count AND minimum duration both met). This is the
    /// approved system-scope condition: layout-wide StopRequired/latch with no auto-resume.
    /// </description></item>
    /// </list>
    /// </summary>
    public enum AuthorityLifecycleState
    {
        /// <summary>No valid authoritative KIS shadow has been observed yet (startup).</summary>
        AuthorityNotYetObserved = 0,

        /// <summary>The first valid KIS shadow has been established.</summary>
        AuthorityValid = 1,

        /// <summary>A previously-valid shadow is transiently invalid (not yet qualified as lost).</summary>
        AuthoritySuspect = 2,

        /// <summary>Authority was valid, became suspect, and qualified as lost (operational loss).</summary>
        AuthorityLostAfterValid = 3
    }
}
