# I0 — K0/K1–K5 Integration Readiness and Consolidation

Date: 2026-10-24. OFFLINE integration preparation only. No master merge, no
PR, no hardware.

## 1. Exact revisions

- master HEAD: **`376c56e071a259fbc3546a159d701517e0452b34`**.
- Accepted K5 HEAD: **`c75094fc77632a819ade07412e95ab2ae402d7c4`**
  (K0/K1 `5d3969f` → K2 `8fa3a6e` → K3 `b6ee814` → K4 `26a7bf8` → K5 `c75094f`).
- Current I0 branch: `feature/i0-integration-readiness` @ `74ea1ca` (on top of K5).

## 2. Verified branch ancestry

The K0/K1→K5 chain is **linear** (each HEAD an ancestor of the next, verified via
`merge-base --is-ancestor`). However the chain's **base is `feature/pico-5b-plan`
@ `1f89c0b`, not master**. The first commit after master `376c56e` is `128a0d81`
(the HR22 protocol gate). Cumulative distance: **39 commits = 22 (pico-5b-plan
entanglement) + 17 (K0/K1→K5)**.

## 3. Cumulative change summary (`376c56e..c75094f`)

**171 files, +198,830 / −29.** Components:

| Component | Files | Nature | Origin |
|---|---|---|---|
| `tools/picoscope/` (MCP + adapters + tests + evidence) | ~60 | passive addition | entanglement (PICO-1..4T) |
| `tools/knowledge/` + `tools/diagnostics/` | ~23 | passive addition | K0/K1 + K4 + K5 |
| `tools/tool-registry.yaml`, `tools/preservation_guard.py` | 2 | passive addition | K4 |
| `docs/` (PICO-*, firmware reference, K3/K5 docs) | ~30 | addition | mixed |
| `.opencode/agents/*.md`, `AGENTS.md` | 6 | **modifies existing** | K2 |
| `opencode.json` | 1 | **modifies existing** (MCP reg) | mixed |
| `release/baseline-0/*.hex` | 4 (~172k lines) | passive addition (archive) | entanglement |
| `TrackController5/Python/{Comm.py, flash_one.py, test}` | 3 | **Comm.py modifies existing**; 2 new | K3 |
| `SiebwaldeApp/**/*.cs` | 8 | **production C# change** | entanglement `128a0d81` |

## 4. Production-affecting scripts and risk

- **`TrackController5/Python/Comm.py` (K3, +28/−9)** — a change to the existing
  production firmware-download path (the `DataAquisition` transport used by
  `TrackController.py`/`StateMachines.py`): replaces the `TRACKCONTROL` DNS /
  `.100` fallback with hardcoded `192.168.1.193`, and bounds the receive-parse
  loop. Additive in spirit but it **modifies** the live tooling; the K3 commit
  message "no firmware/C# change" understates this. Needs explicit PO/PL
  acknowledgement before integration.
- **8 C# files (`128a0d81` HR22 protocol gate)** — a production C# behaviour
  change (fail-closed HR22 protocol compatibility check behind the HR7 identity
  check). This is the top of the **separate, unmerged** `feature/protocol-version-gate`
  and **rides along untested** via the pico-5b-plan base. It is not part of the
  K0/K1→K5 increments and has not been independently C#-reviewed in this chain.

## 5. Knowledge/evidence consistency

- `device:amp-1` = **UNKNOWN**, `RUNS_FIRMWARE → 0xE906` (1.0.0, the 2026-10-19
  round-trip); `device:amp-3` = **UNKNOWN**, `RUNS_FIRMWARE → 0x251F`.
  `obs:csharp-init-log-no-flash` = **PROVEN_HARDWARE** (file checksum 0x251F,
  "0 slaves require flashing", no 122). `hyp:erase-not-executed` = **INFERRED**.
