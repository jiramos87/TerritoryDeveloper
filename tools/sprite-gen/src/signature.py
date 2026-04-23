"""signature.py — Art Signatures per class (Stage 6.2, TECH-704).

Canonical calibration signatures summarizing bbox / palette / silhouette /
ground / decoration hints from reference sprites under a class folder.

L15 sample-size policy (verbatim from sprite-gen improvement session §3):

    source_count == 0  -> mode: fallback   (copy from _fallback.json target)
    source_count == 1  -> mode: point-match (single-sprite values; min=max=mean)
    source_count >= 2  -> mode: envelope   (min/max/mean summarization)

L3 staleness guard: :func:`validate_against` recomputes `source_checksum`
against the live folder; mismatch raises :class:`SignatureStaleError` with
the refresh command in the message.

Public surface::

    compute_signature(class_name, folder_glob, *, fallback_graph_path=None,
                      spec_loader=None) -> dict
    validate_against(signature, rendered_img, *, live_sources=None) -> ValidationReport
    SignatureStaleError
    ValidationReport
"""

from __future__ import annotations

import datetime as _dt
import hashlib
import json
from collections import Counter
from colorsys import rgb_to_hsv
from dataclasses import dataclass, field
from pathlib import Path
from statistics import stdev
from typing import Callable, Iterable, Optional

from PIL import Image


# ---------------------------------------------------------------------------
# Public types
# ---------------------------------------------------------------------------


class SignatureStaleError(Exception):
    """Raised when source_checksum drifts from the recomputed value (L3)."""


@dataclass
class ValidationReport:
    """Outcome of :func:`validate_against`.

    Attributes:
        ok: True when every envelope/point-match constraint holds.
        failures: Human-readable reason strings, one per violated constraint.
    """

    ok: bool
    failures: list[str] = field(default_factory=list)


# ---------------------------------------------------------------------------
# Checksum helper
# ---------------------------------------------------------------------------


def _compute_checksum(paths: Iterable[Path]) -> str:
    """Return deterministic sha256 of the concatenated source bytes.

    Ordered by path string so iteration order of the glob does not matter.
    """
    h = hashlib.sha256()
    for p in sorted(Path(x) for x in paths):
        h.update(p.name.encode("utf-8"))
        h.update(b"\0")
        h.update(p.read_bytes())
    return f"sha256:{h.hexdigest()}"


# ---------------------------------------------------------------------------
# Per-sprite measurement extractors
# ---------------------------------------------------------------------------


def _load_rgba(path: Path) -> Image.Image:
    return Image.open(path).convert("RGBA")


def _measure_bbox(img: Image.Image) -> dict:
    """Alpha-bbox measurement.

    Returns dict with `height`, `y0`, `spans_full_width` (bool).
    """
    box = img.getbbox()
    if box is None:
        return {"height": 0, "y0": 0, "spans_full_width": False}
    x0, y0, x1, y1 = box
    return {
        "height": int(y1 - y0),
        "y0": int(y0),
        "spans_full_width": bool(x0 == 0 and x1 == img.width),
    }


def _measure_palette(img: Image.Image) -> dict:
    """Dominant-colour histogram in the top 20% band + full alpha.

    `top_20pct_band`: top-N dominant RGB tuples across the top 20% of content.
    `wall_dominant` / `roof_dominant` are coarse band proxies (bottom 60% /
    top 40% of content respectively) — kept simple until segmentation lands.
    """
    w, h = img.size
    box = img.getbbox() or (0, 0, w, h)
    _x0, y0, _x1, y1 = box
    content_h = max(1, y1 - y0)

    def _top_rgb(band_y0: int, band_y1: int, n: int = 5) -> list[list[int]]:
        pixels: list[tuple[int, int, int]] = []
        for y in range(max(0, band_y0), min(h, band_y1)):
            for x in range(w):
                p = img.getpixel((x, y))
                if p[3] > 0:
                    pixels.append(p[:3])
        if not pixels:
            return []
        return [list(rgb) for rgb, _ in Counter(pixels).most_common(n)]

    top_band_end = y0 + int(0.2 * content_h)
    roof_band_end = y0 + int(0.4 * content_h)
    return {
        "top_20pct_band": _top_rgb(y0, max(y0 + 1, top_band_end), n=5),
        "roof_dominant": _top_rgb(y0, max(y0 + 1, roof_band_end), n=3),
        "wall_dominant": _top_rgb(roof_band_end, y1, n=3),
    }


