"""
slopes.py — Cached loader for tools/sprite-gen/slopes.yaml.

Provides:
    load_slopes()    — lru_cache-backed dict of slope_id -> {n, e, s, w}.
    get_corner_z()   — per-corner Z lookup; raises SlopeKeyError on unknown id.
    SlopeKeyError    — raised when slope_id not in slopes.yaml.

Reference:
    docs/isometric-sprite-generator-exploration.md §7 (slope variant naming)
    ia/specs/isometric-geography-system.md §6.4
"""

from __future__ import annotations

from functools import lru_cache
from pathlib import Path

import yaml


class SlopeKeyError(KeyError):
    """Slope id not found in slopes.yaml.

    Message format: "unknown slope_id 'X'; available: [...]"
    """


@lru_cache(maxsize=1)
def load_slopes() -> dict[str, dict[str, int]]:
    """Return the parsed slopes.yaml as a dict, cached at module level.

    Returns:
        Mapping of slope_id -> {n: int, e: int, s: int, w: int}.

    Raises:
        FileNotFoundError: If slopes.yaml is missing relative to this module.
    """
    path = Path(__file__).parent.parent / "slopes.yaml"
    return yaml.safe_load(path.read_text(encoding="utf-8"))


def get_corner_z(slope_id: str) -> dict[str, int]:
    """Return {n, e, s, w} corner Z offsets (pixels) for *slope_id*.

    Args:
        slope_id: Key into slopes.yaml (e.g. ``"NE-up"``, ``"flat"``).

    Returns:
        Dict with keys ``"n"``, ``"e"``, ``"s"``, ``"w"``; values in ``{0, 16}``.

    Raises:
        SlopeKeyError: If *slope_id* is not present in slopes.yaml.
    """
    slopes = load_slopes()
    if slope_id not in slopes:
        available = sorted(slopes.keys())
        raise SlopeKeyError(
            f"unknown slope_id {slope_id!r}; available: {available}"
        )
    return slopes[slope_id]
