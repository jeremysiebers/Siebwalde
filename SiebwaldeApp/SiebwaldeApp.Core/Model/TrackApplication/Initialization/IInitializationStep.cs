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
        Error,
        /// <summary>
        /// The step detected a condition that requires an explicit operator action before the
        /// destructive path can proceed (for example a firmware mismatch that must never auto-flash).
        /// The state machine maps this to a <see cref="TrackInitStageState.Blocked"/> stage, not a
        /// failure, and names the recovery action (authorize firmware deployment).
        /// </summary>
        Blocked
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

        /// <summary>
        /// Fail-closed result for a condition that requires explicit authorization (never a silent
        /// error). The <see cref="ErrorMessage"/> carries the operator-facing reason.
        /// </summary>
        public static InitStepResult Blocked(string message) => new(InitStepResultKind.Blocked, errorMessage: message);
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
