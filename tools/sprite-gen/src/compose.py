"""
compose.py — Compose layer for sprite-gen (Stage 1.2).

`compose_sprite(spec: dict) -> PIL.Image` wires the primitive library into a
single entry point driven by an archetype spec dict.  YAML parsing lives in
spec.py; this module receives an already-validated dict.

Origin convention (§4 Canvas math, iso_cube Decision Log 2026-04-14):
    (x0, y0) = footprint SE corner, y-down.  For a canvas of size (W, H):
        x0 = W // 2           (horizontal midpoint == SE corner of 1×1 diamond)
        y0 = H                (bottom pixel row; primitives draw upward)

offset_z handling:
    Each composition entry may carry `offset_z` (pixels, positive = up).
    The composer subtracts `offset_z` from `y0` before calling the primitive
    (y-down screen coord; higher z → smaller py).  Primitives themselves have
    no `offset_z` parameter.

Material stub:
    Stage 1.3 will supply a palette layer.  Until then the composer maps each
    `material:` string to a hardcoded fallback RGB via `_MATERIAL_STUB`.
    Unknown material keys fall back to `(180, 160, 140)` (neutral stone).

Reference:
    docs/isometric-sprite-generator-exploration.md §3, §4, §5, §8
"""

from __future__ import annotations

from PIL import Image

from .canvas import canvas_size
from .primitives import iso_cube, iso_prism

# ---------------------------------------------------------------------------
# Errors
# ---------------------------------------------------------------------------


class UnknownPrimitiveError(ValueError):
    """Raised when a composition entry `type:` is not in the dispatch dict."""


# ---------------------------------------------------------------------------
# Dispatch table — extend here for Stage 1.4 iso_stepped_foundation
# ---------------------------------------------------------------------------

_DISPATCH: dict[str, object] = {
    "iso_cube": iso_cube,
    "iso_prism": iso_prism,
}

# ---------------------------------------------------------------------------
# Material stub (deferred to Stage 1.3 palette layer)
# ---------------------------------------------------------------------------

_MATERIAL_STUB: dict[str, tuple[int, int, int]] = {
    "wall_brick_red":    (180,  80,  60),
    "wall_brick_grey":   (150, 150, 150),
    "roof_tile_brown":   (130,  80,  40),
    "roof_tile_grey":    (120, 120, 120),
    "concrete":          (160, 160, 155),
    "glass":             (120, 180, 210),
    "wood":              (160, 120,  70),
}

_MATERIAL_FALLBACK: tuple[int, int, int] = (180, 160, 140)


def _resolve_material(name: str) -> tuple[int, int, int]:
    return _MATERIAL_STUB.get(name, _MATERIAL_FALLBACK)


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------


def compose_sprite(spec: dict) -> Image.Image:
    """Compose a sprite from an archetype spec dict.

    Derives canvas dimensions from the spec's `footprint` and `composition`
    entries, builds an RGBA Pillow canvas, iterates the composition list in
    order (later entries paint on top), and returns the finished image.

    Args:
        spec: Validated archetype dict with at minimum:
            footprint: [fx, fy]          — tile footprint dimensions
            composition: list of entries, each with:
                type:       str   — primitive key ('iso_cube' | 'iso_prism')
                w:          float — tile-unit width (grid-X)
                d:          float — tile-unit depth (grid-Y)
                h:          float — height in pixels
                material:   str   — material key (resolved via stub)
                offset_z:   int   — optional vertical offset in pixels (default 0)
                pitch:      float — iso_prism only
                axis:       str   — iso_prism only ('ns' | 'ew')

    Returns:
        PIL.Image (RGBA) with transparent background.

    Raises:
        UnknownPrimitiveError: If a composition entry's `type:` is not in the
            dispatch dict.
    """
    fx, fy = spec["footprint"]
    composition = spec.get("composition", [])

    # --- Derive extra_h from tallest stack entry ---
    if composition:
        extra_h = max(
            int(entry.get("h", 0)) + int(entry.get("offset_z", 0))
            for entry in composition
        )
    else:
        extra_h = 0

    # --- Canvas size; clamp height to ≥ 64 px (composer owns clamp per canvas.py docstring) ---
    w_px, h_px = canvas_size(fx, fy, extra_h)
    h_px = max(h_px, 64)

    # --- Build transparent RGBA canvas ---
    canvas = Image.new("RGBA", (w_px, h_px), (0, 0, 0, 0))

    # --- SE-corner anchor (y-down; primitives draw upward from this point) ---
    x0 = w_px // 2
    y0 = h_px

    # --- Iterate composition in order (later entries on top) ---
    for entry in composition:
        prim_type = entry.get("type")
        fn = _DISPATCH.get(prim_type)  # type: ignore[arg-type]
        if fn is None:
            raise UnknownPrimitiveError(
                f"Unknown primitive type {prim_type!r}. "
                f"Known types: {sorted(_DISPATCH.keys())}"
            )

        material = _resolve_material(entry.get("material", ""))
        offset_z = int(entry.get("offset_z", 0))
        adjusted_y0 = y0 - offset_z  # y-down: higher z → smaller y

        # Build kwargs common to all primitives
        kwargs: dict = {
            "canvas":   canvas,
            "x0":       x0,
            "y0":       adjusted_y0,
            "w":        float(entry.get("w", 1)),
            "d":        float(entry.get("d", 1)),
            "h":        float(entry.get("h", 0)),
            "material": material,
        }

        # iso_prism-specific kwargs
        if prim_type == "iso_prism":
            kwargs["pitch"] = float(entry.get("pitch", 0.5))
            kwargs["axis"]  = str(entry.get("axis", "ns"))

        fn(**kwargs)  # type: ignore[operator]

    return canvas
