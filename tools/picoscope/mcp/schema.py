"""Strict Pydantic request schemas for the MCP tool surface.

The schemas validate *shape, types and enums* before any backend/SDK call.
Domain rules (trigger source must be enabled, at least one channel enabled, and
so on) are re-validated by the backend's :class:`AcquisitionConfig` once the
profile is mapped onto it; finite resource limits are enforced by
:class:`ScopeService` against :class:`ServerConfig`.
"""

from __future__ import annotations

from typing import Literal, Optional

from pydantic import BaseModel, Field

#: Recognised backend names (aliases match ``backend/api.py``).
BackendName = Literal["simulator", "ps3000a", "picosdk", "real"]

#: Analog channel letters for the 3406D MSO.
AnalogChannel = Literal["A", "B", "C", "D"]

#: Digital ports: 0 -> D0..D7, 1 -> D8..D15.
DigitalPort = Literal[0, 1]

#: Analog couplings.
Coupling = Literal["DC", "AC"]

#: Trigger edges.
TriggerEdge = Literal["rising", "falling", "either"]


class AnalogChannelProfile(BaseModel):
    """One analog channel request (channel A..D)."""

    channel: AnalogChannel
    enabled: bool = True
    range_v: float = Field(gt=0, description="Full-scale range in volts")
    coupling: Coupling = "DC"


class DigitalPortProfile(BaseModel):
    """One digital port request (port 0 = D0..D7, 1 = D8..D15)."""

    port: DigitalPort
    enabled: bool = True
    threshold_mv: float = Field(
        gt=0, le=5000, description="Logic threshold in millivolts (0, 5000]"
    )


class TriggerProfile(BaseModel):
    """Trigger condition + pre/post sample split."""

    source: str = Field(description="A..D, D0..D15, or EXTERNAL")
    threshold: float = Field(description="Trigger threshold in volts")
    edge: TriggerEdge
    pre_samples: int = Field(ge=0, description="Pre-trigger sample count")
    post_samples: int = Field(gt=0, description="Post-trigger sample count")


class AcquisitionProfile(BaseModel):
    """A bounded block-capture request.

    ``pre_samples`` + ``post_samples`` define the total sample count; together
    with the resolved ``sample_interval_ns`` they define the capture window
    duration (enforced against ``max_duration_s``).
    """

    backend: BackendName = "simulator"
    device_id: Optional[str] = None
    analog_channels: list[AnalogChannelProfile] = Field(default_factory=list)
    digital_ports: list[DigitalPortProfile] = Field(default_factory=list)
    trigger: Optional[TriggerProfile] = None
    pre_samples: int = Field(0, ge=0)
    post_samples: int = Field(1000, gt=0)
    timebase: int = Field(10, ge=0)
    trigger_wait_ms: int = Field(5000, ge=0)
    notes: Optional[str] = Field(None, max_length=2048)


class AnalysisRequest(BaseModel):
    """A read-only offline analysis request over an existing capture.

    ``analyzer`` is a strict allowlist; ``settings`` is validated per analyzer
    inside the service (no arbitrary keys, no client-supplied paths, no shell).
    """

    capture_id: str = Field(description="32-char hex capture id")
    analyzer: Literal["uart", "pwm_timing"] = Field(
        description="Allowlisted analyzer: 'uart' or 'pwm_timing'"
    )
    settings: dict = Field(default_factory=dict, description="Analyzer settings (validated)")
