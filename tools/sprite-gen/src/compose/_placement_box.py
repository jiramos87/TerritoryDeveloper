"""_placement_box.py — Building box resolution + anchor helpers (TECH-711)."""

from __future__ import annotations

from ..spec import default_footprint_ratio_for_class


def resolve_building_box(spec: dict) -> tuple[int, int, int, int]:
    """Return `(bx, by, offset_x, offset_y)` for the building mass.

    Iso-tile-aware: `align` + `padding` translate into grid-space shifts
    clamped to keep the building footprint fully inside the tile diamond,
    then projected to screen pixels via the iso transform.

    Defaults (`align: center`, zero padding) return offsets `(0, 0)` so
    existing composer paths are byte-identical.
    """
    canvas_w, canvas_h = spec.get("canvas", [64, 64])
    footprint = spec.get("footprint", [1, 1])
    fx_tiles = max(1, int(footprint[0]))
    fy_tiles = max(1, int(footprint[1]))
    building = spec.get("building") or {}

    footprint_px = building.get("footprint_px")
    if isinstance(footprint_px, (list, tuple)) and len(footprint_px) == 2:
        bx, by = int(footprint_px[0]), int(footprint_px[1])
        wr = bx / max(1, canvas_w)
        dr = by / max(1, canvas_h)
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

    wr_eff = max(0.01, min(1.0, float(wr)))
    dr_eff = max(0.01, min(1.0, float(dr)))

    anchors = {
        "center": (0.5 + wr_eff / 2.0, 0.5 + dr_eff / 2.0),
        "ne":     (1.0, dr_eff),
        "nw":     (wr_eff, dr_eff),
        "se":     (1.0, 1.0),
        "sw":     (wr_eff, 1.0),
        "custom": (0.5 + wr_eff / 2.0, 0.5 + dr_eff / 2.0),
    }
    gx_a, gy_a = anchors.get(align, anchors["center"])

    pad_e = int(padding.get("e", 0))
    pad_w = int(padding.get("w", 0))
    pad_n = int(padding.get("n", 0))
    pad_s = int(padding.get("s", 0))
    gx_a += (pad_w - pad_e) / 64.0
    gy_a += (pad_n - pad_s) / 32.0

    inset = 0.28
    legal_gx_min = wr_eff + inset
    legal_gx_max = 1.0 - inset
    legal_gy_min = dr_eff + inset
    legal_gy_max = 1.0 - inset
    if legal_gx_min > legal_gx_max:
        legal_gx_min = legal_gx_max = (wr_eff + 1.0) / 2.0
    if legal_gy_min > legal_gy_max:
        legal_gy_min = legal_gy_max = (dr_eff + 1.0) / 2.0
    gx_a = max(legal_gx_min, min(legal_gx_max, gx_a))
    gy_a = max(legal_gy_min, min(legal_gy_max, gy_a))

    dgx = gx_a - (0.5 + wr_eff / 2.0)
    dgy = gy_a - (0.5 + dr_eff / 2.0)

    ox = int(round((dgx - dgy) * 16 * fx_tiles))
    oy = int(round((dgx + dgy) * 8 * fy_tiles))
    return bx, by, ox, oy


def _anchor_offset(
    align: str, bx: int, by: int, canvas_w: int, canvas_h: int
) -> tuple[int, int]:
    """Legacy axial anchor helper. Retained for any external callers."""
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
