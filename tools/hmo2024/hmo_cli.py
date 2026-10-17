#!/usr/bin/env python3
"""Entry point for the HMO2024 measurement CLI.

Run from anywhere:

    python tools/hmo2024/hmo_cli.py inventory --port COM3
    python -m hmo2024.hmo_cli inventory --port COM3   # from tools/ dir

The module inserts its own directory onto ``sys.path`` so the flat sibling
modules (``transport``, ``scpi``, ...) resolve regardless of the invocation
style.
"""

import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
if _HERE not in sys.path:
    sys.path.insert(0, _HERE)

# Import after path insertion so the flat modules resolve.
import cli  # noqa: E402


if __name__ == "__main__":
    sys.exit(cli.main())
