"""test_curation_loop.py — TECH-728.

Locks the full Stage 6.5 feedback loop end-to-end:

    TECH-723 log-promote →
    TECH-724 log-reject  →
    TECH-725 compute_envelope (aggregator) →
    TECH-726 compose.render score-and-retry gate →
    TECH-727 .needs_review sidecar on exhaustion

Before/after fixtures cover envelope tightening + carve-out direction.
Presence / absence of the sidecar pins the TECH-727 contract. Deterministic
seeds throughout; no filesystem fixtures.
"""

from __future__ import annotations

import json
from pathlib import Path
from typing import Any

import pytest
import yaml
from PIL import Image

from src import compose as _compose
from src.compose import _FLOOR, _score_variant, render
from src.signature import compute_envelope


# ---------------------------------------------------------------------------
# Shared helpers
# ---------------------------------------------------------------------------


def _catalog_fixture() -> dict:
    """Three-entry flat catalog prior spanning a wide roof range."""
    return {"roof.h_px": {"min": 4.0, "max": 20.0}}


def _build_spec(tmp_path: Path, *, variants: int = 2) -> Path:
    """Minimal spec on disk — returns the spec path."""
    spec: dict[str, Any] = {
        "id": "curloop",
        "class": "residential_small",
        "footprint": [1, 1],
        "terrain": "flat",
        "ground": {"material": "grass_flat"},
        "levels": 1,
        "seed": 42,
        "palette": "residential",
        "building": {
            "footprint_ratio": [0.45, 0.45],
            "composition": [
                {"type": "iso_cube", "role": "wall", "material": "wall_brick_red",
                 "w": 1, "d": 1},
                {"type": "iso_prism", "role": "roof", "material": "roof_tile_brown",
                 "w": 1, "d": 1, "h_px": 8, "pitch": 0.5, "axis": "ns",
                 "offset_z_role": "above_walls"},
            ],
        },
        "output": {"name": "curloop", "variants": variants},
        "diffusion": {"enabled": False},
        "variants": {
            "count": variants,
            "seed_scope": "palette+geometry",
            "vary": {"roof": {"h_px": {"min": 6, "max": 12}}},
        },
    }
    p = tmp_path / "curloop.yaml"
    p.write_text(yaml.safe_dump(spec, sort_keys=False), encoding="utf-8")
    return p


def _load_spec(path: Path) -> dict:
    from src.spec import load_spec
    return load_spec(path)


# ---------------------------------------------------------------------------
# TECH-725 — envelope tightening + carve-out direction
# ---------------------------------------------------------------------------


def test_envelope_tightens_after_promotes() -> None:
    catalog = _catalog_fixture()
    promoted = [
        {"variant_path": f"out/v{i}.png", "timestamp": float(i),
         "vary_values": {"roof": {"h_px": 10 + (i % 2)}}}
        for i in range(5)
    ]
    before = compute_envelope(catalog=catalog, promoted=[], rejected=[])
    after = compute_envelope(catalog=catalog, promoted=promoted, rejected=[])
    before_range = before["roof.h_px"]["max"] - before["roof.h_px"]["min"]
    after_range = after["roof.h_px"]["max"] - after["roof.h_px"]["min"]
    assert after_range < before_range


def test_varies_shrink_toward_reason() -> None:
    catalog = _catalog_fixture()
    rejected = [
        {"variant_path": f"out/r{i}.png", "timestamp": float(i),
         "vary_values": {"roof": {"h_px": 6}}, "reason": "roof-too-shallow"}
        for i in range(5)
    ]
    env = compute_envelope(catalog=catalog, promoted=[], rejected=rejected)
    # Nearest reject was h_px = 6 → min carved to 7.
    assert env["roof.h_px"]["min"] >= 7


# ---------------------------------------------------------------------------
# TECH-727 — sidecar presence / absence
# ---------------------------------------------------------------------------


def test_sidecar_on_exhaustion(tmp_path: Path, monkeypatch) -> None:
    """Impossible-to-satisfy envelope → gate exhausts → sidecar lands on disk."""
    spec_path = _build_spec(tmp_path, variants=1)
    spec = _load_spec(spec_path)

    # Force score_variant below _FLOOR on every attempt → gate always exhausts.
    def _fail_score(_vv, _env):
        return {"score": 0.0, "failing_zones": ["roof.h_px"]}

    monkeypatch.setattr(_compose, "_score_variant", _fail_score)

    envelope = {"roof.h_px": {"min": 100.0, "max": 200.0}}  # renders miss the range
    variant_path = tmp_path / "out" / "curloop_v01.png"
    variant_path.parent.mkdir(parents=True, exist_ok=True)

    images = list(
        render(
            spec,
            envelope=envelope,
            retry_cap=5,
            gate_enabled=True,
            variant_paths=[str(variant_path)],
        )
    )
    assert len(images) == 1

    sidecar = variant_path.with_suffix(".needs_review.json")
    assert sidecar.exists()
    data = json.loads(sidecar.read_text())
    assert data["schema_version"] == 1
    assert data["final_score"] == 0.0
    assert data["failing_zones"] == ["roof.h_px"]
    assert len(data["attempted_seeds"]) == 5
    assert data["envelope_snapshot"] == envelope


