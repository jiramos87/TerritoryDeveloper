"""spec.py — YAML archetype spec loader and validator."""

from __future__ import annotations

import warnings
from pathlib import Path
from typing import Union

import yaml

# TECH-709 — valid alignment anchors for `building.align`.
_VALID_ALIGNS: frozenset[str] = frozenset(
    {"center", "sw", "ne", "nw", "se", "custom"}
)

# TECH-710 — valid seed-scope axes for `variants.seed_scope`.
_VALID_SEED_SCOPES: frozenset[str] = frozenset(
    {"palette", "geometry", "palette+geometry"}
)

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


# TECH-730 — public alias used by preset system + test file for consistency
# with the Stage 6.6 plan digest (`SpecError`). Both names point to the same
# class so existing callers + new preset code share one exception type.
SpecError = SpecValidationError


# ---------------------------------------------------------------------------
# TECH-730 / TECH-731 — Preset system (Stage 6.6)
# ---------------------------------------------------------------------------

_PRESET_DIR: Path = Path(__file__).resolve().parent.parent / "presets"


def _valid_preset_names() -> list[str]:
    """Return sorted list of available preset stems under ``presets/``."""
    if not _PRESET_DIR.is_dir():
        return []
    return sorted(p.stem for p in _PRESET_DIR.glob("*.yaml"))


def _load_preset(name: str) -> dict:
    """Load a preset YAML from ``tools/sprite-gen/presets/<name>.yaml``.

    Raises:
        SpecError: name does not resolve to a file under ``presets/``; the
            error message lists every valid preset stem for fast discovery.
    """
    path = _PRESET_DIR / f"{name}.yaml"
    if not path.exists():
        valid = _valid_preset_names()
        raise SpecError(
            field="preset",
            message=f"unknown preset {name!r}. valid presets: {valid}",
        )
    with path.open("r", encoding="utf-8") as fh:
        data = yaml.safe_load(fh)
    if not isinstance(data, dict):
        raise SpecError(
            field="preset",
            message=(
                f"preset {name!r} did not parse as mapping "
                f"(got {type(data).__name__})"
            ),
        )
    return data


def _merge_vary(base_vary: object, overlay_vary: object) -> dict:
    """Merge preset ``vary`` with author ``vary`` under Stage 6.6 rules.

    Rules (TECH-731):
        - Author ``vary: null`` or ``vary: {}`` → SpecError (whole-block wipe
          is refused; individual axes can be nulled instead).
        - Preset axes survive unchanged when author omits them.
        - Author axis value replaces preset axis value (per-axis override).
        - Author new axis is unioned in.
    """
    if overlay_vary is None or (isinstance(overlay_vary, dict) and not overlay_vary):
        raise SpecError(
            field="vary",
            message=(
                "author spec may not wipe preset `vary:` block — "
                "override individual axes instead"
            ),
        )
    if not isinstance(overlay_vary, dict):
        raise SpecError(
            field="vary",
            message=(
                f"author `vary` must be a dict, got {type(overlay_vary).__name__}"
            ),
        )
    base = dict(base_vary) if isinstance(base_vary, dict) else {}
    for axis, value in overlay_vary.items():
        base[axis] = value  # per-axis replace / union
    return base


def _deep_merge(base: dict, overlay: dict) -> dict:
    """Recursive dict-merge: overlay wins per-key; `vary` routed via TECH-731.

    Scalars / lists / non-dicts in either side → overlay wins entirely.
    Nested dicts → merge recursively. The ``vary`` key is routed through
    :func:`_merge_vary` so the Stage 6.6 wipe-guard + union semantics apply.
    """
    out: dict = dict(base)
    for key, value in overlay.items():
        if key == "vary":
            out[key] = _merge_vary(out.get(key), value)
        elif isinstance(value, dict) and isinstance(out.get(key), dict):
            out[key] = _deep_merge(out[key], value)
        else:
            out[key] = value
    return out


def _apply_preset(data: dict) -> dict:
    """If ``data`` carries a top-level ``preset:`` key, inject + merge.

    Author fields override preset fields per-key; ``vary:`` goes through
    the TECH-731 union/wipe-guard merge. The ``preset`` key is popped so
    downstream validators never see it as an unknown top-level field.
    """
    if "preset" not in data:
        return data
    name = data.pop("preset")
    if not isinstance(name, str) or not name:
        raise SpecError(
            field="preset",
            message=f"`preset:` must be a non-empty string, got {name!r}",
        )
    base = _load_preset(name)
    base.pop("preset", None)  # defensive: presets must not recurse
    return _deep_merge(base, data)


