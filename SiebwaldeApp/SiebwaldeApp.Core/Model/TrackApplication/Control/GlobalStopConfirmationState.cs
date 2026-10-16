using System;
using System.Collections.Generic;

namespace SiebwaldeApp.Core
{
    /// <summary>
    /// Runtime-owned consumer of the <c>GLOBAL_STOP_STATE</c> (111) per-amplifier confirmation
    /// payload. It latches the most recent payload and decodes it against the ordered detected
    /// amplifier list. Release/exit is blocked while any amplifier is non-confirmed (fail closed).
    /// </summary>
    public sealed class GlobalStopConfirmationState : IDisposable
    {
        private readonly ITrackCommClient _comm;
        private IReadOnlyList<ushort> _detected = Array.Empty<ushort>();
        private byte[]? _lastPayload;

        public GlobalStopConfirmationState(ITrackCommClient comm)
        {
            _comm = comm ?? throw new ArgumentNullException(nameof(comm));
            _comm.ControlMessageReceived += OnMessage;
        }

        /// <summary>Sets the ordered amplifier list the 2-bit-per-amplifier payload is decoded against.</summary>
        public void SetDetectedAmplifiers(IReadOnlyList<ushort> detected)
        {
            _detected = detected ?? Array.Empty<ushort>();
        }

        /// <summary>The last decoded per-amplifier confirmation map (empty until a payload arrives).</summary>
        public IReadOnlyDictionary<ushort, AmplifierStopConfirmation> Confirmations
            => TrackAmplifierStopConfirmation.Decode(_lastPayload, _detected);

        /// <summary>True only when every detected amplifier is stop-confirmed.</summary>
        public bool AreAllConfirmed
            => TrackAmplifierStopConfirmation.AllConfirmed(_lastPayload, _detected);

        /// <summary>Exposed for tests: feeds a decoded 111 payload directly.</summary>
        public void FeedPayload(byte[]? payload) => _lastPayload = payload;

        private void OnMessage(object? sender, ControlMessageEventArgs e)
        {
            var message = e.Message;
            if (message.TaskId == TaskId.CONTROLLER && message.Taskcommand == TrackCommand.GLOBAL_STOP_STATE)
            {
                _lastPayload = message.Payload;
            }
        }

        public void Dispose() => _comm.ControlMessageReceived -= OnMessage;
    }
}
