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
    /// There is a SECOND shared mutable the collection serializes: the cross-process
    /// controller-session lease <c>SiebwaldeApp.Core.TrackControllerSessionLease</c> (named mutex
    /// <c>Local\Siebwalde.TrackController.Session</c> + the diagnostic owner file under
    /// <c>CoreConfiguration.LogDirectory</c>). The lease protects ONLY the real UDP transport
    /// (controller port 10001), which must be exclusive across processes. As of the
    /// test-isolation fix, the production code acquires the lease ONLY when it will actually
    /// compose the real transport — i.e. when no in-process <c>transportFactory</c> is supplied and
    /// the mode is <c>Real</c> (see
    /// <c>SiebwaldeApp.Integration.TrackApplicationRuntimeHost.ComposeTrackPartAsync</c> and
    /// <c>SiebwaldeApp.Core.TrackApplication.Maintenance.FlashOneAmplifierRunner.RunAsync</c>).
    /// Offline tests that inject a custom in-process <c>transportFactory</c> therefore never touch
    /// the real lease, so they no longer contend on the shared OS mutex. Tests that DO exercise the
    /// real-lease contention path (holding one owner while the other owner tries to acquire) live in
    /// this serialized collection and hold the mutex only for the duration of a single test.
    /// </para>
    /// </summary>
    [CollectionDefinition("RealModeEndToEnd", DisableParallelization = true)]
    public sealed class RealModeEndToEndCollection
    {
    }
}
