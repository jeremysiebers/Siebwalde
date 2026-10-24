"""Category 23: simulator digital edge-trigger emulation (deterministic).

The simulator emulates digital-edge triggering on D0..D15 (both ports) so the
offline suite exercises the full trigger path without hardware. A custom
``digital_edges`` signal places a known rising/falling edge; the capture window
is positioned so that edge lands exactly at the pre-trigger index. A constant
signal has no edge, so the capture reports ``trigger_status == "timeout"``.
"""

import numpy as np
import pytest

from tools.picoscope.backend.api import open_device, save_capture, load_capture


def _open(port, line, spec, edge, pre, post):
    """Open a simulator device with one digital port and a digital edge trigger."""
    scope = open_device("SIM0001", backend="simulator")
    scope.configure_digital_port(port, True, 1000)
    scope.device.set_signal(line, spec)
    scope.configure_trigger(line, 0.0, edge, pre_samples=pre, post_samples=post)
    return scope


@pytest.mark.parametrize("line,port", [("D0", 0), ("D1", 0), ("D7", 0), ("D8", 1), ("D15", 1)])
def test_rising_edge_trigger_positions_capture(line, port):
    pre, post = 100, 900
    with _open(port, line, {"type": "edges", "edge_sample_indices": [500], "initial": 0},
               "rising", pre, post) as scope:
        cap = scope.capture_block()
    assert cap.trigger_status == "triggered"
    assert cap.trigger_position == pre
    assert cap.trigger_index == pre
    assert cap.sample_count == pre + post
    assert len(cap.digital_samples[line]) == pre + post
    # The edge lands exactly at the pre-trigger index: 0 before, 1 at/after.
    assert cap.digital_samples[line][pre - 1] == 0
    assert cap.digital_samples[line][pre] == 1


def test_falling_edge_trigger_positions_capture():
    pre, post = 50, 450
    with _open(1, "D8", {"type": "edges", "edge_sample_indices": [300], "initial": 1},
               "falling", pre, post) as scope:
        cap = scope.capture_block()
    assert cap.trigger_status == "triggered"
    assert cap.trigger_index == pre
    assert cap.digital_samples["D8"][pre - 1] == 1
    assert cap.digital_samples["D8"][pre] == 0


def test_either_edge_uses_first_available_edge():
    pre, post = 40, 460
    # initial=0: a rising edge at 100, then a falling edge at 200.
    spec = {"type": "edges", "edge_sample_indices": [100, 200], "initial": 0}
    with _open(0, "D0", spec, "either", pre, post) as scope:
        cap = scope.capture_block()
    assert cap.trigger_status == "triggered"
    # The first (rising) edge is the trigger point; it lands at the pre index.
    assert cap.digital_samples["D0"][pre - 1] == 0
    assert cap.digital_samples["D0"][pre] == 1


def test_no_edge_within_horizon_times_out():
    with _open(0, "D0", {"type": "edges", "edge_sample_indices": [], "initial": 0},
               "rising", 100, 900) as scope:
        cap = scope.capture_block()
    assert cap.trigger_status == "timeout"
    assert cap.acquisition_status == "TIMEOUT"
    assert cap.trigger_position is None
    assert cap.trigger_index is None


def test_wrong_edge_direction_times_out():
    # Only a falling edge exists, but the trigger wants a rising edge.
    with _open(0, "D0", {"type": "edges", "edge_sample_indices": [500], "initial": 1},
               "rising", 100, 900) as scope:
        cap = scope.capture_block()
    assert cap.trigger_status == "timeout"


def test_device_reusable_after_digital_trigger_timeout():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_digital_port(0, True, 1000)
        scope.device.set_signal("D0", {"type": "edges", "edge_sample_indices": [], "initial": 0})
        scope.configure_trigger("D0", 0.0, "rising", 100, 900)
        first = scope.capture_block()
        assert first.trigger_status == "timeout"
        # Change the signal to provide an edge; the next capture must succeed.
        scope.device.set_signal("D0", {"type": "edges", "edge_sample_indices": [500], "initial": 0})
        second = scope.capture_block()
        assert second.trigger_status == "triggered"


def test_triggerless_capture_remains_unused_and_unchanged():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_digital_port(0, True, 1000)
        scope.configure_digital_port(1, True, 1200)
        cap = scope.capture_block(post_samples=2000)
    assert cap.trigger_status == "unused"
    assert cap.trigger_position is None
    assert cap.trigger_index is None
    assert cap.acquisition_status == "OK"
    assert set(cap.digital_samples) >= {"D0", "D7", "D8", "D15"}


def test_digital_trigger_serialization_round_trips(tmp_path):
    with _open(0, "D0", {"type": "edges", "edge_sample_indices": [500], "initial": 0},
               "rising", 100, 900) as scope:
        cap = scope.capture_block()
        save_capture(cap, str(tmp_path / "cap"))

    reloaded = load_capture(str(tmp_path / "cap"))
    assert reloaded.equals(cap)
    assert reloaded.trigger_status == "triggered"
    assert reloaded.trigger_index == cap.trigger_index


def test_digital_trigger_threshold_is_ignored_but_validated():
    # The digital trigger carries a (volts) threshold field that must remain a
    # finite number even though it is unused for digital edge detection.
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_digital_port(0, True, 1000)
        scope.device.set_signal("D0", {"type": "edges", "edge_sample_indices": [100], "initial": 0})
        scope.configure_trigger("D0", 1.5, "rising", 0, 200)
        cap = scope.capture_block()
        assert cap.trigger_status == "triggered"
        assert cap.trigger.threshold == 1.5
