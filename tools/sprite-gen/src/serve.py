"""serve.py — FastAPI service mode for sprite-gen (TECH-1433).

Long-lived HTTP service exposing the existing `compose_sprite` /
`sample_variant` / promote pipelines so the web Authoring Console can drive
every CLI knob without shelling out (DEC-A3 single-service shape; DEC-A2
HTTP transport; DEC-A12 future Pydantic schemas).

Endpoints (TECH-1433 scope — typed bodies arrive in TECH-1434):
    GET  /list-archetypes      → list archetype slugs from specs/
    GET  /list-palettes        → list palette ids from palettes/
    POST /render               → run compose pipeline; emit variants under BLOB_ROOT
    POST /promote              → promote a gen:// blob to a target slug

Bind: `127.0.0.1` only per DEC-A3.
Port: env `SPRITE_GEN_PORT`, default `8765`.
Concurrency: serial (single uvicorn worker) per DEC-A39.

Run:
    python -m src serve

The render output root falls back to the existing `tools/sprite-gen/out/`
in TECH-1433 (DEC-A25 closing bullet — back-compat). The canonical
`var/blobs/` swap happens in TECH-1435 once `BlobResolver` ships.
"""

from __future__ import annotations

import copy
import os
import secrets
import sys
from pathlib import Path
from typing import Any, Optional

from .compose import compose_sprite, sample_variant
from .params import PromoteParams, RenderParams
from .spec import SpecValidationError, load_spec

# ---------------------------------------------------------------------------
# Tool-root paths (cwd-independent — mirrors cli.py)
# ---------------------------------------------------------------------------

_SRC_DIR = Path(__file__).resolve().parent
_TOOL_ROOT = _SRC_DIR.parent
_SPECS_DIR = _TOOL_ROOT / "specs"
_PALETTES_DIR = _TOOL_ROOT / "palettes"
_OUT_DIR = _TOOL_ROOT / "out"
_PARAMS_DIR = _SRC_DIR / "params"


def _load_schema_version() -> int:
    """Read the params schema version (params/schema_version.txt)."""
    raw = (_PARAMS_DIR / "schema_version.txt").read_text(encoding="utf-8").strip()
    return int(raw)


def _load_ui_hints() -> dict[str, Any]:
    """Read the ui_hints sidecar JSON."""
    import json as _json

    return _json.loads((_PARAMS_DIR / "ui_hints.json").read_text(encoding="utf-8"))


def _list_archetype_slugs() -> list[str]:
    """Return sorted archetype slugs (filename stems under specs/)."""
    if not _SPECS_DIR.is_dir():
        return []
    return sorted(p.stem for p in _SPECS_DIR.glob("*.yaml"))


def _list_palette_ids() -> list[str]:
    """Return sorted palette ids (filename stems under palettes/, .json only)."""
    if not _PALETTES_DIR.is_dir():
        return []
    return sorted(p.stem for p in _PALETTES_DIR.glob("*.json"))


def _resolve_blob_root() -> Path:
    """Render output root for the service (TECH-1433 fallback shape).

    Honours the `BLOB_ROOT` env var when set; otherwise falls back to the
    legacy CLI output dir `tools/sprite-gen/out/` so existing CLI flows keep
    working until TECH-1435 wires the canonical `var/blobs/` swap point.
    """
    override = os.environ.get("BLOB_ROOT")
    if override:
        return Path(override).expanduser().resolve()
    return _OUT_DIR


def _new_run_id() -> str:
    """Return a short, URL-safe run id."""
    return secrets.token_hex(8)


