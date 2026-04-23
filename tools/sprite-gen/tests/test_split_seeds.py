"""TECH-713 — Split-seed independence tests.

Freezing `palette_seed` while varying `geometry_seed` must leave palette-scoped
`vary.` axes stable and change geometry-scoped axes — and vice versa.
"""

from __future__ import annotations

from src.compose import sample_variant


def _base_spec() -> dict:
    return {
        "id": "seeds_demo",
        "class": "residential_small",
        "footprint": [1, 1],
        "terrain": "flat",
        "palette": "residential",
        "output": {"name": "seeds_demo"},
        "composition": [
            {"type": "iso_cube", "w": 1, "d": 1, "h": 8, "material": "m"},
        ],
        "roof": {"h_px": 0},
        "palette": {"color_wall": ""},  # palette axis target
        "variants": {
            "count": 1,
            "vary": {
                "roof": {"h_px": {"min": 6, "max": 14}},  # geometry
                "palette": {"color_wall": {"values": ["red", "blue", "green", "yellow"]}},  # palette
            },
            "seed_scope": "palette+geometry",
        },
    }


def _palette_value(out: dict) -> str:
    return out["palette"]["color_wall"]


def _geometry_value(out: dict) -> int:
    return out["roof"]["h_px"]


def test_palette_freeze_geometry_varies() -> None:
    """Fix palette_seed, scan geometry_seed → geometry output changes."""
    geoms = set()
    palettes = set()
    for gs in (10, 20, 30, 40):
        spec = _base_spec()
        spec["palette_seed"] = 77
        spec["geometry_seed"] = gs
        out = sample_variant(spec, 0)
        geoms.add(_geometry_value(out))
        palettes.add(_palette_value(out))
    assert len(geoms) >= 3  # geometry varies across 4 geometry seeds
    assert len(palettes) == 1  # palette pinned by frozen palette_seed


def test_geometry_freeze_palette_varies() -> None:
    """Fix geometry_seed, scan palette_seed → palette output changes."""
    geoms = set()
    palettes = set()
    for ps in (11, 22, 33, 44):
        spec = _base_spec()
        spec["palette_seed"] = ps
        spec["geometry_seed"] = 77
        out = sample_variant(spec, 0)
        geoms.add(_geometry_value(out))
        palettes.add(_palette_value(out))
    assert len(geoms) == 1  # geometry pinned by frozen geometry_seed
    assert len(palettes) >= 2  # palette varies across 4 palette seeds


def test_full_freeze_reproducible() -> None:
    """Freezing both seeds → deterministic output."""
    spec = _base_spec()
    spec["palette_seed"] = 1
    spec["geometry_seed"] = 2
    a = sample_variant(spec, 0)
    b = sample_variant(spec, 0)
    assert _palette_value(a) == _palette_value(b)
    assert _geometry_value(a) == _geometry_value(b)