def _measure_silhouette(img: Image.Image) -> dict:
    """Silhouette features relative to the diamond top (y=16 for 64-wide tile).

    `peaks_above_diamond_top.freq` = fraction of opaque top-row pixels above
    y=16 (1.0 when the silhouette crosses the diamond top; 0 otherwise).
    `has_pitched_roof.freq` = fraction of content columns whose topmost
    opaque pixel is narrower than the full content width (pitched-roof proxy).
    """
    w, h = img.size
    diamond_top = 16  # DAS §2.3 invariant for 64-wide iso tile

    box = img.getbbox()
    if box is None:
        return {
            "peaks_above_diamond_top": {"freq": 0.0, "px_above_mean": 0.0},
            "has_pitched_roof": {"freq": 0.0},
        }
    x0, y0, _x1, _y1 = box

    above_px: list[int] = []
    top_ys: list[int] = []
    for x in range(w):
        for y in range(h):
            p = img.getpixel((x, y))
            if p[3] > 0:
                top_ys.append(y)
                if y < diamond_top:
                    above_px.append(diamond_top - y)
                break

    peaks_freq = (len(above_px) / max(1, len(top_ys))) if top_ys else 0.0
    px_above_mean = (sum(above_px) / len(above_px)) if above_px else 0.0

    # Pitched roof proxy — columns that start strictly below y0 of the bbox
    # suggest a sloped silhouette edge.
    pitched = sum(1 for yt in top_ys if yt > y0)
    pitched_freq = (pitched / len(top_ys)) if top_ys else 0.0

    return {
        "peaks_above_diamond_top": {
            "freq": round(peaks_freq, 4),
            "px_above_mean": round(px_above_mean, 4),
        },
        "has_pitched_roof": {"freq": round(pitched_freq, 4)},
    }


def _measure_ground(img: Image.Image) -> dict:
    """Ground band measurement (bottom ~20% of content bbox) — TECH-719.

    Returns:
        ``dominant``: single modal RGB list ``[r, g, b]``, or ``None`` on empty band.
        ``variance.hue_stddev``: population HSV hue stddev in degrees, or ``None``.
        ``variance.value_stddev``: population HSV value stddev in percent, or ``None``.

    L15 fallback contract: zero-pixel band → all fields ``None``.
    """
    w, h = img.size
    box = img.getbbox() or (0, 0, w, h)
    _x0, y0, _x1, y1 = box
    content_h = max(1, y1 - y0)
    band_y0 = max(y0, y1 - int(0.2 * content_h))

    pixels: list[tuple[int, int, int]] = []
    for y in range(band_y0, min(h, y1)):
        for x in range(w):
            p = img.getpixel((x, y))
            if p[3] > 0:
                pixels.append(p[:3])

    if not pixels:
        return {"dominant": None, "variance": {"hue_stddev": None, "value_stddev": None}}

    # Modal RGB as dominant.
    dominant_rgb, _ = Counter(pixels).most_common(1)[0]
    dominant = list(dominant_rgb)

    # HSV stddev in natural units: hue degrees (0–360), value percent (0–100).
    hsvs = [rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0) for (r, g, b) in pixels]
    hues_deg = [h * 360.0 for h, _s, _v in hsvs]
    vals_pct = [v * 100.0 for _h, _s, v in hsvs]

    h_stddev = round(stdev(hues_deg), 4) if len(hues_deg) > 1 else 0.0
    v_stddev = round(stdev(vals_pct), 4) if len(vals_pct) > 1 else 0.0

    return {
        "dominant": dominant,
        "variance": {
            "hue_stddev": h_stddev,
            "value_stddev": v_stddev,
        },
    }


