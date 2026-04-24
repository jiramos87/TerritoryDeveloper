"""Pure placement engine (TECH-768, DAS §R9).

Consumed by compose_sprite (TECH-769). Return shape
`list[tuple[primitive_name: str, x_px: int, y_px: int, kwargs: dict]]`.

Decoration dict shape: `{primitive, strategy, count?, rows?, cols?, coords?, kwargs?}`.

Strategy semantics:
    corners          — 4 items at footprint pixel corners.
    perimeter        — `count` evenly-spaced items along border (seeded tiebreak).
    random_border    — `count` items sampled from border pixel set (seeded).
    grid             — `rows * cols` items at evenly-spaced interior positions.
    centered_front   — 1 item at centered front-edge pixel.
    centered_back    — 1 item at centered back-edge pixel.
    explicit         — items from `deco['coords']` list (pass-through).

Seeded strategies use `random.Random(seed + i)` per decoration index `i` —
never global RNG. Same seed + inputs ⇒ byte-identical output.
"""

from __future__ import annotations

import random as _random_mod
from typing import Any, Callable


_STRATEGIES = (
    "corners",
    "perimeter",
    "random_border",
    "grid",
    "centered_front",
    "centered_back",
    "explicit",
)

_TILE_W_PX = 32
_TILE_H_PX = 16


def _validate_footprint(footprint: Any) -> tuple[int, int]:
    if not isinstance(footprint, (tuple, list)) or len(footprint) != 2:
        raise ValueError("footprint must be length-2 tuple of positive ints")
    fx, fy = footprint
    if not isinstance(fx, int) or not isinstance(fy, int) or fx < 1 or fy < 1:
        raise ValueError("footprint must be length-2 tuple of positive ints")
    return fx, fy


def _pixel_extent(fx: int, fy: int) -> tuple[int, int]:
    """Return (w_px, d_px) for a footprint of fx * fy tiles."""
    return fx * _TILE_W_PX, fy * _TILE_H_PX


def _strategy_corners(
    deco: dict,
    fx: int,
    fy: int,
    rng: _random_mod.Random,
) -> list[tuple[str, int, int, dict]]:
    del rng
    name = deco["primitive"]
    kw = deco.get("kwargs", {})
    w_px, d_px = _pixel_extent(fx, fy)
    corners = [(0, 0), (w_px - 1, 0), (0, d_px - 1), (w_px - 1, d_px - 1)]
    return [(name, int(x), int(y), dict(kw)) for x, y in corners]


def _border_points(w_px: int, d_px: int) -> list[tuple[int, int]]:
    pts: list[tuple[int, int]] = []
    for x in range(w_px):
        pts.append((x, 0))
        pts.append((x, d_px - 1))
    for y in range(1, d_px - 1):
        pts.append((0, y))
        pts.append((w_px - 1, y))
    return pts


