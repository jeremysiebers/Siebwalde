"""Reusable Siebwalde capture profiles (config only -- NO hardware execution).

Profiles encode intended channel assignments, thresholds, baud, UART mode and
decoder settings for three measurement scenarios. They are **PROPOSALS**: the
channel/line assignments are what is intended to be wired, NOT physical-wiring
proof. The actual wiring must be verified during a controlled hardware session
(see ``docs/PICOSCOPE-PICO-4-HARDWARE-TEST-PROCEDURE.md``).

No profile here drives any device; they are data passed to the MCP capture or
the offline analysis layer.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import List, Optional

#: Sample-interval recommendation for UART capture (see PICO-4 doc §sampling).
UART_SAMPLE_INTERVAL_NS = 16

#: Project reference baud (2.016 Mbaud -> ~496 ns/bit).
REF_BAUD = 2_016_000

#: Digital threshold (mV) proposal for the RS-422/TTL-ish digital pod.
DIGITAL_THRESHOLD_MV = 1500


@dataclass
class ChannelProposal:
    """One intended channel/line assignment (PROPOSAL, not verified wiring)."""

    name: str
    role: str
    note: str


@dataclass
class CaptureProfile:
    """A reusable capture + analysis configuration (no hardware execution)."""

    key: str
    title: str
    description: str
    sample_interval_ns: int
    baud: int
    uart_mode: str
    trigger: dict
    channels: List[ChannelProposal]
    decode: dict
    proposal_note: str

    def to_dict(self) -> dict:
        return {
            "key": self.key,
            "title": self.title,
            "description": self.description,
            "sample_interval_ns": self.sample_interval_ns,
            "baud": self.baud,
            "uart_mode": self.uart_mode,
            "trigger": dict(self.trigger),
            "channels": [
                {"name": c.name, "role": c.role, "note": c.note} for c in self.channels
            ],
            "decode": dict(self.decode),
            "proposal_note": self.proposal_note,
        }


_PROFILE_A = CaptureProfile(
    key="A",
    title="legacy Modbus UART",
    description=(
        "PIC32 master TX (9-bit address-marked request) and PIC18 RX/TX "
        "(8-bit reply). Decode the request in 9-bit mode (ninth bit = address "
        "marker on the first byte) and the reply in 8-bit mode. Measure "
        "response timing and idle gaps."
    ),
    sample_interval_ns=UART_SAMPLE_INTERVAL_NS,
    baud=REF_BAUD,
    uart_mode="9bit",  # request direction; the reply is decoded separately in 8-bit
    trigger={"source": "D6", "threshold": 0.0, "edge": "falling", "pre_samples": 1000, "post_samples": 60000},
    channels=[
        ChannelProposal("D6", "PIC32 master TX (request) 9-bit", "PROPOSAL: assign to the RS-422 request line"),
        ChannelProposal("D7", "PIC18 slave TX (reply) 8-bit", "PROPOSAL: assign to the RS-422 reply line"),
    ],
    decode={
        "request": {"mode": "9bit", "baud": REF_BAUD, "polarity": "idle_high", "stop_bits": 1},
        "reply": {"mode": "8bit", "baud": REF_BAUD, "polarity": "idle_high", "stop_bits": 1},
        "idle_gap_bits": 20.0,
    },
    proposal_note=(
        "D6/D7 are the historical POD channel assignments from the P2-A0 capture "
        "set. PROPOSAL only; the physical RS-422 wiring must be verified."
    ),
)

_PROFILE_B = CaptureProfile(
    key="B",
    title="bootloader transition",
    description=(
        "Capture the 9->8-bit transition: master request (9-bit) then the "
        "8-bit bootloader exchange (0x55-prefixed), plus RESET and AMP_ID "
        "selection lines. Decode each segment with an explicit mode."
    ),
    sample_interval_ns=UART_SAMPLE_INTERVAL_NS,
    baud=REF_BAUD,
    uart_mode="9bit",  # first segment; transition to 8-bit is explicit
    trigger={"source": "D6", "threshold": 0.0, "edge": "falling", "pre_samples": 1000, "post_samples": 100000},
    channels=[
        ChannelProposal("D6", "PIC32 master TX (request) 9->8-bit", "PROPOSAL: request/bootloader TX"),
        ChannelProposal("D7", "PIC18 slave TX (reply/bootloader) 8-bit", "PROPOSAL: reply/bootloader TX"),
        ChannelProposal("D0", "RESET line", "PROPOSAL: amplifier reset observation"),
        ChannelProposal("D1", "AMP_ID selection", "PROPOSAL: backplane/slot select observation"),
    ],
    decode={
        "application": {"mode": "9bit", "baud": REF_BAUD, "polarity": "idle_high", "stop_bits": 1},
        "bootloader": {"mode": "8bit", "baud": REF_BAUD, "polarity": "idle_high", "stop_bits": 1},
        "transition_marker": "explicit (0x55-prefixed bootloader frames; no address byte per frame)",
    },
    proposal_note=(
        "RESET/AMP_ID line assignments are PROPOSALS. Bootloader frames are 8-bit "
        "(PDSEL=0x0 on the master; RX9=0/ADDEN=0 on the PIC18 bootloader)."
    ),
)

_PROFILE_C = CaptureProfile(
    key="C",
    title="amplifier PWM",
    description=(
        "Measure amplifier PWM output frequency/period/duty plus enable/brake "
        "timing. Analog channel for the PWM voltage; digital lines for "
        "enable/brake gating."
    ),
    sample_interval_ns=16,
    baud=REF_BAUD,  # unused for PWM; kept for schema symmetry
    uart_mode="8bit",  # unused for PWM
    trigger={"source": "A", "threshold": 1.0, "edge": "rising", "pre_samples": 1000, "post_samples": 60000},
    channels=[
        ChannelProposal("A", "PWM output voltage (analog)", "PROPOSAL: H-bridge output / PWM node"),
        ChannelProposal("D0", "ENABLE line", "PROPOSAL: H-bridge enable"),
        ChannelProposal("D1", "BRAKE line", "PROPOSAL: H-bridge brake"),
    ],
    decode={
        "domain": "analog",  # PWM measured from the analog channel
        "threshold_v": 1.0,  # proposed threshold for high/low
        "enable_line": "D0",
        "brake_line": "D1",
    },
    proposal_note=(
        "PWM/ENABLE/BRAKE assignments are PROPOSALS. Analog duty is "
        "threshold-dependent and sample-resolution limited; NOT calibrated."
    ),
)

PROFILES = {
    "A": _PROFILE_A,
    "B": _PROFILE_B,
    "C": _PROFILE_C,
}


def get_profile(key: str) -> CaptureProfile:
    """Return a profile by key (``"A"``/``"B"``/``"C"``) or raise ``KeyError``."""
    return PROFILES[key]


def all_profiles() -> dict:
    """Return all profiles as a JSON-serializable dict."""
    return {k: v.to_dict() for k, v in PROFILES.items()}
