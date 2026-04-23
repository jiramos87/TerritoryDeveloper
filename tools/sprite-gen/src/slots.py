"""slots.py — Parametric slot-name grammar for tiled building layouts.

Stage 9 addendum (TECH-741..744) introduces a parametric form:
``tiled-(row|column)-N`` for any ``N >= 2``. This module owns the parser
(TECH-741) and the anchor resolver (TECH-742). Consumers: composer
building-dispatch (Stage 9 T9.2, later) and the ``row_houses_3x`` preset
(Stage 6.6, TECH-734).

Hard-coded legacy names from the pre-parametric Stage 9 T9.2 scaffold
(``tiled-row-3``, ``tiled-row-4``, ``tiled-column-3``) parse through the
same regex without special-casing.
"""

from __future__ import annotations

import re

from src.spec import SpecError

__all__ = ["parse_slot", "resolve_slot", "SpecError"]

# TECH-741 — parametric slot-name grammar.
# Captures axis ∈ {row, column} and integer N (validated ``N >= 2``).
_SLOT_RE: re.Pattern[str] = re.compile(r"^tiled-(row|column)-(\d+)$")

# TECH-742 — isometric tile size in pixels (Stage 9 canonical: 32 px per
# tile). Kept as a module constant so tests can ``from src.slots import
# _TILE`` to avoid drift with production code.
_TILE: int = 32


def parse_slot(name: str) -> tuple[str, int]:
    """Parse a parametric slot name into ``(axis, N)``.

    Accepts names matching ``^tiled-(row|column)-(\\d+)$`` with ``N >= 2``.
    Legacy hard-coded names (``tiled-row-3``, ``tiled-row-4``,
    ``tiled-column-3``) satisfy the same grammar and parse unchanged.

    Args:
        name: Slot-name string from an archetype YAML spec.

    Returns:
        Tuple ``(axis, N)`` where ``axis`` is ``"row"`` or ``"column"`` and
        ``N`` is the requested building count along that axis.

    Raises:
        SpecError: When ``name`` does not match the grammar or when ``N < 2``.
    """
    m = _SLOT_RE.match(name)
    if m is None:
        raise SpecError(
            "slot",
            f"slot {name!r} does not match tiled-(row|column)-N grammar",
        )
    axis = m.group(1)
    n = int(m.group(2))
    if n < 2:
        raise SpecError(
            "slot",
            f"slot {name!r} has N<2; parametric slots require N>=2",
        )
    return axis, n


def resolve_slot(
    name: str,
    footprint: tuple[int, int],
    idx: int,
    count: int,
) -> tuple[int, int]:
    """Resolve the pixel-space anchor for the ``idx``-th of ``count`` buildings.

    Distributes ``count`` buildings evenly along the axis captured by
    :func:`parse_slot`. Anchors are symmetric around the midline using
    ``(idx + 0.5) / count`` spacing (no edge-hugging). Results are clamped
    to integer pixels — isometric pixel art has no subpixel precision.
    Anchors are kept strictly inside the ground-diamond footprint.

    Args:
        name: Slot name (must parse via :func:`parse_slot`).
        footprint: ``(cols, rows)`` in tiles.
        idx: Zero-based building index (``0 <= idx < count``).
        count: Number of buildings requested by the caller. Must equal
            ``N`` parsed from ``name``.

    Returns:
        Tuple ``(x_px, y_px)`` anchor in pixel space.

    Raises:
        SpecError: When ``count`` does not match the ``N`` parsed from
            ``name`` (silent truncation is refused — the mismatch is
            surfaced so authors can correct the spec).
    """
    axis, n = parse_slot(name)
    if count != n:
        raise SpecError(
            "slot",
            f"slot {name!r} expects count={n}; got count={count}",
        )

    cols, rows = footprint
    w_px = cols * _TILE
    h_px = rows * _TILE

    if axis == "row":
        x = int((idx + 0.5) * w_px / count)
        y = h_px // 2
    else:  # axis == "column"
        x = w_px // 2
        y = int((idx + 0.5) * h_px / count)

    # Footprint sanity: anchors must stay inside the ground diamond's tile
    # bounds. ``(idx + 0.5) / count`` with ``0 <= idx < count`` keeps the
    # normalised position strictly in ``(0, 1)``, so after integer-clamping
    # the anchor is always within ``[0, axis_extent_px)``.
    assert 0 <= x < w_px, f"x={x} outside [0, {w_px}) for {name!r}"
    assert 0 <= y < h_px, f"y={y} outside [0, {h_px}) for {name!r}"

    return x, y
