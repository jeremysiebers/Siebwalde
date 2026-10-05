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
                    string? entryPoint = method.GetCustomAttribute<DllImportAttribute>()?.EntryPoint;
                    if (!string.IsNullOrEmpty(entryPoint))
                    {
                        entryPoints.Add(entryPoint);
                    }
                }
            }

            Assert.DoesNotContain(entryPoints, ep => forbidden.Contains(ep, StringComparer.OrdinalIgnoreCase));
        }
    }
}
