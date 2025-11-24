using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    /// <summary>
    /// Receives feedback from the hardware/backend layer when something
    /// has changed outside of a direct ECoS "set(...)" command.
    /// </summary>
    public interface IHardwareFeedbackSink
    {
        /// <summary>
        /// Called when a switch has changed state externally (hardware, automation, UI).
        /// </summary>
        /// <param name="ecosId">ECoS object id of the switch.</param>
        /// <param name="decoderAddress">Logical address used by the hardware layer.</param>
        /// <param name="outputIndex">0 = straight, 1 = diverging.</param>
        Task OnSwitchChangedAsync(int ecosId, int decoderAddress, int outputIndex);

        /// <summary>
        /// Called by external hardware code when an occupancy sensor changes state.
        /// </summary>
        /// <param name="sensorId">
        /// Logical sensor id as seen by ECoS/Koploper (e.g. contact number 1..n).
        /// </param>
        /// <param name="occupied">
        /// true = occupied, false = free.
        /// </param>
        Task OnSensorChangedAsync(int sensorId, bool occupied);
    }
}
