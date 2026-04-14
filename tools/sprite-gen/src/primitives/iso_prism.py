"""
iso_prism.py — Isometric pitched-roof prism primitive for sprite-gen.

Draws a pitched-roof prism (two sloped quads + two triangular gables) onto
a Pillow canvas using 2:1 isometric projection and NW-light 3-level shade.
Ridge runs NS or EW per `axis` arg.  Same projection basis and shade ramp
as iso_cube.

Projection basis (§4 Canvas math):
    screen_x = x0 + (gx - gy) * 32
    screen_y = y0 - (gx + gy) * 16 - gz          (y-down, gz in pixels)

Polygon faces (§5 Primitives):
    slope NW  — quad facing NW hemisphere → bright (top-ish, same as cube top)
    slope SE  — quad facing SE hemisphere → mid    (shadowed side)
    gable N/E — triangle end-cap         → dark    (treated as E-face equivalent)
    gable S/W — triangle end-cap         → dark    (treated as E-face equivalent)

Shade ramp (§6.3 Palette system):
    bright = base_rgb * 1.2  (clamped 0–255, HSV value scaling)
    mid    = base_rgb * 1.0
    dark   = base_rgb * 0.6

Draw order: gables first, mid (SE) slope, bright (NW) slope last to avoid
overdraw — mirrors iso_cube face ordering convention.

Axis semantics (§5.2 Architecture):
    'ns' → ridge between midpoint of north base-edge and south base-edge;
            slope quads face east (mid) and west (bright).
    'ew' → ridge between midpoint of east base-edge and west base-edge;
            slope quads face south (mid) and north (bright).

Reference:
    docs/isometric-sprite-generator-exploration.md §4 Canvas math
    docs/isometric-sprite-generator-exploration.md §5 Primitive library v1
    docs/isometric-sprite-generator-exploration.md §6 Palette system, §6.3
"""

from __future__ import annotations

from typing import Tuple

from PIL import Image, ImageDraw

# ---------------------------------------------------------------------------
# Type aliases
# ---------------------------------------------------------------------------
RGBTuple = Tuple[int, int, int]

_PITCH_MIN = 1e-3  # clamp pitch below this to avoid degenerate geometry


# ---------------------------------------------------------------------------
# Internal helpers — inline copy from iso_cube.py (Decision Log 2026-04-14:
# no shared module until a third primitive duplicates these helpers)
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


