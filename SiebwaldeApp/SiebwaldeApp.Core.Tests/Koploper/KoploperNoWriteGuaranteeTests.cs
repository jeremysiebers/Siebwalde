using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using SiebwaldeApp.Koploper.Windows;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class KoploperNoWriteGuaranteeTests
    {
        [Fact]
        public void MemoryReaderAccess_HasNoWriteRights()
        {
            Assert.Equal(0u, KoploperProcessAccess.MemoryReaderDesiredAccess & KoploperProcessAccess.WriteRightsMask);
        }

        [Fact]
        public void MemoryReaderDesiredAccess_IsExpectedReadOnlyValue()
        {
            Assert.Equal(0x1010u, KoploperProcessAccess.MemoryReaderDesiredAccess);
        }

        [Fact]
        public void LocatorAccess_HasNoWriteRights()
        {
            Assert.Equal(0u, KoploperProcessAccess.LocatorDesiredAccess & KoploperProcessAccess.WriteRightsMask);
        }

        [Fact]
        public void NoWriteOrInjectionPInvokeEntryPoints()
        {
            string[] forbidden =
            {
                "WriteProcessMemory",
                "VirtualAllocEx",
                "VirtualAllocExNuma",
                "CreateRemoteThread",
                "SetWindowsHookEx",
                "NtWriteVirtualMemory",
                "LoadLibrary",
            };

            Assembly assembly = typeof(KoploperProcessAccess).Assembly;

            var entryPoints = new List<string>();
            foreach (Type type in assembly.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(
                             BindingFlags.Static | BindingFlags.Instance |
                             BindingFlags.Public | BindingFlags.NonPublic |
                             BindingFlags.DeclaredOnly))
                {
                    DllImportAttribute? dllImport = method.GetCustomAttribute<DllImportAttribute>();
                    if (dllImport is null)
                    {
                        continue;
                    }

                    // EntryPoint is null when the DllImport omits an explicit EntryPoint;
                    // the effective entry point then defaults to the method name.
                    string entryPoint = dllImport.EntryPoint ?? method.Name;
                    entryPoints.Add(entryPoint);
                }
            }

            Assert.DoesNotContain(entryPoints, ep => forbidden.Contains(ep, StringComparer.OrdinalIgnoreCase));
        }
    }
}
