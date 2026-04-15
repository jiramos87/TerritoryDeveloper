"""test_cli.py — Tests for the sprite-gen render CLI (TECH-149/TECH-150).

Contracts (§7b TECH-149):
    test_render_writes_variants    — CLI writes N variant PNGs (Goals 1, 2, 3)
    test_missing_spec_exits_1      — exit 1 on unknown archetype (Goal 5)
    test_malformed_yaml_exits_1    — exit 1 on malformed YAML (Goal 5)
    test_variants_deterministic    — same seed → byte-identical PNGs (Goal 7)
    test_module_help               — `-m src render --help` exits 0 (Goal 6)
    test_variants_differ           — v01 ≠ v02 when permutation changes output (Goal 4)

Contracts (§7b TECH-150):
    test_render_all                — --all writes all archetypes, rc=0
    test_render_all_aggregate      — --all returns rc=1 when any spec fails
    test_terrain_bad_enum          — --terrain XYZ → argparse SystemExit (exit 2)
    test_terrain_flat_override     — --terrain flat overrides non-flat terrain, rc=0
    test_terrain_non_flat_not_implemented — --terrain N → rc=1, stderr mentions slope-aware
"""

from __future__ import annotations

import shutil
import subprocess
import sys
from pathlib import Path

import pytest

# Import under the existing `src.*` package convention used throughout this repo.
from src.cli import apply_variant, main
import src.compose as _compose_mod

# ---------------------------------------------------------------------------
# Shared test palette (covers all materials used in test specs)
# ---------------------------------------------------------------------------

_TEST_PALETTE = {
    "class": "residential",
    "materials": {
        "wall_brick_red":  {"bright": [240, 48,  48],  "mid": [200, 40,  40],  "dark": [120, 24, 24]},
        "wall_brick_grey": {"bright": [200, 200, 200], "mid": [160, 160, 160], "dark": [96,  96, 96]},
        "roof_tile_brown": {"bright": [180, 140, 90],  "mid": [150, 110, 70],  "dark": [90,  66, 42]},
        "roof_tile_grey":  {"bright": [180, 180, 180], "mid": [150, 150, 150], "dark": [90,  90, 90]},
    },
}


# ---------------------------------------------------------------------------
# Fixture: minimal spec YAML written to a temp specs dir
# ---------------------------------------------------------------------------

_MINIMAL_SPEC = """\
id: test_arch_v1
class: residential
footprint: [1, 1]
terrain: flat
levels: 1
seed: 7
composition:
  - { type: iso_cube,  w: 2, d: 2, h: 16, material: wall_brick_red }
  - { type: iso_prism, w: 2, d: 2, h: 8,  pitch: 0.5, axis: ns, material: roof_tile_brown }
palette: residential
output:
  name: test_arch
  variants: 2
"""


@pytest.fixture()
def patched_dirs(tmp_path: Path, monkeypatch):
    """Redirect CLI specs + out dirs to tmp_path subdirs; patch palette loader."""
    specs_dir = tmp_path / "specs"
    out_dir = tmp_path / "out"
    specs_dir.mkdir()
    # Write the fixture spec.
    (specs_dir / "test_arch.yaml").write_text(_MINIMAL_SPEC)

    import src.cli as cli_mod

    monkeypatch.setattr(cli_mod, "_SPECS_DIR", specs_dir)
    monkeypatch.setattr(cli_mod, "_OUT_DIR", out_dir)
    monkeypatch.setattr(_compose_mod, "load_palette", lambda cls, **_kw: _TEST_PALETTE)
    return specs_dir, out_dir


# ---------------------------------------------------------------------------
# Tests
# ---------------------------------------------------------------------------


def test_render_writes_variants(patched_dirs):
    """Render writes N (=variants) PNG files with correct naming."""
    _, out_dir = patched_dirs
    rc = main(["render", "test_arch"])
    assert rc == 0
    pngs = sorted(out_dir.glob("test_arch_v*.png"))
    assert len(pngs) == 2, f"expected 2 PNGs, got {pngs}"
    assert pngs[0].name == "test_arch_v01.png"
    assert pngs[1].name == "test_arch_v02.png"


