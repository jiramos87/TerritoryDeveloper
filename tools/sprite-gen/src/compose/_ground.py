"""_ground.py — Ground material accessor, decoration scope gate, decoration apply."""

from __future__ import annotations

from ..placement import place as _place_decorations
from ._dispatch import _DECORATION_DISPATCH
from ._errors import DecorationScopeError, UnknownPrimitiveError


def _ground_material(graw) -> str | None:
    """Return the effective ground material name or None.

    Accepts:
        - str (legacy form)              → returned verbatim unless empty
        - dict (TECH-715 object form)    → ``material`` key (or first of
                                            ``materials`` list)
        - anything else / falsy          → None
    """
    if graw in (None, ""):
        return None
    if isinstance(graw, str):
        return graw
    if isinstance(graw, dict):
        m = graw.get("material")
        if m:
            return str(m)
        pool = graw.get("materials")
        if isinstance(pool, list) and pool:
            return str(pool[0])
    return None


def _apply_decorations(canvas, spec: dict, palette: dict) -> None:
    """Dispatch spec['decorations'] onto canvas via _DECORATION_DISPATCH."""
    decorations = spec.get("decorations", []) or []
    if not decorations:
        return
    footprint = tuple(spec.get("footprint", [1, 1]))
    seed = int(spec.get("seed", 0))
    placed = _place_decorations(decorations, footprint, seed)
    for primitive_name, x_px, y_px, kwargs in placed:
        fn = _DECORATION_DISPATCH.get(primitive_name)
        if fn is None:
            raise UnknownPrimitiveError(
                f"Unknown decoration primitive: {primitive_name!r}"
            )
        fn(canvas, x_px, y_px, palette=palette, **kwargs)


def _scope_gate_decorations(spec: dict) -> None:
    """Raise DecorationScopeError on 1x1 + iso_pool before any render pass."""
    footprint = tuple(spec.get("footprint", [1, 1]))
    decorations = spec.get("decorations", []) or []
    if footprint == (1, 1):
        for deco in decorations:
            if deco.get("primitive") == "iso_pool":
                raise DecorationScopeError(
                    "iso_pool requires footprint >= 2x2"
                )
