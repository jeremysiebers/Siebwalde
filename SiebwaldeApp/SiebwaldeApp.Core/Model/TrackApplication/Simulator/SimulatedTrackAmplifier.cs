using System;

namespace SiebwaldeApp.Core.TrackApplication.Simulator
{
    /// <summary>
    /// Deterministic, software-only model of one PIC18 track amplifier per the frozen Phase-1
    /// wire contract (docs/amplifier-system-architecture.md §R8-4). It owns the 23 holding
    /// registers plus the dynamic control/protection state the contract requires:
    /// identity, two-phase config + APPLY, per-register write validation + write-result,
    /// <c>INIT_DONE</c>/<c>CONFIG_OK</c> gating, the applied-PWM ramp (distinct from the command
    /// echo), and the movement-permission lease (expiry latches <c>INHIBITED</c> +
    /// <c>LEASE_EXPIRED</c>).
    ///
    /// This is a protocol/behaviour model only. It never claims physical PWM, timing or hardware
    /// behaviour. <see cref="Step"/> advances exactly one scheduler-tick-equivalent for the ramp
    /// and the lease; the transport calls it on each fresh FC03 read (modelling the master's poll)
    /// and tests may call it directly for deterministic ramp/lease evidence.
    /// </summary>
    public sealed class SimulatedTrackAmplifier
    {
        /// <summary>HR7 identity for a compatible Phase-1 amplifier: MAP_VERSION 0x01, DEVICE_CLASS 1,
        /// all four capability bits set (applied-readback, ramp, comm-watchdog, two-phase-config).</summary>
        public const ushort Phase1Identity = 0xF101;

        /// <summary>Lease timeout in scheduler ticks (MOTION_LEASE_TIMEOUT_MS 500 / 1.704 ms ≈ 294).</summary>
        public const int LeaseTimeoutTicks = 294;

        private const int Neutral = AmplifierSpeedMapper.NeutralPwm; // 399

        private readonly ushort[] _registers = new ushort[TrackAmplifierRegisters.Count];

        // Internal duty values (raw PWM, no direction/brake bits) kept separate from the encoded
        // HR_APPLIED_PWM register so ramp/lease arithmetic never sees the direction bit.
        private int _requestedDuty = Neutral;
        private int _targetDuty = Neutral;
        private int _appliedDuty = Neutral;

        // Active (applied) config, seeded from defaults; only copied from HR3-6 on a valid APPLY.
        private int _activeAccelStep = 2;
        private int _activeDecelStep = 2;
        private int _activeFastStopStep = 8;

        private ushort _prevControlWord;
        private bool _enableActive;
        private bool _inhibited;
        private bool _stoppingActive;
        private bool _fastStopping;
        private int _state;                 // HR_STATE enum 0..7
        private bool _configApplied;

        private int _leaseTicks;
        private bool _leaseArmed;           // false until the first MOTION_PERMISSION renewal; the lease starts EXPIRED

        public byte SlaveNumber { get; }

        public SimulatedTrackAmplifier(byte slaveNumber, ushort identity = Phase1Identity,
            ushort firmwareChecksum = TrackSimulatorConfig.DefaultFirmwareChecksum)
        {
            SlaveNumber = slaveNumber;
            _registers[TrackAmplifierRegisters.Identity] = identity;
            _registers[TrackAmplifierRegisters.SwChecksum] = firmwareChecksum;
            // Firmware semantic version (mirrors PIC18 AmpAdapter_Init): 1.0.0, protocol 0x01.
            _registers[TrackAmplifierRegisters.FwVersionMajorMinor] = 0x0100;
            _registers[TrackAmplifierRegisters.FwVersionPatchProtocol] = 0x0001;
            _state = 0; // NOT_INITIALIZED
            RefreshState(); // seeds HR8 (ID_SET) + HR12/13/14 (neutral)
        }

        /// <summary>Defensive copy of the current holding registers.</summary>
        public ushort[] Snapshot()
        {
            var copy = new ushort[_registers.Length];
            Array.Copy(_registers, copy, _registers.Length);
            return copy;
        }

