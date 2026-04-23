"""test_parametric_slots.py — Stage 9 addendum TECH-743.

Locks the parametric slot grammar end-to-end: parser accept/reject for
row + column axes with ``N ∈ {2, 3, 4, 5}``; distribute correctness
(equal spacing, integer pixels, footprint respect) per axis. Serves as
regression net for TECH-741 (parser) + TECH-742 (resolver).
"""

from __future__ import annotations

import pytest

from src.slots import _TILE, parse_slot, resolve_slot
from src.spec import SpecError


# ---------------------------------------------------------------------------
# Parser — accept paths (TECH-741)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("n", [2, 3, 4, 5])
def test_parse_row_variants(n: int) -> None:
    assert parse_slot(f"tiled-row-{n}") == ("row", n)


@pytest.mark.parametrize("n", [2, 3, 4, 5])
def test_parse_column_variants(n: int) -> None:
    assert parse_slot(f"tiled-column-{n}") == ("column", n)


@pytest.mark.parametrize(
    "name,expected",
    [
        ("tiled-row-3", ("row", 3)),
        ("tiled-row-4", ("row", 4)),
        ("tiled-column-3", ("column", 3)),
    ],
)
def test_legacy_aliases(name: str, expected: tuple[str, int]) -> None:
    """Legacy hard-coded Stage 9 T9.2 names parse through the same regex."""
    assert parse_slot(name) == expected


# ---------------------------------------------------------------------------
# Parser — reject paths (TECH-741)
# ---------------------------------------------------------------------------


def test_parse_rejects_n_lt_2() -> None:
    with pytest.raises(SpecError):
        parse_slot("tiled-row-1")


@pytest.mark.parametrize(
    "bad",
    [
        "tiled-foo-3",   # wrong axis token
        "row-3",         # missing tiled- prefix
        "tiled-row-",    # missing N
        "tiled-row-3x",  # trailing garbage
        "",              # empty string
    ],
)
def test_parse_rejects_malformed(bad: str) -> None:
    with pytest.raises(SpecError):
        parse_slot(bad)


# ---------------------------------------------------------------------------
# Resolver — equal spacing, row axis (TECH-742)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("n", [2, 3, 4, 5])
def test_distribute_row_equal_spacing(n: int) -> None:
    anchors = [resolve_slot(f"tiled-row-{n}", (2, 2), i, n) for i in range(n)]
    # Row axis varies x; y stays fixed at midline.
    ys = {y for _, y in anchors}
    assert ys == {2 * _TILE // 2}, f"row axis y drifted: {ys}"
    xs = [x for x, _ in anchors]
    deltas = [xs[i + 1] - xs[i] for i in range(n - 1)]
    assert max(deltas) - min(deltas) <= 1, (
        f"row Δx not equal (±1): {deltas}"
    )


# ---------------------------------------------------------------------------
# Resolver — equal spacing, column axis (TECH-742)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("n", [2, 3, 4, 5])
def test_distribute_column_equal_spacing(n: int) -> None:
    anchors = [
        resolve_slot(f"tiled-column-{n}", (2, 2), i, n) for i in range(n)
    ]
    # Column axis varies y; x stays fixed at midline.
    xs = {x for x, _ in anchors}
    assert xs == {2 * _TILE // 2}, f"column axis x drifted: {xs}"
    ys = [y for _, y in anchors]
    deltas = [ys[i + 1] - ys[i] for i in range(n - 1)]
    assert max(deltas) - min(deltas) <= 1, (
        f"column Δy not equal (±1): {deltas}"
    )


# ---------------------------------------------------------------------------
# Resolver — integer-pixel anchors (TECH-742)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize(
    "name,count",
    [
        ("tiled-row-2", 2),
        ("tiled-row-5", 5),
        ("tiled-column-3", 3),
        ("tiled-column-4", 4),
    ],
)
def test_integer_pixel_anchors(name: str, count: int) -> None:
    for i in range(count):
        x, y = resolve_slot(name, (2, 2), i, count)
        # ``type(...) is int`` — stricter than ``isinstance``: rules out bool.
        assert type(x) is int, f"x not int: {type(x).__name__}"
        assert type(y) is int, f"y not int: {type(y).__name__}"


# ---------------------------------------------------------------------------
# Resolver — footprint respect (TECH-742)
# ---------------------------------------------------------------------------


@pytest.mark.parametrize(
    "name,count,footprint",
    [
        ("tiled-row-5", 5, (2, 2)),
        ("tiled-row-3", 3, (3, 1)),
        ("tiled-column-4", 4, (2, 3)),
    ],
)
def test_anchors_inside_footprint(
    name: str, count: int, footprint: tuple[int, int]
) -> None:
    cols, rows = footprint
    w_px, h_px = cols * _TILE, rows * _TILE
    for i in range(count):
        x, y = resolve_slot(name, footprint, i, count)
        assert 0 <= x < w_px, f"x={x} outside [0, {w_px}) for {name!r}"
        assert 0 <= y < h_px, f"y={y} outside [0, {h_px}) for {name!r}"


# ---------------------------------------------------------------------------
# Resolver — count-mismatch raises (TECH-742)
# ---------------------------------------------------------------------------


def test_count_mismatch_raises() -> None:
    with pytest.raises(SpecError):
        resolve_slot("tiled-row-3", (2, 2), 0, 4)