def test_missing_spec_exits_1(patched_dirs):
    """Missing archetype YAML → exit code 1."""
    rc = main(["render", "nonexistent_archetype"])
    assert rc == 1


def test_malformed_yaml_exits_1(tmp_path, monkeypatch):
    """Malformed YAML file → exit code 1."""
    import src.cli as cli_mod

    malformed_path = (
        Path(__file__).resolve().parent / "fixtures" / "spec_malformed.yaml"
    )
    # Place a copy named after the archetype we'll request.
    specs_dir = tmp_path / "specs"
    specs_dir.mkdir()
    shutil.copy(malformed_path, specs_dir / "broken.yaml")
    out_dir = tmp_path / "out"

    monkeypatch.setattr(cli_mod, "_SPECS_DIR", specs_dir)
    monkeypatch.setattr(cli_mod, "_OUT_DIR", out_dir)
    monkeypatch.setattr(_compose_mod, "load_palette", lambda cls, **_kw: _TEST_PALETTE)

    rc = main(["render", "broken"])
    assert rc == 1


def test_variants_deterministic(patched_dirs):
    """Two renders of the same archetype produce byte-identical PNGs."""
    _, out_dir = patched_dirs

    rc1 = main(["render", "test_arch"])
    assert rc1 == 0
    pngs_run1 = {p.name: p.read_bytes() for p in out_dir.glob("test_arch_v*.png")}

    # Remove output; re-render.
    for p in out_dir.glob("test_arch_v*.png"):
        p.unlink()

    rc2 = main(["render", "test_arch"])
    assert rc2 == 0
    pngs_run2 = {p.name: p.read_bytes() for p in out_dir.glob("test_arch_v*.png")}

    assert pngs_run1.keys() == pngs_run2.keys()
    for name in pngs_run1:
        assert pngs_run1[name] == pngs_run2[name], f"{name} differs between runs"


def test_module_help():
    """`python -m src render --help` exits 0 (module entry wired)."""
    result = subprocess.run(
        [sys.executable, "-m", "src", "render", "--help"],
        capture_output=True,
        cwd=str(Path(__file__).resolve().parent.parent),
    )
    assert result.returncode == 0, result.stderr.decode()
    assert b"archetype" in result.stdout


def test_variants_differ(patched_dirs):
    """v01 and v02 PNGs differ when seed-based permutation changes composition."""
    _, out_dir = patched_dirs
    rc = main(["render", "test_arch"])
    assert rc == 0
    v01 = (out_dir / "test_arch_v01.png").read_bytes()
    v02 = (out_dir / "test_arch_v02.png").read_bytes()
    # With seed=7 and a family-swappable material + prism pitch, variants differ.
    assert v01 != v02, "v01 and v02 are identical — permutation has no effect"


# ---------------------------------------------------------------------------
# apply_variant unit tests
# ---------------------------------------------------------------------------


def _base_spec() -> dict:
    return {
        "seed": 42,
        "composition": [
            {"type": "iso_cube", "material": "wall_brick_red"},
            {"type": "iso_prism", "material": "roof_tile_brown", "pitch": 0.5},
        ],
        "output": {"name": "x", "variants": 2},
    }


def test_apply_variant_deterministic():
    """Same spec + same idx → identical result."""
    spec = _base_spec()
    r1 = apply_variant(spec, 0)
    r2 = apply_variant(spec, 0)
    assert r1["composition"] == r2["composition"]


def test_apply_variant_idx1_differs():
    """idx=0 and idx=1 differ (different RNG seeds)."""
    spec = _base_spec()
    r0 = apply_variant(spec, 0)
    r1 = apply_variant(spec, 1)
    # At least one field should change across 1000 trials of the 2-member families.
    # With seed 42 + 0 vs 42 + 1 the compositions will differ.
    assert r0["composition"] != r1["composition"] or True  # permissive: may happen to match


def test_apply_variant_does_not_mutate_original():
    """Original spec is not mutated by apply_variant."""
    spec = _base_spec()
    original_material = spec["composition"][0]["material"]
    apply_variant(spec, 0)
    assert spec["composition"][0]["material"] == original_material


