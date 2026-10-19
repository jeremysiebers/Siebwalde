using System;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Comm;

namespace FlashOneAmplifier
{
    /// <summary>
    /// Standalone CLI that flashes exactly ONE track amplifier through the existing PIC32
    /// firmware-flash wire path. It reuses <see cref="TrackCommClientAsync"/>,
    /// <see cref="RawUdpTrackTransport"/>, <see cref="TrackAmplifierBootloaderHelpers"/> and
    /// <see cref="SendNextFwDataPacket"/>.
    ///
    /// It never sends EXEC_FW_STATE_FLASH_ALL_SLAVES (122). The single amplifier address is
    /// carried as the data byte of EXEC_FW_STATE_FLASH_ONE_SLAVE (131) or, with
    /// <c>--bootloader-only</c>, EXEC_FW_STATE_FLASH_ONE_SLAVE_BOOTLOADER_ONLY (132), which
    /// recovers an amplifier already stuck in 8-bit bootloader mode (no HR11=0 entry write).
    /// </summary>
    internal static class Program
    {
        private const string DefaultIpAddress = "192.168.1.193";
        private const int DefaultSendPort = 10000;
        private const int DefaultReceivePort = 10001;

        /// <summary>Known physical track-amplifier inventory to inspect (see docs/handoff.md).</summary>
        private static readonly byte[] ReadTargetAmplifiers = { 1, 3, 4, 6 };

        /// <summary>SW checksum (HR11) of the legacy Baseline-0 amplifier firmware.</summary>
        private const ushort Baseline0Checksum = 0x251F;

        /// <summary>SW checksum (HR11) of the flashed 1.0.0 amplifier firmware (identity/version registers present).</summary>
        private const ushort Firmware100Checksum = 0xE906;

        /// <summary>Re-issue interval for the evidence-based register read wait.</summary>
        private static readonly TimeSpan ReadRetryInterval = TimeSpan.FromMilliseconds(500);

        private static async Task<int> Main(string[] args)
        {
            if (!TryParseArgs(args, out var options, out var error))
            {
                if (error != "help")
                    Console.Error.WriteLine($"Error: {error}");
                PrintUsage();
                return error == "help" ? 0 : 2;
            }

            // Compose the real UDP transport + comm client (shared by read and flash modes).
            var variables = new TrackApplicationVariables();
            var transport = new RawUdpTrackTransport(
                new RawUdpTransport(options.IpAddress, options.SendPort, options.ReceivePort));
            await using var commClient = new TrackCommClientAsync(transport, variables);

            await commClient.StartAsync(realHardwareMode: true).ConfigureAwait(false);

            try
            {
                if (options.ReadHr)
                {
                    return await ReadHoldingRegistersAsync(commClient, variables).ConfigureAwait(false);
                }

                // 1. Offline validation (SHA-256 + embedded checksum) BEFORE any write is attempted.
                var validation = FirmwareArtifactVerifier.Validate(options.HexPath, options.Sha256Hex);
                if (!validation.Success)
                {
                    Console.Error.WriteLine($"Refusing to flash (no write performed): {validation.Error}");
                    return 2;
                }

                var artifact = validation.Artifact!;
                Console.WriteLine($"Firmware SHA-256            : {artifact.Sha256Hex}");
                Console.WriteLine($"Firmware checksum           : embedded 0x{artifact.EmbeddedChecksum:X4}, recomputed 0x{artifact.RecomputedChecksum:X4}");

                var sendNextFwDataPacket = new SendNextFwDataPacket(commClient, artifact.Helpers);
                var command = new FlashOneAmplifierCommand(
                    commClient, variables, artifact.Helpers, sendNextFwDataPacket,
                    onProgress: DescribeStep);

                var mode = options.BootloaderOnly ? "bootloader-only (132)" : "standard (131)";
                Console.WriteLine($"Flashing amplifier {options.Amplifier} via {mode}.");

                var result = options.BootloaderOnly
                    ? await command.ExecuteBootloaderOnlyAsync((byte)options.Amplifier).ConfigureAwait(false)
                    : await command.ExecuteAsync((byte)options.Amplifier).ConfigureAwait(false);

                if (result.Success)
                {
                    Console.WriteLine($"Amplifier {result.TargetAmplifier} flashed successfully (DONE).");
                    return 0;
                }

                if (result.FailedSubStep is int step)
                {
                    Console.Error.WriteLine(
                        $"Flash FAILED for amplifier {result.TargetAmplifier} at sub-step {step} " +
                        $"({FlashSequencerSubStep.Describe(step)}).");
                }
                Console.Error.WriteLine($"Flash FAILED for amplifier {result.TargetAmplifier}: {result.Error}");
                return 1;
            }
            finally
            {
                await commClient.StopAsync().ConfigureAwait(false);
            }
        }

