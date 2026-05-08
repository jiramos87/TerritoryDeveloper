"""_errors.py — Custom exception types for the compose layer."""

from __future__ import annotations


class DecorationScopeError(ValueError):
    """Raised when decoration primitive exceeds footprint scope (e.g. iso_pool on 1x1)."""


class UnknownPrimitiveError(ValueError):
    """Raised when a composition entry `type:` is not in the dispatch dict."""
