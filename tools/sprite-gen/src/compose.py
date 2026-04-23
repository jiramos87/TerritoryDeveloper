"""
compose.py — Compose layer for sprite-gen (Stage 1.3 + TECH-177).

``compose_sprite(spec: dict) -> PIL.Image`` wires the primitive library into a
single entry point driven by an archetype spec dict.  YAML parsing lives in
spec.py; this module receives an already-validated dict.

Origin convention (§4 Canvas math, iso_cube Decision Log 2026-04-14):
    (x0, y0) = footprint SE corner, y-down.  For a canvas of size (W, H):
        x0 = W // 2           (horizontal midpoint == SE corner of 1×1 diamond)
        y0 = H                (bottom pixel row; primitives draw upward)

offset_z handling:
    Each composition entry may carry ``offset_z`` (pixels, positive = up).
    The composer subtracts ``offset_z`` from ``y0`` before calling the primitive
    (y-down screen coord; higher z → smaller py).  Primitives themselves have
    no ``offset_z`` parameter.

Palette wiring (Stage 1.3 palette system):
    ``load_palette(spec["palette"])`` is called once per ``compose_sprite`` call.
    The loaded palette dict and the raw material string are passed directly into
    each primitive via ``material=`` and ``palette=`` kwargs.
    ``PaletteKeyError`` propagates from primitives → caller (CLI wraps → exit 2).
    Missing palette file → ``FileNotFoundError`` propagates → generic exit 1.

Slope auto-insert (TECH-177):
    When ``spec["terrain"]`` is set to a non-``"flat"`` slope id,
    ``compose_sprite`` prepends an ``iso_stepped_foundation`` call before the
    composition loop and grows ``extra_h`` by ``max(corner_z) + 2`` (the lip).
    ``SlopeKeyError`` propagates from ``slopes.get_corner_z`` → caller (CLI
    exit 1).  Absent or ``"flat"`` terrain is a no-op.

Reference:
    docs/isometric-sprite-generator-exploration.md §3, §4, §5, §8
"""

from __future__ import annotations

import copy
import random as _random_mod
from colorsys import hsv_to_rgb, rgb_to_hsv
from random import Random

from PIL import Image

from .canvas import canvas_size
from .constants import DEFAULT_LEVEL_H, LEVEL_H
from .palette import load_palette
from .primitives import (
    iso_cube,
    iso_ground_diamond,
    iso_ground_noise,
    iso_prism,
    iso_stepped_foundation,
)
from .slopes import SlopeKeyError, get_corner_z  # noqa: F401 — re-exported for callers
from .spec import (
    _DEFAULT_GROUND,
    composition_entries,
    default_footprint_ratio_for_class,
    default_ground_for_class,
)

# ---------------------------------------------------------------------------
# Errors
# ---------------------------------------------------------------------------


class UnknownPrimitiveError(ValueError):
    """Raised when a composition entry `type:` is not in the dispatch dict."""


def _expand_level_entries(composition: list, spec: dict) -> list:
    """Stage 6 — repeat role=wall by ``spec.levels``; roof above_walls offset."""
    cls = str(spec.get("class", ""))
    level_h = LEVEL_H.get(cls, DEFAULT_LEVEL_H)
    levels = int(spec.get("levels", 1))
    out: list[dict] = []
    for entry in composition:
        role = entry.get("role")
        if role == "wall" and "h_px" not in entry and "h" not in entry:
            if levels > 1:
                base = int(entry.get("offset_z", 0))
                for i in range(levels):
                    c = dict(entry)
                    c["h_px"] = level_h
                    c["offset_z"] = base + i * level_h
                    out.append(c)
                continue
            c = dict(entry)
            c["h_px"] = level_h
            out.append(c)
            continue
        if role == "roof" and entry.get("offset_z_role") == "above_walls":
            c = dict(entry)
            c["offset_z"] = levels * level_h
            out.append(c)
            continue
        out.append(dict(entry))
    return out


