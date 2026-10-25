"""Cold-start SLAVEINFO raw-UDP capture (read-only diagnostic).

Run a bounded, single-session cold start against the PIC32 TrackController and
capture *every* inbound UDP datagram so the Project Lead can distinguish, from
raw wire evidence, why the SLAVEINFO stream is empty after all six telemetry
commands are acknowledged:

    * "frames arrive with SlaveDetected == 0"  (detection failed, but the
      upload loop IS emitting)  vs
    * "no SLAVEINFO frames at all"            (the upload loop / timer is not
      emitting)  vs
    * "frames arrive at a different length"   (envelope mismatch)

Stdlib only (``socket`` / ``struct`` / ``time`` / ``argparse``). No ``Comm`` /
``DataAquisition`` / ``flash_one`` import graph, no third-party packages, no
filesystem writes, no flash / reset / hardware-control. It opens exactly one UDP
socket, sends exactly the six cold-start commands, then listens for a bounded
number of seconds. This is a *single* diagnostic run; it does not own, arbitrate
or lock the device across processes.

Command order and framing mirror the KIS-09 read-only host
(``SiebwaldeApp/SiebwaldeApp.Koploper.Validation.Host``):

    * ``ReadOnlyTelemetryValidationHost.EnableSessionAsync`` sends
      250 -> 100 -> 101 -> 104 -> 106 -> 105 in that order.
    * ``SessionCommandData = new byte[80]`` and
      ``SendSessionCommandAsync(command)`` -> ``new SendMessage(command, data)``
    * ``TrackCommClientAsync.BuildFrameFromSendMessage`` produces
      ``[0xAA, command] + data`` (no footer), i.e. 82 bytes for these commands.

Command byte values (``PublicEnums.cs`` / ``enums.h``):

    250 = 0xFA  CLIENT_CONNECTION_REQUEST      (registers the client address)
    100 = 0x64  EXEC_MBUS_STATE_SLAVES_ON
    101 = 0x65  EXEC_MBUS_STATE_SLAVE_DETECT
    104 = 0x68  EXEC_MBUS_STATE_SLAVE_INIT
    106 = 0x6A  EXEC_MBUS_STATE_START_DATA_UPLOAD  (starts the comm-cycle timer
                                                    and sets upload=ALL)
    105 = 0x69  EXEC_MBUS_STATE_SLAVE_ENABLE       (-> SERVICE_TASKS)

Network (``ethernet.c``, ``live_transport.py``): the PIC32 listens on
SERVER_PORT 10000 and replies from CLIENT_PORT 10001 to the client's bound port;
this script therefore binds local UDP port 10001 and transmits to
``endpoint:10000``.

SLAVEINFO frame layout (source-mirrored, both envelopes).

Legacy 12-register (the DEPLOYED firmware — ``NUMBER_OF_HOLDING_PETITREGISTERS
= 12``, ``sizeof(SLAVE_INFO) = 40``) wrapped in the 82-byte ``udpTrans_t``:

        [0]  = 0xAA  udpTrans_t.header
        [1]  = 0xFF  udpTrans_t.command = SLAVEINFO
        [2]  = 0xAA  SLAVE_INFO.Header
        [3]  = SlaveNumber
        [4]  = SlaveDetected          (0 or 1; see note below)
        [5]  = struct padding
        [6..29]  = HoldingReg[0..11]  (12 x uint16 LE)
        [20..21] = HR7  (identity)   [28..29] = HR11 (checksum)
        [30..31] = MbReceiveCounter   [32..33] = MbSentCounter
        [34..37] = MbCommError (uint32 LE)   [38] = MbExceptionCode
        [39] = SpiCommErrorCounter    [40] = Footer 0x55
        [41..81] = trailing udpTrans_t bytes (uninitialised garbage)

Phase-1 23-register packed (``TrackSlaveInfoFrame.Encode``, 63 bytes, used by
the simulator) omits the legacy trailing padding: HoldingReg[0..22] at
[6..51], MbReceiveCounter [52..53], MbSentCounter [54..55], MbCommError
[56..59], MbExceptionCode [60], SpiCommErrorCounter [61], Footer 0x55 at [62].

The decoder below detects the layout by footer position (0x55 at [40] =>
legacy 12-register; 0x55 at [62] => Phase-1 23-register) and reports which it
saw. HR7/HR11 are at the same offsets in both layouts.

Note on SlaveDetected: on the deployed legacy firmware the uploaded
``SlaveDetected`` byte is a DETECT-phase latch, not a live-presence signal.
Amps 1/3/4/6 report real FC03 polling data (HR7/HR11 non-zero, MbCommError =
SLAVE_DATA_OK) while ``SlaveDetected`` reads 0 after they are re-addressed by
SLAVE_INIT (they no longer answer the 0xAA DETECT probe). Treat
``MbCommError == SLAVE_DATA_OK`` with a non-zero HR11/HR7 as the presence
signal, not ``SlaveDetected``.
"""

