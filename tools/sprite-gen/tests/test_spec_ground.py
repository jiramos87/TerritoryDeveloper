"""TECH-715 — ground schema normalization tests.

Covers:
    - string form normalises to {material, materials, hue_jitter, value_jitter, texture}
    - object form round-trips with missing subkeys defaulted to None
    - ``materials: [...]`` pool accepted; ``material`` stays None
    - supplying both ``material`` and ``materials`` raises SpecValidationError
    - ``ground: none`` sentinel + absent/None remain unchanged
"""

from __future__ import annotations

from pathlib import Path

import pytest
import yaml

from src.spec import SpecValidationError, load_spec


def _base_spec() -> dict:
    return {
        "id": "t715",
        "class": "residential_small",
        "footprint": [1, 1],
        "terrain": "flat",
        "composition": [{"type": "iso_cube", "material": "wall_brick_red", "w": 1, "d": 1, "h": 16}],
        "palette": "residential",
        "output": {"path": "out.png"},
    }


def _write(tmp_path: Path, data: dict) -> Path:
    p = tmp_path / "spec.yaml"
    p.write_text(yaml.safe_dump(data), encoding="utf-8")
    return p


def test_ground_string_form_normalises(tmp_path: Path) -> None:
    data = _base_spec()
    data["ground"] = "grass_flat"
    spec = load_spec(_write(tmp_path, data))
    assert spec["ground"] == {
        "material": "grass_flat",
        "materials": None,
        "hue_jitter": None,
        "value_jitter": None,
        "texture": None,
        "passthrough": False,  # TECH-745 default
    }


def test_ground_object_form_defaults_filled(tmp_path: Path) -> None:
    data = _base_spec()
    data["ground"] = {"material": "grass_flat", "hue_jitter": {"min": -5, "max": 5}}
    spec = load_spec(_write(tmp_path, data))
    assert spec["ground"]["material"] == "grass_flat"
    assert spec["ground"]["hue_jitter"] == {"min": -5, "max": 5}
    assert spec["ground"]["materials"] is None
    assert spec["ground"]["value_jitter"] is None
    assert spec["ground"]["texture"] is None


def test_ground_materials_pool_accepted(tmp_path: Path) -> None:
    data = _base_spec()
    data["ground"] = {"materials": ["grass_flat", "pavement"]}
    spec = load_spec(_write(tmp_path, data))
    assert spec["ground"]["materials"] == ["grass_flat", "pavement"]
    assert spec["ground"]["material"] is None


def test_ground_material_and_materials_raises(tmp_path: Path) -> None:
    data = _base_spec()
    data["ground"] = {"material": "grass_flat", "materials": ["pavement"]}
    with pytest.raises(SpecValidationError):
        load_spec(_write(tmp_path, data))


def test_ground_empty_materials_raises(tmp_path: Path) -> None:
    data = _base_spec()
    data["ground"] = {"materials": []}
    with pytest.raises(SpecValidationError):
        load_spec(_write(tmp_path, data))


def test_ground_unknown_key_raises(tmp_path: Path) -> None:
    data = _base_spec()
    data["ground"] = {"material": "grass_flat", "bogus": True}
    with pytest.raises(SpecValidationError):
        load_spec(_write(tmp_path, data))


def test_ground_none_sentinel_stays_string(tmp_path: Path) -> None:
    data = _base_spec()
    data["ground"] = "none"
    spec = load_spec(_write(tmp_path, data))
    assert spec["ground"] == "none"


def test_ground_absent_untouched(tmp_path: Path) -> None:
    data = _base_spec()
    spec = load_spec(_write(tmp_path, data))
    assert "ground" not in spec or spec.get("ground") in (None,)