def _ramp(base_rgb: RGBTuple) -> tuple[RGBTuple, RGBTuple, RGBTuple]:
    """Compute (bright, mid, dark) shade ramp from a base RGB tuple.

    HSV value scaling per §6.3 (inline copy — iso_cube.py):
        bright = base * 1.2  (clamped 0–255)
        mid    = base * 1.0
        dark   = base * 0.6  (clamped 0–255)

    Args:
        base_rgb: (R, G, B) tuple in range 0–255.

    Returns:
        Three RGB tuples: (bright, mid, dark).
    """
    def _scale(rgb: RGBTuple, factor: float) -> RGBTuple:
        return tuple(min(255, max(0, int(c * factor))) for c in rgb)  # type: ignore[return-value]

    bright = _scale(base_rgb, 1.2)
    mid    = base_rgb
    dark   = _scale(base_rgb, 0.6)
    return bright, mid, dark


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def iso_prism(
    canvas: Image.Image,
    x0: int,
    y0: int,
    w: float,
    d: float,
    h: float,
    pitch: float,
    axis: str,
    material: RGBTuple,
) -> None:
    """Draw an isometric pitched-roof prism on *canvas* in-place.

    Two sloped quads and two triangular gables are filled with a NW-light
    shade ramp. Ridge runs along the NS or EW axis of the footprint.

    Projection: 2:1 isometric, 32 px per tile unit (§4 Canvas math).
    Origin (x0, y0) = footprint SE corner on canvas, y-down.

    Shade ramp: HSV value ×1.2 / ×1.0 / ×0.6, clamped 0–255 (§6.3).

    Vertex derivation table (grid-space; z in pixels; SE origin):
        Base corners (z=0):
            SE_b = (0,   0,   0)    SW_b = (0,   d,   0)
            NE_b = (w,   0,   0)    NW_b = (w,   d,   0)

        axis='ns' ridge (§5.2):
            ridge_s = (0,   d/2, ridge_z)   — south midpoint, z up
            ridge_n = (w,   d/2, ridge_z)   — north midpoint, z up

            Slope quads (4-vert each):
                bright (NW-facing): SE_b, NE_b, ridge_n, ridge_s
                mid    (SE-facing): SW_b, NW_b, ridge_n, ridge_s   (reversed winding)
            Gable triangles:
                dark (south end): SE_b, SW_b, ridge_s
                dark (north end): NE_b, NW_b, ridge_n

        axis='ew' ridge (§5.2):
            ridge_e = (w/2, 0,   ridge_z)   — east midpoint, z up
            ridge_w = (w/2, d,   ridge_z)   — west midpoint, z up

            Slope quads (4-vert each):
                bright (NW-facing): NW_b, SW_b, ridge_w, ridge_e   (west-ish slope)
                mid    (SE-facing): NE_b, SE_b, ridge_e, ridge_w   (east-ish slope, reversed)
            Gable triangles:
                dark (east end): NE_b, SE_b, ridge_e
                dark (west end): NW_b, SW_b, ridge_w

    Draw order: both gables → mid slope → bright slope (avoids overdraw).

    Args:
        canvas:   PIL.Image target; mutated in place.
        x0:       Canvas X of the footprint SE corner (y-down).
        y0:       Canvas Y of the footprint SE corner (y-down).
        w:        Prism width in tile units (extends along grid-X / SE diagonal).
        d:        Prism depth in tile units (extends along grid-Y / SW diagonal).
        h:        Base height in pixels (eave level above ground).
        pitch:    Ridge height multiplier in range 0..1; ridge_z = h * pitch.
                  Values below 1e-3 clamped to 1e-3 to avoid degenerate geometry.
        axis:     Ridge direction: 'ns' (ridge runs N-S) or 'ew' (ridge runs E-W).
        material: Base RGB tuple for the material; shade ramp derived internally.
                  Palette lookup deferred to Stage 1.3.

    Raises:
        ValueError: If `axis` is not 'ns' or 'ew'.
    """
    if axis not in ("ns", "ew"):
        raise ValueError(f"iso_prism: axis must be 'ns' or 'ew', got {axis!r}")

    pitch = max(_PITCH_MIN, pitch)
    ridge_z = h * pitch

    bright, mid, dark = _ramp(material)
    draw = ImageDraw.Draw(canvas)

    # --- Base corners (z = h; eave level — composer stacks iso_cube body below) ---
    # Per §5.1: base plane at z=0 relative to THIS call's y0; the composer
    # positions y0 at the eave level by stacking an iso_cube for the wall mass.
    se_b = _project(0, 0, 0, x0, y0)
    ne_b = _project(w, 0, 0, x0, y0)
    nw_b = _project(w, d, 0, x0, y0)
    sw_b = _project(0, d, 0, x0, y0)

    if axis == "ns":
        # Ridge runs from south midpoint (gx=0, gy=d/2) to north (gx=w, gy=d/2)
        ridge_s = _project(0,   d / 2, ridge_z, x0, y0)
        ridge_n = _project(w,   d / 2, ridge_z, x0, y0)

        # Slope facing east/SE hemisphere → mid shade
        slope_mid_poly = [sw_b, nw_b, ridge_n, ridge_s]
        # Slope facing west/NW hemisphere → bright shade
        slope_bright_poly = [se_b, ne_b, ridge_n, ridge_s]

        # Triangular gables (south & north end-caps) → dark
        gable_s = [se_b, sw_b, ridge_s]
        gable_n = [ne_b, nw_b, ridge_n]

    else:  # axis == "ew"
        # Ridge runs from east midpoint (gx=w/2, gy=0) to west (gx=w/2, gy=d)
        ridge_e = _project(w / 2, 0,   ridge_z, x0, y0)
        ridge_w = _project(w / 2, d,   ridge_z, x0, y0)

        # Slope facing south/SE hemisphere → mid shade
        slope_mid_poly = [ne_b, se_b, ridge_e, ridge_w]
        # Slope facing north/NW hemisphere → bright shade
        slope_bright_poly = [nw_b, sw_b, ridge_w, ridge_e]

        # Triangular gables (east & west end-caps) → dark
        gable_e = [ne_b, se_b, ridge_e]
        gable_w = [nw_b, sw_b, ridge_w]

        gable_s = gable_e  # reuse variable names for draw block below
        gable_n = gable_w

    # --- Draw order: gables → mid slope → bright slope (avoids overdraw) ---
    draw.polygon(gable_s, fill=dark)
    draw.polygon(gable_n, fill=dark)
    draw.polygon(slope_mid_poly, fill=mid)
    draw.polygon(slope_bright_poly, fill=bright)