def load_spec_from_dict(data: dict) -> dict:
    """Validate a preloaded dict spec (TECH-730 test helper).

    Same pipeline as :func:`load_spec` but skips the YAML file read so
    tests can hand in a dict directly. Supports top-level ``preset:``.
    """
    if not isinstance(data, dict):
        raise SpecValidationError(
            field="<root>",
            message=(
                f"missing required field: <root> (expected mapping, "
                f"got {type(data).__name__})"
            ),
        )
    data = _apply_preset(dict(data))

    if "composition" not in data and isinstance(data.get("building"), dict):
        bc = data["building"].get("composition")
        if isinstance(bc, list):
            data = {**data, "composition": bc}

    data.setdefault("include_in_signature", True)

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

    footprint = data["footprint"]
    if len(footprint) != 2 or not all(isinstance(v, int) for v in footprint):
        raise SpecValidationError(
            field="footprint",
            message=(
                "field 'footprint' must be a 2-element list of ints, "
                f"got {footprint!r}"
            ),
        )

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
                message=(
                    f"field 'composition[{i}]' must be a dict, "
                    f"got {type(entry).__name__}"
                ),
            )
        if "type" not in entry:
            raise SpecValidationError(
                field="composition",
                message=f"field 'composition[{i}]' missing required key 'type'",
            )

    building = data.get("building")
    if isinstance(building, dict):
        data["building"] = _normalize_building_placement(building)

    _normalize_variants(data)
    _normalize_seeds(data)
    _normalize_ground(data)

    return data


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

    # TECH-730 — preset injection + author-override merge runs before any
    # schema validation so the merged spec passes required-key checks.
    data = _apply_preset(data)

    # R11: `building.composition` aliases to top-level for validation
    if "composition" not in data and isinstance(data.get("building"), dict):
        bc = data["building"].get("composition")
        if isinstance(bc, list):
            data = {**data, "composition": bc}

    # TECH-706: Stage 6.2 per-sprite opt-out from signature ingestion.
    # Default True so existing specs behave identically; signature refresh
    # filters out sprites whose source YAML sets this to False.
    data.setdefault("include_in_signature", True)

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

    # TECH-709 — placement schema additions (building.footprint_px / padding / align).
    building = data.get("building")
    if isinstance(building, dict):
        data["building"] = _normalize_building_placement(building)

    # TECH-710 — variants block + split seeds (palette_seed / geometry_seed).
    _normalize_variants(data)
    _normalize_seeds(data)

    # TECH-715 — ground accepts string or object; "none" sentinel stays string.
    _normalize_ground(data)

    return data


# ---------------------------------------------------------------------------
# TECH-709 — placement-field normalization (building.footprint_px, padding, align)
# ---------------------------------------------------------------------------