def test_no_sidecar_on_pass(tmp_path: Path, monkeypatch) -> None:
    """Trivially satisfied envelope → first attempt passes → no sidecar."""
    spec_path = _build_spec(tmp_path, variants=1)
    spec = _load_spec(spec_path)

    def _pass_score(_vv, _env):
        return {"score": 1.0, "failing_zones": []}

    monkeypatch.setattr(_compose, "_score_variant", _pass_score)

    envelope = {"roof.h_px": {"min": 0.0, "max": 1000.0}}
    variant_path = tmp_path / "out" / "curloop_v01.png"
    variant_path.parent.mkdir(parents=True, exist_ok=True)

    list(
        render(
            spec,
            envelope=envelope,
            retry_cap=5,
            gate_enabled=True,
            variant_paths=[str(variant_path)],
        )
    )
    sidecar = variant_path.with_suffix(".needs_review.json")
    assert not sidecar.exists()


# ---------------------------------------------------------------------------
# TECH-726 — gate determinism + flag-off byte-identical
# ---------------------------------------------------------------------------


def test_retry_trajectory_deterministic(tmp_path: Path, monkeypatch) -> None:
    """Two runs with the same seeds produce the same attempted_seeds list."""
    spec_path = _build_spec(tmp_path, variants=1)
    spec = _load_spec(spec_path)

    def _fail_score(_vv, _env):
        return {"score": 0.0, "failing_zones": ["roof.h_px"]}

    monkeypatch.setattr(_compose, "_score_variant", _fail_score)

    envelope = {"roof.h_px": {"min": 100.0, "max": 200.0}}
    variant_a = tmp_path / "a.png"
    variant_b = tmp_path / "b.png"

    list(render(spec, envelope=envelope, retry_cap=4, gate_enabled=True,
                variant_paths=[str(variant_a)]))
    list(render(spec, envelope=envelope, retry_cap=4, gate_enabled=True,
                variant_paths=[str(variant_b)]))

    sc_a = json.loads(variant_a.with_suffix(".needs_review.json").read_text())
    sc_b = json.loads(variant_b.with_suffix(".needs_review.json").read_text())
    assert sc_a["attempted_seeds"] == sc_b["attempted_seeds"]
    # Seed formula: palette_seed + i*(retry_cap+1) + retry, with palette_seed=42,
    # i=0, retry_cap=4 → 42, 43, 44, 45 (4 retries).
    assert sc_a["attempted_seeds"] == [42, 43, 44, 45]


def test_flag_off_byte_identical(tmp_path: Path) -> None:
    """Flag off / envelope=None → delegates to compose_sprite(sample_variant(...))."""
    spec_path = _build_spec(tmp_path, variants=2)
    spec = _load_spec(spec_path)

    # Baseline — direct pre-Stage-6.5 path.
    from src.compose import compose_sprite, sample_variant
    baseline = [compose_sprite(sample_variant(spec, i)) for i in range(2)]

    # Flag-off render() generator.
    got = list(render(spec, envelope=None, gate_enabled=True))
    assert len(got) == len(baseline)
    for b, g in zip(baseline, got):
        assert isinstance(g, Image.Image)
        assert b.tobytes() == g.tobytes()

    # gate_enabled=False with non-None envelope is also the flag-off path.
    envelope = {"roof.h_px": {"min": 6.0, "max": 12.0}}
    got_off = list(render(spec, envelope=envelope, gate_enabled=False))
    for b, g in zip(baseline, got_off):
        assert b.tobytes() == g.tobytes()


# ---------------------------------------------------------------------------
# TECH-726 — carved-zone hard-fail
# ---------------------------------------------------------------------------


def test_gate_carved_zone_hard_fail() -> None:
    """Sample outside envelope bounds → score forced to 0.0."""
    envelope = {"roof.h_px": {"min": 8.0, "max": 12.0}}
    vary_values = {"roof": {"h_px": 20}}  # outside [8, 12]
    result = _score_variant(vary_values, envelope)
    assert result["score"] == 0.0
    assert result["failing_zones"] == ["roof.h_px"]
