# Siebwalde Firmware Flashing, Recovery, Baselines and Hardware Bring-Up — Technical Reference

**Reference status:** technical synthesis, 2026-10-18. **Purpose:** durable, source-grounded engineering knowledge for Project Lead, Architect, Developer, Integrator and future sessions. **Do not confuse this reference with a hardware acceptance certificate or a new authority grant.**

## 0. Reading contract, scope and evidence confidence

This document consolidates (1) PIC32 firmware source (`TrackController5/firmware/src` supplied as `PIC32_src.zip`), (2) PIC18 application (`TrackAmplifier4.X`), (3) PIC18 independent bootloader (`TrackAmplifierBootLoader.X`), (4) regular/prototype backplane (`TrackBackplane2.X`), (5) C# `SiebwaldeApp.Core` snapshots, (6) repository documents/workflow snapshots, (7) PL-reported hardware execution, and (8) explicit PO architecture decisions. Source snapshots are **not automatically equivalent to the current PR #47/#48 branch**. At every new code change, independently check current repository HEAD and complete source/build provenance.

Evidence classes used throughout:

- **SOURCE-VERIFIED:** code or artifact directly inspected in the supplied snapshot; use the cited paths/symbols to rediscover on current branch.
- **HARDWARE-REPORTED:** PL reports actual successful test/program/readback; final acceptance still depends on independent evidence and verified scope.
- **PO-CONFIRMED:** physical or product behavior confirmed by the human operator/owner.
- **INFERRED/RISK:** engineering inference; do not promote to established physical fact without evidence.
- **PENDING:** not tested, or present only in unmerged/reviewed implementation.
- **FUTURE:** deliberately out of current Phase-1 testing scope.

**Immediate engineering objective:** use and validate the EXISTING flash toolchain and one-target PIC18 route; secure correct `RecoverSlavesStep` behavior, perform real PIC18 flash/verify/rollback, then proceed to Phase-1 physical acceptance. Do not redesign the bootloader or implement the future FirmwareManager library in this increment.

## 1. System and hardware topology

### 1.1 Devices and control chain

- Master: **PIC32MZ2048EFH144**, Microchip Harmony 2.06 project `TrackController5.X`, Ethernet/UDP PC interface (historically UDP port 10000), 9-bit RS-422 master/slave bus, 8-bit bootloader transfer mode.
- Traction slaves: **PIC18F25K40**, `TrackAmplifier4.X`, nominal bus addresses **1..50**. PIC18 application calculates PWM, direction, brake, occupancy/status and protection under the Phase-1 wire contract.
- Backplane controllers: `TrackBackplane2.X`, addresses **51..55**; these are amplifier-slot selectors, **not target PIC18 traction firmware recipients**.
- Prototype/test oval: **four prototype amplifiers**, addresses **1, 3, 4, 6**, on a prototype backplane with six available physical amplifier positions. **PO-CONFIRMED:** this is an extensively exercised, fully functioning setup; do **not** request that the PL requalify its backplane firmware/build variant without a new concrete failure.
- Full railway: **50 traction amplifiers + 5 backplanes**, ten slots per full backplane.
- Section-to-physical-amplifier mapping exists in C#: `LayoutProfile.PhysicalAmplifierMapping`; `simple-loop.json`: logical section 1→physical amp 1, 2→3, 3→4, 4→6. This mapping identifies the REQUIRED operational amplifiers for the selected layout; do not substitute a generic minimum count.
- Separate C# device grouping: `TrackAmplifierGroups` with operational groups (`MainRailway`, `MountainRailway`), `Spare`, and `Unassigned`. These are separate from `PhysicalAmplifierMapping`; there is no basis to silently treat any extra/unmapped device as an approved spare.

### 1.2 Operational readiness versus maintenance/update membership

**PO-CONFIRMED RULES:**

1. Normal operation of the chosen topology requires **every required mapped amplifier** present, compatible and functional. A partial installation is not ready for normal railway operation; a bench/maintenance session with one amplifier remains valid for targeted firmware tests.
2. A missing required amplifier cannot be replaced logically merely by a discovered spare or by meeting an arbitrary slave-count threshold.
3. A recognized, compatible spare amplifier may be visible for diagnostics and must join appropriate approved firmware batch-update campaigns. It is **never addressed with normal PWM, enable, motion-permission or traction commands** merely because it is connected. Unmapped does not prove an output is physically disconnected.
4. Spares and mapped amps are both candidates for safe, explicitly controlled maintenance/recovery. Unknown/ambiguous slave type or address is not an eligible spare.
5. Maintain separate sets: expected operational amps, detected amp-class devices, qualified spares, recovery candidates, approved firmware-update target set. A count over all slave types mixes concerns.
6. Backplanes 51..55 can receive their own backplane-control commands but must never be flashed with a PIC18 track-amplifier image.

### 1.3 Prototype-backplane mapping — existing design, NOT a new verification project

`TrackBackplane2.X/main_proto_backplane.c`, `Set_Amplifier()` maps the six logical HR0 masks to the **physical last six ID select pins**:

| PIC32 logical slot | Backplane HR0 mask | Physical prototype ID line |
|---:|---:|---|
| 1 | `0x0001` | `AMP_ID5_SET_LAT` |
| 2 | `0x0002` | `AMP_ID6_SET_LAT` |
| 3 | `0x0004` | `AMP_ID7_SET_LAT` |
| 4 | `0x0008` | `AMP_ID8_SET_LAT` |
| 5 | `0x0010` | `AMP_ID9_SET_LAT` |
| 6 | `0x0020` | `AMP_ID10_SET_LAT` |

