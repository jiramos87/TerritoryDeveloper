"""
iso_stepped_foundation.py — Iso stair/wedge foundation primitive for sprite-gen.

Bridges sloped ground plane (variable per-corner Z from slopes.yaml) to a flat
top at lip = max(zn, ze, zs, zw) + 2 px.

Visible faces: south + east + top only.  N/W never drawn.
(Invariant #9 parity — cliff face visibility: south + east only.)

Projection basis (copied from iso_cube.py §4 Canvas math — not refactored):
    screen_x = x0 + (gx - gy) * 32
    screen_y = y0 - (gx + gy) * 16 - gz   (y-down, gz in pixels)

Reference:
    ia/specs/isometric-geography-system.md §5.7  (cliff face visibility)
    ia/specs/isometric-geography-system.md §6.4  (slope variant naming)
    docs/isometric-sprite-generator-exploration.md §4 Canvas math
    docs/isometric-sprite-generator-exploration.md §5 Primitive library v1
"""

from __future__ import annotations

from PIL import Image, ImageDraw

from ..palette import apply_ramp
from ..slopes import SlopeKeyError, get_corner_z  # noqa: F401 — re-exported for callers


# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

def _project(gx: float, gy: float, gz: float, x0: int, y0: int) -> tuple[int, int]:
    """Map grid-space (gx, gy, gz) to canvas pixel coords.

    Copied verbatim from iso_cube._project (Decision Log 2026-04-15 — no
    shared extraction this stage).

    Args:
        gx: Grid X (east direction, tile units).
        gy: Grid Y (north direction, tile units).
        gz: Vertical offset in pixels (z=0 at ground, up = positive).
        x0: Canvas X of footprint SE corner.
        y0: Canvas Y of footprint SE corner.

    Returns:
        (px, py) integer pixel coordinates (y-down).
    """
    px = int(x0 + (gx - gy) * 32)
    py = int(y0 - (gx + gy) * 16 - gz)
    return px, py


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def iso_stepped_foundation(
    canvas: Image.Image,
    x0: int,
    y0: int,
    fx: int,
    fy: int,
    slope_id: str,
    material: str,
    palette: dict,
) -> None:
    """Draw an isometric stair/wedge foundation on *canvas* in-place.

    Reads per-corner Z offsets from slopes.yaml for *slope_id*, computes a
    flat lip at ``max(zn, ze, zs, zw) + 2`` px, then fills:
        - South face quad (SE/SW ground corners → SE/SW lip corners).
        - East face quad  (NE/SE ground corners → NE/SE lip corners).
        - Top rhombus     (full fx×fy footprint at z = lip).

    N/W faces are NOT drawn (invariant #9 — cliff face visibility).

    Footprint corners in grid-space (tile units), y-down canvas, SE origin:
        SE = (0,   0  )
        NE = (fx,  0  )
        NW = (fx,  fy )
        SW = (0,   fy )

    Per-corner ground Z:
        SE corner → zs   (south Z from slopes.yaml)
        SW corner → zw   (west Z)
        NE corner → ze   (east Z — NE is on the east diagonal)
        NW corner → zn   (north Z — NW is on the north diagonal)

    Args:
        canvas:   PIL.Image target; mutated in place.
        x0:       Canvas X of footprint SE corner.
        y0:       Canvas Y of footprint SE corner.
        fx:       Footprint width in tile units (east direction).
        fy:       Footprint depth in tile units (north direction).
        slope_id: Key into slopes.yaml (e.g. ``"NE-up"``, ``"flat"``).
        material: Palette material key (e.g. ``"wall_brick_red"``).
        palette:  Loaded palette dict from ``load_palette``; supplies ramp colours.

    Raises:
        SlopeKeyError:  If *slope_id* is not present in slopes.yaml.
        PaletteKeyError: If *material* is not in ``palette["materials"]``.
    """
    corners = get_corner_z(slope_id)  # raises SlopeKeyError on unknown id
    zn: int = corners["n"]
    ze: int = corners["e"]
    zs: int = corners["s"]
    zw: int = corners["w"]

    lip: int = max(zn, ze, zs, zw) + 2

    # Ramp colours for the three faces.
    bright = apply_ramp(palette, material, "top")
    mid    = apply_ramp(palette, material, "south")
    dark   = apply_ramp(palette, material, "east")

    draw = ImageDraw.Draw(canvas)

    # --- Ground-plane corners ---
    # Each corner sits at z = the slope corner that "owns" that footprint vertex.
    # Mapping rationale:
    #   SE corner (gx=0,  gy=0 ) → south edge  → zs
    #   SW corner (gx=0,  gy=fy) → west  edge  → zw
    #   NE corner (gx=fx, gy=0 ) → east  edge  → ze
    #   NW corner (gx=fx, gy=fy) → north edge  → zn
    se_g = _project(0,  0,  zs, x0, y0)
    sw_g = _project(0,  fy, zw, x0, y0)
    ne_g = _project(fx, 0,  ze, x0, y0)
    # nw_g not drawn (N/W faces hidden)

    # --- Lip-height corners (all at z = lip) ---
    se_l = _project(0,  0,  lip, x0, y0)
    sw_l = _project(0,  fy, lip, x0, y0)
    ne_l = _project(fx, 0,  lip, x0, y0)
    nw_l = _project(fx, fy, lip, x0, y0)

    # --- South face: SE_ground, SW_ground, SW_lip, SE_lip ---
    south_poly = [se_g, sw_g, sw_l, se_l]

    # --- East face: NE_ground, SE_ground, SE_lip, NE_lip ---
    east_poly = [ne_g, se_g, se_l, ne_l]

    # --- Top rhombus: NW_lip, NE_lip, SE_lip, SW_lip ---
    top_poly = [nw_l, ne_l, se_l, sw_l]

    # Draw south + east first, then top (same order as iso_cube to avoid overdraw).
    draw.polygon(south_poly, fill=mid)
    draw.polygon(east_poly,  fill=dark)
    draw.polygon(top_poly,   fill=bright)
