"""test_blob_resolver.py — Tests for the Python half of BlobResolver
(TECH-1435).

Contracts (TECH-1435 §Test Blueprint):
    test_resolves_gen_uri_to_default_root
    test_blob_root_env_override
    test_rejects_non_gen_scheme
    test_rejects_malformed_gen_uri
    test_read_returns_file_bytes
    test_bootstrap_script_idempotent
"""

from __future__ import annotations

import os
import subprocess
from pathlib import Path

import pytest

from src.blob_resolver import (
    BlobResolver,
    MalformedBlobUriError,
    UnsupportedSchemeError,
)


_TOOL_ROOT = Path(__file__).resolve().parents[1]
_REPO_ROOT = _TOOL_ROOT.parent.parent


def test_resolves_gen_uri_to_default_root(monkeypatch) -> None:
    monkeypatch.delenv("BLOB_ROOT", raising=False)
    resolver = BlobResolver()
    out = resolver.resolve("gen://abc/3")
    assert out.name == "3.png"
    assert out.parent.name == "abc"
    # Default root must end at repo-root/var/blobs (cwd-independent).
    assert out.parents[1].name == "blobs"
    assert out.parents[2].name == "var"


def test_blob_root_env_override(monkeypatch, tmp_path) -> None:
    monkeypatch.setenv("BLOB_ROOT", str(tmp_path))
    resolver = BlobResolver()
    out = resolver.resolve("gen://abc/0")
    assert out == tmp_path / "abc" / "0.png"


def test_explicit_constructor_arg(tmp_path) -> None:
    resolver = BlobResolver(blob_root=tmp_path)
    out = resolver.resolve("gen://run/4")
    assert out == tmp_path / "run" / "4.png"


def test_rejects_non_gen_scheme(tmp_path) -> None:
    resolver = BlobResolver(blob_root=tmp_path)
    with pytest.raises(UnsupportedSchemeError):
        resolver.resolve("s3://bucket/key")


def test_rejects_malformed_gen_uri(tmp_path) -> None:
    resolver = BlobResolver(blob_root=tmp_path)
    with pytest.raises(MalformedBlobUriError):
        resolver.resolve("gen://only-run-id")
    with pytest.raises(MalformedBlobUriError):
        resolver.resolve("gen://run/notanumber")


def test_read_returns_file_bytes(monkeypatch, tmp_path) -> None:
    monkeypatch.setenv("BLOB_ROOT", str(tmp_path))
    run_dir = tmp_path / "run42"
    run_dir.mkdir()
    payload = b"\x89PNG\r\n"
    (run_dir / "0.png").write_bytes(payload)
    resolver = BlobResolver()
    assert resolver.read("gen://run42/0") == payload


def test_bootstrap_script_idempotent(tmp_path) -> None:
    """Run the bootstrap script twice; second invocation must be a no-op.

    We exec the real script under the real repo to keep the contract honest;
    the two invocations should be byte-identical post-conditions:
        - var/blobs/ exists
        - .gitignore carries `var/blobs/` exactly once
    """
    script = _REPO_ROOT / "tools" / "scripts" / "bootstrap-blob-root.sh"
    blob_root = _REPO_ROOT / "var" / "blobs"
    gitignore = _REPO_ROOT / ".gitignore"

    assert script.exists()
    # First run.
    subprocess.run(["bash", str(script)], cwd=str(_REPO_ROOT), check=True)
    first_gitignore = gitignore.read_text(encoding="utf-8")
    first_blob_root_exists = blob_root.is_dir()
    first_gitkeep = (blob_root / ".gitkeep").is_file()
    # Second run.
    subprocess.run(["bash", str(script)], cwd=str(_REPO_ROOT), check=True)
    second_gitignore = gitignore.read_text(encoding="utf-8")
    assert first_blob_root_exists
    assert first_gitkeep
    assert first_gitignore == second_gitignore
    # `var/blobs/` line appears exactly once.
    matches = [
        line for line in second_gitignore.splitlines() if line == "var/blobs/"
    ]
    assert len(matches) == 1
