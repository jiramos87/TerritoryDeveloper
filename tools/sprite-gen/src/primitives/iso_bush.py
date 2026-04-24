"""iso_bush — low green puff primitive (TECH-764, DAS §R9).

~6×6 px filled ellipse puff; 2-level internal ramp (bright top arc + mid body).
"""

from __future__ import annotations

from typing import Any

from PIL import Image

from ..palette import PaletteKeyError


def _rgb(triple: Any) -> tuple[int, int, int]:
    r, g, b = triple
    return int(r), int(g), int(b)


def _put(canvas: Image.Image, x: int, y: int, colour: tuple[int, int, int]) -> None:
    w, h = canvas.size
    if 0 <= x < w and 0 <= y < h:
        canvas.putpixel((x, y), colour + (255,))


def iso_bush(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    scale: float = 1.0,
    variant: int = 0,
    palette: dict[str, Any],
    **kwargs: object,
) -> None:
    """Draw a low green puff bush at (x0, y0).

    Args:
        canvas:  PIL image (modified in place).
        x0, y0:  Anchor — horizontal + vertical centre of the puff.
        scale:   Size multiplier (puff is ~6×6 at scale=1.0).
        variant: Reserved; currently unused.
        palette: Loaded palette dict with `materials.bush.{bright, mid}`.
        **kwargs: Forward-compat; ignored.

    Raises:
        PaletteKeyError: palette missing `materials.bush`.
    """
    del kwargs, variant

    materials = palette.get("materials", {})
    ramp = materials.get("bush")
    if ramp is None:
        raise PaletteKeyError("bush")

    bright = _rgb(ramp["bright"])
    mid = _rgb(ramp["mid"])

    puff_w = max(1, int(round(6 * scale)))
    puff_h = max(1, int(round(6 * scale)))
    hw = puff_w // 2
    hh = puff_h // 2

    top_band = max(1, int(round(puff_h * 0.4)))

    for dy in range(-hh, hh + 1):
        ratio_y = dy / max(1, hh)
        dx_max = int(round(hw * ((1 - ratio_y * ratio_y) ** 0.5)))
        # Top 40% = bright; lower = mid
        colour = bright if dy < -hh + top_band else mid
        for dx in range(-dx_max, dx_max + 1):
            _put(canvas, x0 + dx, y0 + dy, colour)
