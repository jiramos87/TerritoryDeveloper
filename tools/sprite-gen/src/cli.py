"""cli.py — CLI dispatcher for sprite-gen.

Entry: `python -m sprite_gen render {archetype}`
       `python -m sprite_gen render --all`

Exit codes:
    0 — success
    1 — spec missing / invalid YAML / schema validation error / promote source missing
    2 — argparse user-error (bad flag value, etc.)
    4 — Aseprite binary not found (promote --edit)
    5 — registry HTTP / transport / unrecoverable catalog error
"""

from __future__ import annotations

import argparse
import copy
import glob as _glob
import random
import sys
from pathlib import Path
from typing import Optional

import yaml

from .spec import SpecValidationError, load_spec
from .compose import compose_sprite
from .signature import compute_signature
from .palette import (
    GplParseError,
    PaletteKeyError,
    extract_palette,
    export_gpl,
    import_gpl,
    load_palette,
    write_palette_json,
)

# ---------------------------------------------------------------------------
# Tool-root paths (cwd-independent)
# ---------------------------------------------------------------------------

_SRC_DIR = Path(__file__).resolve().parent
_TOOL_ROOT = _SRC_DIR.parent
_SPECS_DIR = _TOOL_ROOT / "specs"
_OUT_DIR = _TOOL_ROOT / "out"
_PALETTES_DIR = _TOOL_ROOT / "palettes"
_SIGNATURES_DIR = _TOOL_ROOT / "signatures"
_REPO_ROOT = _TOOL_ROOT.parent.parent

# TECH-705 — class name -> list of glob patterns resolving to reference PNGs
# under Assets/Sprites/. Keep patterns narrow enough to exclude slope /
# unrelated size variants. Globs are joined into a single {a,b}-style call
# via shell-agnostic iteration in :func:`_cmd_refresh_signatures`.
_CLASS_SOURCE_GLOBS: dict[str, list[str]] = {
    "residential_small": [
        str(_REPO_ROOT / "Assets/Sprites/Residential/House1.png"),
        str(_REPO_ROOT / "Assets/Sprites/Residential/House1-64.png"),
    ],
}

# ---------------------------------------------------------------------------
# Slope variant enum — matches Slope variant naming glossary +
# Assets/Sprites/Slopes/ filename stems.  17 land variants + "flat" sentinel.
# ---------------------------------------------------------------------------

_VALID_SLOPE_IDS: frozenset[str] = frozenset(
    {
        "flat",
        "N", "S", "E", "W",
        "NE", "NW", "SE", "SW",
        "NE-up", "NW-up", "SE-up", "SW-up",
        "NE-bay", "NW-bay", "NW-bay-2", "SE-bay", "SW-bay",
    }
)

# ---------------------------------------------------------------------------
# Temporary material-family swap map (replaced by Stage 1.3 palette metadata)
# ---------------------------------------------------------------------------

# Each group contains materials within the same visual family that may be
# swapped with one another.  The family map is keyed by any member string;
# the value is the full list of alternatives in that family.
_MATERIAL_FAMILIES: dict[str, list[str]] = {
    "wall_brick_red": ["wall_brick_red", "wall_brick_grey"],
    "wall_brick_grey": ["wall_brick_red", "wall_brick_grey"],
    "roof_tile_brown": ["roof_tile_brown", "roof_tile_grey"],
    "roof_tile_grey": ["roof_tile_brown", "roof_tile_grey"],
}


# ---------------------------------------------------------------------------
# Variant permutation helper
# ---------------------------------------------------------------------------


def apply_variant(spec: dict, variant_idx: int) -> dict:
    """Return a deep copy of *spec* with seed-based per-variant permutations.

    Permutations applied:
    - Material swap: each composition entry whose ``material`` is a known
      family member is randomly replaced by another member of the same family.
    - Prism pitch scale: for ``type: iso_prism`` entries that carry ``pitch``,
      ``new_pitch = clamp(pitch * rng.uniform(0.8, 1.2), 0.0, 1.0)``.

    Unknown material keys are left unchanged (compose_sprite falls back to
    neutral stone as documented in compose.py).

    Args:
        spec: Already-validated archetype spec dict.
        variant_idx: Zero-based variant index; drives ``random.Random`` seed.

    Returns:
        Mutated deep copy — original spec is never modified.
    """
    base_seed = spec.get("seed", 0)
    rng = random.Random(base_seed + variant_idx)

    mutated = copy.deepcopy(spec)
    for entry in mutated.get("composition", []):
        material = entry.get("material")
        if material and material in _MATERIAL_FAMILIES:
            entry["material"] = rng.choice(_MATERIAL_FAMILIES[material])

        if entry.get("type") == "iso_prism" and "pitch" in entry:
            raw = entry["pitch"] * rng.uniform(0.8, 1.2)
            entry["pitch"] = max(0.0, min(1.0, raw))

    return mutated