# ---------------------------------------------------------------------------
# TECH-718 — ground ramp HSV jitter helper
# ---------------------------------------------------------------------------


def _jitter_ground_palette(
    palette: dict,
    material: str,
    hue_jitter: dict | None,
    value_jitter: dict | None,
    *,
    seed: int,
) -> dict:
    """Return a palette copy with *material*'s bright/mid/dark ramp jittered (TECH-718).

    Returns *palette* unchanged when both jitters are None or span zero (identity path
    preserves byte-identical legacy behaviour).
    """
    entry = palette["materials"].get(material, {})
    raw_ramp: list[tuple[int, int, int]] = []
    for key in ("bright", "mid", "dark"):
        val = entry.get(key)
        if val is not None:
            raw_ramp.append(tuple(int(c) for c in val))  # type: ignore[arg-type]

    jittered = _jittered_ramp(raw_ramp, hue_jitter, value_jitter, seed)
    if jittered is raw_ramp:
        return palette  # identity — no mutation needed

    # Shallow-copy palette; deep-copy only the target material entry.
    new_mat_entry = dict(entry)
    for i, key in enumerate(("bright", "mid", "dark")):
        if i < len(jittered):
            new_mat_entry[key] = list(jittered[i])
    new_materials = dict(palette["materials"])
    new_materials[material] = new_mat_entry
    return {**palette, "materials": new_materials}


def _jittered_ramp(
    ramp: list[tuple[int, int, int]],
    hue_jitter: dict | None,
    value_jitter: dict | None,
    seed: int,
) -> list[tuple[int, int, int]]:
    """Return a hue/value-jittered copy of *ramp* (TECH-718).

    Identity (returns *ramp* unchanged) when both jitters are None or span zero.

    Args:
        ramp:         List of ``(R, G, B)`` tuples from the palette.
        hue_jitter:   ``{"min": float, "max": float}`` in degrees, or ``None``.
        value_jitter: ``{"min": float, "max": float}`` in % of HSV V, or ``None``.
        seed:         RNG seed — deterministic per ``palette_seed + variant_index``.

    Returns:
        New list of ``(R, G, B)`` tuples, or *ramp* itself when no jitter applies.
    """
    hj_min = hj_max = 0.0
    vj_min = vj_max = 0.0
    if hue_jitter:
        hj_min = float(hue_jitter.get("min", 0))
        hj_max = float(hue_jitter.get("max", 0))
    if value_jitter:
        vj_min = float(value_jitter.get("min", 0))
        vj_max = float(value_jitter.get("max", 0))

    if hj_min == hj_max == 0.0 and vj_min == vj_max == 0.0:
        return ramp  # identity — no copy needed

    rng = _random_mod.Random(seed)
    dh = rng.uniform(hj_min, hj_max) / 360.0
    dv = rng.uniform(vj_min, vj_max) / 100.0

    out: list[tuple[int, int, int]] = []
    for r, g, b in ramp:
        h, s, v = rgb_to_hsv(r / 255.0, g / 255.0, b / 255.0)
        h = (h + dh) % 1.0
        v = max(0.0, min(1.0, v + dv))
        nr, ng, nb = hsv_to_rgb(h, s, v)
        out.append((int(nr * 255), int(ng * 255), int(nb * 255)))
    return out


# ---------------------------------------------------------------------------
# Dispatch table
# ---------------------------------------------------------------------------

_DISPATCH: dict[str, object] = {
    "iso_cube": iso_cube,
    "iso_ground_diamond": iso_ground_diamond,
    "iso_ground_noise": iso_ground_noise,
    "iso_prism": iso_prism,
    "iso_stepped_foundation": iso_stepped_foundation,
}


# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------


# ---------------------------------------------------------------------------
# TECH-715 — ground accessor: string | object | None → material name or None
# ---------------------------------------------------------------------------


