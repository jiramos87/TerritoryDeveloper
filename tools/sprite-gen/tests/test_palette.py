"""Smoke + contract tests for src/palette.py."""

from __future__ import annotations

import colorsys
import json
import sys
import tempfile
from pathlib import Path

import numpy as np
import pytest
from PIL import Image

# Allow running from repo root: python -m pytest tools/sprite-gen/tests/
sys.path.insert(0, str(Path(__file__).parent.parent / "src"))

from palette import (
    PaletteKeyError,
    apply_ramp,
    extract_palette,
    load_palette,
    material_accents,
    write_palette_json,
)

FIXTURE = Path(__file__).parent / "fixtures" / "palette_smoke.png"


# ---------------------------------------------------------------------------
# smoke
# ---------------------------------------------------------------------------

def test_extract_palette_smoke():
    """3-colour fixture → 3 clusters, each with 4 keys and valid RGB tuples."""
    result = extract_palette("test", [FIXTURE], n_clusters=3)

    assert len(result) == 3, "Expected 3 clusters"

    for idx, entry in result.items():
        for key in ("centroid", "bright", "mid", "dark"):
            assert key in entry, f"Missing key {key!r} in cluster {idx}"
            rgb = entry[key]
            assert len(rgb) == 3, f"{key} must be a 3-tuple"
            for ch in rgb:
                assert 0 <= ch <= 255, f"Channel out of range in {key}: {ch}"


# ---------------------------------------------------------------------------
# determinism
# ---------------------------------------------------------------------------

def test_extract_palette_determinism():
    """Same seed + same input → identical result across two calls."""
    a = extract_palette("test", [FIXTURE], n_clusters=3, seed=42)
    b = extract_palette("test", [FIXTURE], n_clusters=3, seed=42)
    assert a == b, "Results differ between two identical calls"


# ---------------------------------------------------------------------------
# alpha filtering
# ---------------------------------------------------------------------------

def test_extract_palette_alpha_transparent_raises():
    """All-transparent input must raise ValueError."""
    import os

    arr = np.zeros((4, 4, 4), dtype=np.uint8)  # all alpha=0
    with tempfile.NamedTemporaryFile(suffix=".png", delete=False) as f:
        tmp = f.name
    try:
        Image.fromarray(arr, mode="RGBA").save(tmp)
        with pytest.raises(ValueError, match="non-transparent"):
            extract_palette("test", [Path(tmp)], n_clusters=2)
    finally:
        os.unlink(tmp)


def test_extract_palette_empty_paths_raises():
    """Empty source_paths must raise ValueError."""
    with pytest.raises(ValueError, match="source_paths"):
        extract_palette("test", [])


def test_extract_palette_too_few_pixels_raises():
    """Pixel count < n_clusters must raise ValueError."""
    # smoke fixture has 48 non-transparent pixels; ask for 100 clusters
    with pytest.raises(ValueError, match="less than n_clusters"):
        extract_palette("test", [FIXTURE], n_clusters=100)


# ---------------------------------------------------------------------------
# write_palette_json round-trip
# ---------------------------------------------------------------------------

def test_write_palette_json_round_trip(tmp_path):
    """write_palette_json: round-trip dict → file → json.load, assert schema."""
    named_clusters = {
        "wall_brick_red": {
            "centroid": (200, 100, 80),  # must be dropped
            "bright": (220, 120, 100),
            "mid": (200, 100, 80),
            "dark": (120, 60, 48),
        },
        "roof_tile_brown": {
            "bright": (180, 140, 90),
            "mid": (160, 120, 70),
            "dark": (96, 72, 42),
        },
    }

    out_path = write_palette_json("residential", named_clusters, tmp_path)

    assert out_path == tmp_path / "residential.json"
    assert out_path.exists()

    data = json.loads(out_path.read_text(encoding="utf-8"))

    assert data["class"] == "residential"
    assert set(data.keys()) == {"class", "materials"}
    assert set(data["materials"].keys()) == {"wall_brick_red", "roof_tile_brown"}

    for mat_name, mat in data["materials"].items():
        # centroid must not appear in persisted JSON
        assert "centroid" not in mat, f"centroid leaked into {mat_name}"
        for key in ("bright", "mid", "dark"):
            assert key in mat, f"missing {key} in {mat_name}"
            triplet = mat[key]
            assert len(triplet) == 3, f"{key} must be 3-element list in {mat_name}"
            for ch in triplet:
                assert 0 <= ch <= 255, f"channel out of range in {mat_name}.{key}: {ch}"


def test_write_palette_json_creates_dir(tmp_path):
    """write_palette_json creates dest_dir (including parents) if missing."""
    nested = tmp_path / "a" / "b" / "palettes"
    named = {"mat": {"bright": (1, 2, 3), "mid": (1, 2, 3), "dark": (1, 2, 3)}}
    out = write_palette_json("test", named, nested)
    assert out.exists()