# ---------------------------------------------------------------------------
# Palette helpers
# ---------------------------------------------------------------------------


def _prompt_names(clusters: dict) -> list[str]:
    """Interactive TTY loop: print ANSI swatch per cluster, read a name from stdin.

    Args:
        clusters: Output of ``extract_palette`` (index → entry dict).

    Returns:
        List of names in cluster-index order.
    """
    names: list[str] = []
    for idx in sorted(clusters.keys()):
        entry = clusters[idx]
        r, g, b = entry["mid"]
        swatch = f"\x1b[48;2;{r};{g};{b}m      \x1b[0m"
        prompt = f"{swatch} cluster {idx}: RGB({r},{g},{b}) — name? "
        sys.stdout.write(prompt)
        sys.stdout.flush()
        name = input().strip()
        names.append(name)
    return names


def _csv_names(names_flag: str, clusters: dict) -> list[str]:
    """Parse and validate ``--names`` CSV against cluster count.

    Args:
        names_flag: Raw comma-separated string from ``--names``.
        clusters: Output of ``extract_palette``.

    Returns:
        Stripped list of names.

    Raises:
        SystemExit(1): When count doesn't match cluster count.
    """
    names = [n.strip() for n in names_flag.split(",")]
    if len(names) != len(clusters):
        print(
            f"error: --names has {len(names)} entries but {len(clusters)} clusters were found; counts must match.",
            file=sys.stderr,
        )
        sys.exit(1)
    return names


def _cmd_palette(args: argparse.Namespace) -> int:
    """Dispatch palette extract subcommand.

    Args:
        args: Parsed namespace from palette extract parser.

    Returns:
        0 on success, 1 on error.
    """
    # Expand glob — use stdlib glob.glob so absolute patterns work on Python 3.13+.
    pattern: str = args.sources
    source_paths = sorted(Path(p) for p in _glob.glob(pattern, recursive=True))
    if not source_paths:
        print(f"error: no sources matched {pattern!r}", file=sys.stderr)
        return 1

    # Determine cluster count from --names or default 8.
    n_clusters: int = len([n for n in args.names.split(",")]) if args.names else 8

    try:
        clusters = extract_palette(args.cls, source_paths, n_clusters=n_clusters)
    except ValueError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1

    # Obtain names — CSV path or interactive TTY.
    if args.names:
        names = _csv_names(args.names, clusters)
    else:
        is_tty = sys.stdin.isatty() and sys.stdout.isatty()
        if not is_tty:
            print(
                "error: non-interactive shell requires --names",
                file=sys.stderr,
            )
            return 1
        names = _prompt_names(clusters)

    # Zip names → clusters (drop centroid).
    named_clusters: dict[str, dict] = {}
    for name, idx in zip(names, sorted(clusters.keys())):
        named_clusters[name] = clusters[idx]

    out_dir = Path(args.out) if args.out else _PALETTES_DIR
    out_path = write_palette_json(args.cls, named_clusters, out_dir)

    try:
        relpath = out_path.relative_to(Path.cwd())
    except ValueError:
        relpath = out_path
    print(f"wrote {relpath}")
    return 0


# ---------------------------------------------------------------------------
# Palette export / import subcommands
# ---------------------------------------------------------------------------


def _cmd_palette_export(args: argparse.Namespace) -> int:
    """Dispatch `palette export {class}` — write palettes/{class}.gpl.

    Args:
        args: Parsed namespace; expects ``args.cls``.

    Returns:
        0 on success, 1 on error.
    """
    dest = _PALETTES_DIR / f"{args.cls}.gpl"
    try:
        export_gpl(args.cls, dest_path=dest)
    except FileNotFoundError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1

    try:
        relpath = dest.relative_to(Path.cwd())
    except ValueError:
        relpath = dest
    print(f"wrote {relpath}")
    return 0


