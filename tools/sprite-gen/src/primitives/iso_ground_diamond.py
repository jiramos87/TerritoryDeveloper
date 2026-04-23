"""iso_ground_diamond — universal flat ground plate (DAS §2.1, R3, R7, §4.1)."""

from __future__ import annotations

from PIL import Image, ImageDraw

from ..palette import apply_ramp

MATERIALS = (
    "grass_flat",
    "grass_dense",
    "pavement",
    "water_deep",
    "zoning_residential",
    "zoning_commercial",
    "zoning_industrial",
    "mustard_industrial",
)


def iso_ground_diamond(
    *,
    canvas: Image.Image,
    x0: int,
    y0: int,
    fx: int = 1,
    fy: int = 1,
    material: str,
    palette: dict,
    **kwargs: object,
) -> None:
    """Draw flat iso ground diamond; x0/y0 anchor SE footprint (compose convention)."""
    del kwargs
    if material not in MATERIALS:
        raise ValueError(
            f"iso_ground_diamond: unknown material {material!r}; expected one of {MATERIALS}"
        )

    w_canvas, _h_canvas = canvas.size
    span = fx + fy
    expect_w = span * 32
    if w_canvas != expect_w:
        raise ValueError(
            f"iso_ground_diamond: canvas width {w_canvas} != (fx+fy)*32 = {expect_w}"
        )

    # Local canvas coordinates (same as test harness: square (fx+fy)*32 canvas).
    w = w_canvas
    top_y = span * 8 - 1
    h_rh = span * 8
    bright = apply_ramp(palette, material, "top")
    rim = apply_ramp(palette, material, "east")
    pts: list[tuple[float, float]] = [
        (w // 2, top_y),
        (w - 1, top_y + h_rh),
        (w // 2, top_y + 2 * h_rh),
        (0, top_y + h_rh),
    ]

    draw = ImageDraw.Draw(canvas)
    draw.polygon(pts, fill=bright)
    for i in range(len(pts)):
        a = pts[i]
        b = pts[(i + 1) % len(pts)]
        draw.line([a, b], fill=rim, width=1)
