"""Siebwalde K6-A typed live OBSERVE transport for command 113.

A single-purpose, allowlisted UDP transport for exactly one read-only
diagnostic command: ``GET_CONTROLLER_VERSION`` (113). It is *not* a general
command dispatcher — the command id is fixed, the endpoint is fixed, and there
is no method that accepts an arbitrary command id.

Wire framing (source-verified, mirrored read-only):

  Request (2 bytes, NO payload, NO footer):
      [0] = HEADER (0xAA), [1] = command (113)
      Source: SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Comm/
              TrackCommClientAsync.cs ``BuildFrameFromSendMessage``
              (``dataToSend[0] = HEADER``, ``dataToSend[1] = Command``, then
              the payload; the FOOTER is deliberately NOT used). With
              ``SendMessage(GET_CONTROLLER_VERSION, Array.Empty<byte>())`` the
              frame is exactly ``[0xAA, 0x71]``.
      Also: SiebwaldeApp/SiebwaldeApp.Core/Model/TrackApplication/Services/
            PublicEnums.cs ``HEADER = 0xAA``, ``GET_CONTROLLER_VERSION = 113``.

  Response (>= 8 bytes; the firmware emits the full 82-byte ``udpTrans_t``):
      [0] = HEADER (0xAA)
      [1] = command = CONTROLLER (10)
      [2] = data[0] = echo 113
      [3] = data[1] = CTRL_FW_MAJOR
      [4] = data[2] = CTRL_FW_MINOR
      [5] = data[3] = CTRL_FW_PATCH
      [6] = data[4] = CTRL_PROTOCOL_VERSION
      [7] = data[5] = 0
      [8..N] = CTRL_RELEASE_TIMESTAMP (null-terminated ASCII)
      Source: TrackController5/firmware/src/controller.c ``REPLYxCONTROLLERxVERSION``
              + TrackController5/firmware/src/enums.h ``udpTrans_t { header;
              command; data[80]; }``.
      Values: TrackController5/firmware/src/version.h (MAJOR=1, MINOR=0, PATCH=0,
              PROTOCOL=0x01, CTRL_RELEASE_TIMESTAMP="2026-10-17").

Network (source-verified):
      Send to (endpoint, port=10000); the PIC32 listens on SERVER_PORT 10000 and
      replies from CLIENT_PORT 10001 to the client's receive port.
      Source: TrackController5/firmware/src/ethernet.c
              (``#define SERVER_PORT 10000``, ``#define CLIENT_PORT 10001``) and
              TrackController5/Python/Comm.py (``UDP_PORT_TRANS = 10000``,
              ``UDP_PORT_RECV = 10001``).

Safety posture:
      * exactly one request datagram, one bounded receive (no retry, no recovery);
      * fail-closed: timeout, socket error, wrong source, or any structural
        mismatch yields ``FAILED``/``UNKNOWN`` and is never promoted to ``PASS``;
      * the live socket is opened only inside :meth:`send_get_controller_version`
        and is always closed in a ``finally`` block. A test can inject a socket
        factory so the offline test path never touches a real socket.

This module uses the standard library only (socket / struct / datetime /
hashlib). It MUST NOT import ``Comm`` / ``DataAquisition`` (their import graph
binds UDP sockets and exposes a general command transport).
"""

from __future__ import annotations

import hashlib
import socket
import struct
from dataclasses import dataclass
from datetime import datetime, timezone
from typing import Callable, Optional

from . import observe_guard

# --------------------------------------------------------------------------- #
# Wire constants (source-verified)
# --------------------------------------------------------------------------- #

HEADER = 0xAA
TASK_ID_CONTROLLER = 10
CMD_GET_CONTROLLER_VERSION = 113

DEFAULT_ENDPOINT = observe_guard.REQUIRED_ENDPOINT  # "192.168.1.193"
DEFAULT_TX_PORT = 10000   # PIC32 SERVER_PORT (request destination)
DEFAULT_RECV_PORT = 10001  # PIC32 CLIENT_PORT (reply source) / client recv bind
DEFAULT_TIMEOUT = observe_guard.DEFAULT_TIMEOUT_SECONDS

