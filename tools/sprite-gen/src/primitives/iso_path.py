"""iso_path — narrow directional walkway primitive (TECH-766, DAS §R9).

Axis-aligned pavement strip. `axis ∈ {ns, ew}`; `width_px ∈ [2, 4]`.
"""

from __future__ import annotations

from typing import Any

from PIL import Image

from ..palette import PaletteKeyError


_AXES = ("ns", "ew")
_WIDTH_MIN = 2
_WIDTH_MAX = 4


def _rgb(triple: Any) -> tuple[int, int, int]:
    r, g, b = triple
    return int(r), int(g), int(b)


def _put(canvas: Image.Image, x: int, y: int, colour: tuple[int, int, int, int]) -> None:
    w, h = canvas.size
    if 0 <= x < w and 0 <= y < h:
        canvas.putpixel((x, y), colour)


def iso_path(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    length_px: int,
    axis: str,
    palette: dict[str, Any],
    width_px: int = 2,
    **kwargs: object,
) -> None:
    """Draw an axis-aligned pavement strip starting at (x0, y0).

    Args:
        canvas:    PIL image (modified in place).
        x0, y0:    Top-left anchor of the strip.
        length_px: Strip length (axis dimension).
        axis:      'ns' (north-south) or 'ew' (east-west).
        palette:   Loaded palette dict with `materials.pavement.mid`.
        width_px:  Strip width perpendicular to axis; must be in [2, 4].
        **kwargs:  Forward-compat; ignored.

    Raises:
        ValueError:      axis not in canonical set or width_px out of range.
        PaletteKeyError: palette missing `materials.pavement`.
    """
    del kwargs

    if axis not in _AXES:
        raise ValueError(f"axis must be in {set(_AXES)}")
    if not (_WIDTH_MIN <= width_px <= _WIDTH_MAX):
        raise ValueError(f"width_px must be in [{_WIDTH_MIN}, {_WIDTH_MAX}]")

    materials = palette.get("materials", {})
    ramp = materials.get("pavement")
    if ramp is None:
        raise PaletteKeyError("pavement")

    colour = _rgb(ramp["mid"]) + (255,)

    if axis == "ns":
        for y in range(y0, y0 + length_px):
            for x in range(x0, x0 + width_px):
                _put(canvas, x, y, colour)
    else:  # 'ew'
        for y in range(y0, y0 + width_px):
            for x in range(x0, x0 + length_px):
                _put(canvas, x, y, colour)
