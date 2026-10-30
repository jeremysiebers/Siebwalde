using System;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Offline tests for the cross-process controller-session ownership lease
    /// (<see cref="TrackControllerSessionLease"/>). No UDP socket, no network and no hardware are
    /// involved: the tests exercise the FIXED named mutex and the diagnostic owner side-channel
    /// entirely in-process.
    ///
    /// <para>
    /// Abrupt-termination release is deliberately NOT tested with a flaky throwaway child process.
    /// Windows guarantees that a named mutex is abandoned (and therefore released) when its owning
    /// process exits, so an unexpected crash releases ownership automatically; the deterministic
    /// <c>Dispose_ReleasesLease_SoSubsequentAcquireSucceeds</c> test covers the graceful path.
    /// </para>
    ///
    /// <para>
    /// This class shares the fixed <c>Local\Siebwalde.TrackController.Session</c> mutex with the
    /// CLI runner tests and the real-mode runtime-host tests, so it is placed in the serialized
    /// <c>RealModeEndToEnd</c> collection to keep xUnit's cross-class parallelism from contending
    /// on the same named mutex.
    /// </para>
    /// </summary>
    [Collection("RealModeEndToEnd")]
    public class TrackControllerSessionLeaseTests
    {
        [Fact]
        public void TryAcquire_SecondOwner_Fails_AndReportsFirstOwner()
        {
            using var wpf = TrackControllerSessionLease.TryAcquire(
                TrackControllerSessionLease.WpfOwnerName, out var firstBlocking);

            Assert.NotNull(wpf);
            Assert.Null(firstBlocking);

            // A second acquisition (the CLI while the WPF holds the session) must fail and report
            // the holder's persisted owner string.
            var cli = TrackControllerSessionLease.TryAcquire(
                TrackControllerSessionLease.FlashOneConsoleOwnerName, out var blocking);

            Assert.Null(cli);
            Assert.NotNull(blocking);
            Assert.Contains(TrackControllerSessionLease.WpfOwnerName, blocking);
        }

        [Fact]
        public void Dispose_ReleasesLease_SoSubsequentAcquireSucceeds()
        {
            var first = TrackControllerSessionLease.TryAcquire(
                TrackControllerSessionLease.WpfOwnerName, out _);
            Assert.NotNull(first);

            first.Dispose();

            var second = TrackControllerSessionLease.TryAcquire(
                TrackControllerSessionLease.FlashOneConsoleOwnerName, out var blocking);
            Assert.NotNull(second);
            Assert.Null(blocking);

            second.Dispose();
        }

        [Fact]
        public void TryAcquire_SameHolder_ReAcquireFails_AndHolderStaysOwner()
        {
            var first = TrackControllerSessionLease.TryAcquire(
                TrackControllerSessionLease.WpfOwnerName, out _);
            Assert.NotNull(first);

            // Ownership persists for the whole session: re-acquiring while still holding fails,
            // and the reported blocking owner is still the current holder.
            var again = TrackControllerSessionLease.TryAcquire(
                TrackControllerSessionLease.FlashOneConsoleOwnerName, out var blocking);
            Assert.Null(again);
            Assert.NotNull(blocking);
            Assert.Contains(TrackControllerSessionLease.WpfOwnerName, blocking);

            first.Dispose();
        }

        [Fact]
        public async Task CliRunner_BlockedByExistingLease_FailsClosedBeforeAnySend()
        {
            // Simulate the WPF runtime already owning the controller session.
            using var wpfLease = TrackControllerSessionLease.TryAcquire(
                TrackControllerSessionLease.WpfOwnerName, out _);
            Assert.NotNull(wpfLease);

            var transportFactoryInvoked = false;
            var runner = new FlashOneAmplifierRunner(
                transportFactory: () =>
                {
                    transportFactoryInvoked = true;
                    throw new InvalidOperationException("The transport must never be built.");
                },
                preflight: new PermissivePreflight());

            var result = await runner.RunAsync(
                hexPath: "unused.hex",
                expectedSha256Hex: null,
                amplifier: 6);

            // The runner fails closed on the blocking session BEFORE the preflight, HEX validation
            // or any transport composition/send.
            Assert.False(result.Success);
            Assert.Equal(1, result.ExitCode);
            Assert.Contains(TrackControllerSessionLease.WpfOwnerName, result.Error);
            Assert.False(transportFactoryInvoked);
        }

        private sealed class PermissivePreflight : IFlashOneOwnershipPreflight
        {
            public FlashOneOwnershipResult Check() => FlashOneOwnershipResult.Success();
        }
    }
}
