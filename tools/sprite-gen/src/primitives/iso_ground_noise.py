"""iso_ground_noise — scatter accent pixels inside iso ground diamond (TECH-717, DAS §R12)."""

from __future__ import annotations

import random as _random_mod
from typing import Any

from PIL import Image

from ..palette import material_accents

_MAX_DENSITY = 0.15


def _diamond_mask(
    x0: int,
    y0: int,
    *,
    fx: int = 1,
    fy: int = 1,
) -> list[tuple[int, int]]:
    """Return all integer (x, y) coords strictly inside the iso ground diamond.

    Geometry mirrors ``iso_ground_diamond``:
        span = fx + fy
        canvas width  = span * 32
        top_y         = span * 8 - 1
        h_rh          = span * 8
        diamond pts: top, east, bottom, west (rhombus).

    Origin for this helper: x0 = canvas_w // 2, y0 = top_y (top apex).
    Caller may pass any (x0, y0) offset; the mask is built relative to that
    origin then shifted by (x0 - canvas_w//2, y0 - top_y_canonical).
    For simplicity we compute canonical coords then shift.
    """
    span = fx + fy
    canvas_w = span * 32
    top_y_canonical = span * 8 - 1
    h_rh = span * 8
    cx = canvas_w // 2  # horizontal centre == west/east midpoint

    # Canonical rhombus vertices:
    #   top    = (cx, top_y_canonical)
    #   east   = (canvas_w - 1, top_y_canonical + h_rh)
    #   bottom = (cx, top_y_canonical + 2 * h_rh)
    #   west   = (0, top_y_canonical + h_rh)

    # x_shift / y_shift: offset from canonical to caller's requested origin
    x_shift = x0 - cx
    y_shift = y0 - top_y_canonical

    mask: list[tuple[int, int]] = []
    row_min = top_y_canonical
    row_max = top_y_canonical + 2 * h_rh

    for row in range(row_min + 1, row_max):
        # Distance from top (or bottom) apex; half-width at this row:
        dist_from_top = row - row_min
        half_w = (dist_from_top * canvas_w) // (2 * 2 * h_rh)
        if row > row_min + h_rh:
            dist_from_bot = row_max - row
            half_w = (dist_from_bot * canvas_w) // (2 * 2 * h_rh)

        x_left = cx - half_w + 1
        x_right = cx + half_w - 1
        for col in range(x_left, x_right + 1):
            mask.append((col + x_shift, row + y_shift))

    return mask


def iso_ground_noise(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    material: str,
    density: float,
    seed: int,
    palette: dict[str, Any],
    fx: int = 1,
    fy: int = 1,
    **kwargs: object,
) -> None:
    """Scatter accent pixels inside the iso ground diamond (TECH-717).

    Args:
        canvas:   Target PIL image (modified in place).
        x0:       Diamond horizontal centre (matches compose convention).
        y0:       Diamond top apex y (canonical: span*8 - 1).
        material: Palette material key.
        density:  Fraction of mask pixels to paint; clamped to 0..0.15.
        seed:     RNG seed — same args → identical output.
        palette:  Loaded palette dict.
        fx, fy:   Footprint multipliers (default 1×1).

    No-ops:
        - density == 0
        - both accent_dark and accent_light are None for the material
    """
    del kwargs

    accent_dark, accent_light = material_accents(palette, material)
    if accent_dark is None and accent_light is None:
        return

    d = max(0.0, min(_MAX_DENSITY, density))
    if d == 0.0:
        return

    mask = _diamond_mask(x0, y0, fx=fx, fy=fy)
    if not mask:
        return

    rng = _random_mod.Random(seed)
    target = int(round(d * len(mask)))

    for _ in range(target):
        px, py = rng.choice(mask)
        if rng.random() < 0.5:
            colour = accent_dark if accent_dark is not None else accent_light
        else:
            colour = accent_light if accent_light is not None else accent_dark
        canvas.putpixel((px, py), colour)  # type: ignore[arg-type]
