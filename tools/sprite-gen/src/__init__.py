"""
sprite_gen — Isometric sprite generator for Territory Developer.

Public API:
    compose_sprite(spec: dict) -> PIL.Image
        Compose a full sprite from an archetype spec dict.
    UnknownPrimitiveError
        Raised when a composition entry references an unknown primitive type.
    canvas_size(fx, fy, extra_h) -> (width, height)
        Canvas sizing math (Stage 1.1).
    pivot_uv(canvas_h) -> (u, v)
        Unity sprite pivot in UV space (Stage 1.1).
    iso_cube, iso_prism
        Low-level primitive renderers (Stage 1.1).

Reference: docs/isometric-sprite-generator-exploration.md
"""

from .canvas import canvas_size, pivot_uv
from .compose import UnknownPrimitiveError, compose_sprite
from .primitives import iso_cube, iso_prism

__all__ = [
    "canvas_size",
    "pivot_uv",
    "compose_sprite",
    "UnknownPrimitiveError",
    "iso_cube",
    "iso_prism",
]
