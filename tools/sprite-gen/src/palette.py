"""K-means palette extractor + runtime palette API.

Pure functions:
- ``extract_palette``   — K-means colour extraction from reference PNGs.
- ``write_palette_json`` — write named palette to ``{dest_dir}/{cls}.json``.
- ``load_palette``      — read ``{palettes_dir}/{cls}.json`` into a dict.
- ``apply_ramp``        — map (palette, material_name, face) → (R, G, B).

Errors:
- ``PaletteKeyError``   — material name not found in loaded palette.
"""

from __future__ import annotations

import colorsys
import json
from pathlib import Path
from typing import Any

import numpy as np
from PIL import Image
import scipy.cluster.vq as vq

# ---------------------------------------------------------------------------
# Palette runtime API
# ---------------------------------------------------------------------------

#: Default directory for palette JSON files (adjacent to ``src/`` under tool root).
PALETTES_DIR: Path = Path(__file__).resolve().parent.parent / "palettes"

_FACE_TO_SLOT: dict[str, str] = {
    "top":   "bright",
    "south": "mid",
    "east":  "dark",
}


class PaletteKeyError(KeyError):
    """Material name not found in loaded palette."""


class GplParseError(ValueError):
    """Malformed or incomplete GPL palette file."""


def load_palette(cls: str, palettes_dir: Path = PALETTES_DIR) -> dict:
    """Read ``{palettes_dir}/{cls}.json`` and return the parsed dict.

    Args:
        cls:          Sprite class label (e.g. ``"residential"``).
        palettes_dir: Directory containing palette JSON files.
                      Defaults to ``PALETTES_DIR`` (tool-root ``palettes/``).

    Returns:
        Parsed palette dict with schema
        ``{"class": str, "materials": {name: {"bright": [r,g,b], "mid": [...], "dark": [...]}}}``

    Raises:
        FileNotFoundError: If ``{palettes_dir}/{cls}.json`` does not exist.
    """
    path = palettes_dir / f"{cls}.json"
    return json.loads(path.read_text(encoding="utf-8"))


def material_accents(
    palette: dict, material_name: str
) -> tuple[tuple[int, int, int] | None, tuple[int, int, int] | None]:
    """Surface optional ``accent_dark`` / ``accent_light`` RGB tuples (TECH-716).

    Returns ``(accent_dark, accent_light)``; either component is ``None``
    when the palette entry omits that key. Used by scatter primitives
    (``iso_ground_noise``) — absent accents → primitive no-op.

    Args:
        palette:       Loaded palette dict (output of ``load_palette``).
        material_name: Key into ``palette["materials"]``.

    Returns:
        ``(accent_dark_or_none, accent_light_or_none)`` tuple.

    Raises:
        PaletteKeyError: If ``material_name`` is not in ``palette["materials"]``.
    """
    materials = palette["materials"]
    if material_name not in materials:
        raise PaletteKeyError(material_name)
    entry = materials[material_name]

    def _coerce(key: str) -> tuple[int, int, int] | None:
        raw = entry.get(key)
        if raw is None:
            return None
        if not isinstance(raw, (list, tuple)) or len(raw) != 3:
            raise ValueError(
                f"palette[{material_name!r}][{key!r}]: expected 3-element RGB list, got {raw!r}"
            )
        return (int(raw[0]), int(raw[1]), int(raw[2]))

    return _coerce("accent_dark"), _coerce("accent_light")


def apply_ramp(palette: dict, material_name: str, face: str) -> tuple[int, int, int]:
    """Map a face identifier to its ramp RGB from the palette.

    Args:
        palette:       Loaded palette dict (output of ``load_palette``).
        material_name: Key into ``palette["materials"]``.
        face:          One of ``"top"``, ``"south"``, ``"east"``.

    Returns:
        ``(R, G, B)`` integer tuple in range 0–255.

    Raises:
        PaletteKeyError: If ``material_name`` is not in ``palette["materials"]``.
        KeyError:        If ``face`` is not a valid slot key (programmer error).
    """
    materials = palette["materials"]
    if material_name not in materials:
        raise PaletteKeyError(material_name)
    slot = _FACE_TO_SLOT[face]  # KeyError on bad face — programmer error
    r, g, b = materials[material_name][slot]
    return (int(r), int(g), int(b))


def _hsv_to_rgb255(h: float, s: float, v: float) -> tuple[int, int, int]:
    """Convert HSV (0–1 floats) to RGB (0–255 ints), clamping v to [0, 1]."""
    v_clamped = max(0.0, min(1.0, v))
    r, g, b = colorsys.hsv_to_rgb(h, s, v_clamped)
    return (round(r * 255), round(g * 255), round(b * 255))


