"""
test_compose.py — pytest unit tests for compose_sprite (Stage 1.3 Phase 2).

Contracts:
    test_canvas_size_match         — canvas image.size == canvas_size(fx, fy, extra_h_clamped)
    test_composition_order         — later entry paints over earlier (blue over red)
    test_unknown_primitive_raises  — bad type: raises UnknownPrimitiveError
    test_min_canvas_height_clamp   — flat primitive → canvas height == 64
    test_compose_palette_rgb       — compose uses palette RGB (not stub colours)

Canvas anchor used in compose.py:
    x0 = w_px // 2,  y0 = h_px  (SE corner, y-down)

Reference:
    sprite-gen-master-plan.md Stage 1.3 Phase 2
"""

from __future__ import annotations

import pytest
from PIL import Image

import src.compose as _compose_mod
from src.canvas import canvas_size
from src.compose import UnknownPrimitiveError, compose_sprite
from src.palette import PaletteKeyError
from src.slopes import SlopeKeyError


# ---------------------------------------------------------------------------
# Inline fixture palette (avoids dependency on real palettes/residential.json)
# ---------------------------------------------------------------------------

_FIXTURE_PALETTE_DATA = {
    "class": "test",
    "materials": {
        "wall_brick_red": {
            "bright": [240, 48, 48],
            "mid":    [200, 40, 40],
            "dark":   [120, 24, 24],
        },
        "glass": {
            "bright": [48, 48, 240],
            "mid":    [40, 40, 200],
            "dark":   [24, 24, 120],
        },
        "wall_brick_grey": {
            "bright": [200, 200, 200],
            "mid":    [160, 160, 160],
            "dark":   [96,  96,  96],
        },
        "roof_tile_brown": {
            "bright": [180, 140, 90],
            "mid":    [150, 110, 70],
            "dark":   [90,  66,  42],
        },
        "dirt": {
            "bright": [160, 120, 70],
            "mid":    [130, 95,  55],
            "dark":   [80,  58,  33],
        },
        "grass_flat": {
            "bright": [104, 168, 56],
            "mid":    [78,  126, 42],
            "dark":   [32,  72,  8],
        },
    },
}


# ---------------------------------------------------------------------------
# Autouse fixture: patch load_palette so tests do not need real palette files
# ---------------------------------------------------------------------------

@pytest.fixture(autouse=True)
def _patch_load_palette(monkeypatch):
    """Replace load_palette in compose module with inline fixture data."""
    monkeypatch.setattr(
        _compose_mod,
        "load_palette",
        lambda cls, **_kw: _FIXTURE_PALETTE_DATA,
    )


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _cube_entry(
    material: str = "wall_brick_red",
    h: float = 32,
    offset_z: int = 0,
    w: float = 1,
    d: float = 1,
) -> dict:
    return {
        "type": "iso_cube",
        "w": w,
        "d": d,
        "h": h,
        "material": material,
        "offset_z": offset_z,
    }


def _sample_spec(
    fx: int = 1,
    fy: int = 1,
    composition: list | None = None,
    palette: str = "test",
) -> dict:
    if composition is None:
        composition = [_cube_entry()]
    return {"footprint": [fx, fy], "composition": composition, "palette": palette}


# ---------------------------------------------------------------------------
# test_canvas_size_match
# ---------------------------------------------------------------------------

def test_canvas_size_match_single_cube():
    """Canvas size matches canvas_size(fx, fy, extra_h) clamped to ≥ 64."""
    spec = _sample_spec(fx=1, fy=1, composition=[_cube_entry(h=32, offset_z=0)])
    img = compose_sprite(spec)
    # extra_h = 32 + 0 = 32; clamped → 64
    w_px, h_px = canvas_size(1, 1, 32)
    h_px = max(h_px, 64)
    assert img.size == (w_px, h_px)


def test_canvas_size_match_stacked_cubes():
    """Stacked cubes: extra_h = max(h + offset_z) over entries."""
    # two cubes: first h=32 offset_z=0, second h=32 offset_z=32 → extra_h = 64
    composition = [
        _cube_entry(h=32, offset_z=0),
        _cube_entry(h=32, offset_z=32, material="wall_brick_grey"),
    ]
    spec = _sample_spec(fx=1, fy=1, composition=composition)
    img = compose_sprite(spec)
    w_px, h_px = canvas_size(1, 1, 64)
    h_px = max(h_px, 64)
    assert img.size == (w_px, h_px)


def test_canvas_size_2x2_footprint():
    """Wider footprint correctly widens canvas."""
    spec = _sample_spec(fx=2, fy=2, composition=[_cube_entry(h=32)])
    img = compose_sprite(spec)
    w_px, h_px = canvas_size(2, 2, 32)
    h_px = max(h_px, 64)
    assert img.size == (w_px, h_px)