# Result classification (fail-closed).
STATUS_PASS = "PASS"
STATUS_FAILED = "FAILED"
STATUS_UNKNOWN = "UNKNOWN"

# Minimum structural length of a valid reply: header, command, echo,
# MAJOR/MINOR/PATCH, PROTOCOL, reserved.
_MIN_REPLY_LENGTH = 8


# --------------------------------------------------------------------------- #
# Typed model
# --------------------------------------------------------------------------- #


@dataclass(frozen=True)
class ControllerVersion:
    """Semantic PIC32 controller version from a command-113 reply."""

    major: Optional[int] = None
    minor: Optional[int] = None
    patch: Optional[int] = None
    protocol: Optional[int] = None
    build_timestamp: Optional[str] = None

    def to_dict(self) -> dict:
        return {
            "major": self.major,
            "minor": self.minor,
            "patch": self.patch,
            "protocol": self.protocol,
            "build_timestamp": self.build_timestamp,
        }


# --------------------------------------------------------------------------- #
# Pure framing helpers (testable with no socket)
# --------------------------------------------------------------------------- #


def build_get_controller_version_request() -> bytes:
    """Build the exact command-113 request frame.

    Returns ``b"\\xaa\\x71"`` — 2 bytes, no payload, no footer — mirroring the
    C# ``BuildFrameFromSendMessage`` layout (HEADER + command + empty data).
    """
    return struct.pack("<BB", HEADER, CMD_GET_CONTROLLER_VERSION)


def _classify_failure(reason: str) -> dict:
    return {
        "status": STATUS_FAILED,
        "controller_version": None,
        "evidence": {"reason": reason},
    }


def decode_controller_version_response(
    frame: object,
    source_ip: Optional[str] = None,
    expected_endpoint: Optional[str] = None,
) -> dict:
    """Validate and decode a command-113 response frame. Pure (no socket).

    Validation order (fail-closed):

    1. wrong source — ``source_ip`` present but != ``expected_endpoint``;
    2. wrong length — not bytes / shorter than 8 bytes;
    3. bad header — ``frame[0] != 0xAA``;
    4. wrong command — ``frame[1] != CONTROLLER(10)``;
    5. bad echo — ``frame[2] != 113``.

    On success returns ``PASS`` with a decoded :class:`ControllerVersion` dict.
    On any mismatch returns ``FAILED`` with a reason and ``controller_version``
    ``None``. Nothing is ever fabricated.
    """
    if expected_endpoint and source_ip is not None and source_ip != expected_endpoint:
        return _classify_failure("wrong_source")

    if not isinstance(frame, (bytes, bytearray)):
        return _classify_failure("wrong_length")

    raw = bytes(frame)
    if len(raw) < _MIN_REPLY_LENGTH:
        return _classify_failure("wrong_length")

    header, command, echo = raw[0], raw[1], raw[2]
    if header != HEADER:
        return _classify_failure("bad_header")
    if command != TASK_ID_CONTROLLER:
        return _classify_failure("wrong_command")
    if echo != CMD_GET_CONTROLLER_VERSION:
        return _classify_failure("bad_echo")

    major, minor, patch, protocol = raw[3], raw[4], raw[5], raw[6]
    build = raw[8:].split(b"\x00", 1)[0].decode("ascii", "replace").strip()
    version = ControllerVersion(
        major=major,
        minor=minor,
        patch=patch,
        protocol=protocol,
        build_timestamp=build or None,
    )
    return {
        "status": STATUS_PASS,
        "controller_version": version.to_dict(),
        "evidence": {"reason": None},
    }


# --------------------------------------------------------------------------- #
# Live transport (single-purpose, no arbitrary dispatch)
# --------------------------------------------------------------------------- #

# Default socket factory is a module-level function so tests can inject a fake
# without ever creating a real socket.
def _default_socket_factory() -> socket.socket:
    return socket.socket(socket.AF_INET, socket.SOCK_DGRAM)