def _cmd_palette_import(args: argparse.Namespace) -> int:
    """Dispatch `palette import {class} --gpl {path}` — parse .gpl, overwrite JSON.

    Args:
        args: Parsed namespace; expects ``args.cls`` and ``args.gpl``.

    Returns:
        0 on success, 1 on error.
    """
    gpl_path = Path(args.gpl)
    if not gpl_path.exists():
        print(f"error: file not found: {gpl_path}", file=sys.stderr)
        return 1

    try:
        new_palette = import_gpl(args.cls, gpl_path)
    except GplParseError as exc:
        print(f"error: {exc}", file=sys.stderr)
        return 1

    # Load prior JSON for diff (tolerate missing).
    json_path = _PALETTES_DIR / f"{args.cls}.json"
    prior_materials: dict = {}
    if json_path.exists():
        try:
            prior_materials = load_palette(args.cls)["materials"]
        except Exception:
            pass  # Treat unreadable prior as empty — overwrite unconditionally.

    new_materials = new_palette["materials"]

    # Emit diff lines or "no change".
    changed = False
    for mat_name, levels in new_materials.items():
        for level in ("bright", "mid", "dark"):
            new_rgb = levels[level]
            old_rgb = (prior_materials.get(mat_name) or {}).get(level)
            if old_rgb != new_rgb:
                changed = True
                old_str = f"RGB({old_rgb[0]},{old_rgb[1]},{old_rgb[2]})" if old_rgb else "—"
                new_str = f"RGB({new_rgb[0]},{new_rgb[1]},{new_rgb[2]})"
                print(f"+{mat_name}.{level} {old_str} → {new_str}")
    if not changed:
        print("no change")

    # Write updated JSON.
    import json as _json
    _PALETTES_DIR.mkdir(parents=True, exist_ok=True)
    json_path.write_text(_json.dumps(new_palette, indent=2) + "\n", encoding="utf-8")

    try:
        relpath = json_path.relative_to(Path.cwd())
    except ValueError:
        relpath = json_path
    print(f"wrote {relpath}")
    return 0


# ---------------------------------------------------------------------------
# Core render helper (single archetype)
# ---------------------------------------------------------------------------


def _render_one(
    archetype: str,
    terrain_override: Optional[str] = None,
    layered: bool = False,
) -> int:
    """Load spec for *archetype*, optionally override terrain, render PNG variants.

    Args:
        archetype: Archetype id (filename stem under _SPECS_DIR).
        terrain_override: When not None, replaces ``spec['terrain']`` before
            compose.  Must be a value from ``_VALID_SLOPE_IDS``.

    Returns:
        0 on success, 1 on any error (spec missing / bad YAML / validation /
        NotImplementedError for non-flat terrain until Stage 1.4).
    """
    spec_path = _SPECS_DIR / f"{archetype}.yaml"

    try:
        spec = load_spec(spec_path)
    except FileNotFoundError:
        print(f"error: spec not found: {spec_path}", file=sys.stderr)
        return 1
    except yaml.YAMLError as exc:
        print(f"error: malformed YAML in {spec_path}: {exc}", file=sys.stderr)
        return 1
    except SpecValidationError as exc:
        print(f"error: spec validation failed: {exc}", file=sys.stderr)
        return 1

    # Apply terrain override pre-compose.
    if terrain_override is not None:
        spec["terrain"] = terrain_override

    n_variants: int = spec["output"].get("variants", 1)
    out_name: str = spec["output"]["name"]
    _OUT_DIR.mkdir(parents=True, exist_ok=True)

    for idx in range(n_variants):
        variant_spec = apply_variant(spec, idx)
        try:
            img = compose_sprite(variant_spec)
        except PaletteKeyError as exc:
            print(
                f"error: material {exc.args[0]!r} missing in palette {variant_spec.get('palette')!r}",
                file=sys.stderr,
            )
            return 2
        out_file = _OUT_DIR / f"{out_name}_v{idx + 1:02d}.png"
        img.save(out_file)
        # Print relative path when possible, fall back to absolute.
        try:
            relpath = out_file.relative_to(Path.cwd())
        except ValueError:
            relpath = out_file
        print(f"wrote {relpath}")

        if layered:
            from .aseprite_io import write_layered_aseprite
            from .compose import compose_layers

            layers, size = compose_layers(variant_spec)
            ase_file = _OUT_DIR / f"{out_name}_v{idx + 1:02d}.aseprite"
            write_layered_aseprite(ase_file, layers, size)
            try:
                arel = ase_file.relative_to(Path.cwd())
            except ValueError:
                arel = ase_file
            print(f"wrote {arel}")

    return 0


# ---------------------------------------------------------------------------
# Subcommand: render
# ---------------------------------------------------------------------------


