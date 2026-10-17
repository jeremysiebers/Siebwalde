"""Command-line interface.

Subcommands:

* ``discover``   -- enumerate candidate VCP ports, probe ``*IDN?`` safely.
* ``inventory``  -- READ-ONLY instrument state snapshot.
* ``capture``    -- EXPLICIT ACTION: bounded single acquisition (needs --confirm).
* ``export``     -- export an already-held record WITHOUT rearming.
* ``verify``     -- OFFLINE integrity checks/analysis (alias ``analyze``).

Side-effect separation: ``discover``/``inventory``/``verify`` are query-only or
offline. Only ``capture`` mutates instrument acquisition state, and it requires
``--confirm``.
"""

from __future__ import annotations

import argparse
import json
import sys
from typing import Dict, List, Optional

from version import __version__, tool_version, git_commit
import analyze as analyze_mod
import export as export_mod
import inventory as inventory_mod
import records as records_mod
from acquisition import (
    CaptureError,
    CaptureInterrupted,
    CaptureSession,
    CaptureTimeout,
    CaptureNotConfirmed,
    NoTrigger,
    ScopeNotIdle,
)
from transport import HmoTransport, SerialConfig, TransportError

DEFAULT_OUT_ROOT = "tmp"


def _make_transport(port: str, baud: int) -> HmoTransport:
    return HmoTransport(SerialConfig.scope_default(port))


def _print_json(obj) -> None:
    print(json.dumps(obj, indent=2, sort_keys=True, default=str))


# ---------------------------------------------------------------------------
# discover
# ---------------------------------------------------------------------------
def cmd_discover(args) -> int:
    ports: List[str] = []
    if args.ports:
        ports = [p.strip() for p in args.ports.split(",") if p.strip()]
    else:
        try:
            import serial.tools.list_ports

            ports = [p.device for p in serial.tools.list_ports.comports()]
        except Exception as exc:
            print(f"error: cannot enumerate ports: {exc}", file=sys.stderr)
            return 1

    results = []
    scope_port = None
    for port in ports:
        entry = {"port": port, "baud": args.baud, "id": None, "ok": False,
                 "error": None}
        t = _make_transport(port, args.baud)
        try:
            with t:
                ident = t.query("*IDN?", timeout=args.timeout)
                entry["id"] = ident
                entry["ok"] = ident.startswith("HAMEG")
                if entry["ok"] and scope_port is None:
                    scope_port = port
        except TransportError as exc:
            entry["error"] = f"{type(exc).__name__}: {exc}"
        except Exception as exc:
            entry["error"] = f"{type(exc).__name__}: {exc}"
        results.append(entry)

    out = {"candidate_ports": results, "scope_port": scope_port}
    if args.json:
        _print_json(out)
    else:
        for r in results:
            mark = "SCOPE" if r["port"] == scope_port else "     "
            print(f"{mark} {r['port']:12s} id={r['id']} ok={r['ok']} err={r['error']}")
    return 0 if scope_port else 1


# ---------------------------------------------------------------------------
# inventory
# ---------------------------------------------------------------------------
def cmd_inventory(args) -> int:
    t = _make_transport(args.port, args.baud)
    try:
        with t:
            results = inventory_mod.run_inventory(t, timeout=args.timeout)
    except TransportError as exc:
        print(f"error: {type(exc).__name__}: {exc}", file=sys.stderr)
        return 1

    summary = inventory_mod.inventory_summary(results)
    if args.json:
        _print_json({"summary": summary, "results": results})
    else:
        _print_json(summary)
    return 0


# ---------------------------------------------------------------------------
# capture (explicit action)
# ---------------------------------------------------------------------------
def cmd_capture(args) -> int:
    if not args.confirm:
        print(
            "error: capture is an explicit action that arms a single "
            "acquisition; re-run with --confirm to proceed.",
            file=sys.stderr,
        )
        return 2

    tool = {"name": "hmo2024", "version": __version__,
            "git_commit": git_commit()}

    out_root = args.out_dir or DEFAULT_OUT_ROOT
    records = records_mod.RecordManager(out_root)
    t = _make_transport(args.port, args.baud)
    session = CaptureSession(
        transport=t,
        records=records,
        port=args.port,
        baud=args.baud,
        tool=tool,
        confirm=True,
        timeout=args.timeout,
        channel=args.channel,
    )
    try:
        with t:
            manifest = session.arm_and_wait()
    except (CaptureNotConfirmed, ScopeNotIdle, NoTrigger,
            CaptureInterrupted, CaptureTimeout, CaptureError) as exc:
        print(f"capture failed: {type(exc).__name__}: {exc}", file=sys.stderr)
        return 1
    except TransportError as exc:
        print(f"transport error: {type(exc).__name__}: {exc}", file=sys.stderr)
        return 1

    out = {"capture_id": manifest["capture_id"],
           "capture_dir": session.cap_dir,
           "acquisition": manifest["acquisition"],
           "hold": manifest["hold"]}
    if args.json:
        _print_json(out)
    else:
        _print_json(out)
    return 0


