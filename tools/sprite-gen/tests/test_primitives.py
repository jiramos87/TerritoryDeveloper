"""
test_primitives.py — pytest smoke tests for iso_cube and iso_prism.

Oracle geometry: 1×1 footprint, h=32 px, canvas_size(1, 1, 64) = (64, 64).
  extra_h=64 used (not 32 as originally drafted in spec) — see Issues §9#1.

Canvas placement: x0 = w_px // 2 + 16 = 48, y0 = h_px - 1 = 63.
  This puts the footprint SE corner at canvas bottom-right quadrant and
  ensures all three iso_cube faces project within the 64×64 canvas.

Face bbox notes (y-down, origin top-left):
  iso_cube 1×1×32:
    top    (bright) — roughly upper half: y  0..31,  x 16..63
    south  (mid)    — lower-left region:  y 16..63,  x 16..48
    east   (dark)   — lower-right region: y 16..63,  x 48..63

  iso_prism 1×1×32 pitch=0.5 axis='ns':
    slope_bright (NW-facing) — same region as cube top + south-left
    slope_mid    (SE-facing) — mid-left region
    gables       (dark)      — end-cap triangles at south and north

  iso_prism 1×1×32 pitch=0.5 axis='ew':
    slope_bright (N-facing)  — upper-left region
    slope_mid    (S-facing)  — lower region
    gables       (dark)      — end-cap triangles at east and west

Palette: tests use a minimal inline fixture palette so they do not depend
on the real ``palettes/residential.json``.
"""

from __future__ import annotations

import pathlib
import warnings

import pytest
from PIL import Image, ImageChops

from src.canvas import canvas_size
from src.primitives import iso_cube, iso_prism

# ---------------------------------------------------------------------------
# Shared constants
# ---------------------------------------------------------------------------

# Minimal inline fixture palette — provides bright/mid/dark for two materials.
_FIXTURE_PALETTE: dict = {
    "class": "test",
    "materials": {
        "stub_red": {
            "bright": [240, 48, 48],
            "mid":    [200, 40, 40],
            "dark":   [120, 24, 24],
        },
        "stub_blue": {
            "bright": [48, 48, 240],
            "mid":    [40, 40, 200],
            "dark":   [24, 24, 120],
        },
    },
}

# Canvas dimensions for 1×1 footprint with 32 px building height.
# extra_h=64 gives a 64×64 canvas where all three iso_cube faces land
# within canvas bounds (see module docstring + §9 Issues Found).
_W_PX, _H_PX = canvas_size(1, 1, 64)  # (64, 64)
assert (_W_PX, _H_PX) == (64, 64), "canvas_size constant mismatch"

_X0 = _W_PX // 2 + 16   # 48 — footprint SE corner x
_Y0 = _H_PX - 1          # 63 — footprint SE corner y (bottom of canvas)

# Fixture output directory anchored to test file location (cwd-independent).
_FIXTURE_DIR = pathlib.Path(__file__).parent / "fixtures"


# ---------------------------------------------------------------------------
# Helper
# ---------------------------------------------------------------------------

def _alpha_count(img: Image.Image, bbox: tuple[int, int, int, int]) -> int:
    """Count pixels with alpha > 0 within *bbox* (left, top, right, bottom).

    Clips *bbox* to image bounds before iterating to avoid index errors when
    derived bboxes extend slightly outside canvas.
    """
    w, h = img.size
    x1 = max(0, bbox[0])
    y1 = max(0, bbox[1])
    x2 = min(w, bbox[2])
    y2 = min(h, bbox[3])
    count = 0
    for y in range(y1, y2):
        for x in range(x1, x2):
            pixel = img.getpixel((x, y))
            if pixel[3] > 0:
                count += 1
    return count


def _blank_canvas() -> Image.Image:
    """Return a fresh transparent 64×64 RGBA canvas."""
    return Image.new("RGBA", (_W_PX, _H_PX), (0, 0, 0, 0))


# ---------------------------------------------------------------------------
# iso_cube smoke
# ---------------------------------------------------------------------------

# Face bboxes for iso_cube(w=1,d=1,h=32) with x0=48, y0=63:
#   top  (bright): NW_t(48,-1)→clipped, NE_t(80,15)→clipped, SE_t(48,31), SW_t(16,15)
#                  visible region roughly y 0..32, x 16..63
#   south (mid):   SE_b(48,63), SW_b(16,47), SW_t(16,15), SE_t(48,31)
#                  visible region roughly y 15..63, x 16..48
#   east  (dark):  NE_b(80,47)→clipped, SE_b(48,63), SE_t(48,31), NE_t(80,15)→clipped
#                  visible region roughly y 31..63, x 48..63
_BBOX_TOP_CUBE   = (16,  0, 64, 33)   # upper band: where top rhombus clips to
_BBOX_SOUTH_CUBE = (16, 15, 48, 64)   # lower-left parallelogram
_BBOX_EAST_CUBE  = (48, 30, 64, 64)   # lower-right parallelogram (clipped)


