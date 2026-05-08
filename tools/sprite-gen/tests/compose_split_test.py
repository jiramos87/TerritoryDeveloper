"""compose_split_test.py — TECH-23787 / Stage 14.

Verifies that compose.py was successfully atomized into compose/ module folder
with all public symbols accessible via the barrel __init__.py and that
the sub-module files exist on disk.
"""

from __future__ import annotations

import importlib
from pathlib import Path

import pytest


COMPOSE_PKG_ROOT = Path(__file__).parent.parent / "src" / "compose"

EXPECTED_SUBMODULES = [
    "_errors.py",
    "_animate.py",
    "_level_expand.py",
    "_jitter.py",
    "_dispatch.py",
    "_ground.py",
    "_placement_box.py",
    "_variants.py",
    "__init__.py",
]

EXPECTED_PUBLIC_SYMBOLS = [
    # Errors
    "DecorationScopeError",
    "UnknownPrimitiveError",
    # Core
    "compose_sprite",
    "compose_layers",
    # Render
    "render",
    "_FLOOR",
    "_score_variant",
    "_write_needs_review",
    "NeedsReviewSidecar",
    # Placement
    "resolve_building_box",
    # Variants
    "sample_variant",
    # Ground
    "_ground_material",
    "_scope_gate_decorations",
    # Jitter
    "_jitter_ground_palette",
    # Animate
    "_check_animate",
    # Level expand
    "_expand_level_entries",
]


# ---------------------------------------------------------------------------
# Sub-module existence
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("filename", EXPECTED_SUBMODULES)
def test_submodule_file_exists(filename: str) -> None:
    """Each sub-module file must exist in the compose/ folder."""
    target = COMPOSE_PKG_ROOT / filename
    assert target.exists(), f"Missing sub-module: {target}"


def test_old_flat_compose_py_absent() -> None:
    """Original flat compose.py must not exist (replaced by the package)."""
    flat_file = COMPOSE_PKG_ROOT.parent / "compose.py"
    assert not flat_file.exists(), (
        f"Old flat compose.py still present at {flat_file}; "
        "atomization incomplete"
    )


# ---------------------------------------------------------------------------
# Barrel re-exports
# ---------------------------------------------------------------------------


@pytest.mark.parametrize("symbol", EXPECTED_PUBLIC_SYMBOLS)
def test_symbol_importable_from_barrel(symbol: str) -> None:
    """Every public symbol must be importable via `src.compose`."""
    mod = importlib.import_module("src.compose")
    assert hasattr(mod, symbol), (
        f"src.compose missing '{symbol}' — barrel __init__.py re-export incomplete"
    )


# ---------------------------------------------------------------------------
# Functional smoke: compose_sprite round-trip
# ---------------------------------------------------------------------------


_FIXTURE_PALETTE = {
    "class": "test",
    "materials": {
        "grass_flat": {
            "bright": [104, 168, 56],
            "mid": [78, 126, 42],
            "dark": [32, 72, 8],
        },
        "wall_brick_red": {
            "bright": [240, 48, 48],
            "mid": [200, 40, 40],
            "dark": [120, 24, 24],
        },
    },
}


@pytest.fixture(autouse=False)
def _patch_palette(monkeypatch):
    import src.compose as _compose_mod
    monkeypatch.setattr(_compose_mod, "load_palette", lambda _cls, **_kw: _FIXTURE_PALETTE)


def test_compose_sprite_returns_rgba_image(_patch_palette) -> None:
    """compose_sprite produces an RGBA image (smoke test for the split module)."""
    from src.compose import compose_sprite

    spec = {
        "footprint": [1, 1],
        "palette": "test",
        "terrain": "flat",
        "ground": "none",
        "composition": [
            {"type": "iso_cube", "w": 1, "d": 1, "h": 8, "material": "wall_brick_red"},
        ],
    }
    img = compose_sprite(spec)
    assert img.mode == "RGBA"
    assert img.size[0] > 0
    assert img.size[1] >= 64


def test_monkeypatch_load_palette_respected(_patch_palette) -> None:
    """Monkeypatching src.compose.load_palette must affect compose_sprite calls."""
    import src.compose as _mod
    from src.compose import compose_sprite

    calls: list[str] = []
    original = _mod.load_palette

    def _spy(cls, **kw):
        calls.append(cls)
        return _FIXTURE_PALETTE

    _mod.load_palette = _spy
    try:
        spec = {
            "footprint": [1, 1],
            "palette": "sentinel_palette",
            "terrain": "flat",
            "ground": "none",
            "composition": [],
        }
        compose_sprite(spec)
        assert "sentinel_palette" in calls, (
            "load_palette spy was not called — monkeypatching broken after atomization"
        )
    finally:
        _mod.load_palette = original


def test_sample_variant_returns_deep_copy() -> None:
    """sample_variant returns an independent deep copy (no vary = identity)."""
    from src.compose import sample_variant

    spec = {
        "footprint": [1, 1],
        "palette": "residential",
        "terrain": "flat",
        "ground": "none",
        "composition": [],
    }
    out = sample_variant(spec, 0)
    assert out == spec
    assert out is not spec


def test_scope_gate_raises_on_1x1_pool() -> None:
    """DecorationScopeError on iso_pool + 1x1 footprint."""
    from src.compose import DecorationScopeError, _scope_gate_decorations

    spec = {
        "footprint": [1, 1],
        "decorations": [{"primitive": "iso_pool"}],
    }
    with pytest.raises(DecorationScopeError):
        _scope_gate_decorations(spec)
