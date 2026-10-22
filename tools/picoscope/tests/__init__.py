"""Offline pytest suite for the picoscope measurement package."""

import os
import sys

# Ensure the repository root is importable regardless of how pytest is invoked.
_repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
if _repo_root not in sys.path:
    sys.path.insert(0, _repo_root)