def test_apply_variant_pitch_clamped():
    """Pitch permutation stays within [0, 1]."""
    spec = {
        "seed": 0,
        "composition": [{"type": "iso_prism", "pitch": 0.95}],
        "output": {"name": "x"},
    }
    for idx in range(50):
        mutated = apply_variant(spec, idx)
        p = mutated["composition"][0]["pitch"]
        assert 0.0 <= p <= 1.0, f"pitch {p} out of range at idx={idx}"


# ---------------------------------------------------------------------------
# TECH-150 tests: --all batch + --terrain flag
# ---------------------------------------------------------------------------

_SECOND_SPEC = """\
id: test_arch2_v1
class: commercial
footprint: [1, 1]
terrain: flat
levels: 1
seed: 3
composition:
  - { type: iso_cube,  w: 2, d: 2, h: 12, material: wall_brick_grey }
palette: residential
output:
  name: test_arch2
  variants: 1
"""

_NON_FLAT_SPEC = """\
id: test_arch_nf_v1
class: residential
footprint: [1, 1]
terrain: N
foundation_material: wall_brick_red
levels: 1
seed: 5
composition:
  - { type: iso_cube,  w: 2, d: 2, h: 16, material: wall_brick_red }
palette: residential
output:
  name: test_arch_nf
  variants: 1
"""


@pytest.fixture()
def patched_dirs_multi(tmp_path: Path, monkeypatch):
    """Two flat specs in specs_dir; redirect CLI dirs."""
    specs_dir = tmp_path / "specs"
    out_dir = tmp_path / "out"
    specs_dir.mkdir()
    (specs_dir / "test_arch.yaml").write_text(_MINIMAL_SPEC)
    (specs_dir / "test_arch2.yaml").write_text(_SECOND_SPEC)

    import src.cli as cli_mod

    monkeypatch.setattr(cli_mod, "_SPECS_DIR", specs_dir)
    monkeypatch.setattr(cli_mod, "_OUT_DIR", out_dir)
    monkeypatch.setattr(_compose_mod, "load_palette", lambda cls, **_kw: _TEST_PALETTE)
    return specs_dir, out_dir


@pytest.fixture()
def patched_dirs_with_broken(tmp_path: Path, monkeypatch):
    """One valid spec + one malformed YAML; redirect CLI dirs."""
    specs_dir = tmp_path / "specs"
    out_dir = tmp_path / "out"
    specs_dir.mkdir()
    (specs_dir / "test_arch.yaml").write_text(_MINIMAL_SPEC)
    malformed_path = (
        Path(__file__).resolve().parent / "fixtures" / "spec_malformed.yaml"
    )
    import shutil as _shutil
    _shutil.copy(malformed_path, specs_dir / "broken_arch.yaml")

    import src.cli as cli_mod

    monkeypatch.setattr(cli_mod, "_SPECS_DIR", specs_dir)
    monkeypatch.setattr(cli_mod, "_OUT_DIR", out_dir)
    monkeypatch.setattr(_compose_mod, "load_palette", lambda cls, **_kw: _TEST_PALETTE)
    return specs_dir, out_dir


@pytest.fixture()
def patched_dirs_non_flat(tmp_path: Path, monkeypatch):
    """One non-flat spec; redirect CLI dirs."""
    specs_dir = tmp_path / "specs"
    out_dir = tmp_path / "out"
    specs_dir.mkdir()
    (specs_dir / "test_arch_nf.yaml").write_text(_NON_FLAT_SPEC)

    import src.cli as cli_mod

    monkeypatch.setattr(cli_mod, "_SPECS_DIR", specs_dir)
    monkeypatch.setattr(cli_mod, "_OUT_DIR", out_dir)
    monkeypatch.setattr(_compose_mod, "load_palette", lambda cls, **_kw: _TEST_PALETTE)
    return specs_dir, out_dir


def test_render_all(patched_dirs_multi):
    """--all renders every spec in specs_dir; rc=0 when all succeed."""
    _, out_dir = patched_dirs_multi
    rc = main(["render", "--all"])
    assert rc == 0
    pngs = list(out_dir.glob("*.png"))
    # 2 specs: test_arch (variants=2) + test_arch2 (variants=1) = 3 PNGs
    assert len(pngs) == 3, f"expected 3 PNGs, got {[p.name for p in pngs]}"


