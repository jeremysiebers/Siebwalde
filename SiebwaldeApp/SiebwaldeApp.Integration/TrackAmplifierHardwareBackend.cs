using System;
using System.Collections.Generic;
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
    ///
    /// When a <see cref="LookAheadPlanner"/> and an <see cref="IOccupancyProvider"/> are
    /// supplied, the same setpoint is also queued for the next block (look-ahead), unless
    /// the transition forbids it (for example a station departure) or the next block is occupied.
    /// </summary>
    public sealed class TrackAmplifierHardwareBackend : IHardwareBackend
    {
        private static readonly IReadOnlyDictionary<int, SwitchPosition> NoSwitches =
            new Dictionary<int, SwitchPosition>();

        private readonly IBlockPositionProvider _blockPositionProvider;
        private readonly BlockTopology _topology;
        private readonly TrackApplicationVariables _variables;
        private readonly Action<string>? _log;
        private readonly LookAheadPlanner? _lookAheadPlanner;
        private readonly IOccupancyProvider? _occupancyProvider;
        private readonly Func<IReadOnlyDictionary<int, SwitchPosition>>? _switchPositionProvider;

        public TrackAmplifierHardwareBackend(
            IBlockPositionProvider blockPositionProvider,
            BlockTopology topology,
            TrackApplicationVariables variables,
            Action<string>? log = null,
            LookAheadPlanner? lookAheadPlanner = null,
            IOccupancyProvider? occupancyProvider = null,
            Func<IReadOnlyDictionary<int, SwitchPosition>>? switchPositionProvider = null)
        {
            _blockPositionProvider = blockPositionProvider ?? throw new ArgumentNullException(nameof(blockPositionProvider));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _log = log;
            _lookAheadPlanner = lookAheadPlanner;
            _occupancyProvider = occupancyProvider;
            _switchPositionProvider = switchPositionProvider;
        }

        /// <summary>Power off sets every mapped amplifier to neutral (standstill).</summary>
        public bool SetPower(bool on)
        {
            if (on)
            {
                _log?.Invoke("Power ON");
                return true;
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
            return true;
        }

        /// <summary>
        /// Translates a locomotive speed command into amplifier setpoints for the
        /// block the locomotive currently occupies, plus the look-ahead block.
        /// </summary>
        /// <returns>
        /// False when nothing could be commanded (the locomotive has no known position or its
        /// block has no amplifier mapping), so the ECoS backend does not acknowledge a movement
        /// command that had no effect.
        /// </returns>
        public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
        {
            var block = _blockPositionProvider.TryGetBlockForLoc(address);
            if (block is null)
            {
                _log?.Invoke($"Loco {address}: no known block, command ignored");
                return false;
            }

            if (!_topology.TryGetAmplifiers(block.Value, out var amplifiers))
            {
                _log?.Invoke($"Loco {address}: block {block.Value} has no amplifier mapping");
                return false;
            }

            var pwm = AmplifierSpeedMapper.ToPwm(ecosSpeed, direction);

            foreach (var amplifier in amplifiers)
            {
                _variables.SetDesiredAmplifierControl(amplifier, pwm, false);
            }

            _log?.Invoke(
                $"Loco {address}: block {block.Value} speed {ecosSpeed} dir {direction} -> PWM {pwm} on amp(s) {string.Join("+", amplifiers)}");

            ApplyLookAhead(address, block.Value, pwm);

            return true;
        }

        /// <summary>
        /// Switch handling is not part of the track-amplifier translation: switches are driven
        /// by accessory decoders, not by the track amplifiers, and that path is not wired to
        /// real hardware yet. Returns false so the ECoS backend does not report a switch state
        /// change that never reached the layout.
        /// </summary>
        public bool SetSwitch(int decoderAddress, int outputIndex, bool on)
        {
            _log?.Invoke($"Switch addr={decoderAddress} index={outputIndex} state={on} (ignored: no real switch output path yet)");
            return false;
        }

        private void ApplyLookAhead(int locoAddress, int currentBlock, int pwm)
        {
            if (_lookAheadPlanner is null || _occupancyProvider is null)
            {
                return;
            }

            var switchPositions = _switchPositionProvider?.Invoke() ?? NoSwitches;

            if (!_lookAheadPlanner.TryPlanNext(currentBlock, switchPositions, _occupancyProvider, out var nextBlock))
            {
                return;
            }

            // A route whose required switch condition contradicts the known logical state must
            // not be treated as safe: report it (which may trigger a safety stop) and do not
            // pre-command the next block.
            var divergence = Divergence?.CheckTransition(locoAddress, currentBlock, nextBlock);
            if (divergence is not null)
            {
                _log?.Invoke(
                    $"Look-ahead blocked: block {currentBlock} -> {nextBlock} ({divergence.Code}).");
                return;
            }

            if (!_topology.TryGetAmplifiers(nextBlock, out var nextAmplifiers))
            {
                return;
            }

            foreach (var amplifier in nextAmplifiers)
            {
                _variables.SetDesiredAmplifierControl(amplifier, pwm, false);
            }

            _log?.Invoke(
                $"Look-ahead: block {currentBlock} -> {nextBlock}, PWM {pwm} on amp(s) {string.Join("+", nextAmplifiers)}");
        }

        /// <summary>
        /// Divergence checks for the routes this backend is about to command. Bound after
        /// composition, because the checker needs the occupancy provider this backend also uses.
        /// </summary>
        public DivergenceChecker? Divergence { get; set; }
    }
}
