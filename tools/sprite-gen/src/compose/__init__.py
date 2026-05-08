"""compose/ — Sprite composition layer barrel (TECH-23787 / Stage 14 atomization).

All public symbols are re-exported here so import sites using
``from src.compose import X`` or ``from .compose import X`` stay unchanged.

``compose_sprite``, ``compose_layers``, ``render``, ``_score_variant``, and
``_write_needs_review`` are defined here (not in sub-modules) so that test
monkeypatching via ``monkeypatch.setattr(src.compose, "load_palette", ...)``
and ``monkeypatch.setattr(src.compose, "_score_variant", ...)`` works via
normal Python module-globals lookup — identical to the original flat module.
"""

from __future__ import annotations

import json as _json
import math as _math
from dataclasses import asdict as _asdict, dataclass as _dataclass
from pathlib import Path as _Path

from PIL import Image

from ..canvas import canvas_size
from ..palette import load_palette  # exposed here for test monkeypatching
from ..primitives import (
    iso_ground_diamond,
    iso_ground_noise,
    iso_stepped_foundation,
)
from ..slopes import SlopeKeyError, get_corner_z  # noqa: F401 — re-exported for callers
from ..spec import (
    _DEFAULT_GROUND,
    composition_entries,
    default_footprint_ratio_for_class,
    default_ground_for_class,
)

# Sub-module helpers — pure functions with no monkeypatching surface
from ._errors import DecorationScopeError, UnknownPrimitiveError  # noqa: F401
from ._animate import _check_animate  # noqa: F401
from ._level_expand import _expand_level_entries  # noqa: F401
from ._jitter import _jitter_ground_palette, _jittered_ramp  # noqa: F401
from ._dispatch import _DECORATION_DISPATCH, _DISPATCH  # noqa: F401
from ._ground import _apply_decorations, _ground_material, _scope_gate_decorations  # noqa: F401
from ._placement_box import _anchor_offset, resolve_building_box  # noqa: F401
from ._variants import (  # noqa: F401
    _COMPOSITION_ROLE_KEYS,
    _apply_vary_ground,
    _axis_scope,
    _sample_leaf,
    _set_deep,
    _walk_vary,
    sample_variant,
)


# ---------------------------------------------------------------------------
# render quality-gate dataclass + pure helpers (in this namespace so
# monkeypatch.setattr(src.compose, "_score_variant", ...) works in tests).
# ---------------------------------------------------------------------------

_FLOOR: float = 0.5  # score threshold; below → retry


@_dataclass
class NeedsReviewSidecar:
    """Non-blocking diagnostic emitted when the composer gate exhausts retries (TECH-727)."""

    schema_version: int
    final_score: float
    envelope_snapshot: dict
    attempted_seeds: list
    failing_zones: list


def _needs_review_path(variant_path) -> _Path:
    """Return the ``<variant>.needs_review.json`` path next to *variant_path*."""
    p = _Path(variant_path)
    return p.with_suffix(".needs_review.json")


def _write_needs_review(
    variant_path,
    *,
    final_score: float,
    envelope_snapshot: dict,
    attempted_seeds: list,
    failing_zones: list,
) -> _Path:
    """Write the sidecar next to *variant_path* and return its path."""
    sidecar = NeedsReviewSidecar(
        schema_version=1,
        final_score=float(final_score),
        envelope_snapshot=envelope_snapshot,
        attempted_seeds=list(attempted_seeds),
        failing_zones=list(failing_zones),
    )
    out = _needs_review_path(variant_path)
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(_json.dumps(_asdict(sidecar), indent=2, sort_keys=True))
    return out


def _score_variant(vary_values: dict, envelope: dict) -> dict:
    """Score one sampled variant against the live envelope."""
    flat = _flatten_vary_leaves(vary_values)
    failing = _carved_zone_hits(flat, envelope)
    if failing:
        return {"score": 0.0, "failing_zones": failing}

    squares, n = 0.0, 0
    for axis, value in flat.items():
        bounds = envelope.get(axis)
        if bounds is None:
            continue
        lo, hi = float(bounds["min"]), float(bounds["max"])
        c = (lo + hi) / 2.0
        h = (hi - lo) / 2.0
        d = 0.0 if h == 0 else min(abs(value - c) / h, 1.0)
        squares += d * d
        n += 1
    if n == 0:
        return {"score": 1.0, "failing_zones": []}
    l2 = _math.sqrt(squares / n)
    return {"score": 1.0 - l2, "failing_zones": []}