All other ID select outputs are normally high; the selected output is active low. `main.c` implements the full ten-slot variant (masks 1..0x200 -> ID1..ID10), whereas the prototype source remaps logical 1..6 to the physically available ID5..ID10. MPLAB project configurations include a separate `Proto_Backplane` variant. This design is **PO-confirmed as working repeatedly**. Do not assume the GPIO selection also electrically isolates the RS-422 UART data bus; it is a *bootloader entry/identification* selection, not proven UART muxing.

## 2. Firmware artifacts, versions and immutable recovery baselines

### 2.1 Immutable Baseline 0

**Baseline 0 is a project/release classification, not a semantic version retroactively written into legacy binary images.**

| Target | Baseline-0 source / archived artifact | SHA-256 | On-device evidence |
|---|---|---|---|
| PIC18 legacy Offset | `release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex` (historically `TrackAmplifier4.X/dist/Offset/production/TrackAmplifier4.X.production.hex`) | `B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D` | HR11 checksum `0x251F` on prototype amps 1/3/4/6, historically linked to `928ea7c` |
| PIC32 legacy Production | `release/baseline-0/PIC32-Master-legacy-A90E44.hex` | `A90E44956A7CA15BFF7439D36FFA9FD0ADC81F8630D0B27A30E31932E3025584` | ICD3 readback matched all 267,316 populated bytes of original HEX; on-device old image was independently identified before new flash |

The PL reported the PIC32 historical image archived/pushed on `chore/baseline-0-archive` @ `667a490` along with `release/baseline-0/PIC32-readback-pfm.hex` (raw Intel HEX SHA prefix `04AE6A69…`). Actual Git status must be refreshed; a branch push does not mean the image is merged to master.

Do **not** use the different old `default`-configuration PIC32 image (SHA prefix `1BF22EE3`) as a substitute for the validated `Production` Baseline 0. Do not mutate immutable archives.

### 2.2 New firmware 1.0.0 artifacts

| Target | New artifact SHA-256 | Version / contract | Evidence |
|---|---|---|---|
| PIC32 Production | `6CEC61D4C28F514D34C22B4537D9D11A2F4ADEA9A7BDB5C2957E3E2674152101` | `1.0.0`, controller-version command 113, protocol 0x01 | Reproducible 803,291-byte HEX; real ICD3 flash/rollback/reflash reported PASS; version command 113 still PENDING |
| PIC18 Offset | `4F9ED4ECD6B925066D67A9F465022FA5F8DF3716F040F209B58190FE1F305A2A` | `1.0.0`, application checksum `0xE906`, map/protocol 0x01, HR21/HR22 | Offline built and verified; real PIC18 flash PENDING |

The deployed old PIC18 legacy Offset image remains present in some project `dist/Offset` directories even though current source `version.h` may advertise 1.0.0. **A file path, build header or version.h is not firmware identity.** For every flash verify exact HEX bytes/SHA, PIC18 embedded checksum, target/hardware/protocol match, and runtime readback.

For PIC32 Production 1.0.0, source changed `__DATE__/__TIME__` to deterministic `CTRL_RELEASE_TIMESTAMP "2026-10-17"`. PIC32 project uses MPLAB X v6.20 with XC32 v2.50 and `PIC32MZ-EF_DFP` 1.4.168 (installed system-wide, accessible to both IDE and IPECMD). PIC18 Offset checksum remains governed by its exact linker and bootloader contract.

### 2.3 PIC32 round-trip: what is proven, what is NOT

**HARDWARE-REPORTED COMPLETE:** PO-powered-up, ICD3 target rev A3, `DEVSN0 ecda3e32` / `DEVSN1 5ac05410`, firmware suite `01.56.10`; image Baseline 0→PIC32 1.0.0→Baseline 0→PIC32 1.0.0 with `Programming/Verify complete`, target released and ping recovered after each. This PROVES the ICD3 programming/verification/rollback/reflash route on real hardware. PIC32 last programmed image = 1.0.0.

**PENDING:** GET_CONTROLLER_VERSION=113 response using a valid C# CONNECT handshake; PIC32 slave-inventory/legacy-amp communication after new firmware; physically observed safe/neutral output; complete Phase-1 hardware acceptance. Ping and IPECMD “Program Succeeded” alone do **not** prove full runtime behavior.

Original ICD3 power error (`could not detect target voltage VDD`) had a **PO-confirmed mundane cause**: target power intentionally off while the operator was away; power later restored. Not a DFP or ICD3 hardware defect. Do not reopen this as unresolved after the subsequent successful flash cycle.

### 2.4 Validated baseline model

Keep permanently: immutable **Baseline 0**, plus a **current validated full-stack baseline** with PIC32, PIC18, C# host, protocol versions, mapping/topology, exact artifact hashes, toolchain, tests, recovery procedure and independent Integrator evidence. Successful flash and component tests do not automatically promote Baseline 1. Candidate build identity (version+source commit+SHA) is distinct from hardware acceptance status. After the system meets full stack gates, freeze/publish the new accepted baseline without deleting Baseline 0.

## 3. PIC32 master programming and diagnosis

### 3.1 ICD3 and IPECMD

