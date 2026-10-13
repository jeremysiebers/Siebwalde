using System;
using System.Collections.Generic;
using System.Linq;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;

namespace SiebwaldeApp.Integration
{
    /// <summary>
    /// The stateful reconciler + thin writer for the ownership-aware locomotive carrier (Phase 2).
    /// It holds the retained per-DCC speed/direction states, the identity map, the topology and the
    /// physical mapping, and funnels every speed / shadow / occupancy change into the pure
    /// <see cref="LocomotiveCarrierReconciliation.Reconcile"/>, then applies the resulting
    /// <see cref="CarrierDecision"/> through the existing command-acceptance boundary
    /// (<see cref="TrackApplicationVariables.SetDesiredAmplifierControl"/>), the
    /// <see cref="AmplifierCommandTracker"/> (reachability bookkeeping only) and the
    /// <see cref="IControlTrace"/>. Authority comes only from the KIS-07 shadow via the projector,
    /// never from topology alone.
    /// </summary>
    public sealed class LocomotiveCarrierController
    {
        private static readonly TimeSpan DefaultConfirmationWindow = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Provisional post-clear release tail hold: after a departed source becomes filtered Clear,
        /// it is held driven for this long before it is released (HR0=399). Stage 2 of the
        /// two-stage physical-source release. Later user-configurable.
        /// </summary>
        public static readonly TimeSpan DefaultSourceReleaseTailHold = TimeSpan.FromMilliseconds(250);

        private readonly TrackApplicationVariables _variables;
        private readonly AmplifierCommandTracker _commandTracker;
        private readonly IControlTrace? _trace;
        private readonly IReadOnlyDictionary<int, int> _identity;
        private readonly BlockTopology _topology;
        private readonly IReadOnlyList<LayoutPhysicalAmplifierBinding> _mapping;
        private readonly TimeSpan _confirmationWindow;
        private readonly TimeSpan _tailHoldWindow;
        private readonly Func<DateTimeOffset> _clock;
        private readonly int _consecutiveInvalidRequired;
        private readonly TimeSpan _authorityInvalidMinimumDuration;

        private readonly Dictionary<int, (int Speed, int Direction)> _desired = new();
        private readonly Dictionary<int, DateTimeOffset> _tailHoldSince = new();
        private KoploperLogicalSectionShadowObservation? _shadow;
        private PhysicalSectionOccupancyObservation? _physical;
        private Dictionary<int, (int OwnerDcc, int Hr0)> _previousTargets = new();
        private AuthorityLifecycleState _authorityLifecycle = AuthorityLifecycleState.AuthorityNotYetObserved;
        private bool _tracedNotYetObserved;
        private int _consecutiveInvalidCount;
        private DateTimeOffset? _firstInvalidAtUtc;
        private KoploperLogicalSectionShadowObservation? _lastValidAuthoritySnapshot;

        /// <summary>
        /// The stateful physical footprint: logical section -&gt; attributed locomotive DCC address.
        /// This is SEPARATE from the drive-history <see cref="_previousTargets"/>: it remembers which
        /// sections are still plausibly occupied by which locomotive during a transition or recovery,
        /// so a stopped spanning locomotive is not reclassified as an ownerless intrusion. It never
        /// carries drive authority by itself.
        /// </summary>
        private Dictionary<int, int> _footprint = new();

        public LocomotiveCarrierController(
            TrackApplicationVariables variables,
            AmplifierCommandTracker commandTracker,
            IControlTrace? trace,
            IReadOnlyDictionary<int, int> identity,
            BlockTopology topology,
            IReadOnlyList<LayoutPhysicalAmplifierBinding> mapping,
            TimeSpan? confirmationWindow = null,
            TimeSpan? tailHoldWindow = null,
            Func<DateTimeOffset>? clock = null,
            int consecutiveInvalidRequired = 3,
            TimeSpan? authorityInvalidMinimumDuration = null)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _commandTracker = commandTracker ?? throw new ArgumentNullException(nameof(commandTracker));
            _trace = trace;
            _identity = identity ?? throw new ArgumentNullException(nameof(identity));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _mapping = mapping ?? throw new ArgumentNullException(nameof(mapping));
            _confirmationWindow = confirmationWindow ?? DefaultConfirmationWindow;
            _tailHoldWindow = tailHoldWindow ?? DefaultSourceReleaseTailHold;
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
            _consecutiveInvalidRequired = consecutiveInvalidRequired >= 1
                ? consecutiveInvalidRequired
                : throw new ArgumentOutOfRangeException(nameof(consecutiveInvalidRequired));
            _authorityInvalidMinimumDuration = authorityInvalidMinimumDuration ?? TimeSpan.FromMilliseconds(1000);
        }

