using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.TrackApplication.Comm;
using SiebwaldeApp.EcosEmu;
using SiebwaldeApp.Integration;

namespace SiebwaldeApp.StopReachabilityHarness
{
    /// <summary>
    /// Controlled stop-reachability test harness (software only).
    ///
    /// It reproduces the source-confirmed stop-reachability gap without changing any production
    /// behaviour. The following production components are used unchanged:
    ///
    ///   TrackApplicationVariables, TrackControlMain (10 Hz pending-write/runtime loop),
    ///   TrackAmplifierHardwareBackend, SimpleEcosBackend (incl. DCC28 normalization),
    ///   ControlSafetyInterlockBackend, ControlSafetyGuard, EcosHardwareStopSink,
    ///   ControlDiagnostics, SwitchController + SwitchTranslatingHardwareBackend,
    ///   LookAheadPlanner, TrackAmplifierOccupancyProvider, DivergenceChecker,
    ///   BlockTopology / KoploperBlockMap / SwitchMapping from CoreConfiguration,
    ///   AmplifierSpeedMapper and ProtocolSpeedNormalizer.
    ///
    /// Exactly two things are substituted:
    ///   1. the logical block-location source: <see cref="ControllableBlockPositionProvider"/>
    ///      replaces KoploperExternalInfoClient, but keeps the same IBlockPositionProvider
    ///      contract and still raises BlockEntered, so SimpleEcosBackend.OnBlockEntered and
    ///      TrackAmplifierHardwareBackend run their production code;
    ///   2. the diagnostic trigger: the operator/script builds a loco-scoped StopRequired
    ///      ControlDiagnostic and hands it to the real ControlSafetyGuard.Apply(...).
    ///
    /// For the non-hardware dry run only, the final hardware output is a recording
    /// ITrackCommClient, which is explicitly permitted for call-ordering verification.
    ///
    /// No production flag, no production file, and no firmware is modified.
    /// </summary>
    internal sealed class StopReachabilityHarness : IAsyncDisposable
    {
        public const string LoggerInstance = "StopReachabilityHarness";

        private static readonly Regex WriteRegex = new(
            @"slave=(\d+),\s*HR0=0x([0-9A-Fa-f]{4})",
            RegexOptions.Compiled);

        /// <summary>Physical prototype amplifiers 1, 3, 4 and 6.</summary>
        private static readonly ushort[] PrototypeAmps = { 1, 3, 4, 6 };

        private readonly HarnessOptions _options;
        private readonly HarnessLogFactory _logFactory = new();

        private ControlTraceLogger? _controlTrace;
        private HarnessTraceCapture? _traceCapture;

        private TrackApplicationVariables _variables = new();
        private ControllableBlockPositionProvider _blockProvider = new();
        private ITrackCommClient? _commClient;
        private RecordingTrackCommClient? _recorder;
        private TrackControlIntegration? _integration;
        private SimpleEcosBackend? _ecosBackend;
        private TrackControlMain? _controlMain;
        private ControlDiagnostics? _diagnostics;
        private ControlSafetyGuard? _guard;
        private EcosHardwareStopSink? _stopSink;
        private RecordingStopSink? _recordingStopSink;
        private SwitchController? _switches;
        private DivergenceChecker? _divergence;
        private JsonLocoRepository? _locoRepository;
        private BlockTopology? _topology;
        private KoploperBlockMap? _blockMap;
        private TrackAmplifierGroups? _groups;
        private CancellationTokenSource? _cts;
        private int _writeCursor;
        private bool _composed;