PIC32 device `PIC32MZ2048EFH144`, rev A3. MPLAB X IDE v6.20 Production configuration, ICD3 serial `JIT102238953`, firmware suite `01.56.10`, DFP 1.4.168, XC32 2.50. Older IDE 6.05 also available. PIC32 has **no existing application-level Ethernet bootloader**; ICD3 is current authorized programming/recovery path.

The earlier `ipecmd PIC32MZ2048EFH144 ICD3 -I` failed because positional syntax was incorrect; **NO user-pack reinstallation is needed**. Locally verified patterns:

```text
-P32MZ2048EFH144 -TPICD3 -I       [device identify]
-P32MZ2048EFH144 -TPICD3 -GF<file> [read back program/config as supported]
-P32MZ2048EFH144 -TPICD3 -OK -OL   [release from reset]
```

Programming used `-F<file> -M` as part of the **reported, locally verified** first hardware session; always consult the locally installed IPECMD readme for exact erase/verify/config scope before repeat use. ICD3 readback can hold the target in reset: release with the validated sequence and allow ~15 seconds for Ethernet restoration. Maintain exact program/verify transcripts and memory/content comparisons. Compare on-device memory contents, not SHA of differently formatted Intel HEX dumps. Boot/config words matter for real recovery.

ICD3 operations affect the **shared master** and therefore every connected slave; no live locomotive traffic or uncontrolled outputs while held in reset/programming. Process stopped/ping restored is not automatically physical neutral-state evidence.

### 3.2 Ethernet CONNECT and command 113

`TrackController5/firmware/src/controller.c`, `REPLYxCONTROLLERxVERSION()` constructs a controller reply: `header=HEADER`, `command=CONTROLLER`, `data[0]=GET_CONTROLLER_VERSION` (113), `data[1]=MAJOR`, `[2]=MINOR`, `[3]=PATCH`, `[4]=protocol`, `[5]=0`, `[6..]` = null-terminated deterministic release-timestamp. `CONTROLLER_STATE_HANDLE_COMM_DATA` recognizes `GET_CONTROLLER_VERSION` and sends this reply.

C# `ConnectToEthernetTargetStep` implements the established CONNECT lifecycle; `QueryControllerVersionStep` and `TrackCommClientAsync` decode the version. A standalone raw UDP 113 packet returned a datagram from `192.168.1.12` (`01000000`), not valid controller proof. This does **not** show command 113 is missing; it shows an invalid/raw diagnostic is insufficient. Implement a **minimal read-only harness** or safe existing diagnostic that reuses the real C# transport+CONNECT+113 path, without invoking full startup side effects, reset-all-slaves, automatic flashing or train control. Source snapshot `RawUdpTransport` did not filter datagrams by expected remote endpoint; investigate correlation/source filtering against current branch before trusting arbitrary received frames.

Do not falsely assert that PR #47/#48 need to be MERGED before a read-only harness can run; diagnose precise build/runtime dependencies. Hardware tests may use reviewed feature-branch artifacts within the approved test envelope; actual Git merge still requires PO authorization.

## 4. PIC32 master ↔ PIC18 communication contracts

### 4.1 Two link modes, distinct purposes

- **Application mode:** RS-422 UART2, **9-bit address-marked Modbus-like** traffic. The ninth bit is used for addressing. PIC32 `slavefwhandler.c`, `UART2_Config_9bit_Modbus()` uses PDSEL `0x3` (9-bit/no parity), one stop bit. PIC18 operational traffic supports status registers, checksum, control and occupancy.
- **Bootloader mode:** PIC32 `UART2_Config_8bit_Bootloader()` switches UART2 PDSEL `0x0` to **8-bit** serial frames. PIC18 bootloader begins messages with 0x55, does not include amplifier address in each frame. Raw 8-bit bootloader transactions are therefore *not* interchangeable with 9-bit Modbus application transactions.
- PIC32 firmware and `slavebootloaderroutines.c` contain existing bootloader command implementations; do not reimplement in a separate test client without need.
- Oscilloscope reference P2-A0 on PIC32 logical UART D6 RX / D7 TX measured roughly 2.0161 Mbaud / ~496 ns nominal bit (digital sample resolution limiting). Sampled frames included `04 03 00 08 00 02 39 A2` request and `04 03 04 D3 98 00 00 1A 68` response, with project-specific on-wire CRC convention `bit_reverse16(crc16_modbus(payload))` serialized low byte first. Normal 9-bit request/address-marker versus 8-bit PIC18 reply interpretation is in the existing HMO decoder. Do not change CRC/wire semantics based on generic textbook Modbus assumptions.

### 4.2 PIC32 firmware sequencing (source-verified)

`TrackController5/firmware/src/slavefwhandler.c`:

