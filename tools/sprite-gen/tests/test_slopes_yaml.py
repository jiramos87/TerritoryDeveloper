"""Tests for tools/sprite-gen/slopes.yaml structure and filesystem coverage.

Acceptance:
  - YAML parses via yaml.safe_load.
  - Exactly 18 top-level keys: flat + 17 land slope variants.
  - Every value has exactly {n, e, s, w} keys; each value in {0, 16}.
  - All non-flat ids correspond to an existing Assets/Sprites/Slopes/ file.
"""

import pathlib
import yaml
import pytest

REPO_ROOT = pathlib.Path(__file__).parents[3]
SLOPES_YAML = pathlib.Path(__file__).parents[1] / "slopes.yaml"
SLOPES_DIR = REPO_ROOT / "Assets" / "Sprites" / "Slopes"

EXPECTED_KEYS = {
    "flat",
    "N", "S", "E", "W",
    "NE", "NW", "SE", "SW",
    "NE-up", "NW-up", "SE-up", "SW-up",
    "NE-bay", "NW-bay", "NW-bay-2", "SE-bay", "SW-bay",
}

# Mapping from yaml slope id to actual filename in Assets/Sprites/Slopes/.
# Most follow {id}-slope.png; NW-bay-2 uses the irregular NW-bay-slope-2.png.
FILENAME_MAP = {
    slope_id: f"{slope_id}-slope.png"
    for slope_id in EXPECTED_KEYS - {"flat"}
}
FILENAME_MAP["NW-bay-2"] = "NW-bay-slope-2.png"


def load_slopes():
    return yaml.safe_load(SLOPES_YAML.read_text())


def test_yaml_parses():
    data = load_slopes()
    assert isinstance(data, dict)


def test_key_set():
    data = load_slopes()
    assert set(data.keys()) == EXPECTED_KEYS, (
        f"Key mismatch.\n  missing: {EXPECTED_KEYS - set(data.keys())}\n"
        f"  extra: {set(data.keys()) - EXPECTED_KEYS}"
    )


def test_corner_structure_and_values():
    data = load_slopes()
    for slope_id, corners in data.items():
        assert set(corners.keys()) == {"n", "e", "s", "w"}, (
            f"{slope_id}: expected keys {{n,e,s,w}}, got {set(corners.keys())}"
        )
        for corner, value in corners.items():
            assert value in (0, 16), (
                f"{slope_id}.{corner}: value {value!r} not in {{0, 16}}"
            )


def test_filesystem_coverage():
    """Each non-flat slope id maps to an existing file under Assets/Sprites/Slopes/."""
    missing = []
    for slope_id, filename in FILENAME_MAP.items():
        path = SLOPES_DIR / filename
        if not path.exists():
            missing.append(f"{slope_id} → {filename}")
    assert not missing, "Slope files missing from Assets/Sprites/Slopes/:\n" + "\n".join(missing)
