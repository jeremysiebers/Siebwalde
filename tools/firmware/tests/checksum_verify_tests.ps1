<#
.SYNOPSIS
    Negative tests for the PIC18 bootloader-checksum VERIFY step.

.DESCRIPTION
    Loads the independent Intel HEX verification library (checksum-verify.ps1), verifies a
    real built Offset HEX (positive case: the linker-embedded checksum word must equal an
    independent word-sum recompute — the value is image-specific, not hardcoded), then
    generates deliberately corrupted HEX fixtures and asserts the verify step REJECTS each.

    The fixture generator recomputes the expected checksum with the address-driven
    Get-Pic18RecomputedChecksum, an INDEPENDENT code path from the row-based
    Get-CSharpHexChecksum used by build-firmware.ps1, so the test is not self-referential.

    Usage:
        powershell -NoProfile -ExecutionPolicy Bypass -File tools\firmware\tests\checksum_verify_tests.ps1 -HexPath <path-to-offset-hex>

    Exits 0 when every case passes; exits 1 on the first failure.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$HexPath
)

$ErrorActionPreference = 'Stop'

# Load the independent verification library (functions only).
. (Join-Path (Split-Path -Parent $PSScriptRoot) 'checksum-verify.ps1')

$script:Passed = 0
$script:Failed = 0

function Assert-True {
    param([bool]$Condition, [string]$Message)
    if ($Condition) {
        $script:Passed++
        Write-Host ("  PASS: {0}" -f $Message)
    } else {
        $script:Failed++
        Write-Host ("  FAIL: {0}" -f $Message) -ForegroundColor Red
    }
}

function Assert-VerifyRejects {
    param([string]$FixturePath, [string]$CaseName)
    try {
        $null = Test-Pic18ChecksumVerification -Path $FixturePath
        $script:Failed++
        Write-Host ("  FAIL: {0} - verify step accepted a corrupted HEX (should have rejected)." -f $CaseName) -ForegroundColor Red
    } catch {
        $script:Passed++
        Write-Host ("  PASS: {0} - rejected: {1}" -f $CaseName, $_.Exception.Message)
    }
}

# --- Low-level Intel HEX text helpers (fixture generator) ---------------------

function Get-HexParts {
    param([string]$Line)
    $bc  = [Convert]::ToInt32($Line.Substring(1, 2), 16)
    $dataHex = $Line.Substring(9, $bc * 2)
    return [pscustomobject]@{
        ByteCount = $bc
        Address   = [Convert]::ToInt32($Line.Substring(3, 4), 16)
        Type      = $Line.Substring(7, 2)
        DataHex   = $dataHex
        Checksum  = $Line.Substring(9 + $bc * 2, 2)
    }
}

function Get-RecordChecksum {
    # body = bytecount + address + type + data hex (no ':' and no trailing checksum)
    param([string]$Body)
    $sum = 0
    for ($i = 0; $i -lt $Body.Length; $i += 2) {
        $sum += [Convert]::ToInt32($Body.Substring($i, 2), 16)
    }
    $chk = (0x100 - ($sum -band 0xFF)) -band 0xFF
    return ('{0:X2}' -f $chk)
}

function New-FixtureHex {
    param([string[]]$Lines, [string]$Name)
    $tmp = Join-Path ([System.IO.Path]::GetTempPath()) ("pic18_verify_" + $Name + "_" + [System.Guid]::NewGuid().ToString('N') + ".hex")
    [System.IO.File]::WriteAllLines($tmp, $Lines)
    return $tmp
}

# --- Read the real HEX lines once --------------------------------------------
$allLines = [System.IO.File]::ReadAllLines($HexPath)
$firstDataIdx = -1
$lastAppRowIdx = -1
for ($i = 0; $i -lt $allLines.Count; $i++) {
    $l = $allLines[$i].Trim()
    if (-not $l.StartsWith(':')) { continue }
    if ($l.Substring(7, 2) -ne '00') { continue }
    $addr = [Convert]::ToInt32($l.Substring(3, 4), 16)
    if ($firstDataIdx -lt 0) { $firstDataIdx = $i }
    if ($addr -eq 0x7FF0) { $lastAppRowIdx = $i }
}
if ($firstDataIdx -lt 0) { throw "No type-00 data records in $HexPath" }
if ($lastAppRowIdx -lt 0) { throw "No 0x7FF0 application data row in $HexPath" }

Write-Host "=== PIC18 checksum negative tests ==="
Write-Host ("HEX: {0}" -f $HexPath)

