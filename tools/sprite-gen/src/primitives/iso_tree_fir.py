"""iso_tree_fir — stacked-dome fir/conifer tree primitive (TECH-762, DAS §R9).

Draws 2-3 stacked green domes over a dark-green shadow-base ellipse.
Pure pixel-native primitive — no outline pass, internal 3-level ramp.
"""

from __future__ import annotations

from typing import Any

from PIL import Image

from ..palette import PaletteKeyError


_SCALE_MIN = 0.5
_SCALE_MAX = 1.5


def _rgb(triple: Any) -> tuple[int, int, int]:
    r, g, b = triple
    return int(r), int(g), int(b)


def _put(canvas: Image.Image, x: int, y: int, colour: tuple[int, int, int]) -> None:
    w, h = canvas.size
    if 0 <= x < w and 0 <= y < h:
        canvas.putpixel((x, y), colour + (255,))


def _dome(
    canvas: Image.Image,
    cx: int,
    cy: int,
    width: int,
    bright: tuple[int, int, int],
    mid: tuple[int, int, int],
) -> None:
    """Draw one filled half-ellipse dome centred on (cx, cy).

    Width = horizontal extent (px). Height = half of width (rounded).
    Top row = bright; lower rows = mid.
    """
    if width <= 0:
        return
    half = width // 2
    h_rows = max(1, (width + 1) // 2)
    for row in range(h_rows):
        # Half-ellipse row width decreases from top down to bottom
        ratio = 1.0 - (row / max(1, h_rows))
        row_half = int(round(half * (ratio ** 0.5)))
        y = cy - h_rows + row + 1
        colour = bright if row == 0 else mid
        for dx in range(-row_half, row_half + 1):
            _put(canvas, cx + dx, y, colour)


def _shadow_ellipse(
    canvas: Image.Image,
    cx: int,
    cy: int,
    width: int,
    dark: tuple[int, int, int],
) -> None:
    """Draw thin dark-green shadow ellipse below the dome stack."""
    if width <= 0:
        return
    half = max(1, width // 2)
    h_rows = max(1, half // 2)
    for row in range(h_rows):
        ratio = 1.0 - (row / max(1, h_rows))
        row_half = int(round(half * ratio))
        y = cy + row
        for dx in range(-row_half, row_half + 1):
            _put(canvas, cx + dx, y, dark)


def iso_tree_fir(
    canvas: Image.Image,
    x0: int,
    y0: int,
    *,
    scale: float = 1.0,
    variant: int = 0,
    palette: dict[str, Any],
    **kwargs: object,
) -> None:
    """Draw a stacked-dome fir tree at (x0, y0).

    Args:
        canvas:  Target PIL image (modified in place).
        x0, y0:  Anchor — x0 = horizontal centre; y0 = ground contact (base of shadow).
        scale:   Size multiplier in [0.5, 1.5]. <0.75 → 2 domes; ≥0.75 → 3 domes.
        variant: Reserved for future colour-var / asymmetry variations.
        palette: Loaded palette dict with `materials.tree_fir` ramp keys
                 `bright`, `mid`, `dark`.
        **kwargs: Forward-compat; ignored.

    Raises:
        ValueError:      scale outside [0.5, 1.5].
        PaletteKeyError: palette missing `materials.tree_fir`.
    """
    del kwargs, variant

    if not (_SCALE_MIN <= scale <= _SCALE_MAX):
        raise ValueError(f"scale must be in [{_SCALE_MIN}, {_SCALE_MAX}]")

    materials = palette.get("materials", {})
    ramp = materials.get("tree_fir")
    if ramp is None:
        raise PaletteKeyError("tree_fir")

    bright = _rgb(ramp["bright"])
    mid = _rgb(ramp["mid"])
    dark = _rgb(ramp["dark"])

    dome_count = 3 if scale >= 0.75 else 2
    widest = max(2, int(round(8 * scale)))
    overlap = max(1, int(round(2 * scale)))

    # Stack domes from bottom (widest) up; each successively narrower.
    # Bottom dome sits directly above shadow base (y0).
    cur_y = y0 - 1
    for i in range(dome_count):
        dome_width = max(2, widest - i * max(1, widest // max(2, dome_count)))
        _dome(canvas, x0, cur_y, dome_width, bright, mid)
        dome_height = max(1, (dome_width + 1) // 2)
        cur_y -= dome_height - overlap

    _shadow_ellipse(canvas, x0, y0, widest, dark)
