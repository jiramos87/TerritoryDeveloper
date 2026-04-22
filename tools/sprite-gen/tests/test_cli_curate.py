"""test_cli_curate.py — TECH-180 promote/reject CLI integration."""

from __future__ import annotations

import io
from pathlib import Path

import pytest
from PIL import Image

from src import cli as _cli
from src import curate


def _seed_png(path: Path, size: tuple[int, int] = (64, 64)) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.new("RGBA", size, (0, 0, 0, 0)).save(path)


def test_cli_promote_happy(tmp_path: Path, monkeypatch) -> None:
    generated = tmp_path / "Generated"
    monkeypatch.setattr(curate, "GENERATED_DIR", generated)
    src = tmp_path / "out" / "small_v01.png"
    _seed_png(src)
    rc = _cli.main(["promote", str(src), "--as", "small-01", "--no-push"])
    assert rc == 0
    assert (generated / "small-01.png").exists()
    assert (generated / "small-01.png.meta").exists()


def test_cli_promote_missing_src(tmp_path: Path, capsys) -> None:
    rc = _cli.main(["promote", str(tmp_path / "missing.png"), "--as", "x", "--no-push"])
    assert rc == 1
    err = capsys.readouterr().err
    assert "not found" in err


def test_cli_reject_deletes_variants(tmp_path: Path, monkeypatch) -> None:
    out = tmp_path / "out"
    for i in range(1, 5):
        _seed_png(out / f"foo_v{i:02d}.png")
    monkeypatch.setattr(_cli, "_OUT_DIR", out)
    rc = _cli.main(["reject", "foo", "--yes"])
    assert rc == 0
    assert not list(out.glob("foo_v*.png"))


def test_cli_reject_preserves_siblings(tmp_path: Path, monkeypatch) -> None:
    out = tmp_path / "out"
    _seed_png(out / "foo_v01.png")
    _seed_png(out / "foo_alt_v01.png")
    monkeypatch.setattr(_cli, "_OUT_DIR", out)
    rc = _cli.main(["reject", "foo", "--yes"])
    assert rc == 0
    assert not (out / "foo_v01.png").exists()
    assert (out / "foo_alt_v01.png").exists()


def test_cli_reject_no_match(tmp_path: Path, monkeypatch, capsys) -> None:
    out = tmp_path / "out"
    out.mkdir()
    monkeypatch.setattr(_cli, "_OUT_DIR", out)
    rc = _cli.main(["reject", "foo", "--yes"])
    assert rc == 0
    assert "no matches" in capsys.readouterr().err


def test_cli_reject_requires_confirmation(tmp_path: Path, monkeypatch) -> None:
    out = tmp_path / "out"
    _seed_png(out / "foo_v01.png")
    monkeypatch.setattr(_cli, "_OUT_DIR", out)
    monkeypatch.setattr("sys.stdin", io.StringIO("n\n"))
    rc = _cli.main(["reject", "foo"])
    assert rc == 0
    assert (out / "foo_v01.png").exists()


def test_promote_reject_roundtrip(tmp_path: Path, monkeypatch) -> None:
    out = tmp_path / "out"
    generated = tmp_path / "Generated"
    monkeypatch.setattr(curate, "GENERATED_DIR", generated)
    monkeypatch.setattr(_cli, "_OUT_DIR", out)
    _seed_png(out / "foo_v01.png")
    assert _cli.main(["promote", str(out / "foo_v01.png"), "--as", "foo-01", "--no-push"]) == 0
    assert _cli.main(["reject", "foo", "--yes"]) == 0
    assert (generated / "foo-01.png").exists()
    assert not list(out.glob("foo_v*.png"))