def _normalize_building_placement(building: dict) -> dict:
    """Normalise `building.padding` / `building.align` + warn on footprint conflict.

    In-place on the supplied dict; returns the same reference for chaining.

    Behaviour:
        - `padding` default `{n:0, e:0, s:0, w:0}`; missing subkeys default to 0.
        - `padding` values must be ints; `SpecValidationError` raised otherwise.
        - `align` default `center`; non-enum raises `SpecValidationError`.
        - Both `footprint_px` and `footprint_ratio` present → `DeprecationWarning`.
        - `footprint_px` passes through when set but must be a 2-element list.
    """
    # padding: concrete 4-key dict (consumers never need None-checks).
    raw_padding = building.get("padding") or {}
    if not isinstance(raw_padding, dict):
        raise SpecValidationError(
            field="building.padding",
            message=(
                f"field 'building.padding' must be dict, got {type(raw_padding).__name__}"
            ),
        )
    normalised_padding: dict[str, int] = {}
    for side in ("n", "e", "s", "w"):
        value = raw_padding.get(side, 0)
        if not isinstance(value, int) or isinstance(value, bool):
            raise SpecValidationError(
                field="building.padding",
                message=(
                    f"field 'building.padding.{side}' must be int, "
                    f"got {type(value).__name__}"
                ),
            )
        normalised_padding[side] = int(value)
    building["padding"] = normalised_padding

    # align: enum validation with default center.
    align = building.get("align", "center")
    if align not in _VALID_ALIGNS:
        raise SpecValidationError(
            field="building.align",
            message=(
                f"building.align={align!r} not in {sorted(_VALID_ALIGNS)}"
            ),
        )
    building["align"] = align

    # footprint_px shape guard (optional; passes through when absent).
    footprint_px = building.get("footprint_px")
    if footprint_px is not None:
        if (
            not isinstance(footprint_px, list)
            or len(footprint_px) != 2
            or not all(isinstance(v, int) and not isinstance(v, bool) for v in footprint_px)
        ):
            raise SpecValidationError(
                field="building.footprint_px",
                message=(
                    "field 'building.footprint_px' must be a 2-element list of ints, "
                    f"got {footprint_px!r}"
                ),
            )

    # Conflict warning: `footprint_px` + `footprint_ratio` both set.
    if "footprint_px" in building and "footprint_ratio" in building:
        warnings.warn(
            "building.footprint_px wins over footprint_ratio; drop one.",
            DeprecationWarning,
            stacklevel=3,
        )

    return building


# ---------------------------------------------------------------------------
# TECH-710 — variants block + split-seed normalization
# ---------------------------------------------------------------------------


def _normalize_variants(data: dict) -> None:
    """Normalise `variants` to `{count, vary, seed_scope}` object shape.

    Legacy scalar `variants: N` → `{count: N, vary: {}, seed_scope: "palette"}`.
    Object form passes through with defaulted subkeys + enum-validated
    `seed_scope`. `variants: null` (or absent) leaves the key untouched.
    """
    v = data.get("variants")
    if v is None:
        return
    if isinstance(v, bool):
        raise SpecValidationError(
            field="variants",
            message=f"field 'variants' must be int or dict, got {type(v).__name__}",
        )
    if isinstance(v, int):
        data["variants"] = {"count": v, "vary": {}, "seed_scope": "palette"}
        return
    if isinstance(v, dict):
        v.setdefault("count", 1)
        v.setdefault("vary", {})
        scope = v.setdefault("seed_scope", "palette")
        if scope not in _VALID_SEED_SCOPES:
            raise SpecValidationError(
                field="variants.seed_scope",
                message=(
                    f"variants.seed_scope={scope!r} not in {sorted(_VALID_SEED_SCOPES)}"
                ),
            )
        if not isinstance(v.get("vary"), dict):
            raise SpecValidationError(
                field="variants.vary",
                message=(
                    f"field 'variants.vary' must be dict, "
                    f"got {type(v.get('vary')).__name__}"
                ),
            )
        # TECH-720 — validate vary.ground sub-dict when present.
        vary = v["vary"]
        if "ground" in vary:
            if not isinstance(vary["ground"], dict):
                raise SpecValidationError(
                    field="variants.vary.ground",
                    message=f"vary.ground must be dict, got {type(vary['ground']).__name__}",
                )
            vary["ground"] = _normalize_vary_ground(vary["ground"])
        return
    raise SpecValidationError(
        field="variants",
        message=f"field 'variants' must be int or dict, got {type(v).__name__}",
    )


# ---------------------------------------------------------------------------
# TECH-720 — vary.ground grammar validation helpers
# ---------------------------------------------------------------------------


def _validate_range(raw: object, field: str) -> dict:
    """Validate and normalise a ``{min: N, max: N}`` range dict.

    Raises ``SpecValidationError`` when *raw* is not a dict or lacks min/max.
    """
    if not isinstance(raw, dict) or "min" not in raw or "max" not in raw:
        raise SpecValidationError(
            field=field,
            message=f"{field}: expected {{min, max}} dict, got {raw!r}",
        )
    return {"min": float(raw["min"]), "max": float(raw["max"])}


