<#
.SYNOPSIS
    Reproducible, committed command-line build harness for the Siebwalde PIC18/PIC32 firmware.

.DESCRIPTION
    Builds the MPLAB X application projects (PIC18 TrackAmplifier4.X / TrackBackplane2.X,
    PIC18 bootloader TrackAmplifierBootLoader.X, PIC32 TrackController5) from a clean
    out-of-tree copy, using pinned, absolute toolchain paths. It never builds in place and
    never writes into the tracked tree apart from build outputs under `build/`.

    Per build it emits a machine-readable manifest JSON under <OutputDir>/<rev>/manifest.json
    and copies the produced HEX into <OutputDir>/<rev>/<project>/<config>/.

    The harness is read-only with respect to the repository: it does not flash, program or
    connect to hardware, and it does not modify firmware source, tracked artifacts, docs or
    git state.

.PARAMETER Project
    Which project to build: TrackAmplifier4 | TrackBackplane2 | Bootloader | TrackController5 | All.

.PARAMETER Configuration
    Optional configuration name. Defaults per project:
      TrackAmplifier4     -> Offset
      TrackBackplane2     -> default
      Bootloader          -> No_Configurations
      TrackController5    -> Production

.PARAMETER OutputDir
    Base output directory. Defaults to <repo>\build\firmware\out.

.PARAMETER KeepTemp
    Keep the clean temporary build copy under build\firmware\tmp\<rev>\ after the run.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\firmware\build-firmware.ps1 -Project All

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File tools\firmware\build-firmware.ps1 -Project TrackAmplifier4 -Configuration Offset -KeepTemp
#>
[CmdletBinding()]
param(
    [ValidateSet('TrackAmplifier4', 'TrackBackplane2', 'Bootloader', 'TrackController5', 'All')]
    [string]$Project = 'All',

    [string]$Configuration = '',

    [string]$OutputDir = '',

    [switch]$KeepTemp
)

$ErrorActionPreference = 'Stop'

# ---------------------------------------------------------------------------------
# Pinned toolchain matrix (absolute paths).
# ---------------------------------------------------------------------------------
$Toolchain = [ordered]@{
    MplabxApp  = 'C:\Program Files\Microchip\MPLABX\v6.20'
    MplabxBoot = 'C:\Program Files\Microchip\MPLABX\v6.05'
    Xc8App     = 'C:\Program Files\Microchip\xc8\v2.31'
    Xc8Boot    = 'C:\Program Files\Microchip\xc8\v2.40'
    Xc32App    = 'C:\Program Files\Microchip\xc32\v2.50'
    Harmony    = 'C:\Microchip\harmony\v2_06'
    DfpPic18Boot = 'C:\Program Files\Microchip\MPLABX\v6.05\packs\Microchip\PIC18F-K_DFP\1.7.134'
    DfpPic32   = 'C:\Program Files\Microchip\MPLABX\v6.20\packs\Microchip\PIC32MZ-EF_DFP\1.4.168'
}

# ---------------------------------------------------------------------------------
# Repository / directory layout.
# ---------------------------------------------------------------------------------
$RepoRoot  = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
if ([string]::IsNullOrEmpty($OutputDir)) { $OutputDir = Join-Path $RepoRoot 'build\firmware\out' }
$BuildRoot = Join-Path $RepoRoot 'build\firmware'
$TempBase  = Join-Path $BuildRoot 'tmp'