def test_write_palette_json_returns_path(tmp_path):
    """write_palette_json returns exact Path to written file."""
    named = {"mat": {"bright": (10, 20, 30), "mid": (10, 20, 30), "dark": (10, 20, 30)}}
    result = write_palette_json("commercial", named, tmp_path)
    assert isinstance(result, Path)
    assert result.name == "commercial.json"


# ---------------------------------------------------------------------------
# load_palette + apply_ramp (Stage 1.3 Phase 2)
# ---------------------------------------------------------------------------

_MINIMAL_PALETTE = {
    "class": "test",
    "materials": {
        "wall_brick_red": {
            "bright": [240, 48, 48],
            "mid":    [200, 40, 40],
            "dark":   [120, 24, 24],
        },
    },
}


def test_load_palette_round_trip(tmp_path):
    """load_palette reads JSON written by write_palette_json."""
    named = {"wall_brick_red": {"bright": (240, 48, 48), "mid": (200, 40, 40), "dark": (120, 24, 24)}}
    write_palette_json("test", named, tmp_path)
    palette = load_palette("test", palettes_dir=tmp_path)
    assert palette["class"] == "test"
    assert "wall_brick_red" in palette["materials"]


def test_load_palette_file_not_found(tmp_path):
    """load_palette raises FileNotFoundError for missing palette."""
    with pytest.raises(FileNotFoundError):
        load_palette("nonexistent_class_xyz", palettes_dir=tmp_path)


def test_apply_ramp_top_returns_bright():
    """apply_ramp face='top' → bright slot."""
    result = apply_ramp(_MINIMAL_PALETTE, "wall_brick_red", "top")
    assert result == (240, 48, 48)


def test_apply_ramp_south_returns_mid():
    """apply_ramp face='south' → mid slot."""
    result = apply_ramp(_MINIMAL_PALETTE, "wall_brick_red", "south")
    assert result == (200, 40, 40)


def test_apply_ramp_east_returns_dark():
    """apply_ramp face='east' → dark slot."""
    result = apply_ramp(_MINIMAL_PALETTE, "wall_brick_red", "east")
    assert result == (120, 24, 24)


def test_apply_ramp_missing():
    """apply_ramp raises PaletteKeyError on unknown material."""
    with pytest.raises(PaletteKeyError):
        apply_ramp(_MINIMAL_PALETTE, "nonexistent_material", "top")


def test_apply_ramp_returns_ints():
    """apply_ramp always returns a 3-tuple of ints."""
    r, g, b = apply_ramp(_MINIMAL_PALETTE, "wall_brick_red", "top")
    assert isinstance(r, int) and isinstance(g, int) and isinstance(b, int)


def test_apply_ramp_unknown_face_raises_keyerror():
    """apply_ramp raises plain KeyError (NOT ValueError) on unknown face.

    Decision Log §9 — 2026-04-15: _FACE_TO_SLOT[face] raises KeyError on bad face;
    this is idiomatic programmer-error behavior.  Lock to real API, not ValueError.
    """
    with pytest.raises(KeyError):
        apply_ramp(_MINIMAL_PALETTE, "wall_brick_red", "north")


# ---------------------------------------------------------------------------
# Ramp-math tests (Phase 1)
# ---------------------------------------------------------------------------

def _write_solid_png(path: Path, rgb: tuple[int, int, int]) -> None:
    """Write a 4×4 fully-opaque RGBA PNG filled with *rgb*."""
    r, g, b = rgb
    arr = np.full((4, 4, 4), fill_value=[r, g, b, 255], dtype=np.uint8)
    Image.fromarray(arr, mode="RGBA").save(path)


def _expected_ramp(rgb: tuple[int, int, int]) -> dict[str, tuple[int, int, int]]:
    """Compute expected bright/mid/dark from an RGB centroid using palette.py HSV math."""
    r, g, b = rgb
    h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)

    def _to_rgb255(vv: float) -> tuple[int, int, int]:
        vv = max(0.0, min(1.0, vv))
        rr, gg, bb = colorsys.hsv_to_rgb(h, s, vv)
        return (round(rr * 255), round(gg * 255), round(bb * 255))

    return {
        "bright": _to_rgb255(v * 1.2),
        "mid": _to_rgb255(v),
        "dark": _to_rgb255(v * 0.6),
    }


