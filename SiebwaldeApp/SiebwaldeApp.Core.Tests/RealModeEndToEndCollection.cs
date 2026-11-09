using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// xUnit collection that serializes the real-mode end-to-end test classes.
    ///
    /// Both <see cref="DeterministicTrackTransportEndToEndTests"/> and
    /// <see cref="ObservedNeutralV3IntegrationTests"/> mutate the shared static
    /// <see cref="SiebwaldeApp.Core.Properties.CoreSettings.Default"/>.<c>TrackAmplifierFwPath</c> to
    /// point at their own generated temporary firmware hex. Because xUnit runs test classes in
    /// parallel by default, one class restoring/overwriting the setting mid-run can make the other
    /// read a deleted/absent hex, failing <c>FlashFwTrackamplifiersStep</c> and transitioning the
    /// runtime to <c>Failed</c> instead of <c>Running</c>.
    ///
    /// Putting both classes in this collection (with <c>DisableParallelization</c>) makes them run
    /// one after the other, while their own test methods still run in parallel (each uses a unique
    /// temp path, so only the cross-class static-setting mutation is the hazard).
    ///
    /// <para>
    /// There is a SECOND shared mutable the collection serializes but cannot isolate: the
    /// cross-process controller-session lease <c>SiebwaldeApp.Core.TrackControllerSessionLease</c>
    /// (named mutex <c>Local\Siebwalde.TrackController.Session</c> + the diagnostic owner file under
    /// <c>CoreConfiguration.LogDirectory</c>). Every real-mode start (the
    /// <c>SiebwaldeApp.Integration.TrackApplicationRuntimeHost.StartAsync</c> →
    /// <c>ComposeTrackPartAsync</c> path) acquires it as the WPF owner, and
    /// <c>SiebwaldeApp.Core.TrackApplication.Maintenance.FlashOneAmplifierRunner.RunAsync</c>
    /// acquires it as the CLI owner. Both owner types contend on the same OS mutex. Serializing the
    /// classes in-process prevents the two offline test owners from racing each other, but it cannot
    /// prevent a real <c>SiebwaldeApp</c> process (or a leftover lease from a crashed run) from
    /// holding the mutex: in that case the real-mode start transitions to <c>Failed</c> and the flash
    /// runner reports "The track-controller session is owned by …". Making the offline tests stop
    /// acquiring the real lease (skip the lease when a custom in-process <c>transportFactory</c> is
    /// supplied) is a production change and is out of scope for this test-isolation fix.
    /// </para>
    /// </summary>
    [CollectionDefinition("RealModeEndToEnd", DisableParallelization = true)]
    public sealed class RealModeEndToEndCollection
    {
    }
}
