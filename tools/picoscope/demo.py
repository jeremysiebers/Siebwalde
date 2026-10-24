"""Standalone offline demo: simulated PicoScope capture + save/reload round-trip.

Requires only NumPy. No hardware, no drivers, no MCP, no OpenCode, no
Siebwalde C#/firmware.

Invocation (either works, from the repository root)::

    python -m tools.picoscope.demo
    python tools/picoscope/demo.py
"""

import os
import shutil
import sys
import tempfile

if __package__ in (None, ""):
    # Running as a plain script: make the repository root importable.
    _repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
    if _repo_root not in sys.path:
        sys.path.insert(0, _repo_root)

from tools.picoscope.backend.api import discover_devices, load_capture, open_device  # noqa: E402


def _print_summary(capture) -> None:
    print("=== capture metadata ===")
    summary = capture.summary()
    print(f"capture_id        : {summary['capture_id']}")
    print(f"timestamp         : {summary['timestamp']}")
    print(f"device            : {summary['device']['model']} "
          f"(serial {summary['device']['serial']}, backend {summary['device']['backend']})")
    print(f"sample_interval_ns: {summary['sample_interval_ns']}")
    print(f"sample_count      : {summary['sample_count']}")
    for ch in summary["analog_channels"]:
        print(f"analog {ch['channel']}         : enabled={ch['enabled']} "
              f"range_v={ch['range_v']} coupling={ch['coupling']}")
    for p in summary["digital_ports"]:
        print(f"digital port {p['port']}    : enabled={p['enabled']} "
              f"threshold_mv={p['threshold_mv']}")
    if summary["trigger"] is not None:
        t = summary["trigger"]
        print(f"trigger           : source={t['source']} threshold={t['threshold']}V edge={t['edge']}")
    print(f"trigger_position  : {summary['trigger_position']}")
    print(f"acquisition_status: {summary['acquisition_status']}")
    print(f"analog lines      : {sorted(capture.analog_samples)}")
    print(f"digital lines     : {sorted(capture.digital_samples)}")


def main() -> int:
    devices = discover_devices(backend="simulator")
    print(f"discovered {len(devices)} simulated device(s)")
    for d in devices:
        print(f"  - {d['serial']}: {d['model']} ({d['variant']}) driver={d['driver_version']}")

    device_id = devices[0]["serial"]
    tmpdir = tempfile.mkdtemp(prefix="picoscope-demo-")
    try:
        with open_device(device_id, backend="simulator") as scope:
            scope.configure_analog_channel("A", True, 5.0, "DC")
            scope.configure_analog_channel("B", True, 2.0, "DC")
            scope.configure_digital_port(0, True, 1000)  # D0..D7
            scope.configure_digital_port(1, True, 1200)  # D8..D15
            scope.configure_trigger("A", 0.0, "rising", pre_samples=2000, post_samples=6000)

            capture = scope.capture_block()
            path = os.path.join(tmpdir, "demo_capture")
            scope.save_capture(path)

            reloaded = load_capture(path)
            assert reloaded.equals(capture), "round-trip equality failed"

            _print_summary(reloaded)
            print(f"saved/loaded from: {path}.npz + {path}.json")

        print("device closed; ownership released (deterministic cleanup)")
    finally:
        shutil.rmtree(tmpdir, ignore_errors=True)

    print("demo complete (exit 0)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