def _flatten_vary_leaves(vary_values: dict, prefix: str = "") -> dict[str, float]:
    """Walk ``vary_values`` → ``{"dotted.path": scalar}`` (scalar leaves only)."""
    flat: dict[str, float] = {}
    for key, val in vary_values.items():
        path = key if not prefix else f"{prefix}.{key}"
        if isinstance(val, dict):
            flat.update(_flatten_vary_leaves(val, prefix=path))
        elif isinstance(val, (int, float)) and not isinstance(val, bool):
            flat[path] = float(val)
    return flat


def _carved_zone_hits(flat_values: dict[str, float], envelope: dict) -> list[str]:
    """Return axes where the sampled value sits inside a carve-out zone."""
    hits: list[str] = []
    for axis, value in flat_values.items():
        bounds = envelope.get(axis)
        if bounds is None:
            continue
        lo, hi = float(bounds["min"]), float(bounds["max"])
        if value < lo or value > hi:
            hits.append(axis)
    return hits


def _diff_vary_leaves_inline(base_spec: dict, sampled_spec: dict) -> dict:
    """Reconstruct concrete ``vary_values`` from a sampled spec."""
    variants = (base_spec.get("variants") or {})
    vary_decl = variants.get("vary") or {}
    out: dict = {}
    for path_tuple, _leaf in _walk_vary(vary_decl, ()):
        value = _read_sampled_deep(sampled_spec, path_tuple)
        if value is None:
            continue
        _set_nested(out, list(path_tuple), value)
    return out


def _read_sampled_deep(sampled_spec: dict, path: tuple):
    """Fetch scalar value at dotted ``vary:`` leaf path from sampled spec (TECH-720)."""
    if not path:
        return None
    root = path[0]

    if root == "ground":
        g = sampled_spec.get("ground")
        if not isinstance(g, dict):
            return None
        if len(path) == 2 and path[1] in ("hue_jitter", "value_jitter"):
            entry = g.get(path[1])
            if isinstance(entry, dict) and "min" in entry and entry.get("min") == entry.get("max"):
                return entry["min"]
            return None
        if len(path) == 3 and path[1] == "texture" and path[2] == "density":
            tex = g.get("texture")
            if isinstance(tex, dict):
                return tex.get("density")
            return None
        return None

    cursor = sampled_spec
    for key in path:
        if not isinstance(cursor, dict):
            return None
        cursor = cursor.get(key)
    if isinstance(cursor, (int, float)) and not isinstance(cursor, bool):
        return cursor
    if isinstance(cursor, str):
        return cursor
    return None


def _set_nested(out: dict, keys: list, value) -> None:
    cursor = out
    for key in keys[:-1]:
        sub = cursor.get(key)
        if not isinstance(sub, dict):
            sub = {}
            cursor[key] = sub
        cursor = sub
    cursor[keys[-1]] = value


# ---------------------------------------------------------------------------
# compose_sprite + compose_layers — defined here so test monkeypatching of
# ``src.compose.load_palette`` via ``monkeypatch.setattr`` works correctly.
# ---------------------------------------------------------------------------


