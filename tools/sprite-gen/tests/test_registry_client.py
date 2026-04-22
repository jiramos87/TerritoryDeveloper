"""Tests for registry_client (TECH-678)."""

from __future__ import annotations

import pathlib

import pytest
import requests
import responses

from src.curate import (
    build_catalog_payload,
    push_catalog_for_promote,
    rows_match_for_idempotency,
    _load_spec_meta_for_dest,
)
from src.registry_client import (
    CatalogConfigError,
    ConflictError,
    RegistryClient,
    RegistryConnectionError,
    ValidationError,
    resolve_catalog_url,
)


BASE = "http://catalog.test"


@responses.activate
def test_create_200() -> None:
    responses.add(
        responses.POST,
        f"{BASE}/api/catalog/assets",
        json={"id": "42", "slug": "x"},
        status=201,
    )
    c = RegistryClient(BASE)
    out = c.create_asset({"slug": "x"})
    assert out["id"] == "42"


@responses.activate
def test_create_409_conflict_error() -> None:
    responses.add(responses.POST, f"{BASE}/api/catalog/assets", json={"err": "dup"}, status=409)
    c = RegistryClient(BASE)
    with pytest.raises(ConflictError):
        c.create_asset({"slug": "x"})


@responses.activate
def test_get_asset_by_slug_filters() -> None:
    responses.add(
        responses.GET,
        f"{BASE}/api/catalog/assets",
        json={
            "assets": [
                {"slug": "a", "id": "1"},
                {"slug": "b", "id": "2", "world_sprite_path": "P", "generator_archetype_id": "G"},
            ]
        },
        status=200,
    )
    c = RegistryClient(BASE)
    row = c.get_asset_by_slug("b")
    assert row is not None
    assert row["slug"] == "b"


@responses.activate
def test_422_validation() -> None:
    responses.add(responses.POST, f"{BASE}/api/catalog/assets", json={"detail": "bad"}, status=422)
    c = RegistryClient(BASE)
    with pytest.raises(ValidationError):
        c.create_asset({"slug": "x"})


@responses.activate
def test_connection_maps_to_registry_conn() -> None:
    def _cb(_: requests.PreparedRequest) -> tuple[int, dict, str]:
        raise requests.ConnectionError("refused")

    responses.add_callback(
        responses.POST,
        f"{BASE}/api/catalog/assets",
        callback=_cb,
    )
    c = RegistryClient(BASE)
    with pytest.raises(RegistryConnectionError):
        c.create_asset({"slug": "x"})


def test_resolve_env_wins(tmp_path: pathlib.Path, monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("TG_CATALOG_API_URL", "https://env.example/api")
    cfg = tmp_path / "config.toml"
    cfg.write_text('[catalog]\nurl = "https://cfg.example"\n', encoding="utf-8")
    monkeypatch.chdir(tmp_path)
    # resolve reads tools/sprite-gen/config.toml from package root, not cwd — set env only
    assert resolve_catalog_url() == "https://env.example/api"


def test_resolve_config_only(tmp_path: pathlib.Path, monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("TG_CATALOG_API_URL", raising=False)
    import src.registry_client as rc

    cfg = tmp_path / "config.toml"
    cfg.write_text('[catalog]\nurl = "https://cfg.example"\n', encoding="utf-8")
    monkeypatch.setattr(rc, "_TOOL_ROOT", tmp_path)
    assert resolve_catalog_url() == "https://cfg.example"


def test_resolve_both_missing_raises(tmp_path: pathlib.Path, monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.delenv("TG_CATALOG_API_URL", raising=False)
    import src.registry_client as rc

    monkeypatch.setattr(rc, "_TOOL_ROOT", tmp_path)
    with pytest.raises(CatalogConfigError):
        resolve_catalog_url()


@responses.activate
def test_push_idempotent_skip_on_409_match(monkeypatch: pytest.MonkeyPatch, tmp_path: pathlib.Path) -> None:
    monkeypatch.setenv("TG_CATALOG_API_URL", BASE)
    spec_meta = _load_spec_meta_for_dest("my-asset")
    payload = build_catalog_payload("my-asset", 64, spec_meta)
    existing = {
        "slug": "my-asset",
        "id": "9",
        "updated_at": "2020-01-01T00:00:00Z",
        "world_sprite_path": payload["world_sprite_path"],
        "generator_archetype_id": payload["generator_archetype_id"],
    }
    responses.add(responses.POST, f"{BASE}/api/catalog/assets", status=409)
    responses.add(
        responses.GET,
        f"{BASE}/api/catalog/assets",
        json={"assets": [existing]},
        status=200,
    )
    push_catalog_for_promote("my-asset", 64)
    assert len(responses.calls) == 2


@responses.activate
def test_push_patches_on_409_drift(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("TG_CATALOG_API_URL", BASE)
    spec_meta = _load_spec_meta_for_dest("my-asset")
    payload = build_catalog_payload("my-asset", 64, spec_meta)
    drift = {
        "slug": "my-asset",
        "id": "9",
        "updated_at": "2020-01-01T00:00:00Z",
        "world_sprite_path": "Assets/other.png",
        "generator_archetype_id": payload["generator_archetype_id"],
    }
    responses.add(responses.POST, f"{BASE}/api/catalog/assets", status=409)
    responses.add(
        responses.GET,
        f"{BASE}/api/catalog/assets",
        json={"assets": [drift]},
        status=200,
    )
    responses.add(responses.PATCH, f"{BASE}/api/catalog/assets/9", json={"ok": True}, status=200)
    push_catalog_for_promote("my-asset", 64)
    assert any(c.request.method == "PATCH" for c in responses.calls)


def test_rows_match_helper() -> None:
    p = {"world_sprite_path": "A", "generator_archetype_id": "g"}
    assert rows_match_for_idempotency(p, p)
    assert not rows_match_for_idempotency({**p, "world_sprite_path": "B"}, p)
