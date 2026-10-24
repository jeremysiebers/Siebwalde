"""Layer C — a minimal, measurement-only FastMCP stdio adapter (PICO-3).

This package exposes a thin MCP server over the validated
``tools.picoscope.backend`` measurement layer. It is deliberately separate from
Layer A so the backend remains usable without any MCP dependency.

Public modules:

* :mod:`tools.picoscope.mcp.config` — finite limits + server configuration.
* :mod:`tools.picoscope.mcp.evidence` — dedicated evidence dir + capture-id safety.
* :mod:`tools.picoscope.mcp.lock` — narrow cross-process device lock.
* :mod:`tools.picoscope.mcp.schema` — Pydantic request schemas (strict validation).
* :mod:`tools.picoscope.mcp.service` — the allowlisted measurement service.
* :mod:`tools.picoscope.mcp.server` — the FastMCP stdio server (entry point).

See ``docs/PICOSCOPE-PICO-3-MCP-INTEGRATION.md`` for the tool schemas, limits,
ownership handling and OpenCode registration.
"""

__all__ = [
    "config",
    "evidence",
    "lock",
    "schema",
    "service",
    "server",
]
