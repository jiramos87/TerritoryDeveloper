"""TECH-721 — Ground variation tests.

Covers TECH-717 (iso_ground_noise), TECH-718 (composer jitter + auto-insert),
TECH-720 (vary.ground grammar).
"""

from __future__ import annotations

import pytest
from PIL import Image

from src.compose import compose_sprite, sample_variant
from src.palette import load_palette
from src.primitives.iso_ground_noise import _diamond_mask, iso_ground_noise


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _base_spec(**overrides) -> dict:
    spec: dict = {
        "class": "residential_small",
        "footprint": [1, 1],
        "terrain": "flat",
        "ground": "grass_flat",
        "palette": "residential",
        "palette_seed": 100,
        "composition": [{"type": "iso_cube", "material": "wall_brick_red", "w": 1, "d": 1, "h": 16}],
        "output": {"path": "out.png"},
    }
    spec.update(overrides)
    return spec


def _ground_band_bytes(img: Image.Image) -> bytes:
    """Pixel bytes from the bottom 20% of content bbox (ground band proxy)."""
    w, h = img.size
    box = img.getbbox() or (0, 0, w, h)
    _, y0, _, y1 = box
    content_h = max(1, y1 - y0)
    band_y0 = max(y0, y1 - int(0.2 * content_h))
    rows = [img.getpixel((x, y)) for y in range(band_y0, y1) for x in range(w)]
    return bytes(c for px in rows for c in (px if isinstance(px, tuple) else (px,)))


# ---------------------------------------------------------------------------
# TECH-718 — legacy string form byte-identical to object form with zero jitter
# ---------------------------------------------------------------------------


def test_legacy_string_byte_identical() -> None:
    """String-form ground renders byte-identical to object form with zero jitter."""
    str_img = compose_sprite(_base_spec(ground="grass_flat"))
    obj_img = compose_sprite(
        _base_spec(
            ground={
                "material": "grass_flat",
                "materials": None,
                "hue_jitter": {"min": 0, "max": 0},
                "value_jitter": {"min": 0, "max": 0},
                "texture": None,
            }
        )
    )
    assert list(str_img.getdata()) == list(obj_img.getdata())


def test_object_form_round_trip() -> None:
    """Object ground form with zero jitter equals string form (ground band only)."""
    str_img = compose_sprite(_base_spec(ground="grass_flat"))
    obj_img = compose_sprite(
        _base_spec(
            ground={
                "material": "grass_flat",
                "materials": None,
                "hue_jitter": None,
                "value_jitter": None,
                "texture": None,
            }
        )
    )
    assert _ground_band_bytes(str_img) == _ground_band_bytes(obj_img)


# ---------------------------------------------------------------------------
# TECH-720 — vary.ground.material pool
# ---------------------------------------------------------------------------


def test_materials_pool_variants() -> None:
    """4 variants with a 2-material pool → both materials appear."""
    spec = _base_spec(
        ground={
            "material": "grass_flat",
            "materials": None,
            "hue_jitter": None,
            "value_jitter": None,
            "texture": None,
        },
        variants={
            "count": 4,
            "vary": {
                "ground": {
                    "material": {"values": ["grass_flat", "pavement"]},
                }
            },
            "seed_scope": "palette",
        },
    )
    materials = {sample_variant(spec, i)["ground"]["material"] for i in range(4)}
    assert "grass_flat" in materials
    assert "pavement" in materials


# ---------------------------------------------------------------------------
# TECH-718 — non-zero jitter diverges; zero jitter stays identical
# ---------------------------------------------------------------------------


def _ground_only_spec(**overrides) -> dict:
    """Spec with empty composition — renders ground diamond only for clean comparison."""
    spec = _base_spec(composition=[], **overrides)
    return spec


def test_non_zero_jitter_diverges() -> None:
    """Ground-only renders differ across palette seeds when hue_jitter is non-zero."""
    def _render(seed: int) -> list:
        img = compose_sprite(
            _ground_only_spec(
                ground={
                    "material": "grass_flat",
                    "materials": None,
                    "hue_jitter": {"min": -10, "max": 10},
                    "value_jitter": None,
                    "texture": None,
                },
                palette_seed=seed,
            )
        )
        return list(img.getdata())

    renders = [_render(s) for s in (100, 200, 300, 400)]
    # With a ±10° hue range, expect at least two distinct renders over 4 seeds.
    unique = {tuple(r) for r in renders}
    assert len(unique) > 1


