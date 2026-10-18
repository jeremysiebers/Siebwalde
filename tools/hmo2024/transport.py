"""Serial transport for the HAMEG HMO2024 (VCP, SCPI over LF-terminated lines).

Robustness requirements implemented here:

* definite-length block parsing ``#<k><n><payload>`` across arbitrary chunk
  boundaries, with exact reads, length sanity, binary preservation, optional
  trailing terminator, and per-command bounded timeouts;
* partial-reply handling (pyserial ``read()`` may return fewer bytes than
  requested);
* stale/residual-byte flush before every transaction;
* guaranteed ``close()`` via context manager / ``finally``;
* explicit errors for malformed blocks, impossible lengths, timeouts;
* empty-reply handling.

The transport is deliberately decoupled from pyserial: ``serial_factory`` is a
callable that returns a ``read``/``write``/``close`` object. Production uses
pyserial; tests inject a fake object so no real port is required.
"""

from __future__ import annotations

import time
from dataclasses import dataclass
from typing import Callable, Optional, Tuple


class TransportError(Exception):
    """Base transport error."""


class TransportTimeout(TransportError):
    """A bounded read did not complete within its deadline."""


class BlockParseError(TransportError):
    """A definite-length block response was malformed or impossible."""


class TransportNotOpen(TransportError):
    """An operation was attempted on a closed transport."""


# A screenshot BMP on a 800x480 scope is ~1.2 MB. 64 MB is a generous sanity
# ceiling that still catches garbage/malicious "length" fields.
DEFAULT_MAX_BLOCK = 64 * 1024 * 1024

# Default per-transaction deadlines (seconds).
DEFAULT_QUERY_TIMEOUT = 3.0
DEFAULT_BLOCK_TIMEOUT = 30.0

_TERMINATOR = b"\n"


@dataclass
class SerialConfig:
    port: str
    baudrate: int = 115200
    bytesize: int = 8
    parity: str = "N"
    stopbits: int = 1
    # pyserial read() timeout; we manage our own overall deadlines on top.
    inter_byte_timeout: float = 0.2
    write_timeout: float = 2.0

    @classmethod
    def scope_default(cls, port: str) -> "SerialConfig":
        """The characterized instrument link: 115200 8N1."""
        return cls(port=port, baudrate=115200, bytesize=8, parity="N", stopbits=1)


def _pyserial_factory(config: SerialConfig):
    """Default factory: open a real pyserial port (lazy import)."""
    import serial  # local import so offline tests don't need pyserial

    return serial.Serial(
        port=config.port,
        baudrate=config.baudrate,
        bytesize=config.bytesize,
        parity=config.parity,
        stopbits=config.stopbits,
        timeout=config.inter_byte_timeout,
        write_timeout=config.write_timeout,
    )


