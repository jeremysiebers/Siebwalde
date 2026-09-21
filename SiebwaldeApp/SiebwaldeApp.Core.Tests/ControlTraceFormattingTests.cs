using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SiebwaldeApp.Core;
using Xunit;

namespace SiebwaldeApp.Core.Tests
{
    /// <summary>
    /// Focused tests for the production control-trace formatting/helper. They assert the
    /// structured payload (stable event names, stable keys, invariant numbers, deterministic
    /// list/null/enum/boolean formatting) and never the FileLogger source/line prefix.
    /// </summary>
    public class ControlTraceFormattingTests
    {
        /// <summary>Captures what the trace hands to the existing log factory.</summary>
        internal sealed class CaptureLogFactory : ILogFactory
        {
            private readonly List<ILogger> _loggers = new();

            public List<(string Message, string Instance, LogLevel Level)> Records { get; } = new();

            public List<ILogger> AddedLoggers
            {
                get
                {
                    lock (_loggers)
                    {
                        return new List<ILogger>(_loggers);
                    }
                }
            }

            public LogOutputLevel LogOutputLevel { get; set; } = LogOutputLevel.Debug;

            public bool IncludeLogOriginDetails { get; set; }

            public event Action<(string Message, LogLevel Level, string loggerinstance)> NewLog = _ => { };

            public void AddLogger(ILogger logger)
            {
                lock (_loggers)
                {
                    _loggers.Add(logger);
                }
            }

            public void RemoveLogger(ILogger logger)
            {
                lock (_loggers)
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
                Records.Add((message, loggerinstance, level));
            }
        }

        private static string[] Trace(CaptureLogFactory factory)
            => factory.Records
                .Where(r => r.Instance == ControlTraceLogger.LoggerInstance)
                .Select(r => r.Message)
                .ToArray();

        private static string Single(CaptureLogFactory factory)
        {
            var records = Trace(factory);
            Assert.Single(records);
            return records[0];
        }

        [Fact]
        public void SessionStart_UsesStableEventNameAndKeys()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.SessionStart("SiebwaldeApp", null, null);

            var payload = Single(factory);
            Assert.StartsWith("EVENT=CONTROL_TRACE_START ", payload);
            Assert.Contains(" app=SiebwaldeApp", payload);
            Assert.Contains(" mode=<none>", payload);
            Assert.Contains(" version=<none>", payload);
            Assert.Contains($" format={ControlTraceLogger.FormatVersion}", payload);
        }

        [Fact]
        public void EveryEventIsWrittenToTheDedicatedLoggerInstance()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.EcosCommand("set", 1001, new[] { "speedstep[1]" });
            trace.BlockTransition(2, 1, 3, "Koploper");
            trace.AmplifierWrite(1, 399);

