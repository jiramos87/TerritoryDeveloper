"""Smoke + histogram tests for 7 task rows (T7.1-T7.6), 8 primitives.

Residential palette only (v1). Each primitive renders without exception,
produces a non-empty bbox, and dominant colour matches a palette-ramp level.

Covers: TECH-770.
"""

from __future__ import annotations

import json
from collections import Counter
from pathlib import Path

import pytest
from PIL import Image

from src.primitives import (
    iso_bush,
    iso_fence,
    iso_grass_tuft,
    iso_path,
    iso_pavement_patch,
    iso_pool,
    iso_tree_deciduous,
    iso_tree_fir,
)


_PRIMITIVES = (
    "iso_tree_fir",
    "iso_tree_deciduous",
    "iso_bush",
    "iso_grass_tuft",
    "iso_pool",
    "iso_path",
    "iso_pavement_patch",
    "iso_fence",
)

_PALETTE_KEYS = (
    "tree_fir",
    "tree_deciduous",
    "bush",
    "grass_tuft",
    "pool",
    "fence",
    "pavement",
)

_PRIMITIVE_DISPATCH = {
    "iso_tree_fir": iso_tree_fir,
    "iso_tree_deciduous": iso_tree_deciduous,
    "iso_bush": iso_bush,
    "iso_grass_tuft": iso_grass_tuft,
    "iso_pool": iso_pool,
    "iso_path": iso_path,
    "iso_pavement_patch": iso_pavement_patch,
    "iso_fence": iso_fence,
}

_PRIMITIVE_KWARGS = {
    "iso_tree_fir": {"scale": 1.0, "variant": 0},
    "iso_tree_deciduous": {"scale": 1.0, "variant": 0},
    "iso_bush": {"variant": 0},
    "iso_grass_tuft": {"variant": 0},
    "iso_pool": {"w_px": 12, "d_px": 10},
    "iso_path": {"length_px": 10, "axis": "ns", "width_px": 2},
    "iso_pavement_patch": {"w_px": 8, "d_px": 8},
    "iso_fence": {"length_px": 10, "side": "n"},
}

_EXPECTED_RAMP = {
    "iso_tree_fir": "tree_fir",
    "iso_tree_deciduous": "tree_deciduous",
    "iso_bush": "bush",
    "iso_grass_tuft": "grass_tuft",
    "iso_pool": "pool",
    "iso_path": "pavement",
    "iso_pavement_patch": "pavement",
    "iso_fence": "fence",
}


@pytest.fixture
def residential_palette() -> dict:
    path = Path(__file__).resolve().parents[1] / "palettes" / "residential.json"
    with open(path) as f:
        return json.load(f)


@pytest.fixture
def blank_canvas() -> Image.Image:
    return Image.new("RGBA", (64, 64), (0, 0, 0, 0))


def _non_transparent_rgb(canvas: Image.Image) -> list[tuple[int, int, int]]:
    """Return RGB triples of all non-transparent pixels."""
    w, h = canvas.size
    out: list[tuple[int, int, int]] = []
    for y in range(h):
        for x in range(w):
            r, g, b, a = canvas.getpixel((x, y))
            if a > 0:
                out.append((r, g, b))
    return out


def _collect_ramp_rgbs(ramp: dict) -> list[tuple[int, int, int]]:
    """Flatten palette ramp (possibly nested) into list of RGB triples."""
    out: list[tuple[int, int, int]] = []
    for value in ramp.values():
        if isinstance(value, list) and len(value) == 3 and all(isinstance(v, (int, float)) for v in value):
            out.append((int(value[0]), int(value[1]), int(value[2])))
        elif isinstance(value, dict):
            out.extend(_collect_ramp_rgbs(value))
    return out


@pytest.mark.parametrize("name", _PRIMITIVES)
def test_vegetation_smoke_all(name: str, residential_palette: dict, blank_canvas: Image.Image) -> None:
    """Each primitive renders without exception and produces a non-empty bbox."""
    fn = _PRIMITIVE_DISPATCH[name]
    kwargs = _PRIMITIVE_KWARGS[name]
    fn(blank_canvas, 32, 32, palette=residential_palette, **kwargs)
    bbox = blank_canvas.getbbox()
    assert bbox is not None, f"{name}: bbox is empty"
    x0, y0, x1, y1 = bbox
    area = (x1 - x0) * (y1 - y0)
    assert area >= 1, f"{name}: bbox area {area} < 1"


@pytest.mark.parametrize("name", _PRIMITIVES)
def test_vegetation_dominant_colour(
    name: str,
    residential_palette: dict,
    blank_canvas: Image.Image,
) -> None:
    """Dominant colour of rendered primitive matches an expected palette ramp level."""
    fn = _PRIMITIVE_DISPATCH[name]
    kwargs = _PRIMITIVE_KWARGS[name]
    fn(blank_canvas, 32, 32, palette=residential_palette, **kwargs)

    pixels = _non_transparent_rgb(blank_canvas)
    assert pixels, f"{name}: no non-transparent pixels rendered"
    counts = Counter(pixels)
    top2 = [rgb for rgb, _ in counts.most_common(2)]

    ramp = residential_palette["materials"][_EXPECTED_RAMP[name]]
    ramp_rgbs = set(_collect_ramp_rgbs(ramp))

    if name == "iso_pool":
        # Pool: top-2 must include either `bright` fill or `rim`.
        assert any(rgb in ramp_rgbs for rgb in top2), (
            f"{name}: top-2 {top2} not in pool ramp {ramp_rgbs}"
        )
    else:
        # Others: dominant (top-1) must be in ramp.
        assert top2[0] in ramp_rgbs, (
            f"{name}: dominant {top2[0]} not in {_EXPECTED_RAMP[name]} ramp {ramp_rgbs}"
        )


def test_vegetation_palette_keys_present(residential_palette: dict) -> None:
    """Residential palette exposes all 7 decoration ramp keys."""
    materials = residential_palette["materials"]
    for key in _PALETTE_KEYS:
        assert key in materials, f"palette missing key: {key}"
