### Stage 8 — yaml schema v2 + backfill + validator MVP (locator fields) / Template frontmatter + backfill script

**Status:** Final

**Backlog state (4):** TECH-384, TECH-385, TECH-386, TECH-387 all Done (archived).

**Objectives:** Ship the 2-field spec-frontmatter mirror in `ia/templates/project-spec-template.md` (additive; lazy — populated on next `/kickoff`, no retroactive rewrite). Author `tools/scripts/backfill-parent-plan-locator.sh` as an idempotent one-shot pass over open yaml; parses `title` suffix + walks plans for forward resolution; `--dry-run` preview; `--skip-unresolvable` hook stubbed (used by Step 6 archive pass).

**Exit:**

- `ia/templates/project-spec-template.md` frontmatter has `parent_plan: {path}` + `task_key: {T_key}` placeholder rows, wrapped in a block comment explaining the 2-field mirror rule.
- `tools/scripts/backfill-parent-plan-locator.sh (new)` — runs clean on current `ia/backlog/*.yaml`; idempotent (second run = zero writes); supports `--dry-run` + `--skip-unresolvable`; logs counts (resolved / skipped / errors).
- Backfill driver under `tools/scripts/backfill-parent-plan-locator.mjs (new)` — parses `title` suffix regex `\(Stage (\d+\.\d+) Phase (\d+)\)$` + walks plan task tables by `Issue: {id}` match for forward `parent_plan` + `task_key` resolution.
- Fixture test covers: resolved record / title-suffix-missing (skipped) / plan-not-found (skipped).
- Phase 1 — Template frontmatter mirror.
- Phase 2 — Backfill script + driver + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | Add 2-field mirror to spec template | **TECH-384** | Done (archived) | Edit `ia/templates/project-spec-template.md` frontmatter block — add `parent_plan: {{PARENT_PLAN_PATH}}` + `task_key: {{T_KEY}}` rows. Add a block comment immediately above naming the 2-field mirror rule + that step/stage/phase derive from `task_key` parser (no 5-field frontmatter). Lazy — not retroactive. |
| T8.2 | Extend frontmatter schema doc | **TECH-385** | Done (archived) | Edit `ia/templates/frontmatter-schema.md` — document `parent_plan` + `task_key` as optional-until-Step-6 fields; valid format (`task_key` regex `^T\d+\.\d+(\.\d+)?$`). Reference exploration source doc. |
| T8.3 | Implement backfill driver | **TECH-386** | Done (archived) | `tools/scripts/backfill-parent-plan-locator.mjs (new)` — loads all `ia/backlog/*.yaml`; for each, parses `title` suffix `(Stage X.Y Phase Z)`; walks `ia/projects/*master-plan*.md` task tables via regex `\ | T[\d.]+ \ | .* \ | \*\*{id}\*\*` for forward `parent_plan`; on resolve, writes v2 fields via schema-v2 writer (T3.1.3). Supports `--dry-run` + `--skip-unresolvable`. |
| T8.4 | Shell wrapper + backfill fixtures | **TECH-387** | Done (archived) | `tools/scripts/backfill-parent-plan-locator.sh (new)` — thin wrapper: `exec node …` exit-code passthrough; caveman header documents `--dry-run` + `--skip-unresolvable` + `--archive` (no-op). Fixture set under `tools/scripts/test-fixtures/backfill-locator/` covering resolved / already-populated / plan-missing (both flag modes); harness diffs stdout + exit code; driver gains `IA_REPO_ROOT` env override for sandbox isolation. |
