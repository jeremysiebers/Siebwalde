"""Deterministic offline tests for the K7 send-state accuracy correction.

Verifies that a command-113 send reports a typed ``send_state`` and a truthful
``sent`` flag: a bind / socket-create / send failure never yields a false SENT,
and the furthest completed transport stage is recorded. A successful round-trip
still decodes 0.1.0 / protocol 0 / "2026-10-20". No real sockets, serial ports,
or hardware are opened (a fake socket is injected).

Run with:
    tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
"""

from __future__ import annotations

import errno
import socket
import sys
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics.live_transport import (  # noqa: E402
    CMD_GET_CONTROLLER_VERSION,
    HEADER,
    PHYSICAL_EXECUTION_UNKNOWN,
    SEND_STATE_BIND_FAILED,
    SEND_STATE_NOT_ATTEMPTED,
    SEND_STATE_RECEIVED,
    SEND_STATE_SEND_FAILED,
    SEND_STATE_SENT,
    SEND_STATE_VALIDATED,
    SEND_STATES,
    STATUS_FAILED,
    STATUS_PASS,
    STATUS_UNKNOWN,
    TASK_ID_CONTROLLER,
    Command113Transport,
    build_get_controller_version_request,
)
from tools.diagnostics.observe_guard import ObserveSession  # noqa: E402

ENDPOINT = "192.168.1.193"
RECV_PORT = 10001


def _session() -> ObserveSession:
    """An unconsumed single-use authority; the transport consumes it on send."""
    return ObserveSession(113, ENDPOINT)


def _reply(major=0, minor=1, patch=0, protocol=0, build=b"2026-10-20") -> bytes:
    """Firmware-style command-113 reply (mirrors udpTrans_t, 82 bytes)."""
    frame = bytearray(82)
    frame[0] = HEADER
    frame[1] = TASK_ID_CONTROLLER
    frame[2] = CMD_GET_CONTROLLER_VERSION
    frame[3] = major
    frame[4] = minor
    frame[5] = patch
    frame[6] = protocol
    frame[7] = 0  # reserved
    frame[8 : 8 + len(build)] = build
    return bytes(frame)


class FakeSocket:
    """Scripted UDP socket stand-in; records sends and injects failures."""

    def __init__(self, replies=None, timeout=False, bind_error=None, send_error=None):
        self.replies = list(replies or [])
        self.timeout = timeout
        self.bind_error = bind_error
        self.send_error = send_error
        self.sent = []
        self.sendto_calls = 0
        self.bound = None
        self.closed = False

    def settimeout(self, value):
        self.timeout_value = value

    def bind(self, addr):
        if self.bind_error is not None:
            raise self.bind_error
        self.bound = addr

    def sendto(self, data, addr):
        self.sendto_calls += 1
        if self.send_error is not None:
            raise self.send_error
        self.sent.append((bytes(data), addr))

    def recvfrom(self, bufsize):
        if self.timeout or not self.replies:
            raise socket.timeout("timed out")
        return self.replies.pop(0)

    def close(self):
        self.closed = True


# --------------------------------------------------------------------------- #
# send_state vocabulary
# --------------------------------------------------------------------------- #

def test_send_state_vocabulary_is_exact():
    assert SEND_STATES == {
        SEND_STATE_NOT_ATTEMPTED,
        SEND_STATE_BIND_FAILED,
        SEND_STATE_SEND_FAILED,
        SEND_STATE_SENT,
        SEND_STATE_RECEIVED,
        SEND_STATE_VALIDATED,
    }


# --------------------------------------------------------------------------- #
# Fail-closed send-state accuracy (no false SENT)
# --------------------------------------------------------------------------- #

def test_bind_failure_is_unknown_and_not_sent():
    fake = FakeSocket(bind_error=OSError(errno.EADDRINUSE, "address already in use"))
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    assert res["controller_version"] is None
    ev = res["evidence"]
    assert ev["reason"] == "socket_error"
    assert ev["send_state"] == SEND_STATE_BIND_FAILED
    assert ev["sent"] is False  # no false SENT on bind failure
    assert fake.sent == []      # nothing was transmitted
    assert fake.closed is True