# ---------------------------------------------------------------------------
# export
# ---------------------------------------------------------------------------
def cmd_export(args) -> int:
    records = records_mod.RecordManager(os_dir(args.record))
    cap_dir = args.record
    t = _make_transport(args.port, args.baud)
    try:
        with t:
            manifest = export_mod.export_held(t, records, cap_dir, args.channel)
    except (export_mod.ExportError, TransportError) as exc:
        print(f"export failed: {type(exc).__name__}: {exc}", file=sys.stderr)
        return 1

    out = {"capture_id": manifest["capture_id"],
           "artifacts": manifest["artifacts"],
           "flags": manifest["flags"]}
    if args.json:
        _print_json(out)
    else:
        _print_json(out)
    return 0


def os_dir(path: str) -> str:
    import os

    return os.path.dirname(os.path.abspath(path)) if path else "."


# ---------------------------------------------------------------------------
# verify / analyze (offline)
# ---------------------------------------------------------------------------
def cmd_verify(args) -> int:
    try:
        report = analyze_mod.verify_record(args.record)
    except analyze_mod.AnalyzeError as exc:
        print(f"verify failed: {exc}", file=sys.stderr)
        return 1

    if args.json:
        _print_json(report)
    else:
        for c in report["checks"]:
            mark = "PASS" if c["ok"] else "FAIL"
            print(f"[{mark}] {c['check']}  {c.get('detail','')}")
        print(f"overall: {'PASS' if report['passed'] else 'FAIL'} "
              f"({report['artifact_count']} artifacts)")

    # Optional measurement when a scale is supplied on the CLI.
    if args.scale is not None:
        records = records_mod.RecordManager(os_dir(args.record))
        manifest = records.load_manifest(args.record)
        raw = _find_waveform_raw(args.record, manifest)
        if raw is not None:
            samples = _parse_samples(raw)
            m = analyze_mod.measure(samples, scale=args.scale)
            _print_json({"measurements": m})

    return 0 if report["passed"] else 1


def _find_waveform_raw(cap_dir: str, manifest: Dict) -> Optional[str]:
    import os

    for a in manifest.get("artifacts", []):
        if a.get("kind") == "waveform_ascii":
            p = os.path.join(cap_dir, a["rel_path"])
            if os.path.isfile(p):
                with open(p, "r", encoding="ascii") as f:
                    return f.read()
    return None


def _parse_samples(raw: str):
    import scpi

    return scpi.parse_float_list(raw)


# ---------------------------------------------------------------------------
# parser
# ---------------------------------------------------------------------------
def build_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        prog="hmo_cli.py",
        description="HAMEG HMO2024 measurement CLI (software/tooling only).",
    )
    p.add_argument("--version", action="version", version=tool_version())

    sub = p.add_subparsers(dest="command", required=True)

    def add_port(sp, required=True):
        sp.add_argument("--port", default="COM3", help="serial port (default COM3)")
        sp.add_argument("--baud", type=int, default=115200)
        sp.add_argument("--timeout", type=float, default=3.0)

    # discover
    sp = sub.add_parser("discover", help="probe candidate ports for the scope")
    sp.add_argument("--ports", help="comma-separated ports; omit to auto-enumerate")
    sp.add_argument("--baud", type=int, default=115200)
    sp.add_argument("--timeout", type=float, default=1.0)
    sp.add_argument("--json", action="store_true")
    sp.set_defaults(func=cmd_discover)

    # inventory
    sp = sub.add_parser("inventory", help="read-only instrument state")
    add_port(sp, required=False)
    sp.add_argument("--json", action="store_true")
    sp.set_defaults(func=cmd_inventory)

    # capture
    sp = sub.add_parser("capture", help="bounded single acquisition (explicit action)")
    add_port(sp, required=False)
    sp.add_argument("--confirm", action="store_true",
                    help="required: acknowledge arming a single acquisition")
    sp.add_argument("--channel", type=int, default=1)
    sp.add_argument("--out-dir", default=DEFAULT_OUT_ROOT)
    sp.add_argument("--json", action="store_true")
    sp.set_defaults(func=cmd_capture)

    # export
    sp = sub.add_parser("export", help="export a held record without rearming")
    add_port(sp, required=False)
    sp.add_argument("--record", required=True, help="held capture directory")
    sp.add_argument("--channel", type=int, default=1)
    sp.add_argument("--json", action="store_true")
    sp.set_defaults(func=cmd_export)

    # verify / analyze
    sp = sub.add_parser("verify", aliases=["analyze"],
                        help="offline integrity checks and analysis")
    sp.add_argument("--record", required=True, help="held capture directory")
    sp.add_argument("--scale", type=float, default=None,
                    help="vertical volts/div for measurements (optional)")
    sp.add_argument("--json", action="store_true")
    sp.set_defaults(func=cmd_verify)

    return p


def main(argv: Optional[List[str]] = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        return args.func(args)
    except KeyboardInterrupt:
        print("interrupted", file=sys.stderr)
        return 130


if __name__ == "__main__":
    sys.exit(main())