        /// <summary>The retained speed/direction per DCC address (single source of truth).</summary>
        public IReadOnlyDictionary<int, (int Speed, int Direction)> DesiredLocoStates => _desired;

        /// <summary>The most recent KIS-07 shadow, or null when not yet observed.</summary>
        public KoploperLogicalSectionShadowObservation? Shadow => _shadow;

        /// <summary>The most recent physical section occupancy observation, or null.</summary>
        public PhysicalSectionOccupancyObservation? Physical => _physical;

        /// <summary>The most recent carrier decision produced by <see cref="Reconcile"/>.</summary>
        public CarrierDecision? LastDecision { get; private set; }

        /// <summary>
        /// The authority-lifecycle state. It is <see cref="AuthorityLifecycleState.AuthorityNotYetObserved"/>
        /// until the first valid KIS shadow, <see cref="AuthorityLifecycleState.AuthorityValid"/> while a
        /// valid shadow is present, <see cref="AuthorityLifecycleState.AuthoritySuspect"/> while a
        /// previously-valid shadow is transiently invalid (before N+X qualification), and
        /// <see cref="AuthorityLifecycleState.AuthorityLostAfterValid"/> once the loss has qualified
        /// (consecutive-invalid count AND minimum duration both met). A recovered-valid shadow returns to
        /// <see cref="AuthorityLifecycleState.AuthorityValid"/>; the layout safety latch (owned by the guard,
        /// not the carrier) is what preserves no-auto-resume after a qualified loss, and it is only cleared
        /// by controlled operator recovery.
        /// </summary>
        public AuthorityLifecycleState AuthorityLifecycle => _authorityLifecycle;

        /// <summary>True during startup before the first valid authoritative KIS shadow.</summary>
        public bool IsAuthorityNotReady => _authorityLifecycle == AuthorityLifecycleState.AuthorityNotYetObserved;

        /// <summary>True while a previously-valid shadow is transiently invalid (before N+X qualification).</summary>
        public bool IsAuthoritySuspect => _authorityLifecycle == AuthorityLifecycleState.AuthoritySuspect;

        /// <summary>
        /// The current consecutive-invalid shadow count used by the N+X authority-loss qualification.
        /// Read-only probe for test observability; it is 0 whenever authority is not in a suspect
        /// state (a qualified loss resets it before the transition to
        /// <see cref="AuthorityLifecycleState.AuthorityLostAfterValid"/>).
        /// </summary>
        public int ConsecutiveInvalidCount => _consecutiveInvalidCount;

        /// <summary>
        /// Retains a logical speed/direction for a DCC address and reconciles. The carrier is the
        /// drive-authority path: the physical targets come only from the projector (shadow), not
        /// from the topology/look-ahead. Returns true (the logical speed is retained); the movement
        /// gate is the backstop inside <see cref="TrackApplicationVariables.SetDesiredAmplifierControl"/>.
        /// </summary>
        public bool SetLocoSpeed(int address, int ecosSpeed, int direction)
        {
            // Startup not-ready (no valid authoritative KIS shadow yet): deny non-zero movement
            // WITHOUT retaining it as a deferred command. A rejected startup speed must never
            // auto-replay when authority later becomes valid; a fresh valid ECoS command is required.
            if (ecosSpeed != 0 && IsAuthorityNotReady)
            {
                _trace?.Abnormal(
                    "KIS_AUTHORITY_NOT_READY",
                    null,
                    $"loco {address} non-zero speed refused: no valid authoritative KIS shadow established yet.");
                return false;
            }

            // Authority Suspect continuity (N+X): do NOT increase authority exposure. A newly
            // requested higher speed, a direction change, or NEW movement authority for a loco with
            // no retained drive state is frozen. A speed decrease (including 0 = stop) is always
            // allowed and remains immediate, as do the safety commands handled outside this method.
            if (ecosSpeed > 0 && IsAuthoritySuspect)
            {
                if (!_desired.TryGetValue(address, out var current) ||
                    ecosSpeed > current.Speed ||
                    direction != current.Direction)
                {
                    _trace?.Abnormal(
                        "KIS_AUTHORITY_SUSPECT_FROZEN",
                        null,
                        $"loco {address} speed {ecosSpeed} dir {direction} refused: authority suspect, " +
                        "speed increase / direction change / new movement is frozen.");
                    return false;
                }
            }

            _desired[address] = (ecosSpeed, direction);
            Reconcile();
            return true;
        }

