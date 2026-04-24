### Stage 13 — Mutations + Authorship + Bridge + Journal Lifecycle / Journal Lifecycle


**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement `journal_entry_sync` idempotent upsert via `content_hash` SHA-256 dedup; Postgres migration for `content_hash` column (3-step backfill); cascade-delete semantics on issue archive; `project_spec_closeout_digest` gains `journaled_sections` field.

**Exit:**

- `journal_entry_sync(issue_id, mode: "upsert", body)` called twice with same body → one DB row (dedup via `content_hash`).
- `journal_entry_sync(issue_id, mode: "delete", cascade: true)` removes all rows for issue.
- Migration `add-journal-content-hash.ts` idempotent on re-run (second run = no-op if column exists).
- `project_spec_closeout_digest` response includes `journaled_sections: string[]`.
- `closeout` skill body updated to call `journal_entry_sync` instead of `project_spec_journal_persist`.
- Tests green.
- Phase 1 — Idempotent sync + migration.
- Phase 2 — Closeout digest + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | journal_entry_sync | _pending_ | _pending_ | Implement `journal_entry_sync(issue_id, mode: "upsert" | "delete", body?, cascade?: bool)` in `project-spec-journal.ts` via `wrapTool`: upsert path: compute `SHA256(issue_id + kind + body)` as `content_hash`, `INSERT ... ON CONFLICT (content_hash) DO NOTHING`; delete+cascade path: `DELETE WHERE issue_id = $1`; `db_unconfigured` → envelope error. Register as MCP tool. |
| T13.2 | Journal content_hash migration | _pending_ | _pending_ | Author `tools/migrations/add-journal-content-hash.ts`: Step 1 — `ALTER TABLE ia_project_spec_journal ADD COLUMN IF NOT EXISTS content_hash TEXT`; Step 2 — batched SHA-256 backfill (500 rows/batch) computing hash from existing `(issue_id, kind, body)` columns; Step 3 — add unique partial index `UNIQUE (content_hash) WHERE content_hash IS NOT NULL`; Step 4 — `ALTER COLUMN content_hash SET NOT NULL`. Full rollback: `DROP COLUMN content_hash`. |
| T13.3 | Closeout digest journaled_sections | _pending_ | _pending_ | Extend `project-spec-closeout-digest.ts`: after computing checklist, query `SELECT DISTINCT kind FROM ia_project_spec_journal WHERE issue_id = $1`; add `journaled_sections: string[]` to `payload`; `db_unconfigured` → `journaled_sections: []`, `meta.partial.failed++`. Update `ia/skills/stage-closeout-plan/SKILL.md` + `ia/skills/plan-applier/SKILL.md` (Mode stage-closeout) to read `journaled_sections` before calling `journal_entry_sync` (skip if already persisted). Retired `ia/skills/closeout/SKILL.md` / `project-spec-close/SKILL.md` paths folded into this pair per M6 collapse. |
| T13.4 | Journal lifecycle tests | _pending_ | _pending_ | Tests for `journal_entry_sync`: dedup — same `(issue_id, kind, body)` twice → single DB row; different body same issue → two rows; cascade delete removes all issue rows; migration: second run no-op (idempotent). Tests for `project_spec_closeout_digest.journaled_sections`: populated when journal has prior entries; empty `[]` when db_unconfigured. |

#### §Stage File Plan

> Opus `stage-file-plan` writes structured `{operation, target_path, target_anchor, payload}` tuples here per pending Task. Sonnet `stage-file-apply` reads tuples and materializes BACKLOG rows + spec stubs. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/stage-file` planner pass._

#### §Plan Fix

> Opus `plan-review` writes targeted fix tuples here when a Stage's Task specs need tightening before first `/implement`. Sonnet `plan-applier` Mode plan-fix reads tuples and applies edits. Contract: `ia/rules/plan-apply-pair-contract.md`.

_pending — populated by `/plan-review` when fixes are needed._

#### §Stage Audit

> Opus `opus-audit` writes one `§Audit` paragraph per Task row here (Stage-scoped bulk, non-pair) once every Task reaches Done post-verify. Feeds `§Stage Closeout Plan` migration tuples downstream. Contract: `ia/rules/plan-apply-pair-contract.md` Stage-scoped non-pair row.

_pending — populated by `/audit {{this-doc}} Stage {{N.M}}` once all Tasks reach Done post-verify._

#### §Stage Closeout Plan

> Opus `stage-closeout-plan` writes unified tuple list here ONCE per Stage when all Task rows reach `Done` post-verify. Sonnet `stage-closeout-apply` reads tuples and applies verbatim. Contract: `ia/rules/plan-apply-pair-contract.md` seam #4.

_pending — populated by `/closeout {{this-doc}} Stage {{N.M}}` planner pass when all Tasks reach `Done`._

---
