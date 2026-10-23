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
        "status": "UNKNOWN",
        "summary": "Prototype amplifier address 1. Ended the 2026-10-19 round-trip on 0xE906 (1.0.0); HR11=0xE906 runtime readback. Its exact current state is UNKNOWN (not re-verified after PICO-5B).",
        "source_path": "docs/firmware-flash-recovery-technical-reference.md",
        "source_location": "2.5", "source_revision": "1f89c0b", "evidence_status": "UNVERIFIED",
    }),
    ("HardwareDevice", {
        "id": "device:amp-3", "label": "Amplifier 3", "kind": "PIC18",
        "address": "3", "role": "traction slave",
        "status": "UNKNOWN",
        "summary": "Prototype amplifier address 3. Named as the PICO-5B single-target flash target; ERASE/WRITE were sent without confirmed bootloader acks, so its application state is UNKNOWN (PO directive).",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "2", "source_revision": "1f89c0b", "evidence_status": "UNVERIFIED",
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
        "id": "firmware:pic18-0x195A", "label": "PIC18 L2 0x195A candidate", "kind": "application",
        "target": "PIC18", "checksum": "0x195A", "version": "L2",
        "status": "PROVEN_SOURCE",
        "summary": "L2 version-ID candidate (checksum 0x195A). NOT deployed; never flashed to hardware.",
        "source_path": "docs/L2-STAGE-A-PYTHON-COMPARISON-AND-RETRY.md",
        "source_location": "summary", "source_revision": "1f89c0b", "evidence_status": "SOURCE_REF",
    }),
    ("FirmwareArtifact", {
        "id": "firmware:pic18-0x2D00", "label": "PIC18 diagnostic 0x2D00", "kind": "diagnostic",
        "target": "PIC18", "checksum": "0x2D00", "version": "diagnostic",
        "image_path": "release/diagnostic-pico-5b/TrackAmplifier4.X.production.hex",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Temporary PICO-5B diagnostic image (checksum 0x2D00) built by uncommenting main.c test byte; archived feature/pico-5b-diagnostic @ bea4251.",
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
        "summary": "Python single-target flash driver; 26 offline tests; the basis of the PICO-5B live runs.",
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
        "id": "obs:po-normal-start", "label": "PO verified normal start",
        "what": "PO confirmed via the Siebwalde app that all amplifiers start normally and the target was not stuck in bootloader.",
        "date": "2026-10-23", "method": "PO operator check",
        "status": "UNKNOWN",
        "summary": "PO reported no amplifier stuck in bootloader after the PICO-5B attempt; consistent with ERASE not having executed, but the exact target state was not independently re-verified.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "operator", "source_revision": "1f89c0b", "evidence_status": "UNVERIFIED", "observed_date": "2026-10-23",
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
        "evidence_for": "No bootloader ack; PO observed all amps start normally and none stuck in bootloader.",
        "evidence_against": "",
        "status": "INFERRED",
        "summary": "Consistent with observations, but the target application state remains UNKNOWN until independently re-verified.",
        "source_path": "docs/PICO-5B-PHYSICAL-SESSION-PREFLIGHT.md",
        "source_location": "operator", "source_revision": "1f89c0b", "evidence_status": "DERIVED",
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
        "kind": "artifact", "path": "release/diagnostic-pico-5b/TrackAmplifier4.X.production.hex",
        "format": "intel-hex", "hash": "ABAEF621C25E1AB046FB98834E7A5DB84D49C0FBEAF49AFEAF62DAAF082D9E53",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Temporary diagnostic image used in PICO-5B.",
        "source_path": "release/diagnostic-pico-5b/TrackAmplifier4.X.production.hex",
        "source_location": "artifact", "source_revision": "bea4251", "evidence_status": "TEST_PASS",
    }),
    ("EvidenceSource", {
        "id": "evidence:flash-one-driver", "label": "flash_one.py driver",
        "kind": "source", "path": "TrackController5/Python/flash_one.py", "format": "python",
        "status": "PROVEN_OFFLINE_TEST",
        "summary": "Single-target flash driver (26 offline tests).",
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
]


# ---------------------------------------------------------------------------
# Relationships: (rel_name, from_id, to_id, properties).
# ---------------------------------------------------------------------------
RELS: list[tuple[str, str, str, dict]] = [
    # RUNS_FIRMWARE
    ("RUNS_FIRMWARE", "device:amp-1", "firmware:pic18-0xE906", {"since": "2026-10-19 round-trip", "superseded": False}),
    ("RUNS_FIRMWARE", "device:amp-3", "firmware:pic18-0x251F", {"since": "pre-PICO-5B", "superseded": False}),
    ("RUNS_FIRMWARE", "device:amp-4", "firmware:pic18-0x251F", {"since": "pre-PICO-5B", "superseded": False}),
    ("RUNS_FIRMWARE", "device:amp-6", "firmware:pic18-0x251F", {"since": "pre-PICO-5B", "superseded": False}),
    ("RUNS_FIRMWARE", "device:pic32", "firmware:pic32-0.1.0", {"since": "L1", "superseded": False}),

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
]


# ===========================================================================
# Engineering tool registry (K4-A) — deterministic mirror of tools/tool-registry.yaml
# ===========================================================================
# The registry file (tools/tool-registry.yaml) remains the authoritative
# source; this mirror keeps the graph rebuild self-contained and offline.
# Every node here carries ``source_path = tools/tool-registry.yaml``.
#
# ``K4_SOURCE_REVISION`` is a PLACEHOLDER for the K4 commit: set it to the real
# K4 commit SHA once this increment is committed. Until then it is deliberately
# a non-commit token so nothing can be mistaken for a git-resolved revision.
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
