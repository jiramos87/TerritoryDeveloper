"""E2E promote + catalog (TECH-679)."""

from __future__ import annotations

import responses

from src import cli as _cli
from src import curate


@responses.activate
def test_smoke_post_counts(monkeypatch, tmp_path) -> None:
    base = "http://snap.example"
    monkeypatch.setenv("TG_CATALOG_API_URL", base)
    monkeypatch.setattr(_cli, "_OUT_DIR", tmp_path)
    monkeypatch.setattr(curate, "GENERATED_DIR", tmp_path / "Gen")
    responses.add(responses.POST, f"{base}/api/catalog/assets", json={"id": "1"}, status=201)

    assert _cli.main(["render", "building_residential_small"]) == 0
    pngs = list(tmp_path.glob("building_residential_small_v*.png"))
    assert pngs
    rc = _cli.main(["promote", str(pngs[0]), "--as", "residential-small-01"])
    assert rc == 0
    posts = [c for c in responses.calls if c.request.url.endswith("/api/catalog/assets") and c.request.method == "POST"]
    assert len(posts) == 1


@responses.activate
def test_smoke_no_push_zero_http(monkeypatch, tmp_path) -> None:
    base = "http://snap.example"
    monkeypatch.setenv("TG_CATALOG_API_URL", base)
    monkeypatch.setattr(_cli, "_OUT_DIR", tmp_path)
    monkeypatch.setattr(curate, "GENERATED_DIR", tmp_path / "Gen")
    responses.add(responses.POST, f"{base}/api/catalog/assets", json={"id": "1"}, status=201)

    assert _cli.main(["render", "building_residential_small"]) == 0
    pngs = list(tmp_path.glob("building_residential_small_v*.png"))
    assert _cli.main(["promote", str(pngs[0]), "--as", "x-no-push", "--no-push"]) == 0
    assert not responses.calls
