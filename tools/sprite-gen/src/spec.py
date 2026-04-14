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
