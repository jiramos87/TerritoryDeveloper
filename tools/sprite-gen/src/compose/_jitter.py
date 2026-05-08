"""_jitter.py — Ground ramp HSV jitter helpers (TECH-718)."""

from __future__ import annotations

import random as _random_mod
from colorsys import hsv_to_rgb, rgb_to_hsv


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
