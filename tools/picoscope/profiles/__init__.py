"""Reusable Siebwalde capture profiles (config-only; see siebwalde.py)."""

from __future__ import annotations

from .siebwalde import (  # noqa: F401
    DIGITAL_THRESHOLD_MV,
    REF_BAUD,
    UART_SAMPLE_INTERVAL_NS,
    CaptureProfile,
    ChannelProposal,
    PROFILES,
    all_profiles,
    get_profile,
)

__all__ = [
    "CaptureProfile",
    "ChannelProposal",
    "PROFILES",
    "get_profile",
    "all_profiles",
    "REF_BAUD",
    "UART_SAMPLE_INTERVAL_NS",
    "DIGITAL_THRESHOLD_MV",
]
