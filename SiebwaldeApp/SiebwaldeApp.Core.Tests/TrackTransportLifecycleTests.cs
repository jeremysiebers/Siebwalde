using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using SiebwaldeApp.Core.TrackApplication.Comm;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

public class TrackTransportLifecycleTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task CommReceiveFault_CleansUpAndPreservesOriginalFailure(bool dispose, bool cleanupFaults)
    {
        var transport = new ControlledTransport { CleanupFaults = cleanupFaults };
        var comm = new TrackCommClientAsync(transport, new TrackApplicationVariables());
        await comm.StartAsync(false);
        await transport.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var original = new InvalidDataException("injected receive failure");
        transport.Frame.TrySetException(original);

        var thrown = await Assert.ThrowsAsync<InvalidDataException>(() =>
            dispose ? comm.DisposeAsync().AsTask() : comm.StopAsync());
        Assert.Same(original, thrown);
        Assert.Contains(nameof(ControlledTransport.ReceiveAsync), thrown.StackTrace);
        Assert.True(transport.Token.IsCancellationRequested);
        Assert.Equal(1, transport.CloseCount);
        Assert.Equal(dispose ? 1 : 0, transport.DisposeCount);

        // Stop is already complete, including after a fault; disposal retains its
        // existing delegation semantics (the transport must tolerate repeat disposal).
        transport.CleanupFaults = false;
        await comm.StopAsync();
        await comm.StopAsync();
        await comm.DisposeAsync();
        await comm.DisposeAsync();
        Assert.Equal(1, transport.CloseCount);
        Assert.Equal(dispose ? 3 : 2, transport.DisposeCount);
    }

    [Fact]
    public async Task CommCancellation_StopsAndDisposesRepeatedlyWithoutFault()
    {
        var transport = new ControlledTransport();
        var comm = new TrackCommClientAsync(transport, new TrackApplicationVariables());
        await comm.StartAsync(false);
        await transport.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await comm.StopAsync();
        await comm.StopAsync();
        await comm.DisposeAsync();
        await comm.DisposeAsync();
        Assert.True(transport.Token.IsCancellationRequested);
        Assert.Equal(1, transport.CloseCount);
        Assert.Equal(2, transport.DisposeCount);
    }

    [Fact]
    public async Task RawUdpLateCallback_UsesCancelledToken_AndChannelCompletes()
    {
        var inner = new ControlledRawUdp();
        var transport = new RawUdpTrackTransport(inner);
        await transport.OpenAsync();
        Assert.False(inner.Token.IsCancellationRequested);
        await transport.CloseAsync();
        var failure = await Record.ExceptionAsync(() => inner.Callback!(new byte[] { 1 }));
        Assert.IsAssignableFrom<OperationCanceledException>(failure);
        Assert.True(inner.Token.IsCancellationRequested);
        await AssertCompleted(transport);
        await transport.CloseAsync();
        await transport.DisposeAsync();
        await transport.DisposeAsync();
        Assert.Equal(2, inner.DisposeCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawUdpReceiveFault_CompletesChannelAndPreservesFailure(bool dispose)
    {
        var inner = new ControlledRawUdp();
        var original = new InvalidDataException("injected UDP receive failure");
        var loop = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        inner.Loop = loop.Task;
        var transport = new RawUdpTrackTransport(inner);
        await transport.OpenAsync();
        loop.SetException(original);
        var thrown = await Assert.ThrowsAsync<InvalidDataException>(() =>
            dispose ? transport.DisposeAsync().AsTask() : transport.CloseAsync());
        Assert.Same(original, thrown);
        Assert.True(inner.Token.IsCancellationRequested);
        await AssertCompleted(transport);
        Assert.Equal(dispose ? 1 : 0, inner.DisposeCount);
        await transport.CloseAsync();
        await transport.DisposeAsync();
        await transport.DisposeAsync();
        Assert.Equal(dispose ? 3 : 2, inner.DisposeCount);
    }

    [Fact]
    public async Task SimulatorImmediateClose_HeartbeatExitsWithoutFault_AndChannelCompletes()
    {
        // The task is private and CloseAsync intentionally does not join it. Inspect
        // it in this regression test so a fault cannot hide as an unobserved task.
        var heartbeatField = typeof(DeterministicTrackTransport).GetField(
            "_heartbeatTask", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await Task.Factory.StartNew(() =>
        {
            for (int i = 0; i < 128; i++)
            {
                var transport = new DeterministicTrackTransport(new TrackSimulatorConfig(
                    periodicSlaveInfoInterval: TimeSpan.FromHours(1)));
                transport.OpenAsync().GetAwaiter().GetResult();
                var heartbeat = Assert.IsAssignableFrom<Task>(heartbeatField.GetValue(transport));
                transport.CloseAsync().GetAwaiter().GetResult();
                heartbeat.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                Assert.True(heartbeat.IsCompletedSuccessfully);
                AssertCompleted(transport).GetAwaiter().GetResult();
                transport.CloseAsync().GetAwaiter().GetResult();
                transport.DisposeAsync().AsTask().GetAwaiter().GetResult();
                transport.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    private static async Task AssertCompleted(ITrackTransport transport)
    {
        await using var reader = transport.ReceiveAsync().GetAsyncEnumerator();
        Assert.False(await reader.MoveNextAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(5)));
    }

    private sealed class ControlledTransport : ITrackTransport
    {
        public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource<byte[]> Frame { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken Token;
        public bool CleanupFaults;
        public int CloseCount;
        public int DisposeCount;
        public Task OpenAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public async IAsyncEnumerable<byte[]> ReceiveAsync(
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            Token = cancellationToken;
            using var registration = cancellationToken.Register(() => Frame.TrySetCanceled(cancellationToken));
            Entered.TrySetResult();
            yield return await Frame.Task;
        }
        public Task CloseAsync(CancellationToken cancellationToken = default)
        {
            CloseCount++;
            if (CleanupFaults) throw new IOException("injected close failure");
            return Task.CompletedTask;
        }
        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            if (CleanupFaults) throw new IOException("injected dispose failure");
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ControlledRawUdp : IRawUdpTransport
    {
        public EndPoint RemoteEndPointClient => new IPEndPoint(IPAddress.Loopback, 1);
        public EndPoint RemoteEndPointHost => RemoteEndPointClient;
        public Func<byte[], Task>? Callback;
        public CancellationToken Token;
        public Task Loop = Task.CompletedTask;
        public int DisposeCount;
        public void Send(byte[] payload) { }
        public Task StartReceiveLoopAsync(Func<byte[], Task> onFrameAsync, CancellationToken token)
        {
            Callback = onFrameAsync;
            Token = token;
            return Loop;
        }
        public void Dispose() => DisposeCount++;
    }
}
