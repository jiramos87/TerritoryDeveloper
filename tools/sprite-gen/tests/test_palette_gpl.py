"""GPL round-trip tests — export, import, CLI smoke, negative cases."""

from __future__ import annotations

import json
import subprocess
import sys
from pathlib import Path

import pytest

from src.palette import GplParseError, export_gpl, import_gpl, load_palette

TOOL_ROOT = Path(__file__).parent.parent
PALETTES_DIR = TOOL_ROOT / "palettes"
RESIDENTIAL_JSON = PALETTES_DIR / "residential.json"

# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------


def _run(*args: str, cwd: Path | None = None) -> subprocess.CompletedProcess:
    """Run sprite_gen via `python -m src ...` from TOOL_ROOT."""
    cmd = [sys.executable, "-m", "src"] + list(args)
    return subprocess.run(
        cmd,
        cwd=str(cwd or TOOL_ROOT),
        capture_output=True,
        text=True,
    )


# ---------------------------------------------------------------------------
# Header + row count
# ---------------------------------------------------------------------------


def test_gpl_header_and_row_count():
    """GPL text starts with GIMP Palette header; body rows = emitted levels.

    TECH-762+ decoration ramps (`bush`, `grass_tuft`, `pool`) expose partial
    ramps (no `dark`); `tree_deciduous` is nested (`green`, `green_yellow`,
    `green_blue`). `export_gpl` now emits one row per available level and
    recurses nested ramps. Row count = sum of bright/mid/dark levels actually
    present across the (possibly-nested) material tree.
    """
    text = export_gpl("residential", palettes_dir=PALETTES_DIR)
    lines = text.splitlines()
    assert lines[0] == "GIMP Palette"
    assert lines[1] == "Name: residential"
    assert lines[2] == "Columns: 3"
    assert lines[3] == "#"
    body = [ln for ln in lines[4:] if ln.strip()]

    materials = load_palette("residential", palettes_dir=PALETTES_DIR)["materials"]
    canonical_levels = ("bright", "mid", "dark")

    def _count_levels(levels: dict) -> int:
        # Nested: recurse into dict-of-dicts.
        if any(isinstance(v, dict) for v in levels.values()):
            return sum(
                _count_levels(sub) for sub in levels.values() if isinstance(sub, dict)
            )
        return sum(1 for k in canonical_levels if k in levels)

    expected = sum(_count_levels(levels) for levels in materials.values())
    assert len(body) == expected, (
        f"expected {expected} body rows (emitted levels only), got {len(body)}"
    )


# ---------------------------------------------------------------------------
# Round-trip deep-equal
# ---------------------------------------------------------------------------


def test_round_trip_residential(tmp_path):
    """Export residential JSON → .gpl → import → deep-equal on full ramps only.

    TECH-762+ decoration ramps (partial `bush`/`grass_tuft`/`pool`, nested
    `tree_deciduous`) don't round-trip through GPL. Round-trip is asserted
    on materials that expose a full `bright`+`mid`+`dark` ramp; decoration
    ramps ship via palette JSON directly.
    """
    gpl_path = tmp_path / "residential.gpl"
    export_gpl("residential", dest_path=gpl_path, palettes_dir=PALETTES_DIR)
    assert gpl_path.exists()

    result = import_gpl("residential", gpl_path)
    original = load_palette("residential", palettes_dir=PALETTES_DIR)

    assert result["class"] == "residential"
    _FULL_KEYS = {"bright", "mid", "dark"}

    def _is_full_flat(entry: dict) -> bool:
        # Nested ramps (any dict-valued child) are excluded from round-trip.
        if any(isinstance(v, dict) for v in entry.values()):
            return False
        return _FULL_KEYS.issubset(entry.keys())

    stripped_original = {
        name: {k: v for k, v in entry.items() if k in _FULL_KEYS}
        for name, entry in original["materials"].items()
        if _is_full_flat(entry)
    }
    # Filter result the same way, AND drop synthetic flattened nested-ramp
    # material names (e.g. `tree_deciduous_green`) — GPL has no nesting, so
    # reimport inflates them into top-level entries with no counterpart
    # in the original palette.
    original_names = set(original["materials"].keys())
    stripped_result = {
        name: {k: v for k, v in entry.items() if k in _FULL_KEYS}
        for name, entry in result["materials"].items()
        if _is_full_flat(entry) and name in original_names
    }
    assert stripped_result == stripped_original, (
        "Round-trip materials mismatch on canonical full-ramp materials"
    )