        /// <summary>Feeds a new KIS-07 shadow, advances the authority lifecycle, then reconciles.</summary>
        public void OnShadowChanged(KoploperLogicalSectionShadowObservation? shadow)
        {
            _shadow = shadow;
            AdvanceAuthorityLifecycle(_shadow);
            Reconcile();
        }

        /// <summary>Feeds a new physical section occupancy observation and reconciles.</summary>
        public void OnOccupancyChanged(PhysicalSectionOccupancyObservation? physical)
        {
            _physical = physical;
            Reconcile();
        }

        /// <summary>Runs the pure projector with the current state, applies Stage 2 (tail hold), then writes.</summary>
        public CarrierDecision Reconcile()
        {
            ReconstructFootprintIfEmpty();

            // Continuity during AuthoritySuspect: drive the established footprint from the
            // last-known-good snapshot rather than the invalid shadow, so a single bad KIS read does
            // not collapse the footprint to neutral / a layout stop. The current (invalid) shadow is
            // still used for the qualified-loss path (AuthorityLostAfterValid) which must fail closed.
            KoploperLogicalSectionShadowObservation? driveShadow =
                _authorityLifecycle == AuthorityLifecycleState.AuthoritySuspect
                    ? _lastValidAuthoritySnapshot
                    : _shadow;

            CarrierDecision decision = LocomotiveCarrierReconciliation.Reconcile(
                _desired,
                _identity,
                driveShadow,
                _physical,
                _topology,
                _mapping,
                _previousTargets,
                _footprint,
                _confirmationWindow);

            decision = ApplyTailHold(decision);

            _footprint = new Dictionary<int, int>(decision.PhysicalFootprint ?? new Dictionary<int, int>());

            LastDecision = decision;
            Apply(decision);
            return decision;
        }

        /// <summary>
        /// Advances the authority lifecycle from the current shadow validity and traces TRANSITIONS
        /// (never every cycle). The N+X qualification model means a single bad KIS read does NOT
        /// latch: <see cref="AuthorityLifecycleState.AuthorityValid"/> plus a first invalid
        /// observation enters <see cref="AuthorityLifecycleState.AuthoritySuspect"/> (recording the
        /// first-invalid timestamp and a consecutive-invalid count of 1 while keeping the
        /// last-known-good snapshot). The transition into
        /// <see cref="AuthorityLifecycleState.AuthorityLostAfterValid"/> happens ONLY when the
        /// consecutive-invalid count reaches the configured N AND the invalid duration reaches the
        /// configured X; that transition also revokes the retained drive state (<see cref="_desired"/>)
        /// so a recovered shadow never auto-restarts. Startup
        /// (<see cref="AuthorityLifecycleState.AuthorityNotYetObserved"/>) never routes through
        /// Suspect and never latches.
        /// </summary>
        private void AdvanceAuthorityLifecycle(KoploperLogicalSectionShadowObservation? shadow)
        {
            bool valid = shadow is { ShadowValid: true };
            DateTimeOffset now = _clock();

            switch (_authorityLifecycle)
            {
                case AuthorityLifecycleState.AuthorityNotYetObserved:
                    if (valid)
                    {
                        _authorityLifecycle = AuthorityLifecycleState.AuthorityValid;
                        _lastValidAuthoritySnapshot = shadow;
                        _consecutiveInvalidCount = 0;
                        _firstInvalidAtUtc = null;
                        _trace?.CarrierAuthorityLifecycle("AUTHORITY_VALID");
                    }
                    else if (!_tracedNotYetObserved)
                    {
                        _tracedNotYetObserved = true;
                        _trace?.CarrierAuthorityLifecycle("AUTHORITY_NOT_YET_OBSERVED");
                    }

                    break;

                case AuthorityLifecycleState.AuthorityValid:
                    if (valid)
                    {
                        _lastValidAuthoritySnapshot = shadow;
                    }
                    else
                    {
                        _authorityLifecycle = AuthorityLifecycleState.AuthoritySuspect;
                        _firstInvalidAtUtc = now;
                        _consecutiveInvalidCount = 1;
                        _trace?.CarrierAuthorityLifecycle(
                            "AUTHORITY_SUSPECT_ENTER",
                            invalidCount: 1,
                            firstInvalidAtUtc: _firstInvalidAtUtc);
                    }

                    break;

                case AuthorityLifecycleState.AuthoritySuspect:
                    if (valid)
                    {
                        long recoveredDurationMs = ElapsedInvalidMs(now);
                        int recoveredMaxCount = _consecutiveInvalidCount;
                        _authorityLifecycle = AuthorityLifecycleState.AuthorityValid;
                        _lastValidAuthoritySnapshot = shadow;
                        _consecutiveInvalidCount = 0;
                        _firstInvalidAtUtc = null;
                        _trace?.CarrierAuthorityLifecycle(
                            "AUTHORITY_TRANSIENT_INVALID_RECOVERED",
                            invalidCount: recoveredMaxCount,
                            invalidDurationMs: recoveredDurationMs);
                    }
                    else
                    {
                        _consecutiveInvalidCount++;
                        long elapsedMs = ElapsedInvalidMs(now);

                        if (_consecutiveInvalidCount >= _consecutiveInvalidRequired &&
                            now - (_firstInvalidAtUtc ?? now) >= _authorityInvalidMinimumDuration)
                        {
                            _authorityLifecycle = AuthorityLifecycleState.AuthorityLostAfterValid;

                            // Operational authority loss revokes the retained drive state, so a later
                            // recovered shadow never auto-restarts from a stale speed (no auto-resume).
                            _desired.Clear();

                            _trace?.CarrierAuthorityLifecycle(
                                "AUTHORITY_LOST_AFTER_VALID",
                                invalidCount: _consecutiveInvalidCount,
                                invalidDurationMs: elapsedMs,
                                reason: "N+X qualified (count and duration thresholds met)");
                        }
                        else
                        {
                            _trace?.CarrierAuthorityLifecycle(
                                "AUTHORITY_SUSPECT_CONTINUE",
                                invalidCount: _consecutiveInvalidCount,
                                invalidDurationMs: elapsedMs);
                        }
                    }

                    break;

                case AuthorityLifecycleState.AuthorityLostAfterValid:
                    if (valid)
                    {
                        // Authority is valid again. The retained drive state was already cleared on
                        // loss, so this never auto-restarts movement; the safety latch (owned by the
                        // guard, not the carrier) remains until controlled operator recovery.
                        _authorityLifecycle = AuthorityLifecycleState.AuthorityValid;
                        _lastValidAuthoritySnapshot = shadow;
                        _consecutiveInvalidCount = 0;
                        _firstInvalidAtUtc = null;
                        _trace?.CarrierAuthorityLifecycle("AUTHORITY_RECOVERED_BUT_LATCH_REMAINS");
                    }

                    break;
            }
        }