def _strategy_perimeter(
    deco: dict,
    fx: int,
    fy: int,
    rng: _random_mod.Random,
) -> list[tuple[str, int, int, dict]]:
    """Evenly-spaced border points with seeded rotation offset.

    Seed determines which border index serves as the first pick so same
    seed → identical layout, different seed → shifted layout.
    """
    name = deco["primitive"]
    kw = deco.get("kwargs", {})
    count = int(deco.get("count", 4))
    if count < 1:
        return []
    w_px, d_px = _pixel_extent(fx, fy)
    border = _border_points(w_px, d_px)
    n = len(border)
    offset = rng.randrange(n) if n > 0 else 0
    out: list[tuple[str, int, int, dict]] = []
    for i in range(count):
        idx = (offset + (i * n) // count) % n
        x, y = border[idx]
        out.append((name, int(x), int(y), dict(kw)))
    return out


def _strategy_random_border(
    deco: dict,
    fx: int,
    fy: int,
    rng: _random_mod.Random,
) -> list[tuple[str, int, int, dict]]:
    name = deco["primitive"]
    kw = deco.get("kwargs", {})
    count = int(deco.get("count", 4))
    if count < 1:
        return []
    w_px, d_px = _pixel_extent(fx, fy)
    border = _border_points(w_px, d_px)
    out: list[tuple[str, int, int, dict]] = []
    for _ in range(count):
        x, y = rng.choice(border)
        out.append((name, int(x), int(y), dict(kw)))
    return out


def _strategy_grid(
    deco: dict,
    fx: int,
    fy: int,
    rng: _random_mod.Random,
) -> list[tuple[str, int, int, dict]]:
    del rng
    name = deco["primitive"]
    kw = deco.get("kwargs", {})
    rows = int(deco.get("rows", 1))
    cols = int(deco.get("cols", 1))
    if rows < 1 or cols < 1:
        raise ValueError("grid rows and cols must be >= 1")
    w_px, d_px = _pixel_extent(fx, fy)
    out: list[tuple[str, int, int, dict]] = []
    for r in range(rows):
        for c in range(cols):
            x = (c + 1) * w_px // (cols + 1)
            y = (r + 1) * d_px // (rows + 1)
            out.append((name, int(x), int(y), dict(kw)))
    return out


def _strategy_centered_front(
    deco: dict,
    fx: int,
    fy: int,
    rng: _random_mod.Random,
) -> list[tuple[str, int, int, dict]]:
    del rng
    name = deco["primitive"]
    kw = deco.get("kwargs", {})
    w_px, d_px = _pixel_extent(fx, fy)
    return [(name, w_px // 2, d_px - 1, dict(kw))]


def _strategy_centered_back(
    deco: dict,
    fx: int,
    fy: int,
    rng: _random_mod.Random,
) -> list[tuple[str, int, int, dict]]:
    del rng
    name = deco["primitive"]
    kw = deco.get("kwargs", {})
    w_px, _ = _pixel_extent(fx, fy)
    return [(name, w_px // 2, 0, dict(kw))]


def _strategy_explicit(
    deco: dict,
    fx: int,
    fy: int,
    rng: _random_mod.Random,
) -> list[tuple[str, int, int, dict]]:
    del fx, fy, rng
    name = deco["primitive"]
    kw = deco.get("kwargs", {})
    coords = deco.get("coords", [])
    out: list[tuple[str, int, int, dict]] = []
    for pair in coords:
        if len(pair) != 2:
            raise ValueError("explicit coords entries must be [x, y] pairs")
        x, y = pair
        out.append((name, int(x), int(y), dict(kw)))
    return out


_STRATEGY_FUNCS: dict[str, Callable[[dict, int, int, _random_mod.Random], list[tuple[str, int, int, dict]]]] = {
    "corners": _strategy_corners,
    "perimeter": _strategy_perimeter,
    "random_border": _strategy_random_border,
    "grid": _strategy_grid,
    "centered_front": _strategy_centered_front,
    "centered_back": _strategy_centered_back,
    "explicit": _strategy_explicit,
}


def place(
    decorations: list[dict],
    footprint: tuple[int, int],
    seed: int,
) -> list[tuple[str, int, int, dict]]:
    """Dispatch placement strategies over decoration list.

    Args:
        decorations: List of dicts `{primitive, strategy, count?, rows?, cols?, coords?, kwargs?}`.
        footprint:   `(fx_tiles, fy_tiles)` — both positive ints.
        seed:        Base integer seed; per-decoration RNG = `Random(seed + i)`.

    Returns:
        Flat list of `(primitive_name, x_px, y_px, kwargs_dict)` tuples.

    Raises:
        ValueError: malformed footprint, unknown strategy, or malformed per-strategy params.
    """
    fx, fy = _validate_footprint(footprint)

    out: list[tuple[str, int, int, dict]] = []
    for i, deco in enumerate(decorations):
        strategy = deco.get("strategy")
        if strategy not in _STRATEGIES:
            raise ValueError(f"strategy must be in {set(_STRATEGIES)}")
        rng = _random_mod.Random(int(seed) + i)
        fn = _STRATEGY_FUNCS[strategy]
        out.extend(fn(deco, fx, fy, rng))
    return out
