### Stage 3 — Data Migration: Master Plans + Backlog Schema / Transform Script + Master-Plan In-Place Migration

**Status:** Final

**Objectives:** Author and run `migrate-master-plans.ts`. Canary on 1 low-risk closed plan, then batch on remaining 15. Each run reads from snapshot (idempotent), emits to current path. Per-file progress tracked in migration JSON for crash resume.

**Exit:**

- `tools/scripts/migrate-master-plans.ts` exists and parses old Step/Stage/Phase/Task AST; emits Stage/Task; preserves task-row `Issue` + `Status` columns verbatim; appends old Phase exit criteria to parent Stage exit; renames task ids `T{step}.{stage}.{task}` → `T{stage}.{task}` (stage renumbered sequentially).
- Canary: `blip-master-plan.md` (fully closed, safe) migrated + `npm run validate:all` passes.
- All 15 remaining open master plans migrated; 2 randomly selected manually diffed.
- Migration JSON M2 per-file entries all `done`.
- `npm run validate:all` passes on full set.
- Phase 1 — Transform script authoring + canary run.
- Phase 2 — Batch migration + validate.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Author migrate-master-plans.ts | **TECH-450** | Done (archived) | Write `tools/scripts/migrate-master-plans.ts` (TypeScript, run via `npx tsx`): (a) reads `ia/state/lifecycle-refactor-migration.json` M2.per-file; skips files with status `done`; (b) for each pending file, reads from `ia/state/pre-refactor-snapshot/{relative-path}` (not current); (c) parses markdown AST: detect Step→Stage→Phase→Task structure; map old Step+Stage pair → new Stage (name = "{Step name} / {Stage name}"); merge Phase exit bullets into parent Stage Exit section; strip Phase heading rows from Phases section; drop `Phase` column from task-table headers; renumber task ids `T{N}.{M}.{k}` → `T{stage_seq}.{k}` preserving Issue + Status; (d) emit to current file path (not snapshot); (e) flip file to `done` in migration JSON immediately after emit. |
| T3.2 | Canary run + parser fix | **TECH-451** | Done (archived) | Run `npx tsx tools/scripts/migrate-master-plans.ts --only blip-master-plan.md`; diff output against snapshot; verify task rows verbatim (Issue ids unchanged), exit criteria merged, Phase column absent; run `npm run validate:all`; fix any parser edge cases (e.g. nested code blocks, missing Phase sections); commit fix to migration script; mark canary `done` in migration JSON. |
| T3.3 | Batch migration — 15 remaining plans | **TECH-452** | Done (archived) | Run `npx tsx tools/scripts/migrate-master-plans.ts` (all pending); monitor migration JSON per-file progress; on crash: re-run (idempotent — reads snapshot + skips done files); update migration JSON M2 per-file to all `done`; manually diff 2 randomly selected plans (e.g. `zone-s-economy-master-plan.md` + `multi-scale-master-plan.md`) against expected output. |
| T3.4 | Batch validate + M2 flip | **TECH-453** | Done (archived) | Run `npm run validate:all` on full repo after batch migration; fix any validation failures (likely: task-id format checks in validators); run `npm run validate:frontmatter`; flip migration JSON M2 `done`. |

---
