"""TECH-747 — Cross-tile passthrough tests (Stage 7 addendum).

Locks the Stage 7 addendum passthrough contract:
    - TECH-745: `ground.passthrough` is a strict bool (default False) under
      the ground object schema; non-bool raises `SpecValidationError`.
    - TECH-746: `passthrough=true` inhibits `iso_ground_noise` entirely and
      clamps ground jitter so neighbour tiles blend seamlessly.

Design notes (do not silently relax on future failure):
    - `test_default_byte_identical` asserts bytes-equality between the two
      spec forms that MUST render identically when passthrough is off.
      Any composer change that drifts the default path will flip this test.
    - `test_passthrough_skips_noise` asserts a bounded, non-zero byte-diff
      so future jitter/noise reshuffles can't silently collapse the gap.
    - `test_passthrough_clamps_jitter` asserts byte-equality between an
      author-supplied `hue_jitter: ±10` passthrough render and the zero-
      jitter baseline — the clamp must collapse `±10` to ≤`±0.01`, which
      rounds to identity through `_jitter_ground_palette`'s no-op path.
"""

from __future__ import annotations

import pytest
from PIL import Image

from src.compose import compose_sprite
from src.spec import SpecValidationError, load_spec_from_dict


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _base_spec(**overrides) -> dict:
    spec: dict = {
        "id": "t745",
        "class": "residential_small",
        "footprint": [1, 1],
        "terrain": "flat",
        "ground": "grass_flat",
        "palette": "residential",
        "palette_seed": 100,
        "composition": [
            {"type": "iso_cube", "material": "wall_brick_red", "w": 1, "d": 1, "h": 16}
        ],
        "output": {"path": "out.png"},
    }
    spec.update(overrides)
    return spec


def _ground_only_spec(**overrides) -> dict:
    """Spec with empty composition — renders ground diamond only for clean byte compare."""
    return _base_spec(composition=[], **overrides)


def _render_bytes(spec: dict) -> bytes:
    img: Image.Image = compose_sprite(spec)
    return img.tobytes()


# ---------------------------------------------------------------------------
# TECH-745 — schema flag
# ---------------------------------------------------------------------------


def test_passthrough_flag_parses() -> None:
    """`ground.passthrough: true` lands in the resolved spec as `True`."""
    spec = load_spec_from_dict(
        _base_spec(ground={"material": "grass_flat", "passthrough": True})
    )
    assert spec["ground"]["passthrough"] is True


def test_passthrough_default_false() -> None:
    """Object form without `passthrough` key materialises default `False`."""
    spec = load_spec_from_dict(_base_spec(ground={"material": "grass_flat"}))
    assert spec["ground"]["passthrough"] is False


def test_passthrough_string_form_default_false() -> None:
    """Legacy string form normalises to `passthrough: False`."""
    spec = load_spec_from_dict(_base_spec(ground="grass_flat"))
    assert spec["ground"]["passthrough"] is False


@pytest.mark.parametrize(
    "bogus",
    ["yes", 1, 0, None, "true", 1.0],
    ids=["str_yes", "int_1", "int_0", "none", "str_true", "float"],
)
def test_passthrough_non_bool_raises(bogus: object) -> None:
    """Non-bool `passthrough` raises `SpecValidationError`."""
    with pytest.raises(SpecValidationError):
        load_spec_from_dict(
            _base_spec(ground={"material": "grass_flat", "passthrough": bogus})
        )


# ---------------------------------------------------------------------------
# TECH-746 — composer inhibit + clamp
# ---------------------------------------------------------------------------


def test_default_byte_identical() -> None:
    """`passthrough=False` (explicit or default) renders identically to legacy string form."""
    legacy = _render_bytes(_base_spec(ground="grass_flat"))
    default_obj = _render_bytes(
        _base_spec(
            ground={
                "material": "grass_flat",
                "materials": None,
                "hue_jitter": None,
                "value_jitter": None,
                "texture": None,
                "passthrough": False,
            }
        )
    )
    assert legacy == default_obj


def test_passthrough_skips_noise() -> None:
    """Enabling passthrough on a spec with texture density must change the rendered bytes.

    The non-passthrough variant triggers `iso_ground_noise`; the passthrough
    variant inhibits it. Therefore the render must differ. Difference is
    bounded: we only expect noise-speckle pixels to change (well under half
    the canvas) so a runaway composer change would blow past the ceiling.
    """
    textured = _ground_only_spec(
        ground={
            "material": "grass_flat",
            "materials": None,
            "hue_jitter": None,
            "value_jitter": None,
            "texture": {"density": 0.15},
            "passthrough": False,
        }
    )
    passthrough = _ground_only_spec(
        ground={
            "material": "grass_flat",
            "materials": None,
            "hue_jitter": None,
            "value_jitter": None,
            "texture": {"density": 0.15},
            "passthrough": True,
        }
    )
    a = _render_bytes(textured)
    b = _render_bytes(passthrough)
    assert len(a) == len(b), "canvas size must not change"
    diff = sum(1 for x, y in zip(a, b) if x != y)
    assert diff > 0, "expected noise-pixel divergence between passthrough on/off"
    # Upper bound: noise only paints speckle, never majority of canvas bytes.
    assert diff < len(a) // 2, f"diff {diff} > ceiling {len(a)//2}"


def test_passthrough_clamps_jitter() -> None:
    """Author-supplied wide `hue_jitter` on a passthrough tile collapses to identity.

    Both renders produced below should be byte-identical to the zero-jitter
    baseline because the composer clamps the jitter band to ±0.01 (degrees),
    which rounds to the identity path in `_jittered_ramp`.
    """
    zero_baseline = _render_bytes(
        _ground_only_spec(
            ground={
                "material": "grass_flat",
                "materials": None,
                "hue_jitter": {"min": 0, "max": 0},
                "value_jitter": None,
                "texture": None,
                "passthrough": True,
            }
        )
    )
    wide_clamped = _render_bytes(
        _ground_only_spec(
            ground={
                "material": "grass_flat",
                "materials": None,
                "hue_jitter": {"min": -10, "max": 10},
                "value_jitter": {"min": -5, "max": 5},
                "texture": None,
                "passthrough": True,
            }
        )
    )
    assert zero_baseline == wide_clamped


def test_passthrough_no_clamp_when_off() -> None:
    """Sanity: when `passthrough=False`, wide jitter still drives divergence.

    Guards against the clamp running unconditionally (regression on the
    `if g_passthrough:` branch in compose.py).
    """
    zero = _render_bytes(
        _ground_only_spec(
            ground={
                "material": "grass_flat",
                "materials": None,
                "hue_jitter": {"min": 0, "max": 0},
                "value_jitter": None,
                "texture": None,
                "passthrough": False,
            }
        )
    )
    wide = _render_bytes(
        _ground_only_spec(
            ground={
                "material": "grass_flat",
                "materials": None,
                "hue_jitter": {"min": -20, "max": 20},
                "value_jitter": None,
                "texture": None,
                "passthrough": False,
            }
        )
    )
    assert zero != wide, "wide hue jitter must still render differently when passthrough is off"
