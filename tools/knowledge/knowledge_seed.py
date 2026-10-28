"""Siebwalde Knowledge Foundation — curated initial knowledge seed.

This module holds the deterministic, provenance-bearing facts that seed the
knowledge graph. Every node carries an explicit ``status`` (PROVEN_HARDWARE /
PROVEN_SOURCE / PROVEN_OFFLINE_TEST / INFERRED / HYPOTHESIS / UNKNOWN /
CONTRADICTED) and the shared provenance fields (source path + location + git
revision + hash + evidence + evidence status + observation date).

CRITICAL RULES (per the PO directive):
- Import only evidence actually supported by the source material. Do not
  promote hypotheses to proven facts.
- Where the data does not support a conclusion, the status is UNKNOWN.
- This seed is a structured index; the authoritative sources remain the Git
  tree, the immutable firmware artifacts and the raw test evidence.

Sources read to produce this seed (branch ``feature/pico-5b-plan`` @
``1f89c0b`` unless a more specific revision is cited per-fact):
    docs/firmware-flash-recovery-technical-reference.md
    docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md
    docs/PICOSCOPE-PROFILE-B-BOOTLOADER-MEASUREMENT-PLAN.md
    docs/current-state.md
"""

from __future__ import annotations

import re

# ---------------------------------------------------------------------------
# Nodes: (node_type, properties).  ``id`` is the stable primary key.
# ---------------------------------------------------------------------------
NODES: list[tuple[str, dict]] = [
    # ==================================================================
    # HardwareDevice
    # ==================================================================
    ("HardwareDevice", {
        "id": "device:pic32", "label": "PIC32 master", "kind": "PIC32",
        "address": "192.168.1.193", "role": "RS-422 bus master / firmware-download host",
        "status": "PROVEN_SOURCE",
        "summary": "PIC32MZ2048EFH144 master; Ethernet/UDP host interface; 9-bit application UART and 8-bit bootloader UART on the same RS-422 bus.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "1.1", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("HardwareDevice", {
        "id": "device:amp-1", "label": "Amplifier 1", "kind": "PIC18",
        "address": "1", "role": "traction slave",
        "status": "PROVEN_HARDWARE",
        "summary": "Prototype amplifier address 1. K8B (2026-10-25) fresh physical readback: HR11=0x251F (legacy checksum, byte-stable vs the 2026-10-23 C# log), HR7 raw 0x002D (legacy HR7 is NOT a Phase-1 identity register), HR21/HR22 UNSUPPORTED. Resolves the prior UNKNOWN (a rollback to legacy 0x251F had occurred but was not independently captured).",
        "source_path": "docs/K8B-COMMUNICATION-RECOVERY.md",
        "source_location": "4", "source_revision": "a49ea004", "evidence_status": "OBSERVED",
    }),
    ("HardwareDevice", {
        "id": "device:amp-3", "label": "Amplifier 3", "kind": "PIC18",
        "address": "3", "role": "traction slave",
        "status": "PROVEN_HARDWARE",
        "summary": "Prototype amplifier address 3. K8B (2026-10-25) fresh physical readback: HR11=0x251F (legacy checksum), HR7 raw 0x002E, HR21/HR22 UNSUPPORTED. Resolves the prior UNKNOWN (PICO-5B ERASE/WRITE without confirmed acks; now freshly read back as legacy 0x251F).",
        "source_path": "docs/K8B-COMMUNICATION-RECOVERY.md",
        "source_location": "4", "source_revision": "a49ea004", "evidence_status": "OBSERVED",
    }),
    ("HardwareDevice", {
        "id": "device:amp-4", "label": "Amplifier 4", "kind": "PIC18",
        "address": "4", "role": "traction slave",
        "status": "PROVEN_HARDWARE",
        "summary": "Prototype amplifier address 4; legacy firmware 0x251F.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("HardwareDevice", {
        "id": "device:amp-6", "label": "Amplifier 6", "kind": "PIC18",
        "address": "6", "role": "traction slave",
        "status": "PROVEN_HARDWARE",
        "summary": "Prototype amplifier address 6; legacy firmware 0x251F.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("HardwareDevice", {
        "id": "device:backplane-proto", "label": "Prototype backplane", "kind": "backplane",
        "address": "51", "role": "amplifier slot selector",
        "status": "PROVEN_SOURCE",
        "summary": "Proto_Backplane build variant; six amplifier positions; logical slot N maps to physical ID(N+4) pin.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "1.3", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("HardwareDevice", {
        "id": "device:picoscope-3406d", "label": "PicoScope 3406D MSO", "kind": "scope",
        "address": "USB", "role": "measurement instrumentation",
        "status": "PROVEN_HARDWARE",
        "summary": "MSO used for PICO-4T/PICO-5B UART measurements; native digital edge trigger.",
        "source_path": "docs/PICOSCOPE-PICO-4-DIGITAL-TRIGGER-VALIDATION.md",
        "source_location": "summary", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),

    # ==================================================================
    # FirmwareArtifact
    # ==================================================================
    ("FirmwareArtifact", {
        "id": "firmware:pic18-0x251F", "label": "PIC18 legacy Offset 0x251F", "kind": "application",
        "target": "PIC18", "checksum": "0x251F", "version": "legacy",
        "image_path": "release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex",
        "status": "PROVEN_HARDWARE",
        "summary": "Known-good legacy PIC18 application (checksum 0x251F), historically linked to source 928ea7c. Rollback image.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.1", "source_revision": "1f89c0b",
        "source_hash": "B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D",
        "evidence_status": "OBSERVED",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic18-0xE906", "label": "PIC18 1.0.0 Offset 0xE906", "kind": "application",
        "target": "PIC18", "checksum": "0xE906", "version": "1.0.0",
        "image_path": "TrackAmplifier4.X/dist/Offset/production/TrackAmplifier4.X.production.hex",
        "status": "PROVEN_HARDWARE",
        "summary": "Phase-1 1.0.0 PIC18 application (checksum 0xE906); flash/rollback/reflash round-trip proven on amp 1 (2026-10-19).",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.2", "source_revision": "1f89c0b",
        "source_hash": "4F9ED4ECD6B925066D67A9F465022FA5F8DF3716F040F209B58190FE1F305A2A",
        "evidence_status": "OBSERVED",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic18-0x5263", "label": "PIC18 1.0.1 Offset 0x5263 (LED-corrected + de-energize)", "kind": "application",
        "target": "PIC18", "checksum": "0x5263", "version": "1.0.1",
        "image_path": "build/firmware/out/b2ea9735/TrackAmplifier4/Offset/TrackAmplifier4.X.production.hex",
        "status": "PROVEN_HARDWARE",
        "summary": "Phase-1 1.0.1 PIC18 application (checksum 0x5263) bundling (a) the PO-approved LED_RUN correction AmpCore_IsBridgeDriving()=config_ok&&enable&&!inhibited (replaces state==AMP_ST_RUN) and (b) the PO-approved bridge de-energize on comm-loss/lease-expiry/STOP_REQUEST once neutral is reached (AmpHw_Enable enable-term gains &&!inhibited; hard fault still cuts via !hard). HR7=0xF101, HR22=0x0101, protocol 1. Built XC8 2.31 / MPLABX v6.20 / PIC18F25K40; test_amp.py PASS. DEPLOYED 2026-10-27 to all four prototype amps (1/3/4/6) via single-target 131 (FlashOneAmplifier), verified by SLAVEINFO readback.",
        "source_path": "TrackAmplifier4.X/amp_core.c",
        "source_location": "AmpCore_IsBridgeDriving + AmpCore_Update AmpHw_Enable", "source_revision": "b2ea9735",
        "source_hash": "EE08DAA56D637F123908CE88EF8CE45B1396EFDCE4E0A0E87A4C6057844BC1EB",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-27",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic18-0xFFC2", "label": "PIC18 1.0.2 Offset 0xFFC2 (LED_RUN actual-drive + LED_WAR restoration)", "kind": "application",
        "target": "PIC18", "checksum": "0xFFC2", "version": "1.0.2",
        "image_path": "build/firmware/out/14b217be/TrackAmplifier4/Offset/TrackAmplifier4.X.production.hex",
        "status": "PROVEN_HARDWARE",
        "summary": "Phase-1 1.0.2 PIC18 application (checksum 0xFFC2). LED_RUN now reads the actual applied hardware state PWM3EN && !brake_active (ON at neutral + during controlled stop ramp; OFF on hard inhibit/brake-only/disabled); LED_WAR restored to blink while normal PWM drive is not active (WAR = !LED_RUN). Observation-only (no AmpCore/ramp/safety change). HR7=0xF101, HR22=0x0201 (patch=2 in high byte), protocol 1. Built XC8 2.31; test_amp.py PASS. DEPLOYED 2026-10-27 to all four prototype amps (1/3/4/6) via single-target 131.",
        "source_path": "TrackAmplifier4.X/main.c",
        "source_location": "Amplifier_ServiceTick LED_RUN_LAT = PWM3EN && !brake_active", "source_revision": "fb67387a",
        "source_hash": "C6BA4B477D6841EAE6BC5C03D4673A6BF11DCD48427396B272EB18ABF730FA9A",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-27",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic18-0x195A", "label": "PIC18 L2 0x195A candidate", "kind": "application",
        "target": "PIC18", "checksum": "0x195A", "version": "L2",
        "status": "PROVEN_SOURCE",
        "summary": "L2 version-ID candidate (checksum 0x195A). NOT deployed; never flashed to hardware. Source doc is an untracked working-tree file (not committed), so provenance is not durable.",
        "source_path": "docs/L2-STAGE-A-PYTHON-COMPARISON-AND-RETRY.md",
        "source_location": "summary", "source_revision": "unknown", "evidence_status": "UNVERIFIED",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic18-0x2D00", "label": "PIC18 diagnostic 0x2D00", "kind": "diagnostic",
        "target": "PIC18", "checksum": "0x2D00", "version": "diagnostic",
        "image_path": "release/diagnostic-pico-5b/TrackAmplifier4.X.production.hex (external: Siebwalde-pico5b-checksum worktree; not in main repo)",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Temporary PICO-5B diagnostic image (checksum 0x2D00) built by uncommenting main.c test byte; archived feature/pico-5b-diagnostic @ bea4251. The HEX file itself lives in an external worktree, not the main repo; the SHA-256 below is the durable reference.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "8", "source_revision": "bea4251",
        "source_hash": "ABAEF621C25E1AB046FB98834E7A5DB84D49C0FBEAF49AFEAF62DAAF082D9E53",
        "evidence_status": "TEST_PASS",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic32-a90e44", "label": "PIC32 legacy Production A90E44", "kind": "application",
        "target": "PIC32", "version": "Baseline-0",
        "image_path": "release/baseline-0/PIC32-Master-legacy-A90E44.hex",
        "status": "PROVEN_HARDWARE",
        "summary": "PIC32 Baseline-0 legacy Production image; ICD3 readback matched all populated bytes.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.1", "source_revision": "1f89c0b",
        "source_hash": "A90E44956A7CA15BFF7439D36FFA9FD0ADC81F8630D0B27A30E31932E3025584",
        "evidence_status": "OBSERVED",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic32-0.1.0", "label": "PIC32 L1 0.1.0", "kind": "application",
        "target": "PIC32", "version": "0.1.0",
        "status": "PROVEN_HARDWARE",
        "summary": "Deployed PIC32 L1 0.1.0 (HEX SHA-256 prefix EC7E3E6E); kept on the PIC32 for the L2/PICO-5B single-target tests.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "12.A.3", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic32-1.0.0", "label": "PIC32 1.0.0", "kind": "application",
        "target": "PIC32", "version": "1.0.0",
        "status": "PROVEN_HARDWARE",
        "summary": "PIC32 1.0.0 (deterministic build, controller-version 113 = 1.0.0/protocol 0x01).",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.2", "source_revision": "1f89c0b",
        "source_hash": "6CEC61D4C28F514D34C22B4537D9D11A2F4ADEA9A7BDB5C2957E3E2674152101",
        "evidence_status": "OBSERVED",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic32-1.0.1", "label": "PIC32 1.0.1", "kind": "application",
        "target": "PIC32", "version": "1.0.1",
        "status": "PROVEN_HARDWARE",
        "summary": "PIC32 1.0.1 (commands 131/132 single-target flash; controller-version 113 = 1.0.1/protocol 0x01/build 2026-10-26). Deployed P2-B2-A1.",
        "source_path": "docs/P2-B2-A1-PIC32-1.0.1-IMPLEMENTATION-PREFLIGHT.md",
        "source_location": "1", "source_revision": "b09ac316",
        "source_hash": "AD4EC03A96EA879D7F2AEC0DADB34C979FC57E56B13C62E3A2CF63190D42306D",
        "evidence_status": "OBSERVED",
    }),

    # ==================================================================
    # SourceRevision
    # ==================================================================
    ("SourceRevision", {
        "id": "source:928ea7c", "label": "TrackAmplifier4.X legacy baseline",
        "revision": "928ea7c", "branch": "restored baseline",
        "subject": "Pre-refactor PIC18 source; reproduces 0x251F byte-identically.",
        "status": "PROVEN_SOURCE",
        "summary": "PIC18 legacy source baseline that reproduces the 0x251F Offset HEX.",
        "source_path": "docs/firmware-toolchain-readiness.md",
        "source_location": "13", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("SourceRevision", {
        "id": "source:401291f", "label": "flash_one.py single-target driver",
        "revision": "401291f", "branch": "feature/legacy-l2-stage-a-driver",
        "subject": "Single-target Python flash driver (G1-G4 logging, bounded parsing, no auto-retry).",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Python single-target flash driver; 43 offline tests (post-K3 fail-closed correction); the basis of the PICO-5B live runs.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "9", "source_revision": "1f89c0b", "evidence_status": "TEST_PASS",
    }),
    ("SourceRevision", {
        "id": "source:f802d87", "label": "single-target bootloader recovery",
        "revision": "f802d87", "branch": "feature/pic18-single-target-bootloader-recovery",
        "subject": "PR #49: command 131/132 + global-reset/bring-up corrections + --read-hr CLI.",
        "status": "PROVEN_HARDWARE",
        "summary": "Hardware-verified single-target flash (131) + bootloader-only recovery (132) branch; not merged.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("SourceRevision", {
        "id": "source:bea4251", "label": "PICO-5B diagnostic firmware",
        "revision": "bea4251", "branch": "feature/pico-5b-diagnostic",
        "subject": "Archived 0x2D00 diagnostic firmware build.",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Branch holding the 0x2D00 diagnostic image.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "8", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("SourceRevision", {
        "id": "source:667a490", "label": "Baseline-0 archive",
        "revision": "667a490", "branch": "chore/baseline-0-archive",
        "subject": "Immutable Baseline 0 archive (PIC18 0x251F + PIC32 A90E44 + readback).",
        "status": "PROVEN_HARDWARE",
        "summary": "Immutable Baseline-0 artifact archive.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.1", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),

    # ==================================================================
    # ProtocolCommand (commands + the taskcommand acks they expect)
    # ==================================================================
    ("ProtocolCommand", {
        "id": "command:250", "command": 250, "command_name": "CLIENT_CONNECTION_REQUEST",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Ethernet client-connection request; response CONTROLLER/CONNECTED/DONE.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:103", "command": 103, "command_name": "SLAVE_FW_FLASH",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Enter firmware-flash mode; response FWHANDLERINIT/CONNECTED.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:120", "command": 120, "command_name": "RECEIVE_FW_FILE",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Firmware-file download standby; response FWFILEDOWNLOAD/140 STANDBY.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:141", "command": 141, "command_name": "FW_DATA",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Firmware data packet (480 x 64-byte blocks); ends with 143 + RECEIVED_CHECKSUM_OK.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:121", "command": 121, "command_name": "RECEIVE_CONFIG_WORD",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Config-word download standby; response FWCONFIGWORDDOWNLOAD/150.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:151", "command": 151, "command_name": "FW_CONFIG_WORD",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Config word payload (12 bytes); response 152.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:123", "command": 123, "command_name": "SELECT_SLAVE",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Select a slave and force it into bootloader by writing HR11=0 to the boot-init address 0xAA. No host ack. Non-destructive (no erase), but a running amp answers on its assigned address so the write is dropped unless the target is at the boot address (freshly reset / power-cycled).",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:124", "command": 124, "command_name": "GET_BOOTLOADER_VERSION",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Read-only bootloader version probe (CMD_READ_VERSION 0x00). Table reads only; no NVMCON WR/FREE, no reset. Cannot identify which amplifier answers.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "4", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:125", "command": 125, "command_name": "ERASE_FLASH",
        "direction": "HOST_TO_PIC32", "destructive": True,
        "status": "PROVEN_SOURCE",
        "summary": "Erase application flash 0x0800..0x7FFF (480x64B), including the checksum word. FIRST DESTRUCTIVE command. Expects ack 202.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "6", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:126", "command": 126, "command_name": "WRITE_FLASH",
        "direction": "HOST_TO_PIC32", "destructive": True,
        "status": "PROVEN_SOURCE",
        "summary": "Write application flash blocks. Expects ack 212 per block.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:127", "command": 127, "command_name": "WRITE_CONFIG",
        "direction": "HOST_TO_PIC32", "destructive": True,
        "status": "PROVEN_SOURCE",
        "summary": "Write config word. Expects ack 222.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:128", "command": 128, "command_name": "CHECK_CHECKSUM",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Verify checksum. Expects ack 232.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:129", "command": 129, "command_name": "SLAVE_RESET",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Reset the target out of bootloader. Expects ack 242.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:130", "command": 130, "command_name": "EXIT",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Exit flash mode. No ack (bounded delay).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:122", "command": 122, "command_name": "FLASH_ALL_SLAVES_AUTO",
        "direction": "HOST_TO_PIC32", "destructive": True,
        "status": "PROVEN_HARDWARE",
        "summary": "Fleet auto-flash (FlashAllSlavesAuto). Works only after the full init sequence (ResetAllSlaves); historically proven. NOT used in single-target tests.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("ProtocolCommand", {
        "id": "command:131", "command": 131, "command_name": "FLASH_ONE_SLAVE",
        "direction": "HOST_TO_PIC32", "destructive": True,
        "status": "PROVEN_HARDWARE",
        "summary": "Single-target flash. Requires a global reset (107 RESET + 100 SLAVES_ON) first, so the target listens at boot address 0xAA. Proven on amp 1 (2026-10-19).",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("ProtocolCommand", {
        "id": "command:132", "command": 132, "command_name": "FLASH_ONE_SLAVE_BOOTLOADER_ONLY",
        "direction": "HOST_TO_PIC32", "destructive": True,
        "status": "PROVEN_HARDWARE",
        "summary": "Bootloader-only recovery for a single slave. Proven (recovered amp 1 after a failed 131 flash).",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("ProtocolCommand", {
        "id": "command:113", "command": 113, "command_name": "GET_CONTROLLER_VERSION",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Read-only controller version query; request [0xAA,0x71] (no payload/footer); reply CONTROLLER(10) + echo 113 + MAJOR/MINOR/PATCH + PROTOCOL + build timestamp. Implemented by REPLYxCONTROLLERxVERSION (controller.c); only side effect is a UDP version reply (no flash/reset/MBUS).",
        "source_path": "TrackController5/firmware/src/controller.c",
        "source_location": "REPLYxCONTROLLERxVERSION", "source_revision": "5269624e", "evidence_status": "SOURCE_REF",
    }),
    # Expected acks (taskcommand feedback)
    ("ProtocolCommand", {
        "id": "ack:202", "command": 202, "command_name": "ERASE_FLASH_RETURNED_OK",
        "direction": "PIC32_TO_HOST", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Expected ack for command 125 (ERASE).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "ack:212", "command": 212, "command_name": "WRITE_FLASH_RETURNED_OK",
        "direction": "PIC32_TO_HOST", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Expected ack for command 126 (WRITE).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "ack:222", "command": 222, "command_name": "WRITE_CONFIG_RETURNED_OK",
        "direction": "PIC32_TO_HOST", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Expected ack for command 127 (WRITE_CONFIG).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "ack:232", "command": 232, "command_name": "CHECK_CHECKSUM_RETURNED_OK",
        "direction": "PIC32_TO_HOST", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Expected ack for command 128 (CHECK_CHECKSUM).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "ack:242", "command": 242, "command_name": "SLAVE_RESET_RETURNED_OK",
        "direction": "PIC32_TO_HOST", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Expected ack for command 129 (SLAVE_RESET).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "ack:194", "command": 194, "command_name": "GET_BOOTLOADER_VERSION_OK",
        "direction": "PIC32_TO_HOST", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Expected OK response for command 124.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),

    # ==================================================================
    # ProtocolState
    # ==================================================================
    ("ProtocolState", {
        "id": "state:app-mode", "label": "9-bit application mode",
        "state_name": "UART2 9-bit Modbus",
        "status": "PROVEN_HARDWARE",
        "summary": "RS-422 UART2, 9-bit address-marked Modbus-like traffic (PDSEL 0x3, no parity, one stop bit); measured ~2.0161 Mbaud.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "4.1", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("ProtocolState", {
        "id": "state:bootloader-mode", "label": "8-bit bootloader mode",
        "state_name": "UART2 8-bit bootloader",
        "status": "PROVEN_HARDWARE",
        "summary": "PIC32 switches UART2 to 8-bit (PDSEL 0x0); frames begin with 0x55, unaddressed. The 9->8-bit transition was physically captured on D1.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "4.1", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("ProtocolState", {
        "id": "state:fwhandler", "label": "Firmware-handler state",
        "state_name": "FWHANDLER",
        "status": "PROVEN_SOURCE",
        "summary": "PIC32 firmware-handler state entered by command 103; drives the single-target 120..130 sequence.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolState", {
        "id": "state:fw-download", "label": "Firmware download state",
        "state_name": "FWFILEDOWNLOAD",
        "status": "PROVEN_SOURCE",
        "summary": "PIC32 firmware-download task (140 STANDBY -> 142 DONE -> 143 CHECKSUM).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "5", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),

    # ==================================================================
    # SafetyInvariant
    # ==================================================================
    ("SafetyInvariant", {
        "id": "safety:erase-boundary", "label": "ERASE is the destructive boundary",
        "statement": "Command 125 (ERASE) is the first destructive command; the last ordinary non-destructive abort point is BEFORE 125. 125 erases 0x0800..0x7FFF including the checksum word.",
        "applies_to": "single-target flash", "requires_authorization": True,
        "status": "PROVEN_SOURCE",
        "summary": "Do not treat ERASE as non-destructive; abort is only safe before 125.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "6", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("SafetyInvariant", {
        "id": "safety:physical-isolation", "label": "Physical target isolation is mandatory",
        "statement": "The 8-bit bootloader protocol is unaddressed; software alone cannot guarantee an unaddressed erase/write affects only the intended amplifier. Exclusive-target safety requires physically unplugging all non-target amplifiers.",
        "applies_to": "bootloader erase/write", "requires_authorization": True,
        "status": "PROVEN_SOURCE",
        "summary": "Physical isolation (unplug non-target amps) is required before any unaddressed erase/write.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "3", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("SafetyInvariant", {
        "id": "safety:no-assume-ok", "label": "No ASSUME-OK for destructive operations",
        "statement": "A RECEIVE_DATA_TIMEOUT on a destructive command (ERASE/WRITE) is NOT confirmation that the command executed. An ERASE timeout must never be treated as 'assume erase ok, proceed'.",
        "applies_to": "flash driver", "requires_authorization": True,
        "status": "PROVEN_SOURCE",
        "summary": "The --proceed-on-timeout / ASSUME-OK driver behavior is unsafe and must not be used for live flashing until fail-closed behavior is independently verified.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "6", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("SafetyInvariant", {
        "id": "safety:authorization-required", "label": "Flash requires explicit PO authorization",
        "statement": "Flashing, recovery, reset, power-cycle and other state-changing hardware operations require explicit, scoped Product Owner authorization.",
        "applies_to": "all hardware operations", "requires_authorization": True,
        "status": "PROVEN_SOURCE",
        "summary": "Live hardware and firmware-flash operations are human-gated.",
        "source_path": "docs/development-workflow.md",
        "source_location": "11", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("SafetyInvariant", {
        "id": "safety:bootloader-exclusive", "label": "Confirm at most one bootloader responder",
        "statement": "Before 123/125, use the read-only command 124 to confirm at most one bootloader responder is on the bus. A 9-bit application amplifier does not answer raw 8-bit 0x55.",
        "applies_to": "bootloader pre-flash", "requires_authorization": False,
        "status": "PROVEN_SOURCE",
        "summary": "Use the non-destructive 124 probe to confirm bootloader-bus exclusivity before destructive steps.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "4", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("SafetyInvariant", {
        "id": "safety:unknown-state", "label": "Unknown recovery state is fail-closed",
        "statement": "When a firmware recovery state is unknown, report UNKNOWN and STOP; do not assume a target is erased or intact, and do not issue further destructive commands without explicit re-authorization.",
        "applies_to": "recovery", "requires_authorization": True,
        "status": "PROVEN_SOURCE",
        "summary": "A fresh agent facing an unknown firmware state must stop and report UNKNOWN, not guess.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "7", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),

    # ==================================================================
    # TestExecution
    # ==================================================================
    ("TestExecution", {
        "id": "test:picoscope-278", "label": "PicoScope regression 278",
        "test_name": "PICO-4T baseline regression", "result": "PASS",
        "date": "2026-10-22", "environment": "offline",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "278 PicoScope tests pass on the PICO-4T digital-trigger baseline.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "10", "source_revision": "1f89c0b", "evidence_status": "TEST_PASS",
    }),
    ("TestExecution", {
        "id": "test:flash-one-26", "label": "flash_one.py 26 tests",
        "test_name": "flash_one.py offline tests", "result": "PASS",
        "date": "2026-10-22", "environment": "offline",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "26 flash_one.py offline tests OK (logging, parsing, ack handling, no auto-retry).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "10", "source_revision": "1f89c0b", "evidence_status": "TEST_PASS",
    }),
    ("TestExecution", {
        "id": "test:pico-5b-upload", "label": "PICO-5B firmware upload",
        "test_name": "PICO-5B host->PIC32 upload", "result": "PASS",
        "date": "2026-10-23", "environment": "live",
        "status": "PROVEN_HARDWARE",
        "summary": "Host->PIC32 firmware upload completed (~1.3s), RECEIVED_CHECKSUM_OK, reached 123 SELECT_SLAVE.",
        "source_path": "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md", "source_location": "1",
        "source_revision": "9ff8e39", "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),
    ("TestExecution", {
        "id": "test:pico-5b-erase-attempt", "label": "PICO-5B ERASE/WRITE attempt",
        "test_name": "PICO-5B bootloader ERASE/WRITE", "result": "FAIL",
        "date": "2026-10-23", "environment": "live",
        "status": "PROVEN_HARDWARE",
        "summary": "ERASE (125) and WRITE (126) timed out (201/211); no bootloader ack received; WRITE_CONFIG (127) aborted on a 211/221 mismatch.",
        "source_path": "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md", "source_location": "1",
        "source_revision": "9ff8e39", "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),

    # ==================================================================
    # Observation
    # ==================================================================
    ("Observation", {
        "id": "obs:amp3-backplane-mapping", "label": "Amplifier 3 prototype backplane mapping",
        "what": "Address 3 -> SelectSlave HR0 0x04 -> AMP_ID7 -> RA3 (pin 5, active-low) -> net ID_7 -> DB15 connector J21 pin 7 -> amp DB15 pin 7 -> RA0 (bootloader entry). Off-by-N: logical slot N -> ID(N+4).",
        "date": "2026-10-23", "method": "source reconstruction (main_proto_backplane.c)",
        "status": "PROVEN_SOURCE",
        "summary": "On the prototype backplane, amplifier address 3 maps to J21 / ID_7 / RA3 (not J20 / ID_3).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "2", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("Observation", {
        "id": "obs:9to8-transition", "label": "9->8-bit transition captured",
        "what": "PIC32 sent 8-bit 0x55 0x03 0xE0 0x01 0x55 0xFF (ERASE) after 123 SELECT_SLAVE; UART switched 9->8-bit.",
        "date": "2026-10-23", "method": "PicoScope D1 capture",
        "status": "PROVEN_HARDWARE",
        "summary": "The 9->8-bit UART mode switch and the 8-bit ERASE frame were physically captured on D1 (scope capture 0b20f400...).",
        "source_path": "tools/picoscope/evidence/mcp/0b20f4005903b43ec12369105852a34a.npz",
        "source_location": "D1", "source_revision": "1f89c0b",
        "source_hash": "0b20f4005903b43ec12369105852a34a",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),
    ("Observation", {
        "id": "obs:erase-timeout", "label": "ERASE timed out",
        "what": "Command 125 (ERASE) returned ERASE_FLASH_RECEIVE_DATA_TIMEOUT (201); no bootloader ack.",
        "date": "2026-10-23", "method": "flash_one.py live log",
        "status": "PROVEN_HARDWARE",
        "summary": "ERASE received no bootloader response; timeout 201.",
        "source_path": "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md", "source_location": "1",
        "source_revision": "9ff8e39", "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),
    ("Observation", {
        "id": "obs:write-timeout", "label": "WRITE timed out",
        "what": "Command 126 (WRITE) returned WRITE_FLASH_RECEIVE_DATA_TIMEOUT (211).",
        "date": "2026-10-23", "method": "flash_one.py live log",
        "status": "PROVEN_HARDWARE",
        "summary": "WRITE received no bootloader response; timeout 211.",
        "source_path": "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md", "source_location": "1",
        "source_revision": "9ff8e39", "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),
    ("Observation", {
        "id": "obs:write-config-timeout-code-bug", "label": "WRITE_CONFIG timeout-code firmware bug",
        "what": "slavebootloaderroutines.c WRITExCONFIG timeout branch reports WRITE_FLASH_RECEIVE_DATA_TIMEOUT (211) as its TASK_COMMAND instead of WRITE_CONFIG_RECEIVE_DATA_TIMEOUT (221). Its LOG line says WRITE_CONFIG but the emitted code is 211.",
        "date": "2026-10-23", "method": "firmware source inspection (slavebootloaderroutines.c ~line 470)",
        "status": "PROVEN_SOURCE",
        "summary": "Confirmed firmware bug: command 127 (WRITE_CONFIG) reports timeout code 211 (WRITE_FLASH's), not 221. Diagnostic-only; fail-closed behavior is unaffected (any ERROR aborts).",
        "source_path": "TrackController5/firmware/src/slavebootloaderroutines.c",
        "source_location": "WRITExCONFIG timeout branch", "source_revision": "8fa3a6e", "evidence_status": "SOURCE_REF",
    }),
    ("Observation", {
        "id": "obs:write-config-timeout-mismatch", "label": "WRITE_CONFIG timeout-code mismatch",
        "what": "Command 127 (WRITE_CONFIG) returned WRITE_FLASH_RECEIVE_DATA_TIMEOUT (211), not the expected WRITE_CONFIG code 221.",
        "date": "2026-10-23", "method": "flash_one.py live log",
        "status": "PROVEN_HARDWARE",
        "summary": "The PIC32 reported WRITE_FLASH's timeout code (211) for WRITE_CONFIG; the driver aborted because it expected 221.",
        "source_path": "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md", "source_location": "1",
        "source_revision": "9ff8e39", "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),
    ("Observation", {
        "id": "obs:csharp-automatic-firmware-update", "label": "C# automatic firmware-update (flash-on-mismatch)",
        "what": "FlashFwTrackamplifiersStep State0 compares the HEX file checksum against each detected amplifier's HR11 (SwChecksum); any detected slave 1..50 whose HR11 mismatches the file is auto-flashed via the fleet 122 (EXEC_FW_STATE_FLASH_ALL_SLAVES).",
        "date": "2026-10-23", "method": "C# source inspection (FlashFwTrackamplifiersStep.cs)",
        "status": "PROVEN_SOURCE",
        "summary": "Normal C# initialization CAN automatically flash firmware: a checksum mismatch during startup triggers a fleet flash. This is the automatic-update risk to rule out when interpreting the PO's 'amps functioned' observation.",
        "source_path": "SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Initialization/Steps/FlashFwTrackamplifiersStep.cs",
        "source_location": "State0_DetermineFlashRequirementAsync", "source_revision": "26a7bf8", "evidence_status": "SOURCE_REF",
    }),
    ("Observation", {
        "id": "obs:po-csharp-init-reported", "label": "PO-reported C# initialization (2026-10-23)",
        "what": "Product Owner reports normal C# initialization was executed after the PICO-5B experiment and all installed amplifiers were observed functioning afterward.",
        "date": "2026-10-23", "method": "PO operator report",
        "status": "PROVEN_HARDWARE",
        "summary": "PO-REPORTED PHYSICAL OBSERVATION (not independent register/capture evidence): C# init ran + amplifiers functioned. Does not by itself prove firmware was never erased/modified/restored.",
        "source_path": "docs/K5-CSHARP-INIT-REFERENCE-AND-PICO5B-COMPARISON.md",
        "source_location": "3", "source_revision": "26a7bf8", "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),
    ("Observation", {
        "id": "obs:csharp-init-log-no-flash", "label": "C# init log: 0 slaves require flashing (2026-10-23)",
        "what": "Logging/23-10-2026_TrackAppLog.txt (three sessions 09:00, 13:59, 15:37): File checksum = 0x251F; 0 slaves require flashing; no EXEC_FW_STATE_FLASH_ALL_SLAVES (122) emitted.",
        "date": "2026-10-23", "method": "runtime log analysis (read-only)",
        "status": "PROVEN_HARDWARE",
        "summary": "The auto-flash step ran and reported 0 slaves requiring flash with file checksum 0x251F, i.e. every detected amp's HR11 matched 0x251F. Consistent with ERASE never executing; inconsistent with an erased (0xFFFF) amplifier.",
        "source_path": "Logging/23-10-2026_TrackAppLog.txt",
        "source_location": "State0_DetermineFlashRequirementAsync (line 158)", "source_revision": "26a7bf8", "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),
    ("Observation", {
        "id": "obs:po-normal-start", "label": "PO verified normal start",
        "what": "PO confirmed via the Siebwalde app that all amplifiers start normally and the target was not stuck in bootloader.",
        "date": "2026-10-23", "method": "PO operator check",
        "status": "UNKNOWN",
        "summary": "PO reported no amplifier stuck in bootloader after the PICO-5B attempt; consistent with ERASE not having executed, but the exact target state was not independently re-verified.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "operator", "source_revision": "1f89c0b", "evidence_status": "UNVERIFIED", "observed_date": "2026-10-23",
    }),
    ("Observation", {
        "id": "obs:k6b-cmd113-readback", "label": "K6-B command-113 physical readback",
        "what": "K6-B single command-113 OBSERVE (2026-10-24) against PIC32 192.168.1.193 returned version 0.1.0, protocol 0, build 2026-10-20 (OBSERVE_PASS). One request `aa71`, 82-byte reply `aa0a71...`, source 192.168.1.193, SHA-256 f40a31edf3924488f82e1ee542bac9400d6dd9d3864249b8492504acb4a669c8.",
        "date": "2026-10-24", "method": "UDP command-113 via tools/diagnostics/live_transport.py (allowlisted, single-use)",
        "status": "PROVEN_HARDWARE",
        "summary": "Physical K6-B command-113 readback returned version 0.1.0 / protocol 0 / build 2026-10-20 (OBSERVE_PASS); proves the controller REPORTS 0.1.0, not independently the flash image content.",
        "source_path": "tools/diagnostics/k6b_observe.py",
        "source_revision": "ae18cb4",
        "evidence_ref": "Logging/K6B/k6b-observe-20261024T092944Z.json",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-24",
    }),
    ("Observation", {
        "id": "obs:k6b-version-mismatch", "label": "K6-B firmware version discrepancy (0.1.0 vs 1.0.0)",
        "what": "K6-B physical readback 0.1.0 / protocol 0 / '2026-10-20' differs from master-branch version.h 1.0.0 / protocol 0x01 / '2026-10-17'; the version.h bump was never flashed to the physical PIC32. Deployed firmware 0.1.0 matches the recorded firmware:pic32-0.1.0 node.",
        "date": "2026-10-24", "method": "command-113 readback vs source version.h",
        "status": "PROVEN_HARDWARE",
        "summary": "K6-B physical readback (0.1.0 / protocol 0 / '2026-10-20') differs from master-branch version.h (1.0.0 / protocol 0x01 / '2026-10-17'); the version.h bump was never flashed, so deployed firmware remains 0.1.0.",
        "source_path": "TrackController5/firmware/src/version.h",
        "source_location": "6-13", "source_revision": "5269624e",
        "evidence_ref": "Logging/K6B/k6b-observe-20261024T092944Z.json",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-24",
    }),
    ("Observation", {
        "id": "obs:k7-pic32-identity-transition", "label": "PIC32 firmware identity transition (1.0.0 -> 0.1.0)",
        "what": "PIC32 1.0.0 was deployed (ICD3 flash + command-113 readback 1.0.0/protocol 0x01/'2026-10-17' confirmed 2026-10-19, firmware-flash-recovery §2.2/§2.3), but the K6 readback (2026-10-24) reported 0.1.0/protocol 0/'2026-10-20'. The current physical image does NOT match master version.h (1.0.0); the 1.0.0→0.1.0 transition is UNKNOWN (no captured re-flash evidence).",
        "date": "2026-10-24", "method": "source history (version.h/controller.c) + K6 command-113 readback",
        "status": "PROVEN_HARDWARE",
        "summary": "Deployed PIC32 1.0.0 (proven 2026-10-19) does not match the K6 readback 0.1.0/protocol 0/'2026-10-20'; the 1.0.0->0.1.0 transition is UNKNOWN (no captured re-flash evidence).",
        "source_path": "docs/K7-FIRMWARE-IDENTITY-TIMELINE.md",
        "source_location": "timeline", "source_revision": "bd5a9fa2",
        "evidence_ref": "Logging/K6B/k6b-observe-20261024T092944Z.json",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-24",
    }),
    ("Observation", {
        "id": "obs:k7-cmd124-ineligible", "label": "Command 124 INELIGIBLE for read-only grant",
        "what": "Command 124 GET_BOOTLOADER_VERSION is INELIGIBLE for a read-only grant: it requires the target already in bootloader (state-changing 123+reset), is unaddressed (cannot identify which amplifier), returns bootloader version not the application HR11 checksum, and is reported non-functional (PO-removed from the pre-ERASE gate).",
        "date": "2026-10-24", "method": "source inspection (slavefwhandler.c, slavebootloaderroutines.c, flash_one.py) + docs",
        "status": "PROVEN_SOURCE",
        "summary": "Command 124 is not a read-only application-identity probe: it needs bootloader entry (123+reset), is unaddressed, returns bootloader (not HR11) version, and is non-functional (PO-removed from the pre-ERASE gate).",
        "source_path": "TrackController5/firmware/src/slavefwhandler.c",
        "source_location": "EXEC_FW_STATE_GET_BOOTLOADER_VERSION", "source_revision": "bd5a9fa2",
        "evidence_status": "SOURCE_REF",
    }),
    ("Observation", {
        "id": "obs:k7-next-readonly-hr11-slaveryinfo", "label": "Next read-only path: passive SLAVEINFO HR11",
        "what": "Next read-only operation to establish PIC18 amp-1/amp-3 application identity: passive SLAVEINFO observation of HR11 (+HR7/HR21/HR22) at assigned addresses 1/3 during SERVICE_TASKS, via the cyclic FC03 poll -> SLAVEINFO stream (slavehandler.c SIP4 reads HOLDINGREG10 qty=2 -> MASTER_SLAVE_DATA[addr].HoldingReg[11]). No bootloader, no 123/124, no reset. A cold-start readback (100->106->101->104->105) is NOT fully passive (105 ENABLE).",
        "date": "2026-10-24", "method": "source inspection (slavehandler.c, PetitModbus.c, TrackAmplifier4.X)",
        "status": "PROVEN_SOURCE",
        "summary": "Passive SLAVEINFO observation of HR11 at addresses 1/3 during SERVICE_TASKS is the next read-only path to establish amp-1/amp-3 application identity; no bootloader/123/124/reset.",
        "source_path": "TrackController5/firmware/src/slavehandler.c",
        "source_location": "SIP4 MESSAGE3", "source_revision": "bd5a9fa2",
        "evidence_status": "SOURCE_REF",
    }),
    ("Observation", {
        "id": "obs:k8-slaveryinfo-passive-blocked", "label": "K8 passive SLAVEINFO observation BLOCKED",
        "what": "K8 feasibility (2026-10-24): a genuinely passive SLAVEINFO observation of PIC18 amps 1/3 is BLOCKED. SLAVEINFO (0xFF) is unicast to the single connected client (UDP 10001, server 10000; no multicast/broadcast), request-driven (command 106 START_DATA_UPLOAD sets UPLOAD_STATE_ALL), and the meaningful SERVICE_TASKS stream requires command 105 ENABLE (HR1=0x8000, outputs on) plus a 100ms motion-permission broadcast. 101/104 also write. Access alternatives: state-changing read-only-enablement (250->100->101->104->106->105 via KIS-09 guard), L2 capture of an already-running stream, or warm-reconnect (250 only) against an already-enabled master. Ownership is single-connection; the write-allowlist/guard is process-local (no cross-process arbitration).",
        "date": "2026-10-24",
        "method": "source inspection (ethernet.c, mbus.c, controller.c, slavestartup.c, slavehandler.c, C# RawUdpTransport/TrackCommClientAsync/ReadOnlyTelemetryValidationHost)",
        "status": "PROVEN_SOURCE",
        "summary": "K8 feasibility (2026-10-24): a genuinely passive SLAVEINFO observation of PIC18 amps 1/3 is BLOCKED (unicast + request-driven 106 + state-changing 105 ENABLE); access requires a non-passive method.",
        "source_path": "docs/K8-SLAVEINFO-FEASIBILITY.md",
        "source_location": "delivery mechanism", "source_revision": "14789a5a",
        "evidence_status": "SOURCE_REF",
    }),
    ("Observation", {
        "id": "obs:k8-po-firewall-observation",
        "label": "K8 PO-reported Windows Firewall dialogs",
        "what": "Product Owner reported that approximately 2-3 Windows Firewall permission dialogs were pending during K8 physical comm testing (operator-reported; not independently verified). No executable names or firewall-rule details are asserted here.",
        "date": "2026-10-25",
        "method": "PO operator report",
        "status": "UNKNOWN",
        "summary": "PO-REPORTED (UNVERIFIED): ~2-3 Windows Firewall permission dialogs pending during K8 physical comm testing; no exe names or rule details asserted.",
        "source_path": "docs/K8-FIREWALL-CORRECTIVE.md",
        "source_location": "Corrected root cause", "source_revision": "61c31f48",
        "evidence_status": "UNVERIFIED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:k8-cmd250-timeout-cmd113-success",
        "label": "K8 command-250 timeout / command-113 success",
        "what": "During K8 physical comm testing (2026-10-25): command 250 (CLIENT_CONNECTION_REQUEST, sent by SiebwaldeApp.Koploper.Validation.Host.exe) timed out with no CONTROLLER/CONNECTED/DONE ack; command 113 (GET_CONTROLLER_VERSION, sent by python.exe) succeeded and returned 0.1.0 / protocol 0 / build 2026-10-20, byte-identical to the K6 readback. OBSERVED-RESPONSE vs INFERRED-CAUSE kept separate: the 113 reply proves what the controller REPORTS, not why 250 timed out.",
        "date": "2026-10-25",
        "method": "live UDP transport (K8 physical comm test)",
        "status": "PROVEN_HARDWARE",
        "summary": "Command 250 timed out (no CONTROLLER/CONNECTED/DONE ack); command 113 succeeded (0.1.0/protocol 0/2026-10-20, byte-identical to K6). Observed response vs inferred cause kept distinct.",
        "source_path": "docs/K8-FIREWALL-CORRECTIVE.md",
        "source_location": "Corrected root cause", "source_revision": "61c31f48",
        "evidence_ref": "Logging/K6B/k6b-observe-20261024T092944Z.json",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:k8-next-validation-firewall",
        "label": "K8 next validation: firewall readiness (8 steps)",
        "what": "Required K8 validation before re-classifying the 250 timeout: (1) identify the exact test executables; (2) inspect Windows Firewall rules and active network profiles; (3) determine the inbound UDP ports involved; (4) verify socket binding and routing; (5) compare command 113 vs 250 behavior; (6) analyze packet evidence; (7) determine the firewall-rule correction; (8) retest under a fresh LIVE_HARDWARE grant.",
        "date": "2026-10-25",
        "method": "planned validation (not yet executed)",
        "status": "UNKNOWN",
        "summary": "Next K8 validation requires 8 steps (identify exes, inspect firewall rules/profiles, determine inbound UDP ports, verify socket binding + routing, compare 113 vs 250, analyze packet evidence, determine firewall-rule correction, retest under LIVE_HARDWARE). Not yet executed.",
        "source_path": "docs/K8-FIREWALL-CORRECTIVE.md",
        "source_location": "Minimal corrective live test", "source_revision": "61c31f48",
        "evidence_status": "UNVERIFIED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:k8b-legacy-12-register-slaveryinfo-layout",
        "label": "K8B deployed firmware uses legacy 12-register SLAVEINFO",
        "what": "The deployed PIC32 firmware sends the LEGACY 12-register SLAVE_INFO layout (NUMBER_OF_HOLDING_PETITREGISTERS=12), NOT the 23-register Phase-1 layout. On the 82-byte udpTrans_t wire frame: footer 0x55 at offset 40 (not 62/64), HoldingReg[0..11] at 6..29, MbCommError (uint32 LE) at 34..37, MbReceiveCounter 30..31, MbSentCounter 32..33, MbExceptionCode 38, SpiCommErrorCounter 39. The legacy 12-register layout was also confirmed against the historical commit e2164b43 (PetitModbus.h). The C# decoder previously assumed the 23-register Phase-1 layout and rejected every frame (footer/length mismatch), which masked a healthy data-bearing SLAVEINFO stream as NO_AMPLIFIERS_DETECTED.",
        "date": "2026-10-25",
        "method": "raw UDP capture (33310 frames) + C struct source (PetitModbus.h e2164b43) + fresh physical readback",
        "status": "PROVEN_HARDWARE",
        "summary": "Deployed firmware uses the legacy 12-register SLAVEINFO layout (footer 0x55 at offset 40, not 62); the prior 23-register decoder rejected every frame, masking a healthy stream. See docs/K8B-COMMUNICATION-RECOVERY.md §3.",
        "source_path": "docs/K8B-COMMUNICATION-RECOVERY.md",
        "source_location": "3", "source_revision": "a49ea004",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:k8b-fresh-pic18-readback-251f",
        "label": "K8B fresh PIC18 HR11=0x251F readback (amps 1/3/4/6)",
        "what": "Fresh physical readback through the fixed decoder (2026-10-25): amps 1/3/4/6 all report HR11=0x251F (legacy firmware checksum, byte-stable vs the 2026-10-23 C# log), HR7 raw 0x002D (amp1) / 0x002E (amps 3/4/6) — legacy HR7 is NOT a Phase-1 identity register — and HR21/HR22 read 0 classified UNSUPPORTED (the legacy 12-register firmware has no version registers). MbCommError=SLAVE_DATA_OK for all four.",
        "date": "2026-10-25",
        "method": "C# Validation.Host readonly-validate --live --read-registers + Diagnostics MCP run_controller_session (V4)",
        "status": "PROVEN_HARDWARE",
        "summary": "Fresh physical readback: amps 1/3/4/6 HR11=0x251F (legacy checksum), HR21/HR22 UNSUPPORTED, HR7 raw (not identity). See docs/K8B-COMMUNICATION-RECOVERY.md §4.",
        "source_path": "docs/K8B-COMMUNICATION-RECOVERY.md",
        "source_location": "4", "source_revision": "a49ea004",
        "evidence_ref": "C:/Localdata/Siebwalde-Bewaarmap/04_HARDWARE_EVIDENCE/K8B/SLAVEINFO-LAYOUT/k8b-physical-readback-success.txt",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:k8b-diagnostics-mcp-active-v4",
        "label": "K8B Diagnostics MCP active controller session validated (V4)",
        "what": "The Diagnostics MCP's new active surface (docs/K8B-DIAGNOSTICS-MCP-ACTIVE-DESIGN.md) was validated live: diagnostics_run_controller_session returned PASS with the 6-command cold start 250->100->101->104->106->105 all 1/1 DONE, readiness READY, shutdown clean_shutdown, 4 amplifiers read back. The single-use grant was consumed atomically; re-running returned authorization_absent with no I/O. Offline tools remained side-effect-free.",
        "date": "2026-10-25",
        "method": "Diagnostics MCP orchestrator -> C# Validation.Host subprocess (live, scoped K8B LIVE_HARDWARE)",
        "status": "PROVEN_HARDWARE",
        "summary": "Diagnostics MCP is now operational for controlled live controller sessions (B1..B5): PASS, clean_shutdown, 4 amps, single-use grant proven, offline tools unchanged.",
        "source_path": "docs/K8B-DIAGNOSTICS-MCP-ACTIVE-DESIGN.md",
        "source_location": "9.2", "source_revision": "a49ea004",
        "evidence_ref": "C:/Localdata/Siebwalde-Bewaarmap/04_HARDWARE_EVIDENCE/K8B/SLAVEINFO-LAYOUT/k8b-mcp-active-v4-validation.txt",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:p2-b1-rollback-image-proven",
        "label": "P2-B1 rollback image byte-identical to deployed 0.1.0",
        "what": "The deployed 0.1.0 is the L1 build (commit 39577845, release/legacy-l1/PIC32-Master-L1-version-0.1.0.hex), NOT Baseline-0 A90E44. A non-destructive ICD3 readback (-GF) of the deployed firmware was byte-compared against the L1 image: 267624/267624 populated bytes identical (0 differing, 0 missing). The L1 image is therefore a byte-identical backup, not merely a functionally-equivalent candidate.",
        "date": "2026-10-25",
        "method": "ICD3 readback + Intel HEX byte comparison (compare_hex.py)",
        "status": "PROVEN_HARDWARE",
        "summary": "Rollback image EC7E3E6E (L1 0.1.0) proven byte-identical to the deployed firmware (267624/267624 bytes). A90E44 (Baseline-0, no command 113) is NOT the correct rollback target.",
        "source_path": "docs/P2-B1-FIRMWARE-IMAGE-PROVENANCE.md",
        "source_location": "1", "source_revision": "61fd2a4a",
        "evidence_ref": "C:/Localdata/Siebwalde-Bewaarmap/04_HARDWARE_EVIDENCE/PIC32-1.0.0-Migration/p2-b1-phase-b-rollback-verified.txt",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:p2-b1-pic32-migration-proven",
        "label": "P2-B1 PIC32 0.1.0 -> 1.0.0 -> 0.1.0 -> 1.0.0 physically proven",
        "what": "Full PIC32 migration round-trip physically proven via ICD3: 0.1.0 (L1 EC7E3E6E) -> 1.0.0 (6CEC61D4) -> rollback 0.1.0 (EC7E3E6E) -> final 1.0.0 (6CEC61D4). Each program verified byte-identical (272632/272632 for 1.0.0). Final command 113 = 1.0.0/protocol 1/build 2026-10-17. PIC18 amps 1/3/4/6 remained HR11=0x251F throughout (no PIC18 flash).",
        "date": "2026-10-25",
        "method": "ICD3 program+verify + command 113 + cold-start + HR11 readback (scoped PIC32 FLASH authority)",
        "status": "PROVEN_HARDWARE",
        "summary": "PIC32 1.0.0/protocol 1 deployed and verified; rollback to 0.1.0 physically demonstrated; final state 1.0.0. PIC18 unchanged (HR11=0x251F).",
        "source_path": "docs/P2-B1-FIRMWARE-IMAGE-PROVENANCE.md",
        "source_location": "2", "source_revision": "61fd2a4a",
        "evidence_ref": "C:/Localdata/Siebwalde-Bewaarmap/04_HARDWARE_EVIDENCE/PIC32-1.0.0-Migration/p2-b1-phase-i-final-1.0.0.txt",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:p2-b1-1.0.0-legacy-pic18-inert",
        "label": "P2-B1: 1.0.0 classifies legacy PIC18 as incompatible (detected-but-inert)",
        "what": "Under PIC32 1.0.0, the legacy 12-register PIC18 amps (HR11=0x251F, HR7=0) are detected (detected=1) but fail the Phase-1 HR7 identity shape check, so the master does not address/enable/poll them (mbCommError=IDLE). HR11=0x251F is still read back (no PIC18 flash). This is the expected Phase-1 behavior; motion requires a future PIC18 upgrade. PO-accepted.",
        "date": "2026-10-25",
        "method": "SLAVEINFO raw capture on 1.0.0 (30845 frames)",
        "status": "PROVEN_HARDWARE",
        "summary": "1.0.0 master detects but does not enable legacy PIC18 (HR7=0 fails identity gate); amps remain HR11=0x251F, inert. PO-accepted.",
        "source_path": "docs/P2-B1-PIC32-1.0.0-LEGACY-PIC18-COMPATIBILITY.md",
        "source_location": "3", "source_revision": "61fd2a4a",
        "evidence_ref": "C:/Localdata/Siebwalde-Bewaarmap/04_HARDWARE_EVIDENCE/PIC32-1.0.0-Migration/p2-b1-phase-g-1.0.0-validation.txt",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-25",
    }),
    ("Observation", {
        "id": "obs:p2-b1-reset-required-after-program",
        "label": "P2-B1: clean MCLR reset required after every ICD3 program/readback",
        "what": "After every ICD3 program (-F) or readback (-GF), the PIC32 Ethernet stack can wedge (connected-UDP reply misdirected to a stale client address, or a stuck WAITING_FOR_CLIENT/wrong-handshake loop). A clean MCLR reset (ipecmd -I -OL) plus ~15-20s for boot+DHCP is REQUIRED before the first runtime communication, or CONNECT times out. Applied identically after both the 1.0.0 flash and the 0.1.0 rollback.",
        "date": "2026-10-25",
        "method": "observed during P2-B1 flash/rollback verification",
        "status": "PROVEN_HARDWARE",
        "summary": "A clean MCLR reset + boot wait is a reproducibility requirement after every ICD3 program/readback, not a firmware defect.",
        "source_path": "docs/P2-B1-FIRMWARE-IMAGE-PROVENANCE.md",
        "source_location": "procedural note", "source_revision": "61fd2a4a",
        "evidence_ref": "C:/Localdata/Siebwalde-Bewaarmap/04_HARDWARE_EVIDENCE/PIC32-1.0.0-Migration/p2-b1-phase-h-rollback-verified.txt",
        "evidence_status": "OBSERVED", "observed_date": "2026-10-25",
    }),

    # ==================================================================
    # Incident
    # ==================================================================
    ("Incident", {
        "id": "incident:pico-5b", "label": "PICO-5B supervised bootloader flash",
        "incident_date": "2026-10-23", "severity": "HIGH", "target": "single amplifier (3 per directive; session referred to amp 1)",
        "status": "PROVEN_HARDWARE",
        "summary": "A supervised single-target bootloader flash sent 250->103->120->141x480->121->151->123 then ERASE(125)/WRITE(126)/WRITE_CONFIG(127); all destructive commands timed out without bootloader acks. The driver's ASSUME-OK (--proceed-on-timeout) continued past unconfirmed ERASE/WRITE. Target application state is UNKNOWN.",
        "source_path": "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md",
        "source_location": "5", "source_revision": "9ff8e39", "evidence_status": "OBSERVED", "observed_date": "2026-10-23",
    }),

    # ==================================================================
    # Hypothesis
    # ==================================================================
    ("Hypothesis", {
        "id": "hyp:bootloader-replies-never-worked", "label": "Bootloader replies never work on this prototype",
        "hypothesis": "The PIC18 bootloader RS-422 reply path is broken on the prototype, so bootloader responses never reach the PIC32/host.",
        "evidence_for": "ERASE/WRITE/WRITE_CONFIG all timed out; GET_BOOTLOADER_VERSION historically returned no output.",
        "evidence_against": "The legacy fleet flash (122) historically worked (bootloader acks were handled internally by the PIC32 FlashSequencer); the 1.0.0 round-trip (131/132) on amp 1 completed on hardware.",
        "status": "CONTRADICTED",
        "summary": "Contradicted: bootloader-driven flashing has worked historically (fleet 122 and single-target 131/132), so 'never works' is false.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "DERIVED",
    }),
    ("Hypothesis", {
        "id": "hyp:pic18-not-power-cycled", "label": "PIC18 not power-cycled blocked bootloader entry",
        "hypothesis": "The 123 SELECT_SLAVE write to boot address 0xAA was dropped because the running PIC18 listens on its assigned address; the PIC18 was never power-cycled, so the bootloader never entered and ERASE/WRITE never executed.",
        "evidence_for": "123 writes HR11=0 to 0xAA; a running amp answers on its assigned address (source-verified, ref 2.5); only the PIC32 was reset, not the PIC18s.",
        "evidence_against": "",
        "status": "INFERRED",
        "summary": "Best-supported explanation for why ERASE never executed; not directly captured, but consistent with the source-established 131-requires-global-reset behavior.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "DERIVED",
    }),
    ("Hypothesis", {
        "id": "hyp:erase-not-executed", "label": "ERASE did not execute",
        "hypothesis": "The ERASE/WRITE commands did not actually modify the target firmware (bootloader never entered, so no erase/write occurred).",
        "evidence_for": "No bootloader ack; PO observed all amps start normally; K5 log evidence: 23-10-2026 C# init logged 'File checksum = 0x251F' + '0 slaves require flashing' and emitted no 122 (so all amps' HR11 still 0x251F).",
        "evidence_against": "",
        "status": "INFERRED",
        "summary": "Strengthened by K5 log evidence (no auto-flash, HR11 still 0x251F); still not an independent register capture, so the target application state remains UNKNOWN in the strictest sense until a fresh independent readback.",
        "source_path": "docs/K5-CSHARP-INIT-REFERENCE-AND-PICO5B-COMPARISON.md",
        "source_location": "6", "source_revision": "26a7bf8", "evidence_status": "DERIVED",
    }),
    ("Hypothesis", {
        "id": "hyp:k8-firewall-blocked-udp",
        "label": "K8 command-250 timeout firewall-induced",
        "hypothesis": "Windows Firewall may have blocked inbound UDP for SiebwaldeApp.Koploper.Validation.Host.exe (its permission dialog was still pending), explaining the command-250 timeout.",
        "evidence_for": "K8B (2026-10-25) confirmed a Windows Firewall INBOUND BLOCK rule on the canonical Release exe path (bin\\release\\...\\validation.host.exe) that overrode the Allow rules; the Debug path had Allow. On the first run of the K8B worktree exe path, a fresh inbound Block rule appeared and, after PO approval, was replaced by Allow (UDP+TCP, Public). With the Allow rule in place, command 250 was ACKed 1/1 and the full cold start succeeded.",
        "evidence_against": "None remaining: the firewall rule snapshots were captured (Get-NetFirewallRule) at each step, and the 250 ack success directly followed the Allow-rule approval.",
        "status": "PROVEN_HARDWARE",
        "summary": "CONFIRMED (PROVEN_HARDWARE): the K8 command-250 timeout was host-side firewall. A Block rule on the exe path overrode Allow; after PO approval replaced it with Allow, 250 was ACKed 1/1 and the cold start succeeded. See docs/K8B-COMMUNICATION-RECOVERY.md §2.",
        "source_path": "docs/K8B-COMMUNICATION-RECOVERY.md",
        "source_location": "2", "source_revision": "a49ea004",
        "evidence_status": "OBSERVED",
    }),
    ("Hypothesis", {
        "id": "hyp:k8-protocol-mismatch-250",
        "label": "K8 command-250 protocol-mismatch (ALTERNATIVE)",
        "hypothesis": "The deployed PIC32 0.1.0 CONNECT (250) semantics may be incompatible with protocol-0x01, so 250 never returns CONTROLLER/CONNECTED/DONE against the current host. (ALTERNATIVE to the firewall hypothesis.)",
        "evidence_for": "Command 250 timed out with no ack even though the controller is reachable (command 113 succeeded).",
        "evidence_against": "K8B (2026-10-25) proved the deployed firmware ACKs 250 normally: after the firewall Allow rule was in place, CLIENT_CONNECTION_REQUEST (250) was transmitted/acknowledged 1/1 and the full cold start 250->100->101->104->106->105 all ACKed 1/1. The 250 timeout was therefore host-side (firewall + a legacy-12-register decoder layout mismatch), NOT a protocol-0x01 incompatibility.",
        "status": "CONTRADICTED",
        "summary": "CONTRADICTED: the deployed firmware's CONNECT(250) is NOT protocol-incompatible. After the firewall Allow rule, 250 ACKed 1/1. The timeout was host-side (firewall + decoder layout), documented in docs/K8B-COMMUNICATION-RECOVERY.md §2.",
        "source_path": "docs/K8B-COMMUNICATION-RECOVERY.md",
        "source_location": "2", "source_revision": "a49ea004",
        "evidence_status": "OBSERVED",
    }),

    # ==================================================================
    # Decision
    # ==================================================================
    ("Decision", {
        "id": "decision:software-selection-only", "label": "Software-only target selection (PO)",
        "decision": "The PICO-5B session used software-only target selection, overriding the preflight's 'physical isolation mandatory' finding.",
        "decided_by": "Product Owner", "date": "2026-10-23",
        "status": "PROVEN_SOURCE",
        "summary": "PO chose software-only selection for the single-target test (overrode mandatory physical isolation).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "13", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("Decision", {
        "id": "decision:remove-124-probe", "label": "Remove 124 GET_BOOTLOADER_VERSION probe (PO)",
        "decision": "The 124 GET_BOOTLOADER_VERSION probe was removed because it has never returned output; the pre-ERASE gate now blocks only on the stdin ERASE token.",
        "decided_by": "Product Owner", "date": "2026-10-23",
        "status": "PROVEN_SOURCE",
        "summary": "124 probe removed from the pre-ERASE gate.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "4", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("Decision", {
        "id": "decision:k3-fail-closed-correction", "label": "K3 fail-closed driver correction (PO)",
        "decision": "Removed the --proceed-on-timeout (ASSUME-OK) path from flash_one.py; added an explicit fail-closed FlashState machine (PRE_ERASE -> ERASE_REQUESTED_UNCONFIRMED -> ERASE_CONFIRMED -> WRITE_IN_PROGRESS -> PROGRAMMING_UNCONFIRMED -> VERIFIED_COMPLETE, plus FAILED_UNKNOWN_HARDWARE_STATE). Any timeout/ERROR/missing ack on 125-129 aborts; never converted to success.",
        "decided_by": "Product Owner", "date": "2026-10-23",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "K3 corrected the unsafe ASSUME-OK behavior: the driver now fails closed (43 offline tests, incl. 13 fail-closed regression tests).",
        "source_path": "docs/K3-FAIL-CLOSED-FIRMWARE-DIAGNOSTICS.md",
        "source_location": "2", "source_revision": "e880fde", "evidence_status": "TEST_PASS",
    }),
    ("Decision", {
        "id": "decision:proceed-on-timeout", "label": "--proceed-on-timeout / ASSUME-OK (PO)",
        "decision": "Added --proceed-on-timeout to treat 125-129 RECEIVE_DATA_TIMEOUT as non-fatal (assume erase ok, proceed).",
        "decided_by": "Product Owner", "date": "2026-10-23",
        "status": "CONTRADICTED",
        "summary": "This decision is now flagged UNSAFE: no ASSUME-OK may be accepted for an unconfirmed destructive operation. Superseded by the containment directive and REMOVED in K3 (fail-closed correction).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "6", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("Decision", {
        "id": "decision:containment", "label": "PICO-5B containment (PO directive)",
        "decision": "No additional flash/reset/recovery/power-cycle or state-changing hardware operations are authorized. The Python flash tooling must not be used for further live tests until its fail-closed behavior is independently verified.",
        "decided_by": "Product Owner", "date": "2026-10-23",
        "status": "PROVEN_SOURCE",
        "summary": "Containment directive: stop live flashing; verify fail-closed behavior before any further live test.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "1", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),

    # ==================================================================
    # EvidenceSource
    # ==================================================================
    ("EvidenceSource", {
        "id": "evidence:pico-5b-validation-doc", "label": "PICO-5B physical flash validation (live outcome)",
        "kind": "doc", "path": "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md", "format": "markdown",
        "status": "PROVEN_HARDWARE",
        "summary": "Committed record of the PICO-5B live outcome (ERASE/WRITE timeouts, ASSUME-OK unsafe, target UNKNOWN).",
        "source_path": "docs/PICO-5B-PHYSICAL-FLASH-VALIDATION.md",
        "source_location": "header", "source_revision": "9ff8e39", "evidence_status": "OBSERVED",
    }),
    ("EvidenceSource", {
        "id": "evidence:techref-doc", "label": "Firmware flash/recovery technical reference",
        "kind": "doc", "path": "docs/firmware-flash-recovery-technical-reference.md", "format": "markdown",
        "status": "PROVEN_SOURCE",
        "summary": "Authoritative technical synthesis of firmware flashing, recovery, baselines and hardware bring-up.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "header", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("EvidenceSource", {
        "id": "evidence:preflight-doc", "label": "PICO-5B physical session preflight",
        "kind": "doc", "path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md", "format": "markdown",
        "status": "PROVEN_SOURCE",
        "summary": "PICO-5B supervised bootloader test preparation (corrected ERASE boundary, amp-3 mapping, exclusivity, recovery, operator checklist).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "header", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("EvidenceSource", {
        "id": "evidence:profile-b-doc", "label": "PICO-5B profile B measurement plan",
        "kind": "doc", "path": "docs/PICOSCOPE-PROFILE-B-BOOTLOADER-MEASUREMENT-PLAN.md", "format": "markdown",
        "status": "PROVEN_SOURCE",
        "summary": "Bootloader measurement plan (B1 slot-select, B2 9->8-bit transition, B3 reset).",
        "source_path": "docs/PICOSCOPE-PROFILE-B-BOOTLOADER-MEASUREMENT-PLAN.md",
        "source_location": "header", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("EvidenceSource", {
        "id": "evidence:scope-0b20f400", "label": "PICO-5B B2 9->8-bit capture",
        "kind": "capture", "path": "tools/picoscope/evidence/mcp/0b20f4005903b43ec12369105852a34a.npz",
        "format": "npz", "hash": "0b20f4005903b43ec12369105852a34a",
        "status": "PROVEN_HARDWARE",
        "summary": "Scope capture of the 9->8-bit transition + 8-bit ERASE frame on D1.",
        "source_path": "tools/picoscope/evidence/mcp/0b20f4005903b43ec12369105852a34a.json",
        "source_location": "D1", "source_revision": "1f89c0b", "evidence_status": "OBSERVED",
    }),
    ("EvidenceSource", {
        "id": "evidence:rollback-0x251F", "label": "PIC18 rollback HEX 0x251F",
        "kind": "artifact", "path": "release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex",
        "format": "intel-hex", "hash": "B848EF040DBA538D983EE92D96D84119AF922F686C7C879892E14323E1C1887D",
        "status": "PROVEN_HARDWARE",
        "summary": "Immutable PIC18 rollback artifact (checksum 0x251F).",
        "source_path": "release/baseline-0/PIC18-TrackAmplifier-legacy-0x251F.hex",
        "source_location": "artifact", "source_revision": "667a490", "evidence_status": "OBSERVED",
    }),
    ("EvidenceSource", {
        "id": "evidence:diag-0x2D00", "label": "PIC18 diagnostic HEX 0x2D00",
        "kind": "artifact", "path": "release/diagnostic-pico-5b/TrackAmplifier4.X.production.hex (external worktree)",
        "format": "intel-hex", "hash": "ABAEF621C25E1AB046FB98834E7A5DB84D49C0FBEAF49AFEAF62DAAF082D9E53",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Temporary diagnostic image used in PICO-5B. The HEX file lives in an external worktree (Siebwalde-pico5b-checksum); the SHA-256 is the durable reference.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "8", "source_revision": "bea4251", "evidence_status": "TEST_PASS",
    }),
    ("EvidenceSource", {
        "id": "evidence:flash-one-driver", "label": "flash_one.py driver",
        "kind": "source", "path": "TrackController5/Python/flash_one.py", "format": "python",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Single-target flash driver (43 offline tests post-K3).",
        "source_path": "TrackController5/Python/flash_one.py",
        "source_location": "driver", "source_revision": "401291f", "evidence_status": "TEST_PASS",
    }),
    ("EvidenceSource", {
        "id": "evidence:slavefwhandler", "label": "slavefwhandler.c",
        "kind": "source", "path": "TrackController5/firmware/src/slavefwhandler.c", "format": "c",
        "status": "PROVEN_SOURCE",
        "summary": "PIC32 slave firmware handler (single-target command sequence, SelectSlave HR0 mapping).",
        "source_path": "TrackController5/firmware/src/slavefwhandler.c",
        "source_location": "slavefwhandler.c:253-450", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("EvidenceSource", {
        "id": "evidence:bootloader", "label": "PIC18 bootloader source",
        "kind": "source", "path": "TrackAmplifierBootLoader.X", "format": "c",
        "status": "PROVEN_SOURCE",
        "summary": "PIC18 independent bootloader (unaddressed 8-bit protocol, 0x55 prefix).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "3", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),

    # ==================================================================
    # P2-B2-A1 knowledge persistence (2026-10-26): amp-1 Phase-1 + reset semantics
    # ==================================================================
    ("Observation", {
        "id": "obs:p2-b2-a1-icd3-reset-resets-all", "label": "ICD3 PIC32 MCLR reset = full-system reset",
        "what": "ipecmd -P32MZ2048EFH144 -TPICD3 -I (hold) then -OK -OL (release) resets the PIC32, and the PIC32 reboot re-initialises the RS-422 slaves (MBUS init toggles the RD1 'Slaves_Disable' line via Slaves_Disable_On/Off), so the ICD3 PIC32 reset is a FULL-SYSTEM reset (PIC32 + all amplifiers). PO-confirmed: the equivalent physical hard reset also resets everything. This is the recovery path when amplifiers 3/4/6 are stuck in default-wakeup after a command-131 global reset.",
        "date": "2026-10-26", "method": "ICD3 -I/-OK/-OL + PO physical confirmation",
        "status": "PROVEN_HARDWARE",
        "summary": "ICD3 PIC32 MCLR reset resets the PIC32 AND all amplifiers (via the PIC32 reboot's MBUS init on the RD1 slave-disable line); full-system reset.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "3.1", "source_revision": "b2ea9735", "evidence_status": "OBSERVED", "observed_date": "2026-10-26",
    }),
    ("Observation", {
        "id": "obs:p2-b2-a1-amp1-phase1-deployed", "label": "Amplifier 1 Phase-1 1.0.0 deployed",
        "what": "Amp 1 flashed via command 131 (FlashOneAmplifier --amp 1, single-target FlashSequencer). Runtime readback: HR7=0xF101, HR11=0xE906, HR21=0x0100, HR22=0x0001, HR0=399 neutral, MbCommError=OK.",
        "date": "2026-10-26", "method": "FlashOneAmplifier CLI + fresh SLAVEINFO readback",
        "status": "PROVEN_HARDWARE",
        "summary": "Amplifier 1 now runs Phase-1 1.0.0 (HR11=0xE906); identity 0xF101; version HR21=0x0100/HR22=0x0001.",
        "source_path": "docs/P2-B2-A1-PIC32-1.0.1-IMPLEMENTATION-PREFLIGHT.md",
        "source_location": "deployment", "source_revision": "b2ea9735", "evidence_status": "OBSERVED", "observed_date": "2026-10-26",
    }),
    ("Observation", {
        "id": "obs:p2-b2-a1-amps-4-6-default-wakeup", "label": "Amps 4/6 default-wakeup after 131 global reset",
        "what": "After command 131's global reset, amplifiers 4 and 6 (and 3) stopped answering their assigned unicast addresses (MbCommError=TIMEOUT); HR11=0x251F remained cached. PO physical observation: amps 4/6 in 'default wakeup state' (error LED), amp 1 RX-blink/RUN-off, amp 3 WARN+RX. Software re-init (cold-start, 107 RESET + generous delays) did NOT recover them; a full PIC32 reset (ICD3 or physical hard reset) is the recovery path.",
        "date": "2026-10-26", "method": "SLAVEINFO readback + PO LED observation",
        "status": "PROVEN_HARDWARE",
        "summary": "Amps 4/6 entered a default-wakeup/error state after the 131 global reset and did not recover via software re-init; full-system PIC32 reset required.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "b2ea9735", "evidence_status": "OBSERVED", "observed_date": "2026-10-26",
    }),
    ("Observation", {
        "id": "obs:p2-b2-a2-107-100-reset-required", "label": "107 RESET + 100 SLAVES_ON required before single-target 131 flash",
        "what": "A legacy amplifier re-addressed to its assigned unicast address (e.g. amp 3 at address 3) drops the FlashSequencer HR11=0 bootloader-entry write (case 6, sent to SLAVE_INITIAL_ADDR 0xAA) -> flash fails at sub-step 7 'verify bootloader entry'. The fix is the documented global reset: send EXEC_MBUS_STATE_RESET (107) -> EXEC_MBUS_STATE_SLAVES_ON (100) before the flash, which power-cycles all amps (RD1 Slaves_Disable line) back to default address 0xAA so the HR11=0 write is received. The current FlashOneAmplifierCommand OMITS this reset (documentation drift); amp-1 migration succeeded only because amp 1 was already at 0xAA.",
        "date": "2026-10-26", "method": "live flash attempt + source verification (FlashOneAmplifierCommand.cs, firmware-flash-recovery-technical-reference.md §2.5)",
        "status": "PROVEN_HARDWARE",
        "summary": "The 107+100 global reset must precede a single-target 131 flash when the target amp is at its assigned address, or the HR11=0 entry write is dropped at sub-step 7.",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "b2ea9735", "evidence_status": "OBSERVED", "observed_date": "2026-10-26",
    }),
    ("Observation", {
        "id": "obs:p2-b2-a2-all-four-phase1", "label": "All four amplifiers migrated to Phase-1 1.0.0",
        "what": "Amps 3, 4, 6 migrated individually via 107/100 reset + command 131 (single-target FlashSequencer). Full init cycle (250->100->106->101->104->105) verified all four: HR7=0xF101, HR11=0xE906, HR21=0x0100, HR22=0x0001, MbCommError=OK.",
        "date": "2026-10-26", "method": "FlashOneAmplifier CLI + full-init SLAVEINFO readback",
        "status": "PROVEN_HARDWARE",
        "summary": "Prototype fully migrated: PIC32 1.0.1 + all four amplifiers Phase-1 1.0.0 (0xE906).",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "b2ea9735", "evidence_status": "OBSERVED", "observed_date": "2026-10-26",
    }),

    # ==================================================================
    # P2-B2 baseline audit (LED behavior + performance)
    # ==================================================================
    ("Observation", {
        "id": "obs:p2-b2-led-run-semantics-changed", "label": "LED_RUN semantics changed ENABLE -> RUN (motion)",
        "what": "Legacy (928ea7c) main.c CONFIG case 5 set LED_RUN_LAT=1 when ENABLE (HR_BEMF_CTRL & HR_BEMF_ENABLE_AMPLIFIER_BIT, bit 15 of HR1). Phase-1 (commit 2f27d863 'Implement PIC18 Control Core-lite') redefined LED_RUN: main.c:55 sets LED_RUN_LAT = (amp_model.status.state == AMP_ST_RUN), and main.c:165 sets LED_RUN_LAT=0 at addressing-complete. AMP_ST_RUN (amp_core.c:86-87) requires command.enable && !inhibited && lease_fresh && pwm != AMP_NEUTRAL. So LED_RUN no longer illuminates after init+communication (amp is ENABLED but at neutral setpoint, state=AMP_ST_READY). No PO approval of this observable-behavior change was found.",
        "date": "2026-10-26", "method": "git blame + source diff (928ea7c vs current main.c/amp_core.c)",
        "status": "PROVEN_SOURCE",
        "summary": "LED_RUN changed from 'amplifier ENABLED' (legacy) to 'amplifier RUNNING with motion commanded' (Phase-1). LED pins unchanged (RB5 RUN/RB4 WAR/RB3 ERR/RB2 RX/RB1 TX, active-high).",
        "source_path": "TrackAmplifier4.X/main.c",
        "source_location": "main.c:55,165 vs 928ea7c case 5", "source_revision": "b2ea9735", "evidence_status": "SOURCE_REF", "observed_date": "2026-10-26",
    }),
    ("Observation", {
        "id": "obs:p2-b2-led-run-corrected", "label": "LED_RUN corrected to effective H-bridge drive (1.0.1)",
        "what": "PO approved LED_RUN = 'H-bridge actually enabled and actively driven' (ON at neutral and non-neutral when enabled; OFF on power-on/unaddressed/not-enabled/disabled/FAULT/ESTOP/comm-loss/lease-expiry). Implemented as AmpCore_IsBridgeDriving() = status.config_ok && command.enable && !status.inhibited (amp_core.c), wired at main.c:55 (was state==AMP_ST_RUN). LED_WAR/ERR/RX/TX/OCC untouched. Architect DESIGN_ACCEPTABLE.",
        "date": "2026-10-26", "method": "source change + test_amp.py LED truth table + Architect review",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "LED_RUN now indicates enabled-and-not-inhibited (effective bridge drive), matching the legacy 'ENABLED' diagnostic meaning under the Phase-1 safety architecture.",
        "source_path": "TrackAmplifier4.X/amp_core.c",
        "source_location": "AmpCore_IsBridgeDriving", "source_revision": "b2ea9735", "evidence_status": "TEST_PASS", "observed_date": "2026-10-26",
    }),
    ("Observation", {
        "id": "obs:p2-b2-bridge-deenergize-fix", "label": "Bridge de-energizes at neutral after comm/lease/stop",
        "what": "Pre-existing: on comm-loss/lease-expiry/STOP_REQUEST the bridge forced neutral but left PWM3EN ON (switching ~50% duty, brake released). PO chose de-energize (floating/coast). Fix: AmpCore_Update AmpHw_Enable expression now = config_ok && ((enable && !inhibited) || stopping_active) && !hard, so the bridge keeps switching during the fast-stop ramp then drops PWM3EN at neutral. DISABLE/normal-run/hard-fault behavior unchanged (hard still cuts immediately via !hard). Architect DESIGN_ACCEPTABLE (7 claims). V4 physical verification of PWM3EN=0 pending.",
        "date": "2026-10-26", "method": "source change + test_amp.py peripheral_enable asserts + Architect review",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Safety-stop (comm/lease/stop) now de-energizes the bridge once neutral is reached, so LED_RUN OFF corresponds to a de-energized (floating/coasting) output.",
        "source_path": "TrackAmplifier4.X/amp_core.c",
        "source_location": "AmpCore_Update AmpHw_Enable", "source_revision": "b2ea9735", "evidence_status": "TEST_PASS", "observed_date": "2026-10-26",
    }),
    ("Observation", {
        "id": "obs:p2-b2-led-war-semantics-shifted", "label": "LED_WAR blink exit shifted ENABLE -> id_set",
        "what": "Led_Blink mechanism (TMR6IF ~98ms toggle) is byte-identical legacy vs Phase-1, but the CONFIG case-5 exit condition moved: legacy waited for ENABLE (bit15 HR1) then set LED_WAR=0 + LED_RUN=1; Phase-1 waits for id_set (address assigned) then LED_WAR=0 + LED_RUN=0. Net: LED_WAR stops blinking at address-assigned (before H-bridge enabled), leaving no indication for 'addressed but not yet enabled'. The solid LED seen is LED_ERR (case 0 slot-select wait), identical in both. Recommendation (not yet PO-approved): restore WAR to blink while the bridge is not effectively enabled (=!LED_RUN).",
        "date": "2026-10-26", "method": "source diff 928ea7c vs current main.c CONFIG case 5",
        "status": "PROVEN_SOURCE",
        "summary": "LED_WAR blink semantics regressed from 'waiting for ENABLE' to 'waiting for address' in commit 2f27d863; restoration is a separate PO decision.",
        "source_path": "TrackAmplifier4.X/main.c",
        "source_location": "CONFIG case 5", "source_revision": "b2ea9735", "evidence_status": "SOURCE_REF", "observed_date": "2026-10-26",
    }),
    ("Observation", {
        "id": "obs:p2-b2-pic18-performance-baseline", "label": "PIC18 Phase-1 performance baseline (calculated)",
        "what": "Fosc 64 MHz HFINTOSC (no PLL), Fcy 16 MHz, 62.5 ns/cycle, 16M cycles/s. 2 Mbaud UART (9-bit RX/8-bit TX; 88 cyc/RX byte, 80 cyc/TX byte). PWM 20 kHz (TMR2), control tick 1.704 ms (TMR4, 586.85 Hz, 27264 cycles). HW watchdog OFF; 500 ms software lease. Continuous spin (no sleep). Phase-1 baseline ~500 cyc/tick (~1.8-3% CPU) vs legacy ~65 cyc/tick (~0.24%); Modbus ~104 us/request; unaddressed ~1 RX ISR/frame. Phase-2 capacity: conditionally acceptable (>=94% tick free). Memory MEASURED (XC8 2.31 build, memoryfile.xml): Phase-1 1.0.1 FLASH 14446 B (7223 words, 47.0%) / RAM 526 B (25.7%); legacy 6358 B FLASH (20.7%) / 295 B RAM (14.4%).",
        "date": "2026-10-26", "method": "source analysis (device_config.c, eusart.c, tmr2/4.c, amp_core.c) + datasheet + XC8 2.31 build memoryfile.xml",
        "status": "PROVEN_SOURCE",
        "summary": "PIC18 Phase-1 runs at 16 MIPS with >=94% of each 1.704 ms control tick free; Modbus adds ~104 us/request; ISR/peripheral layer byte-identical to legacy; memory measured (FLASH 7223 words / RAM 526 B for 1.0.1).",
        "source_path": "TrackAmplifier4.X/mcc_generated_files/device_config.c",
        "source_location": "config words + tmr4.c", "source_revision": "b2ea9735", "evidence_status": "SOURCE_REF", "observed_date": "2026-10-26",
    }),
    ("SafetyInvariant", {
        "id": "safety:led-diagnostic-contract", "label": "Human-observable LED behavior is a controlled product interface",
        "statement": "Human-observable hardware diagnostic behavior (LED meanings, startup sequences, blink patterns, fault/ready/run/bootloader indications) is a controlled product interface. Changing it requires a legacy-behavior comparison, description of the new behavior, technical reason, safety analysis, effect on operator diagnosis, and explicit PO approval before deployment.",
        "applies_to": "PIC18 amplifier LEDs (LED_RUN RB5, LED_WAR RB4, LED_ERR RB3, LED_RX RB2, LED_TX RB1, LED_OCC RA5)",
        "requires_authorization": True,
        "status": "PROVEN_SOURCE",
        "summary": "LED semantics must not change silently during firmware refactoring; observable-behavior changes require PO approval. Established P2-B2 (LED_RUN ENABLE->RUN change was unapproved).",
        "source_path": "TrackAmplifier4.X/main.c",
        "source_location": "main.c:55,165 (Phase-1) vs 928ea7c case 5", "source_revision": "b2ea9735", "evidence_status": "SOURCE_REF",
    }),
    ("Observation", {
        "id": "obs:p2-b2-perf-measured", "label": "PIC18 Phase-1 performance PHYSICALLY MEASURED (PicoScope)",
        "what": "Real PicoScope 3406D (JY908/0278) measurements on amp 1 with PT1/PT2 instrumentation builds: M1 control service = Amplifier_ServiceTick 72.5 us / AmpCore_Update 43.4 us per 1695 us tick (4.28% CPU, jitter 11.8 us). M2 Modbus = ProcessPetitModbus 5.19 us idle-poll / 108 us worst-case full request. M3 main loop = 7.66 us/iteration (~128 kHz spin, no sleep), p99 21.3 us, max 182 us (service tick + full Modbus frame). M4 unaddressed 71.8 us vs addressed 72.5 us per tick (~same). M5 combined 182 us max. M6 safety = stop/fault -> AmpHw_Enable(false) within one AmpCore_Update (~43 us), worst-case one tick. Memory (1.0.1): FLASH 7223 words (47.0%), RAM 526 B (25.7%).",
        "date": "2026-10-27", "method": "PT1/PT2 instrumentation builds (0xEA54/0xEABE) + PicoScope block capture + analysis",
        "status": "PROVEN_HARDWARE",
        "summary": "Phase-1 runs the service tick in 72.5 us (4.28% of the 1695 us tick); main loop spins ~128 kHz dominated by Modbus idle-poll; ample tick headroom (95.7%) for Phase-2.",
        "source_path": "TrackAmplifier4.X/main.c",
        "source_location": "Amplifier_ServiceTick / while(1) loop", "source_revision": "b2ea9735", "evidence_status": "OBSERVED", "observed_date": "2026-10-27",
    }),
    ("Observation", {
        "id": "obs:p2-b2-05263-deployed", "label": "All four amplifiers deployed to 1.0.1 (0x5263)",
        "what": "Full init cycle (250->100->106->101->104->105) then SLAVEINFO readback (2026-10-27): amps 1/3/4/6 all HR7=0xF101, HR11=0x5263, HR21=0x0100, HR22=0x0101. Deployed via single-target 131 (FlashOneAmplifier) with built-in 107/100 reset; a stale-PIC32 sub-step-7 failure was cleared by a PIC32 ICD3 hard reset. PIC32 unchanged (1.0.1); PIC18 bootloader unchanged.",
        "date": "2026-10-27", "method": "FlashOneAmplifier CLI (single-target 131) + SLAVEINFO readback",
        "status": "PROVEN_HARDWARE",
        "summary": "All four prototype amplifiers run 1.0.1 (0x5263): LED_RUN correction + bridge de-energize deployed and identity-verified.",
        "source_path": "TrackAmplifier4.X/amp_core.c",
        "source_location": "AmpCore_IsBridgeDriving", "source_revision": "b2ea9735", "evidence_status": "OBSERVED", "observed_date": "2026-10-27",
    }),
    ("Observation", {
        "id": "obs:p2-b2-ffc2-deployed", "label": "All four amplifiers deployed to 1.0.2 (0xFFC2) LED actual-drive",
        "what": "LED_RUN corrected to actual applied H-bridge drive (PWM3EN && !brake_active) and LED_WAR restored (blinks while !LED_RUN), version 1.0.2. Deployed 2026-10-27 to all four prototype amps (1/3/4/6) via single-target 131; SLAVEINFO readback confirms HR7=0xF101, HR11=0xFFC2, HR21=0x0100, HR22=0x0201 (patch=2 in high byte). Observation-only change (no motor/safety/timing change); Architect DESIGN_ACCEPTABLE, test_amp.py PASS.",
        "date": "2026-10-27", "method": "FlashOneAmplifier CLI (single-target 131) + SLAVEINFO readback",
        "status": "PROVEN_HARDWARE",
        "summary": "All four amplifiers run 1.0.2 (0xFFC2): LED_RUN = actual applied drive, LED_WAR = !LED_RUN.",
        "source_path": "TrackAmplifier4.X/main.c",
        "source_location": "Amplifier_ServiceTick LED_RUN_LAT", "source_revision": "fb67387a", "evidence_status": "OBSERVED", "observed_date": "2026-10-27",
    }),
    # ==================================================================
    # P2-C: reusable C# harness + init/telemetry/safety command contract
    # ==================================================================
    ("ProtocolCommand", {
        "id": "command:107", "command": 107, "command_name": "EXEC_MBUS_STATE_RESET",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_HARDWARE",
        "summary": "Asserts Slaves_Disable_On() (RD1 HIGH): ALL amplifiers held in reset/power-cycle (the ONLY firmware reset). Ack MBUS/MBUS_STATE_RESET/DONE after ~1s (mbus.c RESET_WAIT). On the subsequent 100 power-up every PIC18 boots via AmpCore_Init to config_ok=0.",
        "source_path": "TrackController5/firmware/src/mbus.c",
        "source_location": "MBUS_STATE_RESET / MBUS_STATE_RESET_WAIT (377-396)", "source_revision": "c61c7617", "evidence_status": "OBSERVED",
    }),
    ("ProtocolCommand", {
        "id": "command:100", "command": 100, "command_name": "EXEC_MBUS_STATE_SLAVES_ON",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_HARDWARE",
        "summary": "De-asserts Slaves_Disable_Off() (RD1 LOW): amplifiers power up and boot to 0xAA (AmpCore_Init -> config_ok=0, enable=0, inhibited=0). Ack MBUS/MBUS_STATE_SLAVES_ON/DONE after ~0.79s boot-wait.",
        "source_path": "TrackController5/firmware/src/mbus.c",
        "source_location": "MBUS_STATE_SLAVES_ON / SLAVES_BOOT_WAIT (289-303)", "source_revision": "c61c7617", "evidence_status": "OBSERVED",
    }),
    ("ProtocolCommand", {
        "id": "command:108", "command": 108, "command_name": "EXEC_MBUS_SLAVE_DATA_EXCH",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Per-amplifier register write/read (payload [slave, 0xAA/0x55, count, startReg, lo, hi]). GATED by ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX: rejected unless ISxAMPLIFIERxCOMPATIBLE(slave) AND !ISxGLOBALxSTOPxLATCHED(). The only HR0/PWM write vector. Command-108 DONE means mailbox-enqueue acceptance, NOT execution/completion: ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX enqueues the FC16 into the slave mailbox and returns; the actual TX/ACK is asynchronous (FC16_SENT rv=1 is dispatch, not a completed transaction).",
        "source_path": "TrackController5/firmware/src/slavehandler.c",
        "source_location": "ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX (501-552)", "source_revision": "c61c7617", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:110", "command": 110, "command_name": "HOST_HEARTBEAT",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_SOURCE",
        "summary": "Host authority heartbeat. Sets LastHostHeartbeat and arms the latch if DISARMED; does NOT clear a LATCHED latch (only REARM 112 does). 500ms reference / 2s host-authority watchdog.",
        "source_path": "TrackController5/firmware/src/controller.c",
        "source_location": "CONTROLLERxHANDLExSAFETYxCOMMAND HOST_HEARTBEAT (164-178)", "source_revision": "c61c7617", "evidence_status": "SOURCE_REF",
    }),
    ("ProtocolCommand", {
        "id": "command:112", "command": 112, "command_name": "REARM_GLOBAL_STOP",
        "direction": "HOST_TO_PIC32", "destructive": False,
        "status": "PROVEN_HARDWARE",
        "summary": "Transitions the PIC32 global-stop latch LATCHED->ARMED and re-arms the host watchdog. AMPLIFIER-NEUTRAL: issues NO MBUS slave write, so it never energizes a bridge. Required before 107 RESET because a latched stop forces the MBUS handler into SERVICE_TASKS (mbus.c:202-210), swallowing the 107 DONE ack. Idempotent (no-op when not latched).",
        "source_path": "TrackController5/firmware/src/controller.c",
        "source_location": "REARM_GLOBAL_STOP (182-189)", "source_revision": "c61c7617", "evidence_status": "OBSERVED",
    }),
    ("SafetyInvariant", {
        "id": "safety:rearm-amp-neutral", "label": "REARM (112) is amplifier-neutral and non-energizing",
        "statement": "REARM_GLOBAL_STOP (112) only transitions the PIC32 latch LATCHED->ARMED and refreshes LastHostHeartbeat; it performs no MBUS slave write and never changes config_ok/enable/PWM, so it cannot energize an H-bridge. Bridge-enable is AmpHw_Enable(config_ok && ((enable && !inhibited) || stopping_active) && !hard), and config_ok is only set by an explicit CONFIG/APPLY after a verified DISABLE.",
        "applies_to": "C# apply-config harness re-arm step; PIC32 global-stop latch",
        "requires_authorization": False,
        "status": "PROVEN_SOURCE",
        "summary": "The harness re-arms (112) safely before init; the 107 RESET (RD1 power-cycle) is what forces config_ok=0 on every run, so the sequence is safe even after a previous successful config, retained ENABLE, or partial success.",
        "source_path": "TrackController5/firmware/src/controller.c",
        "source_location": "controller.c:182-189 + amp_core.c:111-113", "source_revision": "c61c7617", "evidence_status": "SOURCE_REF",
    }),
    ("SafetyInvariant", {
        "id": "safety:rearm-interval-no-hr0", "label": "REARM-to-RESET interval is safe: no amplifier HR0 write can occur",
        "statement": "In the 112 REARM -> 107 RESET interval a previously-configured amplifier (config_ok=1) with retained ENABLE and an expired lease (inhibited=1) cannot re-energize. The only path that clears 'inhibited' and re-applies a non-neutral PWM is the auto-un-inhibit block in the HR_COMMAND (HR0) write handler (amp_adapter.c:104-111), which requires requested_pwm != AMP_NEUTRAL AND executes only when an HR0 write arrives. 112 issues no MBUS write (controller.c:182-189); the resumed lease broadcast writes HR1 only (slavehandler.c:274-284); the init sequence (104=HR2 address, 105=HR1 ENABLE) and the cyclic poll (FC03 reads) contain no amplifier HR0 write. So the auto-un-inhibit never runs and PWM3EN stays 0. 107 RESET then asserts ~MCLR via the RS-485 RESET_A/RESET_B differential pair (ST485EB transceiver) — a HARDWARE RESET, not a power-supply cycle — clearing config_ok/enable. Independently verified by the Architect (DESIGN_ACCEPTABLE).",
        "applies_to": "PIC18 amp_adapter.c HR_COMMAND auto-un-inhibit; PIC32 112/107 ordering",
        "requires_authorization": False,
        "status": "PROVEN_SOURCE",
        "summary": "No amplifier HR0 write can be emitted during the 112->107 interval, so the auto-un-inhibit (the only re-energize-after-inhibit path) never runs; 107 is a hardware ~MCLR reset.",
        "source_path": "TrackAmplifier4.X/amp_adapter.c",
        "source_location": "amp_adapter.c:96-145 + controller.c:182-189 + slavehandler.c:274-284", "source_revision": "162b1368", "evidence_status": "SOURCE_REF",
    }),
    ("SafetyInvariant", {
        "id": "safety:config-volatile", "label": "PIC18 configuration is volatile (RAM) and cleared by reset",
        "statement": "Amplifier configuration (config_ok, active ramp/watchdog/tag, init_done) is held in RAM only. AmpCore_Init memset()s it on boot, and 107 RESET (RD1) power-cycles the amplifiers to config_ok=0 with AMP_FAULT_CONFIG set. A momentary CONFIG_OK is NOT persistent readiness; configuration must be re-applied each session.",
        "applies_to": "PIC18 amplifier config_ok / init_done lifecycle",
        "requires_authorization": False,
        "status": "PROVEN_SOURCE",
        "summary": "config_ok is RAM-only and cleared by any power-cycle/reset; the final ConfigInvalid fault after session end is the expected post-reset state, not a regression.",
        "source_path": "TrackAmplifier4.X/amp_core.c",
        "source_location": "AmpCore_Init (12-27)", "source_revision": "c61c7617", "evidence_status": "SOURCE_REF",
    }),
    ("SafetyInvariant", {
        "id": "safety:lease-gated-on-latch", "label": "Motion-lease renewal stops while the global stop is latched",
        "statement": "The PIC32 broadcasts the motion lease HR1=0x4000 (address 0) every 100ms ONLY while !ISxGLOBALxSTOPxLATCHED() (slavehandler.c:274-284). On host-loss latch the renewal stops and each amplifier self-inhibits + sets AMP_FAULT_LEASE when its 500ms lease expires (amp_core.c:65-68).",
        "applies_to": "PIC32 lease broadcast; PIC18 lease_fresh / AMP_FAULT_LEASE",
        "requires_authorization": False,
        "status": "PROVEN_SOURCE",
        "summary": "Lease expiry (LeaseExpired 0x0080) after session end is the source-defined fail-closed consequence of stopping the heartbeat; it is not a defect.",
        "source_path": "TrackController5/firmware/src/slavehandler.c",
        "source_location": "SendNextMessage lease broadcast (274-284)", "source_revision": "c61c7617", "evidence_status": "SOURCE_REF",
    }),
    ("Observation", {
        "id": "obs:p2c-harness-validation", "label": "P2-C reusable C# harness non-loaded validation",
        "what": "C# apply-config harness (fix/p2c-csharp-compatibility @ c61c7617) against real PIC32 1.0.1 + PIC18 1.0.2: init 250->112->107->100->106->101->104->105 all ACKED, global stop ARMED. Amps 1/3/4 reached CONFIG_OK (identity 0xF101 -> DISABLE -> CONFIG/APPLY -> INIT_DONE|CONFIG_OK, PWM3EN=0). Amp 6 FAILED reproducibly on the WRITE path (config timeout when 4th; DISABLE verification enable_active=1 when alone) while its READ path (cyclic poll) is healthy (identical msg growth to 1/3/4). All H-bridges remained OFF (PWM3EN=0, appliedPWM=399 neutral) throughout.",
        "date": "2026-10-27", "method": "C# Validation.Host apply-config mode (Debug) + read-only SLAVEINFO readback",
        "status": "PROVEN_HARDWARE",
        "summary": "3 of 4 amplifiers configure end-to-end via the reusable harness; amp 6 has a reproducible WRITE-path failure (hypothesis: marginal RS-422, unconfirmed) that needs physical-layer measurement.",
        "source_path": "SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Control/AmplifierConfigApply.cs",
        "source_location": "AmplifierConfigApplyOrchestrator.RunAsync", "source_revision": "c61c7617", "evidence_status": "OBSERVED", "observed_date": "2026-10-27",
    }),
    ("Hypothesis", {
        "id": "hyp:amp6-marginal-write", "label": "Amplifier 6 failure is a PIC32 Modbus answer-timeout (marginal) causing 104 identity-verification failure",
        "hypothesis": "Amplifier 6's intermittent config/DISABLE failure is caused by the PIC32 Modbus answer timeout (~250us, mbus.c divider 4002) being marginal versus the PIC18 response turnaround (RX ~45us + Amplifier_ServiceTick ~72.5us + ProcessPetitModbus up to ~108us + TX ~45us => up to ~270us worst case). A slow response occasionally exceeds 250us -> ModbusReceiveTimeoutCallBack -> SlaveAnswerTimeoutCounter -> SLAVE_DATA_TIMEOUT -> CHECKxMODBUSxCOMMxSTATUS returns SLAVENOK -> AmplifierCompatible=false -> identityRaw=0x0000, compatible=false, register_write_108=0. The RS-485 bus electrical signals are IDENTICAL between amplifier 6 and amplifier 3 (PicoScope, all x1), so this is NOT an electrical/wiring difference (an earlier 'amp6 RX_B inverted' observation was a probe-attenuation artifact).",
        "evidence_for": "CONFIRMED: the PIC32 Modbus answer-timeout divider was reduced 4002->2000 (~250us->~500us, commit c493a33d) and, after flashing, the command-104 identity verification for amplifier 6 passed 20/20 (previously identityRaw=0x0000/compatible=false every run). Source chain verified by Integrator (mbus.c -> PetitModbus.c -> slavecommhandler.c -> slavestartup.c).",
        "evidence_against": "",
        "status": "PROVEN_HARDWARE",
        "superseded": True,
        "summary": "CONFIRMED and physically validated: amp6's failure was the marginal PIC32 Modbus answer timeout (~250us vs ~270us worst-case PIC18 turnaround). The 4002->2000 (~500us) fix is DEPLOYED and the command-104 identity verification passed 20/20 after flash.",
        "source_path": "TrackController5/firmware/src/mbus.c",
        "source_location": "MBUS_Initialize ModbusReceiveTimeoutHandle divider (4002->2000)", "source_revision": "c493a33d", "evidence_status": "OBSERVED", "observed_date": "2026-10-28",
    }),
    ("Decision", {
        "id": "decision:p2c-104-timeout-fix", "label": "P2-C: increase PIC32 Modbus answer timeout 250us -> 500us (DEPLOYED + validated)",
        "decision": "Increase the PIC32 Modbus answer-timeout divider from 4002 (~250us) to 2000 (~500us) in TrackController5/firmware/src/mbus.c (commit c493a33d) to resolve the intermittent command-104 identity-verification failure (marginal timeout vs ~270us worst-case PIC18 turnaround). Minimal one-line change; no safety/energization impact; 500us < 1.2ms comm cycle.",
        "decided_by": "Project Lead (prepared); DEPLOYED and validated under a separate Product Owner flashing GO",
        "date": "2026-10-28",
        "status": "PROVEN_HARDWARE",
        "summary": "DEPLOYED and validated: the 4002->2000 (~500us) timeout fix is flashed (PIC32 8FF89B55.../A77FA25F... instrumentation lineage carries it; ping OK, controller-version 113 = 1.0.1) and the command-104 identity verification passed 20/20 after flash.",
        "source_path": "TrackController5/firmware/src/mbus.c",
        "source_location": "MBUS_Initialize ModbusReceiveTimeoutHandle divider (4002->2000)", "source_revision": "c493a33d", "evidence_status": "OBSERVED", "observed_date": "2026-10-28",
    }),
    ("Observation", {
        "id": "obs:p2c-rs422-measurement", "label": "P2-C RS-422 bus electrical measurement (PicoScope 3406D, probes x1)",
        "what": "Real PicoScope 3406D (JY908/0278) differential measurement of the PIC32->PIC18 RS-485 bus at 62.5 MS/s, all probes x1. IDLE: RX_A (J1 pin12) = 4.35V HIGH, RX_B (J1 pin4) = 0.80V LOW on BOTH amp6 and amp3 (IDENTICAL); differential idle A-B = +3.55V (A>B). TRAFFIC (trigger on amp3 RX_A 2V): RX_A swings to ~0.65V, RX_B swings to ~4.45V -> differential ~+/-3.5V (full 5V-transceiver swing) on BOTH amp6 and amp3. The two amplifier RX signals are electrically IDENTICAL. Earlier 'amp6 RX_B different/inverted' was a probe-attenuation artifact (probes not all at x1), now corrected. Harness correlation in every run (apply-config --slaves 6): amp6 identityRaw=0x0000, compatible=false, register_write_108=0 (no 108 write ever sent).",
        "date": "2026-10-27", "method": "PicoScope 3406D analog A/B/C/D (J1 pin 12/4, GNDD pin 2/8, single common ground, all probes x1) + C# apply-config harness correlation",
        "status": "PROVEN_HARDWARE",
        "summary": "amp6 and amp3 RS-485 RX signals are electrically IDENTICAL (idle A=4.35V/B=0.80V, differential +/-3.5V swing); the amp6 failure is NOT an electrical bus difference.",
        "source_path": "tools/picoscope/evidence/mcp/8d169a17fd9b09ba0834d200b009919a.npz",
        "source_location": "analog channels A/B/C/D", "source_revision": "5bb0855d", "evidence_status": "OBSERVED", "observed_date": "2026-10-27",
    }),
    # ==================================================================
    # P2-C fault investigation (Fault A + Fault B) — 2026-10-28
    # ==================================================================
    ("Observation", {
        "id": "obs:p2c-faulta-104-fix-deployed", "label": "P2-C Fault A resolved: command-104 identity verification 20/20 after timeout fix",
        "what": "The PIC32 Modbus answer-timeout divider was reduced 4002->2000 (~250us->~500us, commit c493a33d) and flashed. The command-104 identity verification for amplifier 6 then passed 20/20 (previously identityRaw=0x0000/compatible=false every run). The deployed PIC32 instrumentation lineage 8FF89B55.../A77FA25F... carries the fix; ping OK and controller-version 113 = 1.0.1 (the 4002->2000 change does not bump the reported version).",
        "date": "2026-10-28", "method": "command-104 identity-verification loop (20/20) against the deployed PIC32 + command-113 version readback",
        "status": "PROVEN_HARDWARE",
        "summary": "Fault A is resolved and physically validated: the marginal Modbus answer timeout (4002->2000, ~500us) is deployed and the amp-6 command-104 identity verification passed 20/20 after flash.",
        "source_path": "TrackController5/firmware/src/mbus.c",
        "source_location": "MBUS_Initialize ModbusReceiveTimeoutHandle divider (4002->2000)", "source_revision": "c493a33d", "evidence_status": "OBSERVED", "observed_date": "2026-10-28",
    }),
    ("Observation", {
        "id": "obs:p2c-faultb-disable-enqueue-mechanism", "label": "P2-C Fault B mechanism: DISABLE ENQUEUE->FC16_SENT->TX_DONE->ACK (no purge/gate-drop, no later ENABLE)",
        "what": "Event log from the deployed PIC32 TX/ACK instrumentation (A77FA25F...): the DISABLE write (HR1=0x0400) is ENQUEUED -> FC16_SENT (rv=1) -> TX_DONE -> ACK identically for every amplifier, with NO mailbox PURGE and NO compatibility GATE-DROP, and there is NO later ENABLE (0x8000) write. So the PIC32 transmits and ACKs the DISABLE exactly like any other amplifier write; the Fault-B symptom originates in the PIC18, not the PIC32 mailbox/compatibility gate.",
        "date": "2026-10-28", "method": "TCP console event log from the deployed PIC32 TX/ACK instrumentation (A77FA25F...)",
        "status": "PROVEN_HARDWARE",
        "summary": "The DISABLE write is enqueued and TX/ACKed identically for every amplifier (FC16_SENT rv=1 -> TX_DONE -> ACK), with no purge/gate-drop and no later ENABLE; Fault B is a PIC18-side staleness, not a PIC32 mailbox/compatibility-gate problem.",
        "source_path": "TrackController5/firmware/src/slavehandler.c",
        "source_location": "ADDxNEWxSLAVExDATAxCMDxTOxSLAVExMAILBOX (ENQUEUE) + ENQUEUExGLOBALxSTOP (PURGE); slavecommhandler.c (dispatch gate + FC16_SENT); modbus/PetitModbus.c (TX_DONE/ACK/TIMEOUT)", "source_revision": "01fcd152", "evidence_ref": "TCP console event log (deployed PIC32 A77FA25F...)", "evidence_status": "OBSERVED", "observed_date": "2026-10-28",
    }),
    ("Observation", {
        "id": "obs:p2c-faultb-stale-hr8-root-cause", "label": "P2-C Fault B root cause: HR8 enable-active mirror lags the DISABLE write by one ~1.704ms tick",
        "what": "The DISABLE write sets command.enable=false synchronously in the PIC18 amp_adapter.c HR_CONTROL DISABLE branch, but status.enable_active (mirrored as HR8 bit 0x0100) is only recomputed in AmpCore_Update() (amp_core.c:100) gated on the ~1.704ms TMR4 tick, and the HR8 mirror is only rebuilt by AmpAdapter_Refresh() at that same tick. The C# apply-config harness sends one FC03 read ~1ms after DISABLE and reads HR8 on the first fresh SLAVEINFO, so it can read the stale pre-DISABLE enable-active=1 (HR8=0xD100). Intermittent because the write is not phase-locked to the tick. This REFUTES the earlier 'later ENABLE re-enables' hypothesis (see hyp:p2c-later-enable-reenables).",
        "date": "2026-10-28", "method": "source inspection (TrackAmplifier4.X amp_adapter.c, amp_core.c, main.c Amplifier_ServiceTick, mcc_generated_files/interrupt_manager.c:107 TMR4 tick, amp_model.h AMP_LEASE_TICKS 294) + harness timing correlation",
        "status": "PROVEN_SOURCE",
        "summary": "HR8's enable-active bit lags the DISABLE write by up to one ~1.704ms TMR4 tick (recomputed only in AmpCore_Update/AmpAdapter_Refresh), so a ~1ms-later FC03 read can observe the stale pre-DISABLE enable-active=1 (HR8=0xD100); intermittent because the write is not tick-phase-locked.",
        "source_path": "TrackAmplifier4.X/amp_adapter.c",
        "source_location": "HR_CONTROL DISABLE branch (116-123) + AmpAdapter_Refresh HR8 0x0100 (165-171); amp_core.c:100; main.c Amplifier_ServiceTick; interrupt_manager.c:107 TMR4 tick; amp_model.h AMP_LEASE_TICKS 294", "source_revision": "53cd34ad", "evidence_status": "SOURCE_REF", "observed_date": "2026-10-28",
    }),
    ("Hypothesis", {
        "id": "hyp:p2c-later-enable-reenables", "label": "P2-C Fault B (REFUTED): a later ENABLE write re-enables the amplifier after DISABLE",
        "hypothesis": "The stale enable-active=1 read on HR8 (HR8=0xD100) after the DISABLE write was caused by a later ENABLE (0x8000) write re-enabling the amplifier, so the DISABLE verify observed a genuinely re-enabled amplifier.",
        "evidence_for": "The C# apply-config DISABLE verify read HR8=0xD100 (enable-active=1) shortly after the DISABLE write, which superficially suggested a re-enable.",
        "evidence_against": "REFUTED: the event log from the deployed PIC32 TX/ACK instrumentation shows NO later ENABLE (0x8000) write after the DISABLE (HR1=0x0400). The enable-active=1 is the stale HR8 mirror: command.enable flips synchronously on DISABLE (amp_adapter.c HR_CONTROL DISABLE branch) but status.enable_active (HR8 bit 0x0100) is only recomputed in AmpCore_Update() at the ~1.704ms TMR4 tick and rebuilt by AmpAdapter_Refresh() at that same tick, so a ~1ms-later FC03 read observes the pre-DISABLE value.",
        "status": "CONTRADICTED",
        "summary": "CONTRADICTED: Fault B is NOT a later-ENABLE re-enable. The event log shows no later ENABLE write; the enable-active=1 is the stale HR8 mirror (recomputed only at the ~1.704ms tick, after AmpAdapter_OnWrite returns).",
        "source_path": "TrackAmplifier4.X/amp_adapter.c",
        "source_location": "HR_CONTROL DISABLE branch + AmpAdapter_Refresh HR8 0x0100", "source_revision": "53cd34ad", "evidence_status": "SOURCE_REF", "observed_date": "2026-10-28",
    }),
    ("Observation", {
        "id": "obs:p2c-faultb-pic18-hr8-fix", "label": "P2-C Fault B correction: sync enable-active + refresh HR8 at end of AmpAdapter_OnWrite",
        "what": "Commit 53cd34ad syncs status.enable_active = command.enable and calls AmpAdapter_Refresh() at the end of AmpAdapter_OnWrite, so the HR8 mirror (bit 0x0100) reflects the write immediately instead of waiting for the next ~1.704ms AmpCore_Update/AmpAdapter_Refresh tick. Regression test added in TrackAmplifier4.X/tests/test_amp.c (fault-B DISABLE reflects enable-active/inhibited synchronously). Status PROVEN_SOURCE (source change) + PROVEN_OFFLINE_TEST (test_amp.py PASS; Integrator PASS). NOT flashed (PIC18 flash requires a separate Product Owner GO).",
        "date": "2026-10-28", "method": "source change (53cd34ad) + test_amp.py regression + Integrator review",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Fault B is corrected in source and offline-tested: AmpAdapter_OnWrite now syncs status.enable_active = command.enable and refreshes the HR8 mirror immediately, removing the one-tick staleness. test_amp.py PASS + Integrator PASS; NOT flashed.",
        "source_path": "TrackAmplifier4.X/amp_adapter.c",
        "source_location": "AmpAdapter_OnWrite (status.enable_active = command.enable + AmpAdapter_Refresh, 153-161)", "source_revision": "53cd34ad", "evidence_ref": "TrackAmplifier4.X/tests/test_amp.c (fault-B regression)", "evidence_status": "TEST_PASS", "observed_date": "2026-10-28",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic18-0x5784", "label": "PIC18 Fault-B candidate 0x5784 (enable-active HR8 sync)", "kind": "application",
        "target": "PIC18", "checksum": "0x5784", "version": "Fault-B-candidate",
        "image_path": "TrackAmplifier4.X/dist/Offset/production/TrackAmplifier4.X.production.hex",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Candidate PIC18 build (C# flash checksum 0x5784; Offset HEX, XC8 2.31 / MPLAB X 6.20) carrying the Fault-B correction: AmpAdapter_OnWrite syncs status.enable_active = command.enable and calls AmpAdapter_Refresh() so the HR8 0x0100 mirror reflects the write immediately. test_amp.py PASS + Integrator PASS. NOT flashed (PIC18 flash requires a separate Product Owner GO).",
        "source_path": "TrackAmplifier4.X/amp_adapter.c",
        "source_location": "AmpAdapter_OnWrite (status.enable_active = command.enable + AmpAdapter_Refresh)", "source_revision": "53cd34ad",
        "source_hash": "5AC7C6D9F6A887C4BAB3441CE82ACF03CA2A95179165E78E35BA37F9124BE2C7",
        "evidence_status": "TEST_PASS", "observed_date": "2026-10-28",
    }),
    ("Observation", {
        "id": "obs:p2c-tx-ack-instrumentation-semantics", "label": "P2-C PIC32 TX/ACK event-ring marker semantics (mbus_event.h)",
        "what": "New PIC32 diagnostic event-ring markers (mbus_event.h): MBUS_EV_ENQUEUE(1)=command-108 mailbox accept; MBUS_EV_GATE_DROP(2)=compatibility-gate reject; MBUS_EV_FC16_SENT(3)=PetitSendMessage TX-state START (DISPATCHED, NOT transmit/ack); MBUS_EV_PURGE(4)=global-stop mailbox purge; MBUS_EV_TX_DONE(5)=bytes physically handed to UART (TX->WAIT_ANSWER); MBUS_EV_ACK(6)=valid FC16 echo (MbCommError OK); MBUS_EV_TIMEOUT(7)=no echo. LIMITATION: FC16_SENT rv=1 is NOT a completed transaction (it is the SEND/dispatch result, not the physical TX or the echo).",
        "date": "2026-10-28", "method": "source inspection (mbus_event.h, modbus/PetitModbus.c, slavecommhandler.c)",
        "status": "PROVEN_SOURCE",
        "summary": "The PIC32 TX/ACK event-ring markers (ENQUEUE/GATE_DROP/FC16_SENT/PURGE/TX_DONE/ACK/TIMEOUT) are documented, with the caveat that FC16_SENT rv=1 is a dispatch indicator, not a completed transaction.",
        "source_path": "TrackController5/firmware/src/mbus_event.h",
        "source_location": "MBUS_EVENT_TYPE enum (19-27); modbus/PetitModbus.c TX_DONE/ACK/TIMEOUT; slavecommhandler.c GATE_DROP/FC16_SENT", "source_revision": "01fcd152", "evidence_status": "SOURCE_REF", "observed_date": "2026-10-28",
    }),
    ("Observation", {
        "id": "obs:p2c-instrumentation-stack-overflow", "label": "P2-C PIC32 instrumentation v1 stack overflow (boot regression) -> event-ring fix",
        "what": "The first PIC32 instrumentation (commit 9e744aee) used LOG_Printf/LOG_Push (2x250B buffers + vsnprintf) in the deep MBUS call chain (SYS_Tasks -> MBUS_Tasks -> PROCESSxNEXTxSLAVE -> SendNextMessage -> SLAVExCOMMUNICATIONxHANDLER) and overflowed the 1 KiB PIC32 stack (.stack 0x400), causing boot failure. Corrected by the bounded 64-entry event-ring (MBUS_EVENT_PUSH, ~O(1) struct copy) with deferred MBUS_EVENT_REPORT (commit daec3603). Root cause PROVEN_SOURCE; boot OK after correction PROVEN_HARDWARE.",
        "date": "2026-10-28", "method": "source inspection (mbus_event.h header comment; slavecommhandler.c MBUS_EV_RING 64) + boot verification after correction",
        "status": "PROVEN_HARDWARE",
        "summary": "The v1 PIC32 instrumentation overflowed the 1 KiB stack (LOG_Printf/vsnprintf in the deep MBUS chain, commit 9e744aee) and boot-failed; the bounded 64-entry event-ring with deferred report (daec3603) fixed it and boot is OK.",
        "source_path": "TrackController5/firmware/src/mbus_event.h",
        "source_location": "header comment (stack overflow) + MBUS_EVENT_TYPE; slavecommhandler.c MBUS_EV_RING 64", "source_revision": "daec3603", "evidence_status": "OBSERVED", "observed_date": "2026-10-28",
    }),
]


# ---------------------------------------------------------------------------
# Relationships: (rel_name, from_id, to_id, properties).
# ---------------------------------------------------------------------------
RELS: list[tuple[str, str, str, dict]] = [
    # RUNS_FIRMWARE
    ("RUNS_FIRMWARE", "device:amp-1", "firmware:pic18-0xE906", {"since": "P2-B2-A1 amp-1 Phase-1 deploy 2026-10-26", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-1", "firmware:pic18-0x5263", {"since": "P2-B2 LED-corrected 1.0.1 deploy 2026-10-27", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-1", "firmware:pic18-0xFFC2", {"since": "P2-B2 LED actual-drive 1.0.2 deploy 2026-10-27", "superseded": False}),
    ("RUNS_FIRMWARE", "device:amp-1", "firmware:pic18-0x251F", {"since": "K8B/P2-B1 fresh readback 2026-10-25", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-3", "firmware:pic18-0xE906", {"since": "P2-B2-A2 amp-3 Phase-1 deploy 2026-10-26", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-3", "firmware:pic18-0x5263", {"since": "P2-B2 LED-corrected 1.0.1 deploy 2026-10-27", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-3", "firmware:pic18-0xFFC2", {"since": "P2-B2 LED actual-drive 1.0.2 deploy 2026-10-27", "superseded": False}),
    ("RUNS_FIRMWARE", "device:amp-3", "firmware:pic18-0x251F", {"since": "pre-PICO-5B", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-4", "firmware:pic18-0xE906", {"since": "P2-B2-A2 amp-4 Phase-1 deploy 2026-10-26", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-4", "firmware:pic18-0x5263", {"since": "P2-B2 LED-corrected 1.0.1 deploy 2026-10-27", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-4", "firmware:pic18-0xFFC2", {"since": "P2-B2 LED actual-drive 1.0.2 deploy 2026-10-27", "superseded": False}),
    ("RUNS_FIRMWARE", "device:amp-4", "firmware:pic18-0x251F", {"since": "pre-PICO-5B", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-6", "firmware:pic18-0xE906", {"since": "P2-B2-A2 amp-6 Phase-1 deploy 2026-10-26", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-6", "firmware:pic18-0x5263", {"since": "P2-B2 LED-corrected 1.0.1 deploy 2026-10-27", "superseded": True}),
    ("RUNS_FIRMWARE", "device:amp-6", "firmware:pic18-0xFFC2", {"since": "P2-B2 LED actual-drive 1.0.2 deploy 2026-10-27", "superseded": False}),
    ("RUNS_FIRMWARE", "device:amp-6", "firmware:pic18-0x251F", {"since": "pre-PICO-5B", "superseded": True}),
    ("RUNS_FIRMWARE", "device:pic32", "firmware:pic32-0.1.0", {"since": "L1", "superseded": True}),
    ("RUNS_FIRMWARE", "device:pic32", "firmware:pic32-1.0.0", {"since": "P2-B1 final deploy 2026-10-25", "superseded": True}),
    ("RUNS_FIRMWARE", "device:pic32", "firmware:pic32-1.0.1", {"since": "P2-B2-A1 deploy 2026-10-26", "superseded": False}),

    # IMPLEMENTED_BY (artifact built from source revision)
    ("IMPLEMENTED_BY", "firmware:pic18-0x251F", "source:928ea7c", {"note": "reproduces 0x251F byte-identically"}),
    ("IMPLEMENTED_BY", "firmware:pic18-0x2D00", "source:bea4251", {"note": "diagnostic build"}),

    # REQUIRES_STATE (command requires a precondition state)
    ("REQUIRES_STATE", "command:125", "state:bootloader-mode", {"note": "ERASE requires the target in 8-bit bootloader mode"}),
    ("REQUIRES_STATE", "command:126", "state:bootloader-mode", {"note": "WRITE requires the target in 8-bit bootloader mode"}),
    ("REQUIRES_STATE", "command:103", "state:fwhandler", {"note": "103 enters the firmware handler"}),

    # EXPECTS_RESPONSE (command -> expected ack)
    ("EXPECTS_RESPONSE", "command:125", "ack:202", {"expected_taskcommand": 202}),
    ("EXPECTS_RESPONSE", "command:126", "ack:212", {"expected_taskcommand": 212}),
    ("EXPECTS_RESPONSE", "command:127", "ack:222", {"expected_taskcommand": 222}),
    ("EXPECTS_RESPONSE", "command:128", "ack:232", {"expected_taskcommand": 232}),
    ("EXPECTS_RESPONSE", "command:129", "ack:242", {"expected_taskcommand": 242}),
    ("EXPECTS_RESPONSE", "command:124", "ack:194", {"expected_taskcommand": 194}),

    # GUARDED_BY (command guarded by a safety invariant)
    ("GUARDED_BY", "command:125", "safety:erase-boundary", {"note": "destructive boundary"}),
    ("GUARDED_BY", "command:125", "safety:physical-isolation", {"note": "unaddressed bootloader bus"}),
    ("GUARDED_BY", "command:125", "safety:authorization-required", {"note": "PO authorization required"}),
    ("GUARDED_BY", "command:126", "safety:physical-isolation", {"note": "unaddressed bootloader bus"}),
    ("GUARDED_BY", "command:126", "safety:no-assume-ok", {"note": "no ASSUME-OK on unconfirmed destructive ops"}),
    ("GUARDED_BY", "command:131", "safety:authorization-required", {"note": "PO authorization required"}),

    # OBSERVED_IN (observation recorded during a test)
    ("OBSERVED_IN", "obs:9to8-transition", "test:pico-5b-upload", {"note": "transition observed after upload + 123"}),
    ("OBSERVED_IN", "obs:erase-timeout", "test:pico-5b-erase-attempt", {"note": "ERASE timeout"}),
    ("OBSERVED_IN", "obs:write-timeout", "test:pico-5b-erase-attempt", {"note": "WRITE timeout"}),
    ("OBSERVED_IN", "obs:write-config-timeout-mismatch", "test:pico-5b-erase-attempt", {"note": "WRITE_CONFIG timeout-code mismatch"}),

    # PROVEN_BY (firmware artifact proven by evidence)
    ("PROVEN_BY", "firmware:pic18-0x251F", "evidence:rollback-0x251F", {"note": "immutable rollback artifact"}),
    ("PROVEN_BY", "firmware:pic18-0x2D00", "evidence:diag-0x2D00", {"note": "diagnostic build archived"}),

    # CONTRADICTED_BY (hypothesis contradicted by evidence)
    ("CONTRADICTED_BY", "hyp:bootloader-replies-never-worked", "evidence:techref-doc", {"note": "ref 2.5 documents fleet 122 + single-target 131/132 succeeding"}),

    # SUPERSEDES (artifact supersedes artifact)
    ("SUPERSEDES", "firmware:pic18-0xE906", "firmware:pic18-0x251F", {"note": "1.0.0 supersedes legacy on amp 1 (round-trip only; fleet not migrated)"}),
    ("SUPERSEDES", "firmware:pic18-0x5263", "firmware:pic18-0xE906", {"note": "1.0.1 (LED-corrected + de-energize) supersedes 1.0.0"}),
    ("SUPERSEDES", "firmware:pic18-0xFFC2", "firmware:pic18-0x5263", {"note": "1.0.2 (LED_RUN actual-drive + LED_WAR restoration) supersedes 1.0.1"}),

    # ==================================================================
    # K8 knowledge persistence: provenance linkage
    # ==================================================================
    ("OBSERVES_COMMAND", "obs:k8-cmd250-timeout-cmd113-success", "command:113", {"note": "K8 comm test exercised 113; succeeded (0.1.0/protocol 0/2026-10-20, byte-identical to K6)"}),
    ("OBSERVES_FIRMWARE", "obs:k8-cmd250-timeout-cmd113-success", "firmware:pic32-0.1.0", {"note": "113 readback reports deployed 0.1.0"}),
    ("RELATED_TO", "obs:k8-cmd250-timeout-cmd113-success", "obs:k8-slaveryinfo-passive-blocked", {"note": "K8 comm test is the follow-up to the passive-SLAVEINFO feasibility finding"}),
    ("SUPPORTED_BY", "hyp:k8-firewall-blocked-udp", "obs:k8-po-firewall-observation", {"note": "pending firewall dialogs reported by PO"}),
    ("SUPPORTED_BY", "hyp:k8-firewall-blocked-udp", "obs:k8-cmd250-timeout-cmd113-success", {"note": "113-success/250-timeout split matches the firewalled-executable hypothesis"}),
    ("SUPPORTED_BY", "hyp:k8-protocol-mismatch-250", "obs:k8-cmd250-timeout-cmd113-success", {"note": "250 timeout despite reachable controller"}),

    # ==================================================================
    # K8B knowledge persistence: root cause + protocol + physical readback
    # ==================================================================
    ("SUPPORTED_BY", "hyp:k8-firewall-blocked-udp", "obs:k8b-fresh-pic18-readback-251f", {"note": "after the firewall Allow rule, 250 ACKed 1/1 and the cold start succeeded"}),
    ("CONTRADICTED_BY", "hyp:k8-protocol-mismatch-250", "evidence:techref-doc", {"note": "techref documents the K8B fresh 0x251F readback (250 ACKed 1/1 after firewall fix); no protocol-0x01 incompatibility"}),
    ("OBSERVES_FIRMWARE", "obs:k8b-fresh-pic18-readback-251f", "firmware:pic18-0x251F", {"note": "fresh HR11=0x251F for amps 1/3/4/6"}),
    ("OBSERVES_FIRMWARE", "obs:k8b-legacy-12-register-slaveryinfo-layout", "firmware:pic18-0x251F", {"note": "12-register SLAVEINFO layout confirms the legacy firmware"}),
    ("RELATED_TO", "obs:k8b-fresh-pic18-readback-251f", "obs:k8b-legacy-12-register-slaveryinfo-layout", {"note": "the 12-register layout fix enabled the fresh readback"}),
    ("RELATED_TO", "obs:k8b-diagnostics-mcp-active-v4", "obs:k8b-fresh-pic18-readback-251f", {"note": "the MCP V4 session reproduced the same readback"}),

    # ==================================================================
    # P2-B1 knowledge persistence: firmware migration + rollback
    # ==================================================================
    ("OBSERVES_FIRMWARE", "obs:p2-b1-rollback-image-proven", "firmware:pic32-0.1.0", {"note": "deployed 0.1.0 = L1 EC7E3E6E, byte-identical to readback"}),
    ("OBSERVES_FIRMWARE", "obs:p2-b1-pic32-migration-proven", "firmware:pic32-1.0.0", {"note": "final PIC32 1.0.0/protocol 1, byte-verified 272632/272632"}),
    ("OBSERVES_FIRMWARE", "obs:p2-b1-1.0.0-legacy-pic18-inert", "firmware:pic18-0x251F", {"note": "legacy PIC18 HR11=0x251F detected-but-inert under 1.0.0 (no flash)"}),
    ("RELATED_TO", "obs:p2-b1-pic32-migration-proven", "obs:p2-b1-rollback-image-proven", {"note": "rollback image enabled the mandatory 0.1.0 rollback test"}),
    ("RELATED_TO", "obs:p2-b1-1.0.0-legacy-pic18-inert", "obs:p2-b1-pic32-migration-proven", {"note": "the inert-legacy-amps outcome was observed during 1.0.0 validation"}),

    # ==================================================================
    # P2-B2-A1 knowledge persistence: amp-1 Phase-1 + reset semantics
    # ==================================================================
    ("OBSERVES_FIRMWARE", "obs:p2-b2-a1-amp1-phase1-deployed", "firmware:pic18-0xE906", {"note": "amp 1 HR11=0xE906, HR7=0xF101, HR21=0x0100, HR22=0x0001"}),
    ("OBSERVES_FIRMWARE", "obs:p2-b2-a1-amp1-phase1-deployed", "firmware:pic32-1.0.1", {"note": "131 single-target flash drove the amp-1 Phase-1 deploy"}),
    ("OBSERVES_FIRMWARE", "obs:p2-b2-a1-amps-4-6-default-wakeup", "firmware:pic18-0x251F", {"note": "amps 3/4/6 remain legacy 0x251F but were stuck in default-wakeup/TIMEOUT after 131"}),
    ("RELATED_TO", "obs:p2-b2-a1-amps-4-6-default-wakeup", "obs:p2-b2-a1-icd3-reset-resets-all", {"note": "the ICD3/full PIC32 reset is the recovery path for the post-131 default-wakeup state"}),

    # ==================================================================
    # P2-B2-A2 knowledge persistence: full four-amplifier Phase-1 migration
    # ==================================================================
    ("OBSERVES_FIRMWARE", "obs:p2-b2-a2-all-four-phase1", "firmware:pic18-0xE906", {"note": "amps 1/3/4/6 all HR7=0xF101 HR11=0xE906 HR21=0x0100 HR22=0x0001"}),
    ("OBSERVES_FIRMWARE", "obs:p2-b2-led-run-corrected", "firmware:pic18-0x5263", {"note": "LED_RUN correction is part of the 1.0.1 image"}),
    ("OBSERVES_FIRMWARE", "obs:p2-b2-bridge-deenergize-fix", "firmware:pic18-0x5263", {"note": "bridge de-energize fix is part of the 1.0.1 image"}),
    ("OBSERVES_FIRMWARE", "obs:p2-b2-05263-deployed", "firmware:pic18-0x5263", {"note": "all four amps read back HR11=0x5263 after deploy"}),
    ("OBSERVES_FIRMWARE", "obs:p2-b2-ffc2-deployed", "firmware:pic18-0xFFC2", {"note": "all four amps read back HR11=0xFFC2 after deploy"}),
    ("RELATED_TO", "obs:p2-b2-a2-107-100-reset-required", "obs:p2-b2-a1-amps-4-6-default-wakeup", {"note": "the 107+100 reset resolves the post-131 default-wakeup / sub-step-7 failure"}),
    ("RELATED_TO", "obs:p2-b2-a2-all-four-phase1", "obs:p2-b2-a2-107-100-reset-required", {"note": "the 107+100 reset preceded each of the three additional amp migrations"}),

    # P2-C: reusable C# harness + init/telemetry/safety command contract
    ("OBSERVES_FIRMWARE", "obs:p2c-harness-validation", "firmware:pic18-0xFFC2", {"note": "amps 1/3/4/6 all read HR11=0xFFC2 during the harness validation"}),
    ("OBSERVES_COMMAND", "obs:p2c-harness-validation", "command:107", {"note": "init 250->112->107->100->106->101->104->105 ACKED"}),
    ("OBSERVES_COMMAND", "obs:p2c-harness-validation", "command:112", {"note": "re-arm (112) cleared the latched host-loss stop; verified state 1->3"}),
    ("OBSERVES_COMMAND", "obs:p2c-harness-validation", "command:108", {"note": "CONFIG/APPLY via 108; DISABLE before APPLY; amp 6 WRITE path failed"}),
    ("GUARDED_BY", "command:108", "safety:lease-gated-on-latch", {"note": "108 register writes rejected while the global stop is latched"}),
    ("GUARDED_BY", "command:112", "safety:rearm-amp-neutral", {"note": "re-arm is non-energizing; safe before init"}),
    ("GUARDED_BY", "command:112", "safety:rearm-interval-no-hr0", {"note": "no amplifier HR0 write can occur in the 112->107 interval"}),
    ("SUPPORTED_BY", "hyp:amp6-marginal-write", "obs:p2c-harness-validation", {"note": "amp 6 WRITE-path failure observed; READ path healthy"}),
    ("SUPPORTED_BY", "hyp:amp6-marginal-write", "obs:p2c-rs422-measurement", {"note": "PicoScope (x1 corrected) shows amp6/amp3 RS-485 RX electrically IDENTICAL -> the amp6 failure is timing, not an electrical bus difference"}),
    ("RELATED_TO", "obs:p2c-harness-validation", "obs:p2-b2-ffc2-deployed", {"note": "P2-C harness validated against the 1.0.2 (0xFFC2) deployment"}),

    # ==================================================================
    # P2-C fault investigation (Fault A + Fault B) provenance linkage
    # ==================================================================
    ("SUPPORTED_BY", "hyp:amp6-marginal-write", "obs:p2c-faulta-104-fix-deployed", {"note": "the 20/20 identity-verification pass after the 4002->2000 timeout fix confirms the marginal-timeout root cause"}),
    ("OBSERVES_FIRMWARE", "obs:p2c-faulta-104-fix-deployed", "firmware:pic32-1.0.1", {"note": "deployed PIC32 8FF89B55.../A77FA25F... instrumentation lineage carries the timeout fix; 113 still reports 1.0.1"}),
    ("OBSERVES_COMMAND", "obs:p2c-faultb-disable-enqueue-mechanism", "command:108", {"note": "the DISABLE (HR1=0x0400) write is a command-108 register write; ENQUEUE->FC16_SENT->TX_DONE->ACK with no purge/gate-drop"}),
    ("RELATED_TO", "obs:p2c-faultb-disable-enqueue-mechanism", "obs:p2c-faultb-stale-hr8-root-cause", {"note": "the clean PIC32 TX/ACK mechanism localises Fault B to the PIC18 HR8 staleness root cause"}),
    ("OBSERVES_FIRMWARE", "obs:p2c-faultb-stale-hr8-root-cause", "firmware:pic18-0xFFC2", {"note": "root cause observed on the deployed 1.0.2 (0xFFC2) PIC18, where HR8 lags the DISABLE write by one ~1.704ms tick"}),
    ("OBSERVES_FIRMWARE", "obs:p2c-faultb-pic18-hr8-fix", "firmware:pic18-0x5784", {"note": "the Fault-B correction is carried by the candidate 0x5784 build"}),
    ("RELATED_TO", "obs:p2c-faultb-pic18-hr8-fix", "obs:p2c-faultb-stale-hr8-root-cause", {"note": "the correction addresses the HR8 staleness root cause"}),
]


# ===========================================================================
# Engineering tool registry (K4-A) — deterministic mirror of tools/tool-registry.yaml
# ===========================================================================
# The registry file (tools/tool-registry.yaml) remains the authoritative
# source; this mirror keeps the graph rebuild self-contained and offline.
# Every node here carries ``source_path = tools/tool-registry.yaml``.
#
# ``K4_SOURCE_REVISION`` is the K4 commit (b5709f3) at which tools/tool-registry.yaml
# was committed; tool nodes carry it as their source_revision.
#
# Status mapping (per PO directive — never invent a validation status):
#   PROVEN_HARDWARE     only tools with recorded hardware evidence (picoscope, hmo2024)
#   PROVEN_OFFLINE_TEST offline-tested tools (knowledge, firmware-tooling, flash_one, legacy CLI)
#   UNKNOWN             not validated (the two PRESERVE_PENDING_REVIEW tools)
# The structural facts (Capability / MCPService / HardwareInterface / Dependency
# / Decision) are registry-sourced, hence PROVEN_SOURCE.
#
# Populated relationships follow the K4-A "Map:" exactly: PROVIDES_CAPABILITY,
# EXPOSED_VIA_MCP, CONNECTS_TO, PRESERVED_BY_POLICY, plus DEPENDS_ON (runtime
# dependency). REUSES, VALIDATED_BY, MIGRATION_CANDIDATE_FOR and their node
# tables (TestEvidence, MigrationDecision) are declared in the schema but are
# intentionally left empty: the registry does not record unambiguous per-tool
# data for them yet.

K4_SOURCE_REVISION = "b5709f3"
_TOOL_REGISTRY_SOURCE = "tools/tool-registry.yaml"

# registry tool id -> the capability that exposes it over MCP (EXPOSED_VIA_MCP).
_MCP_CAPABILITY = {
    "picoscope-mcp": "opencode_mcp",
    "knowledge-foundation": "read_only_knowledge_mcp",
}

_TOOLS = [
    {
        "id": "picoscope-mcp",
        "name": "PicoScope 3406D MSO instrument tooling + MCP",
        "path": "tools/picoscope/",
        "purpose": "Oscilloscope acquisition, digital edge trigger, UART decode, PWM/timing analysis, evidence capture, OpenCode MCP.",
        "lifecycle": "ACTIVE",
        "preservation_policy": "KEEP_ACTIVE_MCP",
        "po_decision": "K4 §5.1 KEEP — ACTIVE / ACCEPTED MCP",
        "language": "Python",
        "entry_points": ["tools.picoscope.mcp (python -m tools.picoscope.mcp)", "tools/picoscope/demo.py"],
        "hardware": "PicoScope 3406D MSO (USB)",
        "side_effect": "MEASUREMENT_ONLY (read-only acquisition; no AWG/signal generation, no flash/reset)",
        "capabilities": ["native_ps3000a_backend", "digital_acquisition", "digital_edge_trigger", "uart_decode_9bit_8bit", "pwm_timing_analysis", "npz_json_evidence", "opencode_mcp"],
        "runtime": "project venv (tools/picoscope/.venv)",
        "offline_tested": True,
        "physically_proven": True,
        "mcp_service": "picoscope",
        "migration_candidate": False,
    },
    {
        "id": "knowledge-foundation",
        "name": "LadybugDB Knowledge Foundation + Knowledge MCP",
        "path": "tools/knowledge/",
        "purpose": "Evidence-backed engineering knowledge graph (LadybugDB) + read-only Knowledge MCP.",
        "lifecycle": "ACTIVE",
        "preservation_policy": "KEEP_ACTIVE_MCP",
        "po_decision": "K4 §5.2 KEEP — ACTIVE / ACCEPTED MCP",
        "language": "Python",
        "entry_points": ["tools.knowledge.mcp_server (python -m tools.knowledge.mcp_server)", "tools.knowledge.ingest.build_graph"],
        "hardware": "none",
        "side_effect": "NONE (read-only graph + deterministic offline ingestion)",
        "capabilities": ["ladybugdb_graph", "deterministic_ingestion", "provenance_evidence", "read_only_knowledge_mcp", "opencode_agent_integration"],
        "runtime": "project venv (tools/knowledge/.venv) + native lbug/OpenSSL DLLs",
        "offline_tested": True,
        "physically_proven": False,
        "mcp_service": "knowledge",
        "migration_candidate": False,
    },
    {
        "id": "firmware-tooling",
        "name": "Firmware build + checksum utilities",
        "path": "tools/firmware/",
        "purpose": "Reproducible PIC18/PIC32 out-of-tree build and HEX checksum verification.",
        "lifecycle": "PROTECTED",
        "preservation_policy": "KEEP_PROTECTED",
        "po_decision": "K4 §5.3 KEEP — PROTECTED, FUTURE MIGRATION CANDIDATE",
        "language": "PowerShell",
        "entry_points": ["tools/firmware/build-firmware.ps1", "tools/firmware/checksum-verify.ps1"],
        "hardware": "none (build/verify only; no flashing)",
        "side_effect": "NONE (offline build + verify; no hardware)",
        "capabilities": ["firmware_build", "hex_generation", "checksum_handling", "firmware_image_verification"],
        "runtime": "Microchip MPLAB X / XC compilers (pinned toolchains)",
        "offline_tested": True,
        "physically_proven": False,
        "mcp_service": None,
        "migration_candidate": True,
    },
    {
        "id": "hmo2024-tooling",
        "name": "Hameg HMO2024 SCPI oscilloscope tooling",
        "path": "tools/hmo2024/",
        "purpose": "Historical HMO2024 measurement: discovery/inventory, SCPI capture/export, waveform + digital/UART decode.",
        "lifecycle": "HISTORICAL",
        "preservation_policy": "KEEP_HISTORICAL",
        "po_decision": "K4 §5.4 KEEP — HISTORICAL / MAINTENANCE-ONLY",
        "language": "Python",
        "entry_points": ["tools/hmo2024/cli.py", "tools/hmo2024/uart/cli.py"],
        "hardware": "Hameg HMO2024 (COM/SCPI)",
        "side_effect": "MEASUREMENT_ONLY (read-only SCPI; no flash/reset)",
        "capabilities": ["scpi_discovery_inventory", "capture_export", "waveform_interpretation", "uart_decode (9-bit/8-bit, project CRC)"],
        "runtime": "system Python",
        "offline_tested": True,
        "physically_proven": True,
        "mcp_service": None,
        "migration_candidate": False,
    },
    {
        "id": "flash-one-driver",
        "name": "Single-target PIC18 flash diagnostic driver (K3 fail-closed)",
        "path": "TrackController5/Python/flash_one.py",
        "purpose": "Offline-safe single-target firmware-download driver; fail-closed FlashState machine (K3).",
        "lifecycle": "PROTECTED",
        "preservation_policy": "KEEP_PROTECTED",
        "po_decision": "K3 corrected; K4 §5.3 reuse for future Diagnostics Core",
        "language": "Python",
        "entry_points": ["TrackController5/Python/flash_one.py", "TrackController5/Python/test_flash_one.py"],
        "hardware": "PIC32 master (UDP) — live path NOT used offline",
        "side_effect": "STATE_CHANGING (live path); OFFLINE dry-run is side-effect-free",
        "capabilities": ["single_target_flash_sequence", "fail_closed_flashstate_machine", "protocol_ack_classification", "host_transaction_logging"],
        "runtime": "system Python",
        "offline_tested": True,
        "physically_proven": False,
        "mcp_service": None,
        "migration_candidate": True,
    },
    {
        "id": "legacy-python-cli",
        "name": "Legacy PIC32 Python command-line tooling",
        "path": "TrackController5/Python/",
        "purpose": "Legacy Python CLI (bootload, Comm UDP transport, Enum/StateMachines) reused by flash_one.py.",
        "lifecycle": "PROTECTED",
        "preservation_policy": "KEEP_PROTECTED",
        "po_decision": "Reuse (not duplicate) per K4 §12",
        "language": "Python",
        "entry_points": ["TrackController5/Python/TrackController.py", "TrackController5/Python/bootload.py"],
        "hardware": "PIC32 master (UDP)",
        "side_effect": "STATE_CHANGING (live); read-only parsing offline",
        "capabilities": ["udp_transport", "hex_parsing", "checksum_parsing", "enum_mirror"],
        "runtime": "system Python",
        "offline_tested": True,
        "physically_proven": False,
        "mcp_service": None,
        "migration_candidate": True,
    },
    {
        "id": "faller-car-bootload",
        "name": "Faller Car uController bootloader Python helper",
        "path": "Faller_Car_uControllerBootLoader.X/Python/bootload.py",
        "purpose": "Bootloader helper for the Faller Car uController firmware.",
        "lifecycle": "HISTORICAL",
        "preservation_policy": "PRESERVE_PENDING_REVIEW",
        "po_decision": "not yet classified",
        "language": "Python",
        "entry_points": ["Faller_Car_uControllerBootLoader.X/Python/bootload.py"],
        "hardware": "Faller Car uController",
        "side_effect": "STATE_CHANGING (live path)",
        "capabilities": ["faller_car_bootloader"],
        "runtime": "system Python",
        "offline_tested": False,
        "physically_proven": False,
        "mcp_service": None,
        "migration_candidate": False,
    },
    {
        "id": "integrator-checksum-helper",
        "name": "Integrator checksum helper (tmp)",
        "path": "tmp/integrator_checksum.py",
        "purpose": "Ad-hoc checksum helper used during firmware review.",
        "lifecycle": "HISTORICAL",
        "preservation_policy": "PRESERVE_PENDING_REVIEW",
        "po_decision": "not yet classified (tmp)",
        "language": "Python",
        "entry_points": ["tmp/integrator_checksum.py"],
        "hardware": "none",
        "side_effect": "NONE",
        "capabilities": ["checksum_comparison"],
        "runtime": "system Python",
        "offline_tested": False,
        "physically_proven": False,
        "mcp_service": None,
        "migration_candidate": False,
    },
    {
        "id": "diagnostics-cold-start-capture",
        "name": "Cold-start SLAVEINFO raw-UDP capture + dual-layout decoder",
        "path": "tools/diagnostics/cold_start_slaveinfo_capture.py",
        "purpose": "Bounded single-session cold start (250->100->101->104->106->105) with raw UDP capture; classifies every datagram and decodes the deployed legacy 12-register and Phase-1 23-register SLAVEINFO layouts (footer-position discrimination). Proved the K8B root cause.",
        "lifecycle": "ACTIVE",
        "preservation_policy": "KEEP",
        "po_decision": "K8B — KEEP_IN_REPOSITORY (proven working reference tool, physically validated 2026-10-25)",
        "language": "Python",
        "entry_points": ["tools/diagnostics/cold_start_slaveinfo_capture.py"],
        "hardware": "PIC32 master (UDP) — live path sends only the six cold-start commands; no 108/107/flash",
        "side_effect": "STATE_CHANGING (live cold-start); decode/classification offline is side-effect-free",
        "capabilities": ["cold_start_init_sequence", "raw_udp_capture", "slaveryinfo_dual_layout_decode", "presence_signal_mb_comm_error"],
        "runtime": "project venv (tools/knowledge/.venv)",
        "offline_tested": True,
        "physically_proven": True,
        "mcp_service": None,
        "migration_candidate": False,
    },
    {
        "id": "diagnostics-mcp-active-session",
        "name": "Diagnostics MCP active controller-session extension (B1..B5)",
        "path": "tools/diagnostics/",
        "purpose": "Grant-gated live controller operation for the Diagnostics MCP: ownership preflight (B1), command-113 identity probe (B4), bounded cold-start init + SLAVEINFO readback + safe stop (B2/B3/B5), and safety abort (B5). Hybrid reuse (C# Validation.Host subprocess + existing Python 113 transport); no second protocol stack.",
        "lifecycle": "ACTIVE",
        "preservation_policy": "KEEP_ACTIVE_MCP",
        "po_decision": "K8B — KEEP_IN_REPOSITORY (accepted Diagnostics MCP surface; live V4 validated 2026-10-25)",
        "language": "Python (+ reuses C# SiebwaldeApp.Koploper.Validation.Host)",
        "entry_points": ["tools.diagnostics.mcp_server (python -m tools.diagnostics.mcp_server)"],
        "hardware": "PIC32 master (UDP) — live path only via scoped single-use grant",
        "side_effect": "STATE_CHANGING (grant-gated session); OFFLINE tools remain side-effect-free",
        "capabilities": ["controller_preflight", "command_113_identity_probe", "bounded_controller_session", "slaveryinfo_readback", "safe_abort", "single_use_grant", "active_offline_separation"],
        "runtime": "project venv (tools/knowledge/.venv) + net8.0 C# host artifact",
        "offline_tested": True,
        "physically_proven": True,
        "mcp_service": "diagnostics",
        "migration_candidate": False,
    },
]


def _slug(value: str) -> str:
    """Deterministic, ASCII-safe slug for stable node ids."""
    value = re.sub(r"[^A-Za-z0-9]+", "-", value).strip("-").lower()
    return value or "x"


def _tool_status(tool: dict) -> str:
    if tool.get("physically_proven"):
        return "PROVEN_HARDWARE"
    if tool.get("offline_tested"):
        return "PROVEN_OFFLINE_TEST"
    return "UNKNOWN"


def _prov(tool_id: str, summary: str, status: str) -> dict:
    """Shared provenance block for tool-registry-derived nodes."""
    return {
        "status": status,
        "summary": summary,
        "source_path": _TOOL_REGISTRY_SOURCE,
        "source_location": f"tools.{tool_id}",
        "source_revision": K4_SOURCE_REVISION,
        "evidence_status": "SOURCE_REF",
    }


def _hardware_kind(name: str) -> str:
    if "USB" in name:
        return "USB"
    if "SCPI" in name:
        return "SCPI"
    if "UDP" in name:
        return "UDP"
    return "MCU"


def _normalize_hardware(hw: str) -> str:
    """Strip a trailing safety note (em-dash suffix) to keep the interface identity."""
    hw = hw.split("\u2014")[0].split(" - ")[0].strip()
    return hw


def _tool_knowledge() -> tuple[list, list]:
    """Generate (nodes, rels) for the tool-registry mirror. Deterministic."""
    nodes: list = []
    rels: list = []

    capabilities: dict[str, str] = {}   # capability name -> node id
    mcp_services: dict[str, str] = {}   # mcp name -> node id
    hardware: dict[str, str] = {}       # normalized name -> node id
    dependencies: dict[str, str] = {}   # runtime string -> node id

    for tool in _TOOLS:
        tid = tool["id"]
        tool_id = f"tool:{tid}"
        status = _tool_status(tool)

        nodes.append(("Tool", {
            "id": tool_id,
            "label": tool["name"],
            "path": tool["path"],
            "purpose": tool["purpose"],
            "lifecycle": tool["lifecycle"],
            "preservation_policy": tool["preservation_policy"],
            "language": tool["language"],
            "entry_points": ", ".join(tool["entry_points"]),
            "hardware": tool["hardware"],
            "side_effect": tool["side_effect"],
            "offline_tested": tool["offline_tested"],
            "physically_proven": tool["physically_proven"],
            "mcp_service": tool.get("mcp_service"),
            "migration_candidate": tool["migration_candidate"],
            "po_decision": tool["po_decision"],
            **_prov(tid, tool["purpose"], status),
        }))

        # capabilities + PROVIDES_CAPABILITY
        for cap in tool["capabilities"]:
            if cap not in capabilities:
                cid = f"capability:{cap}"
                capabilities[cap] = cid
                nodes.append(("Capability", {
                    "id": cid,
                    "label": cap,
                    "name": cap,
                    "description": f"Capability provided by tool '{tool['name']}' (per tool registry).",
                    **_prov(tid, f"Capability '{cap}' of tool '{tid}' (per tool registry).", "PROVEN_SOURCE"),
                }))
            rels.append(("PROVIDES_CAPABILITY", tool_id, capabilities[cap], {}))

        # MCP service + EXPOSED_VIA_MCP (Capability -> MCPService)
        mcp = tool.get("mcp_service")
        if mcp:
            if mcp not in mcp_services:
                mid = f"mcp:{mcp}"
                mcp_services[mcp] = mid
                nodes.append(("MCPService", {
                    "id": mid,
                    "label": mcp,
                    "name": mcp,
                    **_prov(tid, f"MCP service '{mcp}' (per tool registry).", "PROVEN_SOURCE"),
                }))
            cap_for_mcp = _MCP_CAPABILITY.get(tid)
            if cap_for_mcp and cap_for_mcp in capabilities:
                rels.append(("EXPOSED_VIA_MCP", capabilities[cap_for_mcp], mcp_services[mcp], {}))

        # hardware + CONNECTS_TO
        hw_raw = tool.get("hardware") or ""
        if hw_raw and not hw_raw.lower().startswith("none"):
            hw_name = _normalize_hardware(hw_raw)
            if hw_name not in hardware:
                hid = f"hardware:{_slug(hw_name)}"
                hardware[hw_name] = hid
                nodes.append(("HardwareInterface", {
                    "id": hid,
                    "label": hw_name,
                    "name": hw_name,
                    "kind": _hardware_kind(hw_name),
                    **_prov(tid, f"Hardware interface '{hw_name}' (per tool registry).", "PROVEN_SOURCE"),
                }))
            rels.append(("CONNECTS_TO", tool_id, hardware[hw_name], {}))

        # runtime dependency + DEPENDS_ON
        runtime = tool.get("runtime")
        if runtime:
            if runtime not in dependencies:
                did = f"dependency:{_slug(runtime)}"
                dependencies[runtime] = did
                nodes.append(("Dependency", {
                    "id": did,
                    "label": runtime,
                    "name": runtime,
                    **_prov(tid, f"Runtime dependency '{runtime}' (per tool registry).", "PROVEN_SOURCE"),
                }))
            rels.append(("DEPENDS_ON", tool_id, dependencies[runtime], {}))

        # PO preservation decision + PRESERVED_BY_POLICY (Tool -> Decision)
        decision_id = f"decision:k4-tool-preservation-{tid}"
        nodes.append(("Decision", {
            "id": decision_id,
            "label": f"K4 preservation decision for {tid}",
            "decision": tool["po_decision"],
            "decided_by": "Product Owner",
            "date": "2026-10-23",
            **_prov(tid, f"PO preservation decision for tool '{tid}': {tool['po_decision']}", "PROVEN_SOURCE"),
        }))
        rels.append(("PRESERVED_BY_POLICY", tool_id, decision_id, {}))

    return nodes, rels


_tool_nodes, _tool_rels = _tool_knowledge()
NODES.extend(_tool_nodes)
RELS.extend(_tool_rels)
