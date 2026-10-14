using SiebwaldeApp.Core.TrackApplication.Simulator;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

public class TrackCommClientAsyncLifecycleTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ImmediateStartThenTeardown_DoesNotFault(bool dispose)
    {
        // Run on a dedicated thread so Start and teardown execute back-to-back without
        // yielding to the pool that runs the receive delegate. Repeat to exercise both
        // scheduling orders; do not alter process-wide thread-pool limits or use sleeps.
        await Task.Factory.StartNew(() =>
        {
            for (int iteration = 0; iteration < 256; iteration++)
            {
                var transport = new DeterministicTrackTransport(new TrackSimulatorConfig());
                var comm = new TrackCommClientAsync(transport, new TrackApplicationVariables());
                try
                {
                    var start = comm.StartAsync(false);
                    Assert.True(start.IsCompletedSuccessfully);
                    if (dispose)
                        comm.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    else
                    {
                        comm.StopAsync().GetAwaiter().GetResult();
                        comm.DisposeAsync().AsTask().GetAwaiter().GetResult();
                    }

                    Assert.Equal(1, transport.OpenCallCount);
                    Assert.True(transport.CloseCallCount >= 1);
                    Assert.Equal(1, transport.DisposeCallCount);
                }
                finally
                {
                    // Also release the transport if the pre-fix receive task faults.
                    if (!transport.IsDisposed)
                        transport.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
            }
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
}
