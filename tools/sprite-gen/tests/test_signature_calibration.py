"""Parametrized signature-calibration regression (TECH-707).

For every `tools/sprite-gen/signatures/*.signature.json`, load the canonical
class spec, render via `compose_sprite`, and assert the rendered sprite
falls inside the signature envelope.

Replaces the ad-hoc `test_scale_calibration.py` regression — tight bounds
now live in the signature JSON, so new classes auto-join the regression
as their signature files land.
"""

from __future__ import annotations

import json
from pathlib import Path

import pytest

from src.compose import compose_sprite
from src.signature import ValidationReport, validate_against
from src.spec import load_spec

REPO_ROOT = Path(__file__).resolve().parents[3]
SIGNATURES_DIR = REPO_ROOT / "tools/sprite-gen/signatures"
SPECS_DIR = REPO_ROOT / "tools/sprite-gen/specs"

# Class -> canonical spec filename (stem) under tools/sprite-gen/specs/.
_CANONICAL_SPEC: dict[str, str] = {
    "residential_small": "building_residential_small",
}


def _signature_files() -> list[Path]:
    if not SIGNATURES_DIR.is_dir():
        return []
    return sorted(p for p in SIGNATURES_DIR.glob("*.signature.json"))


@pytest.mark.parametrize(
    "sig_path",
    _signature_files(),
    ids=lambda p: p.stem.replace(".signature", ""),
)
def test_signature_calibration(sig_path: Path) -> None:
    signature = json.loads(sig_path.read_text(encoding="utf-8"))
    cls = signature["class"]
    spec_stem = _CANONICAL_SPEC.get(cls)
    if spec_stem is None:
        pytest.skip(f"no canonical spec registered for class {cls!r}")

    spec_path = SPECS_DIR / f"{spec_stem}.yaml"
    spec = load_spec(spec_path)
    rendered = compose_sprite(spec)
    report: ValidationReport = validate_against(signature, rendered)
    assert report.ok, (
        f"signature validation failed for {cls}: {report.failures}"
    )