        /// <summary>The raw HR_VALIDITY (HR10) register, including the occupancy-live bit.</summary>
        public ushort Status => _registers[TrackAmplifierRegisters.Status];

        /// <summary>Overwrites the HR_VALIDITY (HR10) register (test/injection surface).</summary>
        public void SetStatus(ushort status)
        {
            _registers[TrackAmplifierRegisters.Status] = status;
        }

        /// <summary>Applies a FC06/FC16 write of <paramref name="values"/> starting at
        /// <paramref name="startRegister"/>, updating registers, model state and HR_WRITE_RESULT.</summary>
        public void ApplyWrite(byte startRegister, ushort[] values)
        {
            if (values == null || values.Length == 0)
            {
                return;
            }

            for (int i = 0; i < values.Length; i++)
            {
                int address = startRegister + i;
                if (address >= TrackAmplifierRegisters.Count)
                {
                    SetWriteResult((byte)(address & 0xFF), 2, retained: true);
                    continue;
                }

                switch (address)
                {
                    case TrackAmplifierRegisters.PwmCommand: // HR0
                        WriteCommand(values[i]);
                        break;
                    case TrackAmplifierRegisters.Control:   // HR1
                        WriteControl(values[i]);
                        break;
                    case TrackAmplifierRegisters.Address:   // HR2
                        _registers[address] = values[i];
                        SetWriteResult((byte)address, 0, retained: false);
                        RefreshState();
                        break;
                    case TrackAmplifierRegisters.ConfigRamp:      // HR3
                    case TrackAmplifierRegisters.ConfigFastStop:  // HR4
                    case TrackAmplifierRegisters.ConfigWatchdog:  // HR5
                    case TrackAmplifierRegisters.ConfigTag:       // HR6
                        _registers[address] = values[i]; // pending until APPLY
                        SetWriteResult((byte)address, 0, retained: false);
                        break;
                    default: // HR7..HR20 read-only
                        SetWriteResult((byte)address, 2, retained: true);
                        break;
                }
            }
        }

        /// <summary>One scheduler-tick-equivalent: advances the applied-PWM ramp and the lease.</summary>
        public void Step()
        {
            StepRamp();
            StepLease();
            RefreshState();
        }

        /// <summary>Writes the current model state back into the HR_STATE / HR_REQUESTED/TARGET/APPLIED registers.</summary>
        public void RefreshState()
        {
            ushort state = (ushort)((_state & 0xF)
                | TrackAmplifierRegisters.IdSetBit
                | (_enableActive ? TrackAmplifierRegisters.EnableActiveBit : 0)
                | (_configApplied ? (ushort)(TrackAmplifierRegisters.InitDoneBit | TrackAmplifierRegisters.ConfigOkBit) : (ushort)0)
                | (_stoppingActive ? TrackAmplifierRegisters.StoppingActiveBit : 0)
                | (_inhibited ? TrackAmplifierRegisters.InhibitedBit : 0));
            _registers[TrackAmplifierRegisters.State] = state;
            _registers[TrackAmplifierRegisters.RequestedPwm] = (ushort)_requestedDuty;
            _registers[TrackAmplifierRegisters.TargetPwm] = (ushort)_targetDuty;
            _registers[TrackAmplifierRegisters.AppliedPwm] = EncodeApplied();
        }

        // --------------------------------------------------------------------
        // Write handlers
        // --------------------------------------------------------------------

