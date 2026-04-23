"""Geometric regression for iso_ground_diamond (Stage 6 T6.6, DAS §2.1 / R3 / R7)."""

from __future__ import annotations

from pathlib import Path

import pytest
from PIL import Image

from src.palette import apply_ramp, load_palette
from src.primitives.iso_ground_diamond import MATERIALS, iso_ground_diamond

_TOOL_ROOT = Path(__file__).parent.parent
PALETTE = load_palette("residential", palettes_dir=_TOOL_ROOT / "palettes")


def _render(fx: int, fy: int, material: str = "grass_flat") -> Image.Image:
    w = (fx + fy) * 32
    canvas = Image.new("RGBA", (w, w), (0, 0, 0, 0))
    iso_ground_diamond(
        canvas=canvas,
        x0=w // 2,
        y0=w,
        fx=fx,
        fy=fy,
        material=material,
        palette=PALETTE,
    )
    return canvas


@pytest.mark.parametrize("fx,fy,expected", [
    (1, 1, (0, 15, 64, 48)),
    (2, 2, (0, 31, 128, 96)),
    (3, 3, (0, 47, 192, 144)),
])
def test_ground_diamond_bbox(fx, fy, expected):
    assert _render(fx, fy).getbbox() == expected


@pytest.mark.parametrize("material", MATERIALS)
def test_ground_diamond_materials_non_empty(material: str) -> None:
    img = _render(1, 1, material=material)
    assert img.getbbox() is not None, f"empty render for {material}"


def test_ground_diamond_rim_is_one_pixel() -> None:
    img = _render(1, 1, "grass_flat")
    dark = apply_ramp(PALETTE, "grass_flat", "east")
    bright = apply_ramp(PALETTE, "grass_flat", "top")
    assert img.getpixel((32, 15))[:3] == dark
    assert img.getpixel((32, 17))[:3] == bright


def test_ground_diamond_rejects_unknown_material() -> None:
    with pytest.raises(ValueError, match="unknown material"):
        _render(1, 1, material="concrete_lava")
