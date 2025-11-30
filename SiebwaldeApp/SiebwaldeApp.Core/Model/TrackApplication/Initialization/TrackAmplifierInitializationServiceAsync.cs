using System.Threading.Channels;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Async initialization service that orchestrates the initialization steps based on incoming control messages.
    /// </summary>
    public sealed class TrackAmplifierInitializationServiceAsync : ITrackAmplifierInitializationService
    {
        private readonly ITrackCommClient _commClient;
        private readonly TrackApplicationVariables _variables;
        private readonly Dictionary<string, IInitializationStep> _steps
        = new Dictionary<string, IInitializationStep>();
        private readonly Channel<ReceivedMessage> _controlMessages;

        private InitializationStatus _status = InitializationStatus.Idle;
        private readonly string _loggerInstance;

        public event EventHandler<InitializationProgress>? ProgressChanged;
        public event EventHandler<InitializationStatus>? StatusChanged;

        public IReadOnlyDictionary<string, IInitializationStep> Steps => _steps;

        /// <summary>
        /// TrackAmplifierInitializationServiceAsync
        /// </summary>
        /// <param name="commClient"></param>
        /// <param name="variables"></param>
        /// <param name="steps"></param>
        public TrackAmplifierInitializationServiceAsync(
            ITrackCommClient commClient,
            TrackApplicationVariables variables,
            IEnumerable<IInitializationStep> steps,
            string loggerInstance = "Track")
        {
            _commClient = commClient;
            _variables = variables;
            _loggerInstance = loggerInstance ?? "Track";

            foreach (var step in steps)
            {
                _steps[step.Name] = step;
            }

            _controlMessages = Channel.CreateUnbounded<ReceivedMessage>(
                new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

            _commClient.ControlMessageReceived += OnControlMessageReceived;
        }

        private void OnControlMessageReceived(object? sender, ControlMessageEventArgs e)
        {
            var m = e.Message;

            // NOTE: This log shows every control message coming from the transport.
            IoC.Logger.Log(
                $"[CONTROL] TaskId={m.TaskId}, Cmd={m.Taskcommand}, State={m.Taskstate}, Msg={m.Taskmessage}",
                _loggerInstance);

            _controlMessages.Writer.TryWrite(m);
        }


        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_status == InitializationStatus.Running)
                throw new InvalidOperationException("Initialization already running.");

            SetStatus(InitializationStatus.Running);
            RaiseProgress("Initialization", "Starting track amplifier initialization...");

            string currentStepName = "ConnectToEthernetTarget"; // initial step, must match a registered step
            ReceivedMessage? lastMessage = null;

            try
            {
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!_steps.TryGetValue(currentStepName, out var step))
                        throw new InvalidOperationException($"Unknown init step: {currentStepName}");

                    RaiseProgress(step.Name, "Executing step...");

                    lastMessage = await ReadNextMessageOrNullAsync(cancellationToken).ConfigureAwait(false);

                    var result = await step.ExecuteAsync(lastMessage, cancellationToken).ConfigureAwait(false);

                    switch (result.Kind)
                    {
                        case InitStepResultKind.Continue:
                            continue;

                        case InitStepResultKind.NextStep:
                            currentStepName = result.NextStepName
                                ?? throw new InvalidOperationException("Next step name may not be null.");
                            break;

                        case InitStepResultKind.Completed:
                            SetStatus(InitializationStatus.Completed);
                            RaiseProgress(step.Name, "Initialization completed.");
                            return;

                        case InitStepResultKind.Error:
                            SetStatus(InitializationStatus.Failed);
                            RaiseProgress(step.Name, $"Initialization error: {result.ErrorMessage}");
                            return;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                SetStatus(InitializationStatus.Cancelled);
                RaiseProgress(currentStepName, "Initialization cancelled.");
            }
            catch (Exception ex)
            {
                SetStatus(InitializationStatus.Failed);
                RaiseProgress(currentStepName, $"Initialization failed: {ex.Message}");
            }
        }

        private async Task<ReceivedMessage?> ReadNextMessageOrNullAsync(CancellationToken cancellationToken)
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromMilliseconds(100)); // tune as needed

            try
            {
                var reader = _controlMessages.Reader;
                var hasItem = await reader.WaitToReadAsync(timeoutCts.Token).ConfigureAwait(false);

                if (!hasItem) return null;
                return await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // timeout or cancellation -> treat as "no new message"
                return null;
            }
        }

        private void SetStatus(InitializationStatus status)
        {
            if (_status == status) return;
            _status = status;
            StatusChanged?.Invoke(this, status);
        }

        private void RaiseProgress(string stepName, string message, double? percent = null)
        {
            ProgressChanged?.Invoke(this, new InitializationProgress(stepName, message, percent));
        }
    }
}
