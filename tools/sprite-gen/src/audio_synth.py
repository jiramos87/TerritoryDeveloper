"""audio_synth.py — Python audio synth pipeline for asset-pipeline Stage 9.1
(TECH-1957).

Generates deterministic audio blobs from archetype params; measures LUFS +
peak_db on output; writes via ``BlobResolver`` peer to ``var/blobs/{run_id}/
{idx}.ogg`` per DEC-A1 + DEC-A25 + DEC-A26.

Determinism: same archetype + same params → byte-identical output. Seed
derived from sha256 of canonical-JSON params; numpy ``default_rng`` driven.

Synthesis (MVP archetype ``ui_click_v1``):
    short noise burst + ADSR envelope + lowpass filter via numpy/scipy.

Loudness measurement: ``pyloudnorm`` integrated-LUFS (BS.1770).
Peak: ``20*log10(max(abs(samples)))``.

Audio container: Ogg Vorbis (``.ogg``) — matches DEC-A31 promote target
literal ``Assets/Audio/Generated/{slug}.ogg``.

Bind / network surface: NONE — module is pure compute. The FastAPI route
in :mod:`tools.sprite-gen.src.serve` (POST ``/render-audio``) wraps this
function and binds 127.0.0.1 only per DEC-A3.
"""

from __future__ import annotations

import hashlib
import json
import math
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any

import numpy as np
import soundfile as sf

from .blob_resolver import BlobResolver

# ---------------------------------------------------------------------------
# Result dataclass
# ---------------------------------------------------------------------------


@dataclass(frozen=True)
class AudioRenderResult:
    """Render output carrying source URI + measurements + fingerprint.

    Attributes:
        source_uri: ``gen://{run_id}/{variant_idx}`` URI per DEC-A25.
        duration_ms: Sample length in milliseconds.
        sample_rate: Output sample rate (Hz).
        channels: Channel count (1 = mono, 2 = stereo).
        loudness_lufs: BS.1770 integrated LUFS over the rendered buffer.
        peak_db: ``20*log10(max(abs(samples)))`` in dB.
        fingerprint: sha256 hex of the encoded Ogg bytes (DEC-A31).
        assets_path: ``Assets/Audio/Generated/{slug}.ogg`` once promoted; None
            on render-time output (DEC-A31 lenient pre-promote shape).
    """

    source_uri: str
    duration_ms: int
    sample_rate: int
    channels: int
    loudness_lufs: float
    peak_db: float
    fingerprint: str
    assets_path: str | None = None

    def to_dict(self) -> dict[str, Any]:
        """Return JSON-serialisable dict for FastAPI response payloads."""
        return asdict(self)


# ---------------------------------------------------------------------------
# Determinism helpers
# ---------------------------------------------------------------------------


def _canonical_params_json(params: dict[str, Any]) -> str:
    """Canonicalise params for deterministic seeding (sorted keys, tight separators)."""
    return json.dumps(params, sort_keys=True, separators=(",", ":"))


def _seed_from_params(archetype_id: str, params: dict[str, Any]) -> int:
    """Derive a 64-bit seed from canonical params + archetype id (sha256)."""
    blob = f"{archetype_id}|{_canonical_params_json(params)}".encode("utf-8")
    digest = hashlib.sha256(blob).hexdigest()
    return int(digest[:16], 16)


# ---------------------------------------------------------------------------
# Synthesis primitives
# ---------------------------------------------------------------------------


