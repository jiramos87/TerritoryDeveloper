"""
canvas.py — Canvas sizing + Unity pivot math for sprite-gen.

At PPU=64 a 64×64 canvas has pivot_uv=(0.5, 0.25), matching Unity sprite
import defaults for the 2:1 isometric diamond (tileWidth=1, tileHeight=0.5).

Reference: docs/isometric-sprite-generator-exploration.md §4 Canvas math
           (Baseline formula + Unity import defaults).
"""


def canvas_size(fx: int, fy: int, extra_h: int = 0) -> tuple[int, int]:
    """Return canvas (width, height) for a tile footprint of fx × fy tiles.

    Formula: width = (fx + fy) * 32; height = extra_h.

    The baseline height is 0 when extra_h=0 — expected pure-math output.
    The composer (Stage 1.2) owns the minimum-height clamp (64) and stack
    accumulation; that is not this function's concern.

    Reference: docs/isometric-sprite-generator-exploration.md §4 Canvas math,
               Baseline formula.

    Args:
        fx: Footprint size in the X (east-west) tile axis.
        fy: Footprint size in the Y (north-south) tile axis.
        extra_h: Additional pixel height above the diamond baseline (default 0).

    Returns:
        Tuple (width, height) in pixels.

    Examples:
        >>> canvas_size(1, 1)
        (64, 0)
        >>> canvas_size(1, 1, 32)
        (64, 32)
        >>> canvas_size(3, 3, 96)
        (192, 96)
    """
    return (fx + fy) * 32, extra_h


def pivot_uv(canvas_h: int) -> tuple[float, float]:
    """Return Unity sprite pivot as UV coordinates (u, v).

    Formula: u = 0.5; v = 16 / canvas_h.

    At canvas_h=64 this yields (0.5, 0.25), matching the Unity import default
    for PPU=64 isometric sprites (the diamond bottom edge sits 16 px from the
    canvas bottom).

    Reference: docs/isometric-sprite-generator-exploration.md §4 Canvas math,
               Unity import defaults.

    Args:
        canvas_h: Canvas height in pixels. Must be > 0.

    Returns:
        Tuple (u, v) pivot in UV space.

    Raises:
        ValueError: If canvas_h <= 0 (div-by-zero guard; signals a composer
                    bug where a zero-height canvas was passed).

    Examples:
        >>> pivot_uv(64)
        (0.5, 0.25)
        >>> pivot_uv(128)
        (0.5, 0.125)
        >>> pivot_uv(192)
        (0.5, 0.08333333333333333)
    """
    if canvas_h <= 0:
        raise ValueError(
            f"canvas_h must be > 0, got {canvas_h!r}. "
            "A zero-height canvas is a composer bug."
        )
    return 0.5, 16 / canvas_h
