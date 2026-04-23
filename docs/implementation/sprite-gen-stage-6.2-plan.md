# sprite-gen — Stage 6.2 Plan Digest

Compiled 2026-04-23 from 5 task spec(s): **TECH-704** .. **TECH-708**.

**Master plan:** `ia/projects/sprite-gen-master-plan.md` — Stage 6.2 — Art Signatures per class.

**Status:** Closed 2026-04-23. See `BACKLOG-ARCHIVE.md` rows for TECH-704 / TECH-705 / TECH-706 / TECH-707 / TECH-708 (archived yaml under `ia/backlog-archive/`); spec files deleted at closeout.

---

## Stage exit criteria (orchestrator)

- `tools/sprite-gen/src/signature.py` — `compute_signature`, `validate_against`, `SignatureStaleError`; L15 sample-size branches (0 → fallback, 1 → point-match, ≥2 → envelope); L3 staleness guard on `source_checksum`.
- CLI `python3 -m src refresh-signatures [class?]` regenerates JSON; `signatures/_fallback.json` + `signatures/residential_small.signature.json` committed.
- Spec loader surfaces `include_in_signature: bool` (default `True`).
- `tests/test_signature_calibration.py` parametrized over `signatures/*.signature.json`; `tests/test_scale_calibration.py` retired.
- DAS §2.6 forward-pointer to signatures dir + module.
- `pytest tools/sprite-gen/tests/` exits 0 — 221+ tests green.

---

## §Plan Digest — TECH-704 (excerpt)

### §Goal

Ship `tools/sprite-gen/src/signature.py` exposing `compute_signature`, `validate_against`, `SignatureStaleError` with L15 sample-size branching (0 → fallback, 1 → point-match, ≥2 → envelope) and L3 staleness guard on `source_checksum`.

### §Mechanical Steps (summary)

1. Module skeleton + JSON shape + checksum helper.
2. Per-sprite measurement extractors (bbox, palette, silhouette, ground, decoration hints).
3. L15 branches in `_summarize`.
4. `validate_against` + `SignatureStaleError`. Gate: `pytest tests/test_signature.py -q` + full suite.

---

## §Plan Digest — TECH-705 (excerpt)

### §Goal

Ship the operator surface: CLI `python3 -m src refresh-signatures [class?]`, `signatures/` dir, `_fallback.json` graph, and bootstrap `residential_small.signature.json` (envelope mode). All JSON committed so CI reads same snapshot.

### §Mechanical Steps (summary)

1. Wire `refresh-signatures` subparser in `src/__main__.py`.
2. Scaffold `signatures/_fallback.json`.
3. Bootstrap `signatures/residential_small.signature.json`; assert `mode == envelope`.
4. Full-suite regression.

---

## §Plan Digest — TECH-706 (excerpt)

### §Goal

Surface optional top-level `include_in_signature: bool` (default `True`) in `spec.py::load_spec` so signature ingestion can skip opted-out specs without touching render output.

### §Mechanical Steps (summary)

1. `data.setdefault("include_in_signature", True)` after yaml load.
2. Unit tests for default + explicit `False`.
3. Full-suite regression.

---

## §Plan Digest — TECH-707 (excerpt)

### §Goal

Replace `tests/test_scale_calibration.py` with signature-driven parametrized calibration that auto-covers every class whose signature JSON lands in `tools/sprite-gen/signatures/`.

### §Mechanical Steps (summary)

1. Author `tests/test_signature_calibration.py` with `_signature_paths()` helper + parametrized `test_class_calibration`.
2. Retire `tests/test_scale_calibration.py` (delete file).
3. Full-suite regression.

---

## §Plan Digest — TECH-708 (excerpt)

### §Goal

Insert DAS §2.6 — one-paragraph forward-pointer to `tools/sprite-gen/signatures/` + `src/signature.py` as the canonical runtime calibration source.

### §Mechanical Steps (summary)

1. Insert §2.6 block between §2.5 and §3.
2. Confirm grep hit on section heading + `signatures/` pointer.

---

## Dependency graph

- TECH-704 — depends on TECH-701 + TECH-702 + TECH-703 (Stage 6.1 gate per L12).
- TECH-705 — depends on TECH-704 (CLI consumes module).
- TECH-706 — depends on TECH-704 (signature module reads the flag).
- TECH-707 — depends on TECH-704 + TECH-705 (needs module + signatures dir populated).
- TECH-708 — depends on TECH-704 (doc points at existing module).

## Locks consumed

- **L2** — Calibration = summarized Art Signatures per class; runtime never reads raw sprites. **Consumed by:** TECH-704.
- **L3** — Signature JSON carries `source_checksum`; stale raises actionable refresh message. **Consumed by:** TECH-704.
- **L4** — Spec YAML accepts `include_in_signature: false` per-sprite override. **Consumed by:** TECH-706.
- **L15** — Signature policy by catalog size: 0 → fallback, 1 → point-match, ≥2 → envelope. **Consumed by:** TECH-704 (+ TECH-705 via `_fallback.json`).
