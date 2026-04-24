"""iso_fence — cardinal-direction fence line primitive (TECH-767, DAS §R9).

Flat axis-aligned rect per cardinal side. `side ∈ {n, s, e, w}`;
`thickness_px ∈ [1, 2]`.
"""

from __future__ import annotations

from typing import Any

from PIL import Image

from ..palette import PaletteKeyError


_SIDES = ("n", "s", "e", "w")
_THICK_MIN = 1
_THICK_MAX = 2


def _rgb(triple: Any) -> tuple[int, int, int]:
    r, g, b = triple
    return int(r), int(g), int(b)


def _put(canvas: Image.Image, x: int, y: int, colour: tuple[int, int, int, int]) -> None:
    w, h = canvas.size
    if 0 <= x < w and 0 <= y < h:
        canvas.putpixel((x, y), colour)


def iso_fence(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    length_px: int,
    side: str,
    palette: dict[str, Any],
    thickness_px: int = 1,
    **kwargs: object,
) -> None:
    """Draw an axis-aligned fence line on a cardinal side of (x0, y0).

    Args:
        canvas:       PIL image (modified in place).
        x0, y0:       Anchor pixel.
        length_px:    Line length along the side; must be >= 1.
        side:         Cardinal side: 'n', 's', 'e', or 'w'.
        palette:      Loaded palette dict with `materials.fence.bright`.
        thickness_px: Line thickness perpendicular to axis; must be in [1, 2].
        **kwargs:     Forward-compat; ignored.

    Raises:
        ValueError:      side not canonical, length_px < 1, or thickness_px out of range.
        PaletteKeyError: palette missing `materials.fence`.
    """
    del kwargs

    if side not in _SIDES:
        raise ValueError(f"side must be in {set(_SIDES)}")
    if length_px < 1:
        raise ValueError("length_px must be >= 1")
    if not (_THICK_MIN <= thickness_px <= _THICK_MAX):
        raise ValueError(f"thickness_px must be in [{_THICK_MIN}, {_THICK_MAX}]")

    materials = palette.get("materials", {})
    ramp = materials.get("fence")
    if ramp is None:
        raise PaletteKeyError("fence")

    colour = _rgb(ramp["bright"]) + (255,)

    if side == "n":
        x_lo, x_hi = x0, x0 + length_px - 1
        y_lo, y_hi = y0 - thickness_px, y0 - 1
    elif side == "s":
        x_lo, x_hi = x0, x0 + length_px - 1
        y_lo, y_hi = y0 + 1, y0 + thickness_px
    elif side == "e":
        x_lo, x_hi = x0 + 1, x0 + thickness_px
        y_lo, y_hi = y0, y0 + length_px - 1
    else:  # 'w'
        x_lo, x_hi = x0 - thickness_px, x0 - 1
        y_lo, y_hi = y0, y0 + length_px - 1

    for y in range(y_lo, y_hi + 1):
        for x in range(x_lo, x_hi + 1):
            _put(canvas, x, y, colour)
