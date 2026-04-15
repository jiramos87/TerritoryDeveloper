"""
test_slope_regression.py — Slope regression tests.

Contracts:
    test_n_slope_canvas_grows       — N-slope spec yields PNG h > 64 and pivot_uv != (0.5, 0.25)
    test_all_17_slope_ids_render    — all 17 non-flat slope ids render without crash (exit 0 + PNG present)
    test_flat_sentinel_byte_stable  — flat override keeps h == 64 and pivot_uv == (0.5, 0.25)

Reference:
    sprite-gen-master-plan.md T1.4.4
"""

from __future__ import annotations

import sys
from pathlib import Path

import pytest
from PIL import Image

import src.cli as _cli_mod
from src.canvas import pivot_uv
from src.cli import _VALID_SLOPE_IDS, main


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _open_latest_png(out_dir: Path, name_stem: str) -> Image.Image:
    """Return the first PNG matching {name_stem}*.png under out_dir."""
    matches = sorted(out_dir.glob(f"{name_stem}*.png"))
    assert matches, f"No PNG matching {name_stem}*.png found in {out_dir}"
    return Image.open(matches[0])


# ---------------------------------------------------------------------------
# Phase 1 / §7 test_n_slope_canvas_grows
# ---------------------------------------------------------------------------

def test_n_slope_canvas_grows(monkeypatch, tmp_path):
    """N-slope spec renders PNG with h > 64 and pivot_uv != (0.5, 0.25)."""
    monkeypatch.setattr(_cli_mod, "_OUT_DIR", tmp_path)

    rc = main(["render", "building_residential_small_N"])
    assert rc == 0, f"cli.main returned {rc} (expected 0)"

    img = _open_latest_png(tmp_path, "building_residential_small_N")
    assert img.height > 64, (
        f"Expected canvas height > 64 px (slope grew canvas) but got {img.height}"
    )
    actual_pivot = pivot_uv(img.height)
    assert actual_pivot != (0.5, 0.25), (
        f"Expected pivot_uv != (0.5, 0.25) for slope canvas but got {actual_pivot}"
    )


# ---------------------------------------------------------------------------
# Phase 2 / §7 test_all_17_slope_ids_render
# ---------------------------------------------------------------------------

_NON_FLAT_IDS = sorted(_VALID_SLOPE_IDS - {"flat"})


@pytest.mark.parametrize("slope_id", _NON_FLAT_IDS)
def test_all_17_slope_ids_render(slope_id: str, monkeypatch, tmp_path):
    """Every non-flat slope id renders via --terrain override without crash."""
    monkeypatch.setattr(_cli_mod, "_OUT_DIR", tmp_path)

    rc = main(["render", "building_residential_small", "--terrain", slope_id])
    assert rc == 0, f"cli.main returned {rc} for slope_id={slope_id!r} (expected 0)"

    # Output PNG must exist (name comes from spec output.name).
    pngs = list(tmp_path.glob("building_residential_small*.png"))
    assert pngs, (
        f"No output PNG found for slope_id={slope_id!r} under {tmp_path}"
    )


# ---------------------------------------------------------------------------
# Phase 2 / §7 test_flat_sentinel_byte_stable
# ---------------------------------------------------------------------------

def test_flat_sentinel_byte_stable(monkeypatch, tmp_path):
    """--terrain flat override keeps h == 64 and pivot_uv == (0.5, 0.25)."""
    monkeypatch.setattr(_cli_mod, "_OUT_DIR", tmp_path)

    rc = main(["render", "building_residential_small", "--terrain", "flat"])
    assert rc == 0, f"cli.main returned {rc} (expected 0)"

    img = _open_latest_png(tmp_path, "building_residential_small")
    assert img.height == 64, (
        f"Expected flat sentinel h == 64 px but got {img.height}"
    )
    actual_pivot = pivot_uv(img.height)
    assert actual_pivot == (0.5, 0.25), (
        f"Expected pivot_uv == (0.5, 0.25) for flat canvas but got {actual_pivot}"
    )
