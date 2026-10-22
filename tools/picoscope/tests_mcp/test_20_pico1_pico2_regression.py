"""Category 20: PICO-1 / PICO-2 regression (123 offline tests) still passes."""

import subprocess
import sys

from tools.picoscope.tests_mcp.mcp_client import REPO


def test_pico1_pico2_regression_still_passes():
    """Run the PICO-1/PICO-2 offline suite unchanged and require 123 passed."""
    proc = subprocess.run(
        [sys.executable, "-m", "pytest", "tools/picoscope/tests", "-q", "--no-header"],
        cwd=REPO,
        capture_output=True,
        text=True,
        timeout=180,
    )
    combined = proc.stdout + "\n" + proc.stderr
    assert proc.returncode == 0, f"PICO-1/PICO-2 regression failed:\n{combined}"
    assert "123 passed" in combined, f"expected 123 passed, got:\n{combined}"
