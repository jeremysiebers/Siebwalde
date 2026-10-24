"""Category 22: read-only ``scope_analyze_capture`` analysis tool."""

import os
import sys

import numpy as np
import pytest

_REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
if _REPO not in sys.path:
    sys.path.insert(0, _REPO)

from tools.picoscope.analysis import fixtures as fx  # noqa: E402
from tools.picoscope.backend.errors import InvalidConfigError  # noqa: E402
from tools.picoscope.mcp.config import ServerConfig  # noqa: E402
from tools.picoscope.mcp.schema import AnalysisRequest  # noqa: E402
from tools.picoscope.mcp.service import ScopeService  # noqa: E402
from tools.picoscope.tests_mcp.mcp_client import error_text, run, structured  # noqa: E402

_DIGITAL = [{"port": 0, "enabled": True, "threshold_mv": 1500}]


def test_analyze_pwm_via_client(env_factory):
    """End-to-end: capture a digital port, analyze it, get a compact summary."""
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_capture_block", {
            "profile": {"backend": "simulator", "digital_ports": _DIGITAL, "post_samples": 2000}
        })
        assert not res.is_error, error_text(res)
        capture_id = structured(res)["capture_id"]

        res2 = await session.call_tool("scope_analyze_capture", {
            "request": {"capture_id": capture_id, "analyzer": "pwm_timing", "settings": {"source": "D0"}}
        })
        assert not res2.is_error, error_text(res2)
        payload = structured(res2)
        assert payload["result"] == "ok"
        assert payload["analyzer"] == "pwm_timing"
        # D0 default simulator square is 1 kHz -> measurable frequency.
        assert payload["summary"]["frequency_hz"] > 0
        assert payload["summary"]["complete_cycles"] >= 1
        assert os.path.isfile(payload["analysis_path"])

    run(env, body)


def test_analyze_invalid_analyzer_rejected_via_client(env_factory):
    env = env_factory()

    async def body(session):
        res = await session.call_tool("scope_analyze_capture", {
            "request": {"capture_id": "a" * 32, "analyzer": "not_real", "settings": {}}
        })
        assert res.is_error, "unknown analyzer must be rejected"

    run(env, body)


def test_analyze_uart_direct(tmp_path):
    """Direct unit test: the uart analyzer decodes a known fixture line."""
    service = ScopeService(ServerConfig(evidence_dir=str(tmp_path / "evidence")))

    class _FakeCapture:
        sample_interval_ns = 8
        digital_samples = {}

    f = fx.fixture_valid_9bit()
    cap = _FakeCapture()
    cap.digital_samples["D0"] = f["samples"]

    result = service._analyze_uart(cap, {"line": "D0", "mode": "9bit", "baud": f["baud"]})
    assert result["summary"]["frame_count"] == 1
    assert result["frames"][0]["value"] == 0x04
    assert result["frames"][0]["ninth_bit"] == 1


def test_analyze_unknown_setting_rejected(tmp_path):
    service = ScopeService(ServerConfig(evidence_dir=str(tmp_path / "evidence")))

    class _FakeCapture:
        sample_interval_ns = 8
        digital_samples = {"D0": np.zeros(100, dtype=np.uint8)}

    with pytest.raises(InvalidConfigError):
        service._analyze_uart(_FakeCapture(), {"line": "D0", "mode": "9bit", "baud": 2000000, "shell": "x"})


def test_analyze_bad_line_rejected(tmp_path):
    service = ScopeService(ServerConfig(evidence_dir=str(tmp_path / "evidence")))

    class _FakeCapture:
        sample_interval_ns = 8
        digital_samples = {"D0": np.zeros(100, dtype=np.uint8)}

    with pytest.raises(InvalidConfigError):
        service._analyze_uart(_FakeCapture(), {"line": "../etc", "mode": "9bit", "baud": 2000000})


def test_analyze_pwm_analog_direct(tmp_path):
    service = ScopeService(ServerConfig(evidence_dir=str(tmp_path / "evidence")))

    class _FakeCapture:
        sample_interval_ns = 16
        analog_samples = {}

    f = fx.fixture_pwm_analog()
    cap = _FakeCapture()
    cap.analog_samples["A"] = f["samples"]

    result = service._analyze_pwm(cap, {"source": "A"})
    assert result["summary"]["frequency_hz"] > 0
    assert result["summary"]["domain"] == "analog"


def test_analyze_unknown_capture_id_rejected(tmp_path):
    from tools.picoscope.mcp.evidence import EvidenceError

    service = ScopeService(ServerConfig(evidence_dir=str(tmp_path / "evidence")))
    with pytest.raises(EvidenceError):
        service.analyze_capture(AnalysisRequest(capture_id="../../etc", analyzer="uart", settings={}))
