// SiebwaldeApp.Tests/Station/StationTrackTests.cs
using System;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Station
{
    public class StationTrackTests
    {
        public StationTrackTests()
        {
            // Replace IoC logger binding with our test stub
            IoC.Kernel.Rebind<ILogFactory>().ToConstant(new TestLogFactory());
        }

        [Fact]
        public void Passenger_Ready_After_Dwell_And_ExitFree()
        {
            var t = new StationTrack(10, "Test");
            t.PassengerDwell = TimeSpan.Zero;
            t.Occupy(TrainType.Passenger);

            Assert.True(t.IsReadyToDepart(exitFree: true));
            Assert.False(t.IsReadyToDepart(exitFree: false));
        }

        [Fact]
        public void Freight_Ready_When_ExitFree_And_MinDwell()
        {
            var t = new StationTrack(12, "Test");
            t.FreightMinDwell = TimeSpan.Zero;
            t.Occupy(TrainType.Freight);

            Assert.True(t.IsReadyToDepart(exitFree: true));
            Assert.False(t.IsReadyToDepart(exitFree: false));
        }
    }
}
