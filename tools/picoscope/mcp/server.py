"""FastMCP stdio server for the measurement-only PicoScope tooling (Layer C).

Exposes exactly four allowlisted tools: ``scope_info``,
``scope_capture_block``, ``scope_get_capture_metadata`` and
``scope_export_capture``. No AWG / signal generation, no arbitrary SDK command,
no flash / reset / motion / Siebwalde state.

Run as a module::

    python -m tools.picoscope.mcp

or directly::

    python tools/picoscope/mcp/server.py
"""

from __future__ import annotations

from fastmcp import FastMCP

from .schema import AcquisitionProfile, AnalysisRequest, BackendName
from .service import ScopeService

#: Server name (the MCP tool prefix used by OpenCode, e.g. ``picoscope_scope_info``).
SERVER_NAME = "picoscope"

mcp = FastMCP(SERVER_NAME)

_service = ScopeService()


@mcp.tool()
def scope_info(backend: BackendName = "simulator") -> dict:
    """Return compact device identity, backend, capabilities and ownership status.

    Non-owning discovery. ``backend`` may be ``"simulator"`` (default) or
    ``"ps3000a"`` (real, gated). Does not acquire the device.
    """
    return _service.scope_info(backend)


@mcp.tool()
def scope_capture_block(profile: AcquisitionProfile) -> dict:
    """Run one bounded block capture and save evidence; returns compact metadata.

    Validates the ``profile`` against finite limits before any backend/SDK call.
    Never returns raw sample arrays — only a capture id and artifact paths.
    """
    return _service.capture_block(profile)


@mcp.tool()
def scope_get_capture_metadata(capture_id: str) -> dict:
    """Read the metadata of an existing capture by id (no device needed)."""
    return _service.get_capture_metadata(capture_id)


@mcp.tool()
def scope_export_capture(capture_id: str) -> dict:
    """Return validated artifact references (npz + json) and metadata for a capture."""
    return _service.export_capture(capture_id)


@mcp.tool()
def scope_analyze_capture(request: AnalysisRequest) -> dict:
    """Run one allowlisted analyzer over an existing capture (read-only).

    ``analyzer`` is ``"uart"`` or ``"pwm_timing"``; ``settings`` is validated
    per analyzer. Saves detailed analysis evidence and returns a compact summary.
    No device, hardware, arbitrary path, script, or SDK function is exposed.
    """
    return _service.analyze_capture(request)


def main() -> None:
    """Start the stdio server (blocking)."""
    mcp.run(transport="stdio")


if __name__ == "__main__":
    main()
