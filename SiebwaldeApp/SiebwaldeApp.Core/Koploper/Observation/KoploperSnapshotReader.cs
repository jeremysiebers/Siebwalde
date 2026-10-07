using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SiebwaldeApp.Core.Koploper
{
    /// <summary>
    /// Orchestrates a coherent snapshot read: locate the process, verify the binary, detect a
    /// restart, attach a read-only memory reader, read the object graph twice per attempt and
    /// only publish when both walks are coherent. Publishes <see cref="KoploperSourceHealth"/>
    /// and <see cref="KoploperStateSnapshot"/> with a per-generation sequence. No polling, no
    /// background service, no write access and no hardware actions.
    /// </summary>
    public sealed class KoploperSnapshotReader : IKoploperSnapshotReader
    {
        private readonly IKoploperProcessLocator _processLocator;
        private readonly IKoploperExecutableVerifier _executableVerifier;
        private readonly Func<IKoploperMemoryReader> _memoryReaderFactory;
        private readonly IKoploperRawObjectGraphReader _rawGraphReader;
        private readonly IKoploperBlockStateDecoder _blockStateDecoder;
        private readonly KoploperSnapshotReadOptions _options;
        private readonly Func<DateTimeOffset> _clock;

        private KoploperProcessGeneration? _lastGeneration;
        private long _sequence;

        public KoploperSnapshotReader(
            IKoploperProcessLocator processLocator,
            IKoploperExecutableVerifier executableVerifier,
            Func<IKoploperMemoryReader> memoryReaderFactory,
            IKoploperRawObjectGraphReader rawGraphReader,
            IKoploperBlockStateDecoder blockStateDecoder,
            KoploperSnapshotReadOptions? options = null,
            Func<DateTimeOffset>? clock = null)
        {
            _processLocator = processLocator ?? throw new ArgumentNullException(nameof(processLocator));
            _executableVerifier = executableVerifier ?? throw new ArgumentNullException(nameof(executableVerifier));
            _memoryReaderFactory = memoryReaderFactory ?? throw new ArgumentNullException(nameof(memoryReaderFactory));
            _rawGraphReader = rawGraphReader ?? throw new ArgumentNullException(nameof(rawGraphReader));
            _blockStateDecoder = blockStateDecoder ?? throw new ArgumentNullException(nameof(blockStateDecoder));
            _options = options ?? new KoploperSnapshotReadOptions();
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        /// <inheritdoc />
        public KoploperSnapshotReadResult ReadSnapshot()
        {
            // 1. Locate.
            KoploperProcessStatus status = _processLocator.TryLocate(out KoploperProcessInfo? info);
            if (status != KoploperProcessStatus.Found)
            {
                return LocateFailure(status);
            }

            if (info is null)
            {
                return new KoploperSnapshotReadResult(KoploperSourceHealth.Unavailable, null, Array.Empty<KoploperDiagnosticCode>());
            }

            // 2. Verify the executable against the supported layout profile.
            KoploperVersionGateResult gate = _executableVerifier.Verify(info.ExecutablePath, out KoploperExecutableIdentity? observedIdentity);
            switch (gate)
            {
                case KoploperVersionGateResult.UnsupportedVersion:
                    return new KoploperSnapshotReadResult(
                        KoploperSourceHealth.UnsupportedVersion,
                        null,
                        new[] { KoploperDiagnosticCode.KOPLOPER_UNSUPPORTED_BINARY });

                case KoploperVersionGateResult.NotPresent:
                case KoploperVersionGateResult.Unknown:
                    return new KoploperSnapshotReadResult(
                        KoploperSourceHealth.Unavailable,
                        null,
                        new[] { KoploperDiagnosticCode.KOPLOPER_UNSUPPORTED_BINARY });

                case KoploperVersionGateResult.Supported:
                    break;

                default:
                    return new KoploperSnapshotReadResult(
                        KoploperSourceHealth.Unavailable,
                        null,
                        new[] { KoploperDiagnosticCode.KOPLOPER_UNSUPPORTED_BINARY });
            }

            if (observedIdentity is null)
            {
                return new KoploperSnapshotReadResult(
                    KoploperSourceHealth.Unavailable,
                    null,
                    new[] { KoploperDiagnosticCode.KOPLOPER_UNSUPPORTED_BINARY });
            }

            // 3. Compute the process generation and detect a restart. A restart clears the
            //    retained generation and sequence and is reported once via KOPLOPER_RESTARTED.
            var generation = KoploperProcessGeneration.From(info);
            bool restartDetected = false;
            if (_lastGeneration is { } previous && previous != generation)
            {
                restartDetected = true;
                _lastGeneration = null;
                _sequence = 0;
            }

            // 4. Attach a read-only memory reader.
            IKoploperMemoryReader? reader = null;
            try
            {
                reader = _memoryReaderFactory();
                KoploperMemoryAccessResult attach = reader.Attach(info.ProcessId);
                if (attach != KoploperMemoryAccessResult.Attached)
                {
                    return AttachFailure(attach);
                }

                // 5. Bounded coherent-read loop: two walks per attempt must agree, and the
                //    process generation must not change between them.
                var stopwatch = Stopwatch.StartNew();
                KoploperRawObjectGraphObservation? coherent = null;
                int attempts = 0;
                int maxAttempts = _options.MaxAttempts;

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    attempts = attempt;

                    KoploperObjectGraphDecodeResult resultA = _rawGraphReader.Read(reader, info.ModuleBaseAddress, out KoploperRawObjectGraphObservation? observationA);

                    // Re-locate to confirm the process generation is unchanged mid-read.
                    KoploperProcessStatus relocStatus = _processLocator.TryLocate(out KoploperProcessInfo? relocInfo);
                    bool sameGeneration = relocStatus == KoploperProcessStatus.Found
                        && relocInfo is not null
                        && relocInfo.ProcessId == info.ProcessId
                        && relocInfo.ProcessStartTimeUtc == info.ProcessStartTimeUtc;

                    KoploperObjectGraphDecodeResult resultB = _rawGraphReader.Read(reader, info.ModuleBaseAddress, out KoploperRawObjectGraphObservation? observationB);

                    if (resultA == KoploperObjectGraphDecodeResult.Success
                        && resultB == KoploperObjectGraphDecodeResult.Success
                        && sameGeneration
                        && observationA is not null
                        && observationB is not null
                        && KoploperCoherencyValidator.IsCoherent(observationA, observationB))
                    {
                        coherent = observationB;
                        break;
                    }
                }

                stopwatch.Stop();
                TimeSpan readDuration = stopwatch.Elapsed;

                if (coherent is null)
                {
                    return new KoploperSnapshotReadResult(
                        KoploperSourceHealth.Inconsistent,
                        null,
                        new[] { KoploperDiagnosticCode.KOPLOPER_SNAPSHOT_INCONSISTENT });
                }

                // 6. Increment the per-generation sequence (checked: a wrap fails closed).
                long sequence;
                try
                {
                    _sequence = checked(_sequence + 1);
                    sequence = _sequence;
                }
                catch (OverflowException)
                {
                    return new KoploperSnapshotReadResult(
                        KoploperSourceHealth.Unavailable,
                        null,
                        new[] { KoploperDiagnosticCode.KOPLOPER_SNAPSHOT_INCONSISTENT });
                }

                _lastGeneration = generation;

                KoploperBlockStateDecodeResult blockDecode = _blockStateDecoder.Decode(coherent.Registry);

                var locomotives = new List<KoploperLocomotiveSnapshot>(coherent.Registry.Locomotives.Count);
                foreach (KoploperRawLocomotive loco in coherent.Registry.Locomotives)
                {
                    locomotives.Add(new KoploperLocomotiveSnapshot((int)loco.InternalLocomotiveId, loco.ObjectAddress));
                }

                int retryCount = attempts - 1;
                bool degraded = retryCount > 0 || blockDecode.Diagnostics.Count > 0;
                KoploperSourceHealth sourceHealth = degraded ? KoploperSourceHealth.Degraded : KoploperSourceHealth.Healthy;

                var snapshot = new KoploperStateSnapshot(
                    generation,
                    observedIdentity,
                    sequence,
                    _clock(),
                    readDuration,
                    retryCount,
                    true,
                    sourceHealth,
                    blockDecode.Snapshots,
                    blockDecode.Diagnostics,
                    locomotives);

                var diagnostics = new List<KoploperDiagnosticCode>();
                if (restartDetected)
                {
                    diagnostics.Add(KoploperDiagnosticCode.KOPLOPER_RESTARTED);
                }

                return new KoploperSnapshotReadResult(sourceHealth, snapshot, diagnostics);
            }
            finally
            {
                reader?.Dispose();
            }
        }

        private static KoploperSnapshotReadResult LocateFailure(KoploperProcessStatus status)
        {
            return status switch
            {
                KoploperProcessStatus.NotFound => new KoploperSnapshotReadResult(
                    KoploperSourceHealth.ProcessNotFound,
                    null,
                    new[] { KoploperDiagnosticCode.KOPLOPER_PROCESS_NOT_FOUND }),

                KoploperProcessStatus.AccessDenied => new KoploperSnapshotReadResult(
                    KoploperSourceHealth.Unavailable,
                    null,
                    new[] { KoploperDiagnosticCode.KOPLOPER_ACCESS_DENIED }),

                _ => new KoploperSnapshotReadResult(
                    KoploperSourceHealth.Unavailable,
                    null,
                    Array.Empty<KoploperDiagnosticCode>()),
            };
        }

        private static KoploperSnapshotReadResult AttachFailure(KoploperMemoryAccessResult attach)
        {
            return attach switch
            {
                KoploperMemoryAccessResult.ProcessNotFound => new KoploperSnapshotReadResult(
                    KoploperSourceHealth.ProcessNotFound,
                    null,
                    new[] { KoploperDiagnosticCode.KOPLOPER_PROCESS_NOT_FOUND }),

                _ => new KoploperSnapshotReadResult(
                    KoploperSourceHealth.Unavailable,
                    null,
                    new[] { KoploperDiagnosticCode.KOPLOPER_ACCESS_DENIED }),
            };
        }
    }
}