        private long ElapsedInvalidMs(DateTimeOffset now)
            => (long)Math.Max(0, (now - (_firstInvalidAtUtc ?? now)).TotalMilliseconds);

        /// <summary>
        /// At startup/recovery (no remembered footprint) reconstructs a plausible physical footprint
        /// from current authoritative evidence. Reconstruction only answers "which sections contain
        /// which locomotive"; it never produces drive authority or a PWM target, and it fails closed
        /// (empty) when the evidence is inconsistent.
        /// </summary>
        private void ReconstructFootprintIfEmpty()
        {
            if (_footprint.Count > 0)
            {
                return;
            }

            IReadOnlyDictionary<int, int> reconstructed = PhysicalFootprintReconstructor.Reconstruct(
                _shadow, _physical, _topology, _mapping, _identity);

            foreach (var (section, owner) in reconstructed)
            {
                _footprint[section] = owner;
            }
        }

        /// <summary>
        /// Stage 2 — post-clear release tail hold. A departed source that is filtered Clear
        /// (<see cref="CarrierReason.DepartedTailHold"/>) is held driven for
        /// <see cref="_tailHoldWindow"/>, then released (HR0=399). A departed source still physically
        /// occupied, or stale, is held with no tail timer. Any other verdict clears the tail timer so
        /// a new owner's drive/neutral takes over (explicit safe handover, never overwritten by a
        /// latest-writer race).
        /// </summary>
        private CarrierDecision ApplyTailHold(CarrierDecision decision)
        {
            DateTimeOffset now = _clock();
            var seen = new HashSet<int>();
            var adjusted = new List<CarrierAmplifierDecision>(decision.Amplifiers.Count);
            var footprint = new Dictionary<int, int>(decision.PhysicalFootprint ?? new Dictionary<int, int>());

            foreach (CarrierAmplifierDecision d in decision.Amplifiers)
            {
                seen.Add(d.Amplifier);

                // A departed/reconstructed source that is filtered Clear (whether driven at
                // DepartingPhysicalHold or stopped at DepartingPhysicalStop) is held for the tail
                // window, then released: neutral AND its physical attribution is removed.
                if (d.Reason == CarrierReason.DepartedTailHold)
                {
                    if (!_tailHoldSince.TryGetValue(d.Amplifier, out DateTimeOffset since))
                    {
                        _tailHoldSince[d.Amplifier] = now;
                        adjusted.Add(d);
                    }
                    else if (now - since >= _tailHoldWindow)
                    {
                        _tailHoldSince.Remove(d.Amplifier);
                        RemoveFromFootprint(footprint, d.Amplifier);
                        adjusted.Add(new CarrierAmplifierDecision(
                            d.Amplifier, d.OwnerDcc, AmplifierSpeedMapper.NeutralPwm,
                            CarrierVerdict.Release, CarrierReason.PreviousTargetDeparted,
                            "departed-tail-released"));
                    }
                    else
                    {
                        adjusted.Add(d);
                    }

                    continue;
                }

                if (d.Verdict is CarrierVerdict.DepartingPhysicalHold or CarrierVerdict.DepartingPhysicalStop)
                {
                    // Still physically occupied (driven or stopped), or stale: keep the attribution;
                    // reset any tail timer.
                    _tailHoldSince.Remove(d.Amplifier);
                    adjusted.Add(d);
                    continue;
                }

                // Any other verdict (Drive/Release/Revoke/Neutral/...): clear the tail timer.
                _tailHoldSince.Remove(d.Amplifier);
                adjusted.Add(d);
            }

            // Drop tail timers for amplifiers no longer present in the decision.
            foreach (int amp in _tailHoldSince.Keys.Where(k => !seen.Contains(k)).ToList())
            {
                _tailHoldSince.Remove(amp);
            }

            return decision with { Amplifiers = adjusted, PhysicalFootprint = footprint };
        }

