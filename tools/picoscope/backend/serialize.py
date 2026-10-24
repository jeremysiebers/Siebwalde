"""Save/load a :class:`Capture`: raw samples in ``.npz`` + sidecar JSON.

Format contract:

* raw samples are written to ``<base>.npz`` as NumPy arrays
  (``analog_<CH>`` float64 volts, ``digital_<LINE>`` uint8 0/1);
* all metadata is written to ``<base>.json`` (a self-describing sidecar);
* timestamps are UTC ISO 8601 strings, sample intervals are integer
  nanoseconds, and sample counts / trigger position are integer sample indices
  — i.e. no ambiguous units;
* ``load_capture(save_capture(c))`` reconstructs a :class:`Capture` that is
  ``equals()``-equal to the original (round-trip equality).
"""

from __future__ import annotations

import json
import os

import numpy as np

from .model import (
    AnalogChannelConfig,
    Capture,
    DeviceIdentity,
    DigitalPortConfig,
    TriggerConfig,
)

FORMAT_VERSION = 1


def _strip_ext(path: str) -> str:
    """Normalize a save/load path to its extension-less base form."""
    path = os.fspath(path)
    for ext in (".npz", ".json"):
        if path.endswith(ext):
            return path[: -len(ext)]
    return path


def _device_identity_to_dict(di: DeviceIdentity) -> dict:
    return {
        "serial": di.serial,
        "model": di.model,
        "variant": di.variant,
        "driver_version": di.driver_version,
        "backend": di.backend,
        "extra": dict(di.extra),
    }


def _device_identity_from_dict(d: dict) -> DeviceIdentity:
    return DeviceIdentity(
        serial=d["serial"],
        model=d["model"],
        variant=d["variant"],
        driver_version=d["driver_version"],
        backend=d["backend"],
        extra=dict(d.get("extra", {})),
    )


def _analog_config_to_dict(c: AnalogChannelConfig) -> dict:
    return {
        "channel": c.channel,
        "enabled": c.enabled,
        "range_v": c.range_v,
        "coupling": c.coupling,
    }


def _analog_config_from_dict(d: dict) -> AnalogChannelConfig:
    return AnalogChannelConfig(
        channel=d["channel"],
        enabled=d["enabled"],
        range_v=d["range_v"],
        coupling=d["coupling"],
    )


def _digital_config_to_dict(c: DigitalPortConfig) -> dict:
    return {
        "port": c.port,
        "enabled": c.enabled,
        "threshold_mv": c.threshold_mv,
    }


def _digital_config_from_dict(d: dict) -> DigitalPortConfig:
    return DigitalPortConfig(
        port=d["port"],
        enabled=d["enabled"],
        threshold_mv=d["threshold_mv"],
    )


def _trigger_to_dict(t: TriggerConfig) -> dict:
    return {
        "source": t.source,
        "threshold": t.threshold,
        "edge": t.edge,
    }


def _trigger_from_dict(d: dict) -> TriggerConfig:
    return TriggerConfig(
        source=d["source"],
        threshold=d["threshold"],
        edge=d["edge"],
    )


def _meta_to_dict(capture: Capture) -> dict:
    analog_keys = sorted(capture.analog_samples)
    digital_keys = sorted(capture.digital_samples)
    samples_desc = {
        "analog": {
            key: {
                "dtype": str(capture.analog_samples[key].dtype),
                "shape": list(capture.analog_samples[key].shape),
            }
            for key in analog_keys
        },
        "digital": {
            key: {
                "dtype": str(capture.digital_samples[key].dtype),
                "shape": list(capture.digital_samples[key].shape),
            }
            for key in digital_keys
        },
    }
    return {
        "format_version": FORMAT_VERSION,
        "capture_id": capture.capture_id,
        "timestamp": capture.timestamp,
        "device_identity": _device_identity_to_dict(capture.device_identity),
        "analog_channels": [_analog_config_to_dict(c) for c in capture.analog_channels],
        "digital_ports": [_digital_config_to_dict(p) for p in capture.digital_ports],
        "sample_interval_ns": capture.sample_interval_ns,
        "sample_count": capture.sample_count,
        "trigger": None if capture.trigger is None else _trigger_to_dict(capture.trigger),
        "trigger_position": capture.trigger_position,
        "trigger_status": capture.trigger_status,
        "trigger_index": capture.trigger_index,
        "channel_mapping": capture.channel_mapping,
        "acquisition_status": capture.acquisition_status,
        "errors": list(capture.errors),
        "warnings": list(capture.warnings),
        "backend_version": capture.backend_version,
        "software_version": capture.software_version,
        "provenance": capture.provenance,
        "samples": samples_desc,
    }


def save_capture(capture: Capture, path: str) -> str:
    """Persist ``capture`` to ``<base>.npz`` + ``<base>.json``. Returns base path.

    ``path`` may be a base path, a ``.npz`` path or a ``.json`` path; the
    extension is normalized away and both artifacts are written.
    """
    base = _strip_ext(path)
    npz_path = base + ".npz"
    json_path = base + ".json"

    npz_arrays = {}
    for key, arr in capture.analog_samples.items():
        npz_arrays[f"analog_{key}"] = np.asarray(arr)
    for key, arr in capture.digital_samples.items():
        npz_arrays[f"digital_{key}"] = np.asarray(arr)

    meta = _meta_to_dict(capture)

    parent = os.path.dirname(base)
    if parent and not os.path.isdir(parent):
        os.makedirs(parent, exist_ok=True)

    np.savez_compressed(npz_path, **npz_arrays)
    with open(json_path, "w", encoding="utf-8") as fh:
        json.dump(meta, fh, indent=2, sort_keys=True)
        fh.write("\n")
    return base


def load_capture(path: str) -> Capture:
    """Load a :class:`Capture` previously written by :func:`save_capture`."""
    base = _strip_ext(path)
    npz_path = base + ".npz"
    json_path = base + ".json"

    with open(json_path, "r", encoding="utf-8") as fh:
        meta = json.load(fh)

    if meta.get("format_version") != FORMAT_VERSION:
        raise ValueError(
            f"Unsupported capture format version {meta.get('format_version')!r}"
        )

    with np.load(npz_path) as data:
        analog_samples = {
            key.replace("analog_", "", 1): data[f"analog_{key}"]
            for key in meta["samples"]["analog"]
        }
        digital_samples = {
            key.replace("digital_", "", 1): data[f"digital_{key}"]
            for key in meta["samples"]["digital"]
        }

    trigger = None if meta["trigger"] is None else _trigger_from_dict(meta["trigger"])
    return Capture(
        capture_id=meta["capture_id"],
        timestamp=meta["timestamp"],
        device_identity=_device_identity_from_dict(meta["device_identity"]),
        analog_channels=[_analog_config_from_dict(c) for c in meta["analog_channels"]],
        digital_ports=[_digital_config_from_dict(p) for p in meta["digital_ports"]],
        sample_interval_ns=meta["sample_interval_ns"],
        sample_count=meta["sample_count"],
        trigger=trigger,
        trigger_position=meta["trigger_position"],
        trigger_status=meta.get("trigger_status"),
        trigger_index=meta.get("trigger_index"),
        channel_mapping=meta["channel_mapping"],
        acquisition_status=meta["acquisition_status"],
        errors=meta["errors"],
        warnings=meta["warnings"],
        backend_version=meta["backend_version"],
        software_version=meta["software_version"],
        provenance=meta["provenance"],
        analog_samples=analog_samples,
        digital_samples=digital_samples,
    )
