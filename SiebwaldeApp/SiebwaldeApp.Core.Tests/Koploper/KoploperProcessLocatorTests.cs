using System;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Koploper.Windows;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class KoploperProcessLocatorTests
    {
        private static KoploperProcessInfo Info(int processId, long utcTicks)
            => new(
                processId,
                new DateTimeOffset(utcTicks, TimeSpan.Zero),
                (nuint)0x00400000,
                @"C:\Koploper\koploper.exe",
                "koploper.exe");

        [Fact]
        public void IsRestart_NullPrevious_IsTrue()
        {
            var locator = new WindowsKoploperProcessLocator();

            Assert.True(locator.IsRestart(null, Info(100, 1234)));
        }

        [Fact]
        public void IsRestart_SamePidAndStartTime_IsFalse()
        {
            var locator = new WindowsKoploperProcessLocator();

            Assert.False(locator.IsRestart(Info(100, 1234), Info(100, 1234)));
        }

        [Fact]
        public void IsRestart_DifferentPid_IsTrue()
        {
            var locator = new WindowsKoploperProcessLocator();

            Assert.True(locator.IsRestart(Info(100, 1234), Info(200, 1234)));
        }

        [Fact]
        public void IsRestart_SamePidDifferentStartTime_IsTrue()
        {
            var locator = new WindowsKoploperProcessLocator();

            Assert.True(locator.IsRestart(Info(100, 1234), Info(100, 5678)));
        }
    }
}