# ---------------------------------------------------------------------------
# Positive case: the real built HEX verifies and its checksum is 0xE906.
# ---------------------------------------------------------------------------
Write-Host "Positive case (real built HEX):"
$embedded   = Get-Pic18EmbeddedChecksum -Path $HexPath
$recomputed = Get-Pic18RecomputedChecksum -Path $HexPath
# The durable contract is self-consistency: the linker-embedded word at 0x7FFE/0x7FFF
# must equal an independent word-sum over 0x800..0x7FFD. The concrete value is
# image-specific (0xE906 for current source, 0x251F for the legacy F1 baseline), so
# assert the invariant rather than a hardcoded literal.
Assert-True ($embedded -eq $recomputed) ("embedded checksum word == independent recompute (embedded=0x{0:X4}, recomputed=0x{1:X4})" -f $embedded, $recomputed)
Write-Host ("Positive: embedded=0x{0:X4}, recomputed=0x{1:X4} (self-consistent)" -f $embedded, $recomputed)
try {
    $v = Test-Pic18ChecksumVerification -Path $HexPath
    Assert-True $v.Pass "verify step accepted the real built HEX"
} catch {
    Assert-True $false ("verify step accepted the real built HEX: {0}" -f $_.Exception.Message)
}

# ---------------------------------------------------------------------------
# Negative fixtures.
# ---------------------------------------------------------------------------

# 1) Corrupt an Intel HEX record checksum byte (leave the data intact).
Write-Host "Negative: corrupt record checksum:"
$lines = @($allLines)
$p = Get-HexParts $lines[$firstDataIdx]
$bad = ('{0:X2}' -f (([Convert]::ToInt32($p.Checksum, 16) -bxor 0x01)))
$lines[$firstDataIdx] = ':' + $p.ByteCount.ToString('X2') + $p.Address.ToString('X4') + $p.Type + $p.DataHex + $bad
$f = New-FixtureHex $lines 'badrecord'
Assert-VerifyRejects $f 'corrupt record checksum'

# 2) Corrupt a data byte (keep the record checksum valid so only the 16-bit sum changes).
Write-Host "Negative: corrupt data byte:"
$lines = @($allLines)
$p = Get-HexParts $lines[$firstDataIdx]
$first = [Convert]::ToInt32($p.DataHex.Substring(0, 2), 16)
$tampered = ('{0:X2}' -f ($first -bxor 0x01))
$dataHex = $tampered + $p.DataHex.Substring(2)
$body = $p.ByteCount.ToString('X2') + $p.Address.ToString('X4') + $p.Type + $dataHex
$lines[$firstDataIdx] = ':' + $body + (Get-RecordChecksum $body)
$f = New-FixtureHex $lines 'baddata'
Assert-VerifyRejects $f 'corrupt data byte'

# 3) Truncate the application image (drop the last data row + trailing records).
Write-Host "Negative: truncated image (checksum word removed):"
$lines = @()
for ($i = 0; $i -lt $lastAppRowIdx; $i++) { $lines += $allLines[$i] }
$f = New-FixtureHex $lines 'truncated'
Assert-VerifyRejects $f 'truncated image'

# 4) Duplicate / overlapping data row (repeat the first data row at the same address).
Write-Host "Negative: duplicate/overlapping row:"
$lines = @($allLines)
$dup = $lines[$firstDataIdx]
$lines = @($lines[0..$firstDataIdx]) + @($dup) + @($lines[($firstDataIdx + 1)..($lines.Count - 1)])
$f = New-FixtureHex $lines 'duplicate'
Assert-VerifyRejects $f 'duplicate/overlapping row'

# 5) Out-of-range address (move the first data row into the bootloader region 0x0..0x7FF).
Write-Host "Negative: out-of-range address (bootloader overlap):"
$lines = @($allLines)
$p = Get-HexParts $lines[$firstDataIdx]
$body = $p.ByteCount.ToString('X2') + '07F0' + $p.Type + $p.DataHex
$lines[$firstDataIdx] = ':' + $body + (Get-RecordChecksum $body)
$f = New-FixtureHex $lines 'outofrange'
Assert-VerifyRejects $f 'out-of-range address'

# 6) Corrupt the linker-embedded checksum field (byte at 0x7FFE), record checksum fixed.
Write-Host "Negative: corrupt embedded checksum word (0x7FFE):"
$lines = @($allLines)
$p = Get-HexParts $lines[$lastAppRowIdx]
# data byte offset within the row that maps to absolute address 0x7FFE
$offsetInRow = 0x7FFE - $p.Address
$byteHex = $p.DataHex.Substring($offsetInRow * 2, 2)
$tampered = ('{0:X2}' -f ([Convert]::ToInt32($byteHex, 16) -bxor 0x01))
$dataHex = $p.DataHex.Substring(0, $offsetInRow * 2) + $tampered + $p.DataHex.Substring($offsetInRow * 2 + 2)
$body = $p.ByteCount.ToString('X2') + $p.Address.ToString('X4') + $p.Type + $dataHex
$lines[$lastAppRowIdx] = ':' + $body + (Get-RecordChecksum $body)
$f = New-FixtureHex $lines 'badembedded'
Assert-VerifyRejects $f 'corrupt embedded checksum field'

# ---------------------------------------------------------------------------
Write-Host ""
Write-Host ("Result: {0} passed, {1} failed" -f $script:Passed, $script:Failed)
if ($script:Failed -gt 0) {
    Write-Host "CHECKSUM VERIFY NEGATIVE TESTS: FAIL" -ForegroundColor Red
    exit 1
}
Write-Host "CHECKSUM VERIFY NEGATIVE TESTS: PASS" -ForegroundColor Green
exit 0
