# Frozen JSONL calibration corpus

Freeze date: 2026-05-12

DB mirror table: `ia_ui_calibration_verdict` (migration 0157)

Source files migrated:
- `ia/state/ui-calibration-verdicts.jsonl` — 7 rows
- `ia/state/ui-calibration-corpus.jsonl` — 14 rows

Total migrated: 21 rows

Migration script: `tools/scripts/migrate-calibration-jsonl-to-db.mjs`

Run `node tools/scripts/migrate-calibration-jsonl-to-db.mjs --dry-run` to verify parity.
Run `node tools/scripts/migrate-calibration-jsonl-to-db.mjs --apply` to re-apply (idempotent).

Live JSONL source files remain at `ia/state/` for read compatibility during transition.
Full retirement of JSONL read path deferred to UI Toolkit migration plan (Q6 decision).
