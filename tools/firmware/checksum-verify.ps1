<#
.SYNOPSIS
    Independent Intel HEX structural validation and PIC18 bootloader-checksum
    verification library.

.DESCRIPTION
    The PIC18 application firmware embeds a 16-bit little-endian bootloader checksum
    word at program-memory address 0x7FFE/0x7FFF. The XC8 linker computes it over the
    program-memory words 0x800..0x7FFD (the checksum word itself is excluded):

        -mchecksum=800-7FFD@7FFE,width=-2,algorithm=2

    This library parses the final Intel HEX, validates the Intel HEX record checksums,
    checks address coverage / overlap, extracts the embedded word, and recomputes the
    sum with an address-driven (record-based) algorithm. That recompute is intentionally
    a DIFFERENT code path from the row-based `Get-CSharpHexChecksum` in
    build-firmware.ps1, so the negative tests are not self-referential.

    Program-memory addresses are those below 0x10000. Configuration-word records that use
    an extended linear address (>= 0x10000, e.g. PIC18 config words at 0x300000) are out
    of scope for the program-memory range check but are still record-checksum and overlap
    validated.

    This library only defines functions; it never builds, flashes or touches hardware.
#>

# PIC18 application image geometry (mirrors the C#/linker contract).
# Typed [long] so hashtable lookups against parsed record addresses (also [long]) match.
$script:Pic18BootOffset      = [long]0x800   # first application program-memory address
$script:Pic18AppEnd          = [long]0x7FFF  # last application program-memory address
$script:Pic18ChecksumAddress = [long]0x7FFE  # 16-bit LE checksum word lives at 0x7FFE/0x7FFF

