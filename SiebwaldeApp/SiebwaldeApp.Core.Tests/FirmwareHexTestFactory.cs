using System.Text;

namespace SiebwaldeApp.Core.Tests;

/// <summary>
/// Builds complete, valid Intel HEX images for the PIC18 program-memory range (0x800..0x7FFF) so
/// tests can exercise <see cref="Pic18HexChecksumVerifier"/>, <see cref="FirmwareArtifactVerifier"/>
/// and <see cref="TrackAmplifierBootloaderHelpers"/> against a self-contained artifact.
///
/// The image is all-zero except the FIRST program-memory word (0x800/0x801), which is set to
/// <paramref name="checksum"/>, and the embedded checksum word (0x7FFE/0x7FFF), which is set to the
/// same value. Because the only non-zero word in 0x800..0x7FFD is that first word, both the
/// row-based <see cref="TrackAmplifierBootloaderHelpers.GetFileCheckSum"/> and the address-driven
/// <see cref="Pic18HexChecksumVerifier"/> recompute to exactly <paramref name="checksum"/>.
/// </summary>
internal static class FirmwareHexTestFactory
{
    public const long BootOffset = 0x800;
    public const long ChecksumAddress = 0x7FFE;
    public const long AppEnd = 0x7FFF;

    /// <summary>Builds a program-memory byte array whose checksum is exactly <paramref name="checksum"/>.</summary>
    public static byte[] BuildImage(ushort checksum)
    {
        var image = new byte[0x8000];
        image[0x800] = (byte)(checksum & 0xFF);
        image[0x801] = (byte)((checksum >> 8) & 0xFF);
        image[ChecksumAddress] = (byte)(checksum & 0xFF);
        image[ChecksumAddress + 1] = (byte)((checksum >> 8) & 0xFF);
        return image;
    }

    /// <summary>
    /// Emits the image as Intel HEX: 1920 data rows covering 0x800..0x7FFF, one extended-linear
    /// config-word record (at 0x300000, exempt from the program-memory range check), and EOF. The
    /// config record also satisfies <see cref="TrackAmplifierBootloaderHelpers.Execute"/>, which
    /// looks for a 12-byte record after the data rows.
    /// </summary>
    public static string Emit(byte[] image)
    {
        var sb = new StringBuilder();
        for (long addr = BootOffset; addr <= AppEnd; addr += 16)
        {
            var data = new byte[16];
            Array.Copy(image, addr, data, 0, 16);
            sb.AppendLine(BuildRecord((ushort)addr, data));
        }

        sb.AppendLine(":020000040030CA");                                   // extended linear address 0x300000
        sb.AppendLine(":0C00000000FFFFFFFFFFFFFFFFFFFFFFFF00");            // 12-byte config word record
        sb.AppendLine(":00000001FF");                                       // EOF
        return sb.ToString();
    }

    /// <summary>Builds a single Intel HEX data record with a correct record checksum.</summary>
    public static string BuildRecord(ushort address, byte[] data)
    {
        int sum = data.Length + ((address >> 8) & 0xFF) + (address & 0xFF) + 0x00;
        foreach (var b in data)
        {
            sum += b;
        }

        int checksum = (0x100 - (sum & 0xFF)) & 0xFF;

        var sb = new StringBuilder();
        sb.Append(':').Append(data.Length.ToString("X2")).Append(address.ToString("X4")).Append("00");
        foreach (var b in data)
        {
            sb.Append(b.ToString("X2"));
        }

        sb.Append(checksum.ToString("X2"));
        return sb.ToString();
    }
}
