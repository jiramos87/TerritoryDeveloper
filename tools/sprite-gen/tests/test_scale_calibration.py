"""Scale-calibration regression for building_residential_small vs House1-64 (Stage 6 T6.7)."""

from __future__ import annotations

import colorsys
import math
import os
from collections import Counter
from pathlib import Path

import pytest
from PIL import Image

from src.compose import compose_sprite
from src.spec import load_spec

REPO_ROOT = Path(__file__).resolve().parents[3]
assert (REPO_ROOT / ".git").is_dir(), f"test misplaced; REPO_ROOT={REPO_ROOT}"
REFERENCE = REPO_ROOT / "Assets/Sprites/Residential/House1-64.png"
SPEC_PATH = REPO_ROOT / "tools/sprite-gen" / "specs" / "building_residential_small.yaml"

CI = os.environ.get("CI", "").lower() in ("1", "true", "yes")


def _hsv_distance(rgb_a: tuple[int, int, int], rgb_b: tuple[int, int, int]) -> float:
    ha, sa, va = colorsys.rgb_to_hsv(*[c / 255.0 for c in rgb_a])
    hb, sb, vb = colorsys.rgb_to_hsv(*[c / 255.0 for c in rgb_b])
    dh, ds, dv = ha - hb, sa - sb, va - vb
    return math.sqrt(dh * dh + ds * ds + dv * dv) * 100.0 / math.sqrt(3.0)


def _dominant_rgb_in_band(img: Image.Image, y0: int, y1: int) -> tuple[int, int, int] | None:
    px_list: list[tuple[int, int, int]] = []
    w, h = img.size
    for y in range(max(0, y0), min(h, y1)):
        for x in range(w):
            p = img.getpixel((x, y))
            if p[3] > 0:
                px_list.append(p[:3])
    if not px_list:
        return None
    return Counter(px_list).most_common(1)[0][0]


@pytest.fixture(scope="module")
def rendered() -> Image.Image:
    spec = load_spec(SPEC_PATH)
    return compose_sprite(spec)


def test_residential_small_bbox_y1_diamond_bottom(rendered: Image.Image) -> None:
    """DAS §2.3: House1-64 content bbox bottom = 48 (diamond bottom invariant)."""
    box = rendered.getbbox()
    assert box is not None
    _x0, _y0, _x1, y1 = box
    assert y1 == 48, f"y1={y1} != 48 (DAS §2.3 diamond-bottom invariant)"


def test_residential_small_bbox_content_h_envelope(rendered: Image.Image) -> None:
    """DAS §2.3: House1-64 content_h = 35 ± 2 (covers variant permutation)."""
    box = rendered.getbbox()
    assert box is not None
    _x0, y0, _x1, y1 = box
    content_h = y1 - y0
    assert 32 <= content_h <= 36, f"content_h={content_h} outside [32, 36]"


def test_residential_small_bbox_spans_width(rendered: Image.Image) -> None:
    x0, _y0, x1, _y1 = rendered.getbbox()
    assert x0 == 0 and x1 == 64


def test_residential_small_alpha_bbox_height_sane(rendered: Image.Image) -> None:
    """Tight 35px house silhouette is the long-term target; this guards gross scale."""
    _x0, y0, x1, y1 = rendered.getbbox()
    h = y1 - y0
    # Full-stack alpha (ground + building) is taller than 35 until decoration trim;
    # block obvious 3× / collapsed bugs.
    assert 28 <= h <= 56, f"bbox height {h} outside saneness envelope"


@pytest.mark.skipif(
    (not REFERENCE.is_file() or REFERENCE.stat().st_size == 0) and not CI,
    reason="House1-64.png not present locally",
)
def test_top_band_color_reasonably_close_to_house1(rendered: Image.Image) -> None:
    if not REFERENCE.is_file() or REFERENCE.stat().st_size == 0:
        if CI:
            pytest.fail("CI requires Assets/Sprites/Residential/House1-64.png")
        pytest.skip("reference missing")
    ref = Image.open(REFERENCE).convert("RGBA")
    rbox = ref.getbbox()
    assert rbox is not None
    _rx0, ry0, _rx1, ry1 = rbox
    rh = ry1 - ry0
    ref_top = _dominant_rgb_in_band(ref, ry0, ry0 + int(0.2 * max(rh, 1)))
    box = rendered.getbbox()
    assert box is not None
    _x0, y0, _x1, y1 = box
    h = y1 - y0
    ren_top = _dominant_rgb_in_band(
        rendered, y0, y0 + int(0.2 * max(h, 1))
    )
    assert ref_top is not None and ren_top is not None
    d = _hsv_distance(ren_top, ref_top)
    assert d <= 25, f"top-band HSV distance {d} vs reference (loose cap 25 for Stage 6)"


def test_ground_row_grass_tint_visible(rendered: Image.Image) -> None:
    # Top band of the ground plate (DAS C1 #68a838)
    p = rendered.getpixel((31, 16))
    assert p[3] > 0, "expected ground grass"
    g = (104, 168, 56)
    d = math.sqrt(sum((a - b) ** 2 for a, b in zip(p[:3], g)))
    assert d <= 50, f"ground pixel {p[:3]} too far from grass green"
