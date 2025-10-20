// SiebwaldeApp.Tests/Station/StationControllerTests.cs
using SiebwaldeApp.Tests;
using System.Threading.Tasks;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Station
{
    [Collection("IoC bootstrap")]

    public class StationControllerTests
    {
        public StationControllerTests()
        {
            // Replace IoC logger binding with our test stub
            //IoC.Kernel.Rebind<ILogFactory>().ToConstant(new TestLogFactory());
        }

        private (TrackApplication app, TestTrackIn inBus, TestTrackOut outBus) BuildTrackAppWithStation()
        {
            var inBus = new TestTrackIn();
            var outBus = new TestTrackOut();
            var app = new TrackApplication(inBus, outBus);

            // Register 6 station tracks
            void Add(int id, string zone, TrackRole role)
            {
                var entry = new TrackSensor($"{id}-Entry");
                var exit = new TrackSensor($"{id}-Exit");
                var signal = new Signal($"{id}-Head");
                var amp = new Amplifier(id, outBus);
                app.Registry.Register(new TrackBlock(id, entry, exit, signal, amp),
                    new TrackMetadata { Zone = zone, Role = role });
            }

            Add(10, "StationTop", TrackRole.FreightAllowed);
            Add(11, "StationTop", TrackRole.FreightAllowed);
            Add(12, "StationTop", TrackRole.MiddleFreight);
            Add(1, "StationBottom", TrackRole.FreightAllowed);
            Add(2, "StationBottom", TrackRole.FreightAllowed);
            Add(3, "StationBottom", TrackRole.MiddleFreight);

            return (app, inBus, outBus);
        }

        [Fact]
        public async Task Incoming_Passenger_Top_Reserves_Outer_And_Entry_Green()
        {
            var (app, inBus, outBus) = BuildTrackAppWithStation();
            await app.StartAsync();

            inBus.RaiseIncoming(isTop: true, isFreight: false);

            // er wordt een entry-sein groen gezet
            Assert.Contains(outBus.Sent, c => c.Name == nameof(TestTrackOut.SetSignalEntry) && 
                (bool)c.Args[0] == true && (bool)c.Args[1] == true);
        }

        [Fact]
        public async Task No_Free_Track_Stops_Before_Station()
        {
            var (app, inBus, outBus) = BuildTrackAppWithStation();
            await app.StartAsync();

            // Vul alle Top-outer tracks bezet simulatie: reserve + occupy
            var top = app.Station.TopStation;
            var t10 = top.GetByNumber(10); t10?.Reserve(); t10?.Occupy(TrainType.Passenger);
            var t11 = top.GetByNumber(11); t11?.Reserve(); t11?.Occupy(TrainType.Passenger);
            var t12 = top.GetByNumber(12); t12?.Reserve(); t12?.Occupy(TrainType.Freight);

            inBus.RaiseIncoming(isTop: true, isFreight: false);

            Assert.Contains(outBus.Sent, c => c.Name == nameof(TestTrackOut.StopBeforeStation) && 
                (bool)c.Args[0] == true);
        }
    }
}