def test_extract_palette_ramp_math_low_v(tmp_path):
    """Low-V centroid (40, 20, 10): bright/mid/dark match V×1.2/1.0/0.6 HSV round-trip."""
    rgb = (40, 20, 10)
    png = tmp_path / "low_v.png"
    _write_solid_png(png, rgb)

    result = extract_palette("test", [png], n_clusters=1)
    entry = result[0]

    expected = _expected_ramp(rgb)
    assert entry["bright"] == expected["bright"], f"bright mismatch: {entry['bright']} != {expected['bright']}"
    assert entry["mid"] == expected["mid"], f"mid mismatch: {entry['mid']} != {expected['mid']}"
    assert entry["dark"] == expected["dark"], f"dark mismatch: {entry['dark']} != {expected['dark']}"


def test_extract_palette_ramp_math_mid_v(tmp_path):
    """Mid-V centroid (128, 96, 64): bright/mid/dark match V×1.2/1.0/0.6 HSV round-trip."""
    rgb = (128, 96, 64)
    png = tmp_path / "mid_v.png"
    _write_solid_png(png, rgb)

    result = extract_palette("test", [png], n_clusters=1)
    entry = result[0]

    expected = _expected_ramp(rgb)
    assert entry["bright"] == expected["bright"], f"bright mismatch: {entry['bright']} != {expected['bright']}"
    assert entry["mid"] == expected["mid"], f"mid mismatch: {entry['mid']} != {expected['mid']}"
    assert entry["dark"] == expected["dark"], f"dark mismatch: {entry['dark']} != {expected['dark']}"


# ---------------------------------------------------------------------------
# TECH-716 — accent_dark / accent_light surfacing
# ---------------------------------------------------------------------------


def test_material_accents_absent_returns_none_pair():
    """Material without accent keys → (None, None)."""
    palette = {
        "class": "t",
        "materials": {"plain": {"bright": [1, 2, 3], "mid": [1, 2, 3], "dark": [1, 2, 3]}},
    }
    assert material_accents(palette, "plain") == (None, None)


def test_material_accents_present_returns_tuples():
    """Material with both accent keys → (dark_tuple, light_tuple)."""
    palette = {
        "class": "t",
        "materials": {
            "grass_flat": {
                "bright": [1, 2, 3],
                "mid": [1, 2, 3],
                "dark": [1, 2, 3],
                "accent_dark": [54, 96, 28],
                "accent_light": [148, 198, 96],
            }
        },
    }
    dark, light = material_accents(palette, "grass_flat")
    assert dark == (54, 96, 28)
    assert light == (148, 198, 96)


def test_material_accents_partial():
    """Only one accent key set → other component is None."""
    palette = {
        "class": "t",
        "materials": {"m": {"bright": [0, 0, 0], "mid": [0, 0, 0], "dark": [0, 0, 0],
                            "accent_dark": [1, 2, 3]}},
    }
    dark, light = material_accents(palette, "m")
    assert dark == (1, 2, 3)
    assert light is None


def test_material_accents_missing_material_raises():
    with pytest.raises(PaletteKeyError):
        material_accents({"class": "t", "materials": {}}, "nope")


def test_residential_seeded_materials_have_accents():
    """Active palette seeds `grass_flat` + `pavement` with both accent keys."""
    repo_palettes = Path(__file__).parent.parent / "palettes"
    palette = load_palette("residential", palettes_dir=repo_palettes)
    for mat in ("grass_flat", "pavement"):
        dark, light = material_accents(palette, mat)
        assert dark is not None, f"{mat}.accent_dark missing"
        assert light is not None, f"{mat}.accent_light missing"


def test_residential_other_materials_unchanged():
    """Existing material ramps (non-seeded) remain 3-key (no accent leakage)."""
    repo_palettes = Path(__file__).parent.parent / "palettes"
    palette = load_palette("residential", palettes_dir=repo_palettes)
    assert material_accents(palette, "wall_brick_red") == (None, None)
    assert material_accents(palette, "mortar") == (None, None)


def test_extract_palette_ramp_math_high_v_clamped(tmp_path):
    """High-V centroid (240, 200, 180): bright V clamps to 1.0 — no overflow above 255."""
    rgb = (240, 200, 180)
    png = tmp_path / "high_v.png"
    _write_solid_png(png, rgb)

    result = extract_palette("test", [png], n_clusters=1)
    entry = result[0]

    # bright channel must not exceed 255 (clamping verified)
    for ch in entry["bright"]:
        assert 0 <= ch <= 255, f"bright channel overflow: {ch}"

    expected = _expected_ramp(rgb)
    assert entry["bright"] == expected["bright"], f"bright mismatch: {entry['bright']} != {expected['bright']}"
    assert entry["mid"] == expected["mid"], f"mid mismatch: {entry['mid']} != {expected['mid']}"
    assert entry["dark"] == expected["dark"], f"dark mismatch: {entry['dark']} != {expected['dark']}"
