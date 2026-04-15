"""
primitives — Isometric primitive renderers for sprite-gen.

Available primitives:
    iso_cube              — Rectangular box; three visible faces (top, south, east).
                            See iso_cube.py for full API.
    iso_prism             — Pitched-roof prism; two slope quads + two gables.
                            See iso_prism.py for full API.
    iso_stepped_foundation — Stair/wedge foundation bridging sloped ground → flat top.
                             See iso_stepped_foundation.py for full API.

Reference:
    docs/isometric-sprite-generator-exploration.md §5 Primitive library v1
"""

from .iso_cube import iso_cube
from .iso_prism import iso_prism
from .iso_stepped_foundation import iso_stepped_foundation

__all__ = ["iso_cube", "iso_prism", "iso_stepped_foundation"]
