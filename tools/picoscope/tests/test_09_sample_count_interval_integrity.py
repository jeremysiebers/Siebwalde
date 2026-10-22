"""Category 9: sample count + interval integrity."""

import pytest

from tools.picoscope.backend.api import open_device


@pytest.mark.parametrize("pre,post", [(0, 100), (500, 1500), (2000, 8000)])
def test_sample_count_equals_pre_plus_post(pre, post):
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_trigger("A", 0.0, "rising", pre, post)
        cap = scope.capture_block()
        assert cap.sample_count == pre + post
        for arr in cap.analog_samples.values():
            assert len(arr) == cap.sample_count
        for arr in cap.digital_samples.values():
            assert len(arr) == cap.sample_count


def test_sample_interval_is_positive_int_and_matches_timebase():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        cap0 = scope.capture_block(timebase=0)
        cap1 = scope.capture_block(timebase=1)
        assert isinstance(cap0.sample_interval_ns, int)
        assert cap0.sample_interval_ns > 0
        # Default simulator timebase table: tb0=2ns, tb1=4ns.
        assert cap1.sample_interval_ns == 2 * cap0.sample_interval_ns


def test_timing_metadata_is_self_consistent():
    from tools.picoscope.simulator import time_axis_ns

    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        cap = scope.capture_block(post_samples=1000)
        t = time_axis_ns(cap.sample_interval_ns, cap.sample_count)
        assert len(t) == cap.sample_count
        assert t[1] - t[0] == cap.sample_interval_ns
        assert t[-1] == (cap.sample_count - 1) * cap.sample_interval_ns
