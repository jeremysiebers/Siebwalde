"""Deterministic offline tests for the K6-A live command-113 transport.

Covers the exact request framing, the response decoder (valid + malformed +
wrong-source + wrong-command + wrong-length + timeout), the single-shot /
no-retry send, endpoint enforcement, the lack of an arbitrary command
dispatcher, and that the pure test path never opens a socket. No real sockets,
serial ports, or hardware are ever opened (a fake socket is injected).

Run with:
    tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
"""

from __future__ import annotations

import ast
import socket
import sys
from pathlib import Path

import pytest

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics import live_transport as lt  # noqa: E402
from tools.diagnostics.live_transport import (  # noqa: E402
    CMD_GET_CONTROLLER_VERSION,
    HEADER,
    STATUS_FAILED,
    STATUS_PASS,
    STATUS_UNKNOWN,
    TASK_ID_CONTROLLER,
    Command113Transport,
    ControllerVersion,
    build_get_controller_version_request,
    decode_controller_version_response,
)
from tools.diagnostics.observe_guard import EndpointBlocked, ObserveSession  # noqa: E402

ENDPOINT = "192.168.1.193"
TX_PORT = 10000
RECV_PORT = 10001


def _session() -> ObserveSession:
    """An unconsumed single-use authority; the transport consumes it on send."""
    return ObserveSession(113, ENDPOINT)


def _valid_reply(build: bytes = b"2026-10-17", length: int = 82) -> bytes:
    """Build a firmware-style command-113 reply (mirrors udpTrans_t, 82 bytes)."""
    frame = bytearray(length)
    frame[0] = HEADER
    frame[1] = TASK_ID_CONTROLLER
    frame[2] = CMD_GET_CONTROLLER_VERSION
    frame[3] = 1  # MAJOR
    frame[4] = 0  # MINOR
    frame[5] = 0  # PATCH
    frame[6] = 0x01  # PROTOCOL
    frame[7] = 0  # reserved
    frame[8 : 8 + len(build)] = build
    return bytes(frame)


class FakeSocket:
    """Minimal UDP socket stand-in: records sends and returns scripted replies."""

    def __init__(self, replies=None, timeout=False):
        self.replies = list(replies or [])
        self.timeout = timeout
        self.sent = []
        self.timeout_value = None
        self.bound = None
        self.closed = False

    def settimeout(self, value):
        self.timeout_value = value

    def bind(self, addr):
        self.bound = addr

    def sendto(self, data, addr):
        self.sent.append((bytes(data), addr))

    def recvfrom(self, bufsize):
        if self.timeout or not self.replies:
            raise socket.timeout("timed out")
        return self.replies.pop(0)

    def close(self):
        self.closed = True


# --------------------------------------------------------------------------- #
# Request encoder
# --------------------------------------------------------------------------- #

def test_build_request_exact_bytes():
    assert build_get_controller_version_request() == bytes([0xAA, 0x71])
    assert build_get_controller_version_request() == b"\xaa\x71"
    assert len(build_get_controller_version_request()) == 2


def test_request_has_no_footer_and_no_payload():
    # Mirrors C# BuildFrameFromSendMessage: HEADER + command, nothing else.
    assert build_get_controller_version_request() == bytes([HEADER, CMD_GET_CONTROLLER_VERSION])


# --------------------------------------------------------------------------- #
# Response decoder
# --------------------------------------------------------------------------- #

def test_decode_valid_100():
    res = decode_controller_version_response(
        _valid_reply(),
        source_ip=ENDPOINT,
        expected_endpoint=ENDPOINT,
    )
    assert res["status"] == STATUS_PASS
    assert res["controller_version"] == {
        "major": 1,
        "minor": 0,
        "patch": 0,
        "protocol": 0x01,
        "build_timestamp": "2026-10-17",
    }


def test_decode_minimal_length_without_build():
    res = decode_controller_version_response(
        bytes([0xAA, 10, 113, 1, 0, 0, 0x01, 0]),
        source_ip=ENDPOINT,
        expected_endpoint=ENDPOINT,
    )
    assert res["status"] == STATUS_PASS
    assert res["controller_version"]["build_timestamp"] is None


def test_decode_wrong_source_fails():
    res = decode_controller_version_response(
        _valid_reply(),
        source_ip="192.168.1.200",
        expected_endpoint=ENDPOINT,
    )
    assert res["status"] == STATUS_FAILED
    assert res["controller_version"] is None
    assert res["evidence"]["reason"] == "wrong_source"


def test_decode_wrong_command_fails():
    frame = bytearray(_valid_reply())
    frame[1] = 20  # MBUS instead of CONTROLLER(10)
    res = decode_controller_version_response(frame, source_ip=ENDPOINT, expected_endpoint=ENDPOINT)
    assert res["status"] == STATUS_FAILED
    assert res["evidence"]["reason"] == "wrong_command"


def test_decode_wrong_header_fails():
    frame = bytearray(_valid_reply())
    frame[0] = 0x55
    res = decode_controller_version_response(frame, source_ip=ENDPOINT, expected_endpoint=ENDPOINT)
    assert res["status"] == STATUS_FAILED
    assert res["evidence"]["reason"] == "bad_header"


def test_decode_bad_echo_fails():
    frame = bytearray(_valid_reply())
    frame[2] = 112  # echo != 113
    res = decode_controller_version_response(frame, source_ip=ENDPOINT, expected_endpoint=ENDPOINT)
    assert res["status"] == STATUS_FAILED
    assert res["evidence"]["reason"] == "bad_echo"


