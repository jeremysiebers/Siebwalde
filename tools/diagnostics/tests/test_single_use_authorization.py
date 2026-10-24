"""Deterministic offline tests for the K7 single-use authorization hardening.

Verifies the transport cannot send without a valid, consumed single-use
authority; that the token consume is thread-safe (exactly one winner); that the
authorization is never reused and never restored after an I/O error; and that
the token is an in-memory, single-process boundary (NOT a durable cross-process
hardware lock).

Run with:
    tools/knowledge/.venv/Scripts/python.exe -m pytest tools/diagnostics/tests -q
"""

from __future__ import annotations

import socket
import sys
import threading
from pathlib import Path

_REPO = Path(__file__).resolve().parents[3]
if str(_REPO) not in sys.path:
    sys.path.insert(0, str(_REPO))

from tools.diagnostics.live_transport import (  # noqa: E402
    CMD_GET_CONTROLLER_VERSION,
    HEADER,
    SEND_STATE_NOT_ATTEMPTED,
    STATUS_PASS,
    STATUS_UNKNOWN,
    TASK_ID_CONTROLLER,
    Command113Transport,
)
from tools.diagnostics.observe_guard import (  # noqa: E402
    AuthorizationBlocked,
    ObserveSession,
)

ENDPOINT = "192.168.1.193"
RECV_PORT = 10001


def _reply() -> bytes:
    """A valid command-113 reply (0.1.0 / protocol 0 / "2026-10-20")."""
    frame = bytearray(82)
    frame[0] = HEADER
    frame[1] = TASK_ID_CONTROLLER
    frame[2] = CMD_GET_CONTROLLER_VERSION
    frame[3] = 0  # major
    frame[4] = 1  # minor
    frame[5] = 0  # patch
    frame[6] = 0  # protocol
    frame[7] = 0  # reserved
    frame[8:18] = b"2026-10-20"
    return bytes(frame)


class FakeSocket:
    def __init__(self, replies=None, timeout=False):
        self.replies = list(replies or [])
        self.timeout = timeout
        self.sent = []
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
# Transport refuses without a valid, consumed authority (no send)
# --------------------------------------------------------------------------- #

def test_transport_refuses_without_session():
    fake = FakeSocket(replies=[(_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake)  # no session
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    assert res["evidence"]["reason"] == "authorization absent"
    assert res["evidence"]["send_state"] == SEND_STATE_NOT_ATTEMPTED
    assert res["evidence"]["sent"] is False
    assert fake.sent == []  # no send occurred


def test_transport_refuses_already_consumed_session():
    session = ObserveSession(113, ENDPOINT)
    session.consume()  # pre-consume before binding the transport
    fake = FakeSocket(replies=[(_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=session)
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    assert res["evidence"]["reason"] == "authorization already consumed"
    assert fake.sent == []


def test_transport_refuses_invalid_command_session():
    session = ObserveSession(125, ENDPOINT)  # outside the {113} allowlist
    fake = FakeSocket(replies=[(_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=session)
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    assert res["evidence"]["reason"] == "command blocked"
    assert fake.sent == []


def test_transport_refuses_invalid_endpoint_session():
    session = ObserveSession(113, "192.168.1.100")
    fake = FakeSocket(replies=[(_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=session)
    res = t.send_get_controller_version()

    assert res["status"] == STATUS_UNKNOWN
    assert res["evidence"]["reason"] == "endpoint blocked"
    assert fake.sent == []


# --------------------------------------------------------------------------- #
# Single-use: no reuse, no restoration
# --------------------------------------------------------------------------- #

def test_transport_consumes_only_on_send():
    session = ObserveSession(113, ENDPOINT)
    assert session.AUTHORIZED_COMMAND_COUNT_REMAINING == 1
    fake = FakeSocket(replies=[(_reply(), (ENDPOINT, RECV_PORT))])
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=session)

    # Construction does not consume the token.
    assert session.AUTHORIZED_COMMAND_COUNT_REMAINING == 1

    res = t.send_get_controller_version()
    assert res["status"] == STATUS_PASS
    assert session.AUTHORIZED_COMMAND_COUNT_REMAINING == 0


def test_no_reuse_of_one_time_authorization():
    session = ObserveSession(113, ENDPOINT)
    fake = FakeSocket(
        replies=[(_reply(), (ENDPOINT, RECV_PORT)), (_reply(), (ENDPOINT, RECV_PORT))]
    )
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=session)

    first = t.send_get_controller_version()
    assert first["status"] == STATUS_PASS
    assert len(fake.sent) == 1

    second = t.send_get_controller_version()
    assert second["status"] == STATUS_UNKNOWN
    assert second["evidence"]["reason"] == "authorization already consumed"
    assert len(fake.sent) == 1  # exactly one send, no second


def test_no_authorization_restoration_after_error():
    session = ObserveSession(113, ENDPOINT)
    fake = FakeSocket(timeout=True)  # first send times out
    t = Command113Transport(ENDPOINT, socket_factory=lambda: fake, session=session)

    first = t.send_get_controller_version()
    assert first["status"] == STATUS_UNKNOWN
    assert first["evidence"]["reason"] == "timeout"
    assert session.AUTHORIZED_COMMAND_COUNT_REMAINING == 0  # consumed, not restored

    second = t.send_get_controller_version()
    assert second["status"] == STATUS_UNKNOWN
    assert second["evidence"]["reason"] == "authorization already consumed"
    assert len(fake.sent) == 1  # only the first (failed) send happened


# --------------------------------------------------------------------------- #
# Thread-safe consume (exactly one winner)
# --------------------------------------------------------------------------- #

def test_concurrent_consume_exactly_one_succeeds():
    session = ObserveSession(113, ENDPOINT)
    barrier = threading.Barrier(32)
    outcomes = []
    lock = threading.Lock()

    def worker():
        barrier.wait()
        try:
            session.consume()
            outcome = "ok"
        except AuthorizationBlocked:
            outcome = "blocked"
        with lock:
            outcomes.append(outcome)

    threads = [threading.Thread(target=worker) for _ in range(32)]
    for th in threads:
        th.start()
    for th in threads:
        th.join()

    assert outcomes.count("ok") == 1
    assert outcomes.count("blocked") == 31


# --------------------------------------------------------------------------- #
# In-memory, single-process boundary (documented, not a hardware lock)
# --------------------------------------------------------------------------- #

def test_token_is_in_memory_single_process_only():
    # Two independent sessions share no state: the token is an in-memory object,
    # not a durable cross-process authority. A new session (as after a process
    # restart) starts un-consumed and must NOT be read as renewed permission.
    a = ObserveSession(113, ENDPOINT)
    b = ObserveSession(113, ENDPOINT)
    a.consume()
    assert a.AUTHORIZED_COMMAND_COUNT_REMAINING == 0
    assert b.AUTHORIZED_COMMAND_COUNT_REMAINING == 1  # b unaffected by a
