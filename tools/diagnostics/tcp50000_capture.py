#!/usr/bin/env python3
"""Capture the PIC32 TCP diagnostic log (port 50000) and parse the MBUS event ring.

The PIC32 master emits ``Mbus event: type=N slave=S reg=R val=V rv=R seq=Q`` lines over
TCP port 50000 (log.c / slavecommhandler.c MBUS_EVENT_REPORT). This tool connects to the
controller, streams the log to a file (raw evidence) and prints a filtered view of the
MBUS event ring — the only source of per-write ACK / EXCEPTION / TIMEOUT / GATE_DROP /
FC16_SENT / TX_DONE discrimination for a GROUP_READY (HR23=0x0002) write.

Event types (mbus_event.h):
  1 ENQUEUE    command-108 accepted into the mailbox
  2 GATE_DROP  write rejected by the compatibility gate
  3 FC16_SENT  FC16 write dispatched (rv = SEND result)
  4 PURGE      mailbox purged by a global-stop latch
  5 TX_DONE    FC16 bytes physically sent (TX -> WAIT_ANSWER)
  6 ACK        valid FC16 echo received
  7 TIMEOUT    no echo
  8 EXCEPTION  correlated Modbus exception (val = exception code, e.g. 0x03)

This tool is READ-ONLY with respect to the controller: it only connects to the TCP log
port and reads. It never sends a command, never writes a register, never re-arms, never
flashes. It is intended to run ALONGSIDE hil-b/hil-d (which drives the UDP control path)
so the event ring for the GROUP_READY writes is captured.

Usage:
  python tcp50000_capture.py [--host 192.168.1.193] [--port 50000]
                             [--out <path>] [--duration <seconds>] [--slave 6]
"""
import argparse
import socket
import sys
import time

MBUS_EVENT_NAMES = {
    "1": "ENQUEUE",
    "2": "GATE_DROP",
    "3": "FC16_SENT",
    "4": "PURGE",
    "5": "TX_DONE",
    "6": "ACK",
    "7": "TIMEOUT",
    "8": "EXCEPTION",
}


def parse_mbus_event(line):
    """Parse a ``Mbus event: type=N slave=S reg=R val=V rv=R seq=Q`` line into a dict or None."""
    if not line.startswith("Mbus event:"):
        return None
    fields = {}
    for token in line[len("Mbus event:"):].split():
        if "=" in token:
            key, value = token.split("=", 1)
            fields[key] = value
    return fields


def main():
    parser = argparse.ArgumentParser(description="Capture and parse the PIC32 TCP-50000 MBUS event log.")
    parser.add_argument("--host", default="192.168.1.193")
    parser.add_argument("--port", type=int, default=50000)
    parser.add_argument("--out", default=None, help="Raw log output path (append).")
    parser.add_argument("--duration", type=float, default=0.0, help="Stop after N seconds (0 = until Ctrl+C).")
    parser.add_argument("--slave", type=int, default=None, help="Only print MBUS events for this slave address.")
    args = parser.parse_args()

    raw = open(args.out, "a", encoding="utf-8", errors="replace") if args.out else None
    deadline = time.monotonic() + args.duration if args.duration > 0 else None

    print(f"Connecting to {args.host}:{args.port} (read-only capture, Ctrl+C to stop)...", file=sys.stderr)
    try:
        sock = socket.create_connection((args.host, args.port), timeout=10)
    except OSError as exc:
        print(f"CONNECT FAILED: {exc}", file=sys.stderr)
        return 2

    sock.settimeout(1.0)
    buf = b""
    try:
        while True:
            if deadline is not None and time.monotonic() >= deadline:
                break
            try:
                chunk = sock.recv(4096)
            except socket.timeout:
                continue
            if not chunk:
                break
            buf += chunk
            while b"\n" in buf:
                raw_line, buf = buf.split(b"\n", 1)
                line = raw_line.decode("utf-8", errors="replace").rstrip("\r")
                if raw is not None:
                    raw.write(line + "\n")
                ev = parse_mbus_event(line)
                if ev is None:
                    continue
                if args.slave is not None and ev.get("slave") != str(args.slave):
                    continue
                name = MBUS_EVENT_NAMES.get(ev.get("type", "?"), f"TYPE{ev.get('type', '?')}")
                print(
                    f"{time.strftime('%H:%M:%S')} {name:<10} slave={ev.get('slave', '?'):<3} "
                    f"reg={ev.get('reg', '?'):<3} val={ev.get('val', '?'):<5} "
                    f"rv={ev.get('rv', '?'):<3} seq={ev.get('seq', '?')}",
                    flush=True)
    except KeyboardInterrupt:
        print("\nStopped by operator (Ctrl+C).", file=sys.stderr)
    finally:
        sock.close()
        if raw is not None:
            raw.flush()
            raw.close()
            print(f"Raw log written to {args.out}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    sys.exit(main())