        private void WriteCommand(ushort value)
        {
            int pwm = value & TrackAmplifierRegisters.PwmMask;
            if (pwm > AmplifierSpeedMapper.MaxPwm) // 800..1023 rejected, never clamped
            {
                SetWriteResult(TrackAmplifierRegisters.PwmCommand, 1, retained: true);
                return;
            }

            _registers[TrackAmplifierRegisters.PwmCommand] = value; // command echo (never applied evidence)
            _requestedDuty = pwm;

            bool stopRequest = (value & TrackAmplifierRegisters.StopRequestBit) != 0;
            bool emo = (value & TrackAmplifierRegisters.EmergencyStopBit) != 0;

            if (emo)
            {
                _stoppingActive = true;
                _fastStopping = true;
                _inhibited = true;
                _state = 5; // ESTOP
                _targetDuty = Neutral;
            }
            else if (stopRequest)
            {
                _stoppingActive = true;
                _fastStopping = true;
                _inhibited = true;
                _state = 3; // STOPPING
                _targetDuty = Neutral;
            }
            else
            {
                _targetDuty = pwm;
                if (pwm != Neutral)
                {
                    if (_enableActive && _configApplied && LeaseFresh())
                    {
                        // A fresh valid non-neutral command is the only exit from INHIBITED (no
                        // auto-restore of old PWM): the prior PWM is never resumed.
                        _inhibited = false;
                        _state = 2; // RUN
                    }
                }
                else
                {
                    // Normal zero: normal decel to neutral, no INHIBITED, no latch.
                    _stoppingActive = _appliedDuty != Neutral;
                    _fastStopping = false;
                    _state = _stoppingActive ? 3 : (_configApplied ? 1 : 0);
                }
            }

            SetWriteResult(TrackAmplifierRegisters.PwmCommand, 0, retained: false);
            RefreshState();
        }

        private void WriteControl(ushort value)
        {
            // R8-1 / R7-1: reserved bits 9..0 must be zero.
            if ((value & 0x03FF) != 0)
            {
                SetWriteResult(TrackAmplifierRegisters.Control, 1, retained: true);
                return;
            }

            // ENABLE + DISABLE together rejected.
            if ((value & TrackAmplifierRegisters.EnableBit) != 0 && (value & TrackAmplifierRegisters.DisableBit) != 0)
            {
                SetWriteResult(TrackAmplifierRegisters.Control, 3, retained: true);
                return;
            }

            // Set-only level bits.
            if ((value & TrackAmplifierRegisters.EnableBit) != 0)
            {
                _enableActive = true;
            }
            if ((value & TrackAmplifierRegisters.DisableBit) != 0)
            {
                _enableActive = false;
                _stoppingActive = true;
                _fastStopping = true;
                _inhibited = true;
                _state = 3; // STOPPING (disable ramps to neutral)
                _targetDuty = Neutral;
            }
            if ((value & TrackAmplifierRegisters.MotionPermissionBit) != 0)
            {
                // Lease renewal (WRITE only): resets lease ticks; never clears INHIBITED, never
                // re-enables, never supplies a command.
                _leaseArmed = true;
                _leaseTicks = 0;
            }

            // Edge bits fire on a 0->1 transition of the wire bit.
            bool fireApply = Edge(value, _prevControlWord, TrackAmplifierRegisters.ApplyConfigBit);
            bool fireClearFault = Edge(value, _prevControlWord, TrackAmplifierRegisters.ClearFaultBit);
            bool fireResetCounters = Edge(value, _prevControlWord, TrackAmplifierRegisters.ResetCountersBit);

            bool applyRejected = false;
            if (fireApply)
            {
                applyRejected = !ApplyConfig();
            }
            if (fireClearFault && _configApplied)
            {
                _registers[TrackAmplifierRegisters.Fault] = 0;
            }
            if (fireResetCounters)
            {
                _registers[TrackAmplifierRegisters.MessagesReceived] = 0;
                _registers[TrackAmplifierRegisters.MessagesSent] = 0;
            }

            _prevControlWord = value;

            if (applyRejected)
            {
                // Rejected APPLY: write-result reports relationship + retained (set by ApplyConfig).
            }
            else
            {
                SetWriteResult(TrackAmplifierRegisters.Control, 0, retained: false);
            }
            RefreshState();
        }

        private static bool Edge(ushort value, ushort prev, ushort bit)
            => (value & bit) != 0 && (prev & bit) == 0;