def test_socket_creation_failure_is_unknown_not_exception():
    def boom():
        raise OSError("cannot create socket")

    t = Command113Transport(ENDPOINT, socket_factory=boom, session=_session())
    res = t.send_get_controller_version()  # must not raise

    assert res["status"] == STATUS_UNKNOWN
    assert res["controller_version"] is None
    assert res["evidence"]["send_state"] == SEND_STATE_BIND_FAILED
    assert res["evidence"]["sent"] is False


def test_send_failure_is_unknown_and_accurately_not_sent():
    fake = FakeSocket(send_error=OSError(errno.ENETUNREACH, "network unreachable"))
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    ev = res["evidence"]
    assert ev["reason"] == "socket_error"
    assert ev["send_state"] == SEND_STATE_SEND_FAILED
    assert ev["sent"] is False          # sendto raised: local send NOT accepted
    assert fake.sent == []              # nothing was transmitted
    assert fake.sendto_calls == 1       # exactly one sendto attempt, no retry


def test_receive_timeout_is_unknown_and_sent_once():
    fake = FakeSocket(timeout=True)
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    ev = res["evidence"]
    assert ev["reason"] == "timeout"
    assert ev["send_state"] == SEND_STATE_SENT
    assert ev["sent"] is True           # sendto returned before the wait elapsed
    assert fake.sendto_calls == 1       # exactly one send, no hidden retry
    assert fake.closed is True


# --------------------------------------------------------------------------- #
# FAILED responses (decoded but rejected) — send_state = validated
# --------------------------------------------------------------------------- #

def test_wrong_source_is_failed_with_validated_state():
    fake = FakeSocket(replies=[(_reply(), ("192.168.1.200", RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_FAILED
    assert res["controller_version"] is None
    ev = res["evidence"]
    assert ev["reason"] == "wrong_source"
    assert ev["send_state"] == SEND_STATE_VALIDATED
    assert ev["sent"] is True


def test_malformed_short_response_is_failed():
    fake = FakeSocket(replies=[(b"\xaa\x0a", (ENDPOINT, RECV_PORT))])  # too short
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_FAILED
    assert res["evidence"]["reason"] == "wrong_length"
    assert res["evidence"]["send_state"] == SEND_STATE_VALIDATED
    assert res["evidence"]["sent"] is True


def test_decoder_failure_bad_echo_is_failed():
    frame = bytearray(_reply())
    frame[2] = 112  # echo != 113
    fake = FakeSocket(replies=[(bytes(frame), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_FAILED
    assert res["controller_version"] is None
    assert res["evidence"]["reason"] == "bad_echo"
    assert res["evidence"]["send_state"] == SEND_STATE_VALIDATED


# --------------------------------------------------------------------------- #
# Successful round-trip (0.1.0 / protocol 0 / "2026-10-20")
# --------------------------------------------------------------------------- #

def test_successful_roundtrip_passes_010_protocol0_build_2026_10_20():
    fake = FakeSocket(replies=[(_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_PASS
    assert res["controller_version"] == {
        "major": 0,
        "minor": 1,
        "patch": 0,
        "protocol": 0,
        "build_timestamp": "2026-10-20",
    }
    ev = res["evidence"]
    assert ev["send_state"] == SEND_STATE_VALIDATED
    assert ev["sent"] is True
    assert ev["controller_reported"] is True
    assert ev["physical_execution"] == PHYSICAL_EXECUTION_UNKNOWN
    assert ev["authorization"]["status"] == "ALLOWED"
    assert ev["authorization"]["authorized_command_count_remaining"] == 0


# --------------------------------------------------------------------------- #
# No arbitrary command dispatch, no real socket
# --------------------------------------------------------------------------- #

def test_no_arbitrary_command_execution():
    t = Command113Transport(ENDPOINT, session=_session())
    public = {n for n in dir(t) if not n.startswith("_")}
    assert "send" not in public
    assert "dispatch" not in public
    assert "send_command" not in public
    assert "send_get_controller_version" in public
    # The only frame ever emitted is the fixed command-113 request.
    assert build_get_controller_version_request() == bytes([HEADER, CMD_GET_CONTROLLER_VERSION])


def test_no_real_socket_opened_with_fake_factory(monkeypatch):
    def boom(*args, **kwargs):
        raise AssertionError("socket.socket() must not be called in the offline path")

    monkeypatch.setattr(socket, "socket", boom)

    fake = FakeSocket(replies=[(_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=_session())
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_PASS
    assert len(fake.sent) == 1
    assert fake.sent[0][0] == b"\xaa\x71"
