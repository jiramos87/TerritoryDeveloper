"""Tests for tools/sprite-gen/src/spec.py."""

from __future__ import annotations

import copy
import textwrap
from pathlib import Path

import pytest
import yaml

from src.spec import (
    REQUIRED_KEYS,
    SpecValidationError,
    composition_entries,
    default_footprint_ratio_for_class,
    default_ground_for_class,
    load_spec,
)

FIXTURES = Path(__file__).parent / "fixtures"
VALID_YAML = FIXTURES / "spec_valid.yaml"
MALFORMED_YAML = FIXTURES / "spec_malformed.yaml"


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

def _minimal_spec() -> dict:
    """Return a minimal valid spec dict (all required keys, valid shapes)."""
    return {
        "id": "test_v1",
        "class": "residential",
        "footprint": [1, 1],
        "terrain": "flat",
        "composition": [{"type": "iso_cube", "w": 2, "d": 2, "h": 32}],
        "palette": "residential",
        "output": {"name": "test"},
    }


def _write_yaml(tmp_path: Path, data: dict, filename: str = "spec.yaml") -> Path:
    p = tmp_path / filename
    p.write_text(yaml.dump(data), encoding="utf-8")
    return p


# ---------------------------------------------------------------------------
# test_valid_spec_loads
# ---------------------------------------------------------------------------

def test_valid_spec_loads():
    result = load_spec(VALID_YAML)
    assert isinstance(result, dict)
    for key, _ in REQUIRED_KEYS:
        assert key in result, f"required key '{key}' missing from result"
    # optional fields round-trip
    assert "levels" in result
    assert "seed" in result
    assert "diffusion" in result
    assert "variants" in result


# ---------------------------------------------------------------------------
# test_missing_key_raises  (parametrized per REQUIRED_KEYS)
# ---------------------------------------------------------------------------

@pytest.mark.parametrize("key,_type", REQUIRED_KEYS)
def test_missing_key_raises(tmp_path, key, _type):
    spec = _minimal_spec()
    del spec[key]
    path = _write_yaml(tmp_path, spec)

    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(path)

    assert exc_info.value.field == key
    assert "missing required field" in str(exc_info.value)


# ---------------------------------------------------------------------------
# test_wrong_type_raises
# ---------------------------------------------------------------------------

def test_wrong_type_raises_footprint_as_string(tmp_path):
    spec = _minimal_spec()
    spec["footprint"] = "1x1"  # str instead of list
    path = _write_yaml(tmp_path, spec)

    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(path)

    assert exc_info.value.field == "footprint"


def test_wrong_type_raises_output_as_string(tmp_path):
    spec = _minimal_spec()
    spec["output"] = "should-be-dict"
    path = _write_yaml(tmp_path, spec)

    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(path)

    assert exc_info.value.field == "output"


# ---------------------------------------------------------------------------
# test_footprint_shape
# ---------------------------------------------------------------------------

@pytest.mark.parametrize("bad_footprint", [
    [1],           # too short
    [1, 2, 3],     # too long
    ["1", "1"],    # strings instead of ints
    [1.0, 2.0],    # floats instead of ints
])
def test_footprint_shape(tmp_path, bad_footprint):
    spec = _minimal_spec()
    spec["footprint"] = bad_footprint
    path = _write_yaml(tmp_path, spec)

    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(path)

    assert exc_info.value.field == "footprint"


# ---------------------------------------------------------------------------
# test_composition_elements
# ---------------------------------------------------------------------------

@pytest.mark.parametrize("bad_composition", [
    [],                                  # empty list
    ["not_a_dict", "also_not"],          # list of strings
    [{"no_type_key": "iso_cube"}],       # dict missing 'type'
])
def test_composition_elements(tmp_path, bad_composition):
    spec = _minimal_spec()
    spec["composition"] = bad_composition
    path = _write_yaml(tmp_path, spec)

    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(path)

    assert exc_info.value.field == "composition"


# ---------------------------------------------------------------------------
# test_optional_fields_passthrough
# ---------------------------------------------------------------------------