# ---------------------------------------------------------------------------------
# Project definitions.
#   Copies        : source (repo-relative) -> destination (temp-root-relative)
#   ProjectDir    : project directory relative to the temp root
#   ArtifactRel   : artifact path relative to ProjectDir ('{conf}' substituted)
#   RequiresHarmony: create a temp junction so the committed relative Harmony path resolves
#   Checksum      : also compute the C# host flash checksum of the artifact
# ---------------------------------------------------------------------------------
$ProjectDefs = [ordered]@{
    'TrackAmplifier4' = [ordered]@{
        DefaultConf     = 'Offset'
        MplabxVersion   = 'v6.20'
        Compiler        = 'XC8'
        CompilerVersion = '2.31'
        CompilerBin     = (Join-Path $Toolchain.Xc8App 'bin')
        Device          = 'PIC18F25K40'
        Dfp             = 'PIC18F-K_DFP 1.0.48 (project pin)'
        Copies          = @(
            [ordered]@{ From = 'TrackAmplifier4.X';          To = 'TrackAmplifier4.X' }
            [ordered]@{ From = 'TrackAmplifierBootLoader.X'; To = 'TrackAmplifierBootLoader.X' }
        )
        ProjectDir      = 'TrackAmplifier4.X'
        ArtifactRel     = 'dist/{conf}/production/TrackAmplifier4.X.production.hex'
        RequiresHarmony = $false
        Checksum        = $true
    }
    'TrackBackplane2' = [ordered]@{
        DefaultConf     = 'default'
        MplabxVersion   = 'v6.20'
        Compiler        = 'XC8'
        CompilerVersion = '2.31'
        CompilerBin     = (Join-Path $Toolchain.Xc8App 'bin')
        Device          = 'PIC18F25K40'
        Dfp             = 'PIC18F-K_DFP 1.0.48 (project pin)'
        Copies          = @(
            [ordered]@{ From = 'TrackBackplane2.X'; To = 'TrackBackplane2.X' }
        )
        ProjectDir      = 'TrackBackplane2.X'
        ArtifactRel     = 'dist/{conf}/production/TrackBackplane2.X.production.hex'
        RequiresHarmony = $false
        Checksum        = $false
    }
    'Bootloader' = [ordered]@{
        DefaultConf     = 'No_Configurations'
        MplabxVersion   = 'v6.05'
        Compiler        = 'XC8'
        CompilerVersion = '2.40'
        CompilerBin     = (Join-Path $Toolchain.Xc8Boot 'bin')
        Device          = 'PIC18F25K40'
        Dfp             = 'PIC18F-K_DFP 1.7.134'
        Copies          = @(
            [ordered]@{ From = 'TrackAmplifierBootLoader.X'; To = 'TrackAmplifierBootLoader.X' }
        )
        ProjectDir      = 'TrackAmplifierBootLoader.X'
        ArtifactRel     = 'dist/{conf}/production/TrackAmplifierBootLoader.X.production.hex'
        RequiresHarmony = $false
        Checksum        = $false
    }
    'TrackController5' = [ordered]@{
        DefaultConf     = 'Production'
        MplabxVersion   = 'v6.20'
        Compiler        = 'XC32'
        CompilerVersion = '2.50'
        CompilerBin     = (Join-Path $Toolchain.Xc32App 'bin')
        Device          = 'PIC32MZ2048EFH144'
        Dfp             = 'PIC32MZ-EF_DFP 1.4.168'
        Copies          = @(
            [ordered]@{ From = 'TrackController5\firmware'; To = 'TrackController5\firmware' }
        )
        ProjectDir      = 'TrackController5\firmware\TrackController5.X'
        ArtifactRel     = 'dist/{conf}/production/TrackController5.X.production.hex'
        RequiresHarmony = $true
        Checksum        = $false
    }
}

# TrackAmplifier4.X host-checksum constants (must match the C# application).
# See SiebwaldeApp.Core/Model/TrackApplication/Services/PublicEnums.cs (Enums)
# and .../Controller/TrackAmplifierBootloaderHelpers.cs (Execute()).
$ChecksumConstants = [ordered]@{
    PROGEMSIZE      = 0x8000
    BOOTLOADEROFFSET = 0x800
    HEXROWWIDTH     = 16
    JUMPSIZE        = 4
}

# ---------------------------------------------------------------------------------
# Helper functions.
# ---------------------------------------------------------------------------------
function Write-Log {
    param([string]$Message)
    Write-Host ("[build-firmware] " + $Message)
}

