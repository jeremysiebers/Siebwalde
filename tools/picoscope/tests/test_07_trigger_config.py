"""Category 7: trigger configuration."""

import pytest

from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.errors import InvalidConfigError


def test_configure_trigger_roundtrip():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_trigger("A", 1.0, "rising", pre_samples=500, post_samples=1500)
        cfg = scope.config
        assert cfg.trigger.source == "A"
        assert cfg.trigger.threshold == 1.0
        assert cfg.trigger.edge == "rising"
        assert cfg.pre_samples == 500
        assert cfg.post_samples == 1500


def test_trigger_position_equals_pre_samples():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_trigger("A", 0.0, "rising", pre_samples=400, post_samples=1600)
        cap = scope.capture_block()
        assert cap.trigger_position == 400
        assert cap.sample_count == 2000


@pytest.mark.parametrize("bad_edge", ["up", "down", "RISING", "", None])
def test_invalid_edge_rejected(bad_edge):
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        with pytest.raises(InvalidConfigError):
            scope.configure_trigger("A", 0.0, bad_edge, 0, 100)


def test_trigger_source_must_be_enabled():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_trigger("B", 0.0, "rising", 0, 100)
        # Source validity is checked at capture time (config is validated when used).
        with pytest.raises(InvalidConfigError):
            scope.capture_block()