def _measure_decoration_hints(img: Image.Image) -> dict:
    """Coarse decoration hints.

    TECH-704 does not ship segmentation; emit nulls for fields that need it
    (per Open Q #2) and a `grass_ratio_mean` proxy from ground-band green
    dominance.
    """
    w, h = img.size
    box = img.getbbox() or (0, 0, w, h)
    _x0, y0, _x1, y1 = box
    content_h = max(1, y1 - y0)
    band_y0 = max(y0, y1 - int(0.2 * content_h))

    total = 0
    green = 0
    for y in range(band_y0, min(h, y1)):
        for x in range(w):
            p = img.getpixel((x, y))
            if p[3] > 0:
                total += 1
                r, g, b = p[:3]
                if g > r and g > b:
                    green += 1
    grass_ratio = (green / total) if total else 0.0

    return {
        "trees_per_tile_mean": None,
        "grass_ratio_mean": round(grass_ratio, 4),
    }


def _measure(path: Path) -> dict:
    """Measure one sprite file; returns dict matching the JSON shape."""
    img = _load_rgba(path)
    return {
        "bbox": _measure_bbox(img),
        "palette": _measure_palette(img),
        "silhouette": _measure_silhouette(img),
        "ground": _measure_ground(img),
        "decoration_hints": _measure_decoration_hints(img),
    }


# ---------------------------------------------------------------------------
# Summarization (L15 branches)
# ---------------------------------------------------------------------------


def _envelope_scalar(values: list[float]) -> dict:
    return {
        "min": min(values),
        "max": max(values),
        "mean": round(sum(values) / len(values), 4),
    }


def _summarize_bbox(measurements: list[dict], *, point_match: bool) -> dict:
    heights = [m["bbox"]["height"] for m in measurements]
    y0s = [m["bbox"]["y0"] for m in measurements]
    spans = all(m["bbox"]["spans_full_width"] for m in measurements)
    if point_match:
        h0 = heights[0]
        y = y0s[0]
        return {
            "height": {"min": h0, "max": h0, "mean": h0},
            "y0": {"min": y, "max": y, "mean": y},
            "spans_full_width": spans,
        }
    return {
        "height": _envelope_scalar([float(v) for v in heights]),
        "y0": _envelope_scalar([float(v) for v in y0s]),
        "spans_full_width": spans,
    }


def _summarize_silhouette(measurements: list[dict]) -> dict:
    peaks_freq = [m["silhouette"]["peaks_above_diamond_top"]["freq"] for m in measurements]
    peaks_px = [m["silhouette"]["peaks_above_diamond_top"]["px_above_mean"] for m in measurements]
    pitched = [m["silhouette"]["has_pitched_roof"]["freq"] for m in measurements]
    return {
        "peaks_above_diamond_top": {
            "freq": round(sum(peaks_freq) / len(peaks_freq), 4),
            "px_above_mean": round(sum(peaks_px) / len(peaks_px), 4),
        },
        "has_pitched_roof": {
            "freq": round(sum(pitched) / len(pitched), 4),
        },
    }