function Invoke-Process {
    # Runs a native executable, captures stdout+stderr to $LogPath, returns the exit code.
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [string]$ArgumentString = '',
        [string]$WorkingDirectory = $RepoRoot,
        [string]$LogPath,
        [string]$PathPrefix = ''
    )
    if (-not (Test-Path -LiteralPath $FilePath)) {
        throw "Executable not found: $FilePath"
    }
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $FilePath
    $psi.Arguments = $ArgumentString
    $psi.WorkingDirectory = $WorkingDirectory
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    if ($PathPrefix -ne '') {
        $psi.EnvironmentVariables['PATH'] = $PathPrefix + ';' + $env:PATH
    }
    $proc = [System.Diagnostics.Process]::Start($psi)
    $stdout = $proc.StandardOutput.ReadToEnd()
    $stderr = $proc.StandardError.ReadToEnd()
    $proc.WaitForExit()
    if ($LogPath) {
        ($stdout + $stderr) | Set-Content -LiteralPath $LogPath -Encoding ASCII
    }
    return $proc.ExitCode
}

function Invoke-Generator {
    # prjMakefilesGenerator.bat must be launched through cmd.exe; the call operator does this
    # reliably and sets $LASTEXITCODE.
    param(
        [Parameter(Mandatory = $true)][string]$Generator,
        [Parameter(Mandatory = $true)][string]$ProjectDirectory,
        [Parameter(Mandatory = $true)][string]$LogPath
    )
    if (-not (Test-Path -LiteralPath $Generator)) {
        throw "prjMakefilesGenerator not found: $Generator"
    }
    $oldEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $out = & $Generator $ProjectDirectory 2>&1
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $oldEap
    }
    ($out | Out-String) | Set-Content -LiteralPath $LogPath -Encoding ASCII
    return $code
}

function Invoke-Robocopy {
    param(
        [Parameter(Mandatory = $true)][string]$Source,
        [Parameter(Mandatory = $true)][string]$Destination
    )
    if (-not (Test-Path -LiteralPath $Source)) { throw "Copy source not found: $Source" }
    $null = New-Item -ItemType Directory -Path $Destination -Force
    # robocopy exit codes 0-7 indicate success (bit flags for copied/extra files).
    $oldEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        & robocopy $Source $Destination /E /NFL /NDL /NJH /NJS /NP /R:1 /W:1 | Out-Null
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $oldEap
    }
    if ($code -gt 7) { throw "robocopy failed ($code) copying '$Source' -> '$Destination'" }
}

function Get-CSharpHexChecksum {
    # Faithful port of TrackAmplifierBootloaderHelpers.Execute():
    #   ProcessLines = (PROGEMSIZE - BOOTLOADEROFFSET) / HEXROWWIDTH
    #   sum over the first ProcessLines 16-byte rows, little-endian words, skipping the final
    #   2 bytes (the checksum storage location itself), modulo 0xFFFF.
    param([Parameter(Mandatory = $true)][string]$Path)
    [int]$progMemSize   = $ChecksumConstants.PROGEMSIZE
    [int]$bootOffset    = $ChecksumConstants.BOOTLOADEROFFSET
    [int]$hexRowWidth   = $ChecksumConstants.HEXROWWIDTH
    [int]$processLines  = [int](($progMemSize - $bootOffset) / $hexRowWidth)
    $lines = [System.IO.File]::ReadAllLines($Path)
    if ($lines.Count -lt $processLines) {
        throw "HEX file '$Path' has fewer than $processLines data rows."
    }
    [int]$sum = 0
    for ($c = 1; $c -le $processLines; $c++) {
        $line = $lines[$c - 1]
        for ($i = 0; $i -lt 16; $i += 2) {
            if (-not ($c -eq $processLines -and $i -eq 14)) {
                $lo = [int][Convert]::ToByte($line.Substring(9 + 2 * $i, 2), 16)
                $hi = [int][Convert]::ToByte($line.Substring(9 + 2 * ($i + 1), 2), 16)
                $sum = ($sum + $lo + ($hi -shl 8)) -band 0xFFFF
            }
        }
    }
    return $sum
}