def create_app() -> Any:
    """Build and return the FastAPI app instance.

    Factory shape so test clients can spin up isolated apps; module-level
    `app` below is the standard uvicorn entrypoint.
    """
    from fastapi import FastAPI, HTTPException

    app = FastAPI(
        title="sprite-gen",
        version="0.1.0",
        description="Long-lived sprite-gen service (DEC-A3).",
    )

    @app.get("/healthz")
    def healthz() -> dict[str, str]:
        """Liveness probe — implementer convenience; not in §Acceptance."""
        return {"status": "ok"}

    @app.get("/list-archetypes")
    def list_archetypes() -> list[str]:
        """Return archetype slugs derived from specs/."""
        return _list_archetype_slugs()

    @app.get("/list-palettes")
    def list_palettes() -> list[str]:
        """Return palette ids derived from palettes/."""
        return _list_palette_ids()

    @app.get("/parameter-schema")
    def parameter_schema_manifest() -> dict[str, Any]:
        """Return manifest of typed endpoints + their schema_version."""
        version = _load_schema_version()
        return {
            "endpoints": [
                {"name": "render", "schema_version": version},
                {"name": "promote", "schema_version": version},
            ],
        }

    @app.get("/parameter-schema/{endpoint}")
    def parameter_schema(endpoint: str) -> dict[str, Any]:
        """Return Pydantic JSON Schema + ui_hints for a typed endpoint."""
        version = _load_schema_version()
        ui_hints = _load_ui_hints()
        if endpoint == "render":
            return {
                "schema": RenderParams.model_json_schema(),
                "ui_hints": ui_hints.get("render", {}),
                "schema_version": version,
            }
        if endpoint == "promote":
            return {
                "schema": PromoteParams.model_json_schema(),
                "ui_hints": ui_hints.get("promote", {}),
                "schema_version": version,
            }
        raise HTTPException(status_code=404, detail=f"unknown endpoint: {endpoint}")

    @app.post("/render")
    def render(body: RenderParams) -> dict[str, Any]:
        """Run the compose pipeline against an archetype.

        Body validated against RenderParams (TECH-1434 typed contract).

        Returns:
            {run_id, fingerprint, variants: [{idx, blob_ref}]}
        """
        archetype = body.archetype
        spec_path = _SPECS_DIR / f"{archetype}.yaml"
        if not spec_path.exists():
            raise HTTPException(status_code=404, detail=f"archetype not found: {archetype}")

        try:
            spec = load_spec(spec_path)
        except SpecValidationError as exc:
            raise HTTPException(status_code=422, detail=str(exc)) from exc

        # Apply terrain override + caller params.
        merged = copy.deepcopy(spec)
        if body.terrain is not None:
            merged["terrain"] = body.terrain
        if body.params:
            merged.update(body.params)

        n_variants = int(merged.get("output", {}).get("variants", 1))
        out_name = merged.get("output", {}).get("name", archetype)

        run_id = _new_run_id()
        blob_root = _resolve_blob_root()
        run_dir = blob_root / run_id
        run_dir.mkdir(parents=True, exist_ok=True)

        variants: list[dict[str, Any]] = []
        for idx in range(n_variants):
            variant_spec = sample_variant(merged, idx)
            img = compose_sprite(variant_spec)
            out_path = run_dir / f"{out_name}_v{idx + 1:02d}.png"
            img.save(out_path)
            variants.append(
                {
                    "idx": idx,
                    "blob_ref": f"gen://{run_id}/{idx}",
                    "path": str(out_path),
                }
            )

        fingerprint = f"{archetype}:{run_id}"
        return {"run_id": run_id, "fingerprint": fingerprint, "variants": variants}

    @app.post("/promote")
    def promote_endpoint(body: PromoteParams) -> dict[str, Any]:
        """Promote a `gen://` blob to a target slug (DEC-A25 promote action).

        Body validated against PromoteParams (TECH-1434 typed contract).

        Returns:
            {assets_path: str}

        TECH-1435 wires the canonical BlobResolver so this handler can read the
        actual blob and copy to Assets/Sprites/Generated/. TECH-1434 enforces
        the typed body shape; the deterministic assets_path stays the same so
        web-side integration tests don't drift.
        """
        slug = body.slug or body.dest_name
        if not slug:
            raise HTTPException(status_code=422, detail="slug or dest_name required")

        if body.source_uri is not None and not body.source_uri.startswith("gen://"):
            raise HTTPException(status_code=422, detail="source_uri must be gen:// URI")

        assets_path = f"Assets/Sprites/Generated/{slug}.png"
        return {
            "assets_path": assets_path,
            "source_uri": body.source_uri,
            "src": body.src,
        }

    return app


# Module-level app for `uvicorn src.serve:app` style invocation.
app = create_app()


def main(argv: Optional[list[str]] = None) -> int:
    """Run uvicorn against the module-level app on 127.0.0.1.

    Returns:
        Exit code (0 on clean shutdown, non-zero on startup failure).
    """
    try:
        import uvicorn
    except ImportError:
        print(
            "error: uvicorn not installed; run `pip install -r tools/sprite-gen/requirements.txt`",
            file=sys.stderr,
        )
        return 1

    port = int(os.environ.get("SPRITE_GEN_PORT", "8765"))
    uvicorn.run(app, host="127.0.0.1", port=port)
    return 0