def _summarize_ground(measurements: list[dict]) -> dict:
    """Aggregate ground measurements across N sprites (TECH-719).

    ``dominant``: modal RGB across all per-sprite dominants (or ``None``).
    ``variance``: mean of per-sprite stddevs; ``None`` when all are ``None``.
    """
    all_dom: list[tuple[int, int, int]] = []
    for m in measurements:
        d = m["ground"]["dominant"]
        if d is not None:
            all_dom.append(tuple(d))  # type: ignore[arg-type]

    dominant: list[int] | None = None
    if all_dom:
        dominant_rgb, _ = Counter(all_dom).most_common(1)[0]
        dominant = list(dominant_rgb)

    hue_vals = [m["ground"]["variance"]["hue_stddev"] for m in measurements if m["ground"]["variance"]["hue_stddev"] is not None]
    val_vals = [m["ground"]["variance"]["value_stddev"] for m in measurements if m["ground"]["variance"]["value_stddev"] is not None]

    hue_mean: float | None = round(sum(hue_vals) / len(hue_vals), 4) if hue_vals else None
    val_mean: float | None = round(sum(val_vals) / len(val_vals), 4) if val_vals else None

    return {
        "dominant": dominant,
        "variance": {
            "hue_stddev": hue_mean,
            "value_stddev": val_mean,
        },
    }


def _summarize_palette(measurements: list[dict]) -> dict:
    def _aggregate(field_name: str, n: int) -> list[list[int]]:
        all_rgb: list[tuple[int, int, int]] = []
        for m in measurements:
            for rgb in m["palette"][field_name]:
                all_rgb.append(tuple(rgb))
        return [list(rgb) for rgb, _ in Counter(all_rgb).most_common(n)]

    return {
        "wall_dominant": _aggregate("wall_dominant", 3),
        "roof_dominant": _aggregate("roof_dominant", 3),
        "top_20pct_band": _aggregate("top_20pct_band", 5),
    }


def _summarize_decoration_hints(measurements: list[dict]) -> dict:
    grass = [m["decoration_hints"]["grass_ratio_mean"] for m in measurements]
    return {
        "trees_per_tile_mean": None,
        "grass_ratio_mean": round(sum(grass) / len(grass), 4),
    }


def _load_fallback_graph(path: Optional[Path]) -> dict:
    if path is None or not Path(path).is_file():
        return {}
    with Path(path).open("r", encoding="utf-8") as fh:
        return json.load(fh)


def _summarize(
    class_name: str,
    measurements: list[dict],
    checksum: str,
    fallback_graph_path: Optional[Path],
) -> dict:
    """Dispatch on len(measurements) per L15."""
    now = _dt.date.today().isoformat()

    if len(measurements) == 0:
        graph = _load_fallback_graph(fallback_graph_path)
        target = graph.get(class_name)
        return {
            "class": class_name,
            "refreshed_at": now,
            "source_count": 0,
            "source_checksum": checksum,
            "mode": "fallback",
            "fallback_of": target,
            "bbox": None,
            "palette": None,
            "silhouette": None,
            "ground": None,
            "decoration_hints": None,
        }

    point_match = len(measurements) == 1
    mode = "point-match" if point_match else "envelope"
    return {
        "class": class_name,
        "refreshed_at": now,
        "source_count": len(measurements),
        "source_checksum": checksum,
        "mode": mode,
        "fallback_of": None,
        "bbox": _summarize_bbox(measurements, point_match=point_match),
        "palette": _summarize_palette(measurements),
        "silhouette": _summarize_silhouette(measurements),
        "ground": _summarize_ground(measurements),
        "decoration_hints": _summarize_decoration_hints(measurements),
    }


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------