function Get-HexDataRange {
    # Parses an Intel HEX file and returns the lowest/highest absolute data-record addresses
    # (record type 00), the program-region (< 0x10000) range, and the record count below the
    # application boot offset.
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [int]$BootOffset = 0x800
    )
    $lines = [System.IO.File]::ReadAllLines($Path)
    [int64]$base = 0
    [int64]$seg = 0
    $records = New-Object System.Collections.ArrayList
    foreach ($l in $lines) {
        if ($l.Length -lt 11) { continue }
        $type = $l.Substring(7, 2)
        if ($type -eq '04') {
            $base = [Convert]::ToInt64($l.Substring(9, 4), 16) -shl 16
        } elseif ($type -eq '02') {
            $seg = [Convert]::ToInt64($l.Substring(9, 4), 16) -shl 4
        } elseif ($type -eq '00') {
            $off = [Convert]::ToInt64($l.Substring(3, 4), 16)
            $len = [Convert]::ToInt64($l.Substring(1, 2), 16)
            [void]$records.Add([pscustomobject]@{ Start = [int64]($base + $seg + $off); Len = $len })
        }
    }
    if ($records.Count -eq 0) { throw "No data records in '$Path'." }
    $lowest  = [int64](($records | Measure-Object -Property Start -Minimum).Minimum)
    $highest = [int64](($records | ForEach-Object { $_.Start + $_.Len - 1 } | Measure-Object -Maximum).Maximum)
    $prog = @($records | Where-Object { $_.Start -lt 0x10000 })
    $progLow  = [int64](($prog | Measure-Object -Property Start -Minimum).Minimum)
    $progHigh = [int64](($prog | ForEach-Object { $_.Start + $_.Len - 1 } | Measure-Object -Maximum).Maximum)
    $below = @($records | Where-Object { $_.Start -lt $BootOffset })
    return [pscustomobject]@{
        RecordCount          = $records.Count
        LowestAddress        = $lowest
        HighestAddress       = $highest
        ProgramLowAddress    = $progLow
        ProgramHighAddress   = $progHigh
        RecordsBelowBootOffset = $below.Count
    }
}

function Get-GitInfo {
    $full  = (& git -C $RepoRoot rev-parse HEAD).Trim()
    $short = (& git -C $RepoRoot rev-parse --short HEAD).Trim()
    $porcelain = @(& git -C $RepoRoot status --porcelain --untracked-files=no)
    return [pscustomobject]@{
        FullSha  = $full
        ShortSha = $short
        Dirty    = ($porcelain.Count -gt 0)
        Porcelain = $porcelain
    }
}

function Get-DistHashes {
    $dist = Join-Path $RepoRoot 'TrackAmplifier4.X\dist'
    if (-not (Test-Path -LiteralPath $dist)) { return @() }
    return @(Get-ChildItem -LiteralPath $dist -Recurse -File | Sort-Object FullName | ForEach-Object {
        [pscustomobject]@{ Path = $_.FullName.Substring($RepoRoot.Length); Sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash }
    })
}

# ---------------------------------------------------------------------------------
# Pre-flight: git reality, tracked artefact baseline, toolchain presence.
# ---------------------------------------------------------------------------------
$gitBefore = Get-GitInfo
$distBefore = Get-DistHashes

$targets = @()
if ($Project -eq 'All') { $targets = @('TrackAmplifier4', 'TrackBackplane2', 'Bootloader', 'TrackController5') }
else { $targets = @($Project) }

