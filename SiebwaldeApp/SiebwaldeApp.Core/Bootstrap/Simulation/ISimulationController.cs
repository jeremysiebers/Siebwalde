namespace SiebwaldeApp.Core
{
    public interface ISimulationController
    {
        Task StartAsync(CancellationToken token = default);
        void Stop();
    }
}
