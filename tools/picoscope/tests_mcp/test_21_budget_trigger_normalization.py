"""Category 21: interval-independent budget uses the EFFECTIVE trigger split.

Regression test for the PICO-4 budget fix. Before the fix, the sample-count /
memory / file-size budget was computed from the top-level
``pre_samples + post_samples`` even when a trigger (with its own pre/post split)
was supplied. The capture then actually used the trigger split, so an
out-of-budget trigger could slip past validation and run an unbudgeted SDK
capture. The fix normalizes the effective split first (trigger pre/post wins)
and budgets from that.

These tests drive ``ScopeService`` directly (no MCP subprocess) so they stay
focused on the normalization behaviour. They use the deterministic simulator and
never touch hardware.
"""

import os
import sys

import pytest

_REPO = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
if _REPO not in sys.path:
    sys.path.insert(0, _REPO)

from tools.picoscope.backend.errors import InvalidConfigError  # noqa: E402
from tools.picoscope.mcp.config import ServerConfig  # noqa: E402
from tools.picoscope.mcp.schema import AcquisitionProfile  # noqa: E402
from tools.picoscope.mcp.service import ScopeService  # noqa: E402

_ANALOG = [{"channel": "A", "enabled": True, "range_v": 5.0, "coupling": "DC"}]


def _service(tmp_path, **limits):
    cfg = ServerConfig(
        evidence_dir=str(tmp_path / "evidence"),
        allow_real_backend=False,
        **limits,
    )
    return ScopeService(cfg)


def test_trigger_split_over_budget_is_rejected(tmp_path):
    """A trigger whose pre/post exceeds max_sample_count is rejected up front.

    The top-level split is small (in budget); only the trigger split is over
    budget. The fix must reject this before any SDK call.
    """
    service = _service(tmp_path, max_sample_count=5_000)
    profile = AcquisitionProfile(
        backend="simulator",
        analog_channels=_ANALOG,
        pre_samples=0,
        post_samples=1_000,  # small: would pass if budget used the top-level split
        trigger={
            "source": "A",
            "threshold": 0.0,
            "edge": "rising",
            "pre_samples": 0,
            "post_samples": 100_000,  # over budget
        },
    )
    with pytest.raises(InvalidConfigError) as exc_info:
        service.capture_block(profile)
    assert "sample_count" in str(exc_info.value)


def test_trigger_split_memory_over_budget_is_rejected(tmp_path):
    """The memory budget is also computed from the effective trigger split."""
    # 1 analog channel => 8 bytes/sample. 200_000 samples => 1_600_000 bytes.
    service = _service(tmp_path, max_memory_bytes=1_000_000)
    profile = AcquisitionProfile(
        backend="simulator",
        analog_channels=_ANALOG,
        pre_samples=0,
        post_samples=1_000,
        trigger={
            "source": "A",
            "threshold": 0.0,
            "edge": "rising",
            "pre_samples": 0,
            "post_samples": 200_000,
        },
    )
    with pytest.raises(InvalidConfigError) as exc_info:
        service.capture_block(profile)
    assert "memory" in str(exc_info.value)


def test_no_trigger_top_level_split_still_budgeted(tmp_path):
    """Without a trigger, the top-level split remains the budgeted split."""
    service = _service(tmp_path, max_sample_count=5_000)
    profile = AcquisitionProfile(
        backend="simulator",
        analog_channels=_ANALOG,
        pre_samples=0,
        post_samples=100_000,  # over budget via the top-level split
    )
    with pytest.raises(InvalidConfigError) as exc_info:
        service.capture_block(profile)
    assert "sample_count" in str(exc_info.value)


def test_effective_trigger_split_is_what_is_captured(tmp_path):
    """Positive control: an in-budget trigger split is captured exactly.

    The produced capture's ``sample_count`` must equal the trigger's
    ``pre_samples + post_samples`` (not the top-level split), proving the
    normalized effective split is the one both budgeted and captured.
    """
    service = _service(tmp_path)
    trigger_pre, trigger_post = 200, 800
    profile = AcquisitionProfile(
        backend="simulator",
        analog_channels=_ANALOG,
        pre_samples=0,
        post_samples=1,  # ignored because a trigger is supplied
        trigger={
            "source": "A",
            "threshold": 0.0,
            "edge": "rising",
            "pre_samples": trigger_pre,
            "post_samples": trigger_post,
        },
    )
    result = service.capture_block(profile)
    assert result["result"] == "ok"
    assert result["sample_count"] == trigger_pre + trigger_post
    assert result["trigger_status"] == "triggered"
