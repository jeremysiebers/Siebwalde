// SiebwaldeApp.Tests/Station/StationSideTests.cs
using System.Threading;
using Xunit;
using SiebwaldeApp.Tests;

namespace SiebwaldeApp.Core.Tests
{
    public class StationSideTests
    {
        public StationSideTests()
        {
            // Replace IoC logger binding with our test stub
            IoC.Kernel.Rebind<ILogFactory>().ToConstant(new TestLogFactory());
        }

        private TrackApplication BuildAppForSide(string zone, int[] ids, int middle, out StationSide side)
        {
            var inBus  = new TestTrackIn();
            var outBus = new TestTrackOut();
            var app    = new TrackApplication(inBus, outBus);

            // Registry entries voor dit side
            foreach (var id in ids)
            {
                var entry  = new TrackSensor($"{id}-Entry");
                var exit   = new TrackSensor($"{id}-Exit");
                var signal = new Signal($"{id}-Head");
                var amp    = new Amplifier(id, outBus);
                app.Registry.Register(new TrackBlock(id, entry, exit, signal, amp),
                    new TrackMetadata { Zone = zone, Role = id == middle ? TrackRole.MiddleFreight : TrackRole.FreightAllowed });
            }

            side = new StationSide("Top", zone, middle, app, "Test");
            side.Start(CancellationToken.None);
            return app;
        }

        [Fact]
        public void Freight_Uses_Middle_If_Free()
        {
            var app = BuildAppForSide("StationTop", new[] {10,11,12}, 12, out var side);

            var free = side.GetFreeTrack(isFreight: true);
            Assert.NotNull(free);
            Assert.Equal(12, free.Number);
        }

        [Fact]
        public void Passenger_Uses_Outer_If_Free()
        {
            var app = BuildAppForSide("StationTop", new[] {10,11,12}, 12, out var side);

            var free = side.GetFreeTrack(isFreight: false);
            Assert.NotNull(free);
            Assert.Contains(free.Number, new[] {10,11});
        }
    }
}
