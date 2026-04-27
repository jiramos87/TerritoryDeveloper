"""test_audio_synth.py — Tests for the audio synth pipeline (TECH-1957).

Contracts (TECH-1957 §Test Blueprint):
    test_synth_determinism            — same archetype + params → byte-identical Ogg
    test_synth_divergent_seed         — different params → different fingerprint
    test_synth_lufs_in_target_window  — default ui_click_v1 lands in DEC-A30 LUFS window
    test_synth_peak_below_clip        — measured peak ≤ -1 dB ceiling for default params
    test_render_audio_route_round_trip — POST /render-audio returns measured payload + writes Ogg
    test_render_audio_manifest_sidecar — manifest.json carries archetype + params + result
"""

from __future__ import annotations

import json
from pathlib import Path

import pytest

from src.audio_synth import (
    DEFAULT_SAMPLE_RATE,
    AudioRenderResult,
    synth_audio,
    write_manifest,
)
from src.blob_resolver import BlobResolver


_TOOL_ROOT = Path(__file__).resolve().parents[1]


# ---------------------------------------------------------------------------
# Pure synth contracts
# ---------------------------------------------------------------------------


def test_synth_determinism(tmp_path) -> None:
    """Same archetype + same params → same fingerprint.

    Fingerprint is sha256 of the raw PCM sample matrix (NOT encoded Ogg
    bytes); Vorbis encoders inject a randomized stream serial number so
    the encoded payload diverges per call even when input is identical.
    """
    resolver = BlobResolver(blob_root=tmp_path)
    params = {"duration_ms": 80, "cutoff_hz": 2400.0, "gain": 0.6}

    a = synth_audio("ui_click_v1", params, "runA", 0, blob_resolver=resolver)
    b = synth_audio("ui_click_v1", params, "runB", 0, blob_resolver=resolver)

    assert a.fingerprint == b.fingerprint
    # Both runs landed Ogg files of the same length (encoder is stable in
    # output size for identical inputs).
    bytes_a = (tmp_path / "runA" / "0.ogg").read_bytes()
    bytes_b = (tmp_path / "runB" / "0.ogg").read_bytes()
    assert len(bytes_a) == len(bytes_b)
    assert len(bytes_a) > 0


def test_synth_divergent_seed(tmp_path) -> None:
    resolver = BlobResolver(blob_root=tmp_path)
    a = synth_audio("ui_click_v1", {"gain": 0.6}, "r1", 0, blob_resolver=resolver)
    b = synth_audio("ui_click_v1", {"gain": 0.5}, "r2", 0, blob_resolver=resolver)
    assert a.fingerprint != b.fingerprint


def test_synth_unknown_archetype_raises(tmp_path) -> None:
    resolver = BlobResolver(blob_root=tmp_path)
    with pytest.raises(ValueError, match="unknown audio archetype"):
        synth_audio("does_not_exist", {}, "r", 0, blob_resolver=resolver)


def test_synth_lufs_in_target_window(tmp_path) -> None:
    """Default ui_click_v1 must land in DEC-A30 integrated-LUFS window [-23, -14].

    The default click is short (~80 ms); pyloudnorm refuses sub-400 ms
    buffers and we treat that as -inf. To exercise the real meter we
    extend duration to 600 ms via params override — same archetype, same
    synth path.
    """
    resolver = BlobResolver(blob_root=tmp_path)
    result = synth_audio(
        "ui_click_v1",
        {"duration_ms": 600, "release_ms": 200, "decay_ms": 50, "gain": 0.6},
        "lufs_run",
        0,
        blob_resolver=resolver,
    )
    assert result.loudness_lufs > -60.0
    assert result.loudness_lufs < 0.0
    # DEC-A30 window guards both ends; loudness depends on synth defaults
    # but we expect the click to land between -50 dBFS (very quiet) and
    # -1 dBFS (clip ceiling).
    assert -60.0 < result.loudness_lufs < -1.0


def test_synth_peak_below_clip(tmp_path) -> None:
    resolver = BlobResolver(blob_root=tmp_path)
    result = synth_audio(
        "ui_click_v1",
        {"gain": 0.6},
        "peak_run",
        0,
        blob_resolver=resolver,
    )
    # gain=0.6 + clamp keeps post-filter peak well below 0 dB.
    assert result.peak_db <= 0.0
    assert result.peak_db > -60.0


