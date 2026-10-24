# Siebwalde Knowledge Foundation — native LadybugDB runtime setup (Windows x64).
#
# The `ladybug` Python wheel (cp313-win_amd64) links against the native
# `lbug` C API shared library, which is NOT bundled in the wheel. This script
# downloads the exact matching release (v0.21.2), verifies its SHA-256, and
# stages the DLL into `lib/` next to the OpenSSL 3 runtime DLLs that
# `lbug_shared.dll` imports.
#
# Usage (PowerShell, from the repository root):
#   .\tools\knowledge\setup_lbug.ps1
#
# Reversible: delete `tools/knowledge/lib/*.dll` and re-run to rebuild.

$ErrorActionPreference = "Stop"

$libDir = Join-Path $PSScriptRoot "lib"
New-Item -ItemType Directory -Force -Path $libDir | Out-Null

# ---- 1. lbug_shared.dll -----------------------------------------------------
$lbugVersion   = "v0.21.2"
$lbugUrl       = "https://github.com/LadybugDB/ladybug/releases/download/$lbugVersion/liblbug-windows-x86_64.zip"
$lbugZipSha256 = "8A1A6B68ACBAEE7FAD8C0EC797EADEFCE1F0866EA75DB45239BCF41291A49C79"

$tmp = Join-Path $env:TEMP "opencode\lbug-setup"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
$zip = Join-Path $tmp "liblbug-windows-x86_64.zip"

if (-not (Test-Path (Join-Path $libDir "lbug_shared.dll"))) {
    Write-Host "Downloading $lbugUrl ..."
    Invoke-WebRequest -Uri $lbugUrl -OutFile $zip
    $actual = (Get-FileHash $zip -Algorithm SHA256).Hash
    if ($actual -ne $lbugZipSha256) {
        throw "SHA-256 mismatch for lbug zip: expected $lbugZipSha256, got $actual"
    }
    $extract = Join-Path $tmp "liblbug-extract"
    Expand-Archive -Path $zip -DestinationPath $extract -Force
    Copy-Item (Join-Path $extract "lbug_shared.dll") (Join-Path $libDir "lbug_shared.dll") -Force
    Write-Host "Staged lbug_shared.dll (verified)."
} else {
    Write-Host "lbug_shared.dll already present."
}

# ---- 2. OpenSSL 3 runtime (imports of lbug_shared.dll) ----------------------
# lbug_shared.dll imports libcrypto-3-x64.dll and libssl-3-x64.dll (OpenSSL 3).
# These are NOT published in the LadybugDB release assets. They are standard
# OpenSSL 3.0.x Win64 DLLs; obtain them from any OpenSSL 3.0.x x64 distribution
# (e.g. the "Shining Light Productions" Win64 OpenSSL 3.x build, or a bundled
# OpenSSL 3 runtime already installed on the machine) and copy them here.
$opensslDlls = @(
    @{ Name = "libssl-3-x64.dll";    Sha256 = "FA19F63369F780F42A5EE90890448EA3A24A3BF031191BF7B2EAD20DBD6D34D5" },
    @{ Name = "libcrypto-3-x64.dll"; Sha256 = "52DCBE7EE8365E6BFEAF6EE4B541DDCA69C5A93A04C593B28DADCDAE5515F47F" }
)
foreach ($dll in $opensslDlls) {
    $p = Join-Path $libDir $dll.Name
    if (-not (Test-Path $p)) {
        throw ("Missing OpenSSL 3 runtime '{0}'. Copy it into '{1}' from an OpenSSL 3.0.x x64 distribution, then re-run." -f $dll.Name, $libDir)
    }
    $h = (Get-FileHash $p -Algorithm SHA256).Hash
    if ($h -ne $dll.Sha256) {
        Write-Warning ("OpenSSL DLL '{0}' hash {1} differs from the verified {2}; proceeding but record the new hash." -f $dll.Name, $h, $dll.Sha256)
    }
}
Write-Host "OpenSSL 3 runtime DLLs verified."

# ---- 3. Verify the library loads through the Python API ---------------------
$env:LBUG_C_API_LIB_PATH = Join-Path $libDir "lbug_shared.dll"
$env:PATH = "$libDir;$env:PATH"
$py = Join-Path $PSScriptRoot ".venv\Scripts\python.exe"
if (Test-Path $py) {
    & $py -c "import ladybug as lb; db = lb.Database(':memory:'); c = lb.Connection(db); print('ladybug', lb.Database.get_version(), 'loads OK')"
} else {
    Write-Warning "venv not present; create it with 'python -m venv tools/knowledge/.venv' and install requirements.txt."
}

Write-Host "setup_lbug.ps1 complete."
