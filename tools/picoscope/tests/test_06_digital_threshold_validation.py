"""Category 6: digital threshold validation."""

import pytest

from tools.picoscope.backend.api import open_device
from tools.picoscope.backend.errors import InvalidConfigError


@pytest.mark.parametrize("threshold_mv", [1, 1000, 2500, 5000, 1000.5])
def test_valid_thresholds_accepted(threshold_mv):
    with open_device("SIM0001", backend="simulator") as scope:
        scope.configure_digital_port(0, True, threshold_mv)


@pytest.mark.parametrize(
    "bad",
    [0, -100, 5000.1, 6000, float("nan"), float("inf"), float("-inf"), "1000", None],
)
def test_invalid_thresholds_rejected(bad):
    with open_device("SIM0001", backend="simulator") as scope:
        with pytest.raises(InvalidConfigError):
            scope.configure_digital_port(0, True, bad)
