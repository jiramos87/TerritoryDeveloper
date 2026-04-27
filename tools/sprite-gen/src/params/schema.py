"""schema.py — Pydantic v2 body models for sprite-gen FastAPI service.

Each Pydantic field mirrors a CLI knob exposed by `src.cli`:

`RenderParams` covers the existing `python -m src render` subcommand:
    archetype  → positional `archetype` arg
    terrain    → `--terrain SLOPE_ID`
    layered    → `--layered`
    params     → forward-compat dict for in-flight overrides (passed through
                 to the compose pipeline; opaque to the schema validator)

`PromoteParams` covers `python -m src promote`:
    src        → positional SRC arg
    dest_name  → `--as NAME`
    edit       → `--edit`
    no_push    → `--no-push`
    source_uri → web-side gen:// URI carrier (used by the FastAPI handler
                 instead of a local filesystem path; mutually exclusive with
                 `src` at the handler layer — schema accepts either)

Field-parity with `ui_hints.json` is enforced by
`tools/scripts/validate-sprite-gen-schema.ts` and surfaced through
`npm run validate:sprite-gen-schema`.
"""

from __future__ import annotations

from typing import Any, Optional

from pydantic import BaseModel, ConfigDict, Field

# Slope variants accepted by the existing CLI `--terrain` flag (cli.py
# `_VALID_SLOPE_IDS`). Mirror as a tuple so the JSON-Schema dump emits an
# enum constraint.
_VALID_SLOPE_IDS: tuple[str, ...] = (
    "flat",
    "N", "S", "E", "W",
    "NE", "NW", "SE", "SW",
    "NE-up", "NW-up", "SE-up", "SW-up",
    "NE-bay", "NW-bay", "NW-bay-2", "SE-bay", "SW-bay",
)


class RenderParams(BaseModel):
    """Body model for `POST /render`."""

    model_config = ConfigDict(extra="forbid", str_strip_whitespace=True)

    archetype: str = Field(
        ...,
        description="Archetype slug; resolves tools/sprite-gen/specs/{archetype}.yaml.",
    )
    terrain: Optional[str] = Field(
        default=None,
        description="Optional slope override (mirrors CLI --terrain).",
    )
    layered: bool = Field(
        default=False,
        description="Co-emit layered .aseprite alongside flat PNG (CLI --layered).",
    )
    params: dict[str, Any] = Field(
        default_factory=dict,
        description="Forward-compat overrides merged into the loaded spec dict.",
    )


class AudioRenderParams(BaseModel):
    """Body model for `POST /render-audio` (TECH-1957 / Stage 9.1).

    Drives :func:`tools.sprite-gen.src.audio_synth.synth_audio`. ``params``
    carries archetype-specific knobs (e.g. envelope ADSR, cutoff_hz) and
    is opaque to the schema validator — the synth function dispatches on
    ``archetype_id``.
    """

    model_config = ConfigDict(extra="forbid", str_strip_whitespace=True)

    archetype_id: str = Field(
        ...,
        description="Audio archetype slug (e.g. ui_click_v1).",
    )
    archetype_version_id: Optional[str] = Field(
        default=None,
        description="Optional archetype version pin (DEC-A17). Echoed back in the response for traceability.",
    )
    params: dict[str, Any] = Field(
        default_factory=dict,
        description="Archetype param overrides (envelope, cutoff, gain, …).",
    )


class PromoteParams(BaseModel):
    """Body model for `POST /promote`."""

    model_config = ConfigDict(extra="forbid", str_strip_whitespace=True)

    source_uri: Optional[str] = Field(
        default=None,
        description="gen:// URI of the source blob (web-driven promote).",
    )
    src: Optional[str] = Field(
        default=None,
        description="Local PNG / .aseprite path (CLI-driven promote).",
    )
    dest_name: str = Field(
        ...,
        description="Destination slug under Assets/Sprites/Generated/ (CLI --as).",
    )
    slug: Optional[str] = Field(
        default=None,
        description="Alias for dest_name accepted from web clients; one of slug / dest_name required.",
    )
    edit: bool = Field(
        default=False,
        description="Flatten .aseprite via Aseprite CLI before promote (CLI --edit).",
    )
    no_push: bool = Field(
        default=False,
        description="Skip catalog HTTP push after promote (CLI --no-push).",
    )
    archetype: Optional[str] = Field(
        default=None,
        description="Optional archetype tag for cataloguing the promoted asset.",
    )
