"""test_render_integration.py — Stage 1.2 end-to-end integration smoke test (TECH-152).

Contracts (§7b TECH-152):
    test_render_integration_smoke   — CLI → loader → compose → PNG chain (Goals 1–5)

Invokes `python -m src render building_residential_small` via subprocess so the real
`__main__.py` + argparse entry point is exercised — not an in-process import.

Output files written to the real `tools/sprite-gen/out/` directory (the CLI's
`_OUT_DIR` constant is tool-root-anchored and subprocess cannot see monkeypatch).
The `clean_residential_out` fixture pre-deletes only
`building_residential_small_v*.png` to avoid clobbering other archetype artifacts.
"""

from __future__ import annotations

import subprocess
import sys
from pathlib import Path

import pytest

# ---------------------------------------------------------------------------
# Skip guards
# ---------------------------------------------------------------------------

PIL = pytest.importorskip("PIL", reason="Pillow not installed — skip integration smoke")
from PIL import Image  # noqa: E402  (after importorskip guard)

# ---------------------------------------------------------------------------
# Fixture
# ---------------------------------------------------------------------------

_TOOL_ROOT = Path(__file__).resolve().parent.parent
_SPEC_FILE = _TOOL_ROOT / "specs" / "building_residential_small.yaml"


@pytest.fixture()
def clean_residential_out():
    """Delete any pre-existing building_residential_small_v*.png before test; yield tool_root."""
    if not _SPEC_FILE.exists():
        pytest.skip(
            f"Spec file missing: {_SPEC_FILE} — cannot run integration smoke (TECH-151 dep)."
        )
    out_dir = _TOOL_ROOT / "out"
    out_dir.mkdir(exist_ok=True)
    for p in out_dir.glob("building_residential_small_v*.png"):
        p.unlink()
    yield _TOOL_ROOT


# ---------------------------------------------------------------------------
# Test
# ---------------------------------------------------------------------------


def test_render_integration_smoke(clean_residential_out):
    """End-to-end: CLI writes 4 variant PNGs, each 64×64, rc == 0."""
    tool_root = clean_residential_out

    result = subprocess.run(
        [sys.executable, "-m", "src", "render", "building_residential_small"],
        cwd=str(tool_root),
        capture_output=True,
        text=True,
    )
    assert result.returncode == 0, result.stderr

    out_dir = tool_root / "out"
    pngs = sorted(out_dir.glob("building_residential_small_v*.png"))
    assert len(pngs) == 4, (
        f"Expected 4 variant PNGs, got {[p.name for p in pngs]}.\nstderr: {result.stderr}"
    )

    expected_names = [f"building_residential_small_v0{i}.png" for i in range(1, 5)]
    actual_names = [p.name for p in pngs]
    assert actual_names == expected_names, f"Unexpected PNG names: {actual_names}"

    for p in pngs:
        with Image.open(p) as img:
            assert img.size == (64, 64), f"{p.name}: expected (64, 64), got {img.size}"
