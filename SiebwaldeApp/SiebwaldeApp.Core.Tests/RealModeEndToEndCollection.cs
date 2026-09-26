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
    /// </summary>
    [CollectionDefinition("RealModeEndToEnd", DisableParallelization = true)]
    public sealed class RealModeEndToEndCollection
    {
    }
}
