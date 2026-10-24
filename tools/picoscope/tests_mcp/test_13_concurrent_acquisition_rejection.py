"""Category 13: concurrent-acquisition rejection (cross-process + in-process)."""

import os

import pytest

from tools.picoscope.tests_mcp.mcp_client import error_text, run

_ANALOG = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def test_cross_process_busy_rejected(env_factory):
    """A second process holding the device lock blocks an MCP capture.

    The test process itself acquires the OS file lock for ``SIM0001`` in the
    server's evidence dir; the server (a separate process) then tries to acquire
    the same lock and must fail with a clear "busy" error rather than fight for
    the device.
    """
    from tools.picoscope.mcp.lock import CrossProcessLock

    env = env_factory()
    lock_dir = os.path.realpath(env["PICOSCOPE_EVIDENCE_DIR"])
    lock = CrossProcessLock(lock_dir, "SIM0001")
    lock.acquire()
    try:
        async def body(session):
            res = await session.call_tool("scope_capture_block", {"profile": {
                "backend": "simulator",
                "device_id": "SIM0001",
                "analog_channels": _ANALOG,
            }})
            assert res.is_error, "cross-process collision should be rejected"
            assert "owned by another process" in error_text(res)

        run(env, body)
    finally:
        lock.release()


def test_in_flight_guard_direct(tmp_path):
    """Supplementary direct check: a second in-process acquisition is refused.

    (The in-flight guard is defence-in-depth behind the stdio transport; the
    authoritative concurrency evidence is the cross-process test above.)
    """
    from tools.picoscope.backend.errors import DeviceBusyError
    from tools.picoscope.mcp.config import ServerConfig
    from tools.picoscope.mcp.schema import AcquisitionProfile
    from tools.picoscope.mcp.service import ScopeService

    svc = ScopeService(ServerConfig(evidence_dir=str(tmp_path)))
    svc._in_flight.acquire()
    try:
        profile = AcquisitionProfile(
            backend="simulator",
            analog_channels=[{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}],
        )
        with pytest.raises(DeviceBusyError):
            svc.capture_block(profile)
    finally:
        svc._in_flight.release()
