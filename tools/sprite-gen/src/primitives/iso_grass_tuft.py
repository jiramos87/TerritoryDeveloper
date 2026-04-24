"""iso_grass_tuft — deterministic single-pixel green accent scatter (TECH-764, DAS §R9).

1–3 single-pixel accents seeded by `variant`; local RNG — no global leak.
"""

from __future__ import annotations

import random as _random_mod
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


def iso_grass_tuft(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    scale: float = 1.0,
    variant: int = 0,
    palette: dict[str, Any],
    **kwargs: object,
) -> None:
    """Scatter 1–3 single-pixel grass accents at (x0, y0).

    Args:
        canvas:  PIL image (modified in place).
        x0, y0:  Anchor — scatter centre.
        scale:   Scale multiplier — `count = clamp(round(2*scale), 1, 3)`.
        variant: Deterministic seed for pixel placement.
        palette: Loaded palette dict with `materials.grass_tuft.bright`.
        **kwargs: Forward-compat; ignored.

    Raises:
        PaletteKeyError: palette missing `materials.grass_tuft`.
    """
    del kwargs

    materials = palette.get("materials", {})
    ramp = materials.get("grass_tuft")
    if ramp is None:
        raise PaletteKeyError("grass_tuft")

    colour = _rgb(ramp["bright"])

    count = max(1, min(3, int(round(2 * scale))))
    rng = _random_mod.Random(int(variant))
    for _ in range(count):
        dx = rng.randint(-2, 2)
        dy = rng.randint(-1, 1)
        _put(canvas, x0 + dx, y0 + dy, colour)