1. The `SLAVExFWxHANDLER` master state machine receives PC/Ethernet subcommands in a mailbox and handles `EXEC_FW_STATE_RECEIVE_FW_FILE`, `EXEC_FW_STATE_RECEIVE_CONFIG_WORD`, `EXEC_FW_STATE_FLASH_ALL_SLAVES`, `EXEC_FW_STATE_SELECT_SLAVE`, and bootloader primitives.
2. `FwFileDownload()` receives the host image and independently recomputes the 16-bit checksum before marking file reception complete.
3. `ConfigWordDownload()` uses `memcpy(..., 14)` into `FwConfigWord`. The C# legacy HEX helper extracts **12 configuration bytes** from a 0x0C Intel HEX record. Resolve exact 12-vs-14 transport fields: do not label 14 configuration bytes as proven just because PIC32 copies 14 bytes. Determine whether length/padding is legitimate in current host/protocol, and add tests if touched.
4. `FlashAllSlavesAuto()` loops `SlaveId1` from 1 through `NUMBER_OF_AMPLIFIERS` (50). Its predicate checks `MASTER_SLAVE_DATA[SlaveId1].SlaveDetected == true` AND `HoldingReg[11] != fwData.fwchecksum`, then calls `FlashSequencer(SlaveId1)` **sequentially**. Despite its misleading name and the PL phrase “broadcast”, **THIS IS NOT simultaneous unaddressed firmware broadcast**. It is a selective detected+checksum-mismatch firmware campaign. The C# legacy `FlashFwTrackamplifiersStep` independently computes/logs its delta set; PIC32 recomputes selection from its own latest slave table. C# does **not** prove it sends a literal list to the PIC32. Preserve existing proven selective update behavior; update authorization constrains writes, not necessarily removes automatic update support in normal Siebwalde workflow.
5. `FlashSequencer(Slave)` maps target ID 1–50 to backplane 51–55 and slot shift, selects the backplane ID pin via HR0 bitmask, sends HR11=0 to transient initial address 0xAA to reset the chosen PIC18 into the bootloader, then clears backplane HR0 selector before 8-bit bootloader erase/write/config/checksum/reset operations. This sequential path already exists and works for healthy application-to-bootloader handover.
6. `SelectSlave(uint8_t SlaveId)` is a similar **bootloader-entry preparation FSM** exposed by command `EXEC_FW_STATE_SELECT_SLAVE=123`; it derives the backplane and slot, selects via HR0, sends HR11=0 to 0xAA, clears selection, returns DONE/ERROR. On the normal `DONE` branch in the inspected source it only logs and returns to waiting **without obvious `CREATExTASKxSTATUSxMESSAGE` acknowledgement**. An explicit CLI/C# caller must not assume a DONE status is emitted: verify actual transport/response behavior and improve as needed.
7. `SelectSlave()` currently **expects a successful application Modbus response** to the HR11 write at 0xAA. A PIC18 whose application checksum is already corrupt boots directly into 8-bit bootloader mode and may NOT provide this response. Therefore SELECT_SLAVE alone is not proof of a valid recovery path for **bootloader-only** amplifiers. Handle the two entry paths separately.
8. `GoToCase`, `DelayCount1`, etc. are shared static/global sequencing state: before new operations or retries verify proper initialization and no cross-command pollution, especially interrupted operations and operation reentry.

### 4.3 Target selection physical meaning / multi-fault policy

**Source-verified:** `SelectSlave` and `FlashSequencer` drive a backplane active-low ID pin so the chosen PIC18 will enter bootloader when sent the 0xAA/HR11=0 strobe. That pin is **released again** *before* the raw 0x55 flash frames. Therefore it is **not an ongoing per-frame electrical bus isolator**. The entire RS-422 bus could see 8-bit bootloader traffic. The reason ordinary single-target works is that only the selected healthy application is intentionally switched into bootloader mode.

**Source-verified:** `TrackAmplifierBootLoader.X/.../pic18f_bootload.c` checks the application checksum at reset; damaged application image may enter bootloader **automatically**, independently of ID selection. If two PIC18s have invalid application images, both could concurrently listen/respond to unaddressed frames. Identical responses may be indistinguishable in software. Physical bus isolation of an arbitrary pair of simultaneous bootloaders is NOT proven from this software.

**PO DECISION:** multiple simultaneous bootloader-only units are an exceptional **double-fault**, to be reported to the operator with an instruction to physically isolate/unplug all but the chosen recovery amp. It is NOT a reason to redesign the production backplane now or to block ordinary first single-PIC18 flashing with speculative topology-proof tasks. Stop/abort if ambiguous replies, multiple bootloader candidates or unsafe state are detected. Do **not promise perfect detection of identical simultaneous responders**; explain residual risk and operator-isolation recovery procedure. For first live PIC18 flash rely on already repeatedly proven single-amplifier entry scheme and a bounded selected-target workflow.

### 4.4 Firmware selection and spare amps

`FlashFwTrackamplifiersStep` iterates detected amplifier-class devices (1..50) and marks mismatch against file checksum; `FlashAllSlavesAuto` does the same per-slave in PIC32. Normal Siebwalde firmware distribution must retain selective update behavior and later include recognized compatible spare amplifiers, not merely mapped operational ones. Extra devices must NOT be controlled for traction. The dedicated operator/PL CLI needs an explicit **one-amplifier target override** rather than forcing the existing selective multi-amp batch update. Do not claim the current batch mechanism is an undifferentiated broadcast; test only the selected amplifier in the first controlled experiment.

## 5. PIC18 application-to-bootloader entry

`TrackAmplifier4.X/main.c` startup configuration stage observes physical address/ID pin and transient Modbus initial address `0xAA`. In the inspected legacy implementation, writing HR11=0 under the selection conditions triggers reset/bootloader entry. Reconfirm the current Phase-1 HR11 write-preservation amendment in `docs/amplifier-system-architecture.md` and current source. HR11 is also the readback checksum (`ReadFlashChecksum`) in normal application. Addresses 1..50 are PIC18 app addresses; 0xAA is a transient bootloader-entry initial address, NOT the long-term identity of a particular amplifier. Do not interpret HR11=0 as a standalone recovery eligibility signal for an already invalid application.