        public StopReachabilityHarness(HarnessOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        // ---------------------------------------------------------------------------------
        // Composition
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Composes the production control path exactly the way TrackControlHost does for real
        /// mode, but without the ECoS TCP server and without the Koploper external-info client.
        /// </summary>
        public async Task ComposeAsync()
        {
            _logFactory.AddLogger(new HarnessConsoleLogger());
            _traceCapture = new HarnessTraceCapture();
            _logFactory.AddLogger(_traceCapture);
            IoC.ConfigureLogger(_logFactory);

            // The production control trace over the harness log factory. The harness has no file
            // logger by design, so the trace is captured in memory instead of being written to a
            // file; the production WPF app registers the same ControlTraceLogger with a FileLogger.
            _controlTrace = new ControlTraceLogger(_logFactory);
            _controlTrace.SessionStart(
                "SiebwaldeApp.StopReachabilityHarness",
                _options.DryRun ? "DryRun" : "Live",
                null);

            _variables = new TrackApplicationVariables();
            _blockProvider = new ControllableBlockPositionProvider();
            _cts = new CancellationTokenSource();

            _topology = CoreConfiguration.BuildBlockTopology();
            _blockMap = CoreConfiguration.BuildKoploperBlockMap();
            var switchMapping = CoreConfiguration.BuildSwitchMap();

            // Explicit harness grouping so the dry run can demonstrate that the operational
            // grouping is independent of the physical device class and is not inferred from the
            // address range. Amp 6 is the installed prototype and is deliberately MountainRailway
            // here to prove that a detected-but-unmapped amplifier is not silently MainRailway.
            _groups = TrackAmplifierGroups.Create(
                mainRailway: new[] { 1, 3, 4 },
                mountainRailway: new[] { 6 },
                spare: new[] { 50 });

            // The locomotive repository normally lives in the log directory. The harness keeps it
            // in a temporary folder so a run cannot overwrite the production locos.json.
            var locoRepoPath = Path.Combine(
                Path.GetTempPath(), "SiebwaldeStopReachabilityHarness", "locos.json");
            Directory.CreateDirectory(Path.GetDirectoryName(locoRepoPath)!);

            _locoRepository = new JsonLocoRepository(locoRepoPath);
            await _locoRepository.LoadAsync();
            _locoRepository.AddOrUpdate(new LocoInfo
            {
                EcosId = _options.EcosId,
                Address = _options.Address,
                Protocol = _options.Protocol,
                Name = "Harness loco",
                Block = null
            });

            _diagnostics = new ControlDiagnostics();
            _stopSink = new EcosHardwareStopSink(Log, _controlTrace);
            _recordingStopSink = new RecordingStopSink(_stopSink, Log);
            _guard = new ControlSafetyGuard(_recordingStopSink, _diagnostics, Log, _controlTrace);

            var observability = new AmplifierOccupancyObservability(_variables);

            if (_options.DryRun)
            {
                _recorder = new RecordingTrackCommClient(_variables, Log);
                _commClient = _recorder;
            }
            else
            {
                _commClient = await StartLiveTransportAndInitializeAsync();
            }

            // Real-mode switch translation. The physical switch output is not wired yet, exactly
            // as in production, so it deliberately drives nothing and says so.
            _switches = new SwitchController(
                switchMapping,
                new DelegateSwitchOutput(
                    (address, position) =>
                    {
                        Log($"Physical switch output {address} is not wired in the harness; {position} was NOT driven.");
                        return false;
                    },
                    isAvailable: false),
                Log,
                _diagnostics,
                _guard,
                new UnobservableSwitchObserver(),
                observability);

            // Same composition order and wiring as TrackControlIntegration + TrackControlHost real.
            _integration = new TrackControlIntegration(
                _commClient,
                _variables,
                _blockProvider,
                _topology,
                _blockMap,
                _locoRepository,
                feedbackSink: null,
                switchPositionProvider: () => _switches.GetLogicalPositions(),
                log: Log,
                switchController: _switches,
                safetyGuard: _guard,
                diagnostics: _diagnostics,
                trackAmplifierGroups: _groups,
                controlTrace: _controlTrace);

            _ecosBackend = _integration.EcosBackend
                ?? throw new InvalidOperationException(
                    "The integration did not create an in-process ECoS backend.");

            _divergence = new DivergenceChecker(
                _topology,
                _switches,
                _integration.OccupancyProvider,
                observability,
                _diagnostics,
                _guard,
                Log);

            _integration.RealBackend.Divergence = _divergence;

            // Critical production detail: the stop sink is bound to the real backend directly
            // (not to the interlock decorator), exactly as TrackControlHost does. The neutralizer
            // and the retained commanded-actuator tracker are bound as well, so the stop can
            // reach orphaned physical targets and escalate to amplifier-centric neutralization.
            _stopSink.Hardware = _integration.RealBackend;
            _stopSink.Neutralizer = _integration.RealBackend;
            _stopSink.CommandTracker = _integration.CommandTracker;
            _guard.RevalidationCheck = _divergence.IsResolved;

            _switches.Initialize();
            _integration.Attach();

            // Real 10 Hz runtime writer over the same comm client.
            _controlMain = new TrackControlMain(LoggerInstance, _commClient, _variables, _controlTrace);
            _controlMain.StartRuntime(_cts.Token);

            _composed = true;

            Console.WriteLine();
            Console.WriteLine("HARNESS COMPOSED.");
            Console.WriteLine($"  Topology blocks : {string.Join(", ", _topology.Blocks.OrderBy(b => b))}");
            Console.WriteLine($"  Topology routes : {string.Join(", ", _topology.Transitions.Select(t => $"{t.FromBlock}>{t.ToBlock}"))}");
            Console.WriteLine($"  Loco            : ecosId={_options.EcosId} address={_options.Address} protocol={_options.Protocol}");
            Console.WriteLine($"  From/To blocks  : {_options.FromBlock} -> {_options.TargetBlock}");
            Console.WriteLine($"  Comm client     : {_commClient.GetType().Name} ({(_options.DryRun ? "DRY-RUN recording" : "LIVE real track controller")})");
            Console.WriteLine();
        }

        /// <summary>
        /// LIVE only. Starts the real UDP transport and runs the same production initialization
        /// pipeline the WPF application runs, so amplifiers are detected/enabled before the test.
        /// </summary>
        private async Task<ITrackCommClient> StartLiveTransportAndInitializeAsync()
        {
            Console.WriteLine("LIVE mode: starting the real track-controller communication and the production initialization pipeline.");

            var rawUdp = new RawUdpTransport(
                CoreConfiguration.TrackControllerIpAddress,
                CoreConfiguration.TrackControllerSendingPort,
                CoreConfiguration.TrackControllerReceivingPort);
            ITrackTransport transport = new RawUdpTrackTransport(rawUdp);

            var commClient = new TrackCommClientAsync(transport, _variables);

            var bootloaderHelpers = new TrackAmplifierBootloaderHelpers(
                CoreConfiguration.TrackAmplifierFirmwarePath, LoggerInstance);
            var sendNext = new SendNextFwDataPacket(commClient, bootloaderHelpers);

            // Verbatim copy of the production step list (SiebwaldeApplicationModel).
            var steps = new IInitializationStep[]
            {
                new ConnectToEthernetTargetStep(commClient, _variables, LoggerInstance),
                new ResetAllSlavesStep(commClient, _variables, LoggerInstance),
                new DataUploadStep(commClient, _variables, LoggerInstance),
                new DetectSlavesStep(commClient, _variables, LoggerInstance),
                new RecoverSlavesStep(commClient, _variables, sendNext, bootloaderHelpers, LoggerInstance),
                new FlashFwTrackamplifiersStep(commClient, _variables, sendNext, bootloaderHelpers, LoggerInstance),
                new InitTrackamplifiersStep(commClient, LoggerInstance),
                new SetDefaultPwmSetpointsStep(_variables, LoggerInstance),
                new EnableTrackamplifiersStep(commClient, LoggerInstance),
            };

            var initService = new TrackAmplifierInitializationServiceAsync(
                commClient, _variables, steps, LoggerInstance);
            initService.ProgressChanged += (_, e) => Log($"Track init: {e.StepName} - {e.Message}");
            initService.StatusChanged += (_, status) => Log($"Track init status: {status}");

            await commClient.StartAsync(true, _cts!.Token);
            await initService.InitializeAsync(_cts!.Token);

            return commClient;
        }

        // ---------------------------------------------------------------------------------
        // Operator-controlled stages
        // ---------------------------------------------------------------------------------

        /// <summary>Stage 0: normal runtime baseline; amplifier state must be trusted.</summary>
        public async Task Stage0_BaselineAsync()
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine("=== STAGE 0: baseline ===");

            if (_options.DryRun)
            {
                // Dry-run only: reproduce the amplifier frames the real comm client would have
                // parsed (detected + fresh) with a neutral observed register, so the dry run has
                // the same starting state as the live baseline.
                foreach (var slave in PrototypeAmps)
                {
                    SeedAmplifier(slave);
                }

                Console.WriteLine("  (dry-run: amplifier frames simulated as detected+fresh, observed HR0 = neutral 399)");
            }

            PrintBaseline();
            await Task.CompletedTask;
        }

