"""blob_resolver.py — Python half of the canonical `gen://` BlobResolver
(TECH-1435).

Single swap point per DEC-A25; mirrors the TS contract under
`web/lib/blob-resolver.ts`. Future hosted blob stores plug in behind the
same `resolve()` / `read()` signatures via the `BLOB_ROOT` env var.
"""

from __future__ import annotations

import os
import re
from pathlib import Path
from typing import Optional


class UnsupportedSchemeError(ValueError):
    """Raised when the URI scheme is not `gen://`."""


class MalformedBlobUriError(ValueError):
    """Raised when the URI body fails to parse into `{run_id, variant_idx}`."""


_GEN_URI_RE = re.compile(r"^gen://([A-Za-z0-9_-]+)/(\d+)$")


def _default_blob_root() -> Path:
    """Resolve the repo-root `var/blobs/` dir cwd-independently.

    `tools/sprite-gen/src/blob_resolver.py` lives 3 dirs deep below the
    repo root, mirroring `cli.py`'s `_REPO_ROOT` pattern.
    """
    here = Path(__file__).resolve()
    repo_root = here.parents[3]
    return repo_root / "var" / "blobs"


class BlobResolver:
    """Translate `gen://{run_id}/{variant_idx}` URIs to local Paths.

    Args:
        blob_root: Optional override. Falls back to `BLOB_ROOT` env, then
            the default repo-local `var/blobs/` dir.
    """

    def __init__(self, blob_root: Optional[Path] = None) -> None:
        if blob_root is not None:
            self.blob_root = Path(blob_root).expanduser().resolve()
        elif os.environ.get("BLOB_ROOT"):
            self.blob_root = Path(os.environ["BLOB_ROOT"]).expanduser().resolve()
        else:
            self.blob_root = _default_blob_root()

    @classmethod
    def from_env(cls) -> "BlobResolver":
        """Convenience factory mirroring future env-driven config layers."""
        return cls()

    def resolve(self, uri: str) -> Path:
        """Resolve a `gen://` URI to an absolute on-disk Path.

        Raises:
            UnsupportedSchemeError: scheme is not `gen://`
            MalformedBlobUriError: URI body cannot be parsed
        """
        if not uri.startswith("gen://"):
            raise UnsupportedSchemeError(f"unsupported blob URI scheme: {uri}")
        match = _GEN_URI_RE.match(uri)
        if not match:
            raise MalformedBlobUriError(f"malformed gen:// URI: {uri}")
        run_id, variant_idx = match.group(1), match.group(2)
        return self.blob_root / run_id / f"{variant_idx}.png"

    def read(self, uri: str) -> bytes:
        """Return the bytes of a `gen://` URI; raises when the file is absent."""
        return self.resolve(uri).read_bytes()

    def resolve_audio(self, uri: str) -> Path:
        """Resolve a `gen://` URI to the audio (.ogg) blob path.

        Audio peer of :meth:`resolve` (sprite path emits `.png`); both share
        the `gen://{run_id}/{variant_idx}` URI shape per DEC-A25 + TECH-1957.
        """
        if not uri.startswith("gen://"):
            raise UnsupportedSchemeError(f"unsupported blob URI scheme: {uri}")
        match = _GEN_URI_RE.match(uri)
        if not match:
            raise MalformedBlobUriError(f"malformed gen:// URI: {uri}")
        run_id, variant_idx = match.group(1), match.group(2)
        return self.blob_root / run_id / f"{variant_idx}.ogg"

    def write_audio(
        self,
        run_id: str,
        variant_idx: int,
        samples: object,
        sample_rate: int,
    ) -> str:
        """Encode + write an audio buffer as Ogg Vorbis under `var/blobs/{run_id}/{idx}.ogg`.

        Returns the canonical `gen://{run_id}/{idx}` URI.

        ``samples`` can be a 1-D numpy float array (mono) or a 2-D
        ``(n, channels)`` array; libsndfile handles both shapes.
        """
        # soundfile is imported lazily so the module load order matches
        # blob_resolver's existing zero-dep posture.
        import soundfile as sf  # type: ignore[import-not-found]

        target = self.blob_root / run_id / f"{variant_idx}.ogg"
        target.parent.mkdir(parents=True, exist_ok=True)
        sf.write(str(target), samples, sample_rate, format="OGG", subtype="VORBIS")
        return f"gen://{run_id}/{variant_idx}"
