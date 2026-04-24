"""iso_tree_deciduous — round-crown deciduous tree primitive (TECH-763, DAS §R9).

Round crown ellipse + short trunk; `color_var` kwarg picks one of three
ramp variants under palette key `tree_deciduous`. Pure pixel-native.
"""

from __future__ import annotations

from typing import Any

from PIL import Image

from ..palette import PaletteKeyError


_SCALE_MIN = 0.5
_SCALE_MAX = 1.5
_COLOR_VARS = ("green", "green_yellow", "green_blue")


def _rgb(triple: Any) -> tuple[int, int, int]:
    r, g, b = triple
    return int(r), int(g), int(b)


def _put(canvas: Image.Image, x: int, y: int, colour: tuple[int, int, int]) -> None:
    w, h = canvas.size
    if 0 <= x < w and 0 <= y < h:
        canvas.putpixel((x, y), colour + (255,))


def iso_tree_deciduous(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    scale: float = 1.0,
    variant: int = 0,
    palette: dict[str, Any],
    color_var: str = "green",
    **kwargs: object,
) -> None:
    """Draw a round-crown deciduous tree at (x0, y0).

    Args:
        canvas:    Target PIL image (modified in place).
        x0, y0:    Anchor — x0 = horizontal centre; y0 = ground contact (base of trunk).
        scale:     Size multiplier in [0.5, 1.5].
        variant:   Reserved for asymmetry seed; currently unused.
        palette:   Loaded palette dict with `materials.tree_deciduous.{color_var}`.
        color_var: One of 'green', 'green_yellow', 'green_blue'.
        **kwargs:  Forward-compat; ignored.

    Raises:
        ValueError:      scale outside range or color_var not in canonical set.
        PaletteKeyError: palette missing `materials.tree_deciduous` or the requested variant.
    """
    del kwargs, variant

    if color_var not in _COLOR_VARS:
        raise ValueError(f"color_var must be in {set(_COLOR_VARS)}")
    if not (_SCALE_MIN <= scale <= _SCALE_MAX):
        raise ValueError(f"scale must be in [{_SCALE_MIN}, {_SCALE_MAX}]")

    materials = palette.get("materials", {})
    deciduous = materials.get("tree_deciduous")
    if deciduous is None:
        raise PaletteKeyError("tree_deciduous")
    ramp = deciduous.get(color_var)
    if ramp is None:
        raise PaletteKeyError(f"tree_deciduous.{color_var}")

    bright = _rgb(ramp["bright"])
    mid = _rgb(ramp["mid"])
    dark = _rgb(ramp["dark"])

    crown_w = max(4, int(round(10 * scale)))
    crown_h = max(3, int(round(8 * scale)))
    hw = crown_w // 2
    hh = crown_h // 2

    # Crown centre: just above trunk (y0 is trunk base)
    cx = x0
    cy = y0 - 2 - hh

    # Filled ellipse — bright band top quarter, mid body, dark rim 1 px
    for dy in range(-hh, hh + 1):
        # Ellipse equation: (dx/hw)^2 + (dy/hh)^2 <= 1
        ratio_y = dy / max(1, hh)
        dx_max = int(round(hw * ((1 - ratio_y * ratio_y) ** 0.5)))
        for dx in range(-dx_max, dx_max + 1):
            # Rim detection: one of the edge columns / rows
            is_rim = (
                dx in (-dx_max, dx_max)
                or dy in (-hh, hh)
            )
            # Top-band: top quarter gets bright
            if dy < -hh // 2 and not is_rim:
                colour = bright
            elif is_rim:
                colour = dark
            else:
                colour = mid
            _put(canvas, cx + dx, cy + dy, colour)

    # Trunk — 1 px wide × 2 px tall dark rectangle centred under crown
    trunk_top = y0 - 2
    _put(canvas, x0, trunk_top, dark)
    _put(canvas, x0, trunk_top + 1, dark)