def _resolve_sources(
    folder_glob,
    *,
    spec_loader: Optional[Callable[[Path], dict]] = None,
) -> list[Path]:
    """Expand glob(s) + filter out specs flagged include_in_signature=false.

    `folder_glob` accepts either a single glob string or a list of glob
    strings; results are de-duped by path.

    `spec_loader` is an injected callable returning the spec dict for a given
    .yaml path; when None no YAML filtering happens (pure PNG glob).
    """
    from glob import glob as _glob

    patterns = [folder_glob] if isinstance(folder_glob, str) else list(folder_glob)
    collected: set[Path] = set()
    for pat in patterns:
        for p in _glob(pat, recursive=True):
            collected.add(Path(p))
    matches = sorted(collected)
    pngs = [p for p in matches if p.suffix.lower() == ".png"]

    if spec_loader is None:
        return pngs

    # TECH-706 per-sprite opt-out: if a sibling YAML spec declares
    # include_in_signature: false, drop the PNG.
    keep: list[Path] = []
    for png in pngs:
        yaml_path = png.with_suffix(".yaml")
        if yaml_path.is_file():
            try:
                spec = spec_loader(yaml_path)
            except Exception:  # pragma: no cover — loader errors fall back to include
                keep.append(png)
                continue
            if spec.get("include_in_signature", True) is False:
                continue
        keep.append(png)
    return keep


def compute_signature(
    class_name: str,
    folder_glob,
    *,
    fallback_graph_path: Optional[Path] = None,
    spec_loader: Optional[Callable[[Path], dict]] = None,
) -> dict:
    """Compute the full signature dict for *class_name*.

    Args:
        class_name: Canonical class label (e.g. ``residential_small``).
        folder_glob: Glob pattern string OR list of glob patterns
            resolving to source PNGs.
        fallback_graph_path: Optional path to ``_fallback.json`` used when
            ``source_count == 0``.
        spec_loader: Optional callable ``(Path) -> dict`` to honour the
            TECH-706 ``include_in_signature`` per-sprite opt-out.

    Returns:
        JSON-serialisable dict matching the §Stage 6.2 signature shape.
    """
    sources = _resolve_sources(folder_glob, spec_loader=spec_loader)
    checksum = _compute_checksum(sources)
    measurements = [_measure(p) for p in sources]
    return _summarize(class_name, measurements, checksum, fallback_graph_path)


def validate_against(
    signature: dict,
    rendered_img: Image.Image,
    *,
    live_sources: Optional[Iterable[Path]] = None,
) -> ValidationReport:
    """Validate *rendered_img* against *signature* envelope.

    Args:
        signature: Signature dict produced by :func:`compute_signature`.
        rendered_img: Newly rendered sprite (PIL Image).
        live_sources: Optional iterable of source Paths the signature was
            built from; when provided, the function recomputes the checksum
            and raises :class:`SignatureStaleError` on drift (L3).

    Returns:
        :class:`ValidationReport` with ``ok=True`` iff every bbox /
        spans_full_width constraint holds.

    Raises:
        SignatureStaleError: ``live_sources`` checksum drifts from
            ``signature['source_checksum']``.
    """
    if live_sources is not None:
        live_checksum = _compute_checksum(live_sources)
        if live_checksum != signature.get("source_checksum"):
            raise SignatureStaleError(
                f"signature stale — run python3 -m src refresh-signatures {signature.get('class')}"
            )

    failures: list[str] = []

    mode = signature.get("mode")
    if mode == "fallback":
        # Fallback signatures cannot meaningfully validate; treat as pass.
        return ValidationReport(ok=True, failures=[])

    bbox_sig = signature.get("bbox") or {}
    meas = _measure_bbox(rendered_img)

    h_env = bbox_sig.get("height") or {}
    if h_env:
        if not (h_env["min"] <= meas["height"] <= h_env["max"]):
            failures.append(
                f"bbox.height {meas['height']} outside [{h_env['min']}, {h_env['max']}]"
            )

    y0_env = bbox_sig.get("y0") or {}
    if y0_env:
        if not (y0_env["min"] <= meas["y0"] <= y0_env["max"]):
            failures.append(
                f"bbox.y0 {meas['y0']} outside [{y0_env['min']}, {y0_env['max']}]"
            )

    spans = bbox_sig.get("spans_full_width")
    if spans is True and not meas["spans_full_width"]:
        failures.append("bbox.spans_full_width expected True, got False")

    return ValidationReport(ok=not failures, failures=failures)


