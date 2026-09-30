using System;
using SiebwaldeApp.Core.TrackApplication.Simulator;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// A thin <see cref="IHardwareBackend"/> tee that forwards <see cref="SetLocoSpeed"/> and
    /// <see cref="SetPower"/> to BOTH the real <see cref="TrackAmplifierHardwareBackend"/> (which
    /// keeps producing the PWM writes) and the movement simulator (which keeps the simulated
    /// locomotive state in sync). <see cref="SetSwitch"/> passes through to the real backend only;
    /// there is no simulated switch in the first example.
    /// </summary>
    public sealed class SimulationTeeHardwareBackend : IHardwareBackend
    {
        private readonly IHardwareBackend _real;
        private readonly IMovementSimulation _movementSimulation;

        public SimulationTeeHardwareBackend(IHardwareBackend real, IMovementSimulation movementSimulation)
        {
            _real = real ?? throw new ArgumentNullException(nameof(real));
            _movementSimulation = movementSimulation ?? throw new ArgumentNullException(nameof(movementSimulation));
        }

        /// <inheritdoc />
        public bool SetPower(bool on)
        {
            _movementSimulation.SetPower(on);
            return _real.SetPower(on);
        }

        /// <inheritdoc />
        public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
        {
            // IHardwareBackend and IMovementSimulation share the ECoS direction convention
            // (0 = forward, non-zero = reverse), so the raw direction is forwarded unchanged
            // and the simulator sees the same value as the real backend.
            _movementSimulation.SetLocoSpeed(address, ecosSpeed, direction);
            return _real.SetLocoSpeed(address, ecosSpeed, direction);
        }

        /// <inheritdoc />
        public bool SetSwitch(int decoderAddress, int outputIndex, bool on)
            => _real.SetSwitch(decoderAddress, outputIndex, on);
    }
}
