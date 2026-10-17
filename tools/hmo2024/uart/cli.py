"""Command-line entry point for the Siebwalde 9-bit UART decoder.

Decodes a ``pod1.raw`` POD digital capture (with its shared ``ch1.header.raw``
time base and optional ``ch1.raw`` analog channel) into request/response UART
frames and Modbus-RTU telegrams.

Software/tooling only.  Example::

    python -m uart.cli \\
        tools/hmo2024/captures/REF-AMP01-003-DIGITAL/pod1.raw \\
        tools/hmo2024/captures/REF-AMP01-003-DIGITAL/ch1.header.raw \\
        --ch1 tools/hmo2024/captures/REF-AMP01-003-DIGITAL/ch1.raw \\
        --json
"""

from __future__ import annotations

import argparse
import json
import sys
from typing import List, Optional

from . import decoder


def _to_jsonable(obj):
    if isinstance(obj, decoder.Telegram):
        return {
            "direction": obj.direction,
            "frames": len(obj.frames),
            "start_t_s": obj.frames[0].t_start_s if obj.frames else None,
            "data_bytes": obj.data_bytes,
            "bytes_hex": " ".join(f"{b:02X}" for b in obj.data_bytes),
            "ninth_bits": "".join(str(f.ninth) for f in obj.frames) if obj.frames and obj.frames[0].ninth is not None else None,
            "address": obj.address,
            "function": obj.function,
            "function_name": obj.function_name,
            "register_address": obj.register_address,
            "quantity": obj.quantity,
            "byte_count": obj.byte_count,
            "values": obj.values,
            "crc_received": list(obj.crc_received),
            "crc_status": obj.crc_status,
            "crc_siebwalde_ok": obj.crc_siebwalde_ok,
            "crc_standard_ok": obj.crc_standard_ok,
            "starts_with_address": obj.starts_with_address,
            "truncated_by_capture": obj.truncated_by_capture,
            "note": obj.note,
        }
    if isinstance(obj, decoder.Frame):
        return {
            "direction": obj.direction,
            "start_index": obj.start_index,
            "t_start_s": obj.t_start_s,
            "data": obj.data,
            "ninth": obj.ninth,
            "stop_ok": obj.stop_ok,
        }
    if isinstance(obj, (list, tuple)):
        return [_to_jsonable(x) for x in obj]
    if isinstance(obj, dict):
        return {k: _to_jsonable(v) for k, v in obj.items()}
    return obj


def build_arg_parser() -> argparse.ArgumentParser:
    p = argparse.ArgumentParser(
        prog="uart-cli",
        description="Decode a Siebwalde POD digital capture into 9-bit UART "
                    "frames and Modbus-RTU telegrams (software/tooling only).",
    )
    p.add_argument("pod", help="path to pod1.raw (comma-separated 8-bit POD values)")
    p.add_argument("header", help="path to ch1.header.raw (shared time base)")
    p.add_argument("--ch1", help="optional path to ch1.raw for same-acquisition "
                                 "timing correlation")
    p.add_argument("--json", action="store_true", help="emit JSON instead of text")
    return p


def main(argv: Optional[List[str]] = None) -> int:
    args = build_arg_parser().parse_args(argv)
    try:
        result = decoder.decode_pod(args.pod, args.header, ch1_path=args.ch1)
    except decoder.DecoderError as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2

    if args.json:
        # Drop full frame lists for a compact JSON summary; keep telegrams.
        out = {k: v for k, v in result.items() if k != "directions"}
        out["directions"] = {}
        for direction, d in result["directions"].items():
            out["directions"][direction] = {
                "bit": d["bit"],
                "frame_format": d["frame_format"],
                "frame_count": d["frame_count"],
                "bit_period": d["bit_period"],
                "frame_spacing_period_samples": d.get("frame_spacing_period_samples"),
                "telegrams": d["telegrams"],
            }
        json.dump(_to_jsonable(out), sys.stdout, indent=2, default=str)
        sys.stdout.write("\n")
        return 0

    print(decoder.format_report(result))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
