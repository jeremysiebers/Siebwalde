using System.Reflection;
using System.Net;
using System.Net.Sockets;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

/// <summary>
/// Software-only wiring checks: real composition with an inert comm client and isolated loopback
/// ports. The passive factory is constructed but NEVER refreshed/started (no process-memory reads).
/// Private-field probes avoid adding production API solely for composition-test observability.
/// </summary>
public class OvaaltjeLoop2CompositionTests
{
    [Fact]
    public async Task RuntimeSelection_OverridesSimpleLoop_RealCarrierAndFeedbackReceiveSelectedProfile()
    {
        Assert.True(LayoutProfileLoader.TryLoadByName("Simple Loop", out var initial, out _));
        Assert.True(LayoutProfileLoader.TryLoadByName("Ovaaltje Loop 2", out var selected, out _));
        var path = Path.Combine(Path.GetTempPath(), $"kis10-locos-{Guid.NewGuid():N}.json");
        using var host = new TrackControlHost(path, initial!.ToBlockTopology(), initial.ToKoploperBlockMap(),
            ecosListenPort: FreePort(), koploperExternalInfoHost: "127.0.0.1",
            koploperExternalInfoPort: FreePort());
        await using var runtime = new TrackApplicationRuntimeHost(host, fullSimulationProfile: initial);
        try
        {
            runtime.SelectFullSimulationProfile(initial);
            Assert.Equal(TrackRuntimeState.Stopped, runtime.State);
            runtime.SelectFullSimulationProfile(selected!);
            Assert.Same(selected, runtime.ActiveFullSimulationProfile);
            Assert.Equal("Ovaaltje Loop 2", runtime.ActiveProfileName);

            // Start only the ECoS composition, not the track runtime/init pipeline. No hardware
            // transport exists, and the movement permission remains fail-closed.
            await host.StartAsync(TrackControlMode.Real, new InertCommClient(), new TrackApplicationVariables());
            var carrier = Assert.IsType<LocomotiveCarrierController>(host.Carrier);
            var bindings = Field<IReadOnlyDictionary<int, IReadOnlyList<int>>>(carrier, "_blockToSections");
            Assert.Equal(new[] { 1, 2, 3 }, bindings.Keys.OrderBy(id => id));
            Assert.Equal(new[] { 1 }, bindings[1]);
            Assert.Equal(new[] { 2 }, bindings[2]);
            Assert.Equal(new[] { 3, 4 }, bindings[3]);
            var mapping = Field<IReadOnlyList<LayoutPhysicalAmplifierBinding>>(carrier, "_mapping");
            Assert.Equal(new[] { 4, 6 }, KoploperDriveGroupProjector.Project(3, bindings, mapping));
            var identity = Field<IReadOnlyDictionary<int, int>>(carrier, "_identity");
            Assert.Equal(1, identity[1]);
            Assert.Equal(2, identity[2]);

            var integration = Field<TrackControlIntegration>(host, "_integration");
            var feedback = Field<IReadOnlyList<KoploperFeedbackBlock>>(integration.Bridge, "_feedbackMap");
            var block = Assert.Single(feedback, b => b.BlockNumber == 3);
            Assert.Equal(new[] { 3, 4 }, block.Sections.Select(s => s.SectionId));
            Assert.Equal(new[] { 4, 6 }, block.Sections.Select(s => s.Amplifier));
            Assert.Equal(new[] { "1.03", "1.04" }, block.Sections.SelectMany(s => s.Bezetmelders));

            // The host rejects changes once composed; failed selection does not change the
            // runtime's effective profile (SetProfile is called before updating the selection).
            Assert.Throws<InvalidOperationException>(() => runtime.SelectFullSimulationProfile(initial));
            Assert.Same(selected, runtime.ActiveFullSimulationProfile);
        }
        finally
        {
            await host.StopAsync();
            File.Delete(path);
        }
    }

    [Fact]
    public void PassiveFactory_SelectedProfileFeedsOneToManyShadowBinding_WithoutStartingReaders()
    {
        Assert.True(LayoutProfileLoader.TryLoadByName("Ovaaltje Loop 2", out var profile, out _));
        var variables = new TrackApplicationVariables();
        using var observer = Kis09PassiveObserverFactory.Create(variables, profile!);
        Assert.Same(profile, observer.Profile);
        Assert.Same(variables, observer.Variables);
        var shadow = Field<KoploperLogicalSectionShadowObserver>(observer, "_shadowObserver");
        var bindings = Field<IReadOnlyDictionary<int, IReadOnlyList<int>>>(shadow, "_internalBlockToSections");
        Assert.Equal(new[] { 1, 2, 3 }, bindings.Keys.OrderBy(id => id));
        Assert.Equal(new[] { 1 }, bindings[1]);
        Assert.Equal(new[] { 2 }, bindings[2]);
        Assert.Equal(new[] { 3, 4 }, bindings[3]);
        Assert.Null(observer.ReadShadowObservation());
    }

    private static T Field<T>(object instance, string name)
    {
        var field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(field);
        return Assert.IsAssignableFrom<T>(field!.GetValue(instance));
    }

    private static int FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    private sealed class InertCommClient : ITrackCommClient
    {
        public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived { add { } remove { } }
        public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived { add { } remove { } }
        public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Composition test must never send track commands.");
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
