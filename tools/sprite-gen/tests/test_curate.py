"""test_curate.py — TECH-723 (log-promote) + TECH-724 (log-reject).

Covers append-only JSONL logs under curation/, idempotent appends, row
schema, controlled rejection vocab, and filename-parse failures.
"""

from __future__ import annotations

import io
import json
from pathlib import Path
from typing import Any

import pytest
import yaml
from PIL import Image

from src import curate as _curate


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


def _write_spec(
    path: Path,
    *,
    variants: int = 4,
    vary: dict | None = None,
    seed_scope: str = "palette+geometry",
) -> None:
    spec: dict[str, Any] = {
        "id": path.stem,
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
        "output": {"name": path.stem, "variants": variants},
        "diffusion": {"enabled": False},
    }
    if vary is not None:
        spec["variants"] = {"count": variants, "vary": vary, "seed_scope": seed_scope}
    path.write_text(yaml.safe_dump(spec, sort_keys=False), encoding="utf-8")


def _seed_variant_png(path: Path, *, size: tuple[int, int] = (32, 32)) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    # Paint a 16x16 opaque block so palette stats / bbox are non-trivial.
    for y in range(8, 24):
        for x in range(8, 24):
            img.putpixel((x, y), (200, 100, 50, 255))
    img.save(path)


@pytest.fixture()
def curation_env(tmp_path: Path) -> dict:
    specs_dir = tmp_path / "specs"
    specs_dir.mkdir()
    out_dir = tmp_path / "out"
    out_dir.mkdir()
    promoted = tmp_path / "curation" / "promoted.jsonl"
    rejected = tmp_path / "curation" / "rejected.jsonl"

    spec_path = specs_dir / "building_residential_small.yaml"
    _write_spec(spec_path)

    variant = out_dir / "building_residential_small_v01.png"
    _seed_variant_png(variant)

    return {
        "specs_dir": specs_dir,
        "variant": variant,
        "promoted": promoted,
        "rejected": rejected,
    }


# ---------------------------------------------------------------------------
# TECH-723 — log-promote
# ---------------------------------------------------------------------------


def test_log_promote_appends_row(curation_env):
    target = _curate.log_promote(
        curation_env["variant"],
        log_path=curation_env["promoted"],
        specs_dir=curation_env["specs_dir"],
    )
    assert target == curation_env["promoted"]
    lines = target.read_text().splitlines()
    assert len(lines) == 1


def test_log_promote_row_schema(curation_env):
    _curate.log_promote(
        curation_env["variant"],
        log_path=curation_env["promoted"],
        specs_dir=curation_env["specs_dir"],
    )
    row = json.loads(curation_env["promoted"].read_text().splitlines()[0])
    assert set(row.keys()) >= {"variant_path", "vary_values", "bbox",
                               "palette_stats", "timestamp"}
    assert row["variant_path"] == str(curation_env["variant"])
    assert row["bbox"]["width"] > 0
    assert row["bbox"]["height"] > 0
    assert row["palette_stats"]["opaque_count"] > 0
    assert row["palette_stats"]["distinct_colors"] >= 1
    assert isinstance(row["timestamp"], (int, float))


def test_log_promote_history_preserved(curation_env):
    _curate.log_promote(
        curation_env["variant"],
        log_path=curation_env["promoted"],
        specs_dir=curation_env["specs_dir"],
        now=100.0,
    )
    first_bytes = curation_env["promoted"].read_bytes()
    _curate.log_promote(
        curation_env["variant"],
        log_path=curation_env["promoted"],
        specs_dir=curation_env["specs_dir"],
        now=101.0,
    )
    # Second call appends a new row; first row's bytes remain at the head.
    after = curation_env["promoted"].read_bytes()
    assert after.startswith(first_bytes)
    assert len(curation_env["promoted"].read_text().splitlines()) == 2


def test_log_promote_creates_log_file(curation_env):
    assert not curation_env["promoted"].exists()
    _curate.log_promote(
        curation_env["variant"],
        log_path=curation_env["promoted"],
        specs_dir=curation_env["specs_dir"],
    )
    assert curation_env["promoted"].exists()


def test_log_promote_with_vary_captures_values(tmp_path: Path):
    specs_dir = tmp_path / "specs"
    specs_dir.mkdir()
    spec_path = specs_dir / "varied.yaml"
    _write_spec(
        spec_path,
        vary={"roof": {"h_px": {"min": 6, "max": 12}}},
    )
    variant = tmp_path / "out" / "varied_v02.png"
    _seed_variant_png(variant)

    target = tmp_path / "curation" / "promoted.jsonl"
    _curate.log_promote(variant, log_path=target, specs_dir=specs_dir)
    row = json.loads(target.read_text().splitlines()[0])
    # vary_values should reflect the sampled roof.h_px leaf.
    assert "roof" in row["vary_values"]
    assert "h_px" in row["vary_values"]["roof"]
    assert 6 <= row["vary_values"]["roof"]["h_px"] <= 12


