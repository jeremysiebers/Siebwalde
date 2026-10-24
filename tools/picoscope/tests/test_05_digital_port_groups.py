"""Category 5: BOTH digital port groups (D0..D7 and D8..D15)."""

import pytest

from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.errors import InvalidConfigError
from tools.picoscope.backend.model import digital_lines


def test_port_line_mapping():
    assert digital_lines(0) == tuple(f"D{i}" for i in range(8))
    assert digital_lines(1) == tuple(f"D{i}" for i in range(8, 16))


def test_configure_both_ports():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_digital_port(0, True, 1000)
        scope.configure_digital_port(1, True, 1200)
        ports = scope.config.digital_ports
        assert {p.port for p in ports} == {0, 1}
        p0 = next(p for p in ports if p.port == 0)
        p1 = next(p for p in ports if p.port == 1)
        assert p0.threshold_mv == 1000
        assert p1.threshold_mv == 1200


def test_capture_populates_both_port_groups():
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_digital_port(0, True, 1000)
        scope.configure_digital_port(1, True, 1200)
        cap = scope.capture_block(post_samples=200)
        assert set(cap.digital_samples) == set(digital_lines(0)) | set(digital_lines(1))
        for line in cap.digital_samples:
            assert len(cap.digital_samples[line]) == cap.sample_count


@pytest.mark.parametrize("bad_port", [-1, 2, 3, "0", None])
def test_invalid_port_rejected(bad_port):
    with open_device("SIM0001", backend="simulator") as scope:
        with pytest.raises(InvalidConfigError):
            scope.configure_digital_port(bad_port, True, 1000)
