using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.EcosEmu;

namespace SiebwaldeApp.StopReachabilityHarness
{
    /// <summary>
    /// Log factory for the harness.
    ///
    /// It deliberately does not write to any file, so a harness run cannot create or modify
    /// generated output inside the repository. Messages are echoed to the console, and runtime
    /// <c>[WRITE]</c> records are kept so the report can quote the actual production writer
    /// output instead of a re-derived value.
    /// </summary>
    internal sealed class HarnessLogFactory : ILogFactory
    {
        private readonly List<ILogger> _loggers = new();
        private readonly List<string> _writes = new();
        private readonly object _gate = new();

        public LogOutputLevel LogOutputLevel { get; set; } = LogOutputLevel.Debug;

        public bool IncludeLogOriginDetails { get; set; }

        public event Action<(string Message, LogLevel Level, string loggerinstance)> NewLog = _ => { };

        /// <summary>All <c>[WRITE]</c> messages emitted by the production runtime loop, in order.</summary>
        public IReadOnlyList<string> WriteRecords
        {
            get
            {
                lock (_gate)
                {
                    return _writes.ToArray();
                }
            }
        }

        public void AddLogger(ILogger logger)
        {
            if (logger is null) throw new ArgumentNullException(nameof(logger));

            lock (_gate)
            {
                if (!_loggers.Contains(logger)) _loggers.Add(logger);
            }
        }

        public void RemoveLogger(ILogger logger)
        {
            lock (_gate)
            {
                _loggers.Remove(logger);
            }
        }

        public void Log(
            string message,
            string loggerinstance,
            LogLevel level = LogLevel.Informative,
            [CallerMemberName] string origin = "",
            [CallerFilePath] string filepath = "",
            [CallerLineNumber] int linenumber = 0)
        {
            if ((int)level < (int)LogOutputLevel)
                return;

            // The writer records are the primary "Commanded" evidence, so surface them directly.
            if (message.Contains("[WRITE]", StringComparison.Ordinal))
            {
                lock (_gate)
                {
                    _writes.Add(message);
                }

                Console.WriteLine($"    {message}");
            }

            ILogger[] snapshot;
            lock (_gate)
            {
                snapshot = _loggers.ToArray();
            }

            foreach (var logger in snapshot)
            {
                logger.Log(message, level, loggerinstance);
            }

            NewLog((message, level, loggerinstance));
        }
    }

    /// <summary>
    /// Minimal console logger used by the harness log factory.
    /// </summary>
    internal sealed class HarnessConsoleLogger : ILogger
    {
        public void Log(string message, LogLevel level, string loggerinstance)
        {
            // [WRITE] records are already echoed by HarnessLogFactory; avoid a duplicate line.
            if (message.Contains("[WRITE]", StringComparison.Ordinal))
                return;

            if (!string.IsNullOrEmpty(loggerinstance))
            {
                Console.WriteLine($"[{loggerinstance}] {message}");
            }
            else
            {
                Console.WriteLine(message);
            }
        }
    }

    /// <summary>
    /// Substitution 1 of 2: the logical block-location source.
    ///
    /// Production uses <c>KoploperExternalInfoClient</c>, which turns a Koploper external-info
    /// record into <c>_locToBlock[loco] = block; BlockEntered?.Invoke(loco, block)</c>.
    /// This provider reproduces exactly that contract, but the operator (or the script) decides
    /// when a block event happens. <c>SimpleEcosBackend</c> still subscribes to
    /// <see cref="BlockEntered"/> and runs the production <c>OnBlockEntered</c> path, and
    /// <c>TrackAmplifierHardwareBackend</c> still reads <see cref="TryGetBlockForLoc"/>.
    /// No private field of any production component is touched.
    /// </summary>
    internal sealed class ControllableBlockPositionProvider : IBlockPositionProvider
    {
        private readonly Dictionary<int, int> _locToBlock = new();
        private readonly object _gate = new();

        public event Action<int, int>? BlockEntered;

        public int? TryGetBlockForLoc(int loc)
        {
            lock (_gate)
            {
                return _locToBlock.TryGetValue(loc, out var block) ? block : (int?)null;
            }
        }

        /// <summary>Raises the same event the production external-info client raises.</summary>
        public void Inject(int locoAddress, int block)
        {
            lock (_gate)
            {
                _locToBlock[locoAddress] = block;
            }

            BlockEntered?.Invoke(locoAddress, block);
        }
    }