def compose_sprite(spec: dict) -> Image.Image:
    """Compose a sprite from an archetype spec dict.

    Decoration pipeline (TECH-769): scope-gate spec['decorations'] at entry
    (raises DecorationScopeError on 1x1 + iso_pool), then apply via
    _apply_decorations between ground-diamond render and building pass.

    See original compose.py module docstring for full parameter contract.
    """
    _scope_gate_decorations(spec)
    fx, fy = spec["footprint"]
    composition = composition_entries(spec)
    composition = _expand_level_entries(composition, spec)
    slope_id: str = spec.get("terrain", "flat")

    palette = load_palette(spec["palette"])

    def _stack_h(entry: dict) -> int:
        h = entry.get("h_px", entry.get("h", 0))
        return int(h) + int(entry.get("offset_z", 0))

    if composition:
        stack_extra_h = max(_stack_h(entry) for entry in composition)
    else:
        stack_extra_h = 0

    if slope_id != "flat":
        corners = get_corner_z(slope_id)
        lip = max(corners.values()) + 2
    else:
        lip = 0

    extra_h = max(stack_extra_h, lip)

    w_px, h_px = canvas_size(fx, fy, extra_h)
    h_px = max(h_px, 64)

    canvas = Image.new("RGBA", (w_px, h_px), (0, 0, 0, 0))

    x0 = w_px // 2
    y0 = h_px

    cls = str(spec.get("class", ""))
    graw = spec.get("ground")
    if graw != "none":
        g_material = _ground_material(graw)
        in_map = cls in _DEFAULT_GROUND
        if g_material is not None or in_map:
            gmat = g_material if g_material is not None else default_ground_for_class(cls)

            g_passthrough = bool(isinstance(graw, dict) and graw.get("passthrough"))

            g_hue_jitter = isinstance(graw, dict) and graw.get("hue_jitter") or None
            g_value_jitter = isinstance(graw, dict) and graw.get("value_jitter") or None
            if g_passthrough:
                if isinstance(g_hue_jitter, dict):
                    g_hue_jitter = {
                        "min": max(-0.01, min(0.01, float(g_hue_jitter.get("min", 0)))),
                        "max": max(-0.01, min(0.01, float(g_hue_jitter.get("max", 0)))),
                    }
                g_value_jitter = None
            palette_seed: int = int(spec.get("palette_seed", spec.get("seed", 0)) or 0)
            ground_palette = _jitter_ground_palette(
                palette, gmat, g_hue_jitter, g_value_jitter, seed=palette_seed
            )

            iso_ground_diamond(
                canvas=canvas,
                x0=x0,
                y0=y0,
                fx=fx,
                fy=fy,
                material=gmat,
                palette=ground_palette,
            )

            g_texture = isinstance(graw, dict) and graw.get("texture") or None
            if g_texture and not g_passthrough:
                t_density = float(g_texture.get("density", 0.0)) if isinstance(g_texture, dict) else 0.0
                if t_density > 0.0:
                    noise_seed = palette_seed + 1
                    span = fx + fy
                    top_y = span * 8 - 1
                    iso_ground_noise(
                        canvas=canvas,
                        x0=x0,
                        y0=top_y,
                        material=gmat,
                        density=t_density,
                        seed=noise_seed,
                        palette=ground_palette,
                        fx=fx,
                        fy=fy,
                    )

    building = spec.get("building") or {}
    fr = building.get("footprint_ratio")
    if fr is None:
        wr, dr = default_footprint_ratio_for_class(str(spec.get("class", "")))
    else:
        wr, dr = float(fr[0]), float(fr[1])

    spec_for_box = dict(spec)
    spec_for_box["canvas"] = [w_px, h_px]
    _, _, bld_ox, bld_oy = resolve_building_box(spec_for_box)

    if slope_id != "flat":
        foundation_material: str = spec.get("foundation_material", "dirt")
        iso_stepped_foundation(
            canvas=canvas,
            x0=x0,
            y0=y0,
            fx=fx,
            fy=fy,
            slope_id=slope_id,
            material=foundation_material,
            palette=palette,
        )

    _apply_decorations(canvas, spec, palette)

    for entry in composition:
        entry = _check_animate(entry)
        prim_type = entry.get("type")
        fn = _DISPATCH.get(prim_type)  # type: ignore[arg-type]
        if fn is None:
            raise UnknownPrimitiveError(
                f"Unknown primitive type {prim_type!r}. "
                f"Known types: {sorted(_DISPATCH.keys())}"
            )

        if prim_type == "iso_ground_diamond":
            material_g = str(entry.get("material", ""))
            iso_ground_diamond(
                canvas=canvas,
                x0=x0,
                y0=y0,
                fx=int(entry.get("fx", fx)),
                fy=int(entry.get("fy", fy)),
                material=material_g,
                palette=palette,
            )
            continue

        material = str(entry.get("material", ""))
        offset_z = int(entry.get("offset_z", 0))
        pivot_pad = 17 if spec.get("ground") != "none" else 0
        adjusted_y0 = y0 - pivot_pad - offset_z

        kwargs: dict = {
            "canvas":   canvas,
            "x0":       x0 + bld_ox,
            "y0":       adjusted_y0 + bld_oy,
            "material": material,
            "palette":  palette,
        }
        for k in ("w_px", "d_px", "h_px", "w", "d", "h"):
            if k in entry:
                kwargs[k] = int(entry[k]) if k.endswith("_px") else float(entry[k])
        if "w" not in entry and "w_px" not in entry:
            kwargs["w"] = 1.0
        if "d" not in entry and "d_px" not in entry:
            kwargs["d"] = 1.0
        if "h" not in entry and "h_px" not in entry:
            kwargs["h"] = 0.0

        if prim_type == "iso_prism":
            kwargs["pitch"] = float(entry.get("pitch", 0.5))
            kwargs["axis"]  = str(entry.get("axis", "ns"))

        if wr != 1.0 or dr != 1.0:
            if "w_px" in kwargs:
                kwargs["w_px"] = int(round(float(kwargs["w_px"]) * wr))
            elif "w" in kwargs:
                kwargs["w"] = float(kwargs["w"]) * wr
            if "d_px" in kwargs:
                kwargs["d_px"] = int(round(float(kwargs["d_px"]) * dr))
            elif "d" in kwargs:
                kwargs["d"] = float(kwargs["d"]) * dr

        fn(**kwargs)  # type: ignore[operator]

    return canvas