# ---------------------------------------------------------------------------
# TECH-725 — Three-source envelope aggregator
# ---------------------------------------------------------------------------
#
# Turns three inputs — catalog prior, promoted JSONL rows, rejected JSONL rows
# — into a single ``vary.*`` envelope the composer can gate renders against.
#
# Envelope shape::
#
#     {
#         "<dotted.axis.path>": {"min": <num>, "max": <num>},
#         ...
#     }
#
# Keys are dotted ``vary_values`` leaf paths (e.g. ``"roof.h_px"``). The
# envelope dict is distinct from the catalog signature shape (bbox / palette /
# silhouette / ground / decoration_hints) — it models the ``vary.*`` sampling
# space directly.
#
# Semantics (``envelope = catalog ∪ promoted − rejected-zones``):
#
#   - Catalog prior seeds initial bounds per axis.
#   - Promoted rows tighten bounds toward the validated centroid (running
#     ``min`` / ``max`` of promoted ``vary_values``).
#   - Rejected rows carve floor zones per :data:`REASON_AXIS_MAP` — each
#     rejection reason pins the ``min`` or ``max`` of a single axis one unit
#     away from the rejected sample.
#   - Determinism: rows are sorted by ``(variant_path, timestamp)`` before
#     aggregation; same inputs produce byte-identical output.


# Lazy import to avoid circular dependency — curate.py imports signature.py
# transitively via its spec_loader plumbing in a future task. The constant
# itself is bound here as module-level for import stability.
from .curate import REJECTION_REASONS  # noqa: E402  (placed after public API)


REASON_AXIS_MAP: dict[str, tuple[str, str]] = {
    "roof-too-shallow":     ("roof.h_px",         "min"),
    "roof-too-tall":        ("roof.h_px",         "max"),
    "facade-too-saturated": ("facade.saturation", "max"),
    "ground-too-uniform":   ("ground.hue_jitter", "min"),
}


def _row_sort_key(row: dict) -> tuple:
    """Stable sort key for promoted / rejected rows — determinism guard."""
    return (str(row.get("variant_path", "")), float(row.get("timestamp", 0.0)))


def _flatten_vary_values(vary: dict, prefix: str = "") -> dict[str, float]:
    """Walk a ``vary_values`` tree → flat ``{"dotted.path": scalar}`` dict.

    Non-numeric leaves (strings, lists) are dropped — envelope math is scalar.
    """
    flat: dict[str, float] = {}
    for key, val in vary.items():
        path = f"{prefix}{key}" if not prefix else f"{prefix}.{key}"
        if isinstance(val, dict):
            flat.update(_flatten_vary_values(val, prefix=path))
        elif isinstance(val, (int, float)) and not isinstance(val, bool):
            flat[path] = float(val)
    return flat


def _initial_envelope_from_catalog(catalog: Optional[dict]) -> dict[str, dict]:
    """Seed the envelope from an optional catalog prior.

    ``catalog`` accepts a flat ``{axis: {min, max}}`` dict or a nested
    ``vary_values``-shaped dict; both normalize into the canonical flat shape.
    ``None`` or ``{}`` → empty envelope.
    """
    if not catalog:
        return {}
    env: dict[str, dict] = {}
    for key, val in catalog.items():
        if isinstance(val, dict) and {"min", "max"}.issubset(val.keys()):
            env[key] = {"min": float(val["min"]), "max": float(val["max"])}
        elif isinstance(val, dict):
            # Nested shape — flatten recursively.
            nested = _initial_envelope_from_catalog(
                {f"{key}.{k}": v for k, v in val.items()}
            )
            env.update(nested)
    return env


def _promoted_hull(promoted: list[dict]) -> dict[str, dict]:
    """Build the min/max hull over every promoted sample per axis."""
    hull: dict[str, dict] = {}
    for row in promoted:
        for axis, value in _flatten_vary_values(row.get("vary_values", {})).items():
            cur = hull.get(axis)
            if cur is None:
                hull[axis] = {"min": value, "max": value}
            else:
                cur["min"] = min(cur["min"], value)
                cur["max"] = max(cur["max"], value)
    return hull