            Assert.All(factory.Records, r => Assert.Equal(ControlTraceLogger.LoggerInstance, r.Instance));
        }

        [Fact]
        public void EcosCommand_UsesStableKeys()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.EcosCommand("set", 1001, new[] { "speedstep[1]", "dir[0]" });

            var payload = Single(factory);
            Assert.StartsWith("EVENT=ECOS_COMMAND ", payload);
            Assert.Contains(" cmd=set", payload);
            Assert.Contains(" object=1001", payload);
            Assert.Contains(" options=speedstep[1],dir[0]", payload);
        }

        [Fact]
        public void SpeedDecision_ShowsRawAndNormalized()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.SpeedDecision("DCC28", 1001, 2, "speedstep", 1, 5);

            var payload = Single(factory);
            Assert.StartsWith("EVENT=SPEED_DECISION ", payload);
            Assert.Contains(" protocol=DCC28", payload);
            Assert.Contains(" raw=1", payload);
            Assert.Contains(" normalized=5", payload);
        }

        [Fact]
        public void BlockTransition_UnknownPreviousIsNone()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.BlockTransition(2, null, 1, "Koploper");

            var payload = Single(factory);
            Assert.StartsWith("EVENT=BLOCK_TRANSITION ", payload);
            Assert.Contains(" previous=<none>", payload);
            Assert.Contains(" block=1", payload);
        }

        [Fact]
        public void AmplifierCommand_FormatsEnumAndListsDeterministically()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.AmplifierCommand(
                "Loco",
                2,
                1,
                3,
                "Movement",
                416,
                416,
                TrackAmplifierOperationalGroup.MountainRailway,
                5,
                0);

            var payload = Single(factory);
            Assert.StartsWith("EVENT=AMPLIFIER_COMMAND ", payload);
            Assert.Contains(" source=Loco", payload);
            Assert.Contains(" amp=1", payload);
            Assert.Contains(" pwm=416", payload);
            Assert.Contains(" hr0=416", payload);
            Assert.Contains(" group=MountainRailway", payload);
            Assert.Contains(" speed=5", payload);
            Assert.Contains(" dir=0", payload);
        }

        [Fact]
        public void AmplifierLists_AreCommaSeparatedAndEmptyIsNone()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.TrackerAdd(7, 3, new ushort[] { 1, 3 });
            trace.TrackerRemove(7, 3, "NeutralCommanded", Array.Empty<ushort>());

            var records = Trace(factory);
            Assert.Equal(2, records.Length);
            Assert.Contains(" outstanding=1,3", records[0]);
            Assert.Contains(" reason=NeutralCommanded", records[1]);
            Assert.Contains(" outstanding=<none>", records[1]);
        }

        [Fact]
        public void Booleans_AreLowerCaseAndEnumsUseMemberNames()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.SafetyStopResult(
                "Loco",
                7,
                succeeded: true,
                applied: false,
                backendUnavailable: false,
                commanded: new ushort[] { 1 },
                failed: Array.Empty<ushort>(),
                retained: new ushort[] { 1, 3 },
                staleFailed: Array.Empty<ushort>());

            var payload = Single(factory);
            Assert.Contains(" succeeded=true", payload);
            Assert.Contains(" applied=false", payload);
            Assert.Contains(" backendunavailable=false", payload);
            Assert.Contains(" retained=1,3", payload);
        }

        [Fact]
        public void Abnormal_DetailWithLineBreaksStaysOneLine()
        {
            var factory = new CaptureLogFactory();
            var trace = new ControlTraceLogger(factory);

            trace.Abnormal("InvalidAmplifierAddress", 51, "first line\r\nsecond line");

            var payload = Single(factory);
            Assert.DoesNotContain("\n", payload);
            Assert.DoesNotContain("\r", payload);
            Assert.Contains(" kind=InvalidAmplifierAddress", payload);
            Assert.Contains(" amp=51", payload);
        }

        [Fact]
        public void NumericFormatting_IsInvariantUnderAnotherCulture()
        {
            var original = CultureInfo.CurrentCulture;
            try
            {
                // A culture whose number formatting differs from invariant.
                CultureInfo.CurrentCulture = new CultureInfo("de-DE");

                var factory = new CaptureLogFactory();
                var trace = new ControlTraceLogger(factory);

                trace.AmplifierCommand(
                    "Loco",
                    2,
                    1,
                    3,
                    "Movement",
                    416,
                    416,
                    TrackAmplifierOperationalGroup.MainRailway,
                    5,
                    0);

                var payload = Single(factory);
                Assert.Contains(" pwm=416", payload);
                Assert.Contains(" speed=5", payload);
                Assert.DoesNotContain("416,", payload);
            }
            finally
            {
                CultureInfo.CurrentCulture = original;
            }
        }

        [Fact]
        public void BuildLogFilePath_FollowsTheExistingComponentLogConvention()
        {
            var path = ControlTraceLogging.BuildLogFilePath(@"C:\Localdata\Siebwalde\Logging");

            Assert.Equal(@"C:\Localdata\Siebwalde\Logging", Path.GetDirectoryName(path));
            Assert.EndsWith("_ControlTraceLog.txt", path);

            var now = DateTime.Now;
            Assert.StartsWith(
                $"{now.Day}-{now.Month}-{now.Year}_",
                Path.GetFileName(path));
        }

        [Fact]
        public void Register_AddsADedicatedFileLoggerAndEmitsSessionStart()
        {
            var factory = new CaptureLogFactory();

            var trace = ControlTraceLogging.Register(
                factory,
                Path.Combine(Path.GetTempPath(), "siebwalde-trace-register"),
                "SiebwaldeApp");

            var fileLogger = Assert.IsType<FileLogger>(Assert.Single(factory.AddedLoggers));
            Assert.Equal(ControlTraceLogger.LoggerInstance, fileLogger.LoggerInstance);
            Assert.EndsWith("_ControlTraceLog.txt", fileLogger.FilePath);

            var payload = Single(factory);
            Assert.StartsWith("EVENT=CONTROL_TRACE_START ", payload);
            Assert.NotNull(trace);
        }
    }
}
