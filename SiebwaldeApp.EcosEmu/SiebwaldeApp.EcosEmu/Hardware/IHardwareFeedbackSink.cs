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
    }
}
