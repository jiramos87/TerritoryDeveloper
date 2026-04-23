"""Unit tests for src.signature (TECH-704)."""

from __future__ import annotations

import json
from pathlib import Path

import pytest
from PIL import Image

from src.signature import (
    SignatureStaleError,
    ValidationReport,
    compute_signature,
    validate_against,
)


# ---------------------------------------------------------------------------
# Fixtures
# ---------------------------------------------------------------------------


def _write_solid(path: Path, size=(64, 64), rgb=(120, 80, 60), top_band=12) -> None:
    """Write a deterministic RGBA sprite: a solid rectangle with an alpha box."""
    img = Image.new("RGBA", size, (0, 0, 0, 0))
    px = img.load()
    w, h = size
    # Fill a centred rectangle of height h-top_band*2 to get a non-trivial bbox.
    for y in range(top_band, h - 4):
        for x in range(2, w - 2):
            px[x, y] = (rgb[0], rgb[1], rgb[2], 255)
    # Bottom row = greenish "ground" band (for _measure_ground to register).
    for y in range(h - 4, h):
        for x in range(w):
            px[x, y] = (80, 160, 70, 255)
    img.save(path)


@pytest.fixture
def fallback_graph(tmp_path: Path) -> Path:
    p = tmp_path / "_fallback.json"
    p.write_text(json.dumps({"residential_small": "residential_row"}))
    return p


@pytest.fixture
def one_sprite(tmp_path: Path) -> Path:
    folder = tmp_path / "one"
    folder.mkdir()
    _write_solid(folder / "a.png", rgb=(140, 100, 80))
    return folder


@pytest.fixture
def three_sprites(tmp_path: Path) -> Path:
    folder = tmp_path / "three"
    folder.mkdir()
    for i, rgb in enumerate([(140, 100, 80), (130, 95, 75), (150, 105, 85)]):
        _write_solid(folder / f"s{i}.png", rgb=rgb, top_band=12 + i)
    return folder


# ---------------------------------------------------------------------------
# L15 — sample-size branches
# ---------------------------------------------------------------------------


def test_l15_fallback(tmp_path: Path, fallback_graph: Path) -> None:
    empty = tmp_path / "empty"
    empty.mkdir()
    sig = compute_signature(
        "residential_small",
        str(empty / "*.png"),
        fallback_graph_path=fallback_graph,
    )
    assert sig["mode"] == "fallback"
    assert sig["fallback_of"] == "residential_row"
    assert sig["source_count"] == 0
    assert sig["bbox"] is None


def test_l15_point_match(one_sprite: Path) -> None:
    sig = compute_signature("residential_small", str(one_sprite / "*.png"))
    assert sig["mode"] == "point-match"
    assert sig["source_count"] == 1
    # Point-match means min == max == mean per scalar.
    h = sig["bbox"]["height"]
    assert h["min"] == h["max"] == h["mean"]


def test_l15_envelope(three_sprites: Path) -> None:
    sig = compute_signature("residential_small", str(three_sprites / "*.png"))
    assert sig["mode"] == "envelope"
    assert sig["source_count"] == 3
    h = sig["bbox"]["height"]
    assert h["min"] <= h["mean"] <= h["max"]
    assert h["min"] <= h["max"]


# ---------------------------------------------------------------------------
# JSON shape
# ---------------------------------------------------------------------------


def test_signature_shape_envelope(three_sprites: Path) -> None:
    sig = compute_signature("residential_small", str(three_sprites / "*.png"))
    for key in (
        "class",
        "refreshed_at",
        "source_count",
        "source_checksum",
        "mode",
        "fallback_of",
        "bbox",
        "palette",
        "silhouette",
        "ground",
        "decoration_hints",
    ):
        assert key in sig, f"missing key: {key}"
    assert sig["source_checksum"].startswith("sha256:")


# ---------------------------------------------------------------------------
# Staleness guard (L3)
# ---------------------------------------------------------------------------


def test_stale_checksum(three_sprites: Path) -> None:
    sig = compute_signature("residential_small", str(three_sprites / "*.png"))
    # Add a new sprite -> checksum drifts.
    _write_solid(three_sprites / "new.png", rgb=(200, 200, 200))
    live = sorted(three_sprites.glob("*.png"))
    img = Image.open(live[0]).convert("RGBA")
    with pytest.raises(SignatureStaleError) as exc:
        validate_against(sig, img, live_sources=live)
    assert "refresh-signatures" in str(exc.value)


# ---------------------------------------------------------------------------
# validate_against happy/fail paths
# ---------------------------------------------------------------------------


def test_validate_ok(three_sprites: Path) -> None:
    sig = compute_signature("residential_small", str(three_sprites / "*.png"))
    # Use one of the source images as the "rendered" candidate — must fall
    # inside the envelope.
    source = sorted(three_sprites.glob("*.png"))[0]
    img = Image.open(source).convert("RGBA")
    report = validate_against(sig, img)
    assert isinstance(report, ValidationReport)
    assert report.ok is True, report.failures


def test_validate_fail_bbox_out_of_range(three_sprites: Path) -> None:
    sig = compute_signature("residential_small", str(three_sprites / "*.png"))
    # Render a much smaller sprite -> bbox height below envelope min.
    tiny = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    px = tiny.load()
    for y in range(30, 34):
        for x in range(10, 54):
            px[x, y] = (100, 100, 100, 255)
    report = validate_against(sig, tiny)
    assert report.ok is False
    assert any("bbox.height" in f for f in report.failures)


def test_validate_fallback_mode_always_passes(tmp_path: Path, fallback_graph: Path) -> None:
    empty = tmp_path / "empty"
    empty.mkdir()
    sig = compute_signature(
        "residential_small",
        str(empty / "*.png"),
        fallback_graph_path=fallback_graph,
    )
    blank = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    report = validate_against(sig, blank)
    assert report.ok is True
