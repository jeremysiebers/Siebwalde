using Ninject;
using Ninject.Modules;
using System;

namespace SiebwaldeApp.Core
{
    public sealed class SimulationModule : NinjectModule
    {
        private readonly Action<SimulatedTrackBus> _configureScenario;

        public SimulationModule(Action<SimulatedTrackBus> configureScenario = null)
        {
            _configureScenario = configureScenario ?? DefaultScenario;
        }

        public override void Load()
        {
            // 1) Eén simulator instance, GEEN Start in OnActivation!
            Bind<SimulatedTrackBus>()
                .ToSelf()
                .InSingletonScope()
                .OnDeactivation((ctx, sim) => sim.Dispose());

            // 2) Poorten mappen
            Bind<ITrackIn>().ToMethod(ctx => ctx.Kernel.Get<SimulatedTrackBus>()).InSingletonScope();
            Bind<ITrackOut>().ToMethod(ctx => ctx.Kernel.Get<SimulatedTrackBus>()).InSingletonScope();

            // 3) Yard (nog) fake
            Bind<IYardIn>().To<FakeYardAdapter>().InSingletonScope();
            Bind<IYardOut>().To<FakeYardAdapter>().InSingletonScope();

            // 4) Controller die we vanuit de GUI starten
            Bind<ISimulationController>()
                .ToMethod(ctx => new SimulationController(
                    ctx.Kernel.Get<SimulatedTrackBus>(),
                    _configureScenario))
                .InSingletonScope();
        }

        private static void DefaultScenario(SimulatedTrackBus sim)
        {
            var t0 = DateTime.UtcNow.AddSeconds(1);
            sim.Add(
                new AliveStep(t0, true),
                new ExitFreeStep(t0.AddMilliseconds(100), true, true),
                new IncomingStep(t0.AddMilliseconds(400), true, false),
                new EntrySensorStep(t0.AddMilliseconds(900), true, 12),
                new AmplifierOccStep(t0.AddMilliseconds(1100), 12, true),
                new TrainClearedStep(t0.AddSeconds(4), true, 12),
                new AmplifierOccStep(t0.AddSeconds(4).AddMilliseconds(200), 12, false)
            );
        }
    }
}
