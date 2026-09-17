using System;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Bridges Koploper/ECoS locomotive commands to the track amplifiers.
    ///
    /// Locomotive -> block comes from Koploper (<see cref="IBlockPositionProvider"/>);
    /// block -> amplifier(s) comes from the configured <see cref="BlockTopology"/>;
    /// speed -> PWM comes from <see cref="AmplifierSpeedMapper"/>. The resulting
    /// setpoints are queued through <see cref="TrackApplicationVariables.SetDesiredAmplifierControl"/>
    /// and sent by the existing runtime writer.
    /// </summary>
    public sealed class TrackAmplifierHardwareBackend : IHardwareBackend
    {
        private readonly IBlockPositionProvider _blockPositionProvider;
        private readonly BlockTopology _topology;
        private readonly TrackApplicationVariables _variables;
        private readonly Action<string>? _log;

        public TrackAmplifierHardwareBackend(
            IBlockPositionProvider blockPositionProvider,
            BlockTopology topology,
            TrackApplicationVariables variables,
            Action<string>? log = null)
        {
            _blockPositionProvider = blockPositionProvider ?? throw new ArgumentNullException(nameof(blockPositionProvider));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _log = log;
        }

        /// <summary>Power off sets every mapped amplifier to neutral (standstill).</summary>
        public void SetPower(bool on)
        {
            if (on)
            {
                _log?.Invoke("Power ON");
                return;
            }

            foreach (var block in _topology.Blocks)
            {
                if (!_topology.TryGetAmplifiers(block, out var amplifiers))
                {
                    continue;
                }

                foreach (var amplifier in amplifiers)
                {
                    _variables.SetDesiredAmplifierControl(amplifier, AmplifierSpeedMapper.NeutralPwm, false);
                }
            }

            _log?.Invoke("Power OFF: all mapped amplifiers set to neutral");
        }

        /// <summary>
        /// Translates a locomotive speed command into amplifier setpoints for the
        /// block the locomotive currently occupies.
        /// </summary>
        public void SetLocoSpeed(int address, int ecosSpeed, int direction)
        {
            var block = _blockPositionProvider.TryGetBlockForLoc(address);
            if (block is null)
            {
                _log?.Invoke($"Loco {address}: no known block, command ignored");
                return;
            }

            if (!_topology.TryGetAmplifiers(block.Value, out var amplifiers))
            {
                _log?.Invoke($"Loco {address}: block {block.Value} has no amplifier mapping");
                return;
            }

            var pwm = AmplifierSpeedMapper.ToPwm(ecosSpeed, direction);

            foreach (var amplifier in amplifiers)
            {
                _variables.SetDesiredAmplifierControl(amplifier, pwm, false);
            }

            _log?.Invoke(
                $"Loco {address}: block {block.Value} speed {ecosSpeed} dir {direction} -> PWM {pwm} on amp(s) {string.Join("+", amplifiers)}");
        }

        /// <summary>Switch handling is not part of the track-amplifier translation yet.</summary>
        public void SetSwitch(int decoderAddress, int outputIndex, bool on)
        {
            _log?.Invoke($"Switch addr={decoderAddress} index={outputIndex} state={on} (ignored)");
        }
    }
}
