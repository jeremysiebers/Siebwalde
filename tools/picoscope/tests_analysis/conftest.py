"""Offline signal-analysis test suite (Layer B, PICO-4).

Tests the standalone ``tools.picoscope.analysis`` library (UART decoder, PWM +
timing) and the ``tools.picoscope.profiles`` config against deterministic
synthetic fixtures. No hardware, no MCP subprocess, no PicoSDK.
"""

from __future__ import annotations

import os
import sys

import pytest

_REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
if _REPO not in sys.path:
    sys.path.insert(0, _REPO)
