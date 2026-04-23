"""Curation helpers for sprite-gen (TECH-179+; Stage 6.5 TECH-723/724)."""

from __future__ import annotations

import json
import re
import shutil
import subprocess
import sys
import tempfile
import time
from pathlib import Path
from typing import Any, Mapping

from PIL import Image

from .unity_meta import write_meta

_SRC_DIR = Path(__file__).resolve().parent
_TOOL_ROOT = _SRC_DIR.parent
_REPO_ROOT = _TOOL_ROOT.parent.parent
GENERATED_DIR = _REPO_ROOT / "Assets" / "Sprites" / "Generated"
_SPECS_DIR = _TOOL_ROOT / "specs"

# ---------------------------------------------------------------------------
# TECH-723 / TECH-724 — curation feedback-log targets + parse helpers.
# Append-only JSONL under tools/sprite-gen/curation/. Verbs `log-promote`
# and `log-reject` are orthogonal to the TECH-179 `promote` (PNG→Unity
# ship) and `reject` (glob-delete) verbs.
# ---------------------------------------------------------------------------

_CURATION_DIR = _TOOL_ROOT / "curation"
PROMOTED_LOG = _CURATION_DIR / "promoted.jsonl"
REJECTED_LOG = _CURATION_DIR / "rejected.jsonl"

# TECH-724 — controlled rejection reason vocabulary.
REJECTION_REASONS: tuple[str, ...] = (
    "roof-too-shallow",
    "roof-too-tall",
    "facade-too-saturated",
    "ground-too-uniform",
)

# Render filename convention = `{name}_v{NN}.png` (`_render_one` in cli.py).
# `NN` is 1-based zero-padded; the 0-based index passed to
# `compose.sample_variant` is `NN - 1`.
_VARIANT_FILENAME_RE = re.compile(r"^(?P<stem>.+)_v(?P<idx>\d+)\.png$")


class PromoteEditFlagError(ValueError):
    """--edit / source suffix combination invalid."""


class InvalidRejectionReasonError(ValueError):
    """`--reason <tag>` not in :data:`REJECTION_REASONS` (TECH-724)."""


class VariantParseError(ValueError):
    """Filename does not match the render naming pattern."""


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


# ---------------------------------------------------------------------------
# TECH-723 / TECH-724 — Curation feedback logs (log-promote / log-reject)
# ---------------------------------------------------------------------------


def _parse_variant_path(variant_path: str | Path) -> tuple[str, int]:
    """Parse `{stem}_v{NN}.png` → `(stem, variant_idx_0_based)`.

    `NN` is 1-based in the render filename; the ``variant_idx`` that
    ``compose.sample_variant`` expects is 0-based.

    Raises:
        VariantParseError: Filename does not match the pattern.
    """
    name = Path(variant_path).name
    m = _VARIANT_FILENAME_RE.match(name)
    if not m:
        raise VariantParseError(
            f"not a variant PNG (expected `{{stem}}_v{{NN}}.png`): {variant_path}"
        )
    stem = m.group("stem")
    one_based = int(m.group("idx"))
    if one_based <= 0:
        raise VariantParseError(f"variant index must be >= 1: {variant_path}")
    return stem, one_based - 1


def _resolve_spec_path(stem: str, *, specs_dir: Path | None = None) -> Path:
    """Map a rendered-variant stem to its owning spec yaml.

    The loader tries `{stem}.yaml` first, then `{stem.replace('-', '_')}.yaml`
    (mirroring :func:`_load_spec_meta_for_dest`). `specs_dir` is injectable
    for tests.
    """
    base = specs_dir if specs_dir is not None else _SPECS_DIR
    for candidate in (stem, stem.replace("-", "_")):
        p = base / f"{candidate}.yaml"
        if p.exists():
            return p
    raise FileNotFoundError(f"spec not found for variant stem {stem!r} under {base}")


def _load_vary_values(variant_path: str | Path, *, specs_dir: Path | None = None) -> dict:
    """Re-sample `vary:` leaves for *variant_path* via deterministic sampler.

    Leans on ``compose.sample_variant`` (TECH-711 split seeds); no sidecar
    files needed. Returns the diff between base spec and sampled spec —
    only leaves under ``variants.vary`` that resolved to a concrete value.
    """
    from .compose import sample_variant  # local import avoids circular cost
    from .spec import load_spec

    stem, idx0 = _parse_variant_path(variant_path)
    spec_path = _resolve_spec_path(stem, specs_dir=specs_dir)
    spec = load_spec(spec_path)
    sampled = sample_variant(spec, idx0)
    return _diff_vary_leaves(spec, sampled)


def _diff_vary_leaves(base_spec: Mapping[str, Any], sampled_spec: Mapping[str, Any]) -> dict:
    """Return a nested dict of only the `vary.*` leaves that were sampled.

    Walks ``sampled_spec["variants"]["vary"]`` leaf paths; each leaf
    corresponds to a resolved value in ``sampled_spec`` (or in
    ``sampled_spec["ground"]`` for vary.ground). Emits a nested dict
    mirroring the vary-tree shape with concrete values only.
    """
    variants = sampled_spec.get("variants") if isinstance(sampled_spec, Mapping) else None
    if not isinstance(variants, Mapping):
        return {}
    vary = variants.get("vary")
    if not isinstance(vary, Mapping):
        return {}

    out: dict[str, Any] = {}
    for path in _walk_vary_leaf_paths(vary, ()):
        value = _read_sampled_value(sampled_spec, path)
        if value is None:
            continue
        _set_nested(out, path, value)
    return out


