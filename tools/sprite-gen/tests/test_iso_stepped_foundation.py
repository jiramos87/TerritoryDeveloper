"""
test_iso_stepped_foundation.py — pytest tests for iso_stepped_foundation primitive.

Coverage:
  - all_slopes:    All 18 slope ids (flat + 17 variants) render without crash;
                   canvas has non-transparent pixels in south + east regions.
  - missing_id:    SlopeKeyError raised on unknown slope_id; message lists available ids.
  - faces_visible: South + east face regions have opaque pixels; N/W exterior stays transparent.
  - flat_lip:      flat slope → lip = 2 px; primitive still renders south + east faces.

Canvas setup: 1×1 footprint, same placement as test_primitives.py.
  x0 = 48, y0 = 63 on a 64×64 canvas.

Face bbox notes (1×1 footprint, lip ≥ 2 px):
  SE footprint corner at canvas (48, 63).
  South face: SE_g→SW_g→SW_l→SE_l.
    SW_g = _project(0, 1, zw, 48, 63) = (48-32, 63-16-zw) = (16, 47-zw)
    SE_l = _project(0, 0, lip, 48, 63) = (48, 63-lip)
    Region roughly: x 16..48, y (47-max_z)..63.
  East face: NE_g→SE_g→SE_l→NE_l.
    NE_g = _project(1, 0, ze, 48, 63) = (80, 47-ze) → x=80 clips to canvas w=64
    SE_g = (48, 63-zs)
    Region roughly: x 48..64, y (47-max_z)..63.
  N/W exterior: pixels at x<16, y<16 should remain transparent
    (those lie outside footprint projection for a 1×1 footprint).
"""

from __future__ import annotations

import pathlib

import pytest
from PIL import Image

from src.canvas import canvas_size
from src.slopes import SlopeKeyError, load_slopes
from src.primitives.iso_stepped_foundation import iso_stepped_foundation

# ---------------------------------------------------------------------------
# Shared constants
# ---------------------------------------------------------------------------

_FIXTURE_PALETTE: dict = {
    "class": "test",
    "materials": {
        "wall_brick_red": {
            "bright": [240, 48,  48],
            "mid":    [200, 40,  40],
            "dark":   [120, 24,  24],
        },
    },
}

_W_PX, _H_PX = canvas_size(1, 1, 64)   # (64, 64)
assert (_W_PX, _H_PX) == (64, 64), "canvas_size constant mismatch"

_X0 = _W_PX // 2 + 16   # 48 — footprint SE corner x
_Y0 = _H_PX - 1          # 63 — footprint SE corner y

_FIXTURE_DIR = pathlib.Path(__file__).parent / "fixtures"

# All slope ids from slopes.yaml (18 total: flat + 17 variants).
_ALL_SLOPE_IDS = sorted(load_slopes().keys())


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _blank_canvas() -> Image.Image:
    return Image.new("RGBA", (_W_PX, _H_PX), (0, 0, 0, 0))


def _alpha_count(img: Image.Image, bbox: tuple[int, int, int, int]) -> int:
    """Count pixels with alpha > 0 within *bbox* (left, top, right, bottom).

    Clips bbox to image bounds to avoid index errors.
    """
    w, h = img.size
    x1, y1 = max(0, bbox[0]), max(0, bbox[1])
    x2, y2 = min(w, bbox[2]), min(h, bbox[3])
    count = 0
    for y in range(y1, y2):
        for x in range(x1, x2):
            if img.getpixel((x, y))[3] > 0:
                count += 1
    return count


# ---------------------------------------------------------------------------
# all_slopes — parametrized over all 18 slope ids
# ---------------------------------------------------------------------------

@pytest.mark.parametrize("slope_id", _ALL_SLOPE_IDS)
def test_all_slopes(slope_id: str) -> None:
    """All 18 slope ids render without crash; canvas has non-transparent pixels."""
    canvas = _blank_canvas()
    iso_stepped_foundation(
        canvas, _X0, _Y0, 1, 1, slope_id, "wall_brick_red", _FIXTURE_PALETTE
    )
    total_opaque = _alpha_count(canvas, (0, 0, _W_PX, _H_PX))
    assert total_opaque > 0, (
        f"slope_id={slope_id!r}: canvas is fully transparent after render"
    )


