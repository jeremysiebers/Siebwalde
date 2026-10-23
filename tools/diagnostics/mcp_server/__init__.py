"""Read-only, offline Diagnostics MCP server for the Siebwalde Diagnostics Core.

Wraps ``tools.diagnostics.core`` with a narrow set of four read-only tools.
The server is incapable of opening a PIC32 (or any) connection: it has no
filesystem access, no raw command tool, no network/hardware tool, and the
underlying core never touches a socket or serial port.
"""

from .server import main, mcp

__all__ = ["main", "mcp"]