from __future__ import annotations

import argparse
import socket
import struct
import sys
import time

# --------------------------------------------------------------------------- #
# Wire constants (source-mirrored)
# --------------------------------------------------------------------------- #

HEADER = 0xAA
FOOTER = 0x55
SLAVEINFO = 0xFF

# Task IDs (enums.h TASK_ID / PublicEnums.cs TaskId)
TASK_ID = {10: "CONTROLLER", 20: "MBUS", 30: "FWHANDLER"}

# Task states (enums.h TASK_STATE / PublicEnums.cs TaskStates)
TASK_STATE = {4: "ABORT", 5: "BUSY", 6: "CONNECTED", 7: "DONE", 8: "COMMAND", 9: "ERROR"}

# Command names (enums.h TASK_COMMAND / PublicEnums.cs TrackCommand)
COMMAND_NAME = {
    6: "CONNECTED",
    100: "EXEC_MBUS_STATE_SLAVES_ON",
    101: "EXEC_MBUS_STATE_SLAVE_DETECT",
    102: "EXEC_MBUS_STATE_SLAVES_BOOT_WAIT",
    103: "EXEC_MBUS_STATE_SLAVE_FW_FLASH",
    104: "EXEC_MBUS_STATE_SLAVE_INIT",
    105: "EXEC_MBUS_STATE_SLAVE_ENABLE",
    106: "EXEC_MBUS_STATE_START_DATA_UPLOAD",
    107: "EXEC_MBUS_STATE_RESET",
    108: "EXEC_MBUS_SLAVE_DATA_EXCH",
    109: "EXEC_GLOBAL_STOP",
    110: "HOST_HEARTBEAT",
    111: "GLOBAL_STOP_STATE",
    112: "REARM_GLOBAL_STOP",
    113: "GET_CONTROLLER_VERSION",
    250: "CLIENT_CONNECTION_REQUEST",
}

# The mandated cold-start order and their byte values.
COLD_START = (
    (250, 0xFA, "CLIENT_CONNECTION_REQUEST"),
    (100, 0x64, "EXEC_MBUS_STATE_SLAVES_ON"),
    (101, 0x65, "EXEC_MBUS_STATE_SLAVE_DETECT"),
    (104, 0x68, "EXEC_MBUS_STATE_SLAVE_INIT"),
    (106, 0x6A, "EXEC_MBUS_STATE_START_DATA_UPLOAD"),
    (105, 0x69, "EXEC_MBUS_STATE_SLAVE_ENABLE"),
)

# SLAVE_DATA enum values (PetitModbus.h)
SLAVE_DATA_NAME = {
    0x00: "IDLE", 0x01: "BUSY", 0x02: "OK", 0x03: "NOK",
    0x04: "TIMEOUT", 0x05: "EXCEPTION", 0x06: "UNKNOWN",
}

DEFAULT_ENDPOINT = "192.168.1.193"
DEFAULT_TX_PORT = 10000   # PIC32 SERVER_PORT
DEFAULT_RECV_PORT = 10001  # PIC32 CLIENT_PORT / client recv bind


