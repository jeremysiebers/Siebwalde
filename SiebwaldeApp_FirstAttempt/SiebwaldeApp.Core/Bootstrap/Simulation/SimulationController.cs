using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    internal sealed class SimulationController : ISimulationController, IDisposable
    {
        private readonly SimulatedTrackBus _sim;
        private readonly Action<SimulatedTrackBus> _configureScenario;
        private CancellationTokenSource _localCts;
        private bool _configured;

        public SimulationController(SimulatedTrackBus sim, Action<SimulatedTrackBus> configureScenario)
        {
            _sim = sim;
            _configureScenario = configureScenario ?? (_ => { });
        }

        public Task StartAsync(CancellationToken token = default)
        {
            if (!_configured)
            {
                _configureScenario(_sim); // scenario pas nu “inprikken”
                _configured = true;
            }

            // combineer GUI-cts met lokale cts zodat Stop() ook werkt
            _localCts?.Cancel();
            _localCts = CancellationTokenSource.CreateLinkedTokenSource(token);
            IoC.Logger.Log("[Sim] Starting scenario after TrackApplication start", "");
            return _sim.StartAsync(_localCts.Token);
        }

        public void Stop()
        {
            try { _localCts?.Cancel(); } catch { }
            try { _sim.Stop(); } catch { }
        }

        public void Dispose() => Stop();
    }
}
