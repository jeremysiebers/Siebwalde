"""Category 4: analog channel configuration."""

import pytest

from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.errors import InvalidConfigError
from tools.picoscope.backend.model import ANALOG_CHANNELS


def test_configure_analog_channel_roundtrip():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        cfg = scope.config.analog_channels
        assert len(cfg) == 1
        assert cfg[0].channel == "A"
        assert cfg[0].enabled is True
        assert cfg[0].range_v == 5.0
        assert cfg[0].coupling == "DC"


def test_configure_analog_is_idempotent_for_same_channel():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_analog_channel("A", False, 2.0, "AC")
        cfg = [c for c in scope.config.analog_channels if c.channel == "A"]
        assert len(cfg) == 1
        assert cfg[0].enabled is False
        assert cfg[0].range_v == 2.0
        assert cfg[0].coupling == "AC"


@pytest.mark.parametrize("channel", ["A", "B", "C", "D"])
def test_all_analog_channels_accept(channel):
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel(channel, True, 1.0, "DC")


@pytest.mark.parametrize("bad", ["E", "", "a", None])
def test_invalid_channel_rejected(bad):
    with open_device("SIM0001", backend="simulator") as scope:
        with pytest.raises(InvalidConfigError):
            scope.configure_analog_channel(bad, True, 1.0, "DC")


@pytest.mark.parametrize("bad_range", [0.0, -1.0, float("nan"), float("inf")])
def test_invalid_range_rejected(bad_range):
    with open_device("SIM0001", backend="simulator") as scope:
        with pytest.raises(InvalidConfigError):
            scope.configure_analog_channel("A", True, bad_range, "DC")


@pytest.mark.parametrize("bad_coupling", ["XX", "ac", "", None])
def test_invalid_coupling_rejected(bad_coupling):
    with open_device("SIM0001", backend="simulator") as scope:
        with pytest.raises(InvalidConfigError):
            scope.configure_analog_channel("A", True, 1.0, bad_coupling)
