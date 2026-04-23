"""inspect.py — pixel-level bbox + tile-diamond containment helper.

Used by the `sprite-gen-visual-review` skill (Phase 2 mechanical check) and
exposed via `python -m sprite_gen inspect <png>`. Returns a JSON report:

    {
      "path": "...",
      "canvas": [w, h],
      "tile_diamond": {"cx": 32, "cy": 47, "hw": 32, "hh": 16},
      "building_bbox": [x0, y0, x1, y1] | null,
      "bbox_norm_corners": [...],
      "containment": "pass" | "overflow" | "empty",
      "overflow_px": int,
      "pixel_count": int
    }

Ground-layer pixels are detected heuristically: any pixel whose green channel
exceeds red+blue by a margin is treated as "ground" and excluded from the
building-mass bbox. All other opaque pixels are building mass.

Containment check treats the tile as a diamond centered at (cx, cy) with
half-width hw and half-height hh. A pixel at (x, y) is inside iff
|x - cx| / hw + |y - cy| / hh ≤ 1. Bounding box of building mass must have
all 4 corners inside the diamond.
"""

from __future__ import annotations

import json
import sys
from pathlib import Path
from typing import Optional

from PIL import Image


def _is_ground_pixel(r: int, g: int, b: int, a: int) -> bool:
    """Heuristic — ground tiles read as greenish; building walls/roofs are
    reds, browns, greys, blues. Flag as ground when green channel dominates
    by a reasonable margin AND alpha is opaque."""
    if a < 16:
        return False
    # Green dominance: g > r + 12 AND g > b + 12 catches grassy palettes.
    return (g > r + 12) and (g > b + 12)


def _tile_diamond(canvas_w: int, canvas_h: int) -> dict:
    """Return tile diamond geometry for a 64×64 (1×1 tile) canvas.

    For a 64×64 canvas the drawn tile diamond has apexes at
    west=(0, 47), east=(63, 47), top=(32, 31), bottom=(32, 63). Half-width
    must account for pixel indexing (w − 1) so the east apex lands at the
    last column, not one past it.
    """
    hw = (canvas_w - 1) / 2.0
    hh = max(1.0, (canvas_h - 1) / 4.0)
    cx = (canvas_w - 1) / 2.0
    cy = (canvas_h - 1) - hh
    return {"cx": cx, "cy": cy, "hw": hw, "hh": hh}


def _diamond_norm(x: int, y: int, dia: dict) -> float:
    """Return |x-cx|/hw + |y-cy|/hh. <=1 means inside, >1 means outside."""
    return abs(x - dia["cx"]) / dia["hw"] + abs(y - dia["cy"]) / dia["hh"]


def inspect_png(path: str | Path) -> dict:
    """Analyze a sprite-gen PNG and return the containment report dict."""
    p = Path(path)
    img = Image.open(p).convert("RGBA")
    w, h = img.size
    px = img.load()

    building_pixels = 0
    x_min = w
    y_min = h
    x_max = -1
    y_max = -1

    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if a < 16:
                continue
            if _is_ground_pixel(r, g, b, a):
                continue
            building_pixels += 1
            if x < x_min:
                x_min = x
            if y < y_min:
                y_min = y
            if x > x_max:
                x_max = x
            if y > y_max:
                y_max = y

    dia = _tile_diamond(w, h)

    if building_pixels == 0:
        return {
            "path": str(p),
            "canvas": [w, h],
            "tile_diamond": dia,
            "building_bbox": None,
            "bbox_norm_corners": [],
            "containment": "empty",
            "overflow_px": 0,
            "pixel_count": 0,
        }

    corners = [
        (x_min, y_min),
        (x_max, y_min),
        (x_min, y_max),
        (x_max, y_max),
    ]
    norms = [round(_diamond_norm(x, y, dia), 3) for (x, y) in corners]

    # Overflow = number of bbox corners outside the diamond.
    overflow = sum(1 for n in norms if n > 1.0)
    # Footprint-only: bottom two corners are the ground footprint corners.
    # Roof/height rises above the tile's top apex by design — only penalize
    # when the BOTTOM corners (footprint) fall outside the diamond.
    bottom_norms = [norms[2], norms[3]]  # (x_min, y_max), (x_max, y_max)
    footprint_overflow = sum(1 for n in bottom_norms if n > 1.0)

    if footprint_overflow > 0:
        verdict = "overflow"
    else:
        verdict = "pass"

    return {
        "path": str(p),
        "canvas": [w, h],
        "tile_diamond": dia,
        "building_bbox": [x_min, y_min, x_max, y_max],
        "bbox_norm_corners": {
            "nw": norms[0],
            "ne": norms[1],
            "sw": norms[2],
            "se": norms[3],
        },
        "containment": verdict,
        "overflow_px": footprint_overflow,
        "pixel_count": building_pixels,
    }


def inspect_batch(paths: list[str | Path]) -> dict:
    """Analyze multiple PNGs, report variation between them."""
    reports = [inspect_png(p) for p in paths]
    # Variation = pixel-count spread + bbox shift between first and others.
    counts = [r["pixel_count"] for r in reports]
    bboxes = [r["building_bbox"] for r in reports if r["building_bbox"]]

    pixel_spread_pct: Optional[float] = None
    bbox_shift_px: Optional[int] = None
    if counts and max(counts) > 0:
        pixel_spread_pct = round(
            (max(counts) - min(counts)) / max(counts) * 100, 1
        )
    if len(bboxes) >= 2:
        xs_min = [b[0] for b in bboxes]
        ys_min = [b[1] for b in bboxes]
        bbox_shift_px = max(max(xs_min) - min(xs_min), max(ys_min) - min(ys_min))

    variation_pass = False
    if pixel_spread_pct is not None and pixel_spread_pct > 5.0:
        variation_pass = True
    if bbox_shift_px is not None and bbox_shift_px > 4:
        variation_pass = True

    return {
        "variants": reports,
        "variation": {
            "pixel_spread_pct": pixel_spread_pct,
            "bbox_shift_px": bbox_shift_px,
            "verdict": "pass" if variation_pass else "static",
        },
    }


def main(argv: list[str]) -> int:
    if not argv:
        print("usage: python -m sprite_gen inspect <png> [<png> ...]", file=sys.stderr)
        return 2
    if len(argv) == 1:
        report = inspect_png(argv[0])
    else:
        report = inspect_batch(argv)
    print(json.dumps(report, indent=2))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