def _cmd_refresh_signatures(args: argparse.Namespace) -> int:
    """Dispatch `refresh-signatures [class?]` — regenerate signature JSON.

    With no class: iterate every class in `_CLASS_SOURCE_GLOBS`.
    With a class: regenerate only that one.

    Returns:
        0 on success, 1 on unknown class / I/O error.
    """
    import json as _json

    fallback_path = _SIGNATURES_DIR / "_fallback.json"
    targets: list[str]
    if args.cls:
        if args.cls not in _CLASS_SOURCE_GLOBS:
            print(
                f"error: unknown class {args.cls!r}; known: {sorted(_CLASS_SOURCE_GLOBS)}",
                file=sys.stderr,
            )
            return 1
        targets = [args.cls]
    else:
        targets = sorted(_CLASS_SOURCE_GLOBS)

    _SIGNATURES_DIR.mkdir(parents=True, exist_ok=True)

    for cls in targets:
        globs = _CLASS_SOURCE_GLOBS[cls]
        sig = compute_signature(
            cls,
            globs,
            fallback_graph_path=fallback_path if fallback_path.is_file() else None,
            spec_loader=load_spec,
        )
        out_path = _SIGNATURES_DIR / f"{cls}.signature.json"
        out_path.write_text(_json.dumps(sig, indent=2) + "\n", encoding="utf-8")
        try:
            rel = out_path.relative_to(Path.cwd())
        except ValueError:
            rel = out_path
        print(f"wrote {rel} (mode={sig['mode']}, source_count={sig['source_count']})")

    return 0


def _cmd_render(args: argparse.Namespace) -> int:
    """Dispatch render subcommand: single-archetype or --all batch."""
    terrain_override: Optional[str] = getattr(args, "terrain", None)
    layered: bool = bool(getattr(args, "layered", False))

    if getattr(args, "all", False):
        # Batch mode: glob sorted specs, iterate, aggregate exit.
        failed: list[str] = []
        for spec_file in sorted(_SPECS_DIR.glob("*.yaml")):
            stem = spec_file.stem
            rc = _render_one(stem, terrain_override, layered=layered)
            if rc != 0:
                failed.append(stem)
        if failed:
            print(f"failed: {failed}", file=sys.stderr)
            return 1
        return 0

    # Single-archetype mode.
    return _render_one(args.archetype, terrain_override, layered=layered)


# ---------------------------------------------------------------------------
# Main entry point
# ---------------------------------------------------------------------------


