"""Read-only, offline Diagnostics MCP server for the Siebwalde Diagnostics Core.

Wraps ``tools.diagnostics.core`` and ``tools.diagnostics.observe`` with a narrow
set of read-only tools (K4 trace analysis + K5-A OBSERVE parsing/comparison).
The server is incapable of opening a PIC32 (or any) connection: it has no
filesystem access, no raw command tool, no network/hardware tool, and the
underlying core/OBSERVE adapter never touches a socket or serial port.
"""

from .server import main, mcp

__all__ = ["main", "mcp"]
