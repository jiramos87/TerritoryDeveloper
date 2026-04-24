"""iso_pavement_patch — free-form pavement rectangle fill (TECH-766, DAS §R9)."""

from __future__ import annotations

from typing import Any

from PIL import Image

from ..palette import PaletteKeyError


def _rgb(triple: Any) -> tuple[int, int, int]:
    r, g, b = triple
    return int(r), int(g), int(b)


def _put(canvas: Image.Image, x: int, y: int, colour: tuple[int, int, int, int]) -> None:
    w, h = canvas.size
    if 0 <= x < w and 0 <= y < h:
        canvas.putpixel((x, y), colour)


def iso_pavement_patch(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    w_px: int,
    d_px: int,
    palette: dict[str, Any],
    **kwargs: object,
) -> None:
    """Fill a pavement rect at (x0, y0) of size w_px × d_px.

    Args:
        canvas:  PIL image (modified in place).
        x0, y0:  Top-left corner.
        w_px:    Width in px; must be >= 1.
        d_px:    Depth in px; must be >= 1.
        palette: Loaded palette dict with `materials.pavement.mid`.
        **kwargs: Forward-compat; ignored.

    Raises:
        ValueError:      w_px or d_px < 1.
        PaletteKeyError: palette missing `materials.pavement`.
    """
    del kwargs

    if w_px < 1:
        raise ValueError("w_px must be >= 1")
    if d_px < 1:
        raise ValueError("d_px must be >= 1")

    materials = palette.get("materials", {})
    ramp = materials.get("pavement")
    if ramp is None:
        raise PaletteKeyError("pavement")

    colour = _rgb(ramp["mid"]) + (255,)

    for y in range(y0, y0 + d_px):
        for x in range(x0, x0 + w_px):
            _put(canvas, x, y, colour)
