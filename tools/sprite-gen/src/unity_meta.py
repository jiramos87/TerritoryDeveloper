"""Unity .meta YAML writer for promoted sprites (TECH-179)."""

from __future__ import annotations

import re
import uuid
from pathlib import Path

_GUID_RE = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.MULTILINE)


def _read_existing_guid(meta_path: Path) -> str | None:
    if not meta_path.exists():
        return None
    match = _GUID_RE.search(meta_path.read_text(encoding="utf-8"))
    return match.group(1) if match else None


def write_meta(png_path: str | Path, canvas_h: int) -> str:
    """Return Unity `.meta` YAML string for *png_path* at *canvas_h* px tall."""
    png_path = Path(png_path)
    meta_path = png_path.with_suffix(png_path.suffix + ".meta")
    guid = _read_existing_guid(meta_path) or uuid.uuid4().hex
    pivot_y = 16.0 / float(canvas_h)
    return (
        "fileFormatVersion: 2\n"
        f"guid: {guid}\n"
        "TextureImporter:\n"
        "  serializedVersion: 11\n"
        "  spritePixelsToUnits: 64\n"
        f"  spritePivot: {{x: 0.5, y: {pivot_y}}}\n"
        "  filterMode: 0\n"
        "  textureCompression: 0\n"
        "  spriteMode: 1\n"
        "  spriteMeshType: 0\n"
    )