        private bool ApplyConfig()
        {
            int accel = _registers[TrackAmplifierRegisters.ConfigRamp] & 0xFF;
            int decel = (_registers[TrackAmplifierRegisters.ConfigRamp] >> 8) & 0xFF;
            int fastStop = _registers[TrackAmplifierRegisters.ConfigFastStop] & 0xFF;
            int watchdog = _registers[TrackAmplifierRegisters.ConfigWatchdog];

            bool reservedClear = (_registers[TrackAmplifierRegisters.ConfigFastStop] & 0xFF00) == 0;

            if (accel < 1 || accel > 255 || decel < 1 || decel > 255 ||
                fastStop < 1 || fastStop > 255 || watchdog < 50 || watchdog > 60000 || !reservedClear)
            {
                // Rejected: active config retained, write-result reports relationship + retained.
                _registers[TrackAmplifierRegisters.Fault] =
                    (ushort)(_registers[TrackAmplifierRegisters.Fault] | TrackAmplifierRegisters.ConfigInvalidFaultBit);
                SetWriteResult(TrackAmplifierRegisters.Control, 3, retained: true);
                return false;
            }

            _activeAccelStep = accel;
            _activeDecelStep = decel;
            _activeFastStopStep = fastStop;
            _configApplied = true;
            _registers[TrackAmplifierRegisters.Fault] =
                (ushort)(_registers[TrackAmplifierRegisters.Fault] & ~TrackAmplifierRegisters.ConfigInvalidFaultBit);
            _state = 1; // READY
            return true;
        }

        // --------------------------------------------------------------------
        // Ramp + lease
        // --------------------------------------------------------------------

        private void StepRamp()
        {
            if (_targetDuty == _appliedDuty)
            {
                return;
            }

            int step = _fastStopping ? _activeFastStopStep : _activeAccelStep;
            int direction = _targetDuty > _appliedDuty ? 1 : -1;
            int delta = direction * Math.Min(step, Math.Abs(_targetDuty - _appliedDuty));
            _appliedDuty = Clamp(_appliedDuty + delta);

            if (_appliedDuty == _targetDuty)
            {
                if (_targetDuty == Neutral)
                {
                    _stoppingActive = false;
                    _fastStopping = false;
                    if (!_inhibited)
                    {
                        _state = _configApplied ? 1 : 0; // READY / NOT_INITIALIZED
                    }
                }
                else if (_enableActive && _configApplied && !_inhibited)
                {
                    _state = 2; // RUN
                }
            }
        }

        private void StepLease()
        {
            if (!_leaseArmed)
            {
                return;
            }

            _leaseTicks++;
            if (_leaseTicks >= LeaseTimeoutTicks && !_inhibited)
            {
                // Movement-permission lease expired: local fast ramp to neutral + latched INHIBITED.
                _inhibited = true;
                _state = 7; // LEASE_EXPIRED
                _stoppingActive = true;
                _fastStopping = true;
                _targetDuty = Neutral;
                _registers[TrackAmplifierRegisters.Fault] =
                    (ushort)(_registers[TrackAmplifierRegisters.Fault] | TrackAmplifierRegisters.LeaseExpiredFaultBit);
            }
        }

        /// <summary>
        /// The lease is only fresh while it has been explicitly armed (by a MOTION_PERMISSION
        /// renewal) AND the renewal window has not yet elapsed. Mirrors the PIC18
        /// (<c>amp_core.c</c>: <c>lease_ticks = AMP_LEASE_TICKS</c> at init): the lease starts
        /// EXPIRED, so a never-renewed amplifier is never lease-fresh and a non-neutral command
        /// cannot produce RUN until a renewal has armed it.
        /// </summary>
        private bool LeaseFresh() => _leaseArmed && _leaseTicks < LeaseTimeoutTicks;

        // --------------------------------------------------------------------
        // Helpers
        // --------------------------------------------------------------------

        private void SetWriteResult(byte address, byte result, bool retained)
        {
            ushort value = (ushort)((address & 0xFF) | (result << 8) | (retained ? TrackAmplifierRegisters.WriteRetainedBit : 0));
            _registers[TrackAmplifierRegisters.WriteResult] = value;
        }

        private ushort EncodeApplied()
        {
            if (_appliedDuty > AmplifierSpeedMapper.NeutralPwm)
            {
                return (ushort)(_appliedDuty | TrackAmplifierRegisters.AppliedDirectionBit); // forward
            }
            return (ushort)_appliedDuty;
        }

        private static int Clamp(int pwm)
            => Math.Max(AmplifierSpeedMapper.MinPwm, Math.Min(AmplifierSpeedMapper.MaxPwm, pwm));
    }
}
