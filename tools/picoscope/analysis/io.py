"""Capture extraction + deterministic JSON export for the analysis layer.

The analysis layer consumes raw arrays; these helpers bridge the Layer A
``Capture`` object (loaded from the ``.npz`` + sidecar JSON evidence pair) to the
array inputs, and export analysis results as deterministic JSON. They never
mutate the source capture or its on-disk evidence.
"""

from __future__ import annotations

import json
import os
from typing import Any

import numpy as np

from .errors import MissingChannel


def extract_digital_line(capture: Any, line: str) -> np.ndarray:
    """Return the 0/1 sample array for digital ``line`` (e.g. ``"D0"``).

    ``capture`` is a Layer A ``Capture`` (or any object exposing a
    ``digital_samples`` dict). Raises :class:`MissingChannel` when the line is
    absent. The returned array is a read-only view of the capture's data -- it is
    never modified in place.
    """
    samples = getattr(capture, "digital_samples", None)
    if samples is None or line not in samples:
        raise MissingChannel(f"digital line {line!r} is not present in the capture")
    arr = np.asarray(samples[line])
    return arr


def extract_analog_channel(capture: Any, channel: str) -> np.ndarray:
    """Return the volt sample array for analog ``channel`` (e.g. ``"A"``).

    Raises :class:`MissingChannel` when the channel is absent. The returned
    array is a read-only view of the capture's data.
    """
    samples = getattr(capture, "analog_samples", None)
    if samples is None or channel not in samples:
        raise MissingChannel(f"analog channel {channel!r} is not present in the capture")
    return np.asarray(samples[channel])


def jsonable(obj: Any) -> Any:
    """Recursively convert a result into JSON-native types (NumPy scalars etc.)."""
    if isinstance(obj, np.generic):
        return obj.item()
    if isinstance(obj, np.ndarray):
        return obj.tolist()
    if isinstance(obj, dict):
        return {str(k): jsonable(v) for k, v in obj.items()}
    if isinstance(obj, (list, tuple)):
        return [jsonable(v) for v in obj]
    if isinstance(obj, float):
        # Reject NaN/Inf (not valid JSON) by turning them into None.
        return obj if np.isfinite(obj) else None
    return obj


def export_analysis_json(payload: Any, path: str) -> str:
    """Write ``payload`` as deterministic JSON (sorted keys, trailing newline).

    Returns the written path. The parent directory is created if needed.
    """
    clean = jsonable(payload)
    parent = os.path.dirname(os.path.abspath(path))
    if parent and not os.path.isdir(parent):
        os.makedirs(parent, exist_ok=True)
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(clean, fh, indent=2, sort_keys=True)
        fh.write("\n")
    return path


def load_analysis_json(path: str) -> Any:
    """Read a JSON file previously written by :func:`export_analysis_json`."""
    with open(path, "r", encoding="utf-8") as fh:
        return json.load(fh)