def test_iso_cube_draws_three_faces():
    """iso_cube produces non-transparent pixels in top, south, and east face regions."""
    canvas = _blank_canvas()
    iso_cube(canvas, _X0, _Y0, 1, 1, 32, material="stub_red", palette=_FIXTURE_PALETTE)

    assert _alpha_count(canvas, _BBOX_TOP_CUBE)   > 0, "top face has no opaque pixels"
    assert _alpha_count(canvas, _BBOX_SOUTH_CUBE) > 0, "south face has no opaque pixels"
    assert _alpha_count(canvas, _BBOX_EAST_CUBE)  > 0, "east face has no opaque pixels"

    _FIXTURE_DIR.mkdir(exist_ok=True)
    canvas.save(_FIXTURE_DIR / "iso_cube_smoke.png")


def test_iso_cube_face_ramp_slots():
    """iso_cube top pixel == bright, south pixel == mid, east pixel == dark from palette."""
    canvas = _blank_canvas()
    iso_cube(canvas, _X0, _Y0, 1, 1, 32, material="stub_red", palette=_FIXTURE_PALETTE)

    bright = tuple(_FIXTURE_PALETTE["materials"]["stub_red"]["bright"])
    mid    = tuple(_FIXTURE_PALETTE["materials"]["stub_red"]["mid"])
    dark   = tuple(_FIXTURE_PALETTE["materials"]["stub_red"]["dark"])

    # Sample a pixel known to be inside each face region and check RGB matches slot.
    # Top face center ~(32, 16), south face ~(32, 48), east face ~(52, 48).
    def _rgb(img, x, y):
        px = img.getpixel((x, y))
        return px[:3]

    # top face: SE_t=(48,31), SW_t=(16,15) — sample (32, 22) should be bright
    top_rgb = _rgb(canvas, 32, 22)
    assert top_rgb == bright, f"top face RGB {top_rgb} != bright {bright}"

    # south face: midpoint of SE_b(48,63)→SW_b(16,47)→SW_t(16,15)→SE_t(48,31) — sample (32,47)
    south_rgb = _rgb(canvas, 32, 47)
    assert south_rgb == mid, f"south face RGB {south_rgb} != mid {mid}"

    # east face: SE_b(48,63), SE_t(48,31) — sample (50,48) should be dark
    east_rgb = _rgb(canvas, 50, 48)
    assert east_rgb == dark, f"east face RGB {east_rgb} != dark {dark}"


# ---------------------------------------------------------------------------
# iso_prism smoke — NS axis
# ---------------------------------------------------------------------------

# iso_prism NS: ridge_s = _project(0, d/2, ridge_z, 48, 63) = (32, 63-8-16) = (32, 39)
#               ridge_n = _project(w, d/2, ridge_z, 48, 63) = (80, 47-16)  = (80, 31) → clipped x
# slope_bright (NW-facing): [SE_b(48,63), NE_b(80,47), ridge_n(80,31), ridge_s(32,39)]
#   clipped to x≤63 → right half of canvas, y 31..63
# slope_mid (SE-facing):    [SW_b(16,47), NW_b(48,31), ridge_n, ridge_s]
#   x: 16..48, y: 31..47
# gable_s (south): [SE_b(48,63), SW_b(16,47), ridge_s(32,39)]
#   x: 16..48, y: 39..63
# gable_n (north): [NE_b(80,47), NW_b(48,31), ridge_n(80,31)]
#   NE_b off canvas, NW_b(48,31), ridge_n(80,31) → clipped
_BBOX_BRIGHT_NS = (31, 30, 64, 64)   # bright slope (NW-facing, right side)
_BBOX_MID_NS    = (15, 30, 49, 48)   # mid slope (SE-facing, left side)
_BBOX_GABLE_NS  = (15, 38, 49, 64)   # gables (south+north combined region)


def test_iso_prism_ns_draws_slopes_and_gables():
    """iso_prism axis='ns' produces non-transparent pixels in slope and gable regions."""
    canvas = _blank_canvas()
    iso_prism(canvas, _X0, _Y0, 1, 1, 32, pitch=0.5, axis="ns",
              material="stub_red", palette=_FIXTURE_PALETTE)

    assert _alpha_count(canvas, _BBOX_BRIGHT_NS) > 0, "NS bright slope has no opaque pixels"
    assert _alpha_count(canvas, _BBOX_MID_NS)    > 0, "NS mid slope has no opaque pixels"
    assert _alpha_count(canvas, _BBOX_GABLE_NS)  > 0, "NS gables have no opaque pixels"

    _FIXTURE_DIR.mkdir(exist_ok=True)
    canvas.save(_FIXTURE_DIR / "iso_prism_ns_smoke.png")


# ---------------------------------------------------------------------------
# iso_prism smoke — EW axis
# ---------------------------------------------------------------------------

