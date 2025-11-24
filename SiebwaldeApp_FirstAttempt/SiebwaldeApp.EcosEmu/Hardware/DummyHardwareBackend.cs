using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiebwaldeApp.EcosEmu
{
    class DummyHardwareBackend : IHardwareBackend
    {
        private IHardwareFeedbackSink? _feedbackSink;

        /// <summary>
        /// Attaches a feedback sink so this hardware backend can report
        /// external changes (e.g. switch or sensor state) back to the ECoS backend.
        /// </summary>
        public void AttachFeedbackSink(IHardwareFeedbackSink sink)
        {
            _feedbackSink = sink ?? throw new ArgumentNullException(nameof(sink));
        }

        public void SetPower(bool on)
        {
            Console.WriteLine($"[HW] Power {(on ? "ON" : "OFF")}");
        }

        public void SetLocoSpeed(int address, int ecosSpeed, int direction)
        {
            Console.WriteLine($"[HW] Loco addr={address} speed={ecosSpeed} dir={direction}");
        }

        public void SetSwitch(int decoderAddress, int outputIndex, bool on)
        {
            Console.WriteLine($"[HW] Switch addr={decoderAddress} index={outputIndex} state={on}");

            // IMPORTANT:
            // Do NOT call back into the feedback sink here for now, because this
            // method is usually triggered by the ECoS backend itself in response
            // to a "set(...)" command from Koploper. If we reported a change here
            // as external feedback, we would create duplicate events.
            //
            // External switch changes (e.g. hardware/manual changes) should use
            // the helper method SimulateExternalSwitchChangeAsync instead.
        }

        /// <summary>
        /// Test helper: simulate an EXTERNAL switch change (not coming from Koploper).
        /// This will notify the ECoS backend via the feedback sink.
        /// </summary>
        /// <summary>
        /// Test helper: simulate an EXTERNAL switch change (not coming from Koploper).
        /// This will notify the ECoS backend via the feedback sink.
        /// </summary>
        public async Task SimulateExternalSwitchChangeAsync(int ecosId, int decoderAddress, int outputIndex)
        {
            if (_feedbackSink == null)
                return; // method is async Task → mag zonder waarde terugkeren

            Console.WriteLine($"[HW-SIM] External switch change ecosId={ecosId} addr={decoderAddress} idx={outputIndex}");

            // Delay 5 seconds
            await Task.Delay(TimeSpan.FromSeconds(5));

            Console.WriteLine($"[HW-SIM] External switch change ecosId={ecosId} addr={decoderAddress} idx={outputIndex} (after delay)");

            await _feedbackSink.OnSwitchChangedAsync(ecosId, decoderAddress, outputIndex);
        }

        /// <summary>
        /// Test helper: simulate an EXTERNAL sensor/occupancy change.
        /// </summary>
        public async Task SimulateExternalSensorChangeAsync(int sensorId, bool occupied)
        {
            if (_feedbackSink == null)
                return;

            Console.WriteLine($"[HW-SIM] External sensor change requested sensorId={sensorId} occupied={occupied}");

            // ADD DELAY HERE
            await Task.Delay(TimeSpan.FromSeconds(5));

            Console.WriteLine($"[HW-SIM] External sensor change sensorId={sensorId} occupied={occupied}");
            await _feedbackSink.OnSensorChangedAsync(sensorId, occupied);
        }
    }
}