def test_optional_fields_passthrough(tmp_path):
    spec = _minimal_spec()
    spec["levels"] = 3
    spec["seed"] = 99
    spec["variants"] = 2
    spec["diffusion"] = {"enabled": False, "strength": 0.1}
    path = _write_yaml(tmp_path, spec)

    result = load_spec(path)

    assert result["levels"] == 3
    assert result["seed"] == 99
    # TECH-710: scalar `variants: N` normalises to object shape.
    assert result["variants"] == {"count": 2, "vary": {}, "seed_scope": "palette"}
    assert result["diffusion"] == {"enabled": False, "strength": 0.1}


def test_optional_fields_absent_ok(tmp_path):
    """Missing optional fields must not raise."""
    spec = _minimal_spec()
    # none of levels / seed / variants / diffusion present
    path = _write_yaml(tmp_path, spec)
    result = load_spec(path)
    assert "id" in result


# ---------------------------------------------------------------------------
# test_malformed_yaml_raises
# ---------------------------------------------------------------------------

def test_malformed_yaml_raises():
    with pytest.raises(yaml.YAMLError):
        load_spec(MALFORMED_YAML)


# ---------------------------------------------------------------------------
# test missing file → FileNotFoundError
# ---------------------------------------------------------------------------

def test_missing_file_raises(tmp_path):
    with pytest.raises(FileNotFoundError):
        load_spec(tmp_path / "nonexistent.yaml")


# ---------------------------------------------------------------------------
# test non-mapping root raises SpecValidationError
# ---------------------------------------------------------------------------

def test_non_mapping_root_raises(tmp_path):
    p = tmp_path / "list_root.yaml"
    p.write_text("- a\n- b\n", encoding="utf-8")
    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(p)
    assert exc_info.value.field == "<root>"


def test_default_ground_residential_small():
    assert default_ground_for_class("residential_small") == "grass_flat"


def test_default_ground_commercial_store():
    assert default_ground_for_class("commercial_store") == "pavement"


def test_default_footprint_ratio_residential_small():
    assert default_footprint_ratio_for_class("residential_small") == (0.45, 0.45)


def test_composition_entries_prefers_top_level():
    s = {
        "composition": [{"type": "iso_cube", "w": 1, "d": 1, "h": 8, "material": "x"}],
        "building": {"composition": [{"type": "iso_prism", "h": 1, "material": "y"}]},
    }
    assert len(composition_entries(s)) == 1


def test_load_spec_r11_building_composition(tmp_path):
    p = _write_yaml(
        tmp_path,
        {
            "id": "b_r_s",
            "class": "residential_small",
            "footprint": [1, 1],
            "terrain": "flat",
            "palette": "residential",
            "output": {"name": "b"},
            "building": {
                "footprint_ratio": [0.45, 0.45],
                "composition": [
                    {"type": "iso_cube", "role": "wall", "w": 1, "d": 1, "h_px": 8, "material": "m"}
                ],
            },
        },
    )
    d = load_spec(p)
    assert d["class"] == "residential_small"
    assert d["building"]["footprint_ratio"] == [0.45, 0.45]
    assert d["composition"][0]["type"] == "iso_cube"


# ---------------------------------------------------------------------------
# TECH-706 — include_in_signature per-spec override
# ---------------------------------------------------------------------------


def test_include_in_signature_defaults_true(tmp_path):
    p = _write_yaml(tmp_path, _minimal_spec())
    d = load_spec(p)
    assert d["include_in_signature"] is True


def test_include_in_signature_honours_false(tmp_path):
    spec = _minimal_spec()
    spec["include_in_signature"] = False
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["include_in_signature"] is False


def test_include_in_signature_honours_explicit_true(tmp_path):
    spec = _minimal_spec()
    spec["include_in_signature"] = True
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["include_in_signature"] is True


# ---------------------------------------------------------------------------
# TECH-709 — placement schema (building.footprint_px / padding / align)
# ---------------------------------------------------------------------------


def _spec_with_building(**building_fields) -> dict:
    spec = _minimal_spec()
    spec["building"] = {
        "composition": [{"type": "iso_cube", "w": 1, "d": 1, "h": 8, "material": "m"}],
        **building_fields,
    }
    return spec


