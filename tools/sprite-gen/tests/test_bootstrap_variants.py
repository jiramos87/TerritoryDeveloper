"""Tests for TECH-712 — `bootstrap-variants --from-signature` CLI."""

from __future__ import annotations

import json
from pathlib import Path
from unittest.mock import patch

import pytest
import yaml

from src import cli as cli_mod
from src.cli import (
    _deep_merge_preserve_author,
    _derive_vary_from_signature,
    main,
)


# ---------------------------------------------------------------------------
# Pure-helper unit tests
# ---------------------------------------------------------------------------


def test_derive_vary_from_signature_maps_silhouette_to_roof_h_px():
    sig = {
        "silhouette": {
            "peaks_above_diamond_top": {"px_above_mean": 8.0, "freq": 0.5},
        },
        "bbox": {"height": {"min": 32, "max": 48}},
    }
    vary = _derive_vary_from_signature(sig)
    assert "roof" in vary
    assert "h_px" in vary["roof"]
    leaf = vary["roof"]["h_px"]
    assert leaf["min"] >= 0
    assert leaf["max"] > leaf["min"]


def test_derive_vary_from_signature_bbox_to_footprint_ratio():
    sig = {
        "silhouette": {},
        "bbox": {"height": {"min": 32, "max": 48}},
    }
    vary = _derive_vary_from_signature(sig)
    assert vary["footprint_ratio"]["d"]["min"] == 0.5
    assert vary["footprint_ratio"]["d"]["max"] == 0.75


def test_derive_vary_from_empty_signature_returns_empty():
    assert _derive_vary_from_signature({}) == {}


def test_deep_merge_author_keys_win():
    base = {"roof": {"h_px": {"min": 0, "max": 5}}}
    extra = {
        "roof": {
            "h_px": {"min": 6, "max": 14},  # should NOT overwrite
            "tilt": {"min": 0, "max": 1},  # new → writes
        },
        "footprint_ratio": {"d": {"min": 0.5, "max": 0.9}},  # new → writes
    }
    _deep_merge_preserve_author(base, extra)
    assert base["roof"]["h_px"] == {"min": 0, "max": 5}  # author preserved
    assert base["roof"]["tilt"] == {"min": 0, "max": 1}  # new merged
    assert base["footprint_ratio"]["d"] == {"min": 0.5, "max": 0.9}


# ---------------------------------------------------------------------------
# CLI integration tests
# ---------------------------------------------------------------------------


def _seed_signature(sig_dir: Path, class_name: str) -> Path:
    sig_path = sig_dir / f"{class_name}.signature.json"
    sig_path.write_text(
        json.dumps(
            {
                "class": class_name,
                "silhouette": {
                    "peaks_above_diamond_top": {"px_above_mean": 8, "freq": 0.5}
                },
                "bbox": {"height": {"min": 32, "max": 48}},
            }
        ),
        encoding="utf-8",
    )
    return sig_path


def _seed_spec(specs_dir: Path, stem: str, class_name: str, variants=None) -> Path:
    spec_path = specs_dir / f"{stem}.yaml"
    spec = {
        "id": stem,
        "class": class_name,
        "footprint": [1, 1],
        "terrain": "flat",
        "palette": "residential",
        "output": {"name": stem, "variants": 1},
        "composition": [{"type": "iso_cube", "w": 1, "d": 1, "h": 8, "material": "m"}],
    }
    if variants is not None:
        spec["variants"] = variants
    spec_path.write_text(yaml.safe_dump(spec, sort_keys=False), encoding="utf-8")
    return spec_path


@pytest.fixture
def tool_dirs(tmp_path, monkeypatch):
    specs = tmp_path / "specs"
    signatures = tmp_path / "signatures"
    specs.mkdir()
    signatures.mkdir()
    monkeypatch.setattr(cli_mod, "_SPECS_DIR", specs)
    monkeypatch.setattr(cli_mod, "_SIGNATURES_DIR", signatures)
    return specs, signatures


def test_bootstrap_writes_vary(tool_dirs):
    specs, signatures = tool_dirs
    _seed_signature(signatures, "residential_small")
    _seed_spec(specs, "demo", "residential_small")
    rc = main(["bootstrap-variants", "demo", "--from-signature"])
    assert rc == 0
    updated = yaml.safe_load((specs / "demo.yaml").read_text(encoding="utf-8"))
    assert "variants" in updated
    assert isinstance(updated["variants"], dict)
    vary = updated["variants"]["vary"]
    assert "roof" in vary
    assert "footprint_ratio" in vary


def test_bootstrap_preserves_author_keys(tool_dirs):
    specs, signatures = tool_dirs
    _seed_signature(signatures, "residential_small")
    _seed_spec(
        specs,
        "demo",
        "residential_small",
        variants={
            "count": 4,
            "vary": {"roof": {"h_px": {"min": 99, "max": 100}}},
            "seed_scope": "palette",
        },
    )
    rc = main(["bootstrap-variants", "demo", "--from-signature"])
    assert rc == 0
    updated = yaml.safe_load((specs / "demo.yaml").read_text(encoding="utf-8"))
    # author's custom roof.h_px must survive
    assert updated["variants"]["vary"]["roof"]["h_px"] == {"min": 99, "max": 100}


def test_bootstrap_missing_signature(tool_dirs, capsys):
    specs, _ = tool_dirs
    _seed_spec(specs, "demo", "residential_small")
    rc = main(["bootstrap-variants", "demo", "--from-signature"])
    assert rc == 1
    captured = capsys.readouterr()
    assert "refresh-signatures" in captured.err


def test_bootstrap_missing_spec(tool_dirs, capsys):
    rc = main(["bootstrap-variants", "does_not_exist", "--from-signature"])
    assert rc == 1
    assert "spec not found" in capsys.readouterr().err


def test_bootstrap_without_flag_exits_2(tool_dirs, capsys):
    specs, signatures = tool_dirs
    _seed_signature(signatures, "residential_small")
    _seed_spec(specs, "demo", "residential_small")
    rc = main(["bootstrap-variants", "demo"])
    assert rc == 2
    assert "--from-signature" in capsys.readouterr().err


def test_render_does_not_invoke_bootstrap(tool_dirs, tmp_path, monkeypatch):
    """Render path must not rewrite the spec file on disk."""
    specs, signatures = tool_dirs
    _seed_signature(signatures, "residential_small")
    spec_path = _seed_spec(specs, "demo", "residential_small")
    before = spec_path.read_text(encoding="utf-8")
    # Guard against accidental coupling — patch the bootstrap cmd.
    with patch.object(cli_mod, "_cmd_bootstrap_variants") as guard:
        # Render itself may fail (no palette in tmp); only asserting no-call.
        try:
            main(["render", "demo"])
        except SystemExit:
            pass
        except Exception:
            pass
        guard.assert_not_called()
    after = spec_path.read_text(encoding="utf-8")
    assert before == after