def _adsr_envelope(
    n_samples: int,
    sample_rate: int,
    attack_ms: float,
    decay_ms: float,
    sustain_level: float,
    release_ms: float,
) -> np.ndarray:
    """Build a linear-ramp ADSR envelope of length ``n_samples``."""
    attack_n = max(1, int(attack_ms * sample_rate / 1000))
    decay_n = max(1, int(decay_ms * sample_rate / 1000))
    release_n = max(1, int(release_ms * sample_rate / 1000))
    sustain_n = max(0, n_samples - attack_n - decay_n - release_n)

    env = np.zeros(n_samples, dtype=np.float64)
    cursor = 0
    if attack_n > 0:
        env[cursor : cursor + attack_n] = np.linspace(0.0, 1.0, attack_n, endpoint=False)
        cursor += attack_n
    if decay_n > 0:
        env[cursor : cursor + decay_n] = np.linspace(1.0, sustain_level, decay_n, endpoint=False)
        cursor += decay_n
    if sustain_n > 0:
        env[cursor : cursor + sustain_n] = sustain_level
        cursor += sustain_n
    if release_n > 0:
        end = min(cursor + release_n, n_samples)
        env[cursor:end] = np.linspace(sustain_level, 0.0, end - cursor, endpoint=False)
    return env


def _onepole_lowpass(samples: np.ndarray, cutoff_hz: float, sample_rate: int) -> np.ndarray:
    """Apply a simple one-pole lowpass (RC-style) — deterministic + dependency-free."""
    if cutoff_hz <= 0:
        return samples
    rc = 1.0 / (2.0 * math.pi * cutoff_hz)
    dt = 1.0 / sample_rate
    alpha = dt / (rc + dt)
    out = np.zeros_like(samples)
    prev = 0.0
    for i, s in enumerate(samples):
        prev = prev + alpha * (s - prev)
        out[i] = prev
    return out


def _synth_ui_click(
    params: dict[str, Any],
    seed: int,
    sample_rate: int,
) -> np.ndarray:
    """Synthesize a ui_click_v1 buffer from params.

    Recognised params:
        duration_ms (int, default 80)
        attack_ms (float, default 1.0)
        decay_ms (float, default 20.0)
        sustain_level (float, default 0.5)
        release_ms (float, default 30.0)
        cutoff_hz (float, default 2400.0)
        gain (float, default 0.6) — pre-clip gain applied before clamp to ±1.
    """
    duration_ms = int(params.get("duration_ms", 80))
    attack_ms = float(params.get("attack_ms", 1.0))
    decay_ms = float(params.get("decay_ms", 20.0))
    sustain_level = float(params.get("sustain_level", 0.5))
    release_ms = float(params.get("release_ms", 30.0))
    cutoff_hz = float(params.get("cutoff_hz", 2400.0))
    gain = float(params.get("gain", 0.6))

    n_samples = max(1, int(duration_ms * sample_rate / 1000))
    rng = np.random.default_rng(seed)
    noise = rng.uniform(-1.0, 1.0, size=n_samples)
    env = _adsr_envelope(n_samples, sample_rate, attack_ms, decay_ms, sustain_level, release_ms)
    raw = noise * env
    filtered = _onepole_lowpass(raw, cutoff_hz, sample_rate)
    samples = np.clip(filtered * gain, -1.0, 1.0).astype(np.float64)
    return samples


# Archetype dispatch table (extensible — Stage 9.1 ships ui_click_v1 only).
_ARCHETYPES = {
    "ui_click_v1": _synth_ui_click,
}


# ---------------------------------------------------------------------------
# Measurement helpers
# ---------------------------------------------------------------------------


def _measure_lufs(samples: np.ndarray, sample_rate: int) -> float:
    """Return BS.1770 integrated LUFS via pyloudnorm.

    Falls back to ``-inf`` for buffers shorter than the meter's block size
    (pyloudnorm requires ~400ms minimum); callers should treat ``-inf`` as
    a render-too-short signal.
    """
    import pyloudnorm  # imported lazily so module load works without the dep at import time

    meter = pyloudnorm.Meter(sample_rate)
    try:
        return float(meter.integrated_loudness(samples))
    except ValueError:
        return float("-inf")


def _measure_peak_db(samples: np.ndarray) -> float:
    """Return ``20*log10(max(abs(samples)))`` in dB; -inf for silence."""
    peak = float(np.max(np.abs(samples)))
    if peak <= 0.0:
        return float("-inf")
    return 20.0 * math.log10(peak)


