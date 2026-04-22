"""Resolve the Aseprite binary path (TECH-181)."""

from __future__ import annotations

import os
import sys
import tomllib
from pathlib import Path

_INSTALL_HINT = (
    "Aseprite binary not found. Install from https://www.aseprite.org/download/ "
    "or set $ASEPRITE_BIN / [aseprite] bin in tools/sprite-gen/config.toml."
)


class AsepriteBinNotFoundError(RuntimeError):
    """Raised when no probe resolves to an executable Aseprite binary."""


def _is_executable(p: Path) -> bool:
    return p.exists() and p.is_file() and os.access(p, os.X_OK)


def _probe_env() -> Path | None:
    raw = os.environ.get("ASEPRITE_BIN")
    if not raw:
        return None
    p = Path(raw)
    return p if _is_executable(p) else None


def _probe_config(config_path: Path) -> Path | None:
    if not config_path.exists():
        return None
    data = tomllib.loads(config_path.read_text(encoding="utf-8"))
    raw = (data.get("aseprite") or {}).get("bin")
    if not raw:
        return None
    p = Path(str(raw))
    return p if _is_executable(p) else None


def _probe_platform() -> Path | None:
    if sys.platform != "darwin":
        return None
    candidates = [
        Path("/Applications/Aseprite.app/Contents/MacOS/aseprite"),
        Path.home() / "Library/Application Support/Steam/steamapps/common/Aseprite/Aseprite.app/Contents/MacOS/aseprite",
    ]
    for p in candidates:
        if _is_executable(p):
            return p
    return None


def find_aseprite_bin(config_path: Path | None = None) -> Path:
    config_path = config_path or (Path(__file__).resolve().parent.parent / "config.toml")
    for probe in (_probe_env, lambda: _probe_config(config_path), _probe_platform):
        hit = probe()
        if hit is not None:
            return hit
    raise AsepriteBinNotFoundError(_INSTALL_HINT)
