using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using SiebwaldeApp.Integration;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Tests for the passive KIS-09 observer: it reconciles the production-shared
    /// <see cref="TrackApplicationVariables"/> container (KIS-08 physical source) against the KIS-07
    /// logical shadow, WITHOUT a comm client / transport / session guard (no second PIC32 client).
    /// </summary>
    public class Kis09PassiveObserverTests
    {
        [Fact]
        public void ReadPhysicalObservation_ReadsLiveProductionVariables_NotACopy()
        {
            var variables = new TrackApplicationVariables();
            var amp3 = variables.trackAmpItems.First(a => a.SlaveNumber == 3);
            amp3.SlaveDetected = 0;
            amp3.ProtocolReadbackObserved = true;
            amp3.LastDataReceivedUtc = ReconciliationTestFixtures.Now;
            var occupied = new ushort[12];
            occupied[TrackAmplifierRegisters.Status] = TrackAmplifierRegisters.OccupiedBit;
            amp3.HoldingReg = occupied;

            var observer = CreateObserver(variables);

            PhysicalSectionOccupancyObservation phys = observer.ReadPhysicalObservation();
            var section2 = phys.Sections.First(s => s.LogicalSectionId == 2);
            Assert.Equal(PhysicalOccupancy.Occupied, section2.Occupancy);

            // Mutate the LIVE container AFTER construction; the observer must see it (not a copy).
            var amp3b = variables.trackAmpItems.First(a => a.SlaveNumber == 3);
            amp3b.HoldingReg = new ushort[12]; // clear occupancy

            PhysicalSectionOccupancyObservation phys2 = observer.ReadPhysicalObservation();
            var section2b = phys2.Sections.First(s => s.LogicalSectionId == 2);
            Assert.Equal(PhysicalOccupancy.Clear, section2b.Occupancy);
        }

        [Fact]
        public void Refresh_ReturnsReconciliationObservation()
        {
            var variables = new TrackApplicationVariables();
            var observer = CreateObserver(variables);

            LogicalPhysicalReconciliationObservation observation = observer.Refresh();

            Assert.NotNull(observation);
            Assert.Equal("Simple Loop", observation.ProfileId);
        }

        [Fact]
        public async Task Consumer_WritesTraceRecord_AndIsStoppable()
        {
            var variables = new TrackApplicationVariables();
            var observer = CreateObserver(variables);
            string path = Path.Combine(Path.GetTempPath(), $"kis09-passive-{Guid.NewGuid():N}.jsonl");

            var consumer = new Kis09PassiveObserverConsumer(observer, path, interval: TimeSpan.FromMilliseconds(10));

            using var cts = new CancellationTokenSource();
            consumer.Start(cts.Token);

            try
            {
                // Wait for the file to be created and non-empty (start line flushed), then give the
                // loop a moment to write at least one trace record.
                await WaitUntilAsync(() => File.Exists(path) && new FileInfo(path).Length > 0);
                await Task.Delay(150);

                await consumer.StopAsync();

                string[] lines = File.ReadAllLines(path);
                Assert.Contains(lines, l => l.Contains("kis09PassiveScenarioStart"));
                Assert.Contains(lines, l => l.Contains("reconciliation"));
                Assert.Contains(lines, l => l.Contains("kis09PassiveScenarioEnd"));
                Assert.True(lines.Length >= 3);
            }
            finally
            {
                await consumer.StopAsync();
                try
                {
                    File.Delete(path);
                }
                catch (IOException)
                {
                    // Best-effort cleanup.
                }
            }
        }

        [Fact]
        public void ObserverTypes_ReferenceNoCommOrSessionTypes()
        {
            Assembly assembly = typeof(Kis09PassiveObserver).Assembly;

            string[] typeNames =
            {
                "SiebwaldeApp.Integration.Kis09PassiveObserver",
                "SiebwaldeApp.Integration.Kis09PassiveObserverFactory",
                "SiebwaldeApp.Integration.Kis09PassiveObserverConsumer"
            };

            string[] forbidden =
            {
                "TrackCommClientAsync",
                "RawUdpTransport",      // covers RawUdpTransport + RawUdpTrackTransport
                "ITrackCommClient",
                "KoploperSessionCommandGuard",
                "SendMessage"           // the comm SendMessage struct
            };

            var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<Type>();

            foreach (string typeName in typeNames)
            {
                Type? type = assembly.GetType(typeName);
                Assert.NotNull(type);
                CollectTypeSymbols(type!, symbols, visited);
            }

            foreach (string symbol in symbols)
            {
                foreach (string forbiddenSymbol in forbidden)
                {
                    Assert.DoesNotContain(forbiddenSymbol, symbol, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private static Kis09PassiveObserver CreateObserver(TrackApplicationVariables variables)
        {
            var profile = new LayoutProfile
            {
                Name = "Simple Loop",
                PhysicalAmplifierMapping = new[]
                {
                    new LayoutPhysicalAmplifierBinding { SectionId = 1, PhysicalAmplifier = 1 },
                    new LayoutPhysicalAmplifierBinding { SectionId = 2, PhysicalAmplifier = 3 },
                },
                KoploperBlockBindings = new[]
                {
                    new KoploperBlockBinding(1, new[] { 1 }),
                    new KoploperBlockBinding(2, new[] { 2 }),
                }
            };

            var fakeReservation = new FakeReservationObserver();
            var fakeShadow = new FakeKoploperLogicalSectionShadowObserver(
                ReconciliationTestFixtures.Shadow(profileId: "Simple Loop"));

            return new Kis09PassiveObserver(
                variables,
                profile,
                fakeReservation,
                fakeShadow,
                clock: () => ReconciliationTestFixtures.Now);
        }

        private static async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 3000)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (!condition() && sw.ElapsedMilliseconds < timeoutMs)
            {
                await Task.Delay(10);
            }

            Assert.True(condition(), "condition not met within timeout");
        }

        private sealed class FakeReservationObserver : IKoploperReservationObserver
        {
            public KoploperReservationObservation? CurrentObservation => null;

            public KoploperReservationObservation Refresh()
                => new KoploperReservationObservation(
                    Generation: null,
                    SourceSequence: 0,
                    ObserverSequence: 0,
                    CapturedAtUtc: ReconciliationTestFixtures.Now,
                    ObservedAtUtc: ReconciliationTestFixtures.Now,
                    IsFresh: false,
                    IsConsistent: false,
                    SourceHealth: KoploperSourceHealth.Unavailable,
                    IsAuthoritative: false,
                    AuthorityReason: KoploperReservationAuthorityReason.Unavailable,
                    Locomotives: Array.Empty<KoploperLocomotiveTrajectory>(),
                    Conflicts: Array.Empty<KoploperReservationConflict>(),
                    Diagnostics: Array.Empty<KoploperDiagnosticCode>());

            public void Start(CancellationToken cancellationToken)
            {
            }

            public Task StopAsync() => Task.CompletedTask;

            public void Dispose()
            {
            }
        }

        private static void CollectTypeSymbols(Type type, HashSet<string> symbols, HashSet<Type> visited)
        {
            if (!visited.Add(type))
            {
                return;
            }

            symbols.Add(type.Name);

            if (type.BaseType is not null)
            {
                symbols.Add(type.BaseType.Name);
            }

            foreach (Type interfaceType in type.GetInterfaces())
            {
                symbols.Add(interfaceType.Name);
            }

            if (type.IsGenericType)
            {
                foreach (Type argument in type.GetGenericArguments())
                {
                    symbols.Add(argument.Name);
                }
            }

            foreach (MemberInfo member in type.GetMembers(
                         BindingFlags.Public | BindingFlags.NonPublic |
                         BindingFlags.Instance | BindingFlags.Static |
                         BindingFlags.DeclaredOnly))
            {
                symbols.Add(member.Name);

                switch (member)
                {
                    case MethodBase method:
                        if (method is MethodInfo methodInfo)
                        {
                            symbols.Add(methodInfo.ReturnType.Name);
                        }

                        foreach (ParameterInfo parameter in method.GetParameters())
                        {
                            symbols.Add(parameter.ParameterType.Name);
                            symbols.Add(parameter.Name ?? string.Empty);
                        }

                        break;

                    case FieldInfo field:
                        symbols.Add(field.FieldType.Name);
                        break;

                    case PropertyInfo property:
                        symbols.Add(property.PropertyType.Name);
                        break;

                    case EventInfo eventInfo:
                        symbols.Add(eventInfo.EventHandlerType?.Name ?? string.Empty);
                        break;

                    case Type nestedType:
                        CollectTypeSymbols(nestedType, symbols, visited);
                        break;
                }
            }
        }
    }
}