def test_decode_wrong_length_fails():
    for bad in (b"", b"\xaa\x0a\x71", bytes([0xAA, 10, 113, 1, 0, 0, 0x01])):
        res = decode_controller_version_response(bad, source_ip=ENDPOINT, expected_endpoint=ENDPOINT)
        assert res["status"] == STATUS_FAILED, bad
        assert res["evidence"]["reason"] == "wrong_length", bad


def test_decode_malformed_fails():
    for bad in (None, 123, "aa0a710100000100", ["x"], object()):
        res = decode_controller_version_response(bad)
        assert res["status"] == STATUS_FAILED, repr(bad)
        assert res["controller_version"] is None


# --------------------------------------------------------------------------- #
# Live transport (fake socket, no network)
# --------------------------------------------------------------------------- #

def test_transport_sends_exact_request_and_destination():
    fake = FakeSocket(replies=[(_valid_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert len(fake.sent) == 1  # exactly one send, no retry
    data, addr = fake.sent[0]
    assert data == b"\xaa\x71"
    assert addr == (ENDPOINT, TX_PORT)
    assert fake.bound == ("", RECV_PORT)
    assert res["status"] == STATUS_PASS


def test_transport_valid_roundtrip_pass():
    fake = FakeSocket(replies=[(_valid_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_PASS
    assert res["controller_version"] == {
        "major": 1,
        "minor": 0,
        "patch": 0,
        "protocol": 0x01,
        "build_timestamp": "2026-10-17",
    }
    ev = res["evidence"]
    assert ev["request_bytes"] == "aa71"
    assert ev["source_address"] == ENDPOINT
    assert ev["source_port"] == RECV_PORT
    assert ev["command_id"] == 113
    assert ev["response_sha256"] is not None
    assert fake.closed is True


def test_transport_timeout_is_unknown_and_single_shot():
    fake = FakeSocket(timeout=True)
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    assert res["controller_version"] is None
    assert res["evidence"]["reason"] == "timeout"
    assert len(fake.sent) == 1  # one send only — no implicit retry
    assert fake.closed is True


def test_transport_wrong_source_is_failed():
    fake = FakeSocket(replies=[(_valid_reply(), ("192.168.1.200", RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_FAILED
    assert res["controller_version"] is None
    assert res["evidence"]["reason"] == "wrong_source"


def test_transport_socket_error_is_unknown():
    class Boom(FakeSocket):
        def recvfrom(self, bufsize):
            raise OSError("network unreachable")

    fake = Boom()
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    assert res["evidence"]["reason"] == "socket_error"


def test_transport_rejects_non_allowlisted_endpoint():
    with pytest.raises(EndpointBlocked):
        Command113Transport("192.168.1.100")
    with pytest.raises(EndpointBlocked):
        Command113Transport("")
    with pytest.raises(EndpointBlocked):
        Command113Transport(None)


def test_transport_has_no_arbitrary_dispatch():
    """Single-purpose: no generic send(command, ...) entry point exists."""
    t = Command113Transport(ENDPOINT)
    public = {n for n in dir(t) if not n.startswith("_")}
    assert "send" not in public
    assert "dispatch" not in public
    assert "send_command" not in public
    assert "send_get_controller_version" in public


# --------------------------------------------------------------------------- #
# Offline enforcement (no socket opened, stdlib-only, no legacy transport)
# --------------------------------------------------------------------------- #

def test_no_socket_opened_in_pure_path(monkeypatch):
    calls = []

    def boom(*args, **kwargs):
        calls.append((args, kwargs))
        raise AssertionError("socket.socket() must not be called in the pure path")

    monkeypatch.setattr(lt.socket, "socket", boom)

    # Pure framing/decoding and construction must never create a socket.
    lt.build_get_controller_version_request()
    lt.decode_controller_version_response(_valid_reply(), source_ip=ENDPOINT, expected_endpoint=ENDPOINT)
    Command113Transport(ENDPOINT)
    assert calls == []


def test_live_transport_stdlib_only_and_no_legacy_transport():
    src = Path(lt.__file__).read_text(encoding="utf-8")
    tree = ast.parse(src)
    forbidden = {"Comm", "DataAquisition", "flash_one", "bootload"}
    stdlib = {
        "socket",
        "struct",
        "datetime",
        "hashlib",
        "dataclasses",
        "typing",
        "__future__",
    }
    for node in ast.walk(tree):
        if isinstance(node, ast.Import):
            for alias in node.names:
                root = alias.name.split(".")[0]
                assert root not in forbidden, "forbidden import: %s" % alias.name
                assert root in stdlib, "non-stdlib import: %s" % alias.name
        elif isinstance(node, ast.ImportFrom):
            if node.level == 0:
                root = (node.module or "").split(".")[0]
                assert root not in forbidden, "forbidden import: %s" % node.module
                assert root in stdlib, "non-stdlib import: %s" % node.module
            else:
                assert node.level >= 1  # relative import inside tools.diagnostics


def test_controller_version_dataclass_to_dict():
    v = ControllerVersion(major=1, minor=0, patch=0, protocol=1, build_timestamp="2026-10-17")
    assert v.to_dict() == {
        "major": 1,
        "minor": 0,
        "patch": 0,
        "protocol": 1,
        "build_timestamp": "2026-10-17",
    }