        /// <summary>Stage 1: inject loco -> block through the production BlockEntered path.</summary>
        public async Task Stage1_BlockAsync(int block)
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine($"=== STAGE 1: loco {_options.Address} -> block {block} (production BlockEntered path) ===");

            _blockProvider.Inject(_options.Address, block);
            Console.WriteLine($"  ControllableBlockPositionProvider.Inject({_options.Address}, {block}) -> SimpleEcosBackend.OnBlockEntered ran.");

            // Koploper's observed order when placing a loco: direction first, bundled with a stop.
            // Forward = dir[0]; the loco default direction is reverse, so this is required to
            // reproduce the documented forward PWM 416.
            await SendCommandAsync($"set({_options.EcosId},dir[0],speedstep[0])");
            await WaitForPendingWritesDrainedAsync(1, 3, 4, 6);

            var writes = TakeWrites();
            PrintBlockLine(block);
            foreach (var slave in PrototypeAmps)
            {
                PrintAmpEvidence("  BLOCK1", slave, 0, writes);
            }
        }

        /// <summary>Stage 2: lowest reliable non-zero DCC28 speed through the production ECoS path.</summary>
        public async Task Stage2_LowSpeedAsync(int block)
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine($"=== STAGE 2: low non-zero speed in block {block} ===");

            var requested = NormalizeDcc28Step(1);
            Console.WriteLine($"  set({_options.EcosId},speedstep[1]) -> protocol {_options.Protocol} normalizes to {requested} (ProtocolSpeedNormalizer).");

            await SendCommandAsync($"set({_options.EcosId},speedstep[1])");
            await WaitForPendingWritesDrainedAsync(1, 3, 4, 6);

