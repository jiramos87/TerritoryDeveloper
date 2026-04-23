"""TECH-713 — Placement matrix tests for `resolve_building_box`.

Matrix: 5 aligns × 3 padding profiles = 15 parametrized cases. Uses a
64×64 canvas with `footprint_px = [28, 28]` so all anchors produce
deterministic, non-trivial offsets.
"""

from __future__ import annotations

import pytest

from src.compose import resolve_building_box

_ALIGNS = ["center", "sw", "ne", "nw", "se"]

_PADDINGS = [
    {"n": 0, "e": 0, "s": 0, "w": 0},
    {"n": 4, "e": 0, "s": 0, "w": 0},
    {"n": 0, "e": 0, "s": 10, "w": 0},
]


def _spec(align: str, padding: dict) -> dict:
    return {
        "class": "residential_small",
        "footprint": [1, 1],
        "canvas": [64, 64],
        "building": {
            "footprint_px": [28, 28],
            "padding": padding,
            "align": align,
        },
        "composition": [],
    }


@pytest.mark.parametrize("align", _ALIGNS)
@pytest.mark.parametrize("padding", _PADDINGS)
def test_placement_matrix_box_size(align: str, padding: dict) -> None:
    """Mass bbox always echoes `footprint_px` regardless of anchor."""
    bx, by, _, _ = resolve_building_box(_spec(align, padding))
    assert (bx, by) == (28, 28)


@pytest.mark.parametrize("align", _ALIGNS)
@pytest.mark.parametrize("padding", _PADDINGS)
def test_placement_matrix_offsets_deterministic(align: str, padding: dict) -> None:
    """Same (align, padding) always maps to same (ox, oy)."""
    a = resolve_building_box(_spec(align, padding))
    b = resolve_building_box(_spec(align, padding))
    assert a == b


def test_center_zero_padding_no_shift() -> None:
    """Default path must be byte-identical to legacy (no offset)."""
    _, _, ox, oy = resolve_building_box(_spec("center", {"n": 0, "e": 0, "s": 0, "w": 0}))
    assert (ox, oy) == (0, 0)


def test_sw_anchor_shifts_west_and_south() -> None:
    _, _, ox, oy = resolve_building_box(_spec("sw", {"n": 0, "e": 0, "s": 0, "w": 0}))
    assert ox < 0
    assert oy > 0


def test_ne_anchor_shifts_east_and_north() -> None:
    _, _, ox, oy = resolve_building_box(_spec("ne", {"n": 0, "e": 0, "s": 0, "w": 0}))
    assert ox > 0
    assert oy < 0


def test_padding_w_pushes_east() -> None:
    """`padding.w` (gap on west edge) shifts mass eastward (ox += w)."""
    _, _, ox0, _ = resolve_building_box(_spec("center", {"n": 0, "e": 0, "s": 0, "w": 0}))
    _, _, ox1, _ = resolve_building_box(_spec("center", {"n": 0, "e": 0, "s": 0, "w": 5}))
    assert ox1 - ox0 == 5


def test_padding_s_pulls_up() -> None:
    """`padding.s` (gap on south edge) shifts mass northward (oy -= s)."""
    _, _, _, oy0 = resolve_building_box(_spec("center", {"n": 0, "e": 0, "s": 0, "w": 0}))
    _, _, _, oy1 = resolve_building_box(_spec("center", {"n": 0, "e": 0, "s": 10, "w": 0}))
    assert oy1 - oy0 == -10


def test_footprint_ratio_fallback() -> None:
    """No `footprint_px` + explicit ratio → bbox = canvas × ratio."""
    spec = {
        "class": "residential_small",
        "footprint": [1, 1],
        "canvas": [64, 64],
        "building": {
            "footprint_ratio": [0.5, 0.25],
            "padding": {"n": 0, "e": 0, "s": 0, "w": 0},
            "align": "center",
        },
        "composition": [],
    }
    bx, by, _, _ = resolve_building_box(spec)
    assert (bx, by) == (32, 16)


def test_footprint_ratio_default_from_class() -> None:
    """No px + no ratio → class default (residential_small → 0.45/0.45)."""
    spec = {
        "class": "residential_small",
        "footprint": [1, 1],
        "canvas": [64, 64],
        "building": {
            "padding": {"n": 0, "e": 0, "s": 0, "w": 0},
            "align": "center",
        },
        "composition": [],
    }
    bx, by, _, _ = resolve_building_box(spec)
    assert (bx, by) == (round(64 * 0.45), round(64 * 0.45))
