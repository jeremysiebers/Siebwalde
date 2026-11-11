using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SiebwaldeApp.Core
{
    /// <summary>RAM parameters owned by a physical position, not by a device or operational group.</summary>
    public sealed record AmplifierConfiguration(byte Acceleration = 2, byte Deceleration = 2,
        byte FastStop = 8, ushort WatchdogMs = 250, ushort Tag = 1);

    /// <summary>
    /// Downloads the Phase-1 RAM configuration (HR3..HR6) to one amplifier, APPLYs it, and verifies
    /// the INIT_DONE|CONFIG_OK state plus the config echo via fresh readback.
    ///
    /// The optional <paramref name="log"/> sink is diagnostic-only: it observes the same execution
    /// path and the same <see cref="TrackAmplifierItem"/> state without issuing any additional
    /// controller transaction and without changing any register value, timeout, APPLY semantics,
    /// retry or scheduling. When null (the default) the execution path is unchanged.
    ///
    /// Diagnostic states emitted (no new transactions):
    ///   ENTRY       - invocation with the requested configuration and the timeout.
    ///   REQUESTED   - a config/control value is about to be written (intent).
    ///   TRANSMITTED - the write <see cref="SendMessage"/> was dispatched to the transport.
    ///   OBSERVED    - a fresh SLAVEINFO echo/state value was read from <see cref="TrackAmplifierItem.HoldingReg"/>.
    ///   VERIFIED    - an OBSERVED value matched the requested value / INIT_DONE|CONFIG_OK pair.
    ///   FAILED      - an OBSERVED value did not match within the deadline, or a precondition held.
    ///   RETURN      - the exact exit reason (success / incompatible / timeout / exception).
    /// </summary>
    public sealed class AmplifierConfigurationDownloader
    {
        private readonly ITrackCommClient _comm;
        private readonly Action<string>? _log;

        public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(2);

        public AmplifierConfigurationDownloader(ITrackCommClient comm, Action<string>? log = null)
        {
            _comm = comm;
            _log = log;
        }

        public static SendMessage Exchange(ushort slave, byte register, ushort value, bool read = false)
            => new(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH,
                new byte[] { checked((byte)slave), read ? (byte)0x55 : (byte)0xAA, 1, register,
                    (byte)value, (byte)(value >> 8) });

        public async Task<bool> DownloadAsync(TrackAmplifierItem item, AmplifierConfiguration config, CancellationToken ct)
        {
            ushort slave = item.SlaveNumber;
            _log?.Invoke(
                $"slave={slave} ENTRY requested=(accel={config.Acceleration},decel={config.Deceleration}," +
                $"faststop={config.FastStop},watchdog={config.WatchdogMs},tag={config.Tag}) " +
                $"timeout={Timeout.TotalMilliseconds}ms");

            item.ConfigurationVerified = false;
            _log?.Invoke($"slave={slave} OBSERVED ConfigurationVerified=false (reset)");

            if (!AmplifierPhase1Safety.Compatible(item))
            {
                item.Phase1Failure = AmplifierPhase1Safety.IncompatibilityReason(item)
                    ?? AmplifierPhase1Safety.IncompatibleFirmware;

                if (_log is not null)
                {
                    // Diagnostic-only decomposition of the fail-closed reason. These are read-only
                    // pure checks; they do NOT change the assignment above or the control flow. The
                    // freshness gate is the common false-positive: a stale readback makes
                    // Compatible() false while IncompatibilityReason() (which does not assess
                    // freshness) reports "incompatible firmware" as a fallback. Gated behind _log so
                    // the null-log path performs no extra work.
                    bool fresh = TrackAmplifierDataFreshness.HasCurrentProtocolReadback(item);
                    bool identityOk = TrackAmplifierRegisters.HasCompatibleIdentityFields(
                        item.HoldingReg[TrackAmplifierRegisters.Identity]);
                    bool protocolOk = TrackAmplifierRegisters.HasCompatibleProtocolVersion(
                        item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol]);
                    _log(
                        $"slave={slave} FAILED precondition incompatible: {item.Phase1Failure} " +
                        $"(fresh={fresh}, identityOk={identityOk}, protocolOk={protocolOk})");
                    _log($"slave={slave} RETURN false (incompatible)");
                }

                return false;
            }

            if ((item.HoldingReg[TrackAmplifierRegisters.Identity] & TrackAmplifierRegisters.TwoPhaseConfigCapability) == 0)
            {
                item.Phase1Failure = "two-phase configuration unsupported";
                _log?.Invoke(
                    $"slave={slave} FAILED precondition missing TwoPhaseConfigCapability " +
                    $"(HR7=0x{item.HoldingReg[TrackAmplifierRegisters.Identity]:X4})");
                _log?.Invoke($"slave={slave} RETURN false (two-phase unsupported)");
                return false;
            }

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(ct);
            deadline.CancelAfter(Timeout);
            try
            {
                var values = new ushort[]
                {
                    (ushort)(config.Acceleration | config.Deceleration << 8),
                    config.FastStop,
                    config.WatchdogMs,
                    config.Tag
                };
                for (byte i = 0; i < values.Length; i++)
                {
                    byte address = (byte)(TrackAmplifierRegisters.ConfigRamp + i);
                    await WriteAndVerifyAsync(item, address, values[i], deadline.Token).ConfigureAwait(false);
                }

                // Explicit 0 then 1: never depend on a concurrent lease renewal to re-arm APPLY.
                await WriteAndVerifyAsync(item, TrackAmplifierRegisters.Control, 0, deadline.Token).ConfigureAwait(false);

                long sequence = item.ProtocolReadbackSequence;
                _log?.Invoke(
                    $"slave={slave} REQUESTED APPLY HR1=0x{TrackAmplifierRegisters.ApplyConfigBit:X4} " +
                    $"(seq-before={sequence})");
                await _comm.SendAsync(
                    Exchange(slave, TrackAmplifierRegisters.Control, TrackAmplifierRegisters.ApplyConfigBit),
                    deadline.Token).ConfigureAwait(false);
                _log?.Invoke($"slave={slave} TRANSMITTED APPLY HR1=0x{TrackAmplifierRegisters.ApplyConfigBit:X4}");

                // HR15 is read for diagnostics only. The APPLY completion signal is the HR8
                // INIT_DONE|CONFIG_OK pair plus the HR3..6 config echo — never write_result,
                // whose value == 1 is indistinguishable from a 100 ms lease renewal (HR1=0x4000).
                await _comm.SendAsync(
                    Exchange(slave, TrackAmplifierRegisters.State, 0, true), deadline.Token).ConfigureAwait(false);
                await _comm.SendAsync(
                    Exchange(slave, TrackAmplifierRegisters.WriteResult, 0, true), deadline.Token).ConfigureAwait(false);

                await WaitObserveAsync(item,
                    () => item.ProtocolReadbackSequence > sequence &&
                        (item.HoldingReg[TrackAmplifierRegisters.State] & 0x0600) == 0x0600 &&
                        values.SequenceEqual(item.HoldingReg.Skip(TrackAmplifierRegisters.ConfigRamp).Take(4)),
                    () => LogStateObservation(item),
                    deadline.Token).ConfigureAwait(false);

                if (!AmplifierPhase1Safety.Compatible(item))
                    throw new InvalidOperationException("compatibility changed during configuration");

                item.ConfigurationVerified = true;
                item.Phase1Failure = null;
                _log?.Invoke($"slave={slave} VERIFIED APPLY (INIT_DONE|CONFIG_OK + echo)");
                _log?.Invoke($"slave={slave} RETURN true (configured+verified)");
                return true;
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                item.Phase1Failure =
                    $"configuration download/verification timeout; write-result=0x{item.HoldingReg[TrackAmplifierRegisters.WriteResult]:X4}";
                LogStateObservation(item);
                _log?.Invoke($"slave={slave} RETURN false (timeout)");
                return false;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                item.Phase1Failure = $"configuration download failed: {ex.Message}";
                _log?.Invoke($"slave={slave} FAILED exception {ex.GetType().Name}: {ex.Message}");
                _log?.Invoke($"slave={slave} RETURN false (exception)");
                return false;
            }
            catch (OperationCanceledException)
            {
                // Caller-token cancellation (distinct from the deadline timeout above, which is
                // already handled): the existing contract lets this propagate. Log then rethrow.
                _log?.Invoke($"slave={slave} FAILED cancellation (caller token requested)");
                _log?.Invoke($"slave={slave} RETURN throw (cancellation)");
                throw;
            }
        }

        private async Task WriteAndVerifyAsync(TrackAmplifierItem item, byte address, ushort value, CancellationToken ct)
        {
            ushort slave = item.SlaveNumber;
            long sequence = item.ProtocolReadbackSequence;
            _log?.Invoke($"slave={slave} REQUESTED HR{address}=0x{value:X4} (seq-before={sequence})");
            await _comm.SendAsync(Exchange(slave, address, value), ct).ConfigureAwait(false);
            _log?.Invoke($"slave={slave} TRANSMITTED write HR{address}=0x{value:X4}");

            // HR15 (write_result) is read for diagnostics only and MUST NOT gate completion:
            // the PIC32 lease renewal writes HR1=0x4000 every ~100 ms, which sets write_result=1
            // and clobbers the config write's write_result (address 3..6). A rejected write
            // leaves the register echo unchanged, so verifying by the echo
            // (HoldingReg[address] == value) still fails closed. HR1 is a decoded command
            // register (its echo reflects enable/lease state, never the written word), so the
            // explicit HR1=0 write is acknowledged by a fresh readback only.
            await _comm.SendAsync(Exchange(slave, address, 0, true), ct).ConfigureAwait(false);
            await _comm.SendAsync(Exchange(slave, TrackAmplifierRegisters.WriteResult, 0, true), ct).ConfigureAwait(false);

            await WaitObserveAsync(item,
                () => item.ProtocolReadbackSequence > sequence &&
                    (address == TrackAmplifierRegisters.Control || item.HoldingReg[address] == value),
                () => _log?.Invoke(
                    $"slave={slave} OBSERVED HR{address}=0x{item.HoldingReg[address]:X4} " +
                    $"seq={item.ProtocolReadbackSequence}"),
                ct).ConfigureAwait(false);

            _log?.Invoke(
                $"slave={slave} VERIFIED HR{address}=0x{value:X4} observed=0x{item.HoldingReg[address]:X4}");
        }

        /// <summary>Logs the current HR8 + HR3..HR6 + HR15 snapshot (no transaction issued).</summary>
        private void LogStateObservation(TrackAmplifierItem item)
        {
            if (_log is null)
                return;

            ushort[] r = item.HoldingReg;
            _log(
                $"slave={item.SlaveNumber} OBSERVED HR8=0x{r[TrackAmplifierRegisters.State]:X4} " +
                $"HR3=0x{r[TrackAmplifierRegisters.ConfigRamp]:X4} " +
                $"HR4=0x{r[TrackAmplifierRegisters.ConfigFastStop]:X4} " +
                $"HR5=0x{r[TrackAmplifierRegisters.ConfigWatchdog]:X4} " +
                $"HR6=0x{r[TrackAmplifierRegisters.ConfigTag]:X4} " +
                $"HR15=0x{r[TrackAmplifierRegisters.WriteResult]:X4} " +
                $"seq={item.ProtocolReadbackSequence}");
        }

        private static async Task WaitObserveAsync(
            TrackAmplifierItem item,
            Func<bool> predicate,
            Action? observe,
            CancellationToken ct)
        {
            long lastSequence = item.ProtocolReadbackSequence;
            while (!predicate())
            {
                await Task.Delay(10, ct).ConfigureAwait(false);
                if (observe is not null && item.ProtocolReadbackSequence != lastSequence)
                {
                    lastSequence = item.ProtocolReadbackSequence;
                    observe();
                }
            }
        }
    }
}