# iso_prism EW: ridge_e = _project(w/2, 0, ridge_z, 48, 63) = (64, 63-8-16) = (64, 39) → x=64 clips
#               ridge_w = _project(w/2, d, ridge_z, 48, 63) = (32, 63-24-16) = (32, 23)
# slope_bright (NW-facing): [NW_b(48,31), SW_b(16,47), ridge_w(32,23), ridge_e(64,39)]
#   x: 16..64, y: 23..47
# slope_mid (SE-facing): [NE_b(80,47), SE_b(48,63), ridge_e(64,39), ridge_w(32,23)]
#   clipped x, y: 23..63
# gable_e (east): [NE_b(80,47), SE_b(48,63), ridge_e(64,39)] → clipped
# gable_w (west): [NW_b(48,31), SW_b(16,47), ridge_w(32,23)]
#   x: 16..48, y: 23..47
_BBOX_BRIGHT_EW = (15, 22, 65, 48)   # bright slope (N-facing, upper-left region)
_BBOX_MID_EW    = (31, 22, 64, 64)   # mid slope (S-facing, right region)
_BBOX_GABLE_EW  = (15, 22, 49, 48)   # gables (west gable region)


def test_iso_prism_ew_draws_slopes_and_gables():
    """iso_prism axis='ew' produces non-transparent pixels in slope and gable regions."""
    canvas = _blank_canvas()
    iso_prism(canvas, _X0, _Y0, 1, 1, 32, pitch=0.5, axis="ew",
              material="stub_red", palette=_FIXTURE_PALETTE)

    assert _alpha_count(canvas, _BBOX_BRIGHT_EW) > 0, "EW bright slope has no opaque pixels"
    assert _alpha_count(canvas, _BBOX_MID_EW)    > 0, "EW mid slope has no opaque pixels"
    assert _alpha_count(canvas, _BBOX_GABLE_EW)  > 0, "EW gables have no opaque pixels"

    _FIXTURE_DIR.mkdir(exist_ok=True)
    canvas.save(_FIXTURE_DIR / "iso_prism_ew_smoke.png")


# ---------------------------------------------------------------------------
# iso_prism guard — bad axis
# ---------------------------------------------------------------------------

def test_iso_prism_rejects_bad_axis():
    """iso_prism raises ValueError for axis not in {'ns', 'ew'}."""
    canvas = _blank_canvas()
    with pytest.raises(ValueError, match="axis"):
        iso_prism(canvas, _X0, _Y0, 1, 1, 32, pitch=0.5, axis="diag",
                  material="stub_red", palette=_FIXTURE_PALETTE)


# ---------------------------------------------------------------------------
# Stage 6 — pixel-native kwargs (TECH-693)
# ---------------------------------------------------------------------------


def test_iso_cube_accepts_w_px():
    """w_px/d_px/h_px path matches expected face width for 1×1 tile in px."""
    canvas = _blank_canvas()
    iso_cube(
        canvas,
        _X0,
        _Y0,
        w_px=32,
        d_px=32,
        h_px=32,
        material="stub_red",
        palette=_FIXTURE_PALETTE,
    )
    assert _alpha_count(canvas, _BBOX_TOP_CUBE) > 0


def test_iso_cube_tile_alias_parity():
    """Tile w/d/h and pixel kwargs produce identical pixels for 1×1×32."""
    a = _blank_canvas()
    b = _blank_canvas()
    iso_cube(a, _X0, _Y0, 1, 1, 32, material="stub_red", palette=_FIXTURE_PALETTE)
    iso_cube(
        b, _X0, _Y0, w_px=32, d_px=32, h_px=32,
        material="stub_red", palette=_FIXTURE_PALETTE,
    )
    assert ImageChops.difference(a, b).getbbox() is None


def test_iso_cube_px_wins_on_conflict():
    """Both w and w_px → w_px used; DeprecationWarning once."""
    canvas = _blank_canvas()
    with warnings.catch_warnings(record=True) as wrec:
        warnings.simplefilter("always")
        iso_cube(
            canvas,
            _X0,
            _Y0,
            w=1,
            d=1,
            w_px=48,
            d_px=32,
            h_px=32,
            material="stub_red",
            palette=_FIXTURE_PALETTE,
        )
    assert any(issubclass(x.category, DeprecationWarning) for x in wrec)


def test_iso_prism_pixel_kwargs():
    """iso_prism accepts w_px/d_px/h_px like iso_cube."""
    a = _blank_canvas()
    b = _blank_canvas()
    iso_prism(
        a, _X0, _Y0, 1, 1, 32, pitch=0.5, axis="ns",
        material="stub_red", palette=_FIXTURE_PALETTE,
    )
    iso_prism(
        b, _X0, _Y0, w_px=32, d_px=32, h_px=32, pitch=0.5, axis="ns",
        material="stub_red", palette=_FIXTURE_PALETTE,
    )
    assert ImageChops.difference(a, b).getbbox() is None
