"""Placement determinism + composer scope-gate tests (TECH-768 + TECH-769).

Covers:
    - `place()` per-strategy output counts.
    - In-process determinism: identical seed + inputs → byte-identical output.
    - Seed sensitivity: different seeds produce different outputs on seeded strategies.
    - Composer scope gate: 1x1 + `iso_pool` raises `DecorationScopeError`.
"""

from __future__ import annotations

import pytest

from src.compose import DecorationScopeError, compose_sprite
from src.placement import place


_STRATEGY_COUNT_CASES = [
    ("corners", {"primitive": "iso_bush", "strategy": "corners"}, (2, 2), 4),
    (
        "perimeter",
        {"primitive": "iso_bush", "strategy": "perimeter", "count": 6},
        (3, 3),
        6,
    ),
    (
        "random_border",
        {"primitive": "iso_bush", "strategy": "random_border", "count": 5},
        (4, 4),
        5,
    ),
    (
        "grid",
        {"primitive": "iso_bush", "strategy": "grid", "rows": 2, "cols": 3},
        (3, 3),
        6,
    ),
    ("centered_front", {"primitive": "iso_bush", "strategy": "centered_front"}, (2, 2), 1),
    ("centered_back", {"primitive": "iso_bush", "strategy": "centered_back"}, (2, 2), 1),
    (
        "explicit",
        {"primitive": "iso_bush", "strategy": "explicit", "coords": [[0, 0], [10, 0]]},
        (2, 2),
        2,
    ),
]

_SEEDED_STRATEGIES = ("perimeter", "random_border")


def _case_by_name(name: str) -> tuple[dict, tuple[int, int], int]:
    for n, deco, fp, n_count in _STRATEGY_COUNT_CASES:
        if n == name:
            return deco, fp, n_count
    raise KeyError(name)


@pytest.mark.parametrize("name,deco,fp,n", _STRATEGY_COUNT_CASES)
def test_placement_count(name: str, deco: dict, fp: tuple[int, int], n: int) -> None:
    assert len(place([deco], fp, seed=0)) == n


@pytest.mark.parametrize("name,deco,fp,n", _STRATEGY_COUNT_CASES)
def test_placement_determinism_same_seed(
    name: str, deco: dict, fp: tuple[int, int], n: int
) -> None:
    first = place([deco], fp, seed=42)
    second = place([deco], fp, seed=42)
    assert first == second


@pytest.mark.parametrize("name", _SEEDED_STRATEGIES)
def test_placement_seed_sensitivity(name: str) -> None:
    deco, _, _ = _case_by_name(name)
    fp = (4, 4)
    a = place([deco], fp, seed=42)
    b = place([deco], fp, seed=43)
    assert a != b


def _minimal_spec(footprint: list[int], decorations: list[dict]) -> dict:
    """Return minimal compose_sprite spec.

    Uses residential palette + empty composition + 'none' ground to skip
    ground-diamond rendering (keeps the scope-gate happy-path compact).
    """
    return {
        "class": "residential",
        "palette": "residential",
        "footprint": list(footprint),
        "composition": [],
        "ground": "none",
        "terrain": "flat",
        "decorations": decorations,
        "seed": 0,
    }


def test_compose_decoration_scope_error_1x1_pool() -> None:
    spec = _minimal_spec([1, 1], [{"primitive": "iso_pool", "strategy": "centered_front"}])
    with pytest.raises(DecorationScopeError):
        compose_sprite(spec)


def test_compose_decoration_scope_ok_2x2_pool() -> None:
    spec = _minimal_spec(
        [2, 2],
        [
            {
                "primitive": "iso_pool",
                "strategy": "centered_front",
                "kwargs": {"w_px": 10, "d_px": 10},
            }
        ],
    )
    compose_sprite(spec)
