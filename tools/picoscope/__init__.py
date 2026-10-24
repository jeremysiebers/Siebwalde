"""Siebwalde PicoScope measurement package (Layer A backend + simulator).

This is a measurement-only, offline-first Python package. The public surface is
``tools.picoscope.backend.api``; see ``README.md`` and ``SAFETY.md``.

Version info lives in ``version.py``.
"""

from .version import __version__, __tool_name__, git_commit, tool_version  # noqa: F401

__all__ = ["__version__", "__tool_name__", "git_commit", "tool_version"]
