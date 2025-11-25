using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{ 
    public enum InitializationStatus
    {
        Idle,
        Running,
        Completed,
        Failed,
        Cancelled
    }

    public sealed class InitializationProgress
    {
        public string StepName { get; }
        public string Message { get; }
        public double? Percent { get; }

        public InitializationProgress(string stepName, string message, double? percent = null)
        {
            StepName = stepName;
            Message = message;
            Percent = percent;
        }
    }

    public interface ITrackAmplifierInitializationService
    {
        event EventHandler<InitializationProgress>? ProgressChanged;
        event EventHandler<InitializationStatus>? StatusChanged;

        /// <summary>
        /// Runs the full initialization and firmware process for all amplifiers.
        /// </summary>
        Task InitializeAsync(CancellationToken cancellationToken = default);
    }
}