    /// <summary>One parsed amplifier write, used for the dry-run observation echo.</summary>
    internal readonly record struct AmplifierWrite(ushort Slave, ushort Hr0, DateTimeOffset At);

    /// <summary>
    /// Dry-run-only stand-in for the real <c>TrackCommClientAsync</c>.
    ///
    /// It parses the exact <c>EXEC_MBUS_SLAVE_DATA_EXCH</c> frame <c>TrackControlMain</c> builds,
    /// records it, and simulates the amplifier echo by writing the value into the observed
    /// <c>HoldingReg[0]</c> image and republishing it. It never touches a socket or hardware.
    ///
    /// It is used ONLY for the non-hardware dry run, exactly as permitted, so that call ordering
    /// and the resulting observed register values can be checked before any live run.
    /// </summary>
    internal sealed class RecordingTrackCommClient : ITrackCommClient
    {
        private readonly TrackApplicationVariables _variables;
        private readonly Action<string> _log;
        private readonly List<AmplifierWrite> _writes = new();
        private readonly object _gate = new();

        public RecordingTrackCommClient(TrackApplicationVariables variables, Action<string> log)
        {
            _variables = variables ?? throw new ArgumentNullException(nameof(variables));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public event EventHandler<AmplifierDataEventArgs>? AmplifierDataReceived;
#pragma warning disable CS0067 // Not raised by the recorder; present for interface compatibility.
        public event EventHandler<ControlMessageEventArgs>? ControlMessageReceived;
#pragma warning restore CS0067

        public IReadOnlyList<AmplifierWrite> Writes
        {
            get
            {
                lock (_gate)
                {
                    return _writes.ToArray();
                }
            }
        }

        public Task StartAsync(bool realHardwareMode, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Task SendAsync(SendMessage message, CancellationToken cancellationToken = default)
        {
            // Format written by TrackControlMain.BuildSlaveDataExchWriteMessage:
            //   data[0] = slave, data[1] = 0xAA (write), data[2] = register count,
            //   data[3] = start register, data[4..5] = value little-endian.
            if (message.Command == TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH &&
                message.Data is { Length: >= 6 } &&
                message.Data[3] == 0)
            {
                var slave = message.Data[0];
                var hr0 = (ushort)(message.Data[4] | (message.Data[5] << 8));

                lock (_gate)
                {
                    _writes.Add(new AmplifierWrite(slave, hr0, DateTimeOffset.UtcNow));
                }

                var amplifier = _variables.trackAmpItems.FirstOrDefault(a => a.SlaveNumber == slave);
                if (amplifier is not null)
                {
                    // Simulated amplifier echo: the observed PWM register follows the write, and
                    // the amplifier is treated as detected with fresh data.
                    amplifier.HoldingReg[TrackAmplifierRegisters.PwmCommand] = hr0;
                    amplifier.SlaveDetected = 1;
                    amplifier.LastDataReceivedUtc = DateTimeOffset.UtcNow;
                    AmplifierDataReceived?.Invoke(this, new AmplifierDataEventArgs(slave, amplifier));
                }

                _log($"[HARNESS-RECORDER] send slave={slave} HR0=0x{hr0:X4} ({hr0})");
            }

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Observation-only decorator around the real <see cref="EcosHardwareStopSink"/>.
    ///
    /// The real sink now reports command-level stop success/failure and
    /// <c>ControlSafetyGuard</c> inspects that result and escalates when it is incomplete. The
    /// harness forwards every call unchanged and records the returned success flag as evidence.
    /// The stop implementation and the hardware targeting stay real.
    /// </summary>
    internal sealed class RecordingStopSink : ISafetyStopSink
    {
        private readonly ISafetyStopSink _inner;
        private readonly Action<string> _log;

        public RecordingStopSink(ISafetyStopSink inner, Action<string> log)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        public int? LastStopLocoAddress { get; private set; }
        public bool? LastStopLocoResult { get; private set; }
        public bool LayoutStopCalled { get; private set; }

        public SafetyStopResult StopLoco(int address)
        {
            LastStopLocoAddress = address;
            var result = _inner.StopLoco(address);
            LastStopLocoResult = result.Succeeded;
            _log($"real EcosHardwareStopSink.StopLoco({address}) succeeded={result.Succeeded} (the guard inspects this).");
            return result;
        }

        public SafetyStopResult StopLayout()
        {
            LayoutStopCalled = true;
            var result = _inner.StopLayout();
            _log($"real EcosHardwareStopSink.StopLayout() succeeded={result.Succeeded}.");
            return result;
        }
    }
}