# --------------------------------------------------------------------------- #
# Framing helpers (pure)
# --------------------------------------------------------------------------- #

def build_command_frame(command: int) -> bytes:
    """``[0xAA, command] + 80 zero bytes`` — mirrors BuildFrameFromSendMessage."""
    return struct.pack("<BB", HEADER, command) + b"\x00" * 80


def _u16(data: bytes, offset: int) -> int:
    return struct.unpack_from("<H", data, offset)[0]


def _u32(data: bytes, offset: int) -> int:
    return struct.unpack_from("<I", data, offset)[0]


def classify_frame(data: bytes) -> dict:
    """Classify one datagram as SLAVEINFO / status / unknown (no socket I/O)."""
    info = {
        "length": len(data),
        "kind": "unknown",
        "first_bytes": data[:12].hex(" "),
    }
    if len(data) < 2:
        info["kind"] = "short"
        return info

    if data[0] != HEADER:
        info["kind"] = "non_header"
        return info

    command = data[1]

    if command == SLAVEINFO:
        # SLAVEINFO frame. Layout is detected by footer position:
        #   * legacy 12-register (deployed firmware): footer 0x55 at [40],
        #     MbCommError at [34], counters at [30]/[32]  (82-byte udpTrans_t).
        #   * Phase-1 23-register packed (simulator Encode): footer 0x55 at [62],
        #     MbCommError at [56], counters at [52]/[54]  (63-byte frame).
        # HR7 (offset 20) and HR11 (offset 28) are identical in both layouts
        # because the register array starts at [6] in both.
        legacy = len(data) >= 41 and data[40] == FOOTER
        phase1 = len(data) >= 63 and data[62] == FOOTER
        if legacy:
            footer_off, mbcomm_off = 40, 34
            mb_receive_off, mb_sent_off = 30, 32
            envelope = "legacy_12reg_82B"
        elif phase1:
            footer_off, mbcomm_off = 62, 56
            mb_receive_off, mb_sent_off = 52, 54
            envelope = "phase1_23reg_63B"
        else:
            footer_off, mbcomm_off = 40, 34
            mb_receive_off, mb_sent_off = 30, 32
            envelope = f"len_{len(data)}"

        info["kind"] = "SLAVEINFO"
        info["envelope"] = envelope
        info["inner_header"] = data[2] if len(data) > 2 else None
        info["slave_number"] = data[3] if len(data) > 3 else None
        info["slave_detected"] = data[4] if len(data) > 4 else None
        if len(data) >= 30:
            info["hr7_identity"] = _u16(data, 20)       # HoldingReg[7]
            info["hr11_checksum"] = _u16(data, 28)      # HoldingReg[11]
        if len(data) >= mb_sent_off + 2:
            info["mb_receive_counter"] = _u16(data, mb_receive_off)
            info["mb_sent_counter"] = _u16(data, mb_sent_off)
        if len(data) >= mbcomm_off + 4:
            info["mb_comm_error"] = _u32(data, mbcomm_off)
            info["mb_comm_error_name"] = SLAVE_DATA_NAME.get(info["mb_comm_error"], str(info["mb_comm_error"]))
        if len(data) > footer_off:
            info["footer_ok"] = data[footer_off] == FOOTER
            info["footer"] = data[footer_off]
        return info

    if command in TASK_ID:
        # Controller / MBUS / FW status (ack) message:
        #   [0]=0xAA [1]=task_id [2]=task_command [3]=task_state [4]=task_message
        info["kind"] = "status"
        info["task_id"] = TASK_ID[command]
        info["task_command"] = data[2] if len(data) > 2 else None
        info["task_state"] = data[3] if len(data) > 3 else None
        info["task_message"] = data[4] if len(data) > 4 else None
        if info["task_command"] is not None:
            info["task_command_name"] = COMMAND_NAME.get(
                info["task_command"], f"0x{info['task_command']:02X}")
        if info["task_state"] is not None:
            info["task_state_name"] = TASK_STATE.get(
                info["task_state"], f"0x{info['task_state']:02X}")
        info["is_done"] = info["task_state"] == 7
        return info

    info["kind"] = "other_command"
    info["command"] = command
    info["command_name"] = COMMAND_NAME.get(command, f"0x{command:02X}")
    return info


