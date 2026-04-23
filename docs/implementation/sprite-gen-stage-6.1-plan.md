# sprite-gen — Stage 6.1 Plan Digest

Compiled 2026-04-23 from 3 task spec(s): **TECH-701** .. **TECH-703**.

**Master plan:** `ia/projects/sprite-gen-master-plan.md` — Stage 6.1 — Pivot hotfix + regression tighten.

**Status:** Closed 2026-04-23. See `BACKLOG-ARCHIVE.md` rows for TECH-701 / TECH-702 / TECH-703 (archived yaml under `ia/backlog-archive/`); spec files deleted at closeout.

---

## Stage exit criteria (orchestrator)

- `pivot_pad = 17 if spec.get("ground") != "none" else 0` at `compose.py:256` with DAS §2.1/§2.2 citation.
- `test_scale_calibration.py` asserts `y1 == 48` (exact) and `32 <= content_h <= 36`.
- `test_render_integration.py` parametrizes per-spec bbox regression over live 1×1 flat `specs/*.yaml`; each asserts `bbox == (0, 15, 64, 48)`.
- `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 221+ passed.

---

## §Plan Digest — TECH-701 (excerpt)

### §Goal

Lock the DAS §2.1/§2.2 citation on the in-session pivot hotfix at `tools/sprite-gen/src/compose.py:256`. Code is already live; this task prevents comment drift so future readers can trace `pivot_pad = 17` back to `canvas_h − 16 − 1 (inclusive pixel)`.

### §Mechanical Steps (summary)

1. Read `tools/sprite-gen/src/compose.py:253-262`; confirm comment cites DAS §2.1 + §2.2 and explains `17 = 16 + 1`.
2. If drifted, rewrite the 3-line comment above `pivot_pad = 17 …` to the canonical form (see TECH-701 §Examples).
3. Gate: `cd tools/sprite-gen && python3 -m pytest tests/ -q` → 218+ passed.

---

## §Plan Digest — TECH-702 (excerpt)

### §Goal

Lock the DAS §2.3 House1-64 envelope into `test_scale_calibration.py` as exact (`y1 == 48`) + narrow (`content_h ∈ [32, 36]`) assertions so any pivot drift ≥3 px fails CI on the next run.

### §Mechanical Steps (summary)

1. Replace `test_residential_small_bbox_y0_in_envelope` (loose `10 <= y0 <= 16`) with two tight functions: `test_residential_small_bbox_y1_diamond_bottom` (`y1 == 48`) and `test_residential_small_bbox_content_h_envelope` (`32 <= content_h <= 36`).
2. Cite DAS §2.3 in each docstring.
3. Gate: `cd tools/sprite-gen && python3 -m pytest tests/test_scale_calibration.py -q` then full suite `tests/ -q` → 218+ passed.

---

## §Plan Digest — TECH-703 (excerpt)

### §Goal

Close I2: every live flat 1×1 spec in `tools/sprite-gen/specs/` must render with bbox `(0, 15, 64, 48)`; assert via pytest parametrize so new specs get coverage for free.

### §Mechanical Steps (summary)

1. Add `_live_1x1_flat_specs()` helper to `tests/test_render_integration.py` that globs `specs/*.yaml`, filters `footprint == [1,1]` AND `terrain in (None, "flat")`.
2. Add `test_every_live_1x1_spec_bbox` parametrized with the helper; assert `compose_sprite(load_spec(spec_path)).getbbox() == (0, 15, 64, 48)`.
3. Gate: `cd tools/sprite-gen && python3 -m pytest tests/test_render_integration.py -q` then full suite `tests/ -q` → 221+ passed (218 baseline + ≥3 new parametrized cases, 4 today).

---

## Dependency graph

- TECH-701 — no deps (retroactive wording lock).
- TECH-702 — depends on TECH-701 (comment wording locked before assertion tighten).
- TECH-703 — depends on TECH-701 (same reason); related TECH-702 (sibling regression).

## Issues closed by this stage

- **I1** — pivot hotfix uncommented / unreferenced in DAS citation trail → closed by TECH-701.
- **I2** — regression hole: loose `y0` bound + no per-spec bbox coverage → closed by TECH-702 + TECH-703.