def test_placement_defaults(tmp_path):
    """Spec without placement fields normalises to defaults."""
    spec = _spec_with_building()
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["building"]["padding"] == {"n": 0, "e": 0, "s": 0, "w": 0}
    assert d["building"]["align"] == "center"
    assert "footprint_px" not in d["building"]


def test_placement_explicit(tmp_path):
    spec = _spec_with_building(
        footprint_px=[28, 28],
        padding={"n": 1, "e": 2, "s": 3, "w": 4},
        align="sw",
    )
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["building"]["footprint_px"] == [28, 28]
    assert d["building"]["padding"] == {"n": 1, "e": 2, "s": 3, "w": 4}
    assert d["building"]["align"] == "sw"


def test_placement_padding_partial_fills_defaults(tmp_path):
    spec = _spec_with_building(padding={"s": 10})
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["building"]["padding"] == {"n": 0, "e": 0, "s": 10, "w": 0}


def test_align_invalid(tmp_path):
    spec = _spec_with_building(align="diagonal")
    p = _write_yaml(tmp_path, spec)
    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(p)
    assert exc_info.value.field == "building.align"


def test_padding_non_int_raises(tmp_path):
    spec = _spec_with_building(padding={"n": "10px"})
    p = _write_yaml(tmp_path, spec)
    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(p)
    assert exc_info.value.field == "building.padding"


def test_footprint_px_bad_shape_raises(tmp_path):
    spec = _spec_with_building(footprint_px=[28])
    p = _write_yaml(tmp_path, spec)
    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(p)
    assert exc_info.value.field == "building.footprint_px"


# ---------------------------------------------------------------------------
# TECH-710 — variants block + split-seed normalization
# ---------------------------------------------------------------------------


def test_variants_scalar(tmp_path):
    spec = _minimal_spec()
    spec["variants"] = 4
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["variants"] == {"count": 4, "vary": {}, "seed_scope": "palette"}


def test_variants_object(tmp_path):
    spec = _minimal_spec()
    spec["variants"] = {
        "count": 4,
        "vary": {"roof": {"h_px": {"min": 6, "max": 14}}},
        "seed_scope": "geometry",
    }
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["variants"]["count"] == 4
    assert d["variants"]["vary"]["roof"]["h_px"] == {"min": 6, "max": 14}
    assert d["variants"]["seed_scope"] == "geometry"


def test_variants_object_defaults_seed_scope(tmp_path):
    spec = _minimal_spec()
    spec["variants"] = {"count": 3}
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["variants"]["seed_scope"] == "palette"
    assert d["variants"]["vary"] == {}


def test_seed_scope_invalid(tmp_path):
    spec = _minimal_spec()
    spec["variants"] = {"count": 2, "seed_scope": "weird"}
    p = _write_yaml(tmp_path, spec)
    with pytest.raises(SpecValidationError) as exc_info:
        load_spec(p)
    assert exc_info.value.field == "variants.seed_scope"


def test_seed_fanout(tmp_path):
    spec = _minimal_spec()
    spec["seed"] = 42
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["palette_seed"] == 42
    assert d["geometry_seed"] == 42
    assert d["seed"] == 42  # legacy key preserved


def test_split_seeds_explicit(tmp_path):
    spec = _minimal_spec()
    spec["palette_seed"] = 101
    spec["geometry_seed"] = 202
    spec["seed"] = 99
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert d["palette_seed"] == 101
    assert d["geometry_seed"] == 202


def test_split_seeds_absent(tmp_path):
    """No legacy seed + no split seeds → keys remain absent."""
    spec = _minimal_spec()
    p = _write_yaml(tmp_path, spec)
    d = load_spec(p)
    assert "palette_seed" not in d
    assert "geometry_seed" not in d


def test_footprint_conflict_warn(tmp_path):
    spec = _spec_with_building(
        footprint_px=[28, 28],
        footprint_ratio=[0.45, 0.45],
    )
    p = _write_yaml(tmp_path, spec)
    with pytest.warns(DeprecationWarning, match="footprint_px wins"):
        d = load_spec(p)
    assert d["building"]["footprint_px"] == [28, 28]
    assert d["building"]["footprint_ratio"] == [0.45, 0.45]