def format_frame(info: dict) -> str:
    """One-line human-readable rendering of a classified datagram."""
    if info["kind"] == "SLAVEINFO":
        env = info.get("envelope", "?")
        return (
            f"SLAVEINFO {env} slave={info.get('slave_number')} "
            f"detected={info.get('slave_detected')} "
            f"HR7=0x{info.get('hr7_identity', 0):04X} "
            f"HR11=0x{info.get('hr11_checksum', 0):04X} "
            f"mbCommError={info.get('mb_comm_error_name', '?')} "
            f"footer_ok={info.get('footer_ok')}"
        )
    if info["kind"] == "status":
        return (
            f"STATUS {info.get('task_id')} "
            f"{info.get('task_command_name', info.get('task_command'))} "
            f"state={info.get('task_state_name', info.get('task_state'))}"
        )
    if info["kind"] == "other_command":
        return f"CMD 0x{info.get('command'):02X} {info.get('command_name')}"
    if info["kind"] == "short":
        return f"SHORT len={info['length']}"
    return f"{info['kind'].upper()} len={info['length']}"


# --------------------------------------------------------------------------- #
# Socket helpers
# --------------------------------------------------------------------------- #

def make_socket(recv_port: int) -> socket.socket:
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind(("", recv_port))
    return sock


def receive_until(sock, deadline: float) -> list:
    """Receive datagrams until ``deadline`` (monotonic); return (data, addr)."""
    frames = []
    while True:
        remaining = deadline - time.monotonic()
        if remaining <= 0:
            break
        sock.settimeout(remaining)
        try:
            data, addr = sock.recvfrom(4096)
        except socket.timeout:
            break
        frames.append((data, addr))
    return frames


# --------------------------------------------------------------------------- #
# Main
# --------------------------------------------------------------------------- #

