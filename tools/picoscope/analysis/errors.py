"""Exception taxonomy for the offline signal-analysis layer."""

from __future__ import annotations


class AnalysisError(Exception):
    """Base error for the analysis layer."""


class InvalidAnalysisConfig(AnalysisError):
    """A decoder/analyzer configuration or setting is structurally invalid."""


class MissingChannel(AnalysisError):
    """A requested channel/line is absent from the capture."""