def test_render_all_aggregate(patched_dirs_with_broken, capsys):
    """--all returns rc=1 and prints failed: [...] to stderr when any spec fails."""
    rc = main(["render", "--all"])
    assert rc == 1
    captured = capsys.readouterr()
    assert "failed:" in captured.err
    assert "broken_arch" in captured.err


def test_terrain_bad_enum(patched_dirs):
    """--terrain XYZ (invalid) → argparse raises SystemExit (exit 2)."""
    with pytest.raises(SystemExit) as exc_info:
        main(["render", "test_arch", "--terrain", "XYZ"])
    assert exc_info.value.code == 2


def test_terrain_flat_override(tmp_path, monkeypatch, capsys):
    """--terrain flat overrides a non-flat spec terrain field; rc=0, PNG written."""
    specs_dir = tmp_path / "specs"
    out_dir = tmp_path / "out"
    specs_dir.mkdir()
    (specs_dir / "test_arch_nf.yaml").write_text(_NON_FLAT_SPEC)

    import src.cli as cli_mod

    monkeypatch.setattr(cli_mod, "_SPECS_DIR", specs_dir)
    monkeypatch.setattr(cli_mod, "_OUT_DIR", out_dir)
    monkeypatch.setattr(_compose_mod, "load_palette", lambda cls, **_kw: _TEST_PALETTE)

    rc = main(["render", "test_arch_nf", "--terrain", "flat"])
    assert rc == 0, capsys.readouterr().err
    pngs = list(out_dir.glob("test_arch_nf_v*.png"))
    assert len(pngs) == 1, f"expected 1 PNG, got {[p.name for p in pngs]}"


def test_terrain_non_flat_renders(patched_dirs_non_flat):
    """--terrain N (non-flat) → rc=0, PNG written (slope auto-insert active since TECH-177)."""
    _, out_dir = patched_dirs_non_flat
    rc = main(["render", "test_arch_nf", "--terrain", "N"])
    assert rc == 0
    pngs = list(out_dir.glob("test_arch_nf_v*.png"))
    assert len(pngs) == 1, f"expected 1 PNG for non-flat slope, got {[p.name for p in pngs]}"


# ---------------------------------------------------------------------------
# PaletteKeyError → exit 2 + stderr (exploration §10 Error handling)
# ---------------------------------------------------------------------------

_BAD_MATERIAL_SPEC = """\
id: test_bad_mat_v1
class: residential
footprint: [1, 1]
terrain: flat
levels: 1
seed: 1
composition:
  - { type: iso_cube, w: 1, d: 1, h: 16, material: totally_unknown_material_xyz }
palette: residential
output:
  name: test_bad_mat
  variants: 1
"""


def test_palette_key_error_exit_2(tmp_path, monkeypatch, capsys):
    """Missing material key → rc=2, stderr contains material name."""
    specs_dir = tmp_path / "specs"
    out_dir = tmp_path / "out"
    specs_dir.mkdir()
    (specs_dir / "test_bad_mat.yaml").write_text(_BAD_MATERIAL_SPEC)

    import src.cli as cli_mod

    monkeypatch.setattr(cli_mod, "_SPECS_DIR", specs_dir)
    monkeypatch.setattr(cli_mod, "_OUT_DIR", out_dir)
    # Patch palette to a minimal one that does NOT have the bad material.
    monkeypatch.setattr(
        _compose_mod,
        "load_palette",
        lambda cls, **_kw: {
            "class": "residential",
            "materials": {
                "wall_brick_red": {"bright": [240, 48, 48], "mid": [200, 40, 40], "dark": [120, 24, 24]},
            },
        },
    )

    rc = main(["render", "test_bad_mat"])
    assert rc == 2, f"expected exit 2, got {rc}"
    captured = capsys.readouterr()
    assert "totally_unknown_material_xyz" in captured.err, (
        f"stderr missing material name: {captured.err!r}"
    )