def _walk_vary_leaf_paths(node: Any, prefix: tuple[str, ...]):
    """Yield tuple paths to every leaf (`{min,max}` or `{values:…}`) of vary."""
    if not isinstance(node, Mapping):
        return
    if ("min" in node and "max" in node) or "values" in node:
        yield prefix
        return
    for key, sub in node.items():
        yield from _walk_vary_leaf_paths(sub, prefix + (key,))


def _read_sampled_value(sampled: Mapping[str, Any], path: tuple[str, ...]) -> Any:
    """Read the concrete value for a given vary-path from a sampled spec.

    `vary.ground.*` maps into ``sampled["ground"][...]`` (TECH-720 rule);
    everything else walks ``sampled[...]`` directly.
    """
    if not path:
        return None
    root = path[0]
    if root == "ground":
        cursor: Any = sampled.get("ground")
        for part in path[1:]:
            if not isinstance(cursor, Mapping):
                return None
            cursor = cursor.get(part)
        if isinstance(cursor, Mapping) and "min" in cursor and "max" in cursor:
            # vary.ground.*_jitter sampled form `{min: v, max: v}` (compose._apply_vary_ground).
            lo, hi = cursor.get("min"), cursor.get("max")
            if lo == hi:
                return lo
        return cursor
    # Non-ground: walk sampled dict directly. Paths like ('roof', 'h_px').
    cursor2: Any = sampled
    for part in path:
        if not isinstance(cursor2, Mapping):
            return None
        cursor2 = cursor2.get(part)
    return cursor2


def _set_nested(target: dict, path: tuple[str, ...], value: Any) -> None:
    cursor = target
    for key in path[:-1]:
        sub = cursor.get(key)
        if not isinstance(sub, dict):
            sub = {}
            cursor[key] = sub
        cursor = sub
    cursor[path[-1]] = value


def _measure_variant(variant_path: str | Path) -> tuple[dict, dict]:
    """Return `(bbox_dict, palette_stats_dict)` for a rendered variant PNG."""
    img = Image.open(variant_path).convert("RGBA")
    return _measure_bbox(img), _measure_palette_stats(img)


def _measure_bbox(img: Image.Image) -> dict:
    box = img.getbbox()
    if box is None:
        return {"x0": 0, "y0": 0, "width": 0, "height": 0}
    x0, y0, x1, y1 = box
    return {"x0": int(x0), "y0": int(y0), "width": int(x1 - x0), "height": int(y1 - y0)}


def _measure_palette_stats(img: Image.Image) -> dict:
    """Summarise opaque pixels: count, mean RGB, distinct color count."""
    pixels = [px for px in img.getdata() if len(px) == 4 and px[3] > 0]
    if not pixels:
        return {"opaque_count": 0, "mean_rgb": [0, 0, 0], "distinct_colors": 0}
    total = len(pixels)
    rs = sum(p[0] for p in pixels) / total
    gs = sum(p[1] for p in pixels) / total
    bs = sum(p[2] for p in pixels) / total
    distinct = len({(p[0], p[1], p[2]) for p in pixels})
    return {
        "opaque_count": total,
        "mean_rgb": [round(rs, 2), round(gs, 2), round(bs, 2)],
        "distinct_colors": distinct,
    }


def _append_jsonl(row: Mapping[str, Any], target: Path) -> None:
    """Append one JSON row to *target* (parent dir auto-created)."""
    target.parent.mkdir(parents=True, exist_ok=True)
    with target.open("a", encoding="utf-8") as f:
        f.write(json.dumps(row, sort_keys=True) + "\n")


def _build_row(
    variant_path: str | Path,
    *,
    specs_dir: Path | None = None,
    now: float | None = None,
) -> dict:
    """Shared row constructor for `log_promote` / `log_reject`."""
    bbox, palette_stats = _measure_variant(variant_path)
    vary_values = _load_vary_values(variant_path, specs_dir=specs_dir)
    ts = time.time() if now is None else now
    return {
        "variant_path": str(variant_path),
        "vary_values": vary_values,
        "bbox": bbox,
        "palette_stats": palette_stats,
        "timestamp": ts,
    }


def log_promote(
    variant_path: str | Path,
    *,
    log_path: Path | None = None,
    specs_dir: Path | None = None,
    now: float | None = None,
) -> Path:
    """Append one row to *log_path* (defaults to :data:`PROMOTED_LOG`).

    Returns the destination path the row was appended to. Idempotent —
    repeated invocations append new rows; prior rows remain byte-identical.
    """
    target = log_path if log_path is not None else PROMOTED_LOG
    row = _build_row(variant_path, specs_dir=specs_dir, now=now)
    _append_jsonl(row, target)
    return target


def log_reject(
    variant_path: str | Path,
    reason: str,
    *,
    log_path: Path | None = None,
    specs_dir: Path | None = None,
    now: float | None = None,
) -> Path:
    """Append one `{…, reason: <tag>}` row to *log_path* (defaults to :data:`REJECTED_LOG`).

    Raises:
        InvalidRejectionReasonError: ``reason`` not in :data:`REJECTION_REASONS`.
    """
    if reason not in REJECTION_REASONS:
        valid = ", ".join(REJECTION_REASONS)
        raise InvalidRejectionReasonError(
            f"invalid --reason {reason!r}; valid: {valid}"
        )
    target = log_path if log_path is not None else REJECTED_LOG
    row = _build_row(variant_path, specs_dir=specs_dir, now=now)
    row["reason"] = reason
    _append_jsonl(row, target)
    return target
