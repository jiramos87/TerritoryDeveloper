"""test_aseprite_bin.py — TECH-181 probe-order tests."""

from __future__ import annotations

import stat
from pathlib import Path

import pytest

from src.aseprite_bin import AsepriteBinNotFoundError, find_aseprite_bin


def _exec_file(path: Path) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("#!/bin/sh\nexit 0\n", encoding="utf-8")
    path.chmod(path.stat().st_mode | stat.S_IXUSR | stat.S_IXGRP | stat.S_IXOTH)
    return path


def _nonexec_file(path: Path) -> Path:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("x", encoding="utf-8")
    path.chmod(0o644)
    return path


def test_env_var_wins(tmp_path: Path, monkeypatch) -> None:
    bin_path = _exec_file(tmp_path / "ase_env")
    monkeypatch.setenv("ASEPRITE_BIN", str(bin_path))
    cfg = tmp_path / "config.toml"
    cfg.write_text('[aseprite]\nbin = "/ignored"\n', encoding="utf-8")
    assert find_aseprite_bin(cfg) == bin_path


def test_env_fallthrough_on_missing(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.setenv("ASEPRITE_BIN", str(tmp_path / "missing"))
    cfg_bin = _exec_file(tmp_path / "ase_cfg")
    cfg = tmp_path / "config.toml"
    cfg.write_text(f'[aseprite]\nbin = "{cfg_bin}"\n', encoding="utf-8")
    assert find_aseprite_bin(cfg) == cfg_bin


def test_env_fallthrough_on_non_executable(tmp_path: Path, monkeypatch) -> None:
    env_path = _nonexec_file(tmp_path / "ase_nonexec")
    monkeypatch.setenv("ASEPRITE_BIN", str(env_path))
    cfg_bin = _exec_file(tmp_path / "ase_cfg")
    cfg = tmp_path / "config.toml"
    cfg.write_text(f'[aseprite]\nbin = "{cfg_bin}"\n', encoding="utf-8")
    assert find_aseprite_bin(cfg) == cfg_bin


def test_config_toml_resolved(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.delenv("ASEPRITE_BIN", raising=False)
    cfg_bin = _exec_file(tmp_path / "ase_cfg")
    cfg = tmp_path / "config.toml"
    cfg.write_text(f'[aseprite]\nbin = "{cfg_bin}"\n', encoding="utf-8")
    assert find_aseprite_bin(cfg) == cfg_bin


def test_platform_probe_macos(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.delenv("ASEPRITE_BIN", raising=False)
    monkeypatch.setattr("sys.platform", "darwin")
    fake_app = tmp_path / "Applications" / "Aseprite.app" / "Contents" / "MacOS" / "aseprite"
    _exec_file(fake_app)
    import src.aseprite_bin as ab

    monkeypatch.setattr(ab, "_probe_platform", lambda: fake_app)
    cfg = tmp_path / "cfg.toml"
    cfg.write_text("[placeholder]\n", encoding="utf-8")
    assert find_aseprite_bin(cfg) == fake_app


def test_not_found_raises(tmp_path: Path, monkeypatch) -> None:
    monkeypatch.delenv("ASEPRITE_BIN", raising=False)
    import src.aseprite_bin as ab

    monkeypatch.setattr(ab, "_probe_platform", lambda: None)
    cfg = tmp_path / "none.toml"
    cfg.write_text("[placeholder]\n", encoding="utf-8")
    with pytest.raises(AsepriteBinNotFoundError):
        find_aseprite_bin(cfg)
