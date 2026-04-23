"""Stage 6.6 preset system — loader, merge rule, and determinism lock.

TECH-735 — five locked behaviours:
    (a) author field wins merge
    (b) author `vary.padding` doesn't erase preset `vary.roof`
    (c) author `vary: null` raises
    (d) preset-referenced-twice with same seed → byte-identical render
    (e) missing preset → SpecError whose message lists every valid preset name
"""

from __future__ import annotations

import hashlib
from io import BytesIO

import pytest

from src.spec import SpecError, _load_preset, load_spec_from_dict
from src.compose import compose_sprite


# ---------------------------------------------------------------------------
# (a) author field wins merge
# ---------------------------------------------------------------------------


def test_author_field_wins() -> None:
    spec = load_spec_from_dict(
        {
            "preset": "suburban_house_with_yard",
            "id": "t01",
            "output": {"name": "t01.png"},
        }
    )
    # author's id / output.name win over the preset's defaults
    assert spec["id"] == "t01"
    assert spec["output"]["name"] == "t01.png"
    # preset's class + palette survive (no author override)
    assert spec["class"] == "residential_small"
    assert spec["palette"] == "residential"


# ---------------------------------------------------------------------------
# (b) author `vary.padding` preserves preset `vary.roof`
# ---------------------------------------------------------------------------


def test_author_vary_padding_preserves_preset_vary_roof() -> None:
    spec = load_spec_from_dict(
        {
            "preset": "suburban_house_with_yard",
            "id": "t02",
            "output": {"name": "t02.png"},
            "vary": {"padding": {"values": [0, 1]}},
        }
    )
    # preset axes survive unless author explicitly overrides the axis
    assert "roof" in spec["vary"]
    assert "facade" in spec["vary"]
    # author's new axis is unioned in
    assert "padding" in spec["vary"]
    assert spec["vary"]["padding"]["values"] == [0, 1]


def test_author_vary_axis_override_replaces_preset_axis() -> None:
    # per-axis replace: author `vary.roof` wins over preset `vary.roof`
    spec = load_spec_from_dict(
        {
            "preset": "suburban_house_with_yard",
            "id": "t03",
            "output": {"name": "t03.png"},
            "vary": {"roof": {"material": {"values": ["roof_tile_slate"]}}},
        }
    )
    assert spec["vary"]["roof"]["material"]["values"] == ["roof_tile_slate"]
    # other preset axes untouched
    assert "facade" in spec["vary"]


# ---------------------------------------------------------------------------
# (c) author `vary: null` / `vary: {}` raises
# ---------------------------------------------------------------------------


def test_author_vary_null_raises() -> None:
    with pytest.raises(SpecError):
        load_spec_from_dict(
            {
                "preset": "suburban_house_with_yard",
                "id": "t04",
                "output": {"name": "t04.png"},
                "vary": None,
            }
        )


def test_author_vary_empty_dict_raises() -> None:
    with pytest.raises(SpecError):
        load_spec_from_dict(
            {
                "preset": "suburban_house_with_yard",
                "id": "t05",
                "output": {"name": "t05.png"},
                "vary": {},
            }
        )


# ---------------------------------------------------------------------------
# (d) preset referenced twice with same seed → byte-identical render
# ---------------------------------------------------------------------------


def _sha256_png(img) -> str:
    buf = BytesIO()
    img.save(buf, format="PNG")
    return hashlib.sha256(buf.getvalue()).hexdigest()


def test_preset_twice_same_seed_deterministic() -> None:
    # SHA256 byte-equality per TECH-735 acceptance — not perceptual diff.
    # Seeds flow through spec (palette_seed / geometry_seed fanned from `seed`);
    # composer reads them directly, so two loads with the same author-supplied
    # seed must yield byte-identical PNGs.
    payload = {
        "preset": "suburban_house_with_yard",
        "id": "det01",
        "output": {"name": "det01.png"},
        "seed": 42,
    }
    spec_a = load_spec_from_dict(dict(payload))
    spec_b = load_spec_from_dict(dict(payload))
    img_a = compose_sprite(spec_a)
    img_b = compose_sprite(spec_b)
    assert _sha256_png(img_a) == _sha256_png(img_b)


# ---------------------------------------------------------------------------
# (e) missing preset raises with every valid preset name in the message
# ---------------------------------------------------------------------------


def test_missing_preset_raises_with_valid_list() -> None:
    with pytest.raises(SpecError) as excinfo:
        load_spec_from_dict({"preset": "ghost"})
    msg = str(excinfo.value)
    for name in (
        "suburban_house_with_yard",
        "strip_mall_with_parking",
        "row_houses_3x",
    ):
        assert name in msg, f"expected preset name {name!r} in error msg: {msg}"


# ---------------------------------------------------------------------------
# Structural checks per seeded preset (TECH-735 acceptance — each preset
# referenced at least once).
# ---------------------------------------------------------------------------


def test_strip_mall_preset_shape() -> None:
    p = _load_preset("strip_mall_with_parking")
    assert "pavement" in (p["ground"].get("materials") or [])
    assert "pavement_stripe_yellow" in p["ground"]["materials"]
    assert {"facade", "ground"} <= set(p["vary"].keys())


def test_row_houses_preset_uses_tiled_slot() -> None:
    p = _load_preset("row_houses_3x")
    assert p["buildings"][0]["slot"] == "tiled-row-3"
    for axis in ("facade", "roof"):
        assert p["vary"][axis].get("strategy") == "per_tile"
