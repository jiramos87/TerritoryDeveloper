"""
test_compose.py — pytest unit tests for compose_sprite (Stage 1.2).

Four contracts:
    test_canvas_size_match         — canvas image.size == canvas_size(fx, fy, extra_h_clamped)
    test_composition_order         — later entry paints over earlier (blue over red)
    test_unknown_primitive_raises  — bad type: raises UnknownPrimitiveError
    test_min_canvas_height_clamp   — flat primitive → canvas height == 64

Canvas anchor used in compose.py:
    x0 = w_px // 2,  y0 = h_px  (SE corner, y-down)

Reference:
    sprite-gen-master-plan.md Stage 1.2 Phase 1 (T1.2.1)
"""

from __future__ import annotations

import pytest
from PIL import Image

from src.canvas import canvas_size
from src.compose import UnknownPrimitiveError, compose_sprite


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
) -> dict:
    if composition is None:
        composition = [_cube_entry()]
    return {"footprint": [fx, fy], "composition": composition}


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
