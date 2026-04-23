"""
iso_cube.py — Isometric cube (rectangular box) primitive for sprite-gen.

Draws three visible faces of a rectangular box onto a Pillow canvas using
2:1 isometric projection and NW-light 3-level shade (top=bright, S=mid,
E=dark). Shared basis for all building wall + mass primitives.

Projection basis (§4 Canvas math):
    screen_x = (gx - gy) * 32
    screen_y = (gx + gy) * 16 - gz          (y-down, gz in pixels)

Polygon faces (§5 Primitives):
    top     — rhombus (4 verts, NW-lit brightest)
    south   — parallelogram (4 verts, mid shade)
    east    — parallelogram (4 verts, darkest)

Face → ramp slot map (§6.3 Palette system):
    top   → bright
    south → mid
    east  → dark

Reference:
    docs/isometric-sprite-generator-exploration.md §4 Canvas math
    docs/isometric-sprite-generator-exploration.md §5 Primitive library v1
    docs/isometric-sprite-generator-exploration.md §6 Palette system, §6.3
"""

from __future__ import annotations

from PIL import Image, ImageDraw

from ..palette import apply_ramp

from ._kwargs import normalize_dims


# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

def _project(gx: float, gy: float, gz: float, x0: int, y0: int) -> tuple[int, int]:
    """Map grid-space (gx, gy, gz) to canvas pixel coords.

    Origin (x0, y0) is the footprint SE corner on canvas (y-down).

    Formula per §4 Canvas math:
        screen_x = x0 + (gx - gy) * 32
        screen_y = y0 - (gx + gy) * 16 - gz   (gz in pixels, up = negative y)

    Args:
        gx: Grid X coordinate (east direction, tile units).
        gy: Grid Y coordinate (north direction, tile units).
        gz: Vertical offset in pixels (z=0 at ground, positive = up).
        x0: Canvas X of footprint SE corner.
        y0: Canvas Y of footprint SE corner.

    Returns:
        (px, py) integer pixel coordinates on the canvas.
    """
    px = int(x0 + (gx - gy) * 32)
    py = int(y0 - (gx + gy) * 16 - gz)
    return px, py


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def iso_cube(
    canvas: Image.Image,
    x0: int,
    y0: int,
    w: float | None = None,
    d: float | None = None,
    h: float | None = None,
    *,
    w_px: int | None = None,
    d_px: int | None = None,
    h_px: int | None = None,
    material: str = "",
    palette: dict | None = None,
    **kwargs: object,
) -> None:
    """Draw an isometric rectangular box on *canvas* in-place.

    Three faces are filled using palette-driven ramp colours:
        - Top face   (rhombus)         → bright slot
        - South face (parallelogram)   → mid slot
        - East face  (parallelogram)   → dark slot

    Projection: 2:1 isometric, 32 px per tile unit (§4 Canvas math).
    Origin (x0, y0) = footprint SE corner on canvas, y-down (§6 Decision Log).

    Ramp colours come from ``apply_ramp(palette, material, face)`` per §6.3 Palette system.
    NW-light direction hardcoded for v1 (§5 Primitives, Decision Log 2026-04-14).

    Vertex derivation from 8 cube corners (grid-space, z in pixels):
        SE bottom (0,0,0), NE bottom (w,0,0), NW bottom (w,d,0), SW bottom (0,d,0)
        SE top    (0,0,h), NE top    (w,0,h), NW top    (w,d,h), SW top    (0,d,h)

    Top rhombus  : NW_top, NE_top, SE_top, SW_top
    South face   : SE_bot, SW_bot, SW_top, SE_top
    East face    : NE_bot, SE_bot, SE_top, NE_top

    Reference:
        docs/isometric-sprite-generator-exploration.md §4, §5, §6

    Args:
        canvas:   PIL.Image target; mutated in place.
        x0:       Canvas X of the footprint SE corner (y-down).
        y0:       Canvas Y of the footprint SE corner (y-down).
        w:        Cube width in tile units (extends along grid-X / SE diagonal).
        d:        Cube depth in tile units (extends along grid-Y / SW diagonal).
        h:        Cube height in pixels (vertical extrusion).
        material: Palette material key (e.g. ``"wall_brick_red"``).
        palette:  Loaded palette dict from ``load_palette``; supplies ramp colours.

    Raises:
        PaletteKeyError: If ``material`` is not in ``palette["materials"]``.
    """
    del kwargs
    if palette is None:
        raise TypeError("iso_cube: palette is required")
    w_px_c, d_px_c, h_px_c = normalize_dims(
        w=w,
        d=d,
        h=h,
        w_px=w_px,
        d_px=d_px,
        h_px=h_px,
        prim="iso_cube",
    )
    w = w_px_c / 32.0
    d = d_px_c / 32.0
    h = float(h_px_c)

    bright = apply_ramp(palette, material, "top")
    mid    = apply_ramp(palette, material, "south")
    dark   = apply_ramp(palette, material, "east")
    draw = ImageDraw.Draw(canvas)

    # --- 8 cube corners projected to canvas pixels ---
    # Bottom ring (z = 0)
    se_b = _project(0, 0, 0, x0, y0)
    ne_b = _project(w, 0, 0, x0, y0)
    nw_b = _project(w, d, 0, x0, y0)
    sw_b = _project(0, d, 0, x0, y0)

    # Top ring (z = h; _project subtracts gz, so h pixels up shrinks py by h)
    se_t = _project(0, 0, h, x0, y0)
    ne_t = _project(w, 0, h, x0, y0)
    nw_t = _project(w, d, h, x0, y0)
    sw_t = _project(0, d, h, x0, y0)

    # --- Top rhombus: NW, NE, SE, SW corners of top face ---
    top_poly = [nw_t, ne_t, se_t, sw_t]

    # --- South parallelogram: SE_bot, SW_bot, SW_top, SE_top ---
    south_poly = [se_b, sw_b, sw_t, se_t]

    # --- East parallelogram: NE_bot, SE_bot, SE_top, NE_top ---
    east_poly = [ne_b, se_b, se_t, ne_t]

    # --- Fill faces (draw south/east first, top last to avoid overdraw) ---
    draw.polygon(south_poly, fill=mid)
    draw.polygon(east_poly, fill=dark)
    draw.polygon(top_poly, fill=bright)