def _apply_promoted_hull(env: dict[str, dict], hull: dict[str, dict]) -> None:
    """Tighten the envelope toward the promoted hull.

    Semantics: when both catalog prior and promoted hull exist on an axis,
    the envelope collapses to the promoted hull (strictly ≤ prior, since
    well-formed promoted samples sit inside the prior). When only catalog
    exists, the prior is kept. When only promoted exists, the hull seeds the
    axis (no prior to intersect).
    """
    for axis, bounds in hull.items():
        env[axis] = {"min": bounds["min"], "max": bounds["max"]}


def _carve_out(
    env: dict[str, dict],
    axis_path: str,
    bound: str,
    vary_values: dict,
) -> None:
    """Push a bound away from a rejected sample by +1 / −1 unit.

    ``bound == "min"`` → raise ``env[axis].min`` to ``rejected_value + 1``.
    ``bound == "max"`` → lower ``env[axis].max`` to ``rejected_value - 1``.
    No-op when the rejected sample has no value on ``axis_path``.
    If the axis is absent from the envelope, the carve-out seeds it as a
    degenerate point at the carved bound — downstream code treats it as a
    floor/ceiling constraint.
    """
    flat = _flatten_vary_values(vary_values)
    rejected = flat.get(axis_path)
    if rejected is None:
        return
    cur = env.get(axis_path)
    if bound == "min":
        new_min = rejected + 1.0
        if cur is None:
            env[axis_path] = {"min": new_min, "max": new_min}
        else:
            cur["min"] = max(cur["min"], new_min)
            # Ensure min <= max (tighten max if carve-out collapsed range).
            if cur["min"] > cur["max"]:
                cur["max"] = cur["min"]
    elif bound == "max":
        new_max = rejected - 1.0
        if cur is None:
            env[axis_path] = {"min": new_max, "max": new_max}
        else:
            cur["max"] = min(cur["max"], new_max)
            if cur["max"] < cur["min"]:
                cur["min"] = cur["max"]


def compute_envelope(
    catalog: Optional[dict] = None,
    promoted: Optional[list[dict]] = None,
    rejected: Optional[list[dict]] = None,
) -> dict[str, dict]:
    """Aggregate three sources into a live ``vary.*`` envelope.

    Args:
        catalog: Optional catalog prior — flat ``{axis: {min, max}}`` or
            nested ``vary_values``-shaped dict. ``None`` or ``{}`` → empty
            prior (envelope derived from promoted alone).
        promoted: Iterable of promoted JSONL rows (TECH-723 schema). Each row
            must carry ``vary_values`` + ``variant_path`` + ``timestamp``.
        rejected: Iterable of rejected JSONL rows (TECH-724 schema). Each
            row must carry ``reason`` in :data:`REASON_AXIS_MAP`.

    Returns:
        Flat envelope dict ``{axis: {min, max}}``. Axes present in any of the
        three inputs appear in the result; axes present nowhere are omitted.

    Raises:
        KeyError: ``rejected`` row carries a ``reason`` outside
            :data:`REASON_AXIS_MAP`.
    """
    env = _initial_envelope_from_catalog(catalog)

    # Promoted hull is order-independent (min/max aggregates); still sort the
    # input list for stable downstream traces.
    promoted_sorted = sorted(promoted or [], key=_row_sort_key)
    hull = _promoted_hull(promoted_sorted)
    _apply_promoted_hull(env, hull)

    for row in sorted(rejected or [], key=_row_sort_key):
        reason = row["reason"]
        axis_path, bound = REASON_AXIS_MAP[reason]
        _carve_out(env, axis_path, bound, row.get("vary_values", {}))

    return env
