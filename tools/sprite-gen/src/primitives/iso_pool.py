"""iso_pool — light-blue swimming pool primitive (TECH-765, DAS §R9).

Filled rectangle with 1-px white rim. Size params bounded to 8..20 px.

Primitive is footprint-agnostic. 1×1 scope rejection lives in
`compose_sprite` (TECH-769 / T7.8).
"""

from __future__ import annotations

from typing import Any

from PIL import Image

from ..palette import PaletteKeyError


_SIZE_MIN = 8
_SIZE_MAX = 20


def _rgb(triple: Any) -> tuple[int, int, int]:
    r, g, b = triple
    return int(r), int(g), int(b)


def _put(canvas: Image.Image, x: int, y: int, colour: tuple[int, int, int, int]) -> None:
    w, h = canvas.size
    if 0 <= x < w and 0 <= y < h:
        canvas.putpixel((x, y), colour)


def iso_pool(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    w_px: int,
    d_px: int,
    palette: dict[str, Any],
    **kwargs: object,
) -> None:
    """Draw a filled pool rectangle with 1-px white rim.

    Args:
        canvas:  PIL image (modified in place).
        x0, y0:  Top-left corner of the rim rectangle.
        w_px:    Width in px; must be in [8, 20].
        d_px:    Depth in px; must be in [8, 20].
        palette: Loaded palette dict with `materials.pool.{bright, mid, rim}`.
        **kwargs: Forward-compat; ignored.

    Raises:
        ValueError:      w_px or d_px out of [8, 20].
        PaletteKeyError: palette missing `materials.pool`.
    """
    del kwargs

    if not (_SIZE_MIN <= w_px <= _SIZE_MAX):
        raise ValueError(f"w_px must be in [{_SIZE_MIN}, {_SIZE_MAX}]")
    if not (_SIZE_MIN <= d_px <= _SIZE_MAX):
        raise ValueError(f"d_px must be in [{_SIZE_MIN}, {_SIZE_MAX}]")

    materials = palette.get("materials", {})
    ramp = materials.get("pool")
    if ramp is None:
        raise PaletteKeyError("pool")

    fill = _rgb(ramp["bright"]) + (255,)
    rim = _rgb(ramp["rim"]) + (255,)

    # Inner fill
    for y in range(y0 + 1, y0 + d_px - 1):
        for x in range(x0 + 1, x0 + w_px - 1):
            _put(canvas, x, y, fill)

    # 1-px rim ring (top, bottom rows)
    for x in range(x0, x0 + w_px):
        _put(canvas, x, y0, rim)
        _put(canvas, x, y0 + d_px - 1, rim)
    # Rim left + right columns
    for y in range(y0, y0 + d_px):
        _put(canvas, x0, y, rim)
        _put(canvas, x0 + w_px - 1, y, rim)
