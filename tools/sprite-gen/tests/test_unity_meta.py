"""test_unity_meta.py — TECH-179 unit tests."""

from __future__ import annotations

from pathlib import Path

import pytest
import yaml
from PIL import Image

from src import curate
from src.unity_meta import write_meta


def _parse(meta_yaml: str) -> dict:
    return yaml.safe_load(meta_yaml)


def test_write_meta_basic(tmp_path: Path) -> None:
    yml = _parse(write_meta(tmp_path / "x.png", 64))
    ti = yml["TextureImporter"]
    assert ti["spritePixelsToUnits"] == 64
    assert ti["spritePivot"]["x"] == 0.5
    assert ti["spritePivot"]["y"] == 0.25
    assert ti["filterMode"] == 0
    assert ti["textureCompression"] == 0
    assert ti["spriteMode"] == 1


def test_write_meta_pivot_recompute_128(tmp_path: Path) -> None:
    yml = _parse(write_meta(tmp_path / "x.png", 128))
    assert yml["TextureImporter"]["spritePivot"]["y"] == 0.125


def test_write_meta_pivot_slope_80(tmp_path: Path) -> None:
    yml = _parse(write_meta(tmp_path / "x.png", 80))
    assert yml["TextureImporter"]["spritePivot"]["y"] == 0.2


def test_promote_creates_png_and_meta(tmp_path: Path, monkeypatch) -> None:
    generated = tmp_path / "Generated"
    monkeypatch.setattr(curate, "GENERATED_DIR", generated)
    src = tmp_path / "small.png"
    Image.new("RGBA", (64, 64), (0, 0, 0, 0)).save(src)
    curate.promote(src, "promoted_small", push=False)
    assert (generated / "promoted_small.png").exists()
    assert (generated / "promoted_small.png.meta").exists()


def test_promote_preserves_guid_on_rerun(tmp_path: Path, monkeypatch) -> None:
    generated = tmp_path / "Generated"
    monkeypatch.setattr(curate, "GENERATED_DIR", generated)
    src = tmp_path / "small.png"
    Image.new("RGBA", (64, 64), (0, 0, 0, 0)).save(src)
    curate.promote(src, "foo", push=False)
    meta1 = (generated / "foo.png.meta").read_text(encoding="utf-8")
    curate.promote(src, "foo", push=False)
    meta2 = (generated / "foo.png.meta").read_text(encoding="utf-8")
    guid1 = yaml.safe_load(meta1)["guid"]
    guid2 = yaml.safe_load(meta2)["guid"]
    assert guid1 == guid2


def test_promote_missing_src_raises(tmp_path: Path) -> None:
    with pytest.raises(FileNotFoundError):
        curate.promote(tmp_path / "missing.png", "x", push=False)