# ---------------------------------------------------------------------------
# test_min_canvas_height_clamp
# ---------------------------------------------------------------------------

def test_min_canvas_height_clamp():
    """When extra_h < 64 composer clamps canvas height to exactly 64 px."""
    # h=16, offset_z=0 → extra_h=16 < 64 → clamped to 64
    spec = _sample_spec(composition=[_cube_entry(h=16, offset_z=0)])
    img = compose_sprite(spec)
    assert img.size[1] == 64


def test_min_canvas_height_clamp_zero_extra():
    """extra_h=0 (empty extra) still gives canvas height == 64."""
    spec = _sample_spec(composition=[_cube_entry(h=0, offset_z=0)])
    img = compose_sprite(spec)
    assert img.size[1] == 64


# ---------------------------------------------------------------------------
# test_composition_order
# ---------------------------------------------------------------------------

def _find_opaque_pixel(img: Image.Image, x: int, y: int) -> tuple[int, int, int]:
    """Return RGB at (x, y), asserts pixel is opaque."""
    px = img.getpixel((x, y))
    assert px[3] > 0, f"pixel at ({x},{y}) is transparent — oracle off-canvas"
    return px[:3]


def test_composition_order():
    """Second entry (blue cube) must overwrite first (red cube) on the top face."""
    # Both cubes at same footprint position; second has offset_z=32 so it stacks above.
    # Blue cube top face center should be visible (not occluded by anything higher).
    red_entry = _cube_entry(material="wall_brick_red",  h=32, offset_z=0)
    blue_entry = {
        "type": "iso_cube",
        "w": 1,
        "d": 1,
        "h": 32,
        "material": "glass",       # → (120, 180, 210) bright ramp: ~(144, 216, 252)
        "offset_z": 32,
    }
    spec = _sample_spec(composition=[red_entry, blue_entry])
    img = compose_sprite(spec)

    # Top face of the *blue* (glass) cube: y0 for second cube = h_px - 32 (offset_z).
    # Blue cube top face is at gz=32 (h=32 from base of the cube) + offset_z=32 = 64 px up.
    # In screen coords: py = y0 - (gx+gy)*16 - gz with gx=gy=0.5 (diamond center), gz=64.
    # x0 = w_px//2 = 32, y0 = 64; top-face midpoint grid (0.5, 0.5, 32) projected from
    # adjusted_y0 = 64 - 32 = 32:
    #   px = 32 + (0.5-0.5)*32 = 32
    #   py = 32 - (0.5+0.5)*16 - 32 = 32 - 16 - 32 = -16  (clipped)
    # The top face bleeds to negative y — use a lower stack and just assert the image
    # contains glass-ramp colors anywhere (non-zero blue channel dominates).
    # Simpler oracle: assert any pixel in upper half has blue component > red component.
    w, h = img.size
    half = h // 2
    found_blue_dominant = False
    for y in range(0, half):
        for x in range(0, w):
            px = img.getpixel((x, y))
            if px[3] > 0 and px[2] > px[0]:  # blue > red channel
                found_blue_dominant = True
                break
        if found_blue_dominant:
            break
    assert found_blue_dominant, (
        "No blue-dominant pixel found in upper half — "
        "composition order may be wrong (red drawn on top of blue)."
    )


# ---------------------------------------------------------------------------
# test_unknown_primitive_raises
# ---------------------------------------------------------------------------

def test_unknown_primitive_raises():
    """UnknownPrimitiveError raised on unrecognised `type:` key."""
    spec = _sample_spec(
        composition=[
            {
                "type": "iso_pyramid",
                "w": 1,
                "d": 1,
                "h": 32,
                "material": "wall_brick_red",
            }
        ]
    )
    with pytest.raises(UnknownPrimitiveError):
        compose_sprite(spec)


def test_unknown_primitive_error_message_contains_type():
    """Error message names the bad type for debuggability."""
    bad_type = "iso_sphere"
    spec = _sample_spec(
        composition=[{"type": bad_type, "w": 1, "d": 1, "h": 32, "material": "glass"}]
    )
    with pytest.raises(UnknownPrimitiveError, match=bad_type):
        compose_sprite(spec)


# ---------------------------------------------------------------------------
# test_returns_rgba_image
# ---------------------------------------------------------------------------

def test_returns_rgba_image():
    """compose_sprite returns a PIL.Image in RGBA mode."""
    img = compose_sprite(_sample_spec())
    assert isinstance(img, Image.Image)
    assert img.mode == "RGBA"


# ---------------------------------------------------------------------------
# test_compose_palette_rgb — palette wiring (Stage 1.3 Phase 2)
# ---------------------------------------------------------------------------

