using System;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core.TrackApplication.Comm;

namespace SiebwaldeApp.Core
{
    public readonly struct FlashOneRunResult
    {
        public bool Success { get; }
        public string? Error { get; }
        public byte TargetAmplifier { get; }

        /// <summary>
        /// When the PIC32 reports an ERROR, the decoded FlashSequencer sub-step (2..15) that
        /// failed, or null when the failure could not be attributed to a sub-step (a host-side
        /// timeout, a protocol deviation, a failed preflight/validation).
        /// </summary>
        public int? FailedSubStep { get; }

        /// <summary>0 on success, non-zero on any failure (fail-closed; no auto-retry).</summary>
        public int ExitCode => Success ? 0 : 1;

        private FlashOneRunResult(bool success, string? error, byte target, int? failedSubStep)
        {
            Success = success;
            Error = error;
            TargetAmplifier = target;
            FailedSubStep = failedSubStep;
        }

        public static FlashOneRunResult Ok(byte target) => new(true, null, target, null);
        public static FlashOneRunResult Fail(string error, byte target, int? failedSubStep = null)
            => new(false, error, target, failedSubStep);
    }

    /// <summary>
    /// Production console-adapter orchestrator for the proven PIC18 single-target flash path
    /// (command 131 EXEC_FW_STATE_FLASH_ONE_SLAVE). It reuses the REAL production comm client
    /// (<see cref="TrackCommClientAsync"/>) over the REAL UDP transport
    /// (<see cref="RawUdpTransport"/> → <see cref="RawUdpTrackTransport"/>), and composes the
    /// existing <see cref="FlashOneAmplifierCommand"/> + <see cref="TrackAmplifierBootloaderHelpers"/>
    /// + <see cref="SendNextFwDataPacket"/> exactly as the production host does.
    ///
    /// Order of operations (all before any write):
    /// 1. read-only ownership preflight (fail-closed);
    /// 2. firmware HEX validation (SHA-256 + embedded checksum) via
    ///    <see cref="FirmwareArtifactVerifier.Validate"/>;
    /// 3. transport/comm composition, bounded heartbeat start, then
    ///    <see cref="FlashOneAmplifierCommand.ExecuteAsync"/>.
    /// The heartbeat runs only while the flash command is in flight and is never an auto-retry.
    /// </summary>
    public sealed class FlashOneAmplifierRunner
    {
        private const string LoggerInstance = "FlashOneAmplifier";

        private readonly Func<ITrackTransport>? _transportFactory;
        private readonly IFlashOneOwnershipPreflight _preflight;
        private readonly TimeSpan _messageTimeout;
        private readonly TimeSpan _flashTimeout;
        private readonly Action<ReceivedMessage>? _onProgress;

        /// <param name="transportFactory">
        /// Null in production (the real UDP transport is built). Supplied only by offline tests.
        /// </param>
        /// <param name="preflight">Ownership preflight; defaults to the real read-only check.</param>
        public FlashOneAmplifierRunner(
            Func<ITrackTransport>? transportFactory = null,
            IFlashOneOwnershipPreflight? preflight = null,
            TimeSpan? messageTimeout = null,
            TimeSpan? flashTimeout = null,
            Action<ReceivedMessage>? onProgress = null)
        {
            _transportFactory = transportFactory;
            _preflight = preflight ?? new FlashOneOwnershipPreflight();
            _messageTimeout = messageTimeout ?? TimeSpan.FromSeconds(10);
            _flashTimeout = flashTimeout ?? TimeSpan.FromMinutes(15);
            _onProgress = onProgress;
        }

        public async Task<FlashOneRunResult> RunAsync(
            string hexPath,
            string? expectedSha256Hex,
            byte amplifier,
            CancellationToken ct = default)
        {
            // 1. Ownership preflight before any send/bind. Fail closed.
            var preflight = _preflight.Check();
            if (!preflight.Ok)
                return FlashOneRunResult.Fail(
                    preflight.Error ?? "Ownership preflight failed.", amplifier);

            // 2. Validate the HEX (SHA-256 + embedded checksum) before any write. A failed
            //    validation yields no artifact, so no command can be composed and nothing is sent.
            var validation = FirmwareArtifactVerifier.Validate(hexPath, expectedSha256Hex, LoggerInstance);
            if (!validation.Success)
                return FlashOneRunResult.Fail(
                    validation.Error ?? "Firmware validation failed.", amplifier);

            var artifact = validation.Artifact!;

            // 3. Compose the REAL production transport + comm client. The transport factory is
            //    supplied only by offline tests; the production default is the real UDP transport
            //    (192.168.1.193, ports 10000/10001). A bind failure here fails closed.
            ITrackTransport transport;
            try
            {
                transport = _transportFactory?.Invoke() ?? CreateRealTransport();
            }
            catch (Exception ex)
            {
                return FlashOneRunResult.Fail(
                    $"Could not open the track-controller transport: {ex.Message}", amplifier);
            }

            var variables = new TrackApplicationVariables();
            var comm = new TrackCommClientAsync(transport, variables);
            var sendNext = new SendNextFwDataPacket(comm, artifact.Helpers);
            var command = new FlashOneAmplifierCommand(
                comm, variables, artifact.Helpers, sendNext,
                messageTimeout: _messageTimeout,
                flashTimeout: _flashTimeout,
                onProgress: _onProgress);

            try
            {
                await comm.StartAsync(realHardwareMode: true, ct).ConfigureAwait(false);

                // 4. Bounded heartbeat for the whole flash. Started only here and cancelled +
                //    awaited in the inner finally, so it cannot outlive the flash or leak into a
                //    later session. It shares the comm path (send-only) and is isolated from the
                //    flash command's response channel (see FlashOneHeartbeat).
                var heartbeat = new FlashOneHeartbeat(comm);
                heartbeat.Start(ct);
                try
                {
                    var result = await command.ExecuteAsync(amplifier, ct).ConfigureAwait(false);
                    return result.Success
                        ? FlashOneRunResult.Ok(result.TargetAmplifier)
                        : FlashOneRunResult.Fail(
                            result.Error ?? "Flash failed.",
                            result.TargetAmplifier,
                            result.FailedSubStep);
                }
                finally
                {
                    await heartbeat.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                return FlashOneRunResult.Fail(
                    "Timed out waiting for the PIC32 acknowledgement.", amplifier);
            }
            catch (Exception ex)
            {
                return FlashOneRunResult.Fail(ex.Message, amplifier);
            }
            finally
            {
                try { await comm.StopAsync().ConfigureAwait(false); } catch { /* best-effort */ }
                try { await comm.DisposeAsync().ConfigureAwait(false); } catch { /* best-effort */ }
            }
        }

        /// <summary>
        /// The REAL production UDP transport, identical to the transport built by
        /// <see cref="TrackApplicationRuntimeHost.CreateRealTransport"/> in Real mode and by
        /// <see cref="StopReachabilityHarness"/>'s live path.
        /// </summary>
        private static ITrackTransport CreateRealTransport()
        {
            var rawUdp = new RawUdpTransport(
                CoreConfiguration.TrackControllerIpAddress,
                CoreConfiguration.TrackControllerSendingPort,
                CoreConfiguration.TrackControllerReceivingPort);
            return new RawUdpTrackTransport(rawUdp);
        }
    }
}
