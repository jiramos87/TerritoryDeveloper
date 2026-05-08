"""_level_expand.py — Stage 6 level/wall expansion helper."""

from __future__ import annotations

from ..constants import DEFAULT_LEVEL_H, LEVEL_H


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