            var writes = TakeWrites();
            PrintBlockLine(block);
            foreach (var slave in PrototypeAmps)
            {
                PrintAmpEvidence("  LOWSPEED", slave, requested, writes);
            }
        }

        /// <summary>
        /// Stage 3: A -> B logical transition. No stop is sent, exactly like production
        /// OnBlockEntered, which only updates logical state and never neutralizes the vacated amp.
        /// </summary>
        public async Task Stage3_TransitionAsync(int targetBlock)
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine($"=== STAGE 3: transition loco {_options.Address} -> block {targetBlock} (no stop is sent) ===");

            _blockProvider.Inject(_options.Address, targetBlock);
            Console.WriteLine($"  ControllableBlockPositionProvider.Inject({_options.Address}, {targetBlock}) -> SimpleEcosBackend.OnBlockEntered ran.");
            Console.WriteLine("  Production OnBlockEntered only updates logical state; it performs no amplifier neutralization.");

            // Allow the occupancy bridge to observe the (unchanged) amplifier data.
            await Task.Delay(150);

            var writes = TakeWrites();
            PrintBlockLine(targetBlock);
            foreach (var slave in PrototypeAmps)
            {
                PrintAmpEvidence("  TRANSITION", slave, 0, writes);
            }

            Console.WriteLine($"  [WRITE] records this stage: {(writes.Count == 0 ? "<none>" : string.Join(", ", writes.Select(w => $"slave={w.Slave} HR0={w.Hr0}")))}");

            var amp1 = Amp(1).HoldingReg[TrackAmplifierRegisters.PwmCommand];
            var neutral = AmplifierSpeedMapper.NeutralPwm;
            Console.WriteLine(
                $"  KEY QUESTION: does amp 1 remain non-neutral? {(amp1 != neutral ? "YES" : "NO")} " +
                $"(amp1 observed HR0={amp1}, neutral={neutral})");
        }

        /// <summary>Stage 4: same low requested speed again, now targeting the new block's amp.</summary>
        public async Task Stage4_LowSpeedAsync(int targetBlock)
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine($"=== STAGE 4: low non-zero speed again in block {targetBlock} ===");

            var requested = NormalizeDcc28Step(1);
            await SendCommandAsync($"set({_options.EcosId},speedstep[1])");
            await WaitForPendingWritesDrainedAsync(1, 3, 4, 6);

            var writes = TakeWrites();
            PrintBlockLine(targetBlock);
            foreach (var slave in PrototypeAmps)
            {
                PrintAmpEvidence("  BLOCK3", slave, requested, writes);
            }

            Console.WriteLine($"  [WRITE] records this stage: {(writes.Count == 0 ? "<none>" : string.Join(", ", writes.Select(w => $"slave={w.Slave} HR0={w.Hr0}")))}");
        }

        /// <summary>
        /// Stage 5: real loco-scoped safety stop. The diagnostic trigger is the only substituted
        /// part; the guard, stop sink, backend, runtime loop and amplifier writes are real.
        /// </summary>
        public async Task Stage5_SafetyStopAsync()
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine("=== STAGE 5: real per-loco safety stop (ControlSafetyGuard.Apply) ===");

            var diagnostic = new ControlDiagnostic
            {
                Code = DiagnosticCode.OccupancyMismatch,
                Severity = DiagnosticSeverity.StopRequired,
                Subject = $"block {_options.TargetBlock}",
                Detail = "Harness-injected loco-scoped StopRequired diagnostic (trigger substitution).",
                LocoAddress = _options.Address,
                Block = _options.TargetBlock
            };

            Console.WriteLine(
                $"  INJECT DIAGNOSTIC code={diagnostic.Code} severity={diagnostic.Severity} " +
                $"loco={diagnostic.LocoAddress} block={diagnostic.Block} key={diagnostic.Key}");

            var action = _guard!.Apply(diagnostic);
            _diagnostics!.Report(diagnostic.WithSafetyAction(action));
            Console.WriteLine($"  GUARD_ACTION={action} (real ControlSafetyGuard.Apply)");

            await WaitForPendingWritesDrainedAsync(1, 3, 4, 6);

            var writes = TakeWrites();
            var sinkResult = _recordingStopSink!.LastStopLocoResult;

            Console.WriteLine(
                $"STOP_LOCO LOCO={_options.Address} DIAG={diagnostic.Code}|{diagnostic.Subject} " +
                $"GUARD_ACTION={action} SINK_RESULT={FormatBool(sinkResult)}");
            Console.WriteLine("  SINK_RESULT is the command-level result the guard now inspects (observed physical neutralization stays a separate state).");

            PrintPendingWrites();

            foreach (var slave in PrototypeAmps)
            {
                PrintAmpEvidence("  STOP", slave, 0, writes);
            }

            Console.WriteLine($"  [WRITE] records this stage: {(writes.Count == 0 ? "<none>" : string.Join(", ", writes.Select(w => $"slave={w.Slave} HR0={w.Hr0}")))}");

            var amp1 = Amp(1).HoldingReg[TrackAmplifierRegisters.PwmCommand];
            var amp3 = Amp(3).HoldingReg[TrackAmplifierRegisters.PwmCommand];
            Console.WriteLine(
                $"  OBSERVED RESULT: AMP1={amp1} AMP3={amp3} " +
                "(defect-reproduction revision 03f5221 expected amp3 neutral and amp1 still non-neutral; " +
                "this post-fix revision expects both neutral)");
        }

        /// <summary>
        /// Recovery/fallback verification, deliberately separate from the primary defect action:
        /// the real layout stop path Koploper's set(1,stop) uses.
        /// </summary>
        public async Task LayoutStopAsync()
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine("=== RECOVERY / FALLBACK: real layout stop (set(1,stop) -> TrackAmplifierHardwareBackend.SetPower(false)) ===");

            await SendCommandAsync("set(1,stop)");
            await WaitForPendingWritesDrainedAsync(1, 2, 3, 4, 5, 6);

            var writes = TakeWrites();
            Console.WriteLine($"LAYOUT_STOP writes: {(writes.Count == 0 ? "<none>" : string.Join(", ", writes.Select(w => $"slave={w.Slave} HR0={w.Hr0}")))}");

            foreach (var slave in new ushort[] { 1, 2, 3, 4, 5, 6 })
            {
                PrintAmpEvidence("  LAYOUT", slave, 0, writes);
            }

            // The write log only shows slaves whose desired value changed. SetPower(false) itself
            // iterates every BlockTopology block, so report that structural target set too.
            var structuralTargets = _topology!.Blocks
                .OrderBy(b => b)
                .SelectMany(b => _topology.TryGetAmplifiers(b, out var amps) ? amps : Array.Empty<ushort>())
                .Distinct()
                .OrderBy(a => a)
                .ToArray();

            var amp6Mapped = _topology.Blocks.Any(
                b => _topology.TryGetAmplifiers(b, out var amps) && amps.Contains((ushort)6));

            Console.WriteLine(
                $"  SetPower(false) structurally targets every BlockTopology block -> amplifiers: [{string.Join(",", structuralTargets)}]");
            Console.WriteLine(
                $"  amp 6 present in BlockTopology? {amp6Mapped} (installed prototype amp 6 is NOT mapped, so it is not targeted)");
            Console.WriteLine(
                "  note: a slave already at the neutral value produces no pending write, so it can be targeted without appearing in [WRITE] records");
        }

        /// <summary>
        /// Physical-device-classification and backplane-safety verification (dry-run only).
        ///
        /// It seeds detected backplane/configuration slaves 51, 52 and 55 and an unmapped spare
        /// track amplifier, then proves that:
        /// - 1 and 50 classify as track amplifiers, 51/52/55 do not;
        /// - the strongest physical safety neutralization never includes 51..55 and produces no
        ///   HR0/PWM write for them;
        /// - the operational grouping is independent of the physical class and is not inferred
        ///   from the address range.
        /// </summary>
        public async Task VerifyClassificationAsync()
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine("=== CLASSIFICATION / BACKPLANE SAFETY CHECK (dry-run only) ===");

            // Seed detected backplane/configuration slaves and an unmapped spare track amplifier,
            // exactly the devices that the reviewed fallback wrongly treated as track amplifiers.
            foreach (var slave in new ushort[] { 50, 51, 52, 55 })
            {
                SeedAmplifier(slave);
            }

            Console.WriteLine("  PHYSICAL DEVICE CLASS (authoritative TrackAmplifierAddress):");
            foreach (var slave in new[] { 0, 1, 50, 51, 52, 55 })
            {
                Console.WriteLine(
                    $"    slave {slave}: IsTrackAmplifierAddress={TrackAmplifierAddress.IsTrackAmplifierAddress(slave)} " +
                    $"IsBackplaneConfigurationSlave={TrackAmplifierAddress.IsBackplaneConfigurationSlave(slave)}");
            }

            Console.WriteLine("  OPERATIONAL GROUP (independent of physical class and block mapping):");
            foreach (var slave in new[] { 1, 3, 4, 6, 50, 51 })
            {
                Console.WriteLine(
                    $"    slave {slave}: group={_integration!.RealBackend.GetOperationalGroup((ushort)slave)}");
            }

            var known = _integration!.RealBackend.GetKnownPhysicalAmplifiers();
            Console.WriteLine($"  GetKnownPhysicalAmplifiers = [{string.Join(",", known)}]");

            var before = _logFactory.WriteRecords.Count;
            var result = _stopSink!.StopLayout();

            var waitFor = known.Concat(new ushort[] { 51, 52, 55 }).ToArray();
            await WaitForPendingWritesDrainedAsync(waitFor);

            var writes = _logFactory.WriteRecords.Skip(before).ToArray();
            var backplaneWrites = writes
                .Where(w => w.Contains("slave=51", StringComparison.Ordinal) ||
                            w.Contains("slave=52", StringComparison.Ordinal) ||
                            w.Contains("slave=55", StringComparison.Ordinal))
                .ToArray();
            var backplaneInTargets = known.Where(a => a >= TrackAmplifierAddress.MinBackplaneSlave).ToArray();
            var pendingBackplane = _variables.PendingWrites.Keys
                .Where(k => k >= TrackAmplifierAddress.MinBackplaneSlave)
                .ToArray();

            Console.WriteLine(
                $"  StopLayout succeeded={result.Succeeded} commanded=[{string.Join(",", result.CommandedAmplifiers)}] failed=[{string.Join(",", result.FailedAmplifiers)}]");
            Console.WriteLine(
                $"  backplane addresses in GetKnownPhysicalAmplifiers: {(backplaneInTargets.Length == 0 ? "NONE" : string.Join(",", backplaneInTargets))}");
            Console.WriteLine(
                $"  HR0/PWM writes to 51..55: {(backplaneWrites.Length == 0 ? "NONE" : string.Join(" | ", backplaneWrites))}");
            Console.WriteLine(
                $"  pending writes to 51..55: {(pendingBackplane.Length == 0 ? "NONE" : string.Join(",", pendingBackplane))}");
            Console.WriteLine(
                $"  PendingWrites keys: [{string.Join(",", _variables.PendingWrites.Keys.OrderBy(k => k))}]");

            var pass = backplaneInTargets.Length == 0 && backplaneWrites.Length == 0 && pendingBackplane.Length == 0;
            Console.WriteLine($"  BACKPLANE SAFETY CHECK: {(pass ? "PASS" : "FAIL")}");
        }

        /// <summary>
        /// Clean strongest-emergency / layout-neutralization trigger.
        ///
        /// This command invokes the production strongest-emergency/layout neutralization
        /// (<see cref="EcosHardwareStopSink.StopLayout"/>) using the current detected inventory.
        /// It does not seed or fabricate detection state. Production code owns the valid
        /// TrackAmplifier addresses, the strongest emergency target set, the backplane/configuration
        /// exclusion and the neutralization itself; this method only orchestrates the call and
        /// reports the production <see cref="SafetyStopResult"/> and trace evidence.
        ///
        /// It is neutralization-only: it never issues a non-neutral HR0 command. Intended for
        /// controlled integration validation; live use requires the Integrator/Product Owner
        /// workflow authorization.
        /// </summary>
        public async Task<SafetyStopResult> StrongestEmergencyNeutralizeAsync()
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine("=== STRONGEST EMERGENCY / LAYOUT NEUTRALIZATION (production path, no synthetic detection) ===");
            Console.WriteLine("  This command invokes the production strongest-emergency/layout neutralization");
            Console.WriteLine("  using the current detected inventory. It does not seed or fabricate detection state.");
            Console.WriteLine("  Production code owns the valid TrackAmplifier addresses, the strongest emergency");
            Console.WriteLine("  target set, the backplane/configuration exclusion and the neutralization.");
            Console.WriteLine("  Neutralization-only: this command issues no non-neutral HR0 command.");
            Console.WriteLine("  Controlled integration validation only; live use requires Integrator/Product Owner authorization.");

            var known = _integration!.RealBackend.GetKnownPhysicalAmplifiers();
            Console.WriteLine($"  GetKnownPhysicalAmplifiers (production): [{string.Join(",", known)}]");

            var detectionBefore = SnapshotDetectedInventory();
            var detectedBackplane = detectionBefore
                .Where(a => TrackAmplifierAddress.IsBackplaneConfigurationSlave(a))
                .ToArray();
            Console.WriteLine(
                $"  Detected backplane/configuration slaves (51..55): {(detectedBackplane.Length == 0 ? "NONE" : string.Join(",", detectedBackplane))}");
            Console.WriteLine($"  Detected inventory before: [{string.Join(",", detectionBefore)}]");

            var writesBefore = _logFactory.WriteRecords.Count;

            Console.WriteLine("  Requesting the production strongest emergency/layout neutralization (ISafetyStopSink.StopLayout)...");
            var result = StrongestEmergencyTrigger.Invoke(_stopSink!);

            // The production StopLayout call is synchronous and never touches detection state, so
            // the immediate post-call inventory shows whether the call seeded anything.
            var detectionAfter = SnapshotDetectedInventory();

            var waitFor = known
                .Concat(Enumerable.Range(
                    TrackAmplifierAddress.MinBackplaneSlave,
                    TrackAmplifierAddress.MaxBackplaneSlave - TrackAmplifierAddress.MinBackplaneSlave + 1)
                    .Select(i => (ushort)i))
                .Distinct()
                .ToArray();
            await WaitForPendingWritesDrainedAsync(waitFor);

            var writes = _logFactory.WriteRecords.Skip(writesBefore).ToArray();
            var backplaneWrites = writes
                .Where(w => Enumerable
                    .Range(TrackAmplifierAddress.MinBackplaneSlave,
                        TrackAmplifierAddress.MaxBackplaneSlave - TrackAmplifierAddress.MinBackplaneSlave + 1)
                    .Any(s => w.Contains($"slave={s},", StringComparison.Ordinal)))
                .ToArray();
            var backplanePending = _variables.PendingWrites.Keys
                .Where(k => TrackAmplifierAddress.IsBackplaneConfigurationSlave(k))
                .ToArray();
            var backplaneTargeted = known
                .Where(a => TrackAmplifierAddress.IsBackplaneConfigurationSlave(a))
                .ToArray();

            Console.WriteLine(
                $"  SAFETY_STOP_RESULT succeeded={result.Succeeded} applied={result.Applied} backendUnavailable={result.BackendUnavailable}");
            Console.WriteLine(
                $"  commanded=[{string.Join(",", result.CommandedAmplifiers)}] failed=[{string.Join(",", result.FailedAmplifiers)}]");
            Console.WriteLine(
                $"  backplane/configuration addresses in the production target inventory: {(backplaneTargeted.Length == 0 ? "NONE" : string.Join(",", backplaneTargeted))}");
            Console.WriteLine(
                $"  HR0 writes to backplane/configuration 51..55: {(backplaneWrites.Length == 0 ? "NONE" : string.Join(" | ", backplaneWrites))}");
            Console.WriteLine(
                $"  pending writes to 51..55: {(backplanePending.Length == 0 ? "NONE" : string.Join(",", backplanePending))}");
            Console.WriteLine(
                $"  detected inventory immediately after the production call: [{string.Join(",", detectionAfter)}] " +
                $"(unchanged by the call: {DetectionEqual(detectionBefore, detectionAfter)})");
            Console.WriteLine("  Authoritative production trace evidence (command 'trace'): EMERGENCY_TARGET_SET, AMPLIFIER_COMMAND, AMPLIFIER_WRITE, SAFETY_STOP_RESULT.");

            return result;
        }

        /// <summary>Explicit safety-latch reset (recovery step, not the per-loco stop test).</summary>
        public Task ResetSafetyAsync()
        {
            RequireComposed();
            Console.WriteLine();
            Console.WriteLine("=== RECOVERY: explicit safety reset ===");
            var reset = _guard!.Reset();
            Console.WriteLine($"  ControlSafetyGuard.Reset() applied={reset} (refused while the fault revalidates as unresolved)");
            return Task.CompletedTask;
        }

        // ---------------------------------------------------------------------------------
        // Script / interactive drivers
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Dry-run self-test: runs the primary stages in order and then the recovery verification.
        /// Only allowed with the recording comm client, never against live hardware.
        /// </summary>
        public async Task RunScriptAsync()
        {
            if (!_options.DryRun)
            {
                throw new InvalidOperationException(
                    "The scripted sequence is only allowed in dry-run mode; live stages must be operator-controlled.");
            }

            await Stage0_BaselineAsync();
            await Stage1_BlockAsync(_options.FromBlock);
            await Stage2_LowSpeedAsync(_options.FromBlock);
            await Stage3_TransitionAsync(_options.TargetBlock);
            await Stage4_LowSpeedAsync(_options.TargetBlock);
            await Stage5_SafetyStopAsync();
            await LayoutStopAsync();
            await VerifyClassificationAsync();
            PrintControlTrace();

            Console.WriteLine();
            Console.WriteLine("DRY-RUN SCRIPT COMPLETE (no hardware was started, no socket was bound).");
        }

        /// <summary>Interactive operator control. Every stage runs only when the operator asks.</summary>
        public async Task RunInteractiveAsync()
        {
            PrintHelp();

            while (true)
            {
                Console.Write("harness> ");
                var line = Console.ReadLine();
                if (line is null)
                {
                    break;
                }

                var parts = line.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }

                try
                {
                    switch (parts[0].ToLowerInvariant())
                    {
                        case "stage0":
                            await Stage0_BaselineAsync();
                            break;
                        case "stage1":
                            await Stage1_BlockAsync(IntArg(parts, 1, _options.FromBlock));
                            break;
                        case "stage2":
                            await Stage2_LowSpeedAsync(IntArg(parts, 1, _options.FromBlock));
                            break;
                        case "stage3":
                            await Stage3_TransitionAsync(IntArg(parts, 1, _options.TargetBlock));
                            break;
                        case "stage4":
                            await Stage4_LowSpeedAsync(IntArg(parts, 1, _options.TargetBlock));
                            break;
                        case "stage5":
                            await Stage5_SafetyStopAsync();
                            break;
                        case "layoutstop":
                            await LayoutStopAsync();
                            break;
                        case "backplanecheck":
                        case "classify":
                            await VerifyClassificationAsync();
                            break;
                        case "strongeststop":
                        case "emergencyneutralize":
                            await StrongestEmergencyNeutralizeAsync();
                            break;
                        case "resetsafety":
                            await ResetSafetyAsync();
                            break;
                        case "status":
                            PrintStatus();
                            break;
                        case "trace":
                            PrintControlTrace();
                            break;
                        case "help":
                            PrintHelp();
                            break;
                        case "quit":
                        case "exit":
                            Console.WriteLine(
                                "WARNING: process exit does NOT neutralize amplifiers. " +
                                "Run 'layoutstop' first, then master software reset, then physical power removal.");
                            return;
                        default:
                            Console.WriteLine($"Unknown command '{parts[0]}'. Type 'help'.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Command failed: {ex.Message}");
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------

        private async Task SendCommandAsync(string rawLine)
        {
            var cmd = new SimpleEcosCommandParser().Parse(rawLine)
                ?? throw new InvalidOperationException($"Could not parse ECoS command '{rawLine}'.");

            using var writer = new StringWriter();
            await _ecosBackend!.HandleAsync(cmd, writer, _cts!.Token);

            Console.WriteLine($"ECOS> {rawLine}");
            var reply = writer.ToString().Replace("\r\n", " | ").Replace('\n', ' ').Trim();
            if (reply.Length > 0)
            {
                Console.WriteLine($"  < {reply}");
            }
        }

        private async Task WaitForPendingWritesDrainedAsync(params ushort[] slaves)
        {
            var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(3);

            while (DateTime.UtcNow < deadline)
            {
                var anyPending = false;
                foreach (var slave in slaves)
                {
                    if (_variables.PendingWrites.TryGetValue(slave, out var write) && write.HasPendingHr0)
                    {
                        anyPending = true;
                        break;
                    }
                }

                if (!anyPending)
                {
                    break;
                }

                await Task.Delay(50);
            }

            // Let the runtime loop finish the SendAsync call it just started.
            await Task.Delay(150);
        }

        private List<(ushort Slave, ushort Hr0)> TakeWrites()
        {
            var all = _logFactory.WriteRecords;
            var result = new List<(ushort, ushort)>();

            for (var i = _writeCursor; i < all.Count; i++)
            {
                if (TryParseWriteRecord(all[i], out var slave, out var hr0))
                {
                    result.Add((slave, hr0));
                }
            }

            _writeCursor = all.Count;
            return result;
        }

        private static bool TryParseWriteRecord(string message, out ushort slave, out ushort hr0)
        {
            slave = 0;
            hr0 = 0;

            var match = WriteRegex.Match(message);
            if (!match.Success)
            {
                return false;
            }

            slave = ushort.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture);
            hr0 = ushort.Parse(match.Groups[2].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return true;
        }

        private void SeedAmplifier(ushort slave)
        {
            var amplifier = Amp(slave);
            amplifier.SlaveDetected = 1;
            amplifier.HoldingReg[TrackAmplifierRegisters.PwmCommand] = (ushort)AmplifierSpeedMapper.NeutralPwm;
            amplifier.LastDataReceivedUtc = DateTimeOffset.UtcNow;
        }

        private TrackAmplifierItem Amp(ushort slave)
            => _variables.trackAmpItems.First(a => a.SlaveNumber == slave);

        /// <summary>
        /// The addresses currently marked detected. Read-only; used to show that the clean
        /// strongest-emergency command does not mutate or fabricate detected inventory.
        /// </summary>
        private ushort[] SnapshotDetectedInventory()
            => _variables.trackAmpItems
                .Where(a => a is not null && a.SlaveDetected != 0)
                .Select(a => a.SlaveNumber)
                .OrderBy(a => a)
                .ToArray();

        private static bool DetectionEqual(ushort[] before, ushort[] after)
            => before.SequenceEqual(after);

        private static int NormalizeDcc28Step(int step)
            => ProtocolSpeedNormalizer.TryNormalize("DCC28", step, out var normalized) ? normalized : step;

        private void PrintBaseline()
        {
            foreach (var slave in PrototypeAmps)
            {
                var amplifier = Amp(slave);
                Console.WriteLine(
                    $"BASELINE AMP{slave} DETECTED={amplifier.SlaveDetected} " +
                    $"FRESH={TrackAmplifierDataFreshness.IsFresh(amplifier)} " +
                    $"OBSERVED_HR0={amplifier.HoldingReg[TrackAmplifierRegisters.PwmCommand]}");
            }
        }

        private void PrintBlockLine(int block)
        {
            var logical = _blockProvider.TryGetBlockForLoc(_options.Address);
            Console.WriteLine(
                $"BLOCK={block} LOCO={_options.Address} ECOS_ID={_options.EcosId} " +
                $"LOGICAL_BLOCK={(logical?.ToString() ?? "<none>")}");
        }

        private void PrintAmpEvidence(
            string label,
            ushort slave,
            int requested,
            List<(ushort Slave, ushort Hr0)> writes)
        {
            var amplifier = Amp(slave);
            var observed = amplifier.HoldingReg[TrackAmplifierRegisters.PwmCommand];
            var ampWrites = writes.Where(w => w.Slave == slave).ToList();
            var writeText = ampWrites.Count == 0 ? "<none>" : ampWrites[^1].Hr0.ToString(CultureInfo.InvariantCulture);

            Console.WriteLine(
                $"{label} TARGET=amp{slave} REQUESTED={requested} WRITE_HR0={writeText} OBSERVED_HR0={observed}");
        }

        private void PrintPendingWrites()
        {
            if (_variables.PendingWrites.Count == 0)
            {
                Console.WriteLine("  PENDINGWRITES: <empty>");
                return;
            }

            foreach (var kv in _variables.PendingWrites.OrderBy(k => k.Key))
            {
                Console.WriteLine(
                    $"  PENDING slave={kv.Key} HR0={kv.Value.Hr0Value} HasPending={kv.Value.HasPendingHr0}");
            }
        }

        private void PrintControlTrace()
        {
            Console.WriteLine();
            Console.WriteLine("=== PRODUCTION CONTROL TRACE (ControlTraceLog) ===");

            var records = _traceCapture?.Records ?? Array.Empty<string>();
            if (records.Count == 0)
            {
                Console.WriteLine("  <no trace records>");
                return;
            }

            foreach (var record in records)
            {
                Console.WriteLine("  " + record);
            }

            Console.WriteLine($"  ({records.Count} trace records captured in memory; the production app writes the same events to the daily ControlTraceLog file)");
        }

        private void PrintStatus()
        {            Console.WriteLine();
            Console.WriteLine("--- status ---");
            Console.WriteLine($"  logical block for loco {_options.Address}: {_blockProvider.TryGetBlockForLoc(_options.Address)?.ToString() ?? "<none>"}");
            Console.WriteLine($"  safety latched: {_guard?.IsLatched} layoutLatched: {_guard?.IsLayoutLatched} unsafe: {_diagnostics?.IsUnsafe}");
            Console.WriteLine($"  latest diagnostic: {_diagnostics?.Latest}");
            PrintBaseline();
            PrintPendingWrites();
            Console.WriteLine("--------------");
        }

        private void PrintHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Operator commands (each stage runs only when you ask):");
            Console.WriteLine("  stage0                 baseline: verify amplifiers detected/fresh and neutral");
            Console.WriteLine("  stage1 [block]         inject loco -> block via the production BlockEntered path");
            Console.WriteLine("  stage2 [block]         request lowest non-zero DCC28 speed via the real ECoS path");
            Console.WriteLine("  stage3 [block]         inject A->B transition (no stop is sent) and observe");
            Console.WriteLine("  stage4 [block]         request the same low speed again in the new block");
            Console.WriteLine("  stage5                 inject a loco-scoped StopRequired diagnostic into the real guard");
            Console.WriteLine("  layoutstop             real layout stop: set(1,stop) -> SetPower(false)");
            Console.WriteLine("  backplanecheck         dry-run: verify 51..55 are never a track-amplifier target");
            Console.WriteLine("  strongeststop          invoke the production strongest-emergency/layout neutralization");
            Console.WriteLine("                         using the current detected inventory (live or dry-run)");
            Console.WriteLine("                         (alias: emergencyneutralize)");
            Console.WriteLine("  resetsafety            explicit ControlSafetyGuard.Reset()");
            Console.WriteLine("  trace                  print the captured production control trace");
            Console.WriteLine("  status | help | quit");
            Console.WriteLine();
            Console.WriteLine("strongeststop / emergencyneutralize:");
            Console.WriteLine("  This command invokes the production strongest-emergency/layout neutralization");
            Console.WriteLine("  using the current detected inventory. It does not seed or fabricate detection state.");
            Console.WriteLine("  It is neutralization-only and intended for controlled integration validation;");
            Console.WriteLine("  live use requires Integrator/Product Owner workflow authorization.");
            Console.WriteLine("  Production code owns the valid TrackAmplifier classification, the strongest emergency");
            Console.WriteLine("  target set and the backplane/configuration exclusion.");
            Console.WriteLine();
            Console.WriteLine("Recovery chain if a locomotive is left non-neutral:");
            Console.WriteLine("  1) layoutstop (real SetPower(false); targets mapped amps, not unmapped amp 6)");
            Console.WriteLine("  2) master software reset (TrackController5 reset/reinitialization)");
            Console.WriteLine("  3) physical backplane/amplifier power removal");
            Console.WriteLine("  Process termination is NOT neutralization.");
            Console.WriteLine();
        }

        private static int IntArg(string[] parts, int index, int fallback)
            => parts.Length > index && int.TryParse(parts[index], out var value) ? value : fallback;

        private static string FormatBool(bool? value)
            => value is null ? "<none>" : (value.Value ? "True" : "False");

        private void RequireComposed()
        {
            if (!_composed)
            {
                throw new InvalidOperationException("The harness is not composed yet.");
            }
        }

        private void Log(string message) => Console.WriteLine($"[HARNESS] {message}");

        public async ValueTask DisposeAsync()
        {
            try
            {
                _controlMain?.StopRuntime();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HARNESS] StopRuntime failed: {ex.Message}");
            }

            try
            {
                _integration?.Detach();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HARNESS] Detach failed: {ex.Message}");
            }

            try
            {
                if (_commClient is not null)
                {
                    await _commClient.StopAsync();
                    await _commClient.DisposeAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[HARNESS] Comm shutdown failed: {ex.Message}");
            }

            _cts?.Cancel();
            _cts?.Dispose();
        }
    }
}