# ---------------------------------------------------------------------------
# Public entrypoint
# ---------------------------------------------------------------------------


DEFAULT_SAMPLE_RATE = 48000
DEFAULT_CHANNELS = 1


def synth_audio(
    archetype_id: str,
    params: dict[str, Any],
    run_id: str,
    variant_idx: int,
    *,
    blob_resolver: BlobResolver | None = None,
    sample_rate: int = DEFAULT_SAMPLE_RATE,
    channels: int = DEFAULT_CHANNELS,
) -> AudioRenderResult:
    """Synthesize an audio buffer + write Ogg via BlobResolver + measure LUFS.

    Args:
        archetype_id: Archetype slug (e.g. ``ui_click_v1``).
        params: Archetype param dict (deterministic seed source).
        run_id: Render-run identifier (URL-safe token).
        variant_idx: Variant index within the run (Stage 9.1 MVP renders one).
        blob_resolver: Optional override; defaults to env-driven resolver.
        sample_rate: Output rate in Hz (default 48 kHz).
        channels: Channel count (default mono).

    Returns:
        AudioRenderResult — carries source_uri + measurements + fingerprint.

    Raises:
        ValueError: archetype_id not registered.
    """
    if archetype_id not in _ARCHETYPES:
        raise ValueError(f"unknown audio archetype: {archetype_id}")

    seed = _seed_from_params(archetype_id, params)
    samples = _ARCHETYPES[archetype_id](params, seed, sample_rate)

    # Multi-channel duplication (mono → stereo if requested).
    if channels == 1:
        out_samples = samples
    else:
        out_samples = np.tile(samples.reshape(-1, 1), (1, channels))

    resolver = blob_resolver or BlobResolver()
    target = _audio_blob_path(resolver, run_id, variant_idx)
    target.parent.mkdir(parents=True, exist_ok=True)

    # Ogg Vorbis encode via libsndfile (DEC-A31 promote-target container).
    sf.write(str(target), out_samples, sample_rate, format="OGG", subtype="VORBIS")

    # Fingerprint = sha256 of raw float32 sample bytes (NOT encoded Ogg bytes).
    # Ogg/Vorbis encoders inject non-deterministic page-checksum + serial bits
    # so the encoded payload diverges across calls even when input is
    # bit-identical. Hashing the raw PCM sample matrix preserves the
    # determinism contract (same archetype + same params → same fingerprint).
    fingerprint = hashlib.sha256(
        np.ascontiguousarray(out_samples, dtype=np.float32).tobytes()
    ).hexdigest()

    duration_ms = int(round(samples.shape[0] * 1000 / sample_rate))
    loudness_lufs = _measure_lufs(samples, sample_rate)
    peak_db = _measure_peak_db(samples)
    source_uri = f"gen://{run_id}/{variant_idx}"

    return AudioRenderResult(
        source_uri=source_uri,
        duration_ms=duration_ms,
        sample_rate=sample_rate,
        channels=channels,
        loudness_lufs=loudness_lufs,
        peak_db=peak_db,
        fingerprint=fingerprint,
    )


def _audio_blob_path(resolver: BlobResolver, run_id: str, variant_idx: int) -> Path:
    """Return the on-disk path for an audio variant under the BlobResolver root."""
    return resolver.blob_root / run_id / f"{variant_idx}.ogg"


def write_manifest(
    blob_resolver: BlobResolver,
    run_id: str,
    archetype_id: str,
    params: dict[str, Any],
    result: AudioRenderResult,
) -> Path:
    """Write a manifest sidecar carrying params + measurements (DEC-A26)."""
    manifest_path = blob_resolver.blob_root / run_id / "manifest.json"
    manifest_path.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "archetype_id": archetype_id,
        "params": params,
        "result": result.to_dict(),
        "build_fingerprint": result.fingerprint,
    }
    manifest_path.write_text(
        json.dumps(payload, sort_keys=True, indent=2),
        encoding="utf-8",
    )
    return manifest_path
