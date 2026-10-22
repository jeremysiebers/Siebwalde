"""Category 10: multichannel sample alignment."""

import numpy as np

from tools.picoscope.backend.api import open_device


def test_analog_and_digital_share_one_sample_clock():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_analog_channel("B", True, 2.0, "DC")
        scope.configure_digital_port(0, True, 1000)
        scope.configure_digital_port(1, True, 1200)
        cap = scope.capture_block(post_samples=2000)
        lengths = {len(a) for a in cap.analog_samples.values()}
        lengths |= {len(d) for d in cap.digital_samples.values()}
        assert lengths == {cap.sample_count}


def test_identical_signal_spec_yields_identical_aligned_arrays():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_analog_channel("A", True, 5.0, "DC")
        scope.configure_analog_channel("B", True, 5.0, "DC")
        # Force both channels to the same deterministic DC signal.
        scope.device.set_signal("A", {"type": "dc", "level_v": 2.0})
        scope.device.set_signal("B", {"type": "dc", "level_v": 2.0})
        cap = scope.capture_block()
        assert np.array_equal(cap.analog_samples["A"], cap.analog_samples["B"])


def test_digital_lines_across_ports_align_by_index():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_digital_port(0, True, 1000)
        scope.configure_digital_port(1, True, 1200)
        # Same square spec on D0 (port0) and D8 (port1).
        spec = {"type": "square", "freq_hz": 1000.0, "duty": 0.5}
        scope.device.set_signal("D0", spec)
        scope.device.set_signal("D8", spec)
        cap = scope.capture_block()
        assert np.array_equal(cap.digital_samples["D0"], cap.digital_samples["D8"])


def test_known_edge_waveform_positions():
    from tools.picoscope.simulator import digital_edges

    arr = digital_edges([100, 250], n=400, initial=0)
    assert arr[99] == 0
    assert arr[100] == 1
    assert arr[249] == 1
    assert arr[250] == 0
    assert len(arr) == 400
