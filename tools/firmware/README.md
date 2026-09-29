# Siebwalde firmware build harness

`build-firmware.ps1` is a PowerShell 5.1 build harness for the Siebwalde PIC18/PIC32
firmware. It builds the MPLAB X projects from a **clean out-of-tree copy** using pinned,
absolute toolchain paths, then copies each produced HEX into a versioned output directory
and emits a machine-readable manifest.

It is read-only with respect to the repository: it never builds in place, never writes
outside `build/`, never flashes/programs/connects to hardware, and never touches git state.
The tracked `TrackAmplifier4.X/dist/*.hex` artefacts stay byte-identical.

## Usage

```powershell
# Build every project with its default configuration
powershell -NoProfile -ExecutionPolicy Bypass -File tools\firmware\build-firmware.ps1 -Project All

# Build one project / configuration
powershell -NoProfile -ExecutionPolicy Bypass -File tools\firmware\build-firmware.ps1 -Project TrackAmplifier4 -Configuration Offset

# Keep the temporary build copy (for map/list inspection)
powershell -NoProfile -ExecutionPolicy Bypass -File tools\firmware\build-firmware.ps1 -Project TrackAmplifier4 -KeepTemp
```

Parameters:

| Parameter | Values | Default |
| --- | --- | --- |
| `-Project` | `TrackAmplifier4`, `TrackBackplane2`, `Bootloader`, `TrackController5`, `All` | `All` |
| `-Configuration` | project configuration name | per project (see matrix) |
| `-OutputDir` | output directory | `<repo>\build\firmware\out` |
| `-KeepTemp` | switch | off (temp copy removed after the run) |

Default configurations: `TrackAmplifier4` -> `Offset`, `TrackBackplane2` -> `default`,
`Bootloader` -> `No_Configurations`, `TrackController5` -> `Production`.

## Output layout

```
build/firmware/
  tmp/<rev>/                       clean build copies + _logs (git-ignored)
  out/<rev>/manifest.json          machine-readable manifest for the revision
  out/<rev>/<project>/<config>/    produced HEX + build.log
  microchip -> C:\Microchip        temp junction used by the PIC32 build
```

Exit code is non-zero and a `BLOCKER` message is raised on any missing toolchain, generator
failure, or build error; errors are never swallowed.

## Pinned toolchain matrix

| Component | Version | Absolute path |
| --- | --- | --- |
| MPLAB X (application projects, PIC32) | v6.20 | `C:\Program Files\Microchip\MPLABX\v6.20` |
| MPLAB X (bootloader, DFP 1.7.134) | v6.05 | `C:\Program Files\Microchip\MPLABX\v6.05` |
| make | from the MPLAB X version above | `<MPLABX>\gnuBins\GnuWin32\bin\make.exe` |
| makefile generator | from the MPLAB X version above | `<MPLABX>\mplab_platform\bin\prjMakefilesGenerator.bat` |
| XC8 (applications, backplane) | 2.31 | `C:\Program Files\Microchip\xc8\v2.31` |
| XC8 (bootloader) | 2.40 | `C:\Program Files\Microchip\xc8\v2.40` |
| XC32 (PIC32) | 2.50 | `C:\Program Files\Microchip\xc32\v2.50` |
| Harmony | v2_06 | `C:\Microchip\harmony\v2_06` |
| PIC18F-K DFP (bootloader) | 1.7.134 | `...\MPLABX\v6.05\packs\Microchip\PIC18F-K_DFP\1.7.134` |
| PIC32MZ-EF DFP | 1.4.168 | `...\MPLABX\v6.20\packs\Microchip\PIC32MZ-EF_DFP\1.4.168` |

The `make.exe` first found on `PATH` is an unrelated winavr make and is deliberately **not**
used; the MPLAB X make is always invoked by absolute path.

## Build method

1. Copy each project (plus required siblings) from the repository into
   `build/firmware/tmp/<rev>/`, removing any previous copy first.
2. Move the pinned compiler `bin` directory to the front of `PATH`.
3. Run `prjMakefilesGenerator.bat "<temp project dir>"` to generate
   `nbproject/Makefile-*.mk` (these are not committed).
4. Run `make -f nbproject/Makefile-<CONF>.mk SUB=no .build-conf` in the temp project dir.
   `SUB=no` prevents the bootloader sub-project from being built (important for the
   `TrackAmplifier4.X` Offset path, whose `project.xml` declares the bootloader as a
   dependency). This form is also robust for the spaced MPLAB X `make.exe` path.
5. Delete the expected artefact before building, then locate, SHA-256 hash and copy it to
   `build/firmware/out/<rev>/<project>/<config>/`.
6. Write `build/firmware/out/<rev>/manifest.json` and verify that
   `git status --porcelain --untracked-files=no` is empty and that
   `TrackAmplifier4.X/dist` is unchanged.

### PIC32 (TrackController5) notes

The committed project references Harmony through the relative path
`../../../../../microchip/harmony/v2_06`. In the temp copy the harness creates a directory
junction at the computed location (`build/firmware/microchip -> C:\Microchip`) so that path
resolves. Nothing is installed globally.

## TrackAmplifier4.X host flash checksum

The harness computes the same checksum as `SiebwaldeApp.Core`:

- `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Controller/TrackAmplifierBootloaderHelpers.cs`
  (`Execute()`)
- constants in
  `SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Services/PublicEnums.cs`:
  `PROGEMSIZE = 0x8000`, `BOOTLOADEROFFSET = 0x800`, `HEXROWWIDTH = 16`, `JUMPSIZE = 4`.

Algorithm: read `(PROGEMSIZE - BOOTLOADEROFFSET) / HEXROWWIDTH = 1920` 16-byte rows, sum
their little-endian 16-bit words (skipping the final 2 bytes — the checksum storage location
itself), modulo `0xFFFF`.

## Reproducibility

- PIC18 builds (`TrackAmplifier4.X`, `TrackBackplane2.X`, bootloader) are byte-reproducible
  across runs. The freshly built `TrackAmplifier4.X` Offset HEX is byte-identical to the
  committed `dist/Offset/production/TrackAmplifier4.X.production.hex`
  (SHA-256 `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D`) and its
  host checksum is `0x251F`.
- The PIC32 `TrackController5` HEX differs between runs **only** in one Intel HEX record: an
  embedded compile-time string. Proven source: `TrackController5/firmware/src/controller.c:133`
  uses `__DATE__ " " __TIME__`. All other bytes are identical.

## Boundaries

- No flashing/programming/hardware access (no MPLAB IPE/PICkit/`mdb`).
- No modification of firmware source, tracked artefacts, docs or git state.
- A successful build is compile/link evidence only; it is not evidence of physical
  firmware behaviour.