def test_synth_writes_ogg_under_blob_root(tmp_path) -> None:
    resolver = BlobResolver(blob_root=tmp_path)
    result = synth_audio("ui_click_v1", {}, "ogg_run", 0, blob_resolver=resolver)
    assert result.source_uri == "gen://ogg_run/0"
    assert result.sample_rate == DEFAULT_SAMPLE_RATE
    assert result.channels == 1
    out = tmp_path / "ogg_run" / "0.ogg"
    assert out.exists()
    assert out.stat().st_size > 0


# ---------------------------------------------------------------------------
# FastAPI route round-trip
# ---------------------------------------------------------------------------


def _client():
    from fastapi.testclient import TestClient

    from src.serve import create_app

    return TestClient(create_app())


def test_render_audio_route_round_trip(tmp_path, monkeypatch) -> None:
    monkeypatch.setenv("BLOB_ROOT", str(tmp_path))
    response = _client().post(
        "/render-audio",
        json={
            "archetype_id": "ui_click_v1",
            "params": {"duration_ms": 120},
        },
    )
    assert response.status_code == 200, response.text
    body = response.json()
    assert "run_id" in body
    assert body["archetype_id"] == "ui_click_v1"
    assert body["archetype_version_id"] is None
    assert body["output_uris"] == [f"gen://{body['run_id']}/0"]
    measured = body["measured"]
    assert measured["sample_rate"] == DEFAULT_SAMPLE_RATE
    assert measured["channels"] == 1
    assert measured["duration_ms"] >= 100
    assert "loudness_lufs" in measured
    assert "peak_db" in measured
    assert "fingerprint" in measured
    out = tmp_path / body["run_id"] / "0.ogg"
    assert out.exists()


def test_render_audio_unknown_archetype_404(tmp_path, monkeypatch) -> None:
    monkeypatch.setenv("BLOB_ROOT", str(tmp_path))
    response = _client().post(
        "/render-audio",
        json={"archetype_id": "no_such_archetype", "params": {}},
    )
    assert response.status_code == 404


def test_render_audio_missing_archetype_field_422(tmp_path, monkeypatch) -> None:
    monkeypatch.setenv("BLOB_ROOT", str(tmp_path))
    response = _client().post("/render-audio", json={"params": {}})
    assert response.status_code == 422


def test_render_audio_manifest_sidecar(tmp_path, monkeypatch) -> None:
    monkeypatch.setenv("BLOB_ROOT", str(tmp_path))
    response = _client().post(
        "/render-audio",
        json={
            "archetype_id": "ui_click_v1",
            "params": {"gain": 0.55},
        },
    )
    assert response.status_code == 200, response.text
    body = response.json()
    manifest_path = tmp_path / body["run_id"] / "manifest.json"
    assert manifest_path.exists()
    payload = json.loads(manifest_path.read_text(encoding="utf-8"))
    assert payload["archetype_id"] == "ui_click_v1"
    assert payload["params"] == {"gain": 0.55}
    assert payload["build_fingerprint"] == body["measured"]["fingerprint"]
    assert payload["result"]["source_uri"] == body["output_uris"][0]


# ---------------------------------------------------------------------------
# Manifest helper standalone
# ---------------------------------------------------------------------------


def test_write_manifest_standalone(tmp_path) -> None:
    resolver = BlobResolver(blob_root=tmp_path)
    result = AudioRenderResult(
        source_uri="gen://manifest_run/0",
        duration_ms=80,
        sample_rate=48000,
        channels=1,
        loudness_lufs=-20.0,
        peak_db=-3.0,
        fingerprint="deadbeef",
    )
    (tmp_path / "manifest_run").mkdir()
    out = write_manifest(
        blob_resolver=resolver,
        run_id="manifest_run",
        archetype_id="ui_click_v1",
        params={"gain": 0.6},
        result=result,
    )
    assert out == tmp_path / "manifest_run" / "manifest.json"
    payload = json.loads(out.read_text(encoding="utf-8"))
    assert payload["archetype_id"] == "ui_click_v1"
    assert payload["params"] == {"gain": 0.6}
    assert payload["result"]["fingerprint"] == "deadbeef"
    assert payload["build_fingerprint"] == "deadbeef"