class Command113Transport:
    """A single-purpose, allowlisted transport for exactly one command-113 query.

    It cannot dispatch arbitrary commands: the only I/O entry point is
    :meth:`send_get_controller_version`, and the command id is fixed at 113.
    The endpoint is validated against the explicit allowlisted address (no
    fallback, no DNS). The timeout is bounded; there is no retry and no
    recovery.
    """

    def __init__(
        self,
        endpoint: str = DEFAULT_ENDPOINT,
        port: int = DEFAULT_TX_PORT,
        recv_port: int = DEFAULT_RECV_PORT,
        timeout: float = DEFAULT_TIMEOUT,
        socket_factory: Optional[Callable[[], "socket.socket"]] = None,
    ) -> None:
        # Endpoint enforcement is delegated to the executable guard.
        if observe_guard.validate_endpoint(endpoint) != observe_guard.ALLOWED:
            raise observe_guard.EndpointBlocked(
                "endpoint %r is not the explicit allowed endpoint %r"
                % (endpoint, observe_guard.REQUIRED_ENDPOINT)
            )
        self.endpoint = endpoint.strip()
        self.port = int(port)
        self.recv_port = int(recv_port)
        self.timeout = observe_guard.bound_timeout(timeout)
        self._socket_factory = socket_factory or _default_socket_factory

    # -- single command-113 request ----------------------------------------- #

    def send_get_controller_version(self) -> dict:
        """Send exactly one command-113 request and receive one bounded reply.

        Returns a fail-closed result dict::

            {
              "status": "PASS" | "FAILED" | "UNKNOWN",
              "controller_version": {major, minor, patch, protocol,
                                     build_timestamp} | None,
              "evidence": {...},
            }

        A timeout / socket error / wrong source / malformed frame yields
        ``FAILED`` or ``UNKNOWN`` and is never promoted to ``PASS``. The socket
        is opened here, used once, and closed in a ``finally`` block.
        """
        request = build_get_controller_version_request()
        sent_at = datetime.now(timezone.utc).isoformat()

        # Double enforcement before any I/O (executable, not advisory).
        if observe_guard.validate_command(CMD_GET_CONTROLLER_VERSION) != observe_guard.ALLOWED:
            return {
                "status": STATUS_UNKNOWN,
                "controller_version": None,
                "evidence": {"reason": "command blocked", "sent": False},
            }

        sock = self._socket_factory()
        try:
            sock.settimeout(self.timeout)
            sock.bind(("", self.recv_port))
            sock.sendto(request, (self.endpoint, self.port))
            data, addr = sock.recvfrom(2048)
        except (socket.timeout, TimeoutError):
            return {
                "status": STATUS_UNKNOWN,
                "controller_version": None,
                "evidence": {
                    "reason": "timeout",
                    "sent": True,
                    "request_bytes": request.hex(),
                    "sent_at": sent_at,
                },
            }
        except OSError as exc:
            return {
                "status": STATUS_UNKNOWN,
                "controller_version": None,
                "evidence": {
                    "reason": "socket_error",
                    "detail": str(exc),
                    "sent": True,
                    "request_bytes": request.hex(),
                    "sent_at": sent_at,
                },
            }
        finally:
            try:
                sock.close()
            except Exception:
                pass

        received_at = datetime.now(timezone.utc).isoformat()
        source_ip = addr[0] if isinstance(addr, tuple) and len(addr) >= 1 else None
        source_port = addr[1] if isinstance(addr, tuple) and len(addr) >= 2 else None

        result = decode_controller_version_response(
            data,
            source_ip=source_ip,
            expected_endpoint=self.endpoint,
        )

        evidence = dict(result.get("evidence") or {})
        evidence.update(
            {
                "command_id": CMD_GET_CONTROLLER_VERSION,
                "endpoint": self.endpoint,
                "tx_port": self.port,
                "recv_port": self.recv_port,
                "request_bytes": request.hex(),
                "response_bytes": bytes(data).hex() if isinstance(data, (bytes, bytearray)) else None,
                "response_sha256": hashlib.sha256(bytes(data)).hexdigest()
                if isinstance(data, (bytes, bytearray))
                else None,
                "source_address": source_ip,
                "source_port": source_port,
                "sent_at": sent_at,
                "received_at": received_at,
            }
        )
        result["evidence"] = evidence
        return result
