using System;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Initialization step that sets the default PWM setpoint (neutral, 399)
    /// for all track amplifiers before they are enabled.
    /// This only writes HoldingReg0 bits 0..9 and does not enable power.
    /// </summary>
    public sealed class SetDefaultPwmSetpointsStep : IInitializationStep
    {
        private readonly TrackApplicationVariables _variables;
        private readonly string _loggerInstance;
        private bool _done;

        public string Name => "SetDefaultPwmSetpoints";

        public SetDefaultPwmSetpointsStep(
            TrackApplicationVariables variables,
            string loggerInstance)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _loggerInstance = loggerInstance;
        }

        public Task<InitStepResult> ExecuteAsync(ReceivedMessage? lastMessage, CancellationToken cancellationToken)
        {
            if (_done)
            {
                // Nothing to do anymore; proceed to next step.
                return Task.FromResult(InitStepResult.Next("EnableTrackamplifiers"));
            }

            // Default idle PWM setpoint is neutral (standstill, dual-sided PWM).
            _variables.InitializeDefaultPwmSetpoints(AmplifierSpeedMapper.NeutralPwm);

            IoC.Logger.Log(
                $"Init: Default PWM setpoints set to {AmplifierSpeedMapper.NeutralPwm} (neutral) for all track amplifiers.",
                _loggerInstance);

            _done = true;

            // We are done; proceed to next step.
            return Task.FromResult(InitStepResult.Next("EnableTrackamplifiers"));
        }
    }
}