        private void RemoveFromFootprint(Dictionary<int, int> footprint, int amp)
        {
            foreach (LayoutPhysicalAmplifierBinding binding in _mapping)
            {
                if (binding.PhysicalAmplifier == amp)
                {
                    footprint.Remove(binding.SectionId);
                }
            }
        }

        /// <summary>
        /// A read-only capture of the fresh protocol-observed HoldingReg0 per physical track
        /// amplifier. This is protocol-observed state (SLAVEINFO readback), NOT mechanical proof of
        /// the physical output.
        /// </summary>
        public IReadOnlyList<(ushort Amplifier, int ObservedHr0, bool Fresh)> GetObservedHr0()
            => _variables.trackAmpItems
                .Where(item => TrackAmplifierAddress.IsTrackAmplifierAddress(item.SlaveNumber))
                .Select(item => (
                    item.SlaveNumber,
                    item.HoldingReg.Length > TrackAmplifierRegisters.PwmCommand
                        ? (int)item.HoldingReg[TrackAmplifierRegisters.PwmCommand]
                        : 0,
                    TrackAmplifierDataFreshness.HasFreshProtocolReadback(item)))
                .OrderBy(t => t.Item1)
                .ToList();

        /// <summary>Emits the observed HR0 readback to the trace, clearly labelled protocol-observed.</summary>
        public void EmitObservedHr0()
        {
            if (_trace is null)
            {
                return;
            }

            foreach (var (amplifier, hr0, fresh) in GetObservedHr0())
            {
                _trace.AmplifierObserved(amplifier, hr0, fresh);
            }
        }

        private void Apply(CarrierDecision decision)
        {
            var newTargets = new Dictionary<int, (int OwnerDcc, int Hr0)>();

            foreach (CarrierAmplifierDecision d in decision.Amplifiers)
            {
                // Only a legitimate physical track amplifier may receive a write (51..55 never written).
                if (!TrackAmplifierAddress.IsTrackAmplifierAddress(d.Amplifier))
                {
                    continue;
                }

                bool accepted = _variables.SetDesiredAmplifierControl((ushort)d.Amplifier, d.Hr0, emoStop: false);

                bool isDriving = d.Verdict is CarrierVerdict.Drive or CarrierVerdict.DepartingPhysicalHold;

                if (isDriving)
                {
                    if (accepted && d.OwnerDcc is int driveOwner)
                    {
                        _commandTracker.RecordNonNeutral(driveOwner, (ushort)d.Amplifier);
                        newTargets[d.Amplifier] = (driveOwner, d.Hr0);
                    }
                }
                else if (accepted && d.OwnerDcc is int neutralOwner)
                {
                    _commandTracker.RecordNeutral(neutralOwner, (ushort)d.Amplifier);
                }

                _trace?.AmplifierCommand(
                    "Carrier",
                    d.OwnerDcc,
                    (ushort)d.Amplifier,
                    block: null,
                    d.Verdict.ToString(),
                    d.Hr0,
                    TrackApplicationVariables.BuildHr0Value(d.Hr0, emoStop: false),
                    TrackAmplifierOperationalGroup.Unassigned,
                    normalizedSpeed: null,
                    direction: null);
            }

            _previousTargets = newTargets;
        }
    }
}
