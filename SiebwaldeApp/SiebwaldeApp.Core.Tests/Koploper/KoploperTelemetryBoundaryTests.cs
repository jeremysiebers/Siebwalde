using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SiebwaldeApp.Core;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Core.TrackApplication.Topology;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Profile and boundary tests for the KIS-09 bounded telemetry-enablement boundary: the
    /// "Simple Loop" profile mappings, the non-numeric-identity binding-map guarantee, and the
    /// structural no-movement-backend guarantee of the outbound guard.
    /// </summary>
    public class KoploperTelemetryBoundaryTests
    {
        [Fact]
        public void SimpleLoopProfile_HasFourKoploperBlockBindings_AndFourPhysicalBindings()
        {
            Assert.True(
                LayoutProfileLoader.TryLoadFromFile(LayoutProfileLoader.SimpleLoopPath, out var profile, out var errors),
                string.Join("; ", errors));
            Assert.NotNull(profile);

            Assert.Equal(4, profile!.KoploperBlockBindings.Count);
            Assert.Equal(4, profile.PhysicalAmplifierMapping.Count);

            var physical = profile.PhysicalAmplifierMapping
                .ToDictionary(b => b.SectionId, b => b.PhysicalAmplifier);

            Assert.Equal(1, physical[1]);
            Assert.Equal(3, physical[2]);
            Assert.Equal(4, physical[3]);
            Assert.Equal(6, physical[4]);
        }

        [Fact]
        public void BindingMap_IsKeyedByInternalBlockId_NotNumericIdentity()
        {
            var profile = new LayoutProfile
            {
                KoploperBlockBindings = new[] { new KoploperBlockBinding(30, 2) }
            };

            var map = profile.ToKoploperBlockBindingMap();

            Assert.Single(map);
            Assert.True(map.ContainsKey(30));       // keyed by the INTERNAL block id
            Assert.Equal(2, map[30]);
            Assert.False(map.ContainsKey(2));        // NOT keyed by the logical section id (no numeric shortcut)
        }

        [Fact]
        public void GuardBoundary_HasNoMovementOrHardwareWriteBackendReference()
        {
            Assembly assembly = typeof(KoploperSessionCommandGuard).Assembly;

            string[] typeNames =
            {
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandGuard",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandAllowlist",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandCounters",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandRejectedException",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandOrderViolationException",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandMode",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandOrderViolationReason",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandAcknowledgementTimeoutException",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandOutcome",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandEvent"
            };

            string[] forbidden =
            {
                "TrackControlMain",
                "TrackAmplifierHardwareBackend",
                "ControlSafetyInterlockBackend",
                "ControlSafetyGuard",
                "EcosHardwareStopSink",
                "SetDesiredAmplifierControl",
                "EXEC_MBUS_SLAVE_DATA_EXCH",   // 108 — the only HR0/movement write vector
                "EXEC_MBUS_STATE_RESET",        // 107
                "FlashFwTrackamplifiersStep",
                "RecoverSlavesStep",
                "ResetAllSlavesStep",
                "EnableTrackamplifiersStep",
                "SetDefaultPwmSetpointsStep"
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

        [Fact]
        public void Allowlist_Rejects108And107()
        {
            Assert.False(KoploperSessionCommandAllowlist.IsPermitted(TrackCommand.EXEC_MBUS_SLAVE_DATA_EXCH)); // 108
            Assert.False(KoploperSessionCommandAllowlist.IsPermitted(TrackCommand.EXEC_MBUS_STATE_RESET));      // 107
        }

        [Fact]
        public void GuardBoundary_HasNoDllImportEntryPoints()
        {
            Assembly assembly = typeof(KoploperSessionCommandGuard).Assembly;

            string[] typeNames =
            {
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandGuard",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandAllowlist",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandCounters",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandRejectedException",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandOrderViolationException",
                "SiebwaldeApp.Core.Koploper.KoploperSessionCommandAcknowledgementTimeoutException"
            };

            var entryPoints = new List<string>();
            foreach (string typeName in typeNames)
            {
                Type? type = assembly.GetType(typeName);
                Assert.NotNull(type);

                foreach (MethodInfo method in type!.GetMethods(
                             BindingFlags.Static | BindingFlags.Instance |
                             BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.DeclaredOnly))
                {
                    DllImportAttribute? dllImport = method.GetCustomAttribute<DllImportAttribute>();
                    if (dllImport is null)
                    {
                        continue;
                    }

                    entryPoints.Add(dllImport.EntryPoint ?? method.Name);
                }
            }

            Assert.Empty(entryPoints);
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
