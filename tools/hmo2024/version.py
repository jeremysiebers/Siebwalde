"""Tool version and repository provenance (flat module)."""

import os
import subprocess

__version__ = "0.1.0"
__tool_name__ = "hmo2024"

_REPO_ROOT = os.path.abspath(
    os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
)


def git_commit():
    """Best-effort repository HEAD (environment provenance, not tool truth)."""
    try:
        out = subprocess.run(
            ["git", "-C", _REPO_ROOT, "rev-parse", "HEAD"],
            capture_output=True,
            text=True,
            timeout=5,
            check=True,
        )
        return out.stdout.strip()
    except Exception:
        return None


def tool_version():
    c = git_commit()
    if c:
        return f"{__tool_name__} {__version__} (repo HEAD {c})"
    return f"{__tool_name__} {__version__} (repo HEAD unknown)"