Source application initialises drive pins to a safe state (`LM_PWM=0`, `LM_BRAKE=1` in startup). PIC32 Phase-1 safety includes protocol identity, watchdog/lease and global-stop behaviors, but test physical neutral on hardware independently. Old PIC18 legacy images do **not** support new HR21/HR22 identity contract; the host must distinguish LEGACY vs Phase-1 vs bootloader-only vs UNKNOWN and fail closed for normal motion without disabling maintenance capability.

## 6. PIC18 bootloader memory and protocol — code-level reference

### 6.1 Reset and checksum-based recovery

`TrackAmplifierBootLoader.X/mcc_generated_files/pic18f_bootload.c`, `BOOTLOADER_Initialize()` calls `Bootload_Required()`. `bootload.h` defines `IO_PIN_ENTRY_PORT_PIN` = PIC18 `PORTAbits.RA0`, `IO_PIN_ENTRY_RUN_BL=0` (active-low). `Bootload_Required()` waits briefly, enters bootloader if RA0 low, otherwise calculates a checksum of application bytes beginning at `NEW_RESET_VECTOR=0x0800` and compares with the stored 16-bit word at `END_FLASH-2=0x7FFE/0x7FFF`. If checksum mismatches, bootloader remains active and waits for a new image; if valid, it jumps to application 0x0800. Bootloader waits for the selection pin to be released, then ~300 µs before UART/RS-422 TX driver is enabled (see comments/source). **This directly supports the PO's intended recovery from interrupted application flashing.**

### 6.2 Flash address and checksum contract

- Bootloader reserve `0x0000..0x07FF`, application address range `0x0800..0x7FFF`.
- Checksum covers `0x0800..0x7FFD`, stored at `0x7FFE/0x7FFF` (low byte then high byte).
- Current build linker option: `-mchecksum=800-7FFD@7FFE,width=-2,algorithm=2`.
- Legacy PIC18 checksum `0x251F`, target 1.0.0 checksum `0xE906`.
- Existing project code computes a 16-bit additive checksum of little-endian 16-bit words. **This checksum is NOT a Modbus CRC and NOT an authentication mechanism.** Verify both via checked-in `tools/firmware/checksum-verify.ps1` before flashing.
- Bootloader reset condition and configuration write protection must be independently considered; code `device_config.c` has `#pragma config WRTB=ON` (boot block protected) and `WRTC=OFF` (configuration registers not write-protected). Readback or project config validates deployment-specific fuses.

### 6.3 8-bit bootloader commands and frame

`TrackAmplifierBootLoader.X/mcc_generated_files/pic18f_uart.c`, `Run_Bootloader()` accepts start byte **0x55** followed by a 9-byte base command frame and optional data. Bootloader header files describe opcode, data length, EE keys, little-endian address bytes (low/high/upper/unused) and data payload. The command IDs:

| ID | PIC18 bootloader operation |
|---:|---|
| 0 | `READ_VERSION` |
| 1 | `READ_FLASH` |
| 2 | `WRITE_FLASH` |
| 3 | `ERASE_FLASH` |
| 4 | `READ_EE_DATA` |
| 5 | `WRITE_EE_DATA` |
| 6 | `READ_CONFIG` |
| 7 | `WRITE_CONFIG` |
| 8 | `CALC_CHECKSUM` |
| 9 | `RESET_DEVICE` |

The serial header does **not** contain a slave address. PIC32 `slavebootloaderroutines.c` implements send/receive and the erase/write/config/checksum/reset sequencing. PIC18 write block size is 64 bytes (see helpers and memory headers); the C# host delivers four 16-byte HEX rows = one 64-byte packet to PIC32.

### 6.4 Bootloader write/erase protection caveat

`pic18f_bootload.c` functions `Write_Flash()` and `Erase_Flash()` reject `TBLPTR >= END_FLASH` but, in the inspected source, do NOT themselves explicitly reject writes/erases targeting below `0x0800`. `WRTB=ON` should provide device-level boot block protection if actually applied, but is not a substitute for host-side validated address range and verified configuration fuses. Configuration writes are distinct and potentially sensitive. Current first PIC18 test must use known validated exact Offset artifacts and the existing protocol; no bootloader refactoring in this increment.

The parser/receiver also calculates data length and frame buffers; do not claim unchecked frame lengths are safe without source/tests. Record this as a later hardening candidate with clear tests rather than changing legacy bootloader for the first test.

### 6.5 Rare multiple-bootloader failure: operational policy

With two invalid PIC18 applications concurrently on the shared RS-422 line, an unaddressed bootloader protocol cannot guarantee unique recovery. **PO accepts this exceptional double fault.** Report a clear operator action (isolate physical amplifiers until only one target remains) and preserve ability to restore each individually. Do not demand a new Modbus bootloader as a prerequisite for Phase-1.

## 7. C# actual firmware flow and audit findings

### 7.1 Detection threshold and startup

`SiebwaldeApp.Core/Model/TrackApplication/Initialization/Steps/DetectSlavesStep.cs` holds `_minRequiredSlaves = 3`. The detector increments a count for **every** `trackAmpItems` entry with `SlaveDetected == 1`; this can include backplanes 51..55. On `slaveCount >= 3`, it advances to `FlashFwTrackamplifiers`; on `<3`, it attempts `RecoverSlaves` (subject to retries). This is **NOT** a proper expected-device topology check and **NOT** a correct eligibility criterion for one PIC18 bootloader recovery. Historic original reason for three is unknown; do not invent a reason. It may relate to early prototype/full layouts, but PO labels that speculation, not proven.

