using System;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Sits between the ECoS backend and a hardware backend and routes every switch command
    /// through the shared <see cref="SwitchController"/>, so real and simulator mode use the
    /// same translation. Power and locomotive commands are forwarded unchanged.
    ///
    /// ECoS addresses a turnout by the active coil: <c>switch[&lt;addr&gt;g]</c> means
    /// "drive the straight coil", <c>switch[&lt;addr&gt;r]</c> the diverging coil, which the
    /// ECoS backend passes on as output index 0 and 1 with <c>on = true</c>.
    /// </summary>
    public sealed class SwitchTranslatingHardwareBackend : IHardwareBackend
    {
        private readonly IHardwareBackend _inner;
        private readonly SwitchController _controller;
        private readonly Action<string>? _log;

        public SwitchTranslatingHardwareBackend(
            IHardwareBackend inner,
            SwitchController controller,
            Action<string>? log = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _log = log;
        }

        /// <inheritdoc />
        public void SetPower(bool on) => _inner.SetPower(on);

        /// <inheritdoc />
        public void SetLocoSpeed(int address, int ecosSpeed, int direction)
            => _inner.SetLocoSpeed(address, ecosSpeed, direction);

        /// <inheritdoc />
        public bool SetSwitch(int decoderAddress, int outputIndex, bool on)
        {
            if (!on)
            {
                // A two-coil turnout is positioned by energising one coil; de-energising a
                // coil does not define a position, so it must never move the switch.
                _log?.Invoke(
                    $"Switch {decoderAddress} output {outputIndex} was de-energised; no position change is applied.");
                return false;
            }

            var requested = outputIndex == 0 ? SwitchPosition.Straight : SwitchPosition.Diverging;

            return _controller.TryApply(decoderAddress, requested, out _);
        }
    }
}
