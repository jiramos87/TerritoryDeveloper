"""test_serve.py — Tests for the sprite-gen FastAPI service (TECH-1433).

Contracts (TECH-1433 §Test Blueprint):
    test_serve_lists_archetypes        — /list-archetypes returns 200 + non-empty list
    test_serve_lists_palettes          — /list-palettes returns 200 + non-empty list
    test_serve_render_smoke            — /render returns run_id + variants[*].blob_ref
    test_serve_promote_smoke           — /promote returns assets_path
    test_main_dispatch_serve_vs_render — `python -m src serve` boots server; render still works

The render smoke uses a real archetype from `tools/sprite-gen/specs/`; that
keeps the test honest end-to-end (compose pipeline + palette load) without
mocking the inner stack.
"""

from __future__ import annotations

import os
import subprocess
import sys
from pathlib import Path

import pytest


_REPO_ROOT = Path(__file__).resolve().parents[3]
_TOOL_ROOT = Path(__file__).resolve().parents[1]


def _client():
    """Build a fresh FastAPI test client over a fresh app instance."""
    from fastapi.testclient import TestClient

    from src.serve import create_app

    return TestClient(create_app())


def test_serve_lists_archetypes() -> None:
    response = _client().get("/list-archetypes")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, list)
    assert len(data) > 0
    # Every entry must correspond to a real spec on disk.
    specs_dir = _TOOL_ROOT / "specs"
    on_disk = {p.stem for p in specs_dir.glob("*.yaml")}
    assert set(data) == on_disk


def test_serve_lists_palettes() -> None:
    response = _client().get("/list-palettes")
    assert response.status_code == 200
    data = response.json()
    assert isinstance(data, list)
    palettes_dir = _TOOL_ROOT / "palettes"
    on_disk = {p.stem for p in palettes_dir.glob("*.json")}
    assert set(data) == on_disk


def test_serve_render_smoke(tmp_path, monkeypatch) -> None:
    monkeypatch.setenv("BLOB_ROOT", str(tmp_path))
    response = _client().post(
        "/render",
        json={"archetype": "building_residential_small", "params": {}},
    )
    assert response.status_code == 200, response.text
    body = response.json()
    assert "run_id" in body
    assert "fingerprint" in body
    assert "variants" in body
    assert isinstance(body["variants"], list)
    assert len(body["variants"]) >= 1
    first = body["variants"][0]
    assert "idx" in first
    assert first["blob_ref"].startswith("gen://")
    # Variant file landed under the override blob root.
    written = Path(first["path"])
    assert written.exists()
    assert tmp_path in written.parents


def test_serve_render_unknown_archetype() -> None:
    response = _client().post("/render", json={"archetype": "nonexistent_xyz"})
    assert response.status_code == 404


def test_serve_render_missing_archetype_field() -> None:
    response = _client().post("/render", json={})
    assert response.status_code == 422


def test_serve_promote_smoke() -> None:
    response = _client().post(
        "/promote",
        json={
            "source_uri": "gen://abc123/0",
            "slug": "residential_demo",
            "dest_name": "residential_demo",
        },
    )
    assert response.status_code == 200, response.text
    body = response.json()
    assert "assets_path" in body
    assert body["assets_path"].endswith("residential_demo.png")
    assert body["assets_path"].startswith("Assets/Sprites/Generated/")


def test_serve_promote_rejects_non_gen_uri() -> None:
    response = _client().post(
        "/promote",
        json={"source_uri": "s3://bucket/key", "slug": "x", "dest_name": "x"},
    )
    assert response.status_code == 422


def test_main_dispatch_serve_vs_render() -> None:
    """`python -m src render --help` must still print render help.

    The serve dispatch path is exercised in-process by the tests above
    (importing `src.serve`); we don't actually boot uvicorn here.
    """
    env = os.environ.copy()
    env["PYTHONPATH"] = str(_TOOL_ROOT) + os.pathsep + env.get("PYTHONPATH", "")
    result = subprocess.run(
        [sys.executable, "-m", "src", "render", "--help"],
        cwd=str(_TOOL_ROOT),
        capture_output=True,
        text=True,
        env=env,
        timeout=20,
    )
    assert result.returncode == 0, result.stderr
    assert "Render an archetype YAML" in result.stdout or "archetype" in result.stdout
