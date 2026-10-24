"""Siebwalde PicoScope offline signal-analysis library (Layer B).

Pure-Python, NumPy-only, standalone. No MCP dependency, no hardware, no
PicoSDK. Consumes raw captured sample arrays (a NumPy bool/int array per digital
line, or a float64 volt array per analog channel) plus the integer
``sample_interval_ns``, and produces decoders / measurements:

* :mod:`tools.picoscope.analysis.uart`   -- 8-bit / 9-bit UART frame decoder
* :mod:`tools.picoscope.analysis.timing` -- PWM + edge/timing analysis
* :mod:`tools.picoscope.analysis.io`     -- capture extraction + JSON export
* :mod:`tools.picoscope.analysis.fixtures` -- deterministic synthetic fixtures

All timing is derived from ``sample_interval_ns`` and is therefore
sample-resolution limited; it is NOT calibrated physical accuracy. Results are
deterministic: identical inputs produce identical outputs.
"""

from __future__ import annotations

from . import fixtures, io, timing, uart  # noqa: F401
from .errors import AnalysisError, InvalidAnalysisConfig  # noqa: F401
from .timing import analyze_pwm, cross_channel_delta, measure_edges  # noqa: F401
from .uart import UartConfig, UartFrame, decode_uart, decode_uart_9bit, decode_uart_8bit, extract_edges  # noqa: F401

__all__ = [
    "AnalysisError",
    "InvalidAnalysisConfig",
    "UartConfig",
    "UartFrame",
    "decode_uart",
    "decode_uart_8bit",
    "decode_uart_9bit",
    "analyze_pwm",
    "measure_edges",
    "cross_channel_delta",
    "extract_edges",
    "fixtures",
    "io",
    "timing",
    "uart",
]
