using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Unit tests for the LIVE_HARDWARE bounded telemetry-enablement outbound guard. The guard must
    /// forward exactly the six session commands in the mandated cold-start order, WAIT for each
    /// command's master DONE acknowledgement before the next is sent, deny out-of-order commands,
    /// accept only 250 in warm-reconnect mode, and fail-closed on 108/107/arbitrary commands.
    /// Transmitted and Acknowledged are tracked separately; movement counters stay zero.
    /// </summary>
    public class KoploperSessionCommandGuardTests
    {
        private static SendMessage Msg(byte command)
            => new SendMessage(command, new byte[] { 0, 0, 0 });

        private static async Task SendColdSequenceAsync(KoploperSessionCommandGuard guard)
        {
            foreach (byte command in KoploperSessionCommandAllowlist.ColdStartOrder)
            {
                await guard.SendAsync(Msg(command));
            }
        }

        private static ReceivedMessage AckFor(byte command) => command switch
        {
            250 => new ReceivedMessage(TaskId.CONTROLLER, TaskStates.CONNECTED, TaskStates.DONE, 0),
            107 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_RESET, TaskStates.DONE, 0),
            100 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVES_ON, TaskStates.DONE, 0),
            101 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_DETECT, TaskStates.DONE, 0),
            104 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_INIT, TaskStates.DONE, 0),
            106 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD, TaskStates.DONE, 0),
            105 => new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE, TaskStates.DONE, 0),
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
        {
            var sw = Stopwatch.StartNew();
            while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(10);
            }

            Assert.True(condition(), "condition not met within timeout");
        }

        [Fact]
        public async Task FullColdSequence_IsAcceptedInOrder_AndAcknowledged()
        {
            var inner = new FakeCommClient { AutoAck = m => AckFor(m.Command) };
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();

            await SendColdSequenceAsync(guard);

            Assert.Equal(6, inner.Sent.Count);
            Assert.Equal(
                KoploperSessionCommandAllowlist.ColdStartOrder,
                inner.Sent.Select(s => s.Command).ToArray());

            KoploperSessionCommandCounters counters = guard.GetCounters();
            Assert.Equal(6, counters.TotalTransmitted);
            Assert.Equal(6, counters.TotalAcknowledged);
            Assert.Equal(0, counters.AcknowledgementTimeouts);
            Assert.Equal(0, counters.RejectedCommands);
            Assert.Equal(0, counters.OrderViolations);
            Assert.False(guard.IsStopped);
            Assert.Equal(0, counters.TotalApplicationMovementControl);
        }

        [Fact]
        public async Task EachCommandWaitsForItsAckBeforeNextIsSent()
        {
            var inner = new FakeCommClient(); // no auto-ack
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();

            Task runTask = SendColdSequenceAsync(guard);

            for (int i = 0; i < KoploperSessionCommandAllowlist.ColdStartOrder.Count; i++)
            {
                byte command = KoploperSessionCommandAllowlist.ColdStartOrder[i];

                await WaitUntilAsync(() => inner.Sent.Count >= i + 1);
                Assert.True(inner.Sent.Count == i + 1, $"expected exactly {i + 1} sent command(s), found {inner.Sent.Count}");
                Assert.Equal(command, inner.Sent[i].Command);

                // No acknowledgement yet → the NEXT command must NOT be sent.
                await Task.Delay(50);
                Assert.True(inner.Sent.Count == i + 1, $"command {i + 1} must not be sent before command {i} is acknowledged");

                inner.Emit(AckFor(command));
            }

            await runTask;
            Assert.Equal(6, guard.GetCounters().TotalAcknowledged);
            Assert.Equal(6, guard.GetCounters().TotalTransmitted);
        }

        [Fact]
        public async Task MissingAck_Timeout_ReportsNotAcknowledged_AndStops()
        {
            var inner = new FakeCommClient(); // no auto-ack
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromMilliseconds(100));
            guard.BeginNewSession();

            await Assert.ThrowsAsync<KoploperSessionCommandAcknowledgementTimeoutException>(
                () => guard.SendAsync(Msg(KoploperSessionCommandAllowlist.ClientConnectionRequest)));

            Assert.Equal(1, inner.Sent.Count); // 250 was transmitted
            Assert.True(guard.IsStopped);

            KoploperSessionCommandCounters counters = guard.GetCounters();
            Assert.Equal(1, counters.ClientConnectionRequestTransmitted);
            Assert.Equal(0, counters.ClientConnectionRequestAcknowledged);
            Assert.Equal(1, counters.AcknowledgementTimeouts);
            Assert.Equal(0, counters.TotalAcknowledged);

            // The sequence stopped before reaching 105.
            Assert.DoesNotContain(inner.Sent, s => s.Command == KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable);

            // Session is stopped: even a correct next command is refused.
            await Assert.ThrowsAsync<KoploperSessionCommandOrderViolationException>(
                () => guard.SendAsync(Msg(KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn)));
        }

        [Fact]
        public async Task AcknowledgedCounter_IncrementsOnlyOnMatchingDone()
        {
            var inner = new FakeCommClient(); // no auto-ack
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();

            Task sendTask = guard.SendAsync(Msg(KoploperSessionCommandAllowlist.ClientConnectionRequest));
            await WaitUntilAsync(() => inner.Sent.Count == 1);

            // A non-matching status frame must NOT acknowledge 250.
            inner.Emit(new ReceivedMessage(TaskId.MBUS, EnumMbusStatus.MBUS_STATE_SLAVES_ON, TaskStates.DONE, 0));
            await Task.Delay(50);
            Assert.False(sendTask.IsCompleted);
            Assert.Equal(0, guard.GetCounters().ClientConnectionRequestAcknowledged);

            // The matching CONTROLLER/CONNECTED/DONE frame acknowledges it.
            inner.Emit(AckFor(250));
            await sendTask;

            Assert.Equal(1, guard.GetCounters().ClientConnectionRequestTransmitted);
            Assert.Equal(1, guard.GetCounters().ClientConnectionRequestAcknowledged);
        }

        [Fact]
        public async Task WarmReconnect_WaitsForConnectedDone()
        {
            var inner = new FakeCommClient(); // no auto-ack
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginWarmReconnect();

            Task sendTask = guard.SendAsync(Msg(KoploperSessionCommandAllowlist.ClientConnectionRequest));
            await WaitUntilAsync(() => inner.Sent.Count == 1);

            await Task.Delay(50);
            Assert.False(sendTask.IsCompleted);
            Assert.Equal(0, guard.GetCounters().ClientConnectionRequestAcknowledged);

            inner.Emit(AckFor(250));
            await sendTask;
            Assert.Equal(1, guard.GetCounters().ClientConnectionRequestAcknowledged);
        }

        [Fact]
        public async Task SlavesOnBeforeConnectionRequest_IsRejectedAndStopsSession()
        {
            var inner = new FakeCommClient();
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();

            var ex = await Assert.ThrowsAsync<KoploperSessionCommandOrderViolationException>(
                () => guard.SendAsync(Msg(KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn)));

            Assert.Equal(KoploperSessionCommandOrderViolationReason.OutOfOrder, ex.Reason);
            Assert.Equal(KoploperSessionCommandAllowlist.ClientConnectionRequest, ex.ExpectedCommand);
            Assert.Empty(inner.Sent);
            Assert.True(guard.IsStopped);
            Assert.Equal(1, guard.OrderViolationCount);
        }

        [Fact]
        public async Task SlaveEnableBeforeStartDataUpload_IsRejected()
        {
            var inner = new FakeCommClient { AutoAck = m => AckFor(m.Command) };
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();

            foreach (byte command in new byte[] { 250, 100, 101, 104 })
            {
                await guard.SendAsync(Msg(command));
            }

            var ex = await Assert.ThrowsAsync<KoploperSessionCommandOrderViolationException>(
                () => guard.SendAsync(Msg(KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable)));

            Assert.Equal(KoploperSessionCommandOrderViolationReason.OutOfOrder, ex.Reason);
            Assert.Equal(KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload, ex.ExpectedCommand);
            Assert.True(guard.IsStopped);
        }

        [Fact]
        public async Task StartDataUploadAfterSlaveEnable_IsRejected_UnlessNewSession()
        {
            var inner = new FakeCommClient { AutoAck = m => AckFor(m.Command) };
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();
            await SendColdSequenceAsync(guard);

            var ex = await Assert.ThrowsAsync<KoploperSessionCommandOrderViolationException>(
                () => guard.SendAsync(Msg(KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload)));

            Assert.Equal(KoploperSessionCommandOrderViolationReason.ColdSequenceComplete, ex.Reason);
            Assert.True(guard.IsStopped);

            guard.BeginNewSession();
            Assert.False(guard.IsStopped);
            Assert.Equal(KoploperSessionCommandAllowlist.ClientConnectionRequest, guard.ExpectedCommand);

            await guard.SendAsync(Msg(KoploperSessionCommandAllowlist.ClientConnectionRequest));
            Assert.Equal(2, guard.GetCounters().ClientConnectionRequestAcknowledged);
        }

        [Fact]
        public async Task AmplifierRegisterWrite108_AlwaysRejectedAndNotTransmitted()
        {
            var inner = new FakeCommClient { AutoAck = m => AckFor(m.Command) };
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();

            var ex = await Assert.ThrowsAsync<KoploperSessionCommandRejectedException>(
                () => guard.SendAsync(Msg(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH)));

            Assert.Equal(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH, ex.Command);
            Assert.Empty(inner.Sent);
            Assert.Equal(1, guard.RejectedCommandCount);
            Assert.Equal(0, guard.GetCounters().AmplifierRegisterWrite);
        }

        [Fact]
        public async Task Reset107_AlwaysRejectedAndNotTransmitted()
        {
            var inner = new FakeCommClient();
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();

            await Assert.ThrowsAsync<KoploperSessionCommandRejectedException>(
                () => guard.SendAsync(Msg(TrackCommand.EXEC_MBUS_STATE_RESET)));

            Assert.Empty(inner.Sent);
            Assert.Equal(1, guard.RejectedCommandCount);
        }

        [Fact]
        public async Task ApplyConfigMode_Accepts107InOrder_AndAcknowledges()
        {
            var inner = new FakeCommClient { AutoAck = m => AckFor(m.Command) };
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginApplyConfigSession();

            foreach (byte command in KoploperSessionCommandApplyAllowlist.ApplyConfigOrder)
            {
                await guard.SendAsync(Msg(command));
            }

            Assert.Equal(
                KoploperSessionCommandApplyAllowlist.ApplyConfigOrder,
                inner.Sent.Select(s => s.Command).ToArray());

            KoploperSessionCommandCounters counters = guard.GetCounters();
            Assert.Equal(7, counters.TotalTransmitted);
            Assert.Equal(7, counters.TotalAcknowledged);
            Assert.Equal(1, counters.ExecMbusStateResetTransmitted);
            Assert.Equal(1, counters.ExecMbusStateResetAcknowledged);
            Assert.Equal(0, counters.RejectedCommands);
            Assert.Equal(0, counters.OrderViolations);
            Assert.False(guard.IsStopped);
        }

        [Fact]
        public async Task ApplyConfigMode_ResetBeforeConnect_IsOutOfOrder()
        {
            var inner = new FakeCommClient();
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginApplyConfigSession();

            // 107 is permitted in apply-config mode but must still follow 250.
            var ex = await Assert.ThrowsAsync<KoploperSessionCommandOrderViolationException>(
                () => guard.SendAsync(Msg(TrackCommand.EXEC_MBUS_STATE_RESET)));

            Assert.Equal(KoploperSessionCommandOrderViolationReason.OutOfOrder, ex.Reason);
            Assert.Equal(KoploperSessionCommandAllowlist.ClientConnectionRequest, ex.ExpectedCommand);
            Assert.Empty(inner.Sent);
            Assert.True(guard.IsStopped);
        }

        [Fact]
        public async Task ArbitraryCommand_IsRejectedAndNotTransmitted()
        {
            var inner = new FakeCommClient();
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();

            var ex = await Assert.ThrowsAsync<KoploperSessionCommandRejectedException>(
                () => guard.SendAsync(Msg(0x7F)));

            Assert.Equal(0x7F, ex.Command);
            Assert.Empty(inner.Sent);
            Assert.Equal(1, guard.RejectedCommandCount);
        }

        [Fact]
        public async Task SessionCounters_AccumulateAcrossSessions()
        {
            var inner = new FakeCommClient { AutoAck = m => AckFor(m.Command) };
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));

            guard.BeginNewSession();
            await SendColdSequenceAsync(guard);

            guard.BeginNewSession();
            await SendColdSequenceAsync(guard);

            KoploperSessionCommandCounters counters = guard.GetCounters();
            Assert.Equal(12, counters.TotalTransmitted);
            Assert.Equal(12, counters.TotalAcknowledged);
            Assert.Equal(2, counters.ClientConnectionRequestTransmitted);
            Assert.Equal(2, counters.ClientConnectionRequestAcknowledged);
            Assert.Equal(2, counters.ExecMbusStateSlaveEnableTransmitted);
            Assert.Equal(2, counters.ExecMbusStateSlaveEnableAcknowledged);
            Assert.Equal(0, counters.TotalApplicationMovementControl);
        }

        [Fact]
        public async Task TelemetryAndMovementCounters_AreSeparate_MovementAlwaysZero()
        {
            var inner = new FakeCommClient { AutoAck = m => AckFor(m.Command) };
            var guard = new KoploperSessionCommandGuard(inner, TimeSpan.FromSeconds(5));
            guard.BeginNewSession();
            await SendColdSequenceAsync(guard);

            await Assert.ThrowsAsync<KoploperSessionCommandRejectedException>(
                () => guard.SendAsync(Msg(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH)));
            await Assert.ThrowsAsync<KoploperSessionCommandRejectedException>(
                () => guard.SendAsync(Msg(TrackCommand.EXEC_MBUS_STATE_RESET)));

            KoploperSessionCommandCounters counters = guard.GetCounters();
            Assert.Equal(6, counters.TotalTransmitted);
            Assert.Equal(6, counters.TotalAcknowledged);
            Assert.Equal(0, counters.AmplifierRegisterWrite);
            Assert.Equal(0, counters.AmplifierHr0Writes);
            Assert.Equal(0, counters.PwmWrites);
            Assert.Equal(0, counters.DirectionWrites);
            Assert.Equal(0, counters.BrakeWrites);
            Assert.Equal(0, counters.EmoStopWrites);
            Assert.Equal(0, counters.SwitchWrites);
            Assert.Equal(0, counters.LocomotiveCommands);
            Assert.Equal(0, counters.TotalApplicationMovementControl);
            Assert.Equal(2, counters.RejectedCommands);
        }

        [Fact]
        public void Allowlist_PermitsExactlyTheSixSessionCommands()
        {
            Assert.True(KoploperSessionCommandAllowlist.IsPermitted(KoploperSessionCommandAllowlist.ClientConnectionRequest));
            Assert.True(KoploperSessionCommandAllowlist.IsPermitted(KoploperSessionCommandAllowlist.ExecMbusStateSlavesOn));
            Assert.True(KoploperSessionCommandAllowlist.IsPermitted(KoploperSessionCommandAllowlist.ExecMbusStateSlaveDetect));
            Assert.True(KoploperSessionCommandAllowlist.IsPermitted(KoploperSessionCommandAllowlist.ExecMbusStateSlaveInit));
            Assert.True(KoploperSessionCommandAllowlist.IsPermitted(KoploperSessionCommandAllowlist.ExecMbusStateStartDataUpload));
            Assert.True(KoploperSessionCommandAllowlist.IsPermitted(KoploperSessionCommandAllowlist.ExecMbusStateSlaveEnable));

            byte[] forbidden =
            {
                TrackCommand.EXEC_MBUS_STATE_SLAVES_BOOT_WAIT,
                TrackCommand.EXEC_MBUS_STATE_SLAVE_FW_FLASH,
                TrackCommand.EXEC_MBUS_STATE_RESET,
                TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH,
                TrackCommand.EXEC_FW_STATE_FLASH_ALL_SLAVES,
                TrackCommand.EXEC_FW_STATE_SLAVE_RESET,
                0x00,
                0x7F,
                0xFF
            };

            foreach (byte command in forbidden)
            {
                Assert.False(
                    KoploperSessionCommandAllowlist.IsPermitted(command),
                    $"command 0x{command:X2} must not be permitted");
            }
        }

        private sealed class FakeCommClient : ITrackCommClient
        {
            public List<SendMessage> Sent { get; } = new List<SendMessage>();

            public Func<SendMessage, ReceivedMessage?>? AutoAck { get; set; }

            public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
            public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

            public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task StopAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;

            public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            {
                Sent.Add(message);

                if (AutoAck is not null)
                {
                    ReceivedMessage? ack = AutoAck(message);
                    if (ack is ReceivedMessage a)
                    {
                        ControlMessageReceived?.Invoke(this, new ControlMessageEventArgs(a));
                    }
                }

                return Task.CompletedTask;
            }

            public void Emit(ReceivedMessage message)
                => ControlMessageReceived?.Invoke(this, new ControlMessageEventArgs(message));

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
