using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Focused tests for the new simulator-layer controller mute (comm-failure) option. The mute is
    /// a deterministic, simulator-only feature: it makes the simulated controller stay silent so the
    /// production <see cref="ConnectToEthernetTargetStep"/> (command 250) never completes. It does
    /// NOT change production init logic.
    /// </summary>
    public class DeterministicTrackTransportControllerMuteTests
    {
        private static byte[] ConnectionRequestFrame()
            => new byte[] { 0xAA, EnumClientCommands.CLIENT_CONNECTION_REQUEST }; // [HEADER, command]

        [Fact]
        public void Config_ControllerMute_DefaultsToFalse()
        {
            Assert.False(new TrackSimulatorConfig().ControllerMute);
        }

        [Fact]
        public void Config_ControllerMute_WhenSet_IsTrue()
        {
            Assert.True(new TrackSimulatorConfig(controllerMute: true).ControllerMute);
        }

        [Fact]
        public async Task Dispatch_WhenNotMuted_EmitsConnectionReply()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
            await transport.OpenAsync();
            try
            {
                await transport.SendAsync(ConnectionRequestFrame(), 0, 2, CancellationToken.None);

                // The controller replies CONTROLLER/CONNECTED/DONE to the 250 connection request.
                var frame = await ReadOneAsync(transport);
                Assert.Equal(0xAA, frame[0]);
                Assert.Equal(TaskId.CONTROLLER, frame[1]);
                Assert.Equal(TaskStates.CONNECTED, frame[2]);
                Assert.Equal(TaskStates.DONE, frame[3]);
            }
            finally
            {
                await transport.DisposeAsync();
            }
        }

        [Fact]
        public async Task Dispatch_WhenMuted_RecordsCommandButEmitsNoReply()
        {
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(controllerMute: true));
            await transport.OpenAsync();
            try
            {
                await transport.SendAsync(ConnectionRequestFrame(), 0, 2, CancellationToken.None);

                // "Transmitted": the command is still recorded in SentCommands ...
                Assert.Contains(EnumClientCommands.CLIENT_CONNECTION_REQUEST, transport.SentCommands);

                // ... but "never acknowledged": the silent controller emits no frame at all.
                Assert.Empty(await DrainAsync(transport));
            }
            finally
            {
                await transport.DisposeAsync();
            }
        }

        [Fact]
        public async Task Dispatch_WhenMuted_SilencesAllControlReplies()
        {
            // The mute is a controller-wide silence, not a 250-only drop: the version query (113)
            // also produces no reply.
            var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(controllerMute: true));
            await transport.OpenAsync();
            try
            {
                await transport.SendAsync(
                    new byte[] { 0x00, TrackCommand.GET_CONTROLLER_VERSION, 0x00, 0x00, 0x00 },
                    0, 5, CancellationToken.None);

                Assert.Contains(TrackCommand.GET_CONTROLLER_VERSION, transport.SentCommands);
                Assert.Empty(await DrainAsync(transport));
            }
            finally
            {
                await transport.DisposeAsync();
            }
        }

        private static async Task<byte[]> ReadOneAsync(DeterministicTrackTransport transport)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
            await foreach (var frame in transport.ReceiveAsync(cts.Token))
            {
                return frame;
            }

            throw new Xunit.Sdk.XunitException("No frame was emitted.");
        }

        private static async Task<List<byte[]>> DrainAsync(DeterministicTrackTransport transport)
        {
            var result = new List<byte[]>();
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));
            try
            {
                await foreach (var frame in transport.ReceiveAsync(cts.Token))
                {
                    result.Add(frame);
                }
            }
            catch (OperationCanceledException)
            {
                // Timeout reached with no frame: the silent controller is confirmed.
            }

            return result;
        }
    }
}
