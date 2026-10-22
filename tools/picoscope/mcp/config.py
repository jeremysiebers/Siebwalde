"""Finite, configurable limits and server configuration for the MCP layer.

Every acquisition and every response is bounded. Limits are configurable via
environment variables (with the documented defaults below) so the Product Owner
or an operator can tighten or relax them without editing code. Invalid input is
rejected *before* any backend/SDK call.
"""

from __future__ import annotations

import os
from dataclasses import dataclass, field, fields


def _env_int(name: str, default: int) -> int:
    raw = os.environ.get(name)
    if raw is None:
        return default
    try:
        return int(raw)
    except ValueError:
        return default


def _env_float(name: str, default: float) -> float:
    raw = os.environ.get(name)
    if raw is None:
        return default
    try:
        return float(raw)
    except ValueError:
        return default


@dataclass(frozen=True)
class ServerConfig:
    """Bounded configuration for one MCP server process.

    The limits below are *finite* and *configurable*: they enforce the
    measurement-only, bounded-acquisition contract of the MCP layer. Sensible
    defaults are chosen for the 3406D MSO (4 analog + 16 digital) and the
    simulator; they are not hardware-derived guarantees.
    """

    # -- acquisition limits -------------------------------------------------
    #: Maximum total samples per capture (pre + post).
    max_sample_count: int = 1_000_000
    #: Maximum enabled analog channels (A..D).
    max_analog_channels: int = 4
    #: Maximum enabled digital ports (0..1).
    max_digital_ports: int = 2
    #: Maximum capture window duration (seconds).
    max_duration_s: float = 10.0
    #: Maximum trigger readiness wait (milliseconds).
    max_trigger_wait_ms: int = 60_000
    #: Default trigger readiness wait (milliseconds).
    default_trigger_wait_ms: int = 5_000
    #: Wall-clock budget for one whole acquisition (seconds).
    total_acquisition_timeout_s: float = 60.0
    #: Estimated in-memory budget for the raw sample arrays (bytes).
    max_memory_bytes: int = 64 * 1024 * 1024
    #: Budget for a saved capture artifact pair (npz + json) on disk (bytes).
    max_capture_file_bytes: int = 64 * 1024 * 1024
    #: Maximum serialized size of any MCP tool response (bytes).
    max_metadata_response_bytes: int = 256 * 1024

    # -- safety / ownership -------------------------------------------------
    #: Real ``ps3000a`` backend requires an explicit operator gate (default closed).
    allow_real_backend: bool = False
    #: Default evidence directory for captures (dedicated, server-owned).
    evidence_dir: str = field(
        default_factory=lambda: os.environ.get(
            "PICOSCOPE_EVIDENCE_DIR",
            os.path.abspath(
                os.path.join(os.path.dirname(__file__), "..", "evidence", "mcp")
            ),
        )
    )
    #: Simulator-only fault injection (test-only; see service.py). Disabled by
    #: default and never enabled in production.
    test_fault: str | None = field(default_factory=lambda: os.environ.get("PICOSCOPE_MCP_TEST_FAULT"))

    @classmethod
    def from_env(cls) -> "ServerConfig":
        """Build a config from environment overrides + documented defaults."""
        return cls(
            max_sample_count=_env_int("PICOSCOPE_MCP_MAX_SAMPLE_COUNT", cls.max_sample_count),
            max_analog_channels=_env_int("PICOSCOPE_MCP_MAX_ANALOG_CHANNELS", cls.max_analog_channels),
            max_digital_ports=_env_int("PICOSCOPE_MCP_MAX_DIGITAL_PORTS", cls.max_digital_ports),
            max_duration_s=_env_float("PICOSCOPE_MCP_MAX_DURATION_S", cls.max_duration_s),
            max_trigger_wait_ms=_env_int("PICOSCOPE_MCP_MAX_TRIGGER_WAIT_MS", cls.max_trigger_wait_ms),
            default_trigger_wait_ms=_env_int(
                "PICOSCOPE_MCP_DEFAULT_TRIGGER_WAIT_MS", cls.default_trigger_wait_ms
            ),
            total_acquisition_timeout_s=_env_float(
                "PICOSCOPE_MCP_TOTAL_ACQUISITION_TIMEOUT_S", cls.total_acquisition_timeout_s
            ),
            max_memory_bytes=_env_int("PICOSCOPE_MCP_MAX_MEMORY_BYTES", cls.max_memory_bytes),
            max_capture_file_bytes=_env_int(
                "PICOSCOPE_MCP_MAX_CAPTURE_FILE_BYTES", cls.max_capture_file_bytes
            ),
            max_metadata_response_bytes=_env_int(
                "PICOSCOPE_MCP_MAX_METADATA_RESPONSE_BYTES", cls.max_metadata_response_bytes
            ),
            allow_real_backend=os.environ.get("PICOSCOPE_MCP_ALLOW_REAL", "0") in ("1", "true", "yes"),
            evidence_dir=os.environ.get(
                "PICOSCOPE_EVIDENCE_DIR",
                os.path.abspath(
                    os.path.join(os.path.dirname(__file__), "..", "evidence", "mcp")
                ),
            ),
            test_fault=os.environ.get("PICOSCOPE_MCP_TEST_FAULT"),
        )

    def limit_doc(self) -> dict:
        """Compact, human-readable limit summary (for ``scope_info`` and docs)."""
        return {
            "max_sample_count": self.max_sample_count,
            "max_analog_channels": self.max_analog_channels,
            "max_digital_ports": self.max_digital_ports,
            "max_duration_s": self.max_duration_s,
            "max_trigger_wait_ms": self.max_trigger_wait_ms,
            "total_acquisition_timeout_s": self.total_acquisition_timeout_s,
            "max_memory_bytes": self.max_memory_bytes,
            "max_capture_file_bytes": self.max_capture_file_bytes,
            "max_metadata_response_bytes": self.max_metadata_response_bytes,
        }

    def field_names(self) -> list[str]:
        return [f.name for f in fields(self)]
