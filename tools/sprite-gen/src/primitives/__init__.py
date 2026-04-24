"""
primitives — Isometric primitive renderers for sprite-gen.

Available primitives:
    iso_cube              — Rectangular box; three visible faces (top, south, east).
                            See iso_cube.py for full API.
    iso_ground_diamond    — Flat iso ground plate.
                            See iso_ground_diamond.py for full API.
    iso_ground_noise      — Scatter accent pixels inside iso ground diamond (TECH-717).
                            See iso_ground_noise.py for full API.
    iso_prism             — Pitched-roof prism; two slope quads + two gables.
                            See iso_prism.py for full API.
    iso_stepped_foundation — Stair/wedge foundation bridging sloped ground → flat top.
                             See iso_stepped_foundation.py for full API.

Reference:
    docs/isometric-sprite-generator-exploration.md §5 Primitive library v1
"""

from .iso_cube import iso_cube
from .iso_ground_diamond import iso_ground_diamond
from .iso_ground_noise import iso_ground_noise
from .iso_prism import iso_prism
from .iso_stepped_foundation import iso_stepped_foundation
from .iso_tree_deciduous import iso_tree_deciduous
from .iso_tree_fir import iso_tree_fir

__all__ = [
    "iso_cube",
    "iso_ground_diamond",
    "iso_ground_noise",
    "iso_prism",
    "iso_stepped_foundation",
    "iso_tree_deciduous",
    "iso_tree_fir",
]