# ---------------------------------------------------------------------------
# missing_id — SlopeKeyError on unknown id
# ---------------------------------------------------------------------------

def test_missing_id() -> None:
    """SlopeKeyError raised on unknown slope_id; message includes the id + available list."""
    canvas = _blank_canvas()
    with pytest.raises(SlopeKeyError) as exc_info:
        iso_stepped_foundation(
            canvas, _X0, _Y0, 1, 1, "NOT-A-SLOPE", "wall_brick_red", _FIXTURE_PALETTE
        )
    msg = str(exc_info.value)
    assert "NOT-A-SLOPE" in msg, f"SlopeKeyError message missing unknown id: {msg!r}"
    # Message should list available ids (at minimum include a well-known one)
    assert "flat" in msg, f"SlopeKeyError message missing available ids: {msg!r}"


# ---------------------------------------------------------------------------
# faces_visible — south + east have pixels; N/W exterior stays transparent
# ---------------------------------------------------------------------------

def test_faces_visible() -> None:
    """flat slope: south + east face regions have opaque pixels; N/W exterior transparent."""
    canvas = _blank_canvas()
    # flat slope: all corners z=0, lip=2 — minimal geometry, good sanity check.
    iso_stepped_foundation(
        canvas, _X0, _Y0, 1, 1, "flat", "wall_brick_red", _FIXTURE_PALETTE
    )

    # South face region: x 16..48, y 16..63 (conservative — flat zs=0, lip=2)
    # SE_g=(48,63), SW_g=(16,47), SW_l=(16,45), SE_l=(48,61) — tight vertical band
    south_bbox = (16, 45, 48, 64)
    assert _alpha_count(canvas, south_bbox) > 0, "south face region has no opaque pixels"

    # East face region: x 48..64, y 45..63
    east_bbox = (48, 45, 64, 64)
    assert _alpha_count(canvas, east_bbox) > 0, "east face region has no opaque pixels"

    # N/W exterior: top-left corner; 1×1 footprint at (48,63) leaves top-left empty.
    nw_exterior_bbox = (0, 0, 14, 14)
    assert _alpha_count(canvas, nw_exterior_bbox) == 0, (
        "N/W exterior should have zero opaque pixels (N/W faces not drawn)"
    )


# ---------------------------------------------------------------------------
# flat_lip — flat slope yields lip = 2; south + east still drawn
# ---------------------------------------------------------------------------

def test_flat_lip() -> None:
    """flat slope: lip = max(0,0,0,0)+2 = 2; primitive renders south + east faces."""
    canvas = _blank_canvas()
    iso_stepped_foundation(
        canvas, _X0, _Y0, 1, 1, "flat", "wall_brick_red", _FIXTURE_PALETTE
    )

    # With lip=2: SE_l=(48, 63-2)=(48,61), SW_l=(16, 47-2+0)=(16,45)
    # South face between (48,63),(16,47),(16,45),(48,61) — thin band
    south_bbox = (16, 44, 49, 64)
    east_bbox  = (47, 44, 64, 64)
    assert _alpha_count(canvas, south_bbox) > 0, "flat slope: south face not drawn"
    assert _alpha_count(canvas, east_bbox)  > 0, "flat slope: east face not drawn"

    # Save fixture for visual inspection.
    _FIXTURE_DIR.mkdir(exist_ok=True)
    canvas.save(_FIXTURE_DIR / "iso_stepped_foundation_flat.png")


# ---------------------------------------------------------------------------
# Fixture output for raised slope
# ---------------------------------------------------------------------------

def test_ne_up_smoke() -> None:
    """NE-up slope renders without crash; saves fixture PNG."""
    canvas = _blank_canvas()
    iso_stepped_foundation(
        canvas, _X0, _Y0, 1, 1, "NE-up", "wall_brick_red", _FIXTURE_PALETTE
    )
    assert _alpha_count(canvas, (0, 0, _W_PX, _H_PX)) > 0
    _FIXTURE_DIR.mkdir(exist_ok=True)
    canvas.save(_FIXTURE_DIR / "iso_stepped_foundation_NE-up.png")