def test_log_promote_invalid_variant_filename(tmp_path: Path):
    bad = tmp_path / "not_a_variant.png"
    _seed_variant_png(bad)
    with pytest.raises(_curate.VariantParseError):
        _curate.log_promote(bad, log_path=tmp_path / "curation.jsonl")


def test_log_promote_missing_spec(tmp_path: Path):
    specs_dir = tmp_path / "specs"
    specs_dir.mkdir()
    variant = tmp_path / "out" / "ghost_v01.png"
    _seed_variant_png(variant)
    with pytest.raises(FileNotFoundError):
        _curate.log_promote(variant, log_path=tmp_path / "p.jsonl",
                            specs_dir=specs_dir)


# ---------------------------------------------------------------------------
# TECH-724 — log-reject
# ---------------------------------------------------------------------------


def test_log_reject_valid_reason(curation_env):
    target = _curate.log_reject(
        curation_env["variant"], "roof-too-shallow",
        log_path=curation_env["rejected"],
        specs_dir=curation_env["specs_dir"],
    )
    assert target == curation_env["rejected"]
    row = json.loads(target.read_text().splitlines()[0])
    assert row["reason"] == "roof-too-shallow"
    # Must mirror promoted schema keys plus `reason`.
    assert set(row.keys()) == {"variant_path", "vary_values", "bbox",
                               "palette_stats", "timestamp", "reason"}


def test_log_reject_invalid_reason_exits(curation_env):
    with pytest.raises(_curate.InvalidRejectionReasonError):
        _curate.log_reject(
            curation_env["variant"], "blerg",
            log_path=curation_env["rejected"],
            specs_dir=curation_env["specs_dir"],
        )
    # No row appended on invalid reason.
    assert not curation_env["rejected"].exists()


@pytest.mark.parametrize("reason", list(_curate.REJECTION_REASONS))
def test_log_reject_all_initial_reasons(curation_env, reason):
    _curate.log_reject(
        curation_env["variant"], reason,
        log_path=curation_env["rejected"],
        specs_dir=curation_env["specs_dir"],
    )
    row = json.loads(curation_env["rejected"].read_text().splitlines()[0])
    assert row["reason"] == reason


def test_log_reject_row_schema_matches_log_promote(curation_env):
    _curate.log_promote(
        curation_env["variant"],
        log_path=curation_env["promoted"],
        specs_dir=curation_env["specs_dir"],
        now=100.0,
    )
    _curate.log_reject(
        curation_env["variant"], "roof-too-tall",
        log_path=curation_env["rejected"],
        specs_dir=curation_env["specs_dir"],
        now=100.0,
    )
    prom = json.loads(curation_env["promoted"].read_text().splitlines()[0])
    rej = json.loads(curation_env["rejected"].read_text().splitlines()[0])
    assert set(rej.keys()) - set(prom.keys()) == {"reason"}
    assert set(prom.keys()).issubset(rej.keys())


# ---------------------------------------------------------------------------
# CLI integration — `python -m src log-promote` / `log-reject`
# ---------------------------------------------------------------------------


def test_cli_log_promote_happy(curation_env, monkeypatch, capsys):
    from src import cli as _cli
    monkeypatch.setattr(_curate, "PROMOTED_LOG", curation_env["promoted"])
    monkeypatch.setattr(_curate, "_SPECS_DIR", curation_env["specs_dir"])
    rc = _cli.main(["log-promote", str(curation_env["variant"])])
    assert rc == 0
    assert curation_env["promoted"].exists()
    assert "log-promote" in capsys.readouterr().out


def test_cli_log_reject_happy(curation_env, monkeypatch, capsys):
    from src import cli as _cli
    monkeypatch.setattr(_curate, "REJECTED_LOG", curation_env["rejected"])
    monkeypatch.setattr(_curate, "_SPECS_DIR", curation_env["specs_dir"])
    rc = _cli.main(["log-reject", str(curation_env["variant"]),
                    "--reason", "ground-too-uniform"])
    assert rc == 0
    assert curation_env["rejected"].exists()


def test_cli_log_reject_invalid_reason_argparse(curation_env, capsys):
    from src import cli as _cli
    with pytest.raises(SystemExit) as exc:
        _cli.main(["log-reject", str(curation_env["variant"]),
                   "--reason", "not-a-reason"])
    # argparse rejects unknown `choices` with exit code 2.
    assert exc.value.code == 2


def test_cli_log_promote_bad_filename(tmp_path: Path, capsys):
    from src import cli as _cli
    bad = tmp_path / "not_a_variant.png"
    _seed_variant_png(bad)
    rc = _cli.main(["log-promote", str(bad)])
    assert rc == 1
    assert "not a variant" in capsys.readouterr().err


# ---------------------------------------------------------------------------
# No regression — TECH-179 promote / reject still work (Stage 6.5 guard)
# ---------------------------------------------------------------------------


def test_existing_reject_behavior_unchanged(tmp_path: Path, monkeypatch):
    out = tmp_path / "out"
    for i in range(1, 4):
        _seed_variant_png(out / f"foo_v{i:02d}.png")
    count = _curate.reject("foo", out, confirm=False)
    assert count == 3
    assert not list(out.glob("foo_v*.png"))
