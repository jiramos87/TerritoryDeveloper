"""test_params_schema.py — Tests for the params package + schema endpoints.

Contracts (TECH-1434 §Test Blueprint):
    test_render_params_field_parity     — schema fields ⊆ ui_hints fields and back
    test_promote_params_field_parity    — ditto for promote
    test_parameter_schema_endpoint      — /parameter-schema endpoints + 404 on unknown
    test_render_validates_body          — POST /render rejects bad body with 422
    test_validate_sprite_gen_schema_pass — `npm run validate:sprite-gen-schema` exits 0
    test_validate_sprite_gen_schema_fail — drop a hint key → exits non-zero
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

import pytest

from src.params import PromoteParams, RenderParams


_TOOL_ROOT = Path(__file__).resolve().parents[1]
_REPO_ROOT = _TOOL_ROOT.parent.parent
_HINTS_PATH = _TOOL_ROOT / "src" / "params" / "ui_hints.json"


def _client():
    from fastapi.testclient import TestClient

    from src.serve import create_app

    return TestClient(create_app())


def _hint_keys(group: dict) -> set[str]:
    return {k for k in group.keys() if not k.startswith("_")}


def test_render_params_field_parity() -> None:
    schema = RenderParams.model_json_schema()
    schema_fields = set((schema.get("properties") or {}).keys())
    hints = json.loads(_HINTS_PATH.read_text(encoding="utf-8"))
    hints_fields = _hint_keys(hints["render"])
    assert schema_fields == hints_fields, (
        f"render schema-only={schema_fields - hints_fields}; "
        f"hints-only={hints_fields - schema_fields}"
    )


def test_promote_params_field_parity() -> None:
    schema = PromoteParams.model_json_schema()
    schema_fields = set((schema.get("properties") or {}).keys())
    hints = json.loads(_HINTS_PATH.read_text(encoding="utf-8"))
    hints_fields = _hint_keys(hints["promote"])
    assert schema_fields == hints_fields, (
        f"promote schema-only={schema_fields - hints_fields}; "
        f"hints-only={hints_fields - schema_fields}"
    )


def test_parameter_schema_endpoint() -> None:
    client = _client()
    manifest = client.get("/parameter-schema").json()
    assert "endpoints" in manifest
    names = {e["name"] for e in manifest["endpoints"]}
    assert {"render", "promote"} <= names

    render = client.get("/parameter-schema/render").json()
    assert "schema" in render and "ui_hints" in render and "schema_version" in render

    promote = client.get("/parameter-schema/promote").json()
    assert "schema" in promote and "ui_hints" in promote

    unknown = client.get("/parameter-schema/unknown")
    assert unknown.status_code == 404


def test_render_validates_body() -> None:
    # Missing required `archetype` → 422 from FastAPI body validation.
    response = _client().post("/render", json={})
    assert response.status_code == 422


def test_render_rejects_extra_fields() -> None:
    # Pydantic v2 `extra=forbid` should reject unknown top-level keys.
    response = _client().post(
        "/render",
        json={"archetype": "building_residential_small", "bogus_field": True},
    )
    assert response.status_code == 422


def test_validate_sprite_gen_schema_pass() -> None:
    result = subprocess.run(
        ["npm", "run", "--silent", "validate:sprite-gen-schema"],
        cwd=str(_REPO_ROOT),
        capture_output=True,
        text=True,
        timeout=120,
    )
    assert result.returncode == 0, (
        f"stdout={result.stdout!r} stderr={result.stderr!r}"
    )


def test_validate_sprite_gen_schema_fail(tmp_path) -> None:
    """Mutate ui_hints.json (drop a render field) and assert validator fails."""
    backup = _HINTS_PATH.read_text(encoding="utf-8")
    try:
        hints = json.loads(backup)
        # Pop one known render field; validator must flag it.
        hints["render"].pop("layered", None)
        _HINTS_PATH.write_text(json.dumps(hints, indent=2) + "\n", encoding="utf-8")
        result = subprocess.run(
            ["npm", "run", "--silent", "validate:sprite-gen-schema"],
            cwd=str(_REPO_ROOT),
            capture_output=True,
            text=True,
            timeout=120,
        )
        assert result.returncode != 0
        assert "layered" in (result.stderr + result.stdout)
    finally:
        _HINTS_PATH.write_text(backup, encoding="utf-8")