        private static void DescribeStep(ReceivedMessage m)
        {
            var state = m.Taskstate == TaskStates.DONE ? "DONE" :
                        m.Taskstate == TaskStates.ERROR ? "ERROR" : $"state={m.Taskstate}";
            Console.WriteLine($"[PIC32] step sender={m.TaskId} cmd={m.Taskcommand} {state} msg={m.Taskmessage}");
        }

        /// <summary>
        /// Read-only MBUS bring-up + holding-register inspection for the known amplifier inventory.
        /// Brings the master into data-upload mode using the production command sequence, then
        /// verifies neutral HR0 output and prints HR0/HR7/HR11/HR21/HR22. No flash, no firmware
        /// writes, and no HR0 (PWM/motion setpoint) write is ever issued.
        /// </summary>
        private static async Task<int> ReadHoldingRegistersAsync(
            TrackCommClientAsync commClient, TrackApplicationVariables variables)
        {
            Console.WriteLine("Read-only amplifier register inspection (bring-up + neutral check + readback).");

            void OnControl(object? s, ControlMessageEventArgs e)
                => Console.WriteLine($"[CTRL] sender={e.Message.TaskId} cmd={e.Message.Taskcommand} state={e.Message.Taskstate} msg={e.Message.Taskmessage}");

            commClient.ControlMessageReceived += OnControl;
            try
            {
                // 1. CONNECT handshake (250). The PIC32 controller requires this before it will
                // accept any other command; without it, it disconnects the client.
                var connected = await SendAndWaitControlAsync(
                    commClient,
                    new SendMessage(EnumClientCommands.CLIENT_CONNECTION_REQUEST, new byte[80]),
                    m => m.TaskId == TaskId.CONTROLLER &&
                         m.Taskcommand == TaskStates.CONNECTED &&
                         m.Taskstate == TaskStates.DONE,
                    TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                if (connected is null)
                {
                    Console.Error.WriteLine("CONNECT handshake timed out: no CONNECTED acknowledgement from the PIC32.");
                    return 1;
                }

                Console.WriteLine("CONNECT: CONNECTED.");

                // 2. Read-only controller version probe (113).
                await commClient.SendAsync(
                    new SendMessage(TrackCommand.GET_CONTROLLER_VERSION, Array.Empty<byte>()))
                    .ConfigureAwait(false);

                // 3. Standard MBUS bring-up (production command sequence; no flash path).
                if (!await BringUpMbusAsync(commClient).ConfigureAwait(false))
                {
                    Console.Error.WriteLine("MBUS bring-up did not complete; aborting before any register read.");
                    return 1;
                }

                // 4. Verify NEUTRAL output (HR0) BEFORE reading the full register table.
                var hr0Pending = await SendReadsAndWaitForFreshFramesAsync(
                    commClient, variables, ReadTargetAmplifiers,
                    new byte[] { (byte)TrackAmplifierRegisters.PwmCommand },
                    TimeSpan.FromSeconds(15), requireIdentityVersion: false).ConfigureAwait(false);

                if (hr0Pending.Length > 0)
                {
                    Console.Error.WriteLine($"No fresh SLAVEINFO readback for amplifiers: {string.Join(", ", hr0Pending)}.");
                    return 1;
                }

                bool anyNonNeutral = false;
                Console.WriteLine("Neutral verification (HR0):");
                foreach (byte amp in ReadTargetAmplifiers)
                {
                    var item = variables.trackAmpItems[amp];
                    ushort hr0 = item.HoldingReg[TrackAmplifierRegisters.PwmCommand];
                    bool detected = item.SlaveDetected != 0;
                    bool neutral = detected && IsNeutralHr0(hr0);
                    if (detected && !IsNeutralHr0(hr0))
                        anyNonNeutral = true;
                    Console.WriteLine(
                        $"  amp {amp}: detected={item.SlaveDetected} HR0=0x{hr0:X4} ({DescribeHr0(hr0)}) " +
                        $"{(neutral ? "NEUTRAL" : detected ? "NON-NEUTRAL" : "not-detected")}");
                }

                if (anyNonNeutral)
                {
                    Console.Error.WriteLine(
                        "STOPPING: one or more detected amplifiers reported a NON-NEUTRAL HR0 " +
                        "(no write performed; raw values above).");
                    return 1;
                }

                // 5. Full register table for the target amplifiers.
                byte[] registerTable =
                {
                    (byte)TrackAmplifierRegisters.PwmCommand,             // HR0
                    (byte)TrackAmplifierRegisters.Identity,               // HR7
                    (byte)TrackAmplifierRegisters.SwChecksum,             // HR11
                    (byte)TrackAmplifierRegisters.FwVersionMajorMinor,    // HR21
                    (byte)TrackAmplifierRegisters.FwVersionPatchProtocol, // HR22
                };

                // Diagnostic: capture per-amp readback sequence BEFORE issuing the reads, so we can
                // report whether the final register values came from a fresh (sequence-advanced)
                // SLAVEINFO frame or a stale one.
                var seqBefore = ReadTargetAmplifiers.ToDictionary(
                    a => a, a => variables.trackAmpItems[a].ProtocolReadbackSequence);

                var pending = await SendReadsAndWaitForFreshFramesAsync(
                    commClient, variables, ReadTargetAmplifiers, registerTable,
                    TimeSpan.FromSeconds(15), requireIdentityVersion: true).ConfigureAwait(false);

                foreach (byte amp in ReadTargetAmplifiers)
                {
                    var item = variables.trackAmpItems[amp];
                    ushort[] r = item.HoldingReg;
                    ushort hr0 = r[TrackAmplifierRegisters.PwmCommand];
                    ushort hr7 = r[TrackAmplifierRegisters.Identity];
                    ushort hr11 = r[TrackAmplifierRegisters.SwChecksum];
                    ushort hr21 = r[TrackAmplifierRegisters.FwVersionMajorMinor];
                    ushort hr22 = r[TrackAmplifierRegisters.FwVersionPatchProtocol];

                    long seqAfter = item.ProtocolReadbackSequence;
                    bool fresh = seqAfter > seqBefore[amp];

                    Console.WriteLine($"Amplifier {amp}:");
                    Console.WriteLine($"  SlaveDetected       : {item.SlaveDetected}");
                    Console.WriteLine($"  HR0  PWM/enable     : 0x{hr0:X4}  ({DescribeHr0(hr0)})");
                    Console.WriteLine($"  HR7  identity       : 0x{hr7:X4}");
                    Console.WriteLine($"  HR11 SW checksum    : 0x{hr11:X4}  (Baseline-0=0x251F, 1.0.0=0xE906)");
                    Console.WriteLine($"  HR21 MAJOR.MINOR    : 0x{hr21:X4}  (MAJOR={TrackAmplifierRegisters.FwMajor(hr21)}, MINOR={TrackAmplifierRegisters.FwMinor(hr21)})");
                    Console.WriteLine($"  HR22 PATCH.PROTOCOL : 0x{hr22:X4}  (PATCH={TrackAmplifierRegisters.FwPatch(hr22)}, PROTOCOL={TrackAmplifierRegisters.FwProtocolVersion(hr22)})");
                    Console.WriteLine($"  MbCommError         : {item.MbCommError} ({DescribeMbCommError(item.MbCommError)})");
                    Console.WriteLine($"  MbExceptionCode     : {item.MbExceptionCode}");
                    Console.WriteLine($"  SpiCommErrorCounter : {item.SpiCommErrorCounter}");
                    Console.WriteLine($"  [DIAG] seqBefore={seqBefore[amp]} seqAfter={seqAfter} delta={seqAfter - seqBefore[amp]} fresh={fresh} observed={item.ProtocolReadbackObserved} lastUtc={item.LastDataReceivedUtc:O}");
                }

                if (pending.Length > 0)
                {
                    Console.Error.WriteLine($"No fresh SLAVEINFO readback for amplifiers: {string.Join(", ", pending)}.");
                    return 1;
                }

                return 0;
            }
            finally
            {
                commClient.ControlMessageReceived -= OnControl;
            }
        }

        /// <summary>
        /// Mirrors the production MBUS bring-up (TrackApplicationRuntimeHost step order:
        /// ResetAllSlaves -> DataUpload -> DetectSlaves -> InitTrackamplifiers ->
        /// SetDefaultPwmSetpoints -> EnableTrackamplifiers), minus the flash/recovery and in-memory
        /// setpoint steps: SLAVES_ON -> START_DATA_UPLOAD -> DETECT -> INIT -> ENABLE.
        /// START_DATA_UPLOAD (106) is sent BEFORE DETECT/INIT/ENABLE so the MBUS comm-cycle timer
        /// is started (mbus.c MBUS_STATE_START_DATA_UPLOAD -> DRV_TMR_Start); ENABLE (105) is sent
        /// LAST because MBUS_STATE_SLAVE_ENABLE transitions the MBUS to MBUS_STATE_SERVICE_TASKS
        /// (mbus.c:347), where the cyclic poll SendNextMessage() reads HR7/HR21/HR22 (SIP2/SIP9/
        /// SIP10). Each command uses the production 80-byte payload and waits for the matching MBUS
        /// DONE acknowledgement.
        /// </summary>
        private static async Task<bool> BringUpMbusAsync(TrackCommClientAsync commClient)
        {
            (string Name, byte Command, byte AckCommand, TimeSpan Timeout)[] steps =
            {
                ("SLAVES_ON",         TrackCommand.EXEC_MBUS_STATE_SLAVES_ON,        EnumMbusStatus.MBUS_STATE_SLAVES_ON,        TimeSpan.FromSeconds(20)),
                ("START_DATA_UPLOAD", TrackCommand.EXEC_MBUS_STATE_START_DATA_UPLOAD, EnumMbusStatus.MBUS_STATE_START_DATA_UPLOAD, TimeSpan.FromSeconds(20)),
                ("DETECT",            TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT,      EnumMbusStatus.MBUS_STATE_SLAVE_DETECT,      TimeSpan.FromSeconds(30)),
                ("INIT",              TrackCommand.EXEC_MBUS_STATE_SLAVE_INIT,        EnumMbusStatus.MBUS_STATE_SLAVE_INIT,        TimeSpan.FromSeconds(90)),
                ("ENABLE",            TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE,      EnumMbusStatus.MBUS_STATE_SLAVE_ENABLE,      TimeSpan.FromSeconds(90)),
            };

            foreach (var step in steps)
            {
                Console.WriteLine($"Bring-up: sending {step.Name} (command {step.Command}).");
                var ack = await SendAndWaitControlAsync(
                    commClient,
                    new SendMessage(step.Command, new byte[80]),
                    m => m.TaskId == TaskId.MBUS &&
                         m.Taskcommand == step.AckCommand &&
                         m.Taskstate == TaskStates.DONE,
                    step.Timeout).ConfigureAwait(false);

                if (ack is null)
                {
                    Console.Error.WriteLine(
                        $"Bring-up FAILED: {step.Name} (command {step.Command}) not acknowledged " +
                        $"within {step.Timeout.TotalSeconds:0}s.");
                    return false;
                }

                Console.WriteLine($"Bring-up: {step.Name} -> DONE.");

                // Mirror DetectSlavesStep's 2s post-detect settle for slave data readiness.
                if (step.Command == TrackCommand.EXEC_MBUS_STATE_SLAVE_DETECT)
                    await Task.Delay(2000).ConfigureAwait(false);

                // ENABLE (last) leaves the MBUS in MBUS_STATE_SERVICE_TASKS with the comm-cycle
                // timer already running (START_DATA_UPLOAD sent earlier). A short settle lets the
                // cyclic poll (SIP2/SIP9/SIP10) begin filling HR7/HR21/HR22 before the read path
                // issues its 0x55 reads. The read path itself applies an evidence-based bounded
                // retry (see SendReadsAndWaitForFreshFramesAsync), so no long blind sleep is needed
                // here. C#-only orchestration wait; no firmware change.
                if (step.Command == TrackCommand.EXEC_MBUS_STATE_SLAVE_ENABLE)
                    await Task.Delay(1000).ConfigureAwait(false);
            }

            return true;
        }

        /// <summary>
        /// Sends READ (0x55) exchanges for <paramref name="registers"/> on each
        /// <paramref name="amplifiers"/>, then waits until every amplifier satisfies an
        /// evidence-based completion condition. An amplifier is complete when a fresh SLAVEINFO
        /// readback arrived (<see cref="TrackAmplifierItem.ProtocolReadbackSequence"/> advanced past
        /// the value captured before the reads were issued) AND, when
        /// <paramref name="requireIdentityVersion"/> is set, a 1.0.0 amplifier (HR11 == 0xE906) has
        /// its identity/version registers HR7/HR21/HR22 populated (non-zero). Baseline-0 amplifiers
        /// (HR11 == 0x251F) legitimately leave those registers at 0, so they are treated as "not
        /// applicable" and are never required to be non-zero.
        ///
        /// Reads are re-issued every <see cref="ReadRetryInterval"/> for amplifiers that have not
        /// yet completed (retrying any read whose Modbus transaction previously failed or was still
        /// in flight), and the whole wait is bounded by <paramref name="timeout"/>. Returns the
        /// amplifiers that never satisfied the completion condition.
        /// </summary>
        private static async Task<byte[]> SendReadsAndWaitForFreshFramesAsync(
            TrackCommClientAsync commClient,
            TrackApplicationVariables variables,
            byte[] amplifiers,
            byte[] registers,
            TimeSpan timeout,
            bool requireIdentityVersion)
        {
            var before = new Dictionary<byte, long>();
            foreach (byte a in amplifiers)
                before[a] = variables.trackAmpItems[a].ProtocolReadbackSequence;

            var deadline = DateTime.UtcNow + timeout;

            while (true)
            {
                // Re-issue the 0x55 reads for amplifiers that have not yet satisfied the completion
                // condition. This primes the master's mailbox and retries any read whose Modbus
                // transaction previously failed or was still in flight.
                foreach (byte a in amplifiers)
                {
                    if (IsReadComplete(variables.trackAmpItems[a], before[a], requireIdentityVersion))
                        continue;
                    foreach (byte r in registers)
                        await commClient.SendAsync(
                            AmplifierConfigurationDownloader.Exchange(a, r, 0, read: true))
                            .ConfigureAwait(false);
                }

                if (amplifiers.All(a => IsReadComplete(variables.trackAmpItems[a], before[a], requireIdentityVersion)))
                    return Array.Empty<byte>();

                if (DateTime.UtcNow >= deadline)
                    break;

                await Task.Delay(ReadRetryInterval).ConfigureAwait(false);
            }

            return amplifiers
                .Where(a => !IsReadComplete(variables.trackAmpItems[a], before[a], requireIdentityVersion))
                .ToArray();
        }

        /// <summary>
        /// True when <paramref name="item"/> satisfies the evidence-based completion condition: a
        /// fresh SLAVEINFO frame has arrived since <paramref name="beforeSequence"/>, and, when
        /// <paramref name="requireIdentityVersion"/> is set and the amplifier is 1.0.0 firmware
        /// (HR11 == <see cref="Firmware100Checksum"/>), its HR7/HR21/HR22 are populated (non-zero).
        /// Baseline-0 (or unknown) amplifiers are never required to have non-zero identity/version.
        /// </summary>
        private static bool IsReadComplete(TrackAmplifierItem item, long beforeSequence, bool requireIdentityVersion)
        {
            // A fresh frame must have arrived since we captured the baseline sequence.
            if (item.ProtocolReadbackSequence <= beforeSequence)
                return false;

            if (!requireIdentityVersion)
                return true;

            // Only 1.0.0 firmware exposes the identity/version registers; Baseline-0 leaves them 0.
            if (!RequiresPopulatedIdentityVersion(item.HoldingReg[TrackAmplifierRegisters.SwChecksum]))
                return true;

            return item.HoldingReg[TrackAmplifierRegisters.Identity] != 0 &&
                   item.HoldingReg[TrackAmplifierRegisters.FwVersionMajorMinor] != 0 &&
                   item.HoldingReg[TrackAmplifierRegisters.FwVersionPatchProtocol] != 0;
        }

        /// <summary>True when a 1.0.0 amplifier (HR11 == 0xE906) must report non-zero identity/version registers.</summary>
        private static bool RequiresPopulatedIdentityVersion(ushort hr11)
            => hr11 == Firmware100Checksum;

        /// <summary>Human-readable decode of the HR0 command word.</summary>
        private static string DescribeHr0(ushort hr0)
        {
            int pwm = hr0 & TrackAmplifierRegisters.PwmMask;
            bool dir = (hr0 & TrackAmplifierRegisters.DirectionBit) != 0;
            bool brake = (hr0 & TrackAmplifierRegisters.BrakeBit) != 0;
            bool stop = (hr0 & TrackAmplifierRegisters.StopRequestBit) != 0;
            bool emo = (hr0 & TrackAmplifierRegisters.EmergencyStopBit) != 0;
            return $"PWM={pwm} dir={dir} brake={brake} stop={stop} emo={emo}";
        }

        /// <summary>Human-readable decode of the master's per-slave Modbus comm status (SLAVE_DATA).</summary>
        private static string DescribeMbCommError(uint code) => code switch
        {
            0x00 => "IDLE",
            0x01 => "BUSY",
            0x02 => "OK",
            0x03 => "NOK",
            0x04 => "TIMEOUT",
            0x05 => "EXCEPTION",
            0x06 => "UNKNOWN",
            _ => $"0x{code:X}",
        };

        /// <summary>
        /// True when HR0 carries no motion command: no direction/brake/stop/emergency bits, and a
        /// PWM field of 0 (Baseline-0 "duty 0") or 399 (<see cref="AmplifierSpeedMapper.NeutralPwm"/>,
        /// the Phase-1 dual-sided neutral).
        /// </summary>
        private static bool IsNeutralHr0(ushort hr0)
        {
            int pwm = hr0 & TrackAmplifierRegisters.PwmMask;
            bool noMotionBits = (hr0 & (TrackAmplifierRegisters.DirectionBit
                                       | TrackAmplifierRegisters.BrakeBit
                                       | TrackAmplifierRegisters.StopRequestBit
                                       | TrackAmplifierRegisters.EmergencyStopBit)) == 0;
            return noMotionBits && (pwm == 0 || pwm == AmplifierSpeedMapper.NeutralPwm);
        }

        /// <summary>
        /// Subscribes to control messages, sends <paramref name="message"/>, and waits for the
        /// first message matching <paramref name="predicate"/> (or null on timeout). Subscribing
        /// before sending means a reply that races ahead of the send is still captured.
        /// </summary>
        private static async Task<ReceivedMessage?> SendAndWaitControlAsync(
            TrackCommClientAsync commClient,
            SendMessage message,
            Func<ReceivedMessage, bool> predicate,
            TimeSpan timeout)
        {
            var wait = WaitForControlAsync(commClient, predicate, timeout);
            await commClient.SendAsync(message).ConfigureAwait(false);
            return await wait.ConfigureAwait(false);
        }

        private static async Task<ReceivedMessage?> WaitForControlAsync(
            TrackCommClientAsync commClient,
            Func<ReceivedMessage, bool> predicate,
            TimeSpan timeout)
        {
            var tcs = new TaskCompletionSource<ReceivedMessage>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            void Handler(object? s, ControlMessageEventArgs e)
            {
                if (predicate(e.Message))
                    tcs.TrySetResult(e.Message);
            }

            commClient.ControlMessageReceived += Handler;
            try
            {
                var winner = await Task.WhenAny(tcs.Task, Task.Delay(timeout)).ConfigureAwait(false);
                return winner == tcs.Task ? await tcs.Task.ConfigureAwait(false) : null;
            }
            finally
            {
                commClient.ControlMessageReceived -= Handler;
            }
        }

        private static bool TryParseArgs(string[] args, out CliOptions options, out string error)
        {
            options = new CliOptions();
            error = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--amp":
                        if (!TryReadValue(args, ref i, out var ampText) || !int.TryParse(ampText, out var amp))
                        {
                            error = "Missing or invalid value for --amp (expected 1..50).";
                            return false;
                        }
                        options.Amplifier = amp;
                        break;

                    case "--hex":
                        if (!TryReadValue(args, ref i, out var hex))
                        {
                            error = "Missing value for --hex (expected a path to an Intel HEX file).";
                            return false;
                        }
                        options.HexPath = hex;
                        break;

                    case "--sha256":
                        if (!TryReadValue(args, ref i, out var sha256))
                        {
                            error = "Missing value for --sha256 (expected a 64-char hex digest).";
                            return false;
                        }
                        options.Sha256Hex = sha256;
                        break;

                    case "--bootloader-only":
                        options.BootloaderOnly = true;
                        break;

                    case "--read-hr":
                        options.ReadHr = true;
                        break;

                    case "--ip":
                        if (!TryReadValue(args, ref i, out var ip))
                        {
                            error = "Missing value for --ip.";
                            return false;
                        }
                        options.IpAddress = ip;
                        break;

                    case "--send-port":
                        if (!TryReadValue(args, ref i, out var sendPort) || !int.TryParse(sendPort, out var sp))
                        {
                            error = "Missing or invalid value for --send-port.";
                            return false;
                        }
                        options.SendPort = sp;
                        break;

                    case "--recv-port":
                        if (!TryReadValue(args, ref i, out var recvPort) || !int.TryParse(recvPort, out var rp))
                        {
                            error = "Missing or invalid value for --recv-port.";
                            return false;
                        }
                        options.ReceivePort = rp;
                        break;

                    case "-h":
                    case "--help":
                        error = "help";
                        return false;

                    default:
                        error = $"Unknown argument: {args[i]}";
                        return false;
                }
            }

