using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.Koploper.Validation;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Koploper.Windows;

namespace SiebwaldeApp.Koploper.Validation.Host
{
    /// <summary>
    /// KIS-06 simulator cross-validation harness. Commands:
    /// <list type="bullet">
    ///   <item><c>validate</c> — live: run the real read-only reservation observer loop and write
    ///       <see cref="KoploperValidationSample"/> JSONL, then print a summary.</item>
    ///   <item><c>replay</c> — offline: read golden <see cref="KoploperGoldenComparison"/> JSONL and
    ///       feed each line deterministically through <see cref="KoploperComparisonEngine"/>,
    ///       verifying any stored expected result.</item>
    /// </list>
    /// No write access to Koploper and no hardware actions are performed.
    /// </summary>
    public static class Program
    {
        private const string DefaultTracePath = "koploper-validation.jsonl";
        private const string ReadonlyDefaultTracePath = "readonly-validation.jsonl";
        private const int DefaultCycles = 10;
        private const int DefaultIntervalMs = 500;
        private const int DefaultReadRegisterWaitMs = 5000;
        private const int ReadRegisterPollIntervalMs = 250;
        private static readonly int[] ReadRegisterSlaves = { 1, 3, 4, 6 };

        private static readonly KoploperValidationOptions ValidationOptions = new();

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            string command = args[0].ToLowerInvariant();
            string[] rest = args.Skip(1).ToArray();

            return command switch
            {
                "validate" => RunValidate(rest),
                "replay" => RunReplay(rest),
                "identify" => RunIdentify(rest),
                "readonly-validate" => RunReadonlyValidate(rest),
                "--help" or "-h" or "/?" => Help(),
                _ => Unknown(command)
            };
        }

        // ---------------------------------------------------------------- validate (live)