def _normalize_vary_ground(raw: dict) -> dict:
    """Validate and normalise a ``vary.ground`` sub-dict (TECH-720).

    Accepted keys: ``material.values`` (non-empty list), ``hue_jitter`` ({min, max}),
    ``value_jitter`` ({min, max}), ``texture.density`` ({min, max}).
    Unknown top-level keys are silently ignored (forward-compat).
    """
    out: dict = {}
    if "material" in raw:
        values = raw["material"].get("values") if isinstance(raw["material"], dict) else None
        if not values or not isinstance(values, list) or len(values) == 0:
            raise SpecValidationError(
                field="vary.ground.material.values",
                message="vary.ground.material.values: expected non-empty list of strings",
            )
        if not all(isinstance(v, str) for v in values):
            raise SpecValidationError(
                field="vary.ground.material.values",
                message="vary.ground.material.values: all entries must be strings",
            )
        out["material"] = {"values": list(values)}
    for axis in ("hue_jitter", "value_jitter"):
        if axis in raw:
            out[axis] = _validate_range(raw[axis], f"vary.ground.{axis}")
    if "texture" in raw:
        tex = raw["texture"]
        if isinstance(tex, dict) and "density" in tex:
            out["texture"] = {"density": _validate_range(tex["density"], "vary.ground.texture.density")}
    return out


# ---------------------------------------------------------------------------
# TECH-715 — ground-field normalization (string / object form)
# ---------------------------------------------------------------------------


_GROUND_OBJECT_KEYS: frozenset[str] = frozenset(
    {"material", "materials", "hue_jitter", "value_jitter", "texture"}
)


def _normalize_ground(data: dict) -> None:
    """Normalise ``ground:`` to a single object shape.

    Forms accepted:
        - absent / None            → unchanged (composer falls back to class default)
        - ``"none"`` sentinel      → unchanged (skip ground diamond)
        - any other str ``m``      → ``{"material": m, "materials": None,
                                        "hue_jitter": None, "value_jitter": None,
                                        "texture": None}``
        - dict with subset of keys → defaults filled to None; both
                                     ``material`` and ``materials`` set → error.

    Units (documented for consumers — TECH-718):
        ``hue_jitter`` in degrees; ``value_jitter`` in percent of HSV V.
    """
    raw = data.get("ground")
    if raw is None or raw == "none":
        return
    if isinstance(raw, str):
        data["ground"] = {
            "material": raw,
            "materials": None,
            "hue_jitter": None,
            "value_jitter": None,
            "texture": None,
        }
        return
    if isinstance(raw, dict):
        unknown = set(raw.keys()) - _GROUND_OBJECT_KEYS
        if unknown:
            raise SpecValidationError(
                field="ground",
                message=(
                    f"ground: unknown key(s) {sorted(unknown)}; "
                    f"expected {sorted(_GROUND_OBJECT_KEYS)}"
                ),
            )
        if raw.get("material") and raw.get("materials"):
            raise SpecValidationError(
                field="ground",
                message="ground: supply either 'material' or 'materials', not both",
            )
        materials = raw.get("materials")
        if materials is not None:
            if not isinstance(materials, list) or not materials:
                raise SpecValidationError(
                    field="ground.materials",
                    message=(
                        "ground.materials must be a non-empty list of strings"
                    ),
                )
            if not all(isinstance(m, str) for m in materials):
                raise SpecValidationError(
                    field="ground.materials",
                    message="ground.materials entries must be strings",
                )
        data["ground"] = {
            "material": raw.get("material"),
            "materials": materials,
            "hue_jitter": raw.get("hue_jitter"),
            "value_jitter": raw.get("value_jitter"),
            "texture": raw.get("texture"),
        }
        return
    raise SpecValidationError(
        field="ground",
        message=f"ground: expected str or dict, got {type(raw).__name__}",
    )


def _normalize_seeds(data: dict) -> None:
    """Fan legacy scalar `seed: N` → `palette_seed = geometry_seed = N`.

    Explicit split seeds always win; fan-out only happens when neither
    `palette_seed` nor `geometry_seed` is set.
    """
    palette = data.get("palette_seed")
    geometry = data.get("geometry_seed")
    legacy = data.get("seed")
    if palette is None and geometry is None and legacy is not None:
        if not isinstance(legacy, int) or isinstance(legacy, bool):
            raise SpecValidationError(
                field="seed",
                message=f"field 'seed' must be int, got {type(legacy).__name__}",
            )
        data["palette_seed"] = int(legacy)
        data["geometry_seed"] = int(legacy)


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
