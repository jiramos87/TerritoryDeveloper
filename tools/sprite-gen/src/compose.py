"""
compose.py — Compose layer for sprite-gen (Stage 1.3 + TECH-177).

``compose_sprite(spec: dict) -> PIL.Image`` wires the primitive library into a
single entry point driven by an archetype spec dict.  YAML parsing lives in
spec.py; this module receives an already-validated dict.

Origin convention (§4 Canvas math, iso_cube Decision Log 2026-04-14):
    (x0, y0) = footprint SE corner, y-down.  For a canvas of size (W, H):
        x0 = W // 2           (horizontal midpoint == SE corner of 1×1 diamond)
        y0 = H                (bottom pixel row; primitives draw upward)

offset_z handling:
    Each composition entry may carry ``offset_z`` (pixels, positive = up).
    The composer subtracts ``offset_z`` from ``y0`` before calling the primitive
    (y-down screen coord; higher z → smaller py).  Primitives themselves have
    no ``offset_z`` parameter.

Palette wiring (Stage 1.3 palette system):
    ``load_palette(spec["palette"])`` is called once per ``compose_sprite`` call.
    The loaded palette dict and the raw material string are passed directly into
    each primitive via ``material=`` and ``palette=`` kwargs.
    ``PaletteKeyError`` propagates from primitives → caller (CLI wraps → exit 2).
    Missing palette file → ``FileNotFoundError`` propagates → generic exit 1.

Slope auto-insert (TECH-177):
    When ``spec["terrain"]`` is set to a non-``"flat"`` slope id,
    ``compose_sprite`` prepends an ``iso_stepped_foundation`` call before the
    composition loop and grows ``extra_h`` by ``max(corner_z) + 2`` (the lip).
    ``SlopeKeyError`` propagates from ``slopes.get_corner_z`` → caller (CLI
    exit 1).  Absent or ``"flat"`` terrain is a no-op.

Reference:
    docs/isometric-sprite-generator-exploration.md §3, §4, §5, §8
"""

from __future__ import annotations

from PIL import Image

from .canvas import canvas_size
from .palette import load_palette
from .primitives import iso_cube, iso_prism, iso_stepped_foundation
from .slopes import SlopeKeyError, get_corner_z  # noqa: F401 — re-exported for callers

# ---------------------------------------------------------------------------
# Errors
# ---------------------------------------------------------------------------


class UnknownPrimitiveError(ValueError):
    """Raised when a composition entry `type:` is not in the dispatch dict."""


# ---------------------------------------------------------------------------
# Dispatch table
# ---------------------------------------------------------------------------

_DISPATCH: dict[str, object] = {
    "iso_cube": iso_cube,
    "iso_prism": iso_prism,
    "iso_stepped_foundation": iso_stepped_foundation,
}


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------


def compose_sprite(spec: dict) -> Image.Image:
    """Compose a sprite from an archetype spec dict.

    Derives canvas dimensions from the spec's ``footprint`` and ``composition``
    entries, builds an RGBA Pillow canvas, iterates the composition list in
    order (later entries paint on top), and returns the finished image.

    Slope auto-insert (TECH-177):
        When ``spec["terrain"]`` is a non-``"flat"`` slope id,
        ``iso_stepped_foundation`` is drawn **before** the composition loop and
        the canvas grows by the foundation lip (``max_corner_z + 2`` px).
        ``spec["foundation_material"]`` selects the palette key for the
        foundation; defaults to ``"dirt"``.

    Args:
        spec: Validated archetype dict with at minimum:
            footprint:            [fx, fy]  — tile footprint dimensions
            palette:              str       — palette class key (e.g. ``"residential"``)
            terrain:              str       — slope id from slopes.yaml (default ``"flat"``);
                                             non-flat triggers foundation auto-insert.
            foundation_material:  str       — palette key for the foundation layer
                                             (default ``"dirt"``; only used when non-flat).
            composition: list of entries, each with:
                type:       str   — primitive key (``'iso_cube'`` | ``'iso_prism'``)
                w:          float — tile-unit width (grid-X)
                d:          float — tile-unit depth (grid-Y)
                h:          float — height in pixels
                material:   str   — palette material key
                offset_z:   int   — optional vertical offset in pixels (default 0)
                pitch:      float — iso_prism only
                axis:       str   — iso_prism only (``'ns'`` | ``'ew'``)

    Returns:
        PIL.Image (RGBA) with transparent background.

    Raises:
        FileNotFoundError:    If the palette JSON for ``spec["palette"]`` is missing.
        PaletteKeyError:      If a composition entry's ``material`` is absent from the palette.
        UnknownPrimitiveError: If a composition entry's ``type:`` is not in the dispatch dict.
        SlopeKeyError:        If ``spec["terrain"]`` is not a recognised slope id.
    """
    fx, fy = spec["footprint"]
    composition = spec.get("composition", [])
    slope_id: str = spec.get("terrain", "flat")

    # --- Load palette once for the whole sprite ---
    palette = load_palette(spec["palette"])  # FileNotFoundError propagates if missing

    # --- Derive extra_h from tallest stack entry ---
    if composition:
        stack_extra_h = max(
            int(entry.get("h", 0)) + int(entry.get("offset_z", 0))
            for entry in composition
        )
    else:
        stack_extra_h = 0

    # --- Foundation lip: grows extra_h when slope is non-flat (SlopeKeyError propagates) ---
    if slope_id != "flat":
        corners = get_corner_z(slope_id)  # raises SlopeKeyError on unknown id
        lip = max(corners.values()) + 2
    else:
        lip = 0

    extra_h = max(stack_extra_h, lip)

    # --- Canvas size; clamp height to ≥ 64 px (composer owns clamp per canvas.py docstring) ---
    w_px, h_px = canvas_size(fx, fy, extra_h)
    h_px = max(h_px, 64)

    # --- Build transparent RGBA canvas ---
    canvas = Image.new("RGBA", (w_px, h_px), (0, 0, 0, 0))

    # --- SE-corner anchor (y-down; primitives draw upward from this point) ---
    x0 = w_px // 2
    y0 = h_px

    # --- Foundation auto-insert (non-flat terrain only; drawn before composition stack) ---
    if slope_id != "flat":
        foundation_material: str = spec.get("foundation_material", "dirt")
        iso_stepped_foundation(
            canvas=canvas,
            x0=x0,
            y0=y0,
            fx=fx,
            fy=fy,
            slope_id=slope_id,
            material=foundation_material,
            palette=palette,
        )

    # --- Iterate composition in order (later entries on top) ---
    for entry in composition:
        prim_type = entry.get("type")
        fn = _DISPATCH.get(prim_type)  # type: ignore[arg-type]
        if fn is None:
            raise UnknownPrimitiveError(
                f"Unknown primitive type {prim_type!r}. "
                f"Known types: {sorted(_DISPATCH.keys())}"
            )

        material = str(entry.get("material", ""))
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
            "palette":  palette,
        }

        # iso_prism-specific kwargs
        if prim_type == "iso_prism":
            kwargs["pitch"] = float(entry.get("pitch", 0.5))
            kwargs["axis"]  = str(entry.get("axis", "ns"))

        fn(**kwargs)  # type: ignore[operator]

    return canvas