        private static int RunValidate(string[] args)
        {
            int cycles = DefaultCycles;
            int intervalMs = DefaultIntervalMs;
            string tracePath = DefaultTracePath;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--cycles" when i + 1 < args.Length:
                        cycles = int.Parse(args[++i]);
                        break;
                    case "--interval-ms" when i + 1 < args.Length:
                        intervalMs = int.Parse(args[++i]);
                        break;
                    case "--trace" when i + 1 < args.Length:
                        tracePath = args[++i];
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown validate argument: {args[i]}");
                        return 2;
                }
            }

            if (cycles < 0)
            {
                Console.Error.WriteLine("--cycles must be >= 0.");
                return 2;
            }

            var composition = ValidationHostComposition.Create(tracePath);
            var observer = composition.Observer;
            var trace = composition.TraceWriter;

            int authoritative = 0;
            var occupiedByLoc = new Dictionary<int, int?>();
            var reservedByLoc = new Dictionary<int, IReadOnlyList<int>>();
            var results = new SortedSet<KoploperValidationResult>();

            try
            {
                trace.WriteScenarioStart("validate-live");

                for (int cycle = 0; cycle < cycles; cycle++)
                {
                    KoploperReservationObservation observation = observer.Refresh();

                    // Live composition has no independent cross-check source, so the comparison
                    // engine is exercised with a null independent sample: an authoritative
                    // observation yields SourceUnavailable, a non-authoritative one yields
                    // NonAuthoritativeSource.
                    KoploperValidationResult result =
                        KoploperComparisonEngine.Compare(observation, independent: null, ValidationOptions);
                    results.Add(result);

                    if (observation.IsAuthoritative)
                    {
                        authoritative++;
                    }

                    if (observation.Locomotives.Count == 0)
                    {
                        trace.WriteSample(ToSample(observation, result, locId: null, occupiedBlock: null, Array.Empty<int>()));
                    }
                    else
                    {
                        foreach (KoploperLocomotiveTrajectory locomotive in observation.Locomotives)
                        {
                            occupiedByLoc[locomotive.InternalLocomotiveId] = locomotive.OccupiedBlock;
                            reservedByLoc[locomotive.InternalLocomotiveId] = locomotive.ReservedBlocks.ToArray();
                            trace.WriteSample(ToSample(
                                observation,
                                result,
                                locomotive.InternalLocomotiveId,
                                locomotive.OccupiedBlock,
                                locomotive.ReservedBlocks.ToArray()));
                        }
                    }

                    if (cycle < cycles - 1)
                    {
                        Thread.Sleep(intervalMs);
                    }
                }

                trace.WriteScenarioEnd("validate-live");
            }
            finally
            {
                trace.Dispose();
                observer.Dispose();
            }

            Console.WriteLine("== Koploper validate summary ==");
            Console.WriteLine($"Cycles: {cycles}");
            Console.WriteLine($"Authoritative observations: {authoritative}");
            Console.WriteLine($"Per-locomotive occupied/reserved seen ({occupiedByLoc.Count} locos):");
            foreach (int locId in occupiedByLoc.Keys.OrderBy(id => id))
            {
                string reserved = reservedByLoc.TryGetValue(locId, out IReadOnlyList<int>? r)
                    ? string.Join(",", r.OrderBy(b => b))
                    : string.Empty;
                Console.WriteLine($"  loc {locId}: occupied={occupiedByLoc[locId]?.ToString() ?? "?"} reserved=[{reserved}]");
            }

            Console.WriteLine($"Validation results seen: {string.Join(", ", results.Select(r => r.ToString()))}");
            return 0;
        }

        // ---------------------------------------------------------------- replay (offline)

        private static int RunReplay(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("replay requires a golden JSONL file path.");
                return 2;
            }

            string goldenPath = args[0];
            if (!File.Exists(goldenPath))
            {
                Console.Error.WriteLine($"Golden file not found: {goldenPath}");
                return 2;
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            int total = 0;
            int agree = 0;
            var mismatches = new List<string>();

            foreach (string line in File.ReadLines(goldenPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                KoploperGoldenComparison? input;
                try
                {
                    input = JsonSerializer.Deserialize<KoploperGoldenComparison>(line, jsonOptions);
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine($"Skipping malformed line {total + 1}: {ex.Message}");
                    continue;
                }

                if (input is null)
                {
                    continue;
                }

                total++;

                KoploperValidationResult actual =
                    KoploperComparisonEngine.Compare(input.Observation, input.Independent, ValidationOptions);

                if (input.Expected is null || actual == input.Expected)
                {
                    agree++;
                }
                else
                {
                    mismatches.Add($"line {total}: expected={input.Expected} actual={actual}");
                }
            }

            Console.WriteLine("== Koploper replay summary ==");
            Console.WriteLine($"Golden file: {goldenPath}");
            Console.WriteLine($"Samples: {total}");
            Console.WriteLine($"Agree: {agree}");
            Console.WriteLine($"Disagree: {total - agree}");
            foreach (string mismatch in mismatches)
            {
                Console.WriteLine($"  {mismatch}");
            }

            return total == agree ? 0 : 1;
        }

        // ---------------------------------------------------------------- identify (live, read-only)

        private static int RunIdentify(string[] args)
        {
            if (args.Length != 0)
            {
                Console.Error.WriteLine("identify takes no arguments.");
                return 2;
            }

            KoploperSnapshotReader snapshotReader = CreateSnapshotReader();
            KoploperSnapshotReadResult result = snapshotReader.ReadSnapshot();

            Console.WriteLine($"Health: {result.Health}");

            bool failure = result.Snapshot is null
                || result.Health is KoploperSourceHealth.UnsupportedVersion
                    or KoploperSourceHealth.Unavailable
                    or KoploperSourceHealth.Inconsistent
                    or KoploperSourceHealth.ProcessNotFound;

            if (result.Snapshot is null)
            {
                Console.WriteLine($"Diagnostics: {string.Join(", ", result.Diagnostics)}");
                return failure ? 1 : 0;
            }

            KoploperStateSnapshot snapshot = result.Snapshot;

            Console.WriteLine($"ExecutableVersion: {snapshot.ExecutableIdentity.Version}");
            Console.WriteLine($"ExecutableSha256: {snapshot.ExecutableIdentity.Sha256Hex}");
            Console.WriteLine($"ProcessId: {snapshot.Generation.ProcessId}");
            Console.WriteLine($"ProcessStartTimeUtc: {snapshot.Generation.ProcessStartTimeUtc.ToString("O")}");
            Console.WriteLine($"Sequence: {snapshot.Sequence}");
            Console.WriteLine($"CapturedAtUtc: {snapshot.CapturedAtUtc.ToString("O")}");
            Console.WriteLine($"IsConsistent: {snapshot.IsConsistent}");
            Console.WriteLine($"SourceHealth: {snapshot.SourceHealth}");
            Console.WriteLine($"RetryCount: {snapshot.RetryCount}");
            Console.WriteLine($"BlockCount: {snapshot.Blocks.Count}");

            foreach (KoploperBlockSnapshot block in snapshot.Blocks.OrderBy(b => b.InternalBlockId))
            {
                Console.WriteLine(
                    $"BLOCK InternalBlockId={block.InternalBlockId} " +
                    $"DisplayBlockNumber={(block.DisplayBlockNumber?.ToString() ?? "NULL")} " +
                    $"State={block.State} RawState={block.RawState} " +
                    $"OwnerLocomotiveId={(block.OwnerLocomotiveId?.ToString() ?? "NULL")} " +
                    $"ManualBlocked={block.ManualBlocked}");
            }

            Console.WriteLine($"LocomotiveCount: {snapshot.Locomotives.Count}");
            foreach (KoploperLocomotiveSnapshot loco in snapshot.Locomotives)
            {
                Console.WriteLine($"LOCO InternalLocomotiveId={loco.InternalLocomotiveId}");
            }

            return failure ? 1 : 0;
        }

        private static KoploperSnapshotReader CreateSnapshotReader()
        {
            IKoploperMemoryLayout layout = Koploper94MemoryLayout.Instance;
            IKoploperProcessLocator processLocator = new WindowsKoploperProcessLocator();
            IKoploperExecutableVerifier executableVerifier = new KoploperExecutableVerifier(layout);
            Func<IKoploperMemoryReader> memoryReaderFactory = () => new WindowsKoploperMemoryReader();
            IKoploperRawObjectGraphReader rawGraphReader =
                new KoploperRawObjectGraphReader(layout, new KoploperDecodePlausibility());
            IKoploperBlockStateDecoder blockStateDecoder = new KoploperBlockStateDecoder();

            return new KoploperSnapshotReader(
                processLocator,
                executableVerifier,
                memoryReaderFactory,
                rawGraphReader,
                blockStateDecoder,
                options: null,
                clock: () => DateTimeOffset.UtcNow);
        }

        // ---------------------------------------------------------------- readonly-validate

        private static int RunReadonlyValidate(string[] args)
        {
            bool live = false;
            bool warm = false;
            bool readRegisters = false;
            int cycles = DefaultCycles;
            int intervalMs = DefaultIntervalMs;
            int readRegisterWaitMs = DefaultReadRegisterWaitMs;
            string tracePath = ReadonlyDefaultTracePath;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i].ToLowerInvariant())
                {
                    case "--live":
                        live = true;
                        break;
                    case "--warm":
                        warm = true;
                        break;
                    case "--read-registers":
                        readRegisters = true;
                        break;
                    case "--wait-ms" when i + 1 < args.Length:
                        readRegisterWaitMs = int.Parse(args[++i]);
                        break;
                    case "--cycles" when i + 1 < args.Length:
                        cycles = int.Parse(args[++i]);
                        break;
                    case "--interval-ms" when i + 1 < args.Length:
                        intervalMs = int.Parse(args[++i]);
                        break;
                    case "--trace" when i + 1 < args.Length:
                        tracePath = args[++i];
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown readonly-validate argument: {args[i]}");
                        return 2;
                }
            }

            if (cycles < 0)
            {
                Console.Error.WriteLine("--cycles must be >= 0.");
                return 2;
            }

            if (readRegisterWaitMs < 0)
            {
                Console.Error.WriteLine("--wait-ms must be >= 0.");
                return 2;
            }

            ReadOnlyTelemetryValidationHost host = ReadOnlyTelemetryValidationHost.Create("Simple Loop");

            var jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = false,
                Converters = { new JsonStringEnumConverter() }
            };

            try
            {
                Console.WriteLine("== readonly-validate composition ==");
                Console.WriteLine($"Profile: {host.Profile.Name}");
                Console.WriteLine($"KoploperBlockBindings: {host.KoploperBlockBindingMap.Count}");
                foreach (KeyValuePair<int, IReadOnlyList<int>> binding in host.KoploperBlockBindingMap.OrderBy(kv => kv.Key))
                {
                    Console.WriteLine($"  internalBlock {binding.Key} -> logicalSections [{string.Join(", ", binding.Value)}]");
                }

                Console.WriteLine($"PhysicalAmplifierMapping: {host.Profile.PhysicalAmplifierMapping.Count}");
                foreach (LayoutPhysicalAmplifierBinding binding in host.Profile.PhysicalAmplifierMapping)
                {
                    Console.WriteLine($"  section {binding.SectionId} -> physicalAmplifier {binding.PhysicalAmplifier}");
                }

                if (!live)
                {
                    Console.WriteLine("--live not supplied: composing only; NO transport opened, NO command transmitted.");
                }
                else if (warm)
                {
                    // Warm reconnect: the ONLY outbound transmission is CLIENT_CONNECTION_REQUEST (250).
                    host.StartAsync().GetAwaiter().GetResult();
                    host.EnableWarmReconnectAsync().GetAwaiter().GetResult();
                }
                else
                {
                    // Cold start: the ONLY outbound transmissions are the six telemetry-enablement
                    // commands in the mandated order (250→100→101→104→106→105).
                    host.StartAsync().GetAwaiter().GetResult();
                    host.EnableSessionAsync().GetAwaiter().GetResult();
                }

                if (readRegisters)
                {
                    RunReadRegisterReport(host, live, readRegisterWaitMs);
                }
                else
                {
                    WriteReconciliationLoop(host, tracePath, cycles, intervalMs, live, jsonOptions);
                }
            }
            finally
            {
                if (live)
                {
                    host.StopAsync().GetAwaiter().GetResult();
                }

                host.DisposeAsync().GetAwaiter().GetResult();
            }

            Console.WriteLine("== readonly-validate summary ==");
            Console.WriteLine(
                $"Mode: {(readRegisters ? (live ? "read-registers (live)" : "read-registers (dry, no transmission)") : (live ? "live" : "dry (no transmission)"))}; " +
                $"session: {(warm ? "warm-reconnect (250 only)" : "cold-start (250→100→101→104→106→105)")}");

            KoploperSessionCommandCounters counters = host.SessionCommandGuard.GetCounters();
            Console.WriteLine("Telemetry-enablement (session) commands — transmitted / acknowledged:");
            Console.WriteLine($"  CLIENT_CONNECTION_REQUEST: {counters.ClientConnectionRequestTransmitted} / {counters.ClientConnectionRequestAcknowledged}");
            Console.WriteLine($"  EXEC_MBUS_STATE_SLAVES_ON: {counters.ExecMbusStateSlavesOnTransmitted} / {counters.ExecMbusStateSlavesOnAcknowledged}");
            Console.WriteLine($"  EXEC_MBUS_STATE_SLAVE_DETECT: {counters.ExecMbusStateSlaveDetectTransmitted} / {counters.ExecMbusStateSlaveDetectAcknowledged}");
            Console.WriteLine($"  EXEC_MBUS_STATE_SLAVE_INIT: {counters.ExecMbusStateSlaveInitTransmitted} / {counters.ExecMbusStateSlaveInitAcknowledged}");
            Console.WriteLine($"  EXEC_MBUS_STATE_START_DATA_UPLOAD: {counters.ExecMbusStateStartDataUploadTransmitted} / {counters.ExecMbusStateStartDataUploadAcknowledged}");
            Console.WriteLine($"  EXEC_MBUS_STATE_SLAVE_ENABLE: {counters.ExecMbusStateSlaveEnableTransmitted} / {counters.ExecMbusStateSlaveEnableAcknowledged}");
            Console.WriteLine($"Rejected (allowlist) commands: {counters.RejectedCommands}");
            Console.WriteLine($"Order violations: {counters.OrderViolations}");
            Console.WriteLine($"Acknowledgement timeouts: {counters.AcknowledgementTimeouts}");
            Console.WriteLine(
                "Application/movement control (always 0 by construction): " +
                $"registerWrite(108)={counters.AmplifierRegisterWrite} hr0={counters.AmplifierHr0Writes} " +
                $"pwm={counters.PwmWrites} direction={counters.DirectionWrites} brake={counters.BrakeWrites} " +
                $"emo={counters.EmoStopWrites} switch={counters.SwitchWrites} loco={counters.LocomotiveCommands}");
            if (!readRegisters)
            {
                Console.WriteLine($"Trace: {tracePath}");
            }
            return 0;
        }

        // ---------------------------------------------------------------- readonly-validate: register read-back

        private static void WriteReconciliationLoop(
            ReadOnlyTelemetryValidationHost host,
            string tracePath,
            int cycles,
            int intervalMs,
            bool live,
            JsonSerializerOptions jsonOptions)
        {
            using var file = new StreamWriter(
                tracePath,
                append: true,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            file.WriteLine(JsonSerializer.Serialize(new
            {
                type = "readonlyValidateScenarioStart",
                live,
                profileId = host.Profile.Name,
                timestampUtc = DateTimeOffset.UtcNow.ToString("O")
            }, jsonOptions));

            for (int cycle = 0; cycle < cycles; cycle++)
            {
                LogicalPhysicalReconciliationObservation observation = host.Refresh();
                PhysicalSectionOccupancyObservation physical = host.ReadPhysicalObservation();

                KoploperTelemetryTraceRecord trace = KoploperTelemetryTraceBuilder.Build(
                    cycle,
                    DateTimeOffset.UtcNow,
                    host.SessionCommandGuard,
                    physical,
                    observation,
                    host.Profile,
                    host.Variables);

                file.WriteLine(JsonSerializer.Serialize(trace, jsonOptions));

                if (cycle < cycles - 1)
                {
                    Thread.Sleep(intervalMs);
                }
            }

            file.WriteLine(JsonSerializer.Serialize(new
            {
                type = "readonlyValidateScenarioEnd",
                live,
                profileId = host.Profile.Name,
                timestampUtc = DateTimeOffset.UtcNow.ToString("O")
            }, jsonOptions));
        }

        private static void RunReadRegisterReport(ReadOnlyTelemetryValidationHost host, bool live, int waitMs)
        {
            if (!live)
            {
                Console.WriteLine(
                    "--read-registers without --live: composing only; NO transport opened, NO command " +
                    "transmitted, NO registers read.");
                return;
            }

            // Log each freshly-parsed SLAVEINFO frame to stderr for diagnosis. The inner comm
            // client also re-publishes cached amplifier data on a 2 Hz timer without advancing
            // ProtocolReadbackSequence, so de-duplicate on that sequence to log only new frames.
            var lastSequenceBySlave = new Dictionary<int, long>();
            void LogSlaveinfoFrame(object? sender, AmplifierDataEventArgs e)
            {
                TrackAmplifierItem item = e.Amplifier;
                int slave = item.SlaveNumber;
                long sequence = item.ProtocolReadbackSequence;
                if (lastSequenceBySlave.TryGetValue(slave, out long last) && last == sequence)
                {
                    return; // 2 Hz re-publish of an already-logged frame.
                }

                lastSequenceBySlave[slave] = sequence;
                Console.Error.WriteLine(
                    $"[SLAVEINFO] slave={slave} detected={item.SlaveDetected} " +
                    $"HR7=0x{item.HoldingReg[TrackAmplifierRegisters.Identity]:X4} " +
                    $"HR11=0x{item.HoldingReg[TrackAmplifierRegisters.SwChecksum]:X4}");
            }

            host.SessionCommandGuard.AmplifierDataReceived += LogSlaveinfoFrame;
            try
            {
                WaitForRegisterReadback(host.Variables, waitMs);
            }
            finally
            {
                host.SessionCommandGuard.AmplifierDataReceived -= LogSlaveinfoFrame;
            }

            var jsonOptions = new JsonSerializerOptions { WriteIndented = false };

            int emitted = 0;
            foreach (int slave in ReadRegisterSlaves)
            {
                TrackAmplifierItem item = host.Variables.trackAmpItems[slave];
                if (item.SlaveDetected != 1)
                {
                    continue;
                }

                Console.WriteLine(JsonSerializer.Serialize(BuildReadRegisterLine(slave, item), jsonOptions));
                emitted++;
            }

            if (emitted == 0)
            {
                long slaveInfoFramesObserved = host.Variables.trackAmpItems.Sum(item => item.ProtocolReadbackSequence);
                int[] detectedSlaveNumbers = host.Variables.trackAmpItems
                    .Where(item => item.SlaveDetected == 1)
                    .Select(item => (int)item.SlaveNumber)
                    .OrderBy(slave => slave)
                    .ToArray();

                Console.WriteLine(JsonSerializer.Serialize(new
                {
                    type = "NO_AMPLIFIERS_DETECTED",
                    slaveInfoFramesObserved,
                    detectedSlaveNumbers,
                    waitMs
                }, jsonOptions));
            }
        }

        private static void WaitForRegisterReadback(TrackApplicationVariables variables, int waitMs)
        {
            int elapsed = 0;
            while (elapsed < waitMs)
            {
                bool allPopulated = ReadRegisterSlaves.All(slave =>
                {
                    TrackAmplifierItem item = variables.trackAmpItems[slave];
                    return item.SlaveDetected == 1 && item.ProtocolReadbackObserved;
                });

                if (allPopulated)
                {
                    return;
                }

                Thread.Sleep(ReadRegisterPollIntervalMs);
                elapsed += ReadRegisterPollIntervalMs;
            }
        }

        private static object BuildReadRegisterLine(int slave, TrackAmplifierItem item)
        {
            ushort identity = item.HoldingReg[TrackAmplifierRegisters.Identity];
            ushort swChecksum = item.HoldingReg[TrackAmplifierRegisters.SwChecksum];
            ushort fwVersionMajorMinor = item.HoldingReg[TrackAmplifierRegisters.FwVersionMajorMinor];
            ushort fwVersionPatchProtocol = item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol];

            // A legacy amplifier's HR21/HR22 read 0: never decode 0 as a version. Only when both
            // registers are non-zero do we decode the firmware version; otherwise mark UNSUPPORTED.
            bool versionSupported = fwVersionMajorMinor != 0 && fwVersionPatchProtocol != 0;
            string? fwVersion = versionSupported
                ? $"{TrackAmplifierRegisters.FwMajor(fwVersionMajorMinor)}" +
                  $".{TrackAmplifierRegisters.FwMinor(fwVersionMajorMinor)}" +
                  $".{TrackAmplifierRegisters.FwPatch(fwVersionPatchProtocol)}"
                : null;
            int? fwProtocolVersion = versionSupported
                ? TrackAmplifierRegisters.FwProtocolVersion(fwVersionPatchProtocol)
                : (int?)null;

            return new
            {
                type = "amplifierIdentityReadback",
                slaveNumber = slave,
                slaveDetected = item.SlaveDetected,
                identity,
                identityHex = "0x" + identity.ToString("X4"),
                swChecksum,
                swChecksumHex = "0x" + swChecksum.ToString("X4"),
                fwVersionMajorMinor,
                fwVersionMajorMinorHex = "0x" + fwVersionMajorMinor.ToString("X4"),
                fwVersionPatchProtocol,
                fwVersionPatchProtocolHex = "0x" + fwVersionPatchProtocol.ToString("X4"),
                fwVersion,
                fwProtocolVersion,
                versionStatus = versionSupported ? "SUPPORTED" : "UNSUPPORTED",
                protocolReadbackObserved = item.ProtocolReadbackObserved,
                lastDataReceivedUtc = item.LastDataReceivedUtc
            };
        }

        // ---------------------------------------------------------------- sample flattening

        private static KoploperValidationSample ToSample(
            KoploperReservationObservation observation,
            KoploperValidationResult result,
            int? locId,
            int? occupiedBlock,
            IReadOnlyList<int> reservedBlocks)
        {
            return new KoploperValidationSample(
                observation.ObservedAtUtc,
                observation.Generation?.ProcessId ?? 0,
                observation.Generation,
                observation.SourceSequence,
                observation.ObserverSequence,
                observation.SourceHealth,
                observation.IsAuthoritative,
                observation.AuthorityReason,
                locId,
                occupiedBlock,
                reservedBlocks,
                Port5700CurrentBlock: null,
                Port5700Loc: null,
                CrossCheckSource: null,
                GuiMarker: null,
                ScenarioId: "validate-live",
                result,
                Note: null);
        }

        // ---------------------------------------------------------------- usage

        private static int Help()
        {
            PrintUsage();
            return 0;
        }

        private static int Unknown(string command)
        {
            Console.Error.WriteLine($"Unknown command: {command}");
            PrintUsage();
            return 2;
        }

        private static void PrintUsage()
        {
            Console.WriteLine(
                "SiebwaldeApp.Koploper.Validation.Host — KIS-06 simulator cross-validation harness\n" +
                "Usage:\n" +
                "  validate [--cycles N] [--interval-ms N] [--trace PATH]\n" +
                "      Run the live read-only reservation observer loop, write KoploperValidationSample\n" +
                "      JSONL, and print a summary.\n" +
                "  replay <golden.jsonl>\n" +
                "      Feed golden KoploperGoldenComparison JSONL through KoploperComparisonEngine\n" +
                "      deterministically and verify each expected result.\n" +
                "  identify\n" +
                "      Read one coherent read-only Koploper snapshot and dump the raw per-block identity\n" +
                "      table (InternalBlockId vs DisplayBlockNumber) plus snapshot metadata.\n" +
                "  readonly-validate [--live] [--warm] [--read-registers] [--wait-ms N] [--cycles N] [--interval-ms N] [--trace PATH]\n" +
                "      Compose the bounded telemetry-enablement reconciliation host and run a bounded\n" +
                "      reconciliation loop writing machine-readable JSONL. Without --live nothing is\n" +
                "      transmitted. With --live it sends the six-command cold sequence\n" +
                "      (250→100→101→104→106→105) through the guard; with --warm it sends only 250\n" +
                "      (warm reconnect). 108/107 and any other command remain unreachable.\n" +
                "      With --read-registers (requires --live) it instead waits a bounded time for the\n" +
                "      SLAVEINFO receive loop, then prints one machine-readable JSON line per detected\n" +
                "      amplifier (slaves 1,3,4,6) with raw HR7/HR11/HR21/HR22 (decimal + 0x hex), the\n" +
                "      decoded firmware version (UNSUPPORTED when HR21/HR22 read 0), and freshness\n" +
                "      markers (protocolReadbackObserved + lastDataReceivedUtc). No new command is sent.\n");
        }
    }
}
