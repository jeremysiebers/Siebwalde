using System;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp; // TrackAmplifierItem, ReceivedMessage, SendMessage

namespace SiebwaldeApp.Core
{
    public sealed class AmplifierDataEventArgs : EventArgs
    {
        public int AmplifierIndex { get; }
        public TrackAmplifierItem Amplifier { get; }


        public AmplifierDataEventArgs(int amplifierIndex, TrackAmplifierItem amplifier)
        {
            AmplifierIndex = amplifierIndex;
            Amplifier = amplifier;
        }
    }

    public sealed class ControlMessageEventArgs : EventArgs
    {
        public ReceivedMessage Message { get; }

        public ControlMessageEventArgs(ReceivedMessage message)
        {
            Message = message;
        }
    }

    public interface ITrackCommClient : IAsyncDisposable
    {
        /// <summary>
        /// Raised when an amplifier data frame has been parsed and applied.
        /// </summary>
        event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;

        /// <summary>
        /// Raised when a controller / task / bootloader message has been parsed.
        /// </summary>
        event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;

        /// <summary>
        /// Starts the communication client in hardware or simulation mode.
        /// </summary>
        Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stops communication and releases resources.
        /// </summary>
        Task StopAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sends a command to the Ethernet/Modbus master.
        /// </summary>
        Task SendAsync(SendMessage message, CancellationToken cancellationToken = default);
    }
}