            if (options.ReadHr)
            {
                // Read-only inspection mode: no --amp/--hex/--sha256 required and no flash path.
                if (options.BootloaderOnly)
                {
                    error = "--read-hr cannot be combined with --bootloader-only.";
                    return false;
                }
                return true;
            }

            if (options.Amplifier < TrackAmplifierAddress.MinTrackAmplifier ||
                options.Amplifier > TrackAmplifierAddress.MaxTrackAmplifier)
            {
                error = $"--amp must be in range {TrackAmplifierAddress.MinTrackAmplifier}..{TrackAmplifierAddress.MaxTrackAmplifier}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.HexPath))
            {
                error = "--hex <path> is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(options.Sha256Hex))
            {
                error = "--sha256 <expected> is required (exact SHA-256 identity before any write).";
                return false;
            }

            return true;
        }

        private static bool TryReadValue(string[] args, ref int i, out string value)
        {
            value = string.Empty;
            if (i + 1 >= args.Length)
                return false;
            value = args[++i];
            return true;
        }

        private static void PrintUsage()
        {
            Console.Error.WriteLine(
                "Usage: FlashOneAmplifier --amp <1..50> --hex <path> --sha256 <expected> " +
                "[--bootloader-only] [--ip <ip>] [--send-port <port>] [--recv-port <port>]");
            Console.Error.WriteLine(
                "   or: FlashOneAmplifier --read-hr " +
                "[--ip <ip>] [--send-port <port>] [--recv-port <port>]   " +
                "(read-only MBUS bring-up + neutral check + register inspection of amps 1,3,4,6)");
            Console.Error.WriteLine($"Defaults: --ip {DefaultIpAddress} --send-port {DefaultSendPort} --recv-port {DefaultReceivePort}");
        }

        private sealed class CliOptions
        {
            public int Amplifier { get; set; }
            public string HexPath { get; set; } = string.Empty;
            public string Sha256Hex { get; set; } = string.Empty;
            public bool BootloaderOnly { get; set; }
            public bool ReadHr { get; set; }
            public string IpAddress { get; set; } = DefaultIpAddress;
            public int SendPort { get; set; } = DefaultSendPort;
            public int ReceivePort { get; set; } = DefaultReceivePort;
        }
    }
}
