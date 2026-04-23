"""slots.py — Parametric slot-name grammar for tiled building layouts.

Stage 9 addendum (TECH-741..744) introduces a parametric form:
``tiled-(row|column)-N`` for any ``N >= 2``. This module owns the parser
(TECH-741) and — once TECH-742 lands — the anchor resolver. Consumers:
composer building-dispatch (Stage 9 T9.2, later) and the ``row_houses_3x``
preset (Stage 6.6, TECH-734).

Hard-coded legacy names from the pre-parametric Stage 9 T9.2 scaffold
(``tiled-row-3``, ``tiled-row-4``, ``tiled-column-3``) parse through the
same regex without special-casing.
"""

from __future__ import annotations

import re

from src.spec import SpecError

__all__ = ["parse_slot", "SpecError"]

# TECH-741 — parametric slot-name grammar.
# Captures axis ∈ {row, column} and integer N (validated ``N >= 2``).
_SLOT_RE: re.Pattern[str] = re.compile(r"^tiled-(row|column)-(\d+)$")


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