def extract_palette(
    cls: str,
    source_paths: list[Path],
    n_clusters: int = 8,
    alpha_threshold: int = 32,
    seed: int = 42,
) -> dict:
    """Extract a K-means colour palette from one or more reference PNGs.

    Parameters
    ----------
    cls:
        Sprite class label (passed through for consumer convenience).
    source_paths:
        One or more PNG file paths to sample.
    n_clusters:
        Number of K-means clusters (palette entries).
    alpha_threshold:
        Pixels with alpha <= this value are ignored.
    seed:
        RNG seed for deterministic K-means.

    Returns
    -------
    dict keyed by ``cluster_idx`` (0-based, sorted bright-to-dark by HSV V):
        ``{'centroid': (R, G, B), 'bright': (R, G, B), 'mid': (R, G, B), 'dark': (R, G, B)}``

    Raises
    ------
    ValueError
        If ``source_paths`` is empty, no non-transparent pixels are found,
        or the pixel count is less than ``n_clusters``.
    """
    if not source_paths:
        raise ValueError("source_paths must not be empty")

    pixel_chunks: list[np.ndarray] = []

    for p in source_paths:
        img = Image.open(p).convert("RGBA")
        arr = np.array(img, dtype=np.uint8)  # (H, W, 4)
        mask = arr[:, :, 3] > alpha_threshold  # bool (H, W)
        rgb = arr[:, :, :3][mask]  # (N_i, 3)
        if rgb.size > 0:
            pixel_chunks.append(rgb)

    if not pixel_chunks:
        raise ValueError("No non-transparent pixels found across source_paths")

    pixels = np.vstack(pixel_chunks)  # (N_total, 3) uint8

    if len(pixels) < n_clusters:
        raise ValueError(
            f"Pixel count ({len(pixels)}) is less than n_clusters ({n_clusters})"
        )

    centroids, _ = vq.kmeans2(
        pixels.astype(float),
        n_clusters,
        minit="++",
        seed=seed,
    )

    def _luminance(rgb_float: np.ndarray) -> float:
        r, g, b = rgb_float / 255.0
        _, _, v = colorsys.rgb_to_hsv(r, g, b)
        return v

    sorted_centroids = sorted(centroids, key=_luminance, reverse=True)

    result: dict = {}
    for idx, centroid in enumerate(sorted_centroids):
        r, g, b = float(centroid[0]), float(centroid[1]), float(centroid[2])
        h, s, v = colorsys.rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
        result[idx] = {
            "centroid": (round(r), round(g), round(b)),
            "bright": _hsv_to_rgb255(h, s, v * 1.2),
            "mid": _hsv_to_rgb255(h, s, v),
            "dark": _hsv_to_rgb255(h, s, v * 0.6),
        }

    return result


def write_palette_json(
    cls: str,
    named_clusters: dict[str, dict[str, Any]],
    dest_dir: Path,
) -> Path:
    """Write a named palette to ``{dest_dir}/{cls}.json``.

    Parameters
    ----------
    cls:
        Sprite class label (e.g. ``"residential"``).
    named_clusters:
        Mapping of ``{material_name: {"bright": (R,G,B), "mid": (R,G,B), "dark": (R,G,B), ...}}``.
        ``centroid`` key is dropped if present — only ``bright/mid/dark`` persist.
    dest_dir:
        Output directory.  Created (including parents) if missing.

    Returns
    -------
    Path
        Absolute path of the written JSON file.
    """
    dest_dir.mkdir(parents=True, exist_ok=True)
    out_path = dest_dir / f"{cls}.json"

    materials: dict[str, dict[str, list[int]]] = {}
    for name, cluster in named_clusters.items():
        entry: dict[str, list[int]] = {}
        for key in ("bright", "mid", "dark"):
            val = cluster[key]
            entry[key] = list(val)
        materials[name] = entry

    payload = {"class": cls, "materials": materials}
    out_path.write_text(json.dumps(payload, indent=2) + "\n", encoding="utf-8")
    return out_path