# ---------------------------------------------------------------------------------
# Main build loop.
# ---------------------------------------------------------------------------------
$rev = $gitBefore.ShortSha
$TempRoot = Join-Path $TempBase $rev
$OutRev = Join-Path $OutputDir $rev
$null = New-Item -ItemType Directory -Path $TempRoot -Force
$null = New-Item -ItemType Directory -Path $OutRev -Force
$logDir = Join-Path $TempRoot '_logs'
$null = New-Item -ItemType Directory -Path $logDir -Force

$manifestBuilds = New-Object System.Collections.ArrayList
$failed = $false

foreach ($key in $targets) {
    $def = $ProjectDefs[$key]
    $conf = $Configuration
    if ([string]::IsNullOrEmpty($conf)) { $conf = $def.DefaultConf }

    Write-Log ("=== $key / $conf ===")

    $mplabxDir = Join-Path 'C:\Program Files\Microchip\MPLABX' $def.MplabxVersion
    $genBat    = Join-Path $mplabxDir 'mplab_platform\bin\prjMakefilesGenerator.bat'
    $makeExe   = Join-Path $mplabxDir 'gnuBins\GnuWin32\bin\make.exe'

    if (-not (Test-Path -LiteralPath $genBat))  { throw "Missing generator: $genBat" }
    if (-not (Test-Path -LiteralPath $makeExe)) { throw "Missing make: $makeExe" }
    if (-not (Test-Path -LiteralPath $def.CompilerBin)) { throw "Missing compiler bin: $($def.CompilerBin)" }

    # 1) Clean temp copy.
    $projTempDir = Join-Path $TempRoot $def.ProjectDir
    foreach ($copy in $def.Copies) {
        $dest = Join-Path $TempRoot $copy.To
        if (Test-Path -LiteralPath $dest) { Remove-Item -LiteralPath $dest -Recurse -Force }
    }
    foreach ($copy in $def.Copies) {
        $src = Join-Path $RepoRoot $copy.From
        $dest = Join-Path $TempRoot $copy.To
        Invoke-Robocopy -Source $src -Destination $dest
    }

    # 2) Harmony junction so the committed relative path resolves inside the temp copy.
    $harmonyNote = ''
    if ($def.RequiresHarmony) {
        if (-not (Test-Path -LiteralPath $Toolchain.Harmony)) {
            throw "BLOCKER: Harmony not found at $($Toolchain.Harmony)"
        }
        # The project makefiles reference ../../../../../microchip/harmony/v2_06 relative to the
        # project directory; compute that base and point a junction at C:\Microchip.
        $harmBase = [System.IO.Path]::GetFullPath((Join-Path $projTempDir '..\..\..\..\..\microchip'))
        if (-not (Test-Path -LiteralPath $harmBase)) {
            $null = New-Item -ItemType Directory -Path (Split-Path -Parent $harmBase) -Force
            New-Item -ItemType Junction -Path $harmBase -Target 'C:\Microchip' | Out-Null
        }
        $harmonyNote = "junction $harmBase -> C:\Microchip"
    }

    # 3) Generate MPLAB makefiles into the temp copy.
    $genLog = Join-Path $logDir ($key + '-' + $conf + '-generator.log')
    $genCmd = '"' + $genBat + '" "' + $projTempDir + '"'
    $genCode = Invoke-Generator -Generator $genBat -ProjectDirectory $projTempDir -LogPath $genLog
    Write-Log ("prjMakefilesGenerator exit=$genCode")
    $commands = New-Object System.Collections.ArrayList
    [void]$commands.Add([ordered]@{ Step = 'generate-makefiles'; Command = $genCmd; WorkingDirectory = $RepoRoot; ExitCode = $genCode; Log = $genLog })
    if ($genCode -ne 0) {
        throw "BLOCKER: prjMakefilesGenerator failed (exit $genCode) for $key/$conf"
    }

    # 4) Build. Use .build-conf with SUB=no so no sub-project (bootloader) is built.
    $artifactTemp = Join-Path $projTempDir ($def.ArtifactRel.Replace('{conf}', $conf).Replace('/', '\'))
    if (Test-Path -LiteralPath $artifactTemp) { Remove-Item -LiteralPath $artifactTemp -Force }
    $makeArgs = '-f nbproject/Makefile-' + $conf + '.mk SUB=no .build-conf'
    $buildLog = Join-Path $logDir ($key + '-' + $conf + '-build.log')
    $buildCmd = '"' + $makeExe + '" ' + $makeArgs
    Write-Log ("make $makeArgs")
    $buildCode = Invoke-Process -FilePath $makeExe -ArgumentString $makeArgs -WorkingDirectory $projTempDir -LogPath $buildLog -PathPrefix $def.CompilerBin
    Write-Log ("make exit=$buildCode")
    [void]$commands.Add([ordered]@{ Step = 'build'; Command = $buildCmd; WorkingDirectory = $projTempDir; ExitCode = $buildCode; Log = $buildLog })
    if ($buildCode -ne 0) {
        throw "BLOCKER: build failed (exit $buildCode) for $key/$conf. See $buildLog"
    }

    # 5) Locate, hash and publish the artifact.
    if (-not (Test-Path -LiteralPath $artifactTemp)) {
        throw "Build reported success but artifact not found: $artifactTemp"
    }
    $artifactOutDir = Join-Path (Join-Path $OutRev $key) $conf
    $null = New-Item -ItemType Directory -Path $artifactOutDir -Force
    $artifactName = Split-Path -Leaf $artifactTemp
    $artifactOut = Join-Path $artifactOutDir $artifactName
    Copy-Item -LiteralPath $artifactTemp -Destination $artifactOut -Force
    Copy-Item -LiteralPath $buildLog -Destination (Join-Path $artifactOutDir 'build.log') -Force
    $sha = (Get-FileHash -LiteralPath $artifactTemp -Algorithm SHA256).Hash
    $size = (Get-Item -LiteralPath $artifactTemp).Length

    $entry = [ordered]@{
        Project       = $key
        Configuration = $conf
        Device        = $def.Device
        MplabxVersion = $def.MplabxVersion
        Compiler      = ($def.Compiler + ' ' + $def.CompilerVersion)
        CompilerBin   = $def.CompilerBin
        DevicePack    = $def.Dfp
        Harmony       = $harmonyNote
        Status        = 'PASS'
        Artifact = [ordered]@{
            SourcePath = $artifactTemp
            OutputPath = $artifactOut
            FileName   = $artifactName
            SizeBytes  = $size
            Sha256     = $sha
        }
        Commands = $commands
    }

    if ($def.Checksum) {
        $chk = Get-CSharpHexChecksum -Path $artifactTemp
        $range = Get-HexDataRange -Path $artifactTemp -BootOffset $ChecksumConstants.BOOTLOADEROFFSET
        $entry['Checksum16'] = ('0x{0:X4}' -f $chk)
        $entry['ChecksumAlgorithm'] = 'TrackAmplifierBootloaderHelpers.Execute (PROGEMSIZE=0x8000, BOOTLOADEROFFSET=0x800, HEXROWWIDTH=16, JUMPSIZE=4; sum of little-endian words, skipping final 2 bytes, mod 0xFFFF)'
        $entry['HexRange'] = [ordered]@{
            LowestAddress          = ('0x{0:X6}' -f $range.LowestAddress)
            HighestAddress         = ('0x{0:X6}' -f $range.HighestAddress)
            ProgramLowAddress      = ('0x{0:X4}' -f $range.ProgramLowAddress)
            ProgramHighAddress     = ('0x{0:X4}' -f $range.ProgramHighAddress)
            RecordCount            = $range.RecordCount
            RecordsBelowBootOffset = $range.RecordsBelowBootOffset
        }
    }

    [void]$manifestBuilds.Add($entry)
    Write-Log ("PASS $key/$conf  sha256=$sha  size=$size")
}

# ---------------------------------------------------------------------------------
# Post-flight: verify tracked tree untouched, write manifest.
# ---------------------------------------------------------------------------------
$gitAfter = Get-GitInfo
$distAfter = Get-DistHashes

$distChanged = @()
foreach ($b in $distBefore) {
    $a = $distAfter | Where-Object { $_.Path -eq $b.Path } | Select-Object -First 1
    if ($null -eq $a -or $a.Sha256 -ne $b.Sha256) { $distChanged += $b.Path }
}
foreach ($a in $distAfter) {
    if (-not ($distBefore | Where-Object { $_.Path -eq $a.Path })) { $distChanged += $a.Path }
}

if ($gitAfter.Porcelain.Count -gt 0) { throw "Tracked tree is not clean after build: $($gitAfter.Porcelain -join '; ')" }
if ($distChanged.Count -gt 0) { throw "TrackAmplifier4.X/dist changed during build: $($distChanged -join '; ')" }

$compilerVersions = [ordered]@{}
foreach ($tool in @(
        [ordered]@{ Name = 'XC8 2.31'; Exe = (Join-Path $Toolchain.Xc8App 'bin\xc8-cc.exe') }
        [ordered]@{ Name = 'XC8 2.40'; Exe = (Join-Path $Toolchain.Xc8Boot 'bin\xc8-cc.exe') }
        [ordered]@{ Name = 'XC32 2.50'; Exe = (Join-Path $Toolchain.Xc32App 'bin\xc32-gcc.exe') })) {
    if (Test-Path -LiteralPath $tool.Exe) {
        $vlog = Join-Path $logDir (($tool.Name -replace '[^A-Za-z0-9]', '_') + '_version.log')
        $null = Invoke-Process -FilePath $tool.Exe -ArgumentString '--version' -WorkingDirectory $RepoRoot -LogPath $vlog
        $v = (Get-Content -LiteralPath $vlog -ErrorAction SilentlyContinue | Where-Object { $_ -match '\S' } | Select-Object -First 3) -join ' | '
        $compilerVersions[$tool.Name] = $v
    }
}

$manifest = [ordered]@{
    SchemaVersion = 1
    Tool          = 'tools/firmware/build-firmware.ps1'
    Revision      = [ordered]@{
        FullSha = $gitBefore.FullSha
        ShortSha = $gitBefore.ShortSha
        Dirty   = $gitBefore.Dirty
    }
    GeneratedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    Host           = [ordered]@{ ComputerName = $env:COMPUTERNAME; User = $env:USERNAME }
    ToolchainMatrix = [ordered]@{
        MplabxApp  = $Toolchain.MplabxApp
        MplabxBoot = $Toolchain.MplabxBoot
        Xc8App     = $Toolchain.Xc8App
        Xc8Boot    = $Toolchain.Xc8Boot
        Xc32App    = $Toolchain.Xc32App
        Harmony    = $Toolchain.Harmony
        DfpPic18Boot = $Toolchain.DfpPic18Boot
        DfpPic32   = $Toolchain.DfpPic32
        CompilerVersionStrings = $compilerVersions
    }
    ChecksumConstants = $ChecksumConstants
    Verification = [ordered]@{
        GitCleanAfterRun        = ($gitAfter.Porcelain.Count -eq 0)
        TrackAmplifier4DistUnchanged = ($distChanged.Count -eq 0)
    }
    Builds = @($manifestBuilds)
}

$manifestPath = Join-Path $OutRev 'manifest.json'
($manifest | ConvertTo-Json -Depth 10) | Set-Content -LiteralPath $manifestPath -Encoding UTF8

Write-Log ("manifest: $manifestPath")

# Temp cleanup.
if (-not $KeepTemp) {
    if (Test-Path -LiteralPath $TempRoot) { Remove-Item -LiteralPath $TempRoot -Recurse -Force -ErrorAction SilentlyContinue }
    Write-Log "temp removed (use -KeepTemp to retain)"
} else {
    Write-Log ("temp retained: $TempRoot")
}

Write-Log "DONE"
$manifest
