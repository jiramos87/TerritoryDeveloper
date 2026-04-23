"""Stage 6 constants (TECH-696)."""

from __future__ import annotations

from src.constants import LEVEL_H
from src.spec import _DEFAULT_GROUND


def test_level_h_keys_cover_default_ground():
    for k in _DEFAULT_GROUND:
        assert k in LEVEL_H, f"LEVEL_H missing {k}"