**Binding PO decisions:** recovery eligibility is independent of number of healthy/detected devices, independent of active topology completeness, and available even when one amplifier is isolated on bench. Normal operation readiness is a distinct mapping-based contract. Do not just delete a threshold without tracing other startup dependencies. Do not use mere absence of device as evidence of corrupt app or license arbitrary firmware writes.

### 7.2 Normal `FlashFwTrackamplifiersStep`

In `FlashFwTrackamplifiersStep`, initial step validates the chosen firmware file/checksum and counts detected amps 1..50 whose HR11 checksum differs. If no mismatch, it continues to amplifier initialization; if mismatches exist, it sends `EXEC_MBUS_STATE_SLAVE_FW_FLASH`, host firmware file and configuration to PIC32, then `EXEC_FW_STATE_FLASH_ALL_SLAVES` (122) invokes PIC32's **sequential checksum-selective loop**. C# “delta list” in the reports is primarily calculation/logging of required units; PIC32's own detected/checksum table is the final actual selector. Preserve current selective update design. The first PL test requires additional **explicit one-target** maintenance route without automatically running 122 against all detected mismatches.

C# current **PR #48** (not in supplied source snapshots) reportedly adds target-scoped, expiring deployment session, manifest SHA-256 and PIC18 embedded checksum checks, no silent normal-startup auto-flash, audits. Reported 1,066 Release tests PASS, Integrator R2 PASS with one MAJOR: `RecoverSlavesStep` bypasses deployment policy. **Do not assert those code changes are present on master until actual PR status verified.** The eventual product may support deliberately authorized normal selective updates and fleet package distribution; fail-closed unapproved writes are the safety rule.

### 7.3 `RecoverSlavesStep`

The historical C# file has a 15-state-style recovery FSM that uses bootloader raw commands (version, erase, write, configuration, checksum, reset). It does **not** run `EXEC_FW_STATE_SELECT_SLAVE` and assumes one unit is in bootloader mode. It is reachable only through the `<3` detected-slave startup route. This makes routine recovery of one bricked amplifier among four/fifty healthy devices inaccessible.

**Required correction:** keep legitimate count-independent recovery. Split ordinary firmware deployment (package version mismatch + deployment authorization) from bounded emergency recovery (verified/explicitly maintenance-authorized bootloader-only target, correct image, safe state, maximum attempts, logs and post-restore check). The C# code cannot simply prepend `SELECT_SLAVE` in all cases because that command currently relies on working 9-bit application/HR11 response; bootloader-only recovery bypasses normal application. Architect and Integrator must verify physical/single-target behavior and exact transitions. Preserve existing recovery contract even when no healthy amp is discovered. Ambiguous multiple bootloaders → stop/report operator isolation.

### 7.4 C# implementation risk register from legacy snapshots (verify against current revisions)

These are **candidate defects/follow-up tests**, not claims that PR #48 still contains them:

1. `RecoverSlavesStep.cs` around state 15: `lastMessage.Value` used without `HasValue` guard: timeout/null may throw instead of bounded retry.
2. In `RecoverSlavesStep.cs` and `FlashFwTrackamplifiersStep.cs` some predicates have structure `(correctTask && correctCommand && correctState && OK) || NOK`, missing grouping around `NOK`. A stray `NOK` message may match an unrelated transaction. Correct state/task correlation needs tests.
3. `TrackAmplifierBootloaderHelpers.Execute()` reads fixed-format 16-byte Intel HEX rows via `Substring`, looking for `0C` configuration record; full record-length/address/type/checksum validation was not observed in the legacy parser. It appends to `GetHexFileData` and adds checksum without explicit reentrant reset. Use the independent artifact verifier, ensure retry-safe per-operation parser state.
4. `SendNextFwDataPacket` holds `IterationCounter`, incremented by `Enums.JUMPSIZE` after each 64-byte send. Constructor starts at zero, no obvious reset on object reuse. Recreate/reset per operation; test `legacy -> 1.0.0 -> legacy -> 1.0.0` with realistic object lifetime.
5. `TrackAmplifierBootloaderHelpers` extracts 12 config bytes, PIC32 `ConfigWordDownload()` copies 14; inspect host frame, padding and device semantics before changing anything.
6. `TrackAmplifierInitializationServiceAsync` includes CONNECT → controller-version query → `ResetAllSlaves`; therefore full C# startup is not guaranteed read-only. Use read-only handshake harness rather than casually running entire initialization.
7. `RawUdpTransport` snapshot accepts any sender datagram; verify expected IP/source and message correlation for command 113.
8. In PIC32 source, normal `SELECT_SLAVE=123` completion may lack an explicit status reply; do not code a client that waits for a success message unless proven.
9. PIC32 flash FSM maintains static sequencing state; verify bounded reset between repeated operations, not only first flash.
10. Distinguish *host detected delta* from *PIC32 recomputed target selection* to prevent discrepancies with stale register data or mixed old/new firmware.

Document observed current behavior, test proofs, and changed code separately; avoid a big rewrite of unmodified legacy code without clear acceptance value.

## 8. Firmware authorization and emergency-recovery policy

### 8.1 Normal firmware deployment

Authorized normal/update session must define scope: target address or approved batch set, artifact identity and hardware family, SHA-256, PIC18 embedded checksum, protocol/map compatibility, valid exclusive ownership, safe outputs, expiration/revocation, maximum retries, and logging. Checksum mismatch alone does not authorize a flash; nonetheless the product's **selective update feature must be retained** and capable of distributing firmware-only releases independently of the WPF app later.