def export_gpl(
    cls: str,
    dest_path: "Path | None" = None,
    palettes_dir: Path = PALETTES_DIR,
) -> str:
    """Serialize palette JSON to GIMP .gpl format string.

    Args:
        cls:          Sprite class label (e.g. ``"residential"``).
        dest_path:    When provided, the text is written to this path.
        palettes_dir: Directory containing palette JSON files.

    Returns:
        GPL text (``GIMP Palette`` header + one row per material × level).

    Raises:
        FileNotFoundError: If ``{palettes_dir}/{cls}.json`` does not exist.
    """
    palette = load_palette(cls, palettes_dir=palettes_dir)
    lines = ["GIMP Palette", f"Name: {cls}", "Columns: 3", "#"]

    def _emit_levels(prefix: str, levels: dict) -> None:
        """Emit GPL rows for a flat bright/mid/dark ramp, or recurse nested ramps.

        Decoration ramps (TECH-762+) introduced partial ramps (`bush` lacks
        `dark`) and nested ramps (`tree_deciduous.green.*`). Skip missing
        levels rather than crashing; recurse one level into dict-of-dicts.
        """
        flat_keys = ("bright", "mid", "dark")
        # Nested ramp: any value that is itself a dict (not a 3-tuple list)
        has_nested = any(isinstance(v, dict) for v in levels.values())
        if has_nested:
            for sub_name, sub_levels in levels.items():
                if isinstance(sub_levels, dict):
                    _emit_levels(f"{prefix}_{sub_name}", sub_levels)
            return
        for level in flat_keys:
            triple = levels.get(level)
            if triple is None or not isinstance(triple, (list, tuple)) or len(triple) != 3:
                continue
            r, g, b = triple
            lines.append(f"{int(r):3d} {int(g):3d} {int(b):3d}\t{prefix}_{level}")

    for mat_name, levels in palette["materials"].items():
        _emit_levels(mat_name, levels)
    text = "\n".join(lines) + "\n"
    if dest_path is not None:
        dest_path.write_text(text, encoding="utf-8")
    return text


def import_gpl(cls: str, gpl_path: Path) -> dict:
    """Parse a GIMP .gpl file back into palette JSON schema.

    Args:
        cls:      Sprite class label written into the returned dict.
        gpl_path: Path to the ``.gpl`` file.

    Returns:
        ``{"class": cls, "materials": {name: {"bright": [r,g,b], "mid": [...], "dark": [...]}}}``

    Raises:
        FileNotFoundError:  If ``gpl_path`` does not exist.
        GplParseError:      On malformed rows (non-int RGB, bad level suffix,
                            duplicate or missing level triplets).
    """
    raw = gpl_path.read_text(encoding="utf-8")
    lines = raw.splitlines()

    # Skip header lines until the '#' sentinel (inclusive).
    body_lines: list[tuple[int, str]] = []  # (1-based line number, text)
    past_sentinel = False
    for lineno, line in enumerate(lines, 1):
        if not past_sentinel:
            if line.strip() == "#":
                past_sentinel = True
            continue
        body_lines.append((lineno, line))

    _VALID_LEVELS = {"bright", "mid", "dark"}
    materials: dict[str, dict[str, list[int]]] = {}

    for lineno, line in body_lines:
        stripped = line.strip()
        # Skip blank lines and comment lines after the sentinel.
        if not stripped or stripped.startswith("#"):
            continue

        # Split on first tab to separate RGB triple from name.
        parts = stripped.split(None, 3)  # [R, G, B, name]
        if len(parts) < 4:
            raise GplParseError(
                f"line {lineno}: expected 'R G B<TAB>name_level', got {line!r}"
            )
        r_str, g_str, b_str, name_level = parts[0], parts[1], parts[2], parts[3]

        # Parse RGB channels.
        try:
            r, g, b = int(r_str), int(g_str), int(b_str)
        except ValueError:
            raise GplParseError(
                f"line {lineno}: non-integer RGB values: {r_str!r} {g_str!r} {b_str!r}"
            )
        for ch_name, ch_val in (("R", r), ("G", g), ("B", b)):
            if not (0 <= ch_val <= 255):
                raise GplParseError(
                    f"line {lineno}: {ch_name} value {ch_val} out of range 0-255"
                )

        # Split off level suffix via rsplit to handle material names with '_'.
        if "_" not in name_level:
            raise GplParseError(
                f"line {lineno}: name {name_level!r} has no '_' separator (expected name_level)"
            )
        mat_name, level = name_level.rsplit("_", 1)
        if level not in _VALID_LEVELS:
            raise GplParseError(
                f"line {lineno}: level {level!r} not in {{bright, mid, dark}} for {name_level!r}"
            )

        if mat_name not in materials:
            materials[mat_name] = {}
        if level in materials[mat_name]:
            raise GplParseError(
                f"line {lineno}: duplicate level {level!r} for material {mat_name!r}"
            )
        materials[mat_name][level] = [r, g, b]

    # TECH-762+ decoration ramps ship partial (bush/grass_tuft/pool) or nested
    # (tree_deciduous) — `import_gpl` accepts any subset of {bright, mid, dark};
    # callers needing a full ramp assert on their own contract.
    if not materials:
        raise GplParseError("no material rows found in GPL body")

    return {"class": cls, "materials": materials}
