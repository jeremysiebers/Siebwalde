using System.Globalization;

namespace SiebwaldeApp.Core
{
    /// <summary>Result of a PIC18 firmware-image structural + checksum verification.</summary>
    public sealed class Pic18ChecksumVerificationResult
    {
        public bool IsVerified { get; }

        /// <summary>The 16-bit checksum word extracted from program-memory 0x7FFE/0x7FFF.</summary>
        public ushort EmbeddedChecksum { get; }

        /// <summary>The checksum recomputed over 0x800..0x7FFD (0 when verification failed early).</summary>
        public ushort RecomputedChecksum { get; }

        /// <summary>Human-readable failure reason, or null when verified.</summary>
        public string? Error { get; }

        private Pic18ChecksumVerificationResult(bool isVerified, ushort embedded, ushort recomputed, string? error)
        {
            IsVerified = isVerified;
            EmbeddedChecksum = embedded;
            RecomputedChecksum = recomputed;
            Error = error;
        }

        internal static Pic18ChecksumVerificationResult Verified(ushort embedded)
            => new(true, embedded, embedded, null);

        internal static Pic18ChecksumVerificationResult Failed(ushort embedded, ushort recomputed, string error)
            => new(false, embedded, recomputed, error);
    }

    /// <summary>
    /// Independent Intel HEX structural validation and PIC18 bootloader-checksum verification,
    /// mirroring <c>tools/firmware/checksum-verify.ps1</c> in-tree (a deliberately different code
    /// path from the row-based <c>TrackAmplifierBootloaderHelpers.GetFileCheckSum</c>).
    ///
    /// The PIC18 application firmware embeds a 16-bit little-endian bootloader checksum word at
    /// program-memory 0x7FFE/0x7FFF. The XC8 linker computes it over the program-memory words
    /// 0x800..0x7FFD (the checksum word itself excluded):
    ///
    ///     -mchecksum=800-7FFD@7FFE,width=-2,algorithm=2
    ///
    /// This verifier: (i) validates every Intel HEX record checksum; (ii) rejects overlapping or
    /// out-of-range program-memory data; (iii) requires the full 0x800..0x7FFF range to be present;
    /// (iv) extracts the embedded word; and (v) recomputes the sum over 0x800..0x7FFD and requires
    /// it to equal the embedded word. Configuration records (extended linear addresses &gt;= 0x10000)
    /// are validated for record checksum and overlap but are exempt from the program-memory range.
    /// </summary>
    public static class Pic18HexChecksumVerifier
    {
        public const long BootOffset = 0x800;        // first application program-memory address
        public const long AppEnd = 0x7FFF;           // last application program-memory address
        public const long ChecksumAddress = 0x7FFE;  // 16-bit LE checksum word lives at 0x7FFE/0x7FFF

        /// <summary>Verifies the PIC18 image in <paramref name="path"/>. Never throws.</summary>
        public static Pic18ChecksumVerificationResult Verify(string path)
        {
            try
            {
                var bytes = ReadProgramMemoryMap(path);
                var embedded = ExtractEmbeddedChecksum(bytes);
                var recomputed = RecomputeChecksum(bytes);

                if (embedded != recomputed)
                {
                    return Pic18ChecksumVerificationResult.Failed(
                        embedded,
                        recomputed,
                        $"embedded checksum 0x{embedded:X4} != recomputed 0x{recomputed:X4}");
                }

                return Pic18ChecksumVerificationResult.Verified(embedded);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException or FormatException or ArgumentException)
            {
                return Pic18ChecksumVerificationResult.Failed(0, 0, e.Message);
            }
        }

        /// <summary>
        /// Parses the Intel HEX and returns a sparse map of program-memory bytes (0x800..0x7FFF),
        /// performing all structural validation along the way.
        /// </summary>
        private static Dictionary<long, byte> ReadProgramMemoryMap(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"HEX file not found: {path}", path);
            }

            var lines = File.ReadAllLines(path);
            long baseAddress = 0;
            long segmentAddress = 0;
            var map = new Dictionary<long, byte>();
            var seen = new Dictionary<long, int>(); // address -> line number (for overlap diagnostics)
            int lineNo = 0;

