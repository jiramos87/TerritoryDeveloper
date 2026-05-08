"""_variants.py — Variant sampling + vary-walk helpers (TECH-711 / TECH-720)."""

from __future__ import annotations

import copy
from random import Random


# Composition role keys that route `variants.vary.{role}.{field}` writes
# into matching `composition[*]` entries instead of a top-level key.
_COMPOSITION_ROLE_KEYS = frozenset({"wall", "roof", "foundation"})


def sample_variant(spec: dict, variant_idx: int) -> dict:
    """Return a deep copy of *spec* with `variants.vary` ranges sampled."""
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

    vary_ground = vary.get("ground")
    if vary_ground and isinstance(vary_ground, dict):
        palette_active = scope in ("palette", "palette+geometry")
        if palette_active:
            _apply_vary_ground(out, vary_ground, palette_rng)

    for path, leaf in _walk_vary(vary, ()):
        if path and path[0] == "ground":
            continue
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
    """Merge sampled ``vary.ground`` values into ``out["ground"]`` (TECH-720)."""
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
    """Yield `(path_tuple, leaf_dict)` for every terminal leaf under `vary:`."""
    if isinstance(node, dict):
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
    """Classify a `vary.` axis as palette or geometry by path root."""
    if not path:
        return "geometry"
    root = path[0]
    if root in ("palette", "material", "materials"):
        return "palette"
    last = path[-1]
    if any(last.startswith(p) for p in ("color", "hue", "value", "tint")):
        return "palette"
    if root in _COMPOSITION_ROLE_KEYS and last in ("material", "materials"):
        return "palette"
    return "geometry"


def _set_deep(target: dict, path: tuple, value) -> None:
    if path and path[0] in _COMPOSITION_ROLE_KEYS:
        role = path[0]
        composition = target.get("composition")
        if isinstance(composition, list) and len(path) >= 2:
            field_path = path[1:]
            applied = False
            for entry in composition:
                if isinstance(entry, dict) and entry.get("role") == role:
                    _set_deep(entry, field_path, value)
                    applied = True
            if applied:
                return

    cursor = target
    for key in path[:-1]:
        sub = cursor.get(key)
        if not isinstance(sub, dict):
            sub = {}
            cursor[key] = sub
        cursor = sub
    cursor[path[-1]] = value
