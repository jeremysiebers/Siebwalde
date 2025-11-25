using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp; // ReceivedMessage

namespace SiebwaldeApp.Core
{
    public enum InitStepResultKind
    {
        Continue,
        NextStep,
        Completed,
        Error
    }

    public sealed class InitStepResult
    {
        public InitStepResultKind Kind { get; }
        public string? NextStepName { get; }
        public string? ErrorMessage { get; }

        private InitStepResult(InitStepResultKind kind, string? nextStepName = null, string? errorMessage = null)
        {
            Kind = kind;
            NextStepName = nextStepName;
            ErrorMessage = errorMessage;
        }

        public static InitStepResult Continue() => new(InitStepResultKind.Continue);
        public static InitStepResult Next(string stepName) => new(InitStepResultKind.NextStep, stepName);
        public static InitStepResult Completed() => new(InitStepResultKind.Completed);
        public static InitStepResult Error(string message) => new(InitStepResultKind.Error, errorMessage: message);
    }

    public interface IInitializationStep
    {
        string Name { get; }

        /// <summary>
        /// Executes work for this step. Called repeatedly while this step is active.
        /// </summary>
        Task<InitStepResult> ExecuteAsync(ReceivedMessage? lastMessage, CancellationToken cancellationToken);
    }
}