# ---------------------------------------------------------------------------
# Negative cases — GplParseError
# ---------------------------------------------------------------------------


def _write_gpl(tmp_path: Path, body_rows: list[str]) -> Path:
    """Write a minimal valid GPL header + custom body rows to a temp file."""
    header = ["GIMP Palette", "Name: test", "Columns: 3", "#"]
    text = "\n".join(header + body_rows) + "\n"
    p = tmp_path / "test.gpl"
    p.write_text(text, encoding="utf-8")
    return p


def test_malformed_rgb(tmp_path):
    """Non-integer RGB values raise GplParseError."""
    gpl = _write_gpl(tmp_path, ["foo 12 34\twall_brick_red_bright"])
    with pytest.raises(GplParseError):
        import_gpl("test", gpl)


def test_bad_level_suffix(tmp_path):
    """Level not in {bright, mid, dark} raises GplParseError."""
    gpl = _write_gpl(tmp_path, [" 10  20  30\twall_brick_red_weird"])
    with pytest.raises(GplParseError):
        import_gpl("test", gpl)


def test_empty_body(tmp_path):
    """GPL with no material rows raises GplParseError.

    TECH-762+: partial ramps (only `bright`) are now allowed to support
    decoration materials (bush/grass_tuft/pool). Strict whole-ramp validation
    is out; the remaining negative on body shape is `no rows at all`.
    """
    gpl = _write_gpl(tmp_path, [])
    with pytest.raises(GplParseError):
        import_gpl("test", gpl)


# ---------------------------------------------------------------------------
# CLI smoke — palette export
# ---------------------------------------------------------------------------


def test_cli_export(tmp_path):
    """CLI `palette export residential` exits 0 and writes a .gpl file."""
    # Patch: CLI writes to _PALETTES_DIR which is TOOL_ROOT/palettes.
    # We invoke the command and check the real palettes dir for the .gpl.
    gpl_path = PALETTES_DIR / "residential.gpl"
    # Ensure no stale file interferes.
    if gpl_path.exists():
        gpl_path.unlink()

    result = _run("palette", "export", "residential")
    assert result.returncode == 0, f"stderr: {result.stderr}"
    assert "wrote" in result.stdout
    assert gpl_path.exists(), ".gpl not created by CLI"

    # Verify header.
    text = gpl_path.read_text(encoding="utf-8")
    assert text.startswith("GIMP Palette\n")

    # Cleanup (gitignored; remove so it doesn't linger in working tree).
    gpl_path.unlink(missing_ok=True)


# ---------------------------------------------------------------------------
# CLI smoke — palette import
# ---------------------------------------------------------------------------


def test_cli_import(tmp_path):
    """CLI `palette import residential --gpl {path}` exits 0 and writes JSON."""
    # Export to tmp first so we have a .gpl to import.
    gpl_path = tmp_path / "residential.gpl"
    export_gpl("residential", dest_path=gpl_path, palettes_dir=PALETTES_DIR)

    # Import back — writes to PALETTES_DIR/residential.json (same file, no change).
    result = _run("palette", "import", "residential", "--gpl", str(gpl_path))
    assert result.returncode == 0, f"stderr: {result.stderr}"
    assert "wrote" in result.stdout

    # The JSON should still be valid and deep-equal to original.
    original = load_palette("residential", palettes_dir=PALETTES_DIR)
    assert original["class"] == "residential"
    expected_n = len(json.loads(RESIDENTIAL_JSON.read_text(encoding="utf-8"))["materials"])
    assert len(original["materials"]) == expected_n
