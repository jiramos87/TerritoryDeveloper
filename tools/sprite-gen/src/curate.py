"""Curation helpers for sprite-gen (TECH-179+)."""

from __future__ import annotations

import shutil
import subprocess
import sys
import tempfile
from pathlib import Path
from typing import Any, Mapping

from PIL import Image

from .unity_meta import write_meta

_SRC_DIR = Path(__file__).resolve().parent
_TOOL_ROOT = _SRC_DIR.parent
_REPO_ROOT = _TOOL_ROOT.parent.parent
GENERATED_DIR = _REPO_ROOT / "Assets" / "Sprites" / "Generated"
_SPECS_DIR = _TOOL_ROOT / "specs"


class PromoteEditFlagError(ValueError):
    """--edit / source suffix combination invalid."""


def reject(
    archetype: str,
    out_dir: Path,
    confirm: bool = True,
    stdin=None,
    stderr=None,
) -> int:
    """Delete `out/{archetype}_v*.png` matches; return count deleted."""
    stdin = stdin or sys.stdin
    stderr = stderr or sys.stderr
    matches = sorted(out_dir.glob(f"{archetype}_v*.png"))
    if not matches:
        print(f"no matches for {archetype}", file=stderr)
        return 0
    if confirm:
        print(f"about to delete {len(matches)} file(s):", file=stderr)
        for m in matches:
            print(f"  {m}", file=stderr)
        print("proceed? [y/N] ", file=stderr, end="")
        stderr.flush()
        answer = stdin.readline().strip().lower()
        if answer != "y":
            return 0
    for m in matches:
        m.unlink()
    return len(matches)


def _flatten_aseprite(src: Path, aseprite_bin: Path) -> Path:
    fd, name = tempfile.mkstemp(suffix=".png")
    import os

    os.close(fd)
    tmp = Path(name)
    tmp.unlink(missing_ok=True)
    try:
        subprocess.run(
            [str(aseprite_bin), "--batch", str(src), "--save-as", str(tmp)],
            check=True,
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
    except Exception:
        if tmp.exists():
            tmp.unlink()
        raise
    if not tmp.exists() or tmp.stat().st_size == 0:
        if tmp.exists():
            tmp.unlink()
        raise subprocess.CalledProcessError(1, str(aseprite_bin), "empty or missing PNG output")
    return tmp


def _load_spec_meta_for_dest(dest_name: str) -> dict[str, Any]:
    stem = dest_name.replace("-", "_")
    for c in (stem, dest_name):
        p = _SPECS_DIR / f"{c}.yaml"
        if p.exists():
            try:
                import yaml

                data = yaml.safe_load(p.read_text(encoding="utf-8"))
                if isinstance(data, dict):
                    return {
                        "id": str(data.get("id", c)),
                        "class": str(data.get("class", "unknown")),
                    }
            except Exception:
                break
    return {"id": dest_name, "class": "unknown"}


def build_catalog_payload(
    dest_name: str,
    canvas_h: int,
    spec_meta: Mapping[str, Any],
) -> dict[str, Any]:
    pivot_y = 16.0 / float(canvas_h)
    return {
        "slug": dest_name,
        "category": spec_meta.get("class", "unknown"),
        "display_name": dest_name.replace("-", " ").title(),
        "status": "draft",
        "world_sprite_path": f"Assets/Sprites/Generated/{dest_name}.png",
        "ppu": 64,
        "pivot": {"x": 0.5, "y": pivot_y},
        "generator_archetype_id": spec_meta.get("id", dest_name),
        "economy": {"base_cost_cents": 0, "monthly_upkeep_cents": 0},
        "sprite_binds": [],
    }


def rows_match_for_idempotency(existing: Mapping[str, Any], payload: Mapping[str, Any]) -> bool:
    return (
        existing.get("world_sprite_path") == payload.get("world_sprite_path")
        and existing.get("generator_archetype_id") == payload.get("generator_archetype_id")
    )


def push_catalog_for_promote(dest_name: str, canvas_h: int) -> None:
    from .registry_client import ConflictError, RegistryClient, resolve_catalog_url

    spec_meta = _load_spec_meta_for_dest(dest_name)
    payload = build_catalog_payload(dest_name, canvas_h, spec_meta)
    client = RegistryClient(resolve_catalog_url())
    try:
        client.create_asset(payload)
    except ConflictError:
        existing = client.get_asset_by_slug(dest_name)
        if existing is None:
            raise
        if rows_match_for_idempotency(existing, payload):
            return
        asset_id = int(str(existing.get("id", "0")))
        updated_at = str(existing.get("updated_at", ""))
        client.patch_asset(asset_id, payload, updated_at)


def promote(
    src: str | Path,
    dest_name: str,
    *,
    edit: bool = False,
    push: bool = False,
) -> Path:
    """Promote *src* into Generated with Unity `.meta`; optional flatten + catalog push."""
    src_path = Path(src)
    if not src_path.exists():
        raise FileNotFoundError(f"source PNG not found: {src_path}")

    tmp_png: Path | None = None
    work_src = src_path

    if src_path.suffix.lower() == ".aseprite":
        if not edit:
            raise PromoteEditFlagError(".aseprite source requires --edit")
        from .aseprite_bin import find_aseprite_bin

        tmp_png = _flatten_aseprite(src_path, find_aseprite_bin())
        work_src = tmp_png
    elif edit:
        raise PromoteEditFlagError("--edit only valid for .aseprite sources")

    GENERATED_DIR.mkdir(parents=True, exist_ok=True)
    dest_png = GENERATED_DIR / f"{dest_name}.png"
    dest_meta = dest_png.with_suffix(".png.meta")

    shutil.copyfile(work_src, dest_png)
    if tmp_png and tmp_png.exists():
        tmp_png.unlink()

    try:
        canvas_h = Image.open(dest_png).size[1]
        dest_meta.write_text(write_meta(dest_png, canvas_h), encoding="utf-8")
    except Exception:
        if dest_png.exists():
            dest_png.unlink()
        if dest_meta.exists():
            dest_meta.unlink()
        raise

    if push:
        push_catalog_for_promote(dest_name, canvas_h)

    return dest_png
