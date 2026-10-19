using System.Security.Cryptography;
using Xunit;

namespace SiebwaldeApp.Core.Tests;

public class FirmwareArtifactVerificationTests
{
    private static string WriteTemp(string content)
    {
        var path = Path.Combine(Path.GetTempPath(), $"siebwalde-fwtest-{Guid.NewGuid():N}.hex");
        File.WriteAllText(path, content);
        return path;
    }

    private static string Sha256Hex(string path)
        => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    [Fact]
    public void Pic18ChecksumVerifier_ValidImage_PassesAndExtractsChecksum()
    {
        const ushort target = 0xE906;
        var content = FirmwareHexTestFactory.Emit(FirmwareHexTestFactory.BuildImage(target));
        var path = WriteTemp(content);
        try
        {
            var result = Pic18HexChecksumVerifier.Verify(path);
            Assert.True(result.IsVerified, result.Error);
            Assert.Equal(target, result.EmbeddedChecksum);
            Assert.Equal(target, result.RecomputedChecksum);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Pic18ChecksumVerifier_TamperedData_Fails()
    {
        const ushort target = 0xE906;
        var image = FirmwareHexTestFactory.BuildImage(target);
        image[0x800] ^= 0xFF; // tamper the only non-zero data word; record checksums are re-emitted correctly
        var content = FirmwareHexTestFactory.Emit(image);
        var path = WriteTemp(content);
        try
        {
            var result = Pic18HexChecksumVerifier.Verify(path);
            Assert.False(result.IsVerified);
            Assert.Contains("checksum", result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Pic18ChecksumVerifier_CorruptRecordChecksum_Fails()
    {
        const ushort target = 0xE906;
        var content = FirmwareHexTestFactory.Emit(FirmwareHexTestFactory.BuildImage(target));
        var firstLineEnd = content.IndexOf('\n');
        var corrupt = content.Substring(0, firstLineEnd - 3) + "00" + content.Substring(firstLineEnd - 1);
        var path = WriteTemp(corrupt);
        try
        {
            var result = Pic18HexChecksumVerifier.Verify(path);
            Assert.False(result.IsVerified);
            Assert.Contains("record checksum", result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Pic18ChecksumVerifier_MissingByte_Fails()
    {
        const ushort target = 0xE906;
        var content = FirmwareHexTestFactory.Emit(FirmwareHexTestFactory.BuildImage(target));
        var firstLineEnd = content.IndexOf('\n');
        var withoutFirstRecord = content.Substring(firstLineEnd + 1); // gap at 0x800
        var path = WriteTemp(withoutFirstRecord);
        try
        {
            var result = Pic18HexChecksumVerifier.Verify(path);
            Assert.False(result.IsVerified);
            Assert.Contains("not contiguous", result.Error);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FirmwareDeploymentArtifactVerifier_ValidArtifact_Verified()
    {
        const ushort checksum = 0xE906;
        var content = FirmwareHexTestFactory.Emit(FirmwareHexTestFactory.BuildImage(checksum));
        var path = WriteTemp(content);
        try
        {
            var identity = new FirmwareArtifactIdentity(path, Sha256Hex(path), checksum);
            var result = FirmwareDeploymentArtifactVerifier.Verify(identity);
            Assert.True(result.IsVerified, result.Reason);
            Assert.Equal(checksum, result.EmbeddedChecksum);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FirmwareDeploymentArtifactVerifier_WrongSha256_Fails()
    {
        const ushort checksum = 0xE906;
        var content = FirmwareHexTestFactory.Emit(FirmwareHexTestFactory.BuildImage(checksum));
        var path = WriteTemp(content);
        try
        {
            var identity = new FirmwareArtifactIdentity(path, new string('0', 64), checksum);
            var result = FirmwareDeploymentArtifactVerifier.Verify(identity);
            Assert.False(result.IsVerified);
            Assert.Contains("SHA-256 mismatch", result.Reason);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FirmwareDeploymentArtifactVerifier_WrongExpectedChecksum_Fails()
    {
        const ushort checksum = 0xE906;
        var content = FirmwareHexTestFactory.Emit(FirmwareHexTestFactory.BuildImage(checksum));
        var path = WriteTemp(content);
        try
        {
            var identity = new FirmwareArtifactIdentity(path, Sha256Hex(path), 0xAAAA);
            var result = FirmwareDeploymentArtifactVerifier.Verify(identity);
            Assert.False(result.IsVerified);
            Assert.Contains("checksum", result.Reason);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void FirmwareDeploymentArtifactVerifier_MissingFile_Fails()
    {
        var identity = new FirmwareArtifactIdentity(
            Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.hex"),
            new string('0', 64),
            0xE906);
        var result = FirmwareDeploymentArtifactVerifier.Verify(identity);
        Assert.False(result.IsVerified);
        Assert.Contains("not found", result.Reason);
    }
}
