"""
test_canvas.py — pytest coverage for src/canvas.py canvas_size + pivot_uv.

Oracle: docs/isometric-sprite-generator-exploration.md §4 Examples table.
"""

import pytest

from src.canvas import canvas_size, pivot_uv


# ---------------------------------------------------------------------------
# canvas_size — §4 Examples rows
# ---------------------------------------------------------------------------


def test_canvas_size_1x1_flat():
    """1×1 flat tile: width=64, height=0 (no extra_h)."""
    assert canvas_size(1, 1) == (64, 0)


def test_canvas_size_1x1_small_house():
    """1×1 small house: 32 px extra height."""
    assert canvas_size(1, 1, 32) == (64, 32)


def test_canvas_size_3x3_nuclear_plant():
    """3×3 nuclear plant: 96 px extra height."""
    assert canvas_size(3, 3, 96) == (192, 96)


# ---------------------------------------------------------------------------
# pivot_uv — §4 Examples rows
# ---------------------------------------------------------------------------


def test_pivot_uv_64_small_house():
    """canvas_h=64 → (0.5, 0.25) — matches Unity PPU=64 import default."""
    assert pivot_uv(64) == (0.5, 0.25)


def test_pivot_uv_128_skyscraper():
    """canvas_h=128 → (0.5, 0.125)."""
    assert pivot_uv(128) == (0.5, 0.125)


def test_pivot_uv_192_nuclear_plant():
    """canvas_h=192 → (0.5, 16/192) — exact expression both sides."""
    assert pivot_uv(192) == (0.5, 16 / 192)


# ---------------------------------------------------------------------------
# Guard — div-by-zero contract (§5.3)
# ---------------------------------------------------------------------------


def test_pivot_uv_zero_raises():
    """pivot_uv(0) must raise ValueError — locks composer bug guard."""
    with pytest.raises(ValueError):
        pivot_uv(0)
