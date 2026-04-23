"""Stage 6.7 animation reservation — spec loader + composer guard contract.

TECH-739 — four locked behaviours:
    (a) reserved `output.animation` block with `enabled: false` parses clean;
        sibling keys (`frames`, `fps`, `loop`, `phase_offset`, `layers`) pass
        through verbatim.
    (b) `output.animation.enabled: true` raises SpecError referencing DAS §12.
    (c) composition entry with `animate: none` renders normally — primitive
        never sees the `animate` kwarg (stripped by `_check_animate`).
    (d) composition entry with any other `animate:` value raises
        NotImplementedError referencing DAS §12.
"""

from __future__ import annotations

import pytest

from src.compose import compose_sprite
from src.spec import ANIMATION_RESERVED_KEYS, SpecError, load_spec_from_dict


# ---------------------------------------------------------------------------
# Minimal-valid spec factory
# ---------------------------------------------------------------------------


def _base_spec(**overrides: object) -> dict:
    """Return a minimal valid spec dict; callers layer animation / animate keys."""
    spec: dict = {
        "id": "t01",
        "class": "residential_small",
        "footprint": [1, 1],
        "terrain": "flat",
        "palette": "residential",
        "composition": [
            {"type": "iso_cube", "w": 1, "d": 1, "h": 16, "material": "wall_brick_red"},
        ],
        "output": {"name": "t01.png"},
    }
    spec.update(overrides)
    return spec


# ---------------------------------------------------------------------------
# (a) reserved block parses clean; siblings preserved
# ---------------------------------------------------------------------------


def test_reserved_block_parses() -> None:
    spec = load_spec_from_dict(
        _base_spec(
            output={
                "name": "t01.png",
                "animation": {
                    "enabled": False,
                    "frames": 4,
                    "fps": 8,
                    "loop": True,
                    "phase_offset": 0,
                    "layers": ["smoke"],
                },
            }
        )
    )
    assert spec["output"]["animation"]["enabled"] is False
    # siblings preserved verbatim (not interpreted in v1)
    for key in ("frames", "fps", "loop", "phase_offset", "layers"):
        assert key in spec["output"]["animation"], f"reserved sibling {key!r} dropped"
    assert spec["output"]["animation"]["frames"] == 4
    assert spec["output"]["animation"]["layers"] == ["smoke"]


def test_reserved_keys_constant_covers_siblings() -> None:
    # lock the public constant — consumers of the spec module may rely on it.
    assert ANIMATION_RESERVED_KEYS == frozenset(
        {"frames", "fps", "loop", "phase_offset", "layers"}
    )


# ---------------------------------------------------------------------------
# (b) enabled: true raises SpecError referencing DAS §12
# ---------------------------------------------------------------------------


def test_enabled_true_raises() -> None:
    with pytest.raises(SpecError, match=r"DAS §12"):
        load_spec_from_dict(
            _base_spec(
                output={
                    "name": "t01.png",
                    "animation": {"enabled": True},
                }
            )
        )


def test_unknown_animation_sibling_raises() -> None:
    with pytest.raises(SpecError, match=r"DAS §12"):
        load_spec_from_dict(
            _base_spec(
                output={
                    "name": "t01.png",
                    "animation": {"enabled": False, "speed": 2},
                }
            )
        )


# ---------------------------------------------------------------------------
# (c) animate: none renders normally; primitive never sees the kwarg
# ---------------------------------------------------------------------------


def test_animate_none_renders() -> None:
    spec = load_spec_from_dict(
        _base_spec(
            composition=[
                {
                    "type": "iso_cube",
                    "w": 1,
                    "d": 1,
                    "h": 16,
                    "material": "wall_brick_red",
                    "animate": "none",
                },
            ]
        )
    )
    img = compose_sprite(spec)
    # sanity: image exists + has non-trivial size (render actually happened)
    assert img is not None
    assert img.width > 0 and img.height > 0


# ---------------------------------------------------------------------------
# (d) animate: <other> raises NotImplementedError referencing DAS §12
# ---------------------------------------------------------------------------


def test_animate_value_raises() -> None:
    spec = load_spec_from_dict(
        _base_spec(
            composition=[
                {
                    "type": "iso_cube",
                    "w": 1,
                    "d": 1,
                    "h": 16,
                    "material": "wall_brick_red",
                    "animate": "flicker",
                },
            ]
        )
    )
    with pytest.raises(NotImplementedError, match=r"DAS §12"):
        compose_sprite(spec)


def test_animate_raise_msg_quotes_value() -> None:
    spec = load_spec_from_dict(
        _base_spec(
            composition=[
                {
                    "type": "iso_cube",
                    "w": 1,
                    "d": 1,
                    "h": 16,
                    "material": "wall_brick_red",
                    "animate": "pulse",
                },
            ]
        )
    )
    with pytest.raises(NotImplementedError, match=r"'pulse'"):
        compose_sprite(spec)
