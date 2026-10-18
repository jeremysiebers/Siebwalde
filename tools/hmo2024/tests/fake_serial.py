"""Fake serial object for offline tests (no real port).

Two cooperative behaviours:

* ``program`` mode -- a list of ``(expected_prefix, response_bytes)``. Each
  ``write()`` finds the next matching program entry (by decoded command
  prefix) and arms its response bytes for subsequent ``read()`` calls. This
  mirrors the transport's flush-before-write / read-after-write discipline.

* ``chunk_size`` -- when set, each ``read(n)`` returns at most ``chunk_size``
  bytes, forcing the transport to assemble prefixes/payloads across arbitrary
  chunk boundaries (partial-read simulation).

``read()`` returns ``b''`` when no response bytes are pending, which the
transport treats as a pyserial timeout window.
"""

from __future__ import annotations

from typing import List, Optional, Tuple


class FakeSerial:
    def __init__(
        self,
        program: Optional[List[Tuple[str, bytes]]] = None,
        chunk_size: Optional[int] = None,
        timeout: float = 0.05,
    ):
        self.program = list(program or [])
        self.prog_idx = 0
        self._pending = b""
        self.chunk_size = chunk_size
        self.timeout = timeout
        self.written: List[bytes] = []
        self.closed = False
        self.read_sizes: List[int] = []

    def write(self, data) -> int:
        b = bytes(data)
        self.written.append(b)
        text = b.decode("ascii", "replace").rstrip("\n")
        self._pending = b""
        for i in range(self.prog_idx, len(self.program)):
            exp, resp = self.program[i]
            if text.startswith(exp):
                self.prog_idx = i + 1
                self._pending = bytes(resp)
                break
        return len(b)

    def read(self, n) -> bytes:
        self.read_sizes.append(n)
        take = n if self.chunk_size is None else min(n, self.chunk_size)
        out = self._pending[:take]
        self._pending = self._pending[take:]
        return out

    def in_waiting(self) -> int:
        return len(self._pending)

    def close(self) -> None:
        self.closed = True