function Get-IntelHexRecords {
    <#
    .SYNOPSIS
        Parses an Intel HEX file into records. Throws on malformed records.

    .OUTPUTS
        One object per type-00 / type-01 record:
            Address        absolute byte address (long); -1 for EOF
            Bytes          byte[] payload
            Type           record type (0 = data, 1 = EOF)
            ChecksumValid  $true when the record's Intel HEX checksum byte is correct
            Line           1-based source line number
    #>
    param([Parameter(Mandatory = $true)][string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "HEX file not found: $Path"
    }

    $lines = [System.IO.File]::ReadAllLines($Path)
    [long]$base = 0
    [long]$seg  = 0
    $records = New-Object System.Collections.Generic.List[object]
    $lineNo = 0

    foreach ($raw in $lines) {
        $lineNo++
        $line = $raw.Trim()
        if ($line -eq '') { continue }

        if (-not $line.StartsWith(':')) {
            throw "Intel HEX record at line $lineNo does not start with ':'."
        }
        if ((($line.Length - 1) % 2) -ne 0) {
            throw "Intel HEX record at line $lineNo has an odd number of hex digits."
        }

        $byteCount = [Convert]::ToInt32($line.Substring(1, 2), 16)
        $addrField = [Convert]::ToInt64($line.Substring(3, 4), 16)
        $type      = [Convert]::ToInt32($line.Substring(7, 2), 16)

        $dataStart   = 9
        $checksumPos = $dataStart + ($byteCount * 2)
        if ($line.Length -lt ($checksumPos + 2)) {
            throw "Intel HEX record at line $lineNo is truncated."
        }

        $bytes = New-Object byte[] $byteCount
        for ($i = 0; $i -lt $byteCount; $i++) {
            $bytes[$i] = [Convert]::ToByte($line.Substring($dataStart + (2 * $i), 2), 16)
        }
        $recordChecksum = [Convert]::ToInt32($line.Substring($checksumPos, 2), 16)

        # Intel HEX record checksum: the low byte of the sum of every byte
        # (byte count, address hi/lo, type, data, checksum) must be zero.
        $sum = 0
        for ($i = 1; $i -lt $line.Length; $i += 2) {
            $sum += [Convert]::ToInt32($line.Substring($i, 2), 16)
        }
        $checksumValid = (($sum -band 0xFF) -eq 0)

        if ($type -eq 0x04 -and $byteCount -eq 2) {
            # Extended linear address: upper 16 bits of the base address.
            $base = ([long](([int]$bytes[0] -shl 8) -bor [int]$bytes[1]) -shl 16)
        }
        elseif ($type -eq 0x02 -and $byteCount -eq 2) {
            # Extended segment address: shifted left 4 bits.
            $seg = ([long](([int]$bytes[0] -shl 8) -bor [int]$bytes[1]) -shl 4)
        }
        elseif ($type -eq 0x00) {
            $records.Add([pscustomobject]@{
                Address        = [long]($base + $seg + $addrField)
                Bytes          = $bytes
                Type           = $type
                ChecksumValid  = $checksumValid
                Line           = $lineNo
            })
        }
        elseif ($type -eq 0x01) {
            $records.Add([pscustomobject]@{
                Address        = [long]-1
                Bytes          = $bytes
                Type           = $type
                ChecksumValid  = $checksumValid
                Line           = $lineNo
            })
        }
        # Other record types are ignored.
    }

    return $records.ToArray()
}

function Test-IntelHexStructure {
    <#
    .SYNOPSIS
        Validates the structural integrity of the PIC18 application image in an Intel HEX.

        (i)   every record's Intel HEX checksum is valid;
        (ii)  no overlapping / duplicate data bytes;
        (iii) every program-memory data byte is within 0x800..0x7FFF (no bootloader
              region 0x0..0x7FF overlap and no program memory above 0x7FFF), and the
              whole 0x800..0x7FFF range is fully covered (no gaps);
        (iv)  the checksum storage word 0x7FFE/0x7FFF is present.

        Configuration records at extended addresses (>= 0x10000) are checked for record
        checksum and overlap but are exempt from the program-memory range check.
    #>
    param([Parameter(Mandatory = $true)][string]$Path)

    $records = @(Get-IntelHexRecords -Path $Path)
    if ($records.Count -eq 0) {
        throw "No Intel HEX data records found in '$Path'."
    }

    $seen = @{}
    foreach ($r in $records) {
        if ($r.Type -ne 0) { continue }

        if (-not $r.ChecksumValid) {
            throw ("Intel HEX record checksum invalid at line {0} (address 0x{1:X6})." -f $r.Line, $r.Address)
        }

        for ($i = 0; $i -lt $r.Bytes.Length; $i++) {
            $addr = $r.Address + $i

            if ($seen.ContainsKey($addr)) {
                throw ("Duplicate/overlapping data byte at address 0x{0:X6} (lines {1} and {2})." -f $addr, $seen[$addr], $r.Line)
            }
            $seen[$addr] = $r.Line

            # Program-memory range check (configuration space >= 0x10000 is exempt).
            if ($addr -lt 0x10000) {
                if ($addr -lt $script:Pic18BootOffset -or $addr -gt $script:Pic18AppEnd) {
                    throw ("Program-memory data byte at address 0x{0:X4} (line {1}) is outside 0x{2:X4}..0x{3:X4}." -f $addr, $r.Line, $script:Pic18BootOffset, $script:Pic18AppEnd)
                }
            }
        }
    }

    # (iv) + completeness: the checksum word and the entire application range must be present.
    if (-not $seen.ContainsKey($script:Pic18ChecksumAddress) -or -not $seen.ContainsKey($script:Pic18ChecksumAddress + 1)) {
        throw ("Checksum storage word 0x{0:X4}/0x{1:X4} is not present in '$Path'." -f $script:Pic18ChecksumAddress, ($script:Pic18ChecksumAddress + 1))
    }
    for ($a = $script:Pic18BootOffset; $a -le $script:Pic18AppEnd; $a++) {
        if (-not $seen.ContainsKey($a)) {
            throw ("Program-memory image is not contiguous: byte 0x{0:X4} is missing in '$Path'." -f $a)
        }
    }

    return $true
}

function Get-Pic18EmbeddedChecksum {
    <#
    .SYNOPSIS
        Extracts the linker-embedded 16-bit little-endian checksum word from 0x7FFE/0x7FFF
        (low byte at 0x7FFE, high byte at 0x7FFF), matching the runtime ReadFlashChecksum.
    #>
    param([Parameter(Mandatory = $true)][string]$Path)

    $records = @(Get-IntelHexRecords -Path $Path)
    [int]$low  = -1
    [int]$high = -1

    foreach ($r in $records) {
        if ($r.Type -ne 0) { continue }
        for ($i = 0; $i -lt $r.Bytes.Length; $i++) {
            $addr = $r.Address + $i
            if ($addr -eq $script:Pic18ChecksumAddress)     { $low  = [int]$r.Bytes[$i] }
            elseif ($addr -eq ($script:Pic18ChecksumAddress + 1)) { $high = [int]$r.Bytes[$i] }
        }
    }

    if ($low -lt 0 -or $high -lt 0) {
        throw ("Checksum storage word 0x{0:X4}/0x{1:X4} is not present in '$Path'." -f $script:Pic18ChecksumAddress, ($script:Pic18ChecksumAddress + 1))
    }

    return ($low -bor ($high -shl 8))
}

function Get-Pic18RecomputedChecksum {
    <#
    .SYNOPSIS
        Recomputes the PIC18 bootloader checksum over 0x800..0x7FFD using an
        address-driven (record-based) algorithm. This is an INDEPENDENT code path from the
        row-based Get-CSharpHexChecksum in build-firmware.ps1, so a shared bug cannot mask
        a mismatch. Sums little-endian 16-bit words, mod 0x10000.
    #>
    param([Parameter(Mandatory = $true)][string]$Path)

    $records = @(Get-IntelHexRecords -Path $Path)

    $map = @{}
    foreach ($r in $records) {
        if ($r.Type -ne 0) { continue }
        for ($i = 0; $i -lt $r.Bytes.Length; $i++) {
            $addr = $r.Address + $i
            if ($addr -ge $script:Pic18BootOffset -and $addr -le $script:Pic18AppEnd) {
                $map[$addr] = [int]$r.Bytes[$i]
            }
        }
    }

    [long]$sum = 0
    for ($addr = $script:Pic18BootOffset; $addr -lt $script:Pic18ChecksumAddress; $addr += 2) {
        $lo = $map[$addr]
        $hi = $map[$addr + 1]
        $sum = ($sum + $lo + ($hi -shl 8)) -band 0xFFFF
    }

    return [int]$sum
}

function Test-Pic18ChecksumVerification {
    <#
    .SYNOPSIS
        Full independent PIC18 checksum verification: structural checks + "embedded word ==
        recomputed sum". Throws (with the exact failure) on any problem; returns a result
        object on success. The caller (build-firmware.ps1) additionally cross-checks the
        established Get-CSharpHexChecksum value against the same embedded word.
    #>
    param([Parameter(Mandatory = $true)][string]$Path)

    $null = Test-IntelHexStructure -Path $Path
    $embedded   = Get-Pic18EmbeddedChecksum -Path $Path
    $recomputed = Get-Pic18RecomputedChecksum -Path $Path

    if ($embedded -ne $recomputed) {
        throw ("PIC18 checksum VERIFY FAILED for '{0}': embedded word = 0x{1:X4}, recomputed = 0x{2:X4}." -f $Path, $embedded, $recomputed)
    }

    return [pscustomobject]@{
        Pass            = $true
        EmbeddedChecksum = $embedded
        RecomputedChecksum = $recomputed
        ChecksumAddress = $script:Pic18ChecksumAddress
    }
}