The first actual PIC18 test must use one explicitly selected amplifier, not the existing `FLASH_ALL_SLAVES` update campaign when several amps have mismatched checksums. No inadvertent operation on spares, unknown slaves or other mapped amps.

### 8.2 Emergency PIC18 bootloader recovery

A functioning application is NOT required if the bootloader is available after invalid checksum. Recovery is available regardless of detected slave count and mapping, but firmware writes require a trustworthy target context: one chosen physical amp, recognized bootloader, correct image, safe maintenance envelope, bounded retries and post-recovery validation. App-only SELECT_SLAVE cannot be assumed to work for bootloader-only devices.

Distinguish recovered application starting and HR11 matching from mere bootloader acknowledgement. If multiple bootloader-only units may be present and replies are ambiguous, do not assume automatic unique recovery: stop and request isolation. When operator has isolated one, normal bootloader protocol can be used. The operator exception is accepted and should not block single-fault recovery design.

### 8.3 Aborted/interrupted flash

Do not simulate actual power loss during first PIC18 programming session. Test offline interruption/retry policy, bootloader checksum startup paths, and clean rollback. Real interruption/power removal is a separately reviewed physical validation after the normal cycle is proven. Avoid automatic uncontrolled writes after restart without persistent session safety.

## 9. Phase-1 wire contract and control safety context

Keep distinct identity, firmware authorization, traction safety and telemetry:

- PIC18 current Phase-1 version: `version.h`, HR21 encodes MAJOR(high)/MINOR(low), HR22 PATCH(high)/PROTOCOL(low); HR7 identity/map version; HR11 existing firmware checksum and special bootloader-entry semantics. Legacy 12-register firmware has no HR21/HR22 and uses different HR7 interpretation. C# `ProtocolVersion` gate (PR #47) reportedly requires compatible HR7 and HR22 protocol=0x01, zero/unavailable treated as unknown/incompatible, preserving read-only version diagnostics where possible.
- PIC32 `GET_CONTROLLER_VERSION=113` returns semantic version/protocol/timestamp as in §3.2; C# must use correct connect/session and response source/correlation.
- PIC18 HR0 PWM setpoint 0..799 (neutral 399), direction bit10, brake bit11, EMO bit15; HR1 CONTROL set-only and edge bits per frozen Phase-1 system contract. HR1 bit14 is motion-permission lease; global stop denies motion permission, lease expiry requires fast stop. Actual hardware semantics must follow latest normative `docs/amplifier-system-architecture.md` Revision 8 and amendments rather than earlier proposed 21-HR drafts.
- Existing PIC18 bridge and Modbus watchdogs, heartbeat, neutral state, E-stop and communications-loss paths must not be bypassed by firmware updates; no normal train motion during unvalidated mixed PIC32/PIC18 versions.
- Backplane addresses 51..55 are control devices, never traction target firmware images.
- Phase-2 overcurrent/thermal cutoff, calibrated current and short-circuit thresholds remain a separate future physical validation; do not insert these tests into firmware bring-up.

## 10. P2-A0 hardware reference (historical, not Phase-1 validation)

Legacy PIC18 amplifier-1 HMO2024 baseline (scope ID `HAMEG,HMO2024,014458275,04.531`, HO720 USB/VCP COM3 115200-8N1 LF; raw POD1:DATA? 6000 samples):

- Analog CH1 tip one H-bridge output **after common-mode choke**; single-ended ground clip amplifier incoming supply GND, not a differential rail-voltage measurement. Distinguish observed ringing/probe artifacts from component stress.
- D1 PIC18 MCLR; D2 PIC18 RB7/PT2; D3 PIC18 RB6/PT1; D6 PIC32 logical RX; D7 PIC32 logical TX before RS-422.
- REF-AMP01-001 (~60us switching), REF-AMP01-002-LONG (600us, 11 PWM cycles) ~**19.92 kHz**, ~**42.4%** duty under specific legacy test conditions. REF-AMP01-003-DIGITAL synchronized UART (~2.0 Mbaud). Scope captures/tooling PR #45 merged. These do **not** prove 1.0.0 or Phase-2 protection.
- PIC18 legacy amp 1/3/4/6 checksum `0x251F` with functioning test oval; first new firmware requires new physical captures where acceptance specifies.
- PIC18 current-sense analog chain for future P2-A1: LMD18200 CSEN -> R24 3.3k ground -> R25 4.99k / C29 100n -> RB0 ADC; calibrated independent current reference missing; no final protection thresholds in Phase-1 tests.
- Keep HMO2024 capture/decoder tools under `tools/hmo2024/` version controlled; do not require redoing baseline unless physical conditions changed.

## 11. Current workflow/Git snapshot and document consistency

**Reported latest execution:**