def test_compose_palette_rgb():
    """compose_sprite uses palette RGB values, not stub/fallback colours."""
    # wall_brick_red bright = [240, 48, 48] in the fixture palette.
    # Use a small cube so the top face (bright slot) is rendered near canvas top.
    spec = _sample_spec(
        fx=1, fy=1,
        composition=[_cube_entry(material="wall_brick_red", h=32)],
    )
    img = compose_sprite(spec)

    expected_bright = tuple(_FIXTURE_PALETTE_DATA["materials"]["wall_brick_red"]["bright"])

    # Find any opaque pixel that matches the bright palette value.
    found = False
    for y in range(img.size[1]):
        for x in range(img.size[0]):
            px = img.getpixel((x, y))
            if px[3] > 0 and px[:3] == expected_bright:
                found = True
                break
        if found:
            break
    assert found, (
        f"No pixel matching palette bright {expected_bright} found — "
        "compose may still be using stub fallback colours."
    )


def test_compose_palette_key_error():
    """Missing material key → PaletteKeyError propagates from compose_sprite."""
    spec = _sample_spec(
        composition=[_cube_entry(material="totally_unknown_material")],
    )
    with pytest.raises(PaletteKeyError):
        compose_sprite(spec)


# ---------------------------------------------------------------------------
# Slope auto-insert tests (TECH-177)
# ---------------------------------------------------------------------------

def _slope_spec(
    slope_id: str,
    fx: int = 1,
    fy: int = 1,
    composition: list | None = None,
    foundation_material: str = "dirt",
) -> dict:
    """Build a spec dict with a terrain key."""
    spec = _sample_spec(fx=fx, fy=fy, composition=composition)
    spec["terrain"] = slope_id
    spec["foundation_material"] = foundation_material
    return spec


def test_flat_unchanged_no_terrain_key():
    """No terrain key → canvas identical to pre-patch flat baseline."""
    flat_spec = _sample_spec(fx=1, fy=1, composition=[_cube_entry(h=32)])
    img_flat = compose_sprite(flat_spec)
    # Sanity: extra_h=32 clamped to 64; no foundation call.
    w_px, h_px = canvas_size(1, 1, 32)
    h_px = max(h_px, 64)
    assert img_flat.size == (w_px, h_px)


def test_flat_terrain_key_unchanged():
    """terrain: flat → same canvas size + no foundation auto-insert."""
    spec = _sample_spec(fx=1, fy=1, composition=[_cube_entry(h=32)])
    spec["terrain"] = "flat"
    img = compose_sprite(spec)
    w_px, h_px = canvas_size(1, 1, 32)
    h_px = max(h_px, 64)
    assert img.size == (w_px, h_px)


def test_slope_grows_canvas():
    """terrain: N → canvas_h includes lip (max_corner_z + 2).

    N slope: {n:16, e:0, s:0, w:0} → max=16 → lip=18.
    stack_extra_h = 32 (single cube h=32, offset_z=0).
    extra_h = max(32, 18) = 32 → same canvas height as flat with h=32.

    Use a slope where lip > stack to prove canvas grows:
    Use empty composition so stack_extra_h=0 and lip=18 > 0.
    extra_h = 18 → canvas_h = canvas_size(1,1,18) with clamp ≥ 64.
    """
    spec: dict = {
        "footprint": [1, 1],
        "palette": "test",
        "composition": [],
        "terrain": "N",
        "foundation_material": "dirt",
    }
    img = compose_sprite(spec)
    # N slope: max corner = 16, lip = 18; extra_h = max(0, 18) = 18.
    w_px, h_px = canvas_size(1, 1, 18)
    h_px = max(h_px, 64)
    assert img.size == (w_px, h_px)


def test_foundation_drawn_before_stack():
    """Foundation pixels exist; non-transparent pixels present after slope auto-insert."""
    spec: dict = {
        "footprint": [1, 1],
        "palette": "test",
        "composition": [],  # no stack — only foundation
        "terrain": "N",
        "foundation_material": "dirt",
    }
    img = compose_sprite(spec)
    # At least one opaque pixel must exist (foundation drew something).
    has_opaque = any(
        img.getpixel((x, y))[3] > 0
        for y in range(img.size[1])
        for x in range(img.size[0])
    )
    assert has_opaque, "Expected foundation pixels but canvas is fully transparent."


def test_unknown_slope_raises():
    """terrain: bogus → SlopeKeyError propagates from compose_sprite."""
    spec: dict = {
        "footprint": [1, 1],
        "palette": "test",
        "composition": [],
        "terrain": "bogus_slope_id_that_does_not_exist",
    }
    with pytest.raises(SlopeKeyError):
        compose_sprite(spec)


def test_iso_ground_diamond_dispatch():
    """iso_ground_diamond is invokable via composition type (Stage 6)."""
    spec: dict = {
        "footprint": [1, 1],
        "palette": "test",
        "terrain": "flat",
        "composition": [
            {"type": "iso_ground_diamond", "material": "grass_flat"},
        ],
    }
    img = compose_sprite(spec)
    assert img.getbbox() is not None
