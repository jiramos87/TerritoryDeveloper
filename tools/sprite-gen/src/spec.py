"""spec.py — YAML archetype spec loader and validator."""

from __future__ import annotations

from pathlib import Path
from typing import Union

import yaml

# Required keys: (key_name, expected_type)
REQUIRED_KEYS: list[tuple[str, type]] = [
    ("id", str),
    ("class", str),
    ("footprint", list),
    ("terrain", str),
    ("composition", list),
    ("palette", str),
    ("output", dict),
]


class SpecValidationError(Exception):
    """Raised when an archetype YAML spec fails schema validation.

    Attributes:
        field: Name of the missing or malformed field.
    """

    def __init__(self, field: str, message: str | None = None) -> None:
        self.field = field
        self._message = message
        super().__init__(str(self))

    def __str__(self) -> str:
        if self._message:
            return self._message
        return f"missing required field: {self.field}"


def load_spec(path: Union[str, Path]) -> dict:
    """Load and validate an archetype YAML spec file.

    Args:
        path: Path to the ``.yaml`` spec file.

    Returns:
        Validated dict with all required keys present. Optional fields
        (``levels``, ``seed``, ``variants``, ``diffusion``) pass through
        unmodified.

    Raises:
        FileNotFoundError: Path does not exist.
        yaml.YAMLError: File is not valid YAML (bubbles un-wrapped so callers
            retain parse-line information).
        SpecValidationError: Required key missing, wrong type, or structural
            constraint violated.
    """
    path = Path(path)

    with path.open("r", encoding="utf-8") as fh:
        data = yaml.safe_load(fh)

    # Top-level must be a mapping
    if not isinstance(data, dict):
        raise SpecValidationError(
            field="<root>",
            message=f"missing required field: <root> (expected mapping, got {type(data).__name__})",
        )

    # Required-key presence + type checks
    for key, expected_type in REQUIRED_KEYS:
        if key not in data:
            raise SpecValidationError(field=key)
        value = data[key]
        if not isinstance(value, expected_type):
            raise SpecValidationError(
                field=key,
                message=(
                    f"field '{key}' must be {expected_type.__name__}, "
                    f"got {type(value).__name__}"
                ),
            )

    # footprint: exactly 2-element list of ints
    footprint = data["footprint"]
    if len(footprint) != 2 or not all(isinstance(v, int) for v in footprint):
        raise SpecValidationError(
            field="footprint",
            message=(
                "field 'footprint' must be a 2-element list of ints, "
                f"got {footprint!r}"
            ),
        )

    # composition: non-empty list of dicts each with a 'type' key
    composition = data["composition"]
    if len(composition) == 0:
        raise SpecValidationError(
            field="composition",
            message="field 'composition' must be a non-empty list",
        )
    for i, entry in enumerate(composition):
        if not isinstance(entry, dict):
            raise SpecValidationError(
                field="composition",
                message=f"field 'composition[{i}]' must be a dict, got {type(entry).__name__}",
            )
        if "type" not in entry:
            raise SpecValidationError(
                field="composition",
                message=f"field 'composition[{i}]' missing required key 'type'",
            )

    return data


# --- Stage 6 — class defaults (DAS §4.2 + §2.5) ---

_DEFAULT_GROUND: dict[str, str] = {
    "residential_small": "grass_flat",
    "residential_dense_tower": "grass_flat",
    "residential_heavy": "grass_flat",
    "commercial_store": "pavement",
    "commercial_dense": "pavement",
    "commercial_small": "pavement",
    "industrial_light": "pavement",
    "industrial_heavy": "pavement",
    "power_nuclear": "mustard_industrial",
    "waterplant": "grass_flat",
}

_DEFAULT_FOOTPRINT_RATIO: dict[str, tuple[float, float]] = {
    "residential_small": (0.45, 0.45),
    "residential_dense_tower": (0.9, 0.9),
    "residential_heavy": (0.45, 0.45),
    "commercial_store": (0.55, 0.55),
    "commercial_small": (0.55, 0.55),
    "commercial_dense": (0.95, 0.95),
    "industrial_light": (0.7, 0.7),
    "industrial_heavy": (0.7, 0.7),
    "power_nuclear": (0.7, 0.7),
    "waterplant": (0.8, 0.8),
}


def default_ground_for_class(class_name: str) -> str:
    return _DEFAULT_GROUND.get(class_name, "grass_flat")


def default_footprint_ratio_for_class(class_name: str) -> tuple[float, float]:
    return _DEFAULT_FOOTPRINT_RATIO.get(class_name, (1.0, 1.0))


def composition_entries(spec: dict) -> list:
    """Top-level `composition` or R11 `building.composition`, whichever is set."""
    c = spec.get("composition")
    if c is not None:
        return c
    b = spec.get("building")
    if isinstance(b, dict) and b.get("composition") is not None:
        return b["composition"]
    return []