def compose_layers(spec: dict) -> tuple[dict[str, Image.Image], tuple[int, int]]:
    """Per-face RGBA layers for layered `.aseprite` export (TECH-182)."""
    fx, fy = spec["footprint"]
    composition = _expand_level_entries(composition_entries(spec), spec)
    slope_id: str = spec.get("terrain", "flat")

    palette = load_palette(spec["palette"])

    def _stack_h2(entry: dict) -> int:
        h = entry.get("h_px", entry.get("h", 0))
        return int(h) + int(entry.get("offset_z", 0))

    if composition:
        stack_extra_h = max(_stack_h2(entry) for entry in composition)
    else:
        stack_extra_h = 0

    if slope_id != "flat":
        corners = get_corner_z(slope_id)
        lip = max(corners.values()) + 2
    else:
        lip = 0

    extra_h = max(stack_extra_h, lip)
    w_px, h_px = canvas_size(fx, fy, extra_h)
    h_px = max(h_px, 64)
    x0 = w_px // 2
    y0 = h_px

    layers: dict[str, Image.Image] = {
        "top": Image.new("RGBA", (w_px, h_px), (0, 0, 0, 0)),
        "south": Image.new("RGBA", (w_px, h_px), (0, 0, 0, 0)),
        "east": Image.new("RGBA", (w_px, h_px), (0, 0, 0, 0)),
    }

    if slope_id != "flat":
        foundation_layer = Image.new("RGBA", (w_px, h_px), (0, 0, 0, 0))
        foundation_material: str = spec.get("foundation_material", "dirt")
        iso_stepped_foundation(
            canvas=foundation_layer,
            x0=x0,
            y0=y0,
            fx=fx,
            fy=fy,
            slope_id=slope_id,
            material=foundation_material,
            palette=palette,
        )
        layers["foundation"] = foundation_layer

    flat = compose_sprite(spec)
    for face in ("top", "south", "east"):
        layers[face] = flat.copy()

    return layers, (w_px, h_px)


# ---------------------------------------------------------------------------
# Render generator — defined here so ``_score_variant`` and ``_write_needs_review``
# are looked up from this module's globals (enabling test monkeypatching).
# ---------------------------------------------------------------------------


def render(
    spec: dict,
    *,
    envelope: dict | None = None,
    retry_cap: int = 5,
    gate_enabled: bool = False,
    variant_paths: list | None = None,
):
    """Variant-producing generator with optional envelope-gated retry (TECH-726)."""
    variants = spec.get("variants")
    count = (
        variants.get("count", 1) if isinstance(variants, dict) else 1
    )

    gate_active = gate_enabled and envelope is not None

    for i in range(count):
        if not gate_active:
            yield compose_sprite(sample_variant(spec, i))
            continue

        palette_seed = int(spec.get("palette_seed", spec.get("seed", 0)) or 0)
        best = None
        attempts: list[int] = []
        passed = False

        for retry in range(retry_cap):
            seed = palette_seed + i * (retry_cap + 1) + retry
            spec_i = {**spec, "palette_seed": seed}
            sampled = sample_variant(spec_i, i)
            variant_img = compose_sprite(sampled)
            vary_values = _diff_vary_leaves_inline(spec_i, sampled)
            score = _score_variant(vary_values, envelope)
            attempts.append(seed)
            if best is None or score["score"] > best["score"]:
                best = {
                    "image": variant_img,
                    "score": score["score"],
                    "failing_zones": score["failing_zones"],
                    "retry": retry,
                    "seed": seed,
                    "attempts": list(attempts),
                }
            if score["score"] >= _FLOOR:
                passed = True
                yield variant_img
                break

        if not passed and best is not None:
            full_attempts = list(attempts)
            target_path = None
            if variant_paths is not None and i < len(variant_paths):
                target_path = variant_paths[i]
            if target_path is not None:
                _write_needs_review(
                    target_path,
                    final_score=best["score"],
                    envelope_snapshot=envelope,
                    attempted_seeds=full_attempts,
                    failing_zones=best["failing_zones"],
                )
                yield best["image"]
            else:
                yield (
                    best["image"],
                    {
                        "score": best["score"],
                        "seed": best["seed"],
                        "attempts": full_attempts,
                        "failing_zones": best["failing_zones"],
                    },
                )
