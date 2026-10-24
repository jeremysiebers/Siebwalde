"""Category 16: invalid-config rejection (AcquisitionConfig level)."""

import pytest

from tools.picoscope.backend.config import AcquisitionConfig
from tools.picoscope.backend.errors import InvalidConfigError
from tools.picoscope.backend.model import (
    AnalogChannelConfig,
    DigitalPortConfig,
    TriggerConfig,
)


def _valid_config():
    return AcquisitionConfig(
        analog_channels=[AnalogChannelConfig("A", True, 5.0, "DC")],
        digital_ports=[DigitalPortConfig(0, True, 1000)],
        trigger=TriggerConfig("A", 0.0, "rising"),
        pre_samples=100,
        post_samples=900,
        timebase=0,
    )


def test_valid_config_passes():
    _valid_config().validate()


def test_negative_pre_samples_rejected():
    cfg = _valid_config()
    cfg.pre_samples = -1
    with pytest.raises(InvalidConfigError):
        cfg.validate()


def test_zero_post_samples_rejected():
    cfg = _valid_config()
    cfg.post_samples = 0
    with pytest.raises(InvalidConfigError):
        cfg.validate()


def test_negative_timebase_rejected():
    cfg = _valid_config()
    cfg.timebase = -1
    with pytest.raises(InvalidConfigError):
        cfg.validate()


def test_no_enabled_channel_rejected():
    cfg = AcquisitionConfig(
        analog_channels=[AnalogChannelConfig("A", False, 5.0, "DC")],
        digital_ports=[DigitalPortConfig(0, False, 1000)],
        pre_samples=0,
        post_samples=100,
    )
    with pytest.raises(InvalidConfigError):
        cfg.validate()


def test_trigger_source_not_enabled_rejected():
    cfg = AcquisitionConfig(
        analog_channels=[AnalogChannelConfig("A", True, 5.0, "DC")],
        digital_ports=[],
        trigger=TriggerConfig("B", 0.0, "rising"),
        pre_samples=0,
        post_samples=100,
    )
    with pytest.raises(InvalidConfigError):
        cfg.validate()


def test_bad_sample_interval_rejected():
    cfg = _valid_config()
    cfg.sample_interval_ns = 0
    with pytest.raises(InvalidConfigError):
        cfg.validate()
