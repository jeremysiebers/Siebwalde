using System;
using System.IO;
using System.Security.Cryptography;
using SiebwaldeApp.Core.Koploper;
using Xunit;

namespace SiebwaldeApp.Core.Tests.Koploper
{
    public class KoploperExecutableVerifierTests : IDisposable
    {
        private static readonly byte[] FileBytes = { 0x01, 0x02, 0x03, 0x04, 0xAA, 0xBB, 0xCC, 0xDD };

        private readonly string _tempFile;

        public KoploperExecutableVerifierTests()
        {
            _tempFile = Path.Combine(Path.GetTempPath(), $"koploper-verifier-{Guid.NewGuid():N}.bin");
            File.WriteAllBytes(_tempFile, FileBytes);
        }

        public void Dispose()
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }

        [Fact]
        public void Verify_MatchingHash_IsSupported()
        {
            string expectedHash = Convert.ToHexString(SHA256.HashData(FileBytes));
            var verifier = new KoploperExecutableVerifier(new TestLayout(expectedHash));

            KoploperVersionGateResult result = verifier.Verify(_tempFile, out KoploperExecutableIdentity? observed);

            Assert.Equal(KoploperVersionGateResult.Supported, result);
            Assert.NotNull(observed);
            Assert.Equal(expectedHash, observed.Sha256Hex);
            Assert.Equal("Koploper", observed.ProductName);
        }

        [Fact]
        public void Verify_DifferentHash_IsUnsupportedVersion()
        {
            var verifier = new KoploperExecutableVerifier(new TestLayout(new string('0', 64)));

            KoploperVersionGateResult result = verifier.Verify(_tempFile, out KoploperExecutableIdentity? observed);

            Assert.Equal(KoploperVersionGateResult.UnsupportedVersion, result);
            Assert.NotNull(observed);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(FileBytes)), observed.Sha256Hex);
        }

        [Fact]
        public void Verify_MissingFile_IsNotPresent()
        {
            var verifier = new KoploperExecutableVerifier(new TestLayout(new string('0', 64)));
            string missing = Path.Combine(Path.GetTempPath(), $"koploper-missing-{Guid.NewGuid():N}.bin");

            KoploperVersionGateResult result = verifier.Verify(missing, out KoploperExecutableIdentity? observed);

            Assert.Equal(KoploperVersionGateResult.NotPresent, result);
            Assert.Null(observed);
        }

        /// <summary>
        /// Minimal layout with a controllable supported hash; the verifier only reads the hash,
        /// version and preferred image base, so the remaining offsets are irrelevant stubs.
        /// </summary>
        private sealed class TestLayout : IKoploperMemoryLayout
        {
            public TestLayout(string supportedSha256Hex)
            {
                SupportedSha256Hex = supportedSha256Hex;
            }

            public string SupportedSha256Hex { get; }
            public string Version => "9.4.0.9";
            public uint PreferredImageBase => 0x00400000u;
            public uint RootPointerRva => 0;
            public uint RootBlockListOffset => 0;
            public uint RootLocoListOffset => 0;
            public uint TListItemsOffset => 0;
            public uint TListCountOffset => 0;
            public uint TListCapacityOffset => 0;
            public uint BlockInternalIdOffset => 0;
            public uint BlockDisplayIdOffset => 0;
            public uint BlockOwnerOffset => 0;
            public uint BlockStateOffset => 0;
            public uint BlockChangedFlagOffset => 0;
            public uint BlockUpdateTickOffset => 0;
            public uint BlockManualBlockedOffset => 0;
            public uint LocoInternalIdOffset => 0;
            public uint LocoBlockRef54Offset => 0;
            public uint LocoBlockRef58Offset => 0;
        }
    }
}
