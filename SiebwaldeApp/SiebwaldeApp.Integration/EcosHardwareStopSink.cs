using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// Safety stop implementation over the existing hardware backend and the amplifier-centric
    /// neutralizer. It deliberately uses only the mechanisms the ECoS path already uses:
    ///
    /// - per locomotive: <see cref="IHardwareBackend.SetLocoSpeed"/> with ECoS speed 0, which the
    ///   backend maps to the neutral amplifier setpoint (standstill), extended with the retained
    ///   outstanding physical targets for that locomotive;
    /// - whole layout: an amplifier-centric neutralization of every physical track amplifier the
    ///   control path knows about, independent of the current locomotive and block mapping.
    ///
    /// A loco-scoped stop must not depend on the current block mapping alone: a normal A -> B
    /// transition never neutralizes the vacated amplifier, and look-ahead can command a second
    /// amplifier, so the loco's outstanding physical targets are retained by
    /// <see cref="AmplifierCommandTracker"/> and neutralized here as well. The result reports
    /// command delivery, never an observed physical confirmation.
    /// </summary>
    public sealed class EcosHardwareStopSink : ISafetyStopSink
    {
        private readonly Action<string>? _log;

        public EcosHardwareStopSink(Action<string>? log = null)
        {
            _log = log;
        }

        /// <summary>The backend the stop is issued through. Set once composition has finished.</summary>
        public IHardwareBackend? Hardware { get; set; }

        /// <summary>
        /// The amplifier-centric neutralizer used to reach retained physical targets and the
        /// conservative layout fallback. When unset, the bound <see cref="Hardware"/> is used if
        /// it implements <see cref="IAmplifierNeutralizer"/>.
        /// </summary>
        public IAmplifierNeutralizer? Neutralizer { get; set; }

        /// <summary>
        /// The retained commanded-actuator ownership for this control path. Without it a stop can
        /// only see the current block mapping, which is exactly the reachability defect.
        /// </summary>
        public AmplifierCommandTracker? CommandTracker { get; set; }

        /// <inheritdoc />
        public SafetyStopResult StopLoco(int address)
        {
            var neutralizer = Neutralizer ?? Hardware as IAmplifierNeutralizer;

            if (Hardware is null && neutralizer is null)
            {
                _log?.Invoke($"Safety stop for loco {address} not delivered: no hardware backend is bound.");
                return SafetyStopResult.Unavailable();
            }

            var commanded = new List<ushort>();
            var failed = new List<ushort>();
            var applied = false;

            // 1. The existing loco-scoped path neutralizes the currently mapped block (and its
            //    look-ahead target) and records those as neutral in the tracker.
            if (Hardware is not null)
            {
                try
                {
                    applied = Hardware.SetLocoSpeed(address, 0, 0);
                }
                catch (Exception ex)
                {
                    _log?.Invoke($"Safety stop for loco {address}: loco command path failed: {ex.Message}");
                }
            }

            // 2. Amplifier-centric coverage of every physical target the locomotive still owns.
            //    This is what reaches an amplifier whose block mapping has since changed, moved to
            //    another block, or disappeared entirely.
            var outstanding = CommandTracker?.GetOutstanding(address) ?? Array.Empty<ushort>();

            if (outstanding.Count > 0)
            {
                if (neutralizer is null)
                {
                    failed.AddRange(outstanding);
                    _log?.Invoke(
                        $"Safety stop for loco {address}: {outstanding.Count} retained amplifier(s) cannot be neutralized (no amplifier neutralizer is bound).");
                }
                else
                {
                    var notCommanded = neutralizer.NeutralizeAmplifiers(outstanding) ?? Array.Empty<ushort>();
                    var failedSet = new HashSet<ushort>(notCommanded);

                    foreach (var amplifier in outstanding)
                    {
                        if (failedSet.Contains(amplifier))
                        {
                            failed.Add(amplifier);
                        }
                        else
                        {
                            commanded.Add(amplifier);
                        }
                    }

                    if (failed.Count == 0)
                    {
                        CommandTracker?.RecordNeutral(address, outstanding);
                        applied = true;
                    }
                    else
                    {
                        if (commanded.Count > 0)
                        {
                            CommandTracker?.RecordNeutral(address, commanded);
                            applied = true;
                        }

                        _log?.Invoke(
                            $"Safety stop for loco {address}: amplifier(s) {string.Join(",", failed)} could not be commanded neutral.");
                    }
                }
            }

            if (failed.Count > 0)
            {
                return SafetyStopResult.Partial(commanded, failed);
            }

            if (!applied)
            {
                _log?.Invoke($"Safety stop for loco {address} not delivered: no resolvable physical target.");
                return SafetyStopResult.NotApplied();
            }

            _log?.Invoke(
                $"Safety stop: loco {address} commanded neutral (amplifier-centric targets: {(commanded.Count == 0 ? "<none>" : string.Join(",", commanded))}).");
            return SafetyStopResult.Commanded(commanded);
        }

        /// <inheritdoc />
        public SafetyStopResult StopLayout()
        {
            var neutralizer = Neutralizer ?? Hardware as IAmplifierNeutralizer;

            var targets = new SortedSet<ushort>();

            if (neutralizer is not null)
            {
                foreach (var amplifier in neutralizer.GetKnownPhysicalAmplifiers() ?? Array.Empty<ushort>())
                {
                    if (amplifier != 0)
                    {
                        targets.Add(amplifier);
                    }
                }
            }

            if (CommandTracker is not null)
            {
                foreach (var amplifier in CommandTracker.GetAllOutstanding())
                {
                    if (amplifier != 0)
                    {
                        targets.Add(amplifier);
                    }
                }
            }

            if (targets.Count == 0)
            {
                // No amplifier inventory was available, so fall back to the existing central
                // power-off path instead of claiming a neutralization that was never issued.
                if (Hardware is null)
                {
                    _log?.Invoke("Safety stop for the layout not delivered: no hardware backend is bound.");
                    return SafetyStopResult.Unavailable();
                }

                try
                {
                    var applied = Hardware.SetPower(false);
                    _log?.Invoke("Safety stop: layout power off (no amplifier inventory was available).");
                    return applied
                        ? SafetyStopResult.Commanded(Array.Empty<ushort>())
                        : SafetyStopResult.NotApplied();
                }
                catch (Exception ex)
                {
                    _log?.Invoke($"Layout safety stop failed: {ex.Message}");
                    return SafetyStopResult.NotApplied();
                }
            }

            if (neutralizer is null)
            {
                _log?.Invoke(
                    $"Safety stop for the layout not delivered: {targets.Count} physical amplifier(s) known but no amplifier neutralizer is bound.");
                return SafetyStopResult.Partial(Array.Empty<ushort>(), targets.ToArray());
            }

            var notCommanded = neutralizer.NeutralizeAmplifiers(targets.ToArray()) ?? Array.Empty<ushort>();
            var failedSet = new HashSet<ushort>(notCommanded);
            var commanded = targets.Where(a => !failedSet.Contains(a)).ToArray();
            var failed = targets.Where(a => failedSet.Contains(a)).ToArray();

            if (failed.Length == 0)
            {
                CommandTracker?.ClearAll();

                // Keep the logical central power state consistent with the safety action. The
                // neutralization above is the authoritative physical operation.
                if (Hardware is not null)
                {
                    try
                    {
                        Hardware.SetPower(false);
                    }
                    catch (Exception ex)
                    {
                        _log?.Invoke($"Layout power-off after neutralization failed: {ex.Message}");
                    }
                }

                _log?.Invoke(
                    $"Safety stop: layout commanded neutral on {commanded.Length} physical amplifier(s), including detected and mapped outputs.");
                return SafetyStopResult.Commanded(commanded);
            }

            if (commanded.Length > 0)
            {
                CommandTracker?.RecordNeutralGlobally(commanded);
            }

            _log?.Invoke(
                $"Safety stop for the layout incomplete: amplifier(s) {string.Join(",", failed)} could not be commanded neutral.");
            return SafetyStopResult.Partial(commanded, failed);
        }
    }
}