def main(argv: Optional[list[str]] = None) -> int:
    """Parse *argv* and dispatch to the appropriate subcommand.

    Args:
        argv: Argument list (excluding program name).  Defaults to
            ``sys.argv[1:]`` when *None*.

    Returns:
        Integer exit code (0 = success, 1 = user error, 2 = argparse error).
    """
    parser = argparse.ArgumentParser(
        prog="sprite_gen",
        description="Sprite-gen — isometric sprite compositor.",
    )
    subparsers = parser.add_subparsers(dest="command", metavar="COMMAND")

    # -- render ---------------------------------------------------------------
    render_parser = subparsers.add_parser(
        "render",
        help="Render an archetype YAML to PNG variants.",
    )

    # Mutually-exclusive: positional archetype xor --all.
    target_group = render_parser.add_mutually_exclusive_group(required=True)
    target_group.add_argument(
        "archetype",
        nargs="?",
        help="Archetype id (resolves tools/sprite-gen/specs/{archetype}.yaml).",
    )
    target_group.add_argument(
        "--all",
        action="store_true",
        default=False,
        help="Render every spec in tools/sprite-gen/specs/.",
    )

    render_parser.add_argument(
        "--terrain",
        choices=sorted(_VALID_SLOPE_IDS),
        default=None,
        metavar="SLOPE_ID",
        help=(
            "Override spec terrain field before compose. "
            f"Valid ids: {sorted(_VALID_SLOPE_IDS)}. "
            "Non-flat variants raise NotImplementedError until Stage 1.4."
        ),
    )
    render_parser.add_argument(
        "--layered",
        action="store_true",
        default=False,
        help="Co-emit layered .aseprite alongside flat PNG (TECH-182).",
    )

    # -- palette --------------------------------------------------------------
    palette_parser = subparsers.add_parser(
        "palette",
        help="Palette extraction and management.",
    )
    palette_sub = palette_parser.add_subparsers(dest="palette_command", metavar="PALETTE_CMD")

    extract_parser = palette_sub.add_parser(
        "extract",
        help="Extract K-means palette from reference PNG(s) and write JSON.",
    )
    extract_parser.add_argument(
        "cls",
        metavar="CLASS",
        help="Sprite class label (e.g. residential, commercial).",
    )
    extract_parser.add_argument(
        "--sources",
        required=True,
        metavar="GLOB",
        help="Glob pattern for source PNG files (resolved from cwd).",
    )
    extract_parser.add_argument(
        "--names",
        default=None,
        metavar="CSV",
        help=(
            "Comma-separated material names (bypasses interactive prompt). "
            "Count must match --n-clusters (default 8) or len of extracted clusters."
        ),
    )
    extract_parser.add_argument(
        "--out",
        default=None,
        metavar="DIR",
        help=f"Output directory for palette JSON (default: {_PALETTES_DIR}).",
    )

    # palette export
    export_parser = palette_sub.add_parser(
        "export",
        help="Export palette JSON to GIMP .gpl format.",
    )
    export_parser.add_argument(
        "cls",
        metavar="CLASS",
        help="Sprite class label (e.g. residential).",
    )

    # palette import
    import_parser = palette_sub.add_parser(
        "import",
        help="Import a .gpl file back into palette JSON.",
    )
    import_parser.add_argument(
        "cls",
        metavar="CLASS",
        help="Sprite class label (e.g. residential).",
    )
    import_parser.add_argument(
        "--gpl",
        required=True,
        metavar="PATH",
        help="Path to the .gpl file to import.",
    )

    promote_parser = subparsers.add_parser(
        "promote",
        help="Promote a rendered PNG or .aseprite to Assets/Sprites/Generated/.",
    )
    promote_parser.add_argument("src", metavar="SRC", help="Source PNG or .aseprite (with --edit).")
    promote_parser.add_argument("--as", dest="dest_name", required=True, metavar="NAME", help="Destination slug.")
    promote_parser.add_argument(
        "--edit",
        action="store_true",
        default=False,
        help="Flatten .aseprite via Aseprite CLI before promote.",
    )
    promote_parser.add_argument(
        "--no-push",
        action="store_true",
        default=False,
        help="Skip catalog HTTP push after promote.",
    )

    # -- refresh-signatures ---------------------------------------------------
    refresh_parser = subparsers.add_parser(
        "refresh-signatures",
        help="Regenerate tools/sprite-gen/signatures/<class>.signature.json (TECH-705).",
    )
    refresh_parser.add_argument(
        "cls",
        nargs="?",
        default=None,
        metavar="CLASS",
        help=(
            "Optional class name; omit to refresh every known class. "
            f"Known classes: {sorted(_CLASS_SOURCE_GLOBS)}"
        ),
    )

    reject_parser = subparsers.add_parser(
        "reject",
        help="Delete out/{archetype}_v*.png variants.",
    )
    reject_parser.add_argument("archetype", metavar="ARCHETYPE", help="Archetype slug (output name stem).")
    reject_parser.add_argument("--yes", action="store_true", default=False, help="Skip interactive confirmation.")

    parsed = parser.parse_args(argv)

    if parsed.command == "render":
        return _cmd_render(parsed)

    if parsed.command == "refresh-signatures":
        return _cmd_refresh_signatures(parsed)

    if parsed.command == "promote":
        from pathlib import Path as _P

        from .aseprite_bin import AsepriteBinNotFoundError
        from .curate import PromoteEditFlagError, promote
        from .registry_client import (
            CatalogConfigError,
            RegistryClientError,
            RegistryConnectionError,
            ValidationError,
        )

        try:
            promote(
                _P(parsed.src),
                parsed.dest_name,
                edit=parsed.edit,
                push=not parsed.no_push,
            )
        except FileNotFoundError as exc:
            print(f"error: {exc}", file=sys.stderr)
            return 1
        except PromoteEditFlagError as exc:
            print(f"error: {exc}", file=sys.stderr)
            return 1
        except AsepriteBinNotFoundError as exc:
            print(f"error: {exc}", file=sys.stderr)
            return 4
        except CatalogConfigError as exc:
            print(f"error: {exc}", file=sys.stderr)
            return 5
        except (RegistryConnectionError, ValidationError, RegistryClientError) as exc:
            print(f"error: {exc}", file=sys.stderr)
            return 5
        return 0

    if parsed.command == "reject":
        from .curate import reject

        reject(parsed.archetype, _OUT_DIR, confirm=not parsed.yes)
        return 0

    if parsed.command == "palette":
        palette_cmd = getattr(parsed, "palette_command", None)
        if palette_cmd == "extract":
            return _cmd_palette(parsed)
        if palette_cmd == "export":
            return _cmd_palette_export(parsed)
        if palette_cmd == "import":
            return _cmd_palette_import(parsed)
        palette_parser.print_help()
        return 0

    parser.print_help()
    return 0
