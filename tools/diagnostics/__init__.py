"""Siebwalde Diagnostics package (K4-B) — offline diagnostic core.

Exposes the typed, offline, MCP-independent core that analyses saved textual
traces from the K3 fail-closed flash driver. No sockets, serial ports, or
hardware connections are ever opened.
"""

from .core import (  # noqa: F401
    ACKNOWLEDGED,
    EVIDENCE_LEVELS,
    FLASH_STATES,
    OK_CODES,
    NOK_CODES,
    PHYSICALLY_OBSERVED,
    REQUESTED,
    SENT,
    TIMEOUT_CODES,
    UNKNOWN,
    VERIFIED,
    FlashState,
    analyze_firmware_session,
    analyze_udp_trace,
    capabilities,
    classify_feedback_frame,
    compare_protocol_traces,
)

from . import observe  # noqa: F401

__all__ = [
    "ACKNOWLEDGED",
    "EVIDENCE_LEVELS",
    "FLASH_STATES",
    "OK_CODES",
    "NOK_CODES",
    "PHYSICALLY_OBSERVED",
    "REQUESTED",
    "SENT",
    "TIMEOUT_CODES",
    "UNKNOWN",
    "VERIFIED",
    "FlashState",
    "analyze_firmware_session",
    "analyze_udp_trace",
    "capabilities",
    "classify_feedback_frame",
    "compare_protocol_traces",
]