def test_zero_jitter_byte_identical() -> None:
    """Zero-range jitter → all renders identical (ground-only canvas)."""
    def _render(seed: int) -> list:
        return list(
            compose_sprite(
                _ground_only_spec(
                    ground={
                        "material": "grass_flat",
                        "materials": None,
                        "hue_jitter": {"min": 0, "max": 0},
                        "value_jitter": {"min": 0, "max": 0},
                        "texture": None,
                    },
                    palette_seed=seed,
                )
            ).getdata()
        )

    renders = [_render(100 + i) for i in range(4)]
    assert all(r == renders[0] for r in renders[1:])


# ---------------------------------------------------------------------------
# TECH-717 — noise mask confinement + density monotonicity
# ---------------------------------------------------------------------------


def test_noise_mask_and_density() -> None:
    """Zero pixels outside diamond; pixel count strictly monotonic with density."""
    palette = load_palette("residential")
    span = 1 + 1
    top_y = span * 8 - 1
    x0 = 64 // 2
    mask_set = set(_diamond_mask(x0, top_y, fx=1, fy=1))

    counts: dict[float, int] = {}
    for d in (0.0, 0.05, 0.15):
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        iso_ground_noise(
            img, x0, top_y,
            material="grass_flat",
            density=d,
            seed=42,
            palette=palette,
            fx=1,
            fy=1,
        )
        painted = [(x, y) for x in range(64) for y in range(64) if img.getpixel((x, y))[3] > 0]
        outside = [(x, y) for (x, y) in painted if (x, y) not in mask_set]
        assert outside == [], f"density={d}: {len(outside)} pixels outside diamond mask"
        counts[d] = len(painted)

    assert counts[0.0] < counts[0.05] < counts[0.15], (
        f"pixel counts not strictly monotonic: {counts}"
    )


def test_noise_density_clamp() -> None:
    """Density > 0.15 clamps to 0.15; outputs are pixel-identical."""
    palette = load_palette("residential")
    span = 1 + 1
    top_y = span * 8 - 1
    x0 = 64 // 2

    def _render(density: float) -> list:
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        iso_ground_noise(img, x0, top_y, material="grass_flat", density=density, seed=7, palette=palette)
        return list(img.getdata())

    assert _render(0.15) == _render(0.30)
    assert _render(0.15) == _render(1.0)


def test_noise_seed_determinism() -> None:
    """Same args → byte-identical output across calls."""
    palette = load_palette("residential")
    span = 1 + 1
    top_y = span * 8 - 1
    x0 = 64 // 2

    def _render() -> list:
        img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
        iso_ground_noise(img, x0, top_y, material="grass_flat", density=0.08, seed=99, palette=palette)
        return list(img.getdata())

    assert _render() == _render()


def test_noise_missing_accent_noop() -> None:
    """Material with both accents None → primitive is a no-op."""
    palette = load_palette("residential")
    # wall_brick_red has no accent keys → both None
    span = 1 + 1
    top_y = span * 8 - 1
    x0 = 64 // 2
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    iso_ground_noise(img, x0, top_y, material="wall_brick_red", density=0.15, seed=1, palette=palette)
    painted = [(x, y) for x in range(64) for y in range(64) if img.getpixel((x, y))[3] > 0]
    assert painted == [], "no pixels should be painted when both accents are None"


def test_noise_zero_density_noop() -> None:
    """density=0 → no pixels painted."""
    palette = load_palette("residential")
    span = 1 + 1
    top_y = span * 8 - 1
    x0 = 64 // 2
    img = Image.new("RGBA", (64, 64), (0, 0, 0, 0))
    iso_ground_noise(img, x0, top_y, material="grass_flat", density=0.0, seed=1, palette=palette)
    painted = [(x, y) for x in range(64) for y in range(64) if img.getpixel((x, y))[3] > 0]
    assert painted == [], "density=0 should paint no pixels"
