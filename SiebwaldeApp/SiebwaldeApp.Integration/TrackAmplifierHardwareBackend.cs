using System;
using System.Collections.Generic;
using System.Linq;
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
    ///
    /// Every concrete non-neutral command is recorded in the optional
    /// <see cref="AmplifierCommandTracker"/>, so a later safety stop can still reach a physical
    /// amplifier whose block mapping has since changed or disappeared.
    /// </summary>
    public sealed class TrackAmplifierHardwareBackend : IHardwareBackend, IAmplifierNeutralizer
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
        private readonly AmplifierCommandTracker? _commandTracker;

        public TrackAmplifierHardwareBackend(
            IBlockPositionProvider blockPositionProvider,
            BlockTopology topology,
            TrackApplicationVariables variables,
            Action<string>? log = null,
            LookAheadPlanner? lookAheadPlanner = null,
            IOccupancyProvider? occupancyProvider = null,
            Func<IReadOnlyDictionary<int, SwitchPosition>>? switchPositionProvider = null,
            AmplifierCommandTracker? commandTracker = null)
        {
            _blockPositionProvider = blockPositionProvider ?? throw new ArgumentNullException(nameof(blockPositionProvider));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _log = log;
            _lookAheadPlanner = lookAheadPlanner;
            _occupancyProvider = occupancyProvider;
            _switchPositionProvider = switchPositionProvider;
            _commandTracker = commandTracker;
        }

        /// <summary>Power off sets every mapped amplifier to neutral (standstill).</summary>
        public bool SetPower(bool on)
        {
            if (on)
            {
                _log?.Invoke("Power ON");
                return true;
            }

            var neutralized = new List<ushort>();

            foreach (var block in _topology.Blocks)
            {
                if (!_topology.TryGetAmplifiers(block, out var amplifiers))
                {
                    continue;
                }

                foreach (var amplifier in amplifiers)
                {
                    _variables.SetDesiredAmplifierControl(amplifier, AmplifierSpeedMapper.NeutralPwm, false);
                    neutralized.Add(amplifier);
                }
            }

            // A central power-off is a global operation: the mapped amplifiers were commanded
            // neutral, so they are no longer outstanding for any locomotive.
            _commandTracker?.RecordNeutralGlobally(neutralized);

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
                RecordCommand(address, amplifier, pwm);
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
                RecordCommand(locoAddress, amplifier, pwm);
            }

            _log?.Invoke(
                $"Look-ahead: block {currentBlock} -> {nextBlock}, PWM {pwm} on amp(s) {string.Join("+", nextAmplifiers)}");
        }

        /// <summary>
        /// Keeps the safety reachability bookkeeping in step with the concrete physical command
        /// that was just queued. A neutral setpoint removes the target; a non-neutral setpoint
        /// (re)claims it.
        /// </summary>
        private void RecordCommand(int locoAddress, ushort amplifier, int pwm)
        {
            if (_commandTracker is null)
            {
                return;
            }

            if (pwm == AmplifierSpeedMapper.NeutralPwm)
            {
                _commandTracker.RecordNeutral(locoAddress, amplifier);
            }
            else
            {
                _commandTracker.RecordNonNeutral(locoAddress, amplifier);
            }
        }

        /// <summary>
        /// Every physical track amplifier the control path knows about: detected hardware plus
        /// every amplifier named by the configured topology. Detected-but-unmapped amplifiers are
        /// therefore included, because a safety fallback must not leave a communicable output
        /// outside the strongest neutralization operation just because it is absent from the
        /// logical mapping.
        /// </summary>
        public IReadOnlyList<ushort> GetKnownPhysicalAmplifiers()
        {
            var known = new SortedSet<ushort>();

            var items = _variables.trackAmpItems;
            if (items is not null)
            {
                foreach (var amplifier in items)
                {
                    if (amplifier is not null && amplifier.SlaveNumber != 0 && amplifier.SlaveDetected != 0)
                    {
                        known.Add(amplifier.SlaveNumber);
                    }
                }
            }

            foreach (var block in _topology.Blocks)
            {
                if (_topology.TryGetAmplifiers(block, out var amplifiers))
                {
                    foreach (var amplifier in amplifiers)
                    {
                        if (amplifier != 0)
                        {
                            known.Add(amplifier);
                        }
                    }
                }
            }

            return known.ToArray();
        }

        /// <summary>
        /// Commands the given physical amplifiers neutral, independent of the block mapping.
        /// This is the amplifier-centric primitive a safety escalation uses.
        /// </summary>
        public IReadOnlyList<ushort> NeutralizeAmplifiers(IReadOnlyCollection<ushort> amplifiers)
        {
            if (amplifiers is null)
            {
                return Array.Empty<ushort>();
            }

            var failed = new List<ushort>();

            foreach (var amplifier in amplifiers.Distinct())
            {
                if (amplifier == 0)
                {
                    failed.Add(amplifier);
                    continue;
                }

                _variables.SetDesiredAmplifierControl(amplifier, AmplifierSpeedMapper.NeutralPwm, false);
            }

            _log?.Invoke(
                $"Amplifier-centric neutralization queued for amp(s) {string.Join(",", amplifiers.Distinct())}");

            return failed;
        }

        /// <summary>
        /// Divergence checks for the routes this backend is about to command. Bound after
        /// composition, because the checker needs the occupancy provider this backend also uses.
        /// </summary>
        public DivergenceChecker? Divergence { get; set; }
    }
}