- **Finding 1 (stale amp-1 edge):** the graph records amp-1's last verified
  firmware as 0xE906, but the 23-10 C# log implies amp-1 now reads 0x251F — an
  **unrecorded later rollback** (the 2026-10-20 legacy-stack restoration). The
  `UNKNOWN` status is correct (not falsely asserting 0xE906 is current), but the
  rollback event is not itself represented.
- **Finding 2 (file-checksum vs fresh HR11):** the K5 doc correctly separates the
  **file** checksum (0x251F) from the inferred per-amp HR11 readback (C# SLAVEINFO,
  "fresh but not independently hardware-verified"). No strict conflation.
- **Finding 3 (dangling provenance):** some `knowledge_seed.py` `source_path`/
  `image_path` reference files that are untracked/generated (diagnostic `0x2D00`
  HEX, `docs/L2-STAGE-A-...md`, `tools/picoscope/evidence/*`, `TrackAmplifier4.X/dist`).
  This is a provenance-integrity gap independent of the entanglement.

## 6. Dependency and MCP setup validation

- LadybugDB 0.21.2 + native `lbug_shared.dll`/OpenSSL SHA-256 pinned
  (`setup_lbug.ps1`); deterministic rebuild verified (141 nodes / 83 rels ×2).
- Knowledge MCP + Diagnostics MCP register + start offline; PicoScope MCP
  registration preserved. Machine-local absolute paths (venv paths, evidence
  dirs) are **documented portability limitations** — they are hard-coded in
  `opencode.json`/scripts and would need remapping on another workstation.

## 7. Offline regression results

| Suite | Result |
|---|---|
| Knowledge Foundation | **22 passed** |
| Diagnostics (K4/K5) | **55 passed** |
| K3 firmware driver | **43 tests OK** |
| Preservation guard | **OK, exit 0** |

## 8. Independent Integrator verdict

**NEEDS_DECISION** — merge readiness **NO-GO for direct merge** of `c75094f` →
master. Reasons: (a) 22 pico-5b-plan commits ride along (unmerged HR22 C# gate +
picoscope MCP + baseline-0 archive), (b) K3 `Comm.py` modifies the existing
production download path, (c) stale `device:amp-1` edge + dangling provenance.

## 9. Proposed integration revision and GO/NO-GO

**NO-GO for a single direct merge.** Recommended sequence (separate PO decisions):

1. Land the entanglement first via its own review paths — notably
   `feature/protocol-version-gate` (`128a0d81` + `5c916522`) as a **separately
   Integrator-reviewed C# change**, plus the picoscope MCP and the baseline-0
   archive.
2. Rebase the 17 K-commits (`a6e9dde..c75094f`) onto the resulting master.
3. Correct the dangling `source_path`/`image_path` references and the stale
   `device:amp-1` firmware edge before the final merge.

Mechanically the merge is a fast-forward (no conflicts); the blocker is
**semantic**: the K-increments are not cleanly separable from the pico-5b-plan
base without touching the seed provenance.

## 10. Remaining blockers

1. pico-5b-plan entanglement (HR22 C# gate + picoscope MCP + baseline-0) needs
   its own merge/review sequencing.
2. K3 `Comm.py` production-path modification needs explicit acknowledgement.
3. `device:amp-1` stale edge (0xE906 vs 0x251F) + dangling seed provenance.

## 11. Recommended K6 starting point

First physical OBSERVE capability: **PIC32 `GET_CONTROLLER_VERSION` (113)**.
K6 must: verify source-defined request/response; enforce exclusive controller
access (no concurrent C#/Python/MCP); avoid C# startup/reset/flash/bootloader
access; use a narrowly scoped, backend-enforced diagnostic interface; validate
the transport adapter offline first; and require a **new explicit PO
LIVE_HARDWARE** authorization before the first real command. No prior physical
authorization carries forward. The K5 offline OBSERVE adapter + the
`docs/K5-OBSERVE-ADAPTER-AND-OWNERSHIP.md` §3 GO/NO-GO checklist are the
launchpad.