def _ground_material(graw) -> str | None:
    """Return the effective ground material name or None.

    Accepts:
        - str (legacy form)              → returned verbatim unless empty
        - dict (TECH-715 object form)    → ``material`` key (or first of
                                            ``materials`` list)
        - anything else / falsy          → None
    """
    if graw in (None, ""):
        return None
    if isinstance(graw, str):
        return graw
    if isinstance(graw, dict):
        m = graw.get("material")
        if m:
            return str(m)
        pool = graw.get("materials")
        if isinstance(pool, list) and pool:
            return str(pool[0])
    return None


def compose_sprite(spec: dict) -> Image.Image:
    """Compose a sprite from an archetype spec dict.

    Derives canvas dimensions from the spec's ``footprint`` and ``composition``
    entries, builds an RGBA Pillow canvas, iterates the composition list in
    order (later entries paint on top), and returns the finished image.

    Slope auto-insert (TECH-177):
        When ``spec["terrain"]`` is a non-``"flat"`` slope id,
        ``iso_stepped_foundation`` is drawn **before** the composition loop and
        the canvas grows by the foundation lip (``max_corner_z + 2`` px).
        ``spec["foundation_material"]`` selects the palette key for the
        foundation; defaults to ``"dirt"``.

    Args:
        spec: Validated archetype dict with at minimum:
            footprint:            [fx, fy]  — tile footprint dimensions
            palette:              str       — palette class key (e.g. ``"residential"``)
            terrain:              str       — slope id from slopes.yaml (default ``"flat"``);
                                             non-flat triggers foundation auto-insert.
            foundation_material:  str       — palette key for the foundation layer
                                             (default ``"dirt"``; only used when non-flat).
            composition: list of entries, each with:
                type:       str   — primitive key (``'iso_cube'`` | ``'iso_prism'``)
                w:          float — tile-unit width (grid-X)
                d:          float — tile-unit depth (grid-Y)
                h:          float — height in pixels
                material:   str   — palette material key
                offset_z:   int   — optional vertical offset in pixels (default 0)
                pitch:      float — iso_prism only
                axis:       str   — iso_prism only (``'ns'`` | ``'ew'``)

    Returns:
        PIL.Image (RGBA) with transparent background.

    Raises:
        FileNotFoundError:    If the palette JSON for ``spec["palette"]`` is missing.
        PaletteKeyError:      If a composition entry's ``material`` is absent from the palette.
        UnknownPrimitiveError: If a composition entry's ``type:`` is not in the dispatch dict.
        SlopeKeyError:        If ``spec["terrain"]`` is not a recognised slope id.
    """
    fx, fy = spec["footprint"]
    composition = composition_entries(spec)
    composition = _expand_level_entries(composition, spec)
    slope_id: str = spec.get("terrain", "flat")

    # --- Load palette once for the whole sprite ---
    palette = load_palette(spec["palette"])  # FileNotFoundError propagates if missing

    def _stack_h(entry: dict) -> int:
        h = entry.get("h_px", entry.get("h", 0))
        return int(h) + int(entry.get("offset_z", 0))

    # --- Derive extra_h from tallest stack entry ---
    if composition:
        stack_extra_h = max(_stack_h(entry) for entry in composition)
    else:
        stack_extra_h = 0

    # --- Foundation lip: grows extra_h when slope is non-flat (SlopeKeyError propagates) ---
    if slope_id != "flat":
        corners = get_corner_z(slope_id)  # raises SlopeKeyError on unknown id
        lip = max(corners.values()) + 2
    else:
        lip = 0

    extra_h = max(stack_extra_h, lip)

    # --- Canvas size; clamp height to ≥ 64 px (composer owns clamp per canvas.py docstring) ---
    w_px, h_px = canvas_size(fx, fy, extra_h)
    h_px = max(h_px, 64)

    # --- Build transparent RGBA canvas ---
    canvas = Image.new("RGBA", (w_px, h_px), (0, 0, 0, 0))

    # --- SE-corner anchor (y-down; primitives draw upward from this point) ---
    x0 = w_px // 2
    y0 = h_px

    # --- Stage 6: ground diamond (R11 / DAS classes; legacy specs omit `ground` + old class) ---
    cls = str(spec.get("class", ""))
    graw = spec.get("ground")
    if graw != "none":
        # TECH-715: accept object form from spec loader normalisation + raw string.
        g_material = _ground_material(graw)
        in_map = cls in _DEFAULT_GROUND
        if g_material is not None or in_map:
            gmat = g_material if g_material is not None else default_ground_for_class(cls)

            # TECH-718 — build jittered palette for the ground layer when spec requests it.
            g_hue_jitter = isinstance(graw, dict) and graw.get("hue_jitter") or None
            g_value_jitter = isinstance(graw, dict) and graw.get("value_jitter") or None
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

            # TECH-718 — auto-insert iso_ground_noise when texture is specified.
            g_texture = isinstance(graw, dict) and graw.get("texture") or None
            if g_texture:
                t_density = float(g_texture.get("density", 0.0)) if isinstance(g_texture, dict) else 0.0
                if t_density > 0.0:
                    # Derive noise seed from palette_seed + 1 (distinct from jitter seed).
                    noise_seed = palette_seed + 1
                    # Diamond top-apex y: span*8 - 1 (iso_ground_diamond geometry).
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

    # --- Foundation auto-insert (non-flat terrain only; drawn before composition stack) ---
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

    # --- Iterate composition in order (later entries on top) ---
    for entry in composition:
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
        # DAS §2.1/§2.2: diamond bottom row is at y = canvas_h - 17 (16 px pad + 1 for
        # PIL inclusive pixel indexing). Building primitives anchor at diamond bottom,
        # not canvas bottom. Stage 6.1 hotfix.
        pivot_pad = 17 if spec.get("ground") != "none" else 0
        adjusted_y0 = y0 - pivot_pad - offset_z

        # Build kwargs common to all primitives (Stage 6: forward pixel or tile keys)
        kwargs: dict = {
            "canvas":   canvas,
            "x0":       x0,
            "y0":       adjusted_y0,
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

        # iso_prism-specific kwargs
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
# TECH-711 — pure helpers for placement + variant sampling (Stage 6.3)
# ---------------------------------------------------------------------------


def resolve_building_box(spec: dict) -> tuple[int, int, int, int]:
    """Return `(bx, by, offset_x, offset_y)` for the building mass.

    Pure function: no I/O, no PIL. Honours `building.footprint_px`
    (preferred) or falls back to `footprint_ratio` scaled against a 64×64
    canvas. `align` + `padding` translate into `(offset_x, offset_y)` shift
    from the default SE-corner anchor.

    Defaults (`align: center`, zero padding) return offsets `(0, 0)` so
    existing composer paths are byte-identical.

    Args:
        spec: Validated archetype dict (building subkey optional).

    Returns:
        Tuple ``(bx, by, offset_x, offset_y)`` — mass box in px and the
        SE-anchor shift (+x east, +y south) to apply.
    """
    canvas_w, canvas_h = spec.get("canvas", [64, 64])
    building = spec.get("building") or {}

    footprint_px = building.get("footprint_px")
    if isinstance(footprint_px, (list, tuple)) and len(footprint_px) == 2:
        bx, by = int(footprint_px[0]), int(footprint_px[1])
    else:
        ratio = building.get("footprint_ratio")
        if ratio is None:
            wr, dr = default_footprint_ratio_for_class(str(spec.get("class", "")))
        else:
            wr, dr = float(ratio[0]), float(ratio[1])
        bx = int(round(canvas_w * wr))
        by = int(round(canvas_h * dr))

    align = building.get("align", "center")
    padding = building.get("padding") or {"n": 0, "e": 0, "s": 0, "w": 0}

    # Default path — align: center + zero padding → no shift (byte-identical).
    ox, oy = _anchor_offset(align, bx, by, canvas_w, canvas_h)
    ox += int(padding.get("w", 0)) - int(padding.get("e", 0))
    oy += int(padding.get("n", 0)) - int(padding.get("s", 0))
    return bx, by, ox, oy


def _anchor_offset(
    align: str, bx: int, by: int, canvas_w: int, canvas_h: int
) -> tuple[int, int]:
    """Map alignment anchor to SE-corner offset deltas.

    `center` returns (0, 0) — composer's existing centering math already
    places a centered box correctly via the `wr`/`dr` scaling pass, so
    the helper MUST NOT double-shift for the default path.

    Other anchors compute a deterministic shift from the default centered
    position:
        - sw: shift west + south → (-dx, +dy)
        - se: shift east + south → (+dx, +dy)
        - nw: shift west + north → (-dx, -dy)
        - ne: shift east + north → (+dx, -dy)
    where (dx, dy) = half the leftover canvas space beyond the box.
    `custom` returns (0, 0); callers supply explicit offsets separately.
    """
    if align in ("center", "custom"):
        return 0, 0
    dx = max(0, (canvas_w - bx) // 2)
    dy = max(0, (canvas_h - by) // 2)
    if align == "sw":
        return -dx, dy
    if align == "se":
        return dx, dy
    if align == "nw":
        return -dx, -dy
    if align == "ne":
        return dx, -dy
    return 0, 0


def sample_variant(spec: dict, variant_idx: int) -> dict:
    """Return a deep copy of *spec* with `variants.vary` ranges sampled.

    Uses split seeds (`palette_seed` + `geometry_seed`) + `seed_scope` to
    decide which rng drives each axis. Back-compat: when no `vary:` entries
    exist, returns an unchanged deep copy.

    Args:
        spec: Validated archetype dict (`variants` already normalised to
            object form by `load_spec`).
        variant_idx: Zero-based variant index.

    Returns:
        Deep copy with `vary:` ranges resolved to concrete values written
        into the corresponding spec fields.
    """
    out = copy.deepcopy(spec)
    variants = out.get("variants")
    if not isinstance(variants, dict):
        return out
    vary = variants.get("vary") or {}
    if not vary:
        return out

    palette_seed = int(out.get("palette_seed", out.get("seed", 0)) or 0)
    geometry_seed = int(out.get("geometry_seed", out.get("seed", 0)) or 0)
    scope = variants.get("seed_scope", "palette")

    palette_rng = Random(palette_seed + variant_idx)
    geometry_rng = Random(geometry_seed + variant_idx)

    # TECH-720 — vary.ground is palette-domain; process before generic walk.
    vary_ground = vary.get("ground")
    if vary_ground and isinstance(vary_ground, dict):
        palette_active = scope in ("palette", "palette+geometry")
        if palette_active:
            _apply_vary_ground(out, vary_ground, palette_rng)

    for path, leaf in _walk_vary(vary, ()):
        if path and path[0] == "ground":
            continue  # handled above
        axis_scope = _axis_scope(path)
        active = scope in ("palette+geometry",) or scope == axis_scope
        if not active:
            continue
        rng = palette_rng if axis_scope == "palette" else geometry_rng
        value = _sample_leaf(leaf, rng)
        if value is not None:
            _set_deep(out, path, value)
    return out


def _apply_vary_ground(out: dict, vary_ground: dict, rng: Random) -> None:
    """Merge sampled ``vary.ground`` values into ``out["ground"]`` (TECH-720).

    ``out["ground"]`` must already be an object dict (normalised by TECH-715).
    If absent or string-form, this is a no-op (back-compat).
    """
    g = out.get("ground")
    if not isinstance(g, dict):
        return

    if "material" in vary_ground:
        values = vary_ground["material"].get("values")
        if values:
            g["material"] = rng.choice(values)
            g.pop("materials", None)

    for axis in ("hue_jitter", "value_jitter"):
        if axis in vary_ground:
            r = vary_ground[axis]
            lo, hi = float(r["min"]), float(r["max"])
            sampled = rng.uniform(lo, hi)
            g[axis] = {"min": sampled, "max": sampled}

    if "texture" in vary_ground and "density" in vary_ground["texture"]:
        r = vary_ground["texture"]["density"]
        lo, hi = float(r["min"]), float(r["max"])
        density = rng.uniform(lo, hi)
        g.setdefault("texture", {})
        if isinstance(g["texture"], dict):
            g["texture"]["density"] = density
        else:
            g["texture"] = {"density": density}


def _walk_vary(node, prefix: tuple):
    """Yield `(path_tuple, leaf_dict)` for every terminal `{min,max}` or
    `{values: [...]}` leaf under `vary:`."""
    if isinstance(node, dict):
        # Leaf?
        if ("min" in node and "max" in node) or "values" in node:
            yield prefix, node
            return
        for key, sub in node.items():
            yield from _walk_vary(sub, prefix + (key,))


def _sample_leaf(leaf: dict, rng: Random):
    if "values" in leaf and isinstance(leaf["values"], list) and leaf["values"]:
        return rng.choice(leaf["values"])
    if "min" in leaf and "max" in leaf:
        lo, hi = leaf["min"], leaf["max"]
        if isinstance(lo, int) and isinstance(hi, int):
            return rng.randint(lo, hi)
        return rng.uniform(float(lo), float(hi))
    return None


def _axis_scope(path: tuple) -> str:
    """Classify a `vary.` axis as palette or geometry by path root.

    Palette axes: any path whose root is `palette` or `material` or whose
    leaf name begins with `color`/`hue`/`value`. Everything else is
    geometry (footprint, roof, padding, …).
    """
    if not path:
        return "geometry"
    root = path[0]
    if root in ("palette", "material", "materials"):
        return "palette"
    last = path[-1]
    if any(last.startswith(p) for p in ("color", "hue", "value", "tint")):
        return "palette"
    return "geometry"


def _set_deep(target: dict, path: tuple, value) -> None:
    cursor = target
    for key in path[:-1]:
        sub = cursor.get(key)
        if not isinstance(sub, dict):
            sub = {}
            cursor[key] = sub
        cursor = sub
    cursor[path[-1]] = value


# ---------------------------------------------------------------------------
# TECH-726 — Render-time score-and-retry quality gate
# ---------------------------------------------------------------------------
#
# ``render(spec, *, envelope=None, retry_cap=5, gate_enabled=False)`` wraps
# the variant loop with an envelope-aware score-and-retry gate:
#
#   1. Sample ``vary:`` via :func:`sample_variant`.
#   2. Render via :func:`compose_sprite`.
#   3. Score the sampled ``vary_values`` against the envelope
#      (TECH-725 :func:`signature.compute_envelope`).
#   4. If below ``_FLOOR`` and retries remain → advance seed, re-sample.
#
# Flag-off / ``envelope=None`` paths delegate to pre-Stage-6.5
# ``compose_sprite(sample_variant(spec, i))`` unchanged — existing golden
# tests act as the parity oracle (Open Q2 in TECH-726.md).

import json as _json
import math as _math
from dataclasses import asdict as _asdict, dataclass as _dataclass
from pathlib import Path as _Path

_FLOOR: float = 0.5  # score threshold; below → retry


# TECH-727 — versioned JSON sidecar emitted on gate exhaustion.
@_dataclass
class NeedsReviewSidecar:
    """Non-blocking diagnostic emitted when the composer gate exhausts retries.

    Written adjacent to the best-scoring variant as
    ``<variant>.needs_review.json``. Curator UI / CI consume it to surface
    low-confidence renders without failing the pipeline.

    Schema versioned from day one so downstream consumers can evolve.
    """

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
    """Score one sampled variant against the live envelope.

    Contract:
      - Per-axis normalized deviation ``d_a = clamp(|v_a - c_a| / h_a, 0, 1)``
        where ``c_a = (min+max)/2`` and ``h_a = (max-min)/2``. Degenerate
        ``h_a == 0`` → ``d_a = 0``.
      - Aggregate metric: ``L2 = sqrt(mean(d_a^2))``.
      - Score = ``1.0 - L2`` (higher = closer to centroid).
      - Carved-zone hard-fail: if the sample violates any
        :data:`signature.REASON_AXIS_MAP`-derived floor/ceiling, short-circuit
        to ``{"score": 0.0, "failing_zones": [...]}``.

    Returns:
        ``{"score": float, "failing_zones": list[str]}``.
    """
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
    """Return axes where the sampled value sits inside a carve-out zone.

    Carve-out semantics (TECH-725): envelope ``{min, max}`` bounds were
    tightened away from known-bad samples. A fresh sample violates the
    carve-out iff it falls outside the envelope bounds on that axis.
    """
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
    """Reconstruct concrete ``vary_values`` from a sampled spec.

    Mirrors ``curate._diff_vary_leaves`` but avoids the import (circular
    otherwise — curate depends on compose, not the reverse). Walks the
    declared ``variants.vary`` leaf paths and reads values from the sampled
    spec; handles ``vary.ground.*`` specially per TECH-720 (ground axes land
    on ``sampled["ground"][...]``).
    """
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
    """Fetch scalar value at dotted ``vary:`` leaf path from sampled spec.

    Handles TECH-720 ``vary.ground.*`` axes that live at ``spec["ground"][...]``
    after sampling (not at ``spec["vary"]...``), including the
    ``{min==max}`` collapse shape of `hue_jitter` / `value_jitter`.
    """
    if not path:
        return None
    root = path[0]

    if root == "ground":
        g = sampled_spec.get("ground")
        if not isinstance(g, dict):
            return None
        # vary.ground.material → ground.material (scalar string — skipped by
        # scoring, but still reconstructable for diagnostics).
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


def render(
    spec: dict,
    *,
    envelope: dict | None = None,
    retry_cap: int = 5,
    gate_enabled: bool = False,
    variant_paths: list | None = None,
):
    """Variant-producing generator with optional envelope-gated retry.

    Args:
        spec: Validated archetype dict (already normalised by ``load_spec``).
        envelope: Optional ``vary.*`` envelope from
            :func:`signature.compute_envelope`. ``None`` → gate skipped.
        retry_cap: Max attempts per variant when the gate is active.
        gate_enabled: Master feature flag. ``False`` (default) → byte-identical
            pre-Stage-6.5 path, delegating to
            ``compose_sprite(sample_variant(spec, i))`` unchanged.
        variant_paths: Optional list of output paths keyed by variant index.
            When present AND gate exhausts retries AND the path is provided
            for that index, TECH-727 sidecar ``<variant>.needs_review.json``
            is written next to the variant. ``None`` or missing entry → no
            sidecar (caller owns diagnostics via the yielded tuple).

    Yields:
        One :class:`PIL.Image.Image` per variant index in
        ``spec["variants"]["count"]`` (or 1 when ``variants`` absent).

        On exhaustion (``retry_cap`` attempts all below ``_FLOOR``): yields
        the best-scoring image (+ TECH-727 sidecar written if
        ``variant_paths[i]`` supplied) OR, when no path is known, a
        ``(image, diagnostics)`` tuple for caller-side diagnostics.
    """
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
            # Exhausted — TECH-727 sidecar if caller supplied a target path.
            # `attempts` captures every seed tried (full trajectory), not just
            # the prefix up to the best-scoring attempt.
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
