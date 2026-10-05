using System;
using System.Linq;
using SiebwaldeApp.Core.Koploper;
using SiebwaldeApp.Koploper.Windows;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class KoploperWindowsIntegrationSeam
    {
        [Fact]
        public void WindowsSeam_LocatesVerifiesAttachesAndDecodesDemoConfig_WhenPresent()
        {
            if (!OperatingSystem.IsWindows())
            {
                return;
            }

            IKoploperMemoryLayout layout = Koploper94MemoryLayout.Instance;
            var locator = new WindowsKoploperProcessLocator();

            KoploperProcessStatus status = locator.TryLocate(out KoploperProcessInfo? info);
            if (status != KoploperProcessStatus.Found)
            {
                // Koploper is not running. Return early (green) rather than fail: the real
                // xUnit dynamic skip (SkipException.ForSkip) is not honored by the current
                // xunit 2.9.0 + xunit.runner.visualstudio 2.8.1 pairing and surfaces as Failed.
                return;
            }

            // Hard gate: the located binary must be the supported Koploper 9.4.0.9.
            var verifier = new KoploperExecutableVerifier(layout);
            Assert.Equal(KoploperVersionGateResult.Supported, verifier.Verify(info!.ExecutablePath, out _));

            using var reader = new WindowsKoploperMemoryReader();
            Assert.Equal(KoploperMemoryAccessResult.Attached, reader.Attach(info.ProcessId));

            var decoder = new KoploperObjectGraphDecoder(layout, new KoploperDecodePlausibility());
            KoploperObjectGraphDecodeResult decode = decoder.Decode(reader, info.ModuleBaseAddress, out KoploperRawRegistry? registry);

            Assert.Equal(KoploperObjectGraphDecodeResult.Success, decode);
            Assert.NotNull(registry);

            // Demo-config regression data (NOT production constants). It is only meaningful when
            // the standard simulator demo layout (30 blocks / 3 locomotives) is actually loaded;
            // a running process with a different (e.g. empty) layout must not fail the suite.
            if (registry.Blocks.Count != 30 || registry.Locomotives.Count != 3)
            {
                return;
            }

            uint[] locoIds = registry.Locomotives.Select(l => l.InternalLocomotiveId).ToArray();
            Assert.Contains(2u, locoIds);
            Assert.Contains(8u, locoIds);
            Assert.Contains(24u, locoIds);
        }
    }
}