def main(argv=None) -> int:
    parser = argparse.ArgumentParser(
        description="Cold-start SLAVEINFO raw-UDP capture (read-only diagnostic).")
    parser.add_argument("--endpoint", default=DEFAULT_ENDPOINT,
                        help=f"PIC32 address (default {DEFAULT_ENDPOINT})")
    parser.add_argument("--tx-port", type=int, default=DEFAULT_TX_PORT,
                        help=f"PIC32 server port (default {DEFAULT_TX_PORT})")
    parser.add_argument("--recv-port", type=int, default=DEFAULT_RECV_PORT,
                        help=f"local bind port (default {DEFAULT_RECV_PORT})")
    parser.add_argument("--seconds", type=float, default=10.0,
                        help="capture duration after the last command (default 10)")
    parser.add_argument("--ack-timeout", type=float, default=5.0,
                        help="per-command DONE wait (default 5.0 s)")
    parser.add_argument("--no-ack-wait", action="store_true",
                        help="do not wait for each DONE ack; fixed 0.2 s between sends")
    args = parser.parse_args(argv)

    if args.seconds <= 0 or args.ack_timeout <= 0:
        print("--seconds and --ack-timeout must be > 0", file=sys.stderr)
        return 2

    print(f"== cold-start SLAVEINFO capture ==")
    print(f"endpoint={args.endpoint} tx_port={args.tx_port} recv_port={args.recv_port}")
    print(f"capture={args.seconds}s ack_timeout={args.ack_timeout}s "
          f"no_ack_wait={args.no_ack_wait}")

    sock = make_socket(args.recv_port)
    counts = {"total": 0, "slaveinfo": 0, "slaveinfo_detected": 0,
              "slaveinfo_undetected": 0, "status": 0, "done": 0, "other": 0,
              "comm_ok_amps": 0}
    comm_ok_amp_numbers = set()
    length_hist = {}

    def log(data, addr):
        info = classify_frame(data)
        counts["total"] += 1
        length_hist[len(data)] = length_hist.get(len(data), 0) + 1
        if info["kind"] == "SLAVEINFO":
            counts["slaveinfo"] += 1
            if info.get("slave_detected") == 1:
                counts["slaveinfo_detected"] += 1
            else:
                counts["slaveinfo_undetected"] += 1
            # Comm-OK amp presence: MbCommError == SLAVE_DATA_OK with a non-zero
            # HR11 checksum (the legacy DETECT-latch SlaveDetected is not used).
            if (info.get("mb_comm_error") == 0x02
                    and 1 <= info.get("slave_number", 0) <= 50
                    and info.get("hr11_checksum", 0) != 0):
                counts["comm_ok_amps"] += 1
                comm_ok_amp_numbers.add(info["slave_number"])
        elif info["kind"] == "status":
            counts["status"] += 1
            if info.get("is_done"):
                counts["done"] += 1
        else:
            counts["other"] += 1
        full = data.hex(" ")
        if info["kind"] == "SLAVEINFO":
            print(f"[{time.strftime('%H:%M:%S')}] {addr[0]}:{addr[1]} len={len(data):3d} "
                  f"{format_frame(info)}\n    FULL: {full}")
        else:
            print(f"[{time.strftime('%H:%M:%S')}] {addr[0]}:{addr[1]} len={len(data):3d} "
                  f"{info['first_bytes']} | {format_frame(info)}")

    try:
        # --- Phase 1: send the six cold-start commands (with optional ack wait).
        for dec, hexv, name in COLD_START:
            frame = build_command_frame(dec)
            sock.sendto(frame, (args.endpoint, args.tx_port))
            print(f">>> sent {name} (0x{hexv:02X}, {len(frame)} bytes)")

            if args.no_ack_wait:
                time.sleep(0.2)
                continue

            deadline = time.monotonic() + args.ack_timeout
            done_seen = False
            while not done_seen and time.monotonic() < deadline:
                for data, addr in receive_until(sock, deadline):
                    log(data, addr)
                    info = classify_frame(data)
                    if info["kind"] == "status" and info.get("is_done"):
                        done_seen = True
            if not done_seen:
                print(f"    (no DONE ack for {name} within {args.ack_timeout}s)")

        # --- Phase 2: bounded capture of the SLAVEINFO stream.
        print(f">>> capture for {args.seconds}s ...")
        capture_deadline = time.monotonic() + args.seconds
        while time.monotonic() < capture_deadline:
            for data, addr in receive_until(sock, capture_deadline):
                log(data, addr)
    except OSError as exc:
        print(f"socket error: {exc}", file=sys.stderr)
        return 1
    finally:
        sock.close()

    # --- Summary.
    print("== summary ==")
    print(f"datagrams received: {counts['total']}")
    print(f"  SLAVEINFO frames: {counts['slaveinfo']} "
          f"(detected=1: {counts['slaveinfo_detected']}, "
          f"detected=0: {counts['slaveinfo_undetected']})")
    print(f"  status/ack frames: {counts['status']} (DONE: {counts['done']})")
    print(f"  other frames: {counts['other']}")
    print(f"  datagram length histogram: {dict(sorted(length_hist.items()))}")

    if counts["slaveinfo"] == 0:
        print("RESULT: no SLAVEINFO frames arrived -> upload loop / timer is not "
              "emitting (or frames are not delivered).")
    else:
        print(f"RESULT: SLAVEINFO stream delivering. "
              f"comm-OK amps (MbCommError==OK, HR11!=0): "
              f"{sorted(comm_ok_amp_numbers) or 'none'} "
              f"({counts['comm_ok_amps']} frames). "
              f"Note: legacy SlaveDetected is a DETECT-latch, not presence.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