class HmoTransport:
    """Framed SCPI query/block transport with bounded timeouts."""

    def __init__(
        self,
        config: SerialConfig,
        serial_factory: Optional[Callable[[SerialConfig], object]] = None,
        clock: Callable[[], float] = time.monotonic,
        max_block: int = DEFAULT_MAX_BLOCK,
        terminator: bytes = _TERMINATOR,
    ):
        self.config = config
        self._serial_factory = serial_factory or _pyserial_factory
        self._clock = clock
        self.max_block = max_block
        self.terminator = terminator
        self._serial = None
        self.write_log = []  # (command_bytes) audit trail, useful for tests

    # -- lifecycle ---------------------------------------------------------
    def open(self) -> None:
        if self._serial is not None:
            return
        self._serial = self._serial_factory(self.config)
        self._flush_input()

    def close(self) -> None:
        s, self._serial = self._serial, None
        if s is not None:
            try:
                s.close()
            except Exception:
                pass

    def __enter__(self) -> "HmoTransport":
        self.open()
        return self

    def __exit__(self, exc_type, exc, tb) -> bool:
        self.close()
        return False

    @property
    def is_open(self) -> bool:
        return self._serial is not None

    def _require_open(self) -> None:
        if self._serial is None:
            raise TransportNotOpen("transport is not open")

    # -- low-level reads ---------------------------------------------------
    def _flush_input(self) -> None:
        """Drain any stale/residual bytes before a fresh transaction."""
        if self._serial is None:
            return
        try:
            while True:
                b = self._serial.read(4096)
                if not b:
                    break
        except Exception:
            pass

    def _read_exact(self, n: int, timeout: float) -> bytes:
        """Read exactly ``n`` bytes, tolerating partial reads, within ``timeout``."""
        self._require_open()
        if n <= 0:
            return b""
        buf = bytearray()
        deadline = self._clock() + timeout
        while len(buf) < n:
            remaining = deadline - self._clock()
            if remaining <= 0:
                raise TransportTimeout(
                    f"timed out reading {n} bytes (got {len(buf)})"
                )
            chunk = self._serial.read(n - len(buf))
            if chunk:
                buf.extend(chunk)
            else:
                # No data available yet (pyserial timeout window). Yield briefly
                # to avoid a hot spin; the overall deadline still governs.
                time.sleep(0.002)
        return bytes(buf)

    def _read_until(self, terminator: bytes, timeout: float) -> bytes:
        """Read until ``terminator``; return bytes BEFORE it (terminator stripped)."""
        self._require_open()
        buf = bytearray()
        deadline = self._clock() + timeout
        while True:
            remaining = deadline - self._clock()
            if remaining <= 0:
                raise TransportTimeout(
                    f"no terminator {terminator!r} within {timeout}s "
                    f"(got {len(buf)} bytes)"
                )
            chunk = self._serial.read(256)
            if chunk:
                buf.extend(chunk)
                idx = buf.find(terminator)
                if idx >= 0:
                    # Bytes after the terminator are residual; they will be
                    # flushed by the next transaction's _flush_input.
                    return bytes(buf[:idx])
            else:
                time.sleep(0.002)

    def _read_optional_terminator(self, timeout: float) -> Optional[bytes]:
        """After a definite-length block, optionally consume one terminator byte.

        Returns the byte if present within ``timeout``, else None. Raises
        BlockParseError if a present byte is not the expected terminator.
        """
        try:
            b = self._read_exact(1, timeout)
        except TransportTimeout:
            return None
        if b != self.terminator:
            raise BlockParseError(
                f"invalid trailing terminator after block: {b!r} (expected "
                f"{self.terminator!r})"
            )
        return b

    def _read_block_payload(self, timeout: float) -> bytes:
        """Parse the payload of a definite-length block.

        The leading ``#`` has already been consumed by the caller.
        """
        # <k>: number of decimal digits in the length field
        kbyte = self._read_exact(1, timeout)
        if not kbyte.isdigit():
            raise BlockParseError(
                f"malformed block: expected digit count, got {kbyte!r}"
            )
        k = int(kbyte)
        if k < 1 or k > 9:
            raise BlockParseError(f"impossible block length field width {k}")
        lenbytes = self._read_exact(k, timeout)
        if not lenbytes.isdigit():
            raise BlockParseError(f"malformed block length field {lenbytes!r}")
        length = int(lenbytes)
        if length < 0:
            raise BlockParseError(f"negative block length {length}")
        if length > self.max_block:
            raise BlockParseError(
                f"block length {length} exceeds sanity limit {self.max_block}"
            )
        payload = self._read_exact(length, timeout)
        self._read_optional_terminator(0.05)
        return payload

    # -- high-level transaction -------------------------------------------
    def transact(self, command: str, timeout: float) -> Tuple[str, bytes]:
        """Send one SCPI command, return ``(kind, payload)``.

        ``kind`` is ``"ascii"`` (LF-terminated, terminator stripped) or
        ``"block"`` (definite-length binary payload). Input is flushed first.
        """
        self._require_open()
        self._flush_input()
        data = command.encode("ascii") + self.terminator
        self._serial.write(data)
        self.write_log.append(command)

        first = self._read_exact(1, timeout)
        if first == b"#":
            return ("block", self._read_block_payload(timeout))
        if first == self.terminator:
            # Empty reply.
            return ("ascii", b"")
        rest = self._read_until(self.terminator, timeout)
        return ("ascii", first + rest)

    def query(self, command: str, timeout: float = DEFAULT_QUERY_TIMEOUT) -> str:
        """ASCII query; returns the decoded response without terminator."""
        kind, payload = self.transact(command, timeout)
        if kind != "ascii":
            raise BlockParseError(
                f"expected ASCII reply to {command!r}, got block data"
            )
        return payload.decode("ascii", errors="replace").strip()

    def query_block(
        self, command: str, timeout: float = DEFAULT_BLOCK_TIMEOUT
    ) -> bytes:
        """Definite-length block query; returns the raw binary payload."""
        kind, payload = self.transact(command, timeout)
        if kind != "block":
            raise BlockParseError(
                f"expected block reply to {command!r}, got ASCII data"
            )
        return payload

    def write(self, command: str) -> None:
        """Send an event/setting command; no reply is read."""
        self._require_open()
        self._flush_input()
        data = command.encode("ascii") + self.terminator
        self._serial.write(data)
        self.write_log.append(command)