            foreach (var raw in lines)
            {
                lineNo++;
                var line = raw.Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (!line.StartsWith(':'))
                {
                    throw new FormatException($"Intel HEX record at line {lineNo} does not start with ':'.");
                }

                if (((line.Length - 1) % 2) != 0)
                {
                    throw new FormatException($"Intel HEX record at line {lineNo} has an odd number of hex digits.");
                }

                int byteCount = ParseHex(line, 1, 2);
                long addressField = ParseHex(line, 3, 4);
                int type = ParseHex(line, 7, 2);

                int dataStart = 9;
                int checksumPos = dataStart + (byteCount * 2);
                if (line.Length < checksumPos + 2)
                {
                    throw new FormatException($"Intel HEX record at line {lineNo} is truncated.");
                }

                var data = new byte[byteCount];
                for (int i = 0; i < byteCount; i++)
                {
                    data[i] = (byte)ParseHex(line, dataStart + (2 * i), 2);
                }

                // Record checksum: low byte of the sum of every byte (count, addr hi/lo, type, data, checksum) is zero.
                int sum = 0;
                for (int i = 1; i < line.Length; i += 2)
                {
                    sum += ParseHex(line, i, 2);
                }

                bool checksumValid = (sum & 0xFF) == 0;

                if (type == 0x04 && byteCount == 2)
                {
                    baseAddress = ((long)((data[0] << 8) | data[1])) << 16;
                }
                else if (type == 0x02 && byteCount == 2)
                {
                    segmentAddress = ((long)((data[0] << 8) | data[1])) << 4;
                }
                else if (type == 0x00)
                {
                    if (!checksumValid)
                    {
                        throw new FormatException(
                            $"Intel HEX record checksum invalid at line {lineNo} (address 0x{baseAddress + segmentAddress + addressField:X6}).");
                    }

                    long recordAddress = baseAddress + segmentAddress + addressField;
                    for (int i = 0; i < data.Length; i++)
                    {
                        long addr = recordAddress + i;
                        if (seen.TryGetValue(addr, out var priorLine))
                        {
                            throw new FormatException(
                                $"Duplicate/overlapping data byte at address 0x{addr:X6} (lines {priorLine} and {lineNo}).");
                        }

                        seen[addr] = lineNo;

                        // Program-memory range check (configuration space >= 0x10000 is exempt).
                        if (addr < 0x10000)
                        {
                            if (addr < BootOffset || addr > AppEnd)
                            {
                                throw new FormatException(
                                    $"Program-memory data byte at address 0x{addr:X4} (line {lineNo}) is outside 0x{BootOffset:X4}..0x{AppEnd:X4}.");
                            }

                            map[addr] = data[i];
                        }
                    }
                }
                else if (type == 0x01)
                {
                    // EOF; no data. Any following records are ignored by the .ps1 semantics.
                    break;
                }
                // Other record types are ignored.
            }

            if (map.Count == 0)
            {
                throw new FormatException($"No Intel HEX data records found in '{path}'.");
            }

            // Completeness: the checksum word and the whole application range must be present.
            for (long a = BootOffset; a <= AppEnd; a++)
            {
                if (!map.ContainsKey(a))
                {
                    throw new FormatException($"Program-memory image is not contiguous: byte 0x{a:X4} is missing in '{path}'.");
                }
            }

            return map;
        }

        private static ushort ExtractEmbeddedChecksum(Dictionary<long, byte> map)
        {
            int low = map[ChecksumAddress];
            int high = map[ChecksumAddress + 1];
            return (ushort)(low | (high << 8));
        }

        private static ushort RecomputeChecksum(Dictionary<long, byte> map)
        {
            long sum = 0;
            for (long addr = BootOffset; addr < ChecksumAddress; addr += 2)
            {
                int low = map[addr];
                int high = map[addr + 1];
                sum = (sum + low + (high << 8)) & 0xFFFF;
            }

            return (ushort)sum;
        }

        private static int ParseHex(string s, int startIndex, int length)
            => int.Parse(s.Substring(startIndex, length), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
    }
}
