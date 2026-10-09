using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    /// <summary>
    /// Structural no-write/no-movement guarantee for the logical/physical reconciliation subtree.
    /// The reconciliation types must reference no process-memory-write, movement, PWM, HR0,
    /// carrier, switch or neutralization symbols, and must declare no P/Invoke entry points.
    /// </summary>
    public class ReconciliationNoWriteGuaranteeTests
    {
        private static readonly string[] ReconciliationTypeNames =
        {
            "SiebwaldeApp.Core.Koploper.AutomaticSectionRole",
            "SiebwaldeApp.Core.Koploper.LogicalSectionReconciliationState",
            "SiebwaldeApp.Core.Koploper.SectionSafetyEligibility",
            "SiebwaldeApp.Core.Koploper.LogicalSectionReconciliation",
            "SiebwaldeApp.Core.Koploper.LogicalLocomotiveReconciliation",
            "SiebwaldeApp.Core.Koploper.LogicalPhysicalReconciliationObservation",
            "SiebwaldeApp.Core.Koploper.ReconciliationDiagnosticCode",
            "SiebwaldeApp.Core.Koploper.LogicalPhysicalReconciliationProjector",
            "SiebwaldeApp.Core.Koploper.ILogicalPhysicalReconciliationObserver",
            "SiebwaldeApp.Core.Koploper.LogicalPhysicalReconciliationObserver",
            "SiebwaldeApp.Core.Koploper.LogicalPhysicalReconciliationObserverOptions"
        };

        private static readonly string[] ForbiddenSymbols =
        {
            "WriteProcessMemory",
            "Movement",
            "RunAllowed",
            "CanMove",
            "Pwm",
            "HR0",
            "Carrier",
            "Switch",
            "Neutral"
        };

        [Fact]
        public void ReconciliationSubtree_ReferencesNoMovementOrHardwareWriteSymbols()
        {
            Assembly assembly = typeof(LogicalPhysicalReconciliationProjector).Assembly;

            var symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<Type>();

            foreach (string typeName in ReconciliationTypeNames)
            {
                Type? type = assembly.GetType(typeName);
                Assert.NotNull(type);
                CollectTypeSymbols(type!, symbols, visited);
            }

            foreach (string symbol in symbols)
            {
                foreach (string forbidden in ForbiddenSymbols)
                {
                    Assert.DoesNotContain(forbidden, symbol, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        [Fact]
        public void ReconciliationSubtree_HasNoDllImportEntryPoints()
        {
            Assembly assembly = typeof(LogicalPhysicalReconciliationProjector).Assembly;

            var entryPoints = new List<string>();
            foreach (string typeName in ReconciliationTypeNames)
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

            // Note: type references are collected by simple name (Type.Name), not FullName, so a
            // constructed generic type's assembly-qualified metadata (e.g. "Culture=neutral") is
            // never mistaken for a forbidden "Neutral" symbol reference.
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
