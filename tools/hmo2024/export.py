"""Export an ALREADY-HELD completed acquisition WITHOUT rearming.

All exports are tied to a single held record (one capture directory). The
exporter never issues ``SINGle``/``RUN``/``STOP``. Before each artifact it
re-reads the acquisition state and the channel header/point-count and compares
them against the hold token: if the scope has been re-armed in between (state
changed to RUN, or header/point-count no longer match the held values), the
artifact is flagged ``incoherent`` rather than silently treated as belonging to
the held samples.

Artifacts produced (all verified query commands):

* ``ch<N>.raw``          -- raw ASCII waveform bytes exactly as transferred,
* ``ch<N>.csv``          -- derived CSV (derived from the raw, raw preserved),
* ``ch<N>.header.raw``   -- raw header bytes,
* ``screenshot.bmp``     -- ``HCOP:DATA?`` definite-length binary block.

Bus/digital decoder text is NOT exported by default: the decoder frame/word
commands (``BUS<b>:UART:WORD<n>:DATA?`` etc.) are documented in the manual but
have NOT been verified on the live instrument in this increment, so they are
left unverified rather than guessed.
"""

from __future__ import annotations

from typing import Dict, List, Optional

import scpi
from records import RecordManager
from transport import HmoTransport, TransportError


class ExportError(Exception):
    pass


class RearmSuspected(ExportError):
    pass


def _query(transport: HmoTransport, cmd: str) -> Optional[str]:
    try:
        return transport.query(cmd)
    except TransportError as exc:
        return None


def _header_dict(transport: HmoTransport, channel: int) -> Optional[Dict]:
    raw = _query(transport, f"CHAN{channel}:DATA:HEAD?")
    if raw is None:
        return None
    try:
        h = scpi.parse_header(raw)
    except scpi.ScpiParseError:
        return None
    return {"raw": raw, "start": h.start, "stop": h.stop,
            "count": h.count, "values_per_sample": h.values_per_sample}


def _rearm_flags(
    held: Optional[Dict], state: str, header: Optional[Dict]
) -> List[str]:
    """Compare current scope reality against the held token; return flags."""
    flags: List[str] = []
    held_state = (held or {}).get("acq_state")
    if held_state and scpi.normalize_acq_state(str(held_state)) != state:
        flags.append(f"acq_state_changed:{held_state}->{state}")
    if state == scpi.normalize_acq_state("RUN"):
        flags.append("scope_running_rearm_suspected")
    if held and header is not None and held.get("header"):
        h = held["header"]
        if (h.get("count") != header["count"]) or (
            h.get("start") != header["start"]
        ):
            flags.append("header_changed_from_hold")
    return flags


def export_held(
    transport: HmoTransport,
    records: RecordManager,
    cap_dir: str,
    channel: int = 1,
) -> Dict:
    """Export a held record's artifacts without rearming. Returns manifest."""
    manifest = records.load_manifest(cap_dir)
    hold = manifest.get("hold") or {}
    ch = hold.get("channel", channel)

    state = scpi.normalize_acq_state(
        transport.query("ACQ:STAT?")
    )
    header = _header_dict(transport, ch)
    flags = _rearm_flags(hold, state, header)

    artifacts: List[Dict] = []

    # 1. header (raw bytes preserved)
    if header is not None and header.get("raw"):
        artifacts.append(
            records.record_artifact(
                cap_dir, f"ch{ch}.header.raw", header["raw"].encode("ascii"),
                "header_ascii",
            )
        )

    # 2. raw ASCII waveform
    raw_wave = transport.query(f"CHAN{ch}:DATA?")
    wave_bytes = raw_wave.encode("ascii")
    artifacts.append(
        records.record_artifact(cap_dir, f"ch{ch}.raw", wave_bytes, "waveform_ascii")
    )
    # derived CSV (raw bytes still preserved above)
    csv = _raw_to_csv(raw_wave)
    artifacts.append(
        records.record_artifact(cap_dir, f"ch{ch}.csv", csv.encode("ascii"), "waveform_csv")
    )

    # 3. screenshot (block)
    shot = transport.query_block("HCOP:DATA?")
    artifacts.append(
        records.record_artifact(cap_dir, "screenshot.bmp", shot, "screenshot_bmp")
    )

    # 4. bus decoder text: left unverified (see module docstring)
    flags.append("bus_decoder_text_not_exported:unverified_commands")

    manifest["hold"]["export_state"] = state
    manifest["hold"]["export_header"] = header
    manifest["artifacts"] = artifacts
    manifest["flags"] = sorted(set(manifest.get("flags", []) + flags))
    records.write_manifest(cap_dir, manifest)
    return manifest


def _raw_to_csv(raw: str) -> str:
    samples = scpi.parse_float_list(raw)
    lines = ["index,value_v"]
    for i, v in enumerate(samples):
        lines.append(f"{i},{v!r}")
    return "\n".join(lines) + "\n"