- PR #43 PIC18/PIC32/C# Phase-1 contract merged; PR #45 HMO tooling merged; PR #46 firmware versioning merged.
- PR #47 `feature/protocol-version-gate`: reportedly OPEN, CI green, Integrator R2 PASS.
- PR #48 `feature/firmware-deployment-authorization` @ `0a4e3eb`: reportedly OPEN; 1,066/1,066 Release software tests; Integrator R2 PASS except MAJOR ungated `RecoverSlavesStep`. Its target-scoped session gate has NOT been independently inspected in supplied ZIP snapshot.
- `chore/baseline-0-archive` @ `667a490` pushed; PIC32 round-trip test evidence reported stored in manifest. Verify actual branch/remote/merge state before claiming merged.
- **Scope of standing PO authority:** PL can autonomously repeat flash/verify/rollback cycles on a **verified isolated prototype** (PIC32 via ICD3, PIC18 via existing bootloader) with recovery artifacts, safe outputs, clear target and independent gates. Does NOT imply protected-branch PR merge authority or permission to flash active railway controllers. The PO has powered the prototype back on; still verify current state before each session.
- `docs/amplifier-phase1-bringup-preflight.md` in supplied docs ZIP is **historically outdated**: claims PIC32 A90E44 not archived, PIC32 identity UNKNOWN, no flash authority. Preserve context but mark superseded by actual archive/readback/program/rollback evidence and standing scoped PO authority.
- `docs/firmware-toolchain-readiness.md` retains historical F1 toolchain/build constraints; do not delete or rewrite historical evidence; link to this new authoritative cross-layer technical reference for later facts.
- `docs/current-state.md` snapshot currently contains old master HEAD and pre-hardware statuses. Reconcile compact current snapshot with latest actual Git PRs and hardware results.
- `.opencode/workflow/active-state.json` snapshot lists older P2-A0/legacy assumptions and incorrectly calls `FlashAllSlavesAuto` a simultaneous broadcast; reconcile **local active state** with current increment, evidence and PO authority, preserving backup/archive and Workflow v1 rules. Do not incorrectly commit ignored active-state file to Git.
- `docs/README.md`: add this reference prominently in domain/firmware read order.
- `docs/decisions.md`: append (do not replace historical decisions) PO decisions on recovery-count independence, prototype-backplane proven mapping, selective batch update + spares, double-fault operator isolation, immutable Baseline 0, current ICD3 route and future firmware library/OTA.
- `docs/amplifier-system-architecture.md`: normative Phase-1 wire contract (Revision 8 and amendments) unchanged. Add cross-link/erratum only if a current authoritative contradiction is demonstrated; avoid overwriting safety contract based on older docs or snapshots.
- `.opencode/agents/*.md`: existing Workflow v1 role contracts. No new agent roles, permissions or governance redesign required merely to document firmware knowledge. Ensure PL knows this reference is mandatory for amplifier-firmware increments.

## 12. Required next tests and order (brief, operational)

**P0 — fast PIC32 read-only runtime check:** run existing C# Ethernet CONNECT path + controller command113, confirm version 1.0.0/protocol and slave inventory. Do not accidentally run normal startup reset/flash steps. Confirm source endpoint/response correlation. Report last proven layer if blocked.

**P1 — smallest safe PIC18 single-target route:** examine PIC32 existing `FlashSequencer(SlaveId)` and `EXEC_FW_STATE_SELECT_SLAVE=123` interface/ack before implementation; do not mistake 123 for complete flash command or sufficient bootloader-only selection. Reuse existing file download, checksum, config and 0x55 protocol, create only the minimal single-target maintenance call (host or CLI). No duplicate protocol. Use the repeatedly proven proto-backplane behavior; no unnecessary requalification.

**P2 — count-independent emergency recovery:** correct `RecoverSlavesStep` eligibility independent of `<3` and ordinary firmware update authorization; ensure explicit targeted bounded recovery even with application invalid; do not assume 123 works if PIC18 app is absent. Add accurate operator intervention for ambiguous/double bootloader state. Resolve PR #48 MAJOR with Architect and independent Integrator review.

**P3 — hardware PIC18 cycle:** first one selected amp, verify legacy `0x251F`, flash checked Offset 1.0.0 `0xE906`, verify HR7/HR11/HR21/HR22 and safety, verify other amp images unchanged, restore 0x251F, verify, reflash 1.0.0 if safe. Record raw transcript/firmware hashes and runtime state. Avoid flash-all with multiple mismatched legacy amps.

**P4 — Phase-1 stack acceptance:** remaining neutral PWM, lease/watchdog, CRC, E-stop, reconnect, communication loss, physical waveform, independent Integrator. Promote Baseline 1 only at full acceptance.

**P5 — future design only after existing chain works:** one reusable C# Firmware.Core consumed by regular SiebwaldeApp (retains selective firmware distribution and maintenance) plus standalone FirmwareManager CLI (PL/test/rollback). Support firmware-only package updates; future PIC32 secure Ethernet OTA, FY PIC18 Ethernet, YArd. Maintain ICD3 independent recovery. Separately **after main application stabilizes**, investigate bootloader v2 minimal Modbus-addressed transfer, bootloader <=0x0800 constraints, device config protection, migration/recovery, with PL+Architect+Developer+Integrator. Do not rewrite current PIC18 bootloader during Phase-1 bring-up.

## 13. Acceptance / failure evidence discipline

Every report must distinguish: **planned, attempted, program verified, readback verified, runtime protocol observed, physically observed, safety validated, full-stack validated**. PIC32 ICD3 program+ping PASS is not command113 PASS; PIC18 raw bootloader ACK is not application/runtime/neutral PASS. Prefer independent Integrator evidence, exact commands, hashes, test IDs, logs and rollback. Stop affected operation on uncertain identity, wrong artifact, absent recovery, unsafe output, conflicting owner or ambiguous multi-bootloader response. Report only actual remaining PO physical needs, not repeated routine flash permission requests.

**End of technical reference.**
