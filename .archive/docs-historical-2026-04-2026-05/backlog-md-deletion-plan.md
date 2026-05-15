# BACKLOG.md + BACKLOG-ARCHIVE.md — safe deletion plan

Status: planning doc. Not filed as issue.
Date: 2026-05-08
Author: investigation triggered by `compaction-loop-mitigation.md` baseline (top-2 doc by LOC).

## Goal

Delete `BACKLOG.md` (1032 lines) + `BACKLOG-ARCHIVE.md` (3016 lines) + matching section manifests + materialize generators. Replace as authoritative source with DB queries (`ia_tasks` + `ia_master_plans` + `ia_stages` + `ia_tasks.raw_markdown`).

Outcome: remove ~10K lines of generated/regenerated artifacts from repo, eliminate parse round-trip, simplify skill prose, free agent context (top-doc hits in compaction summaries).

## Constraint: not safe today

DB already holds the data (mig 0017 added `ia_tasks.raw_markdown`). But `backlog-parser.ts` still does `fs.readFileSync` on the .md files. Five MCP tools depend on the parser. Deleting files now → entry-point of `router_for_task` (`backlog_issue`) breaks → every skill chain dies.

## Scope inventory

### Active runtime readers (must refactor before delete)

| Surface | File | Call site |
|---|---|---|
| Parser core | `tools/mcp-ia-server/src/parser/backlog-parser.ts` | `fs.readFileSync(BACKLOG_FILE)` line 305; `fs.readFileSync(BACKLOG_ARCHIVE_FILE)` line 354 |
| MCP tool | `tools/mcp-ia-server/src/tools/backlog-issue.ts` | `parseBacklogIssue()` |
| MCP tool | `tools/mcp-ia-server/src/tools/backlog-list.ts` | scope param drives parser |
| MCP tool | `tools/mcp-ia-server/src/tools/backlog-search.ts` | regex over file contents via parser |
| MCP tool | `tools/mcp-ia-server/src/tools/master-plan-locate.ts` | parser → resolve plan link |
| MCP tool | `tools/mcp-ia-server/src/tools/invariant-preflight.ts` | parser → guard checks |
| Diagnostic script | `tools/mcp-ia-server/scripts/project-spec-dependents.ts` | greps `BACKLOG.md` for orphan scan |

### Generators (delete after refactor)

| File | Role |
|---|---|
| `tools/postgres-ia/materialize-backlog-from-db.mjs` | DB → BACKLOG.md (current) |
| `tools/scripts/materialize-backlog.mjs` | yaml → BACKLOG.md (legacy) |
| `tools/scripts/materialize-backlog.sh` | shell wrapper |
| `tools/scripts/migrate-backlog-to-yaml.mjs` | one-shot historical migration (already-completed work) |
| `tools/scripts/backlog-yaml-writer.mjs` | yaml write helper still used? — verify before delete |

### Tests (rewrite against DB fixtures)

- `tests/parser/backlog-parser.test.ts`
- `tests/parser/closeout-parse.test.ts`
- `tests/tools/backlog-issue.test.ts`
- `tests/tools/backlog-list.test.ts`
- `tests/tools/backlog-search.test.ts`
- `tests/tools/invariant-preflight.test.ts`
- `tools/scripts/tests/materialize-backlog-mode.test.sh`

### Section manifests (delete with files)

- `ia/state/backlog-sections.json` (3114 lines)
- `ia/state/backlog-archive-sections.json` (3165 lines)

### Citation-only (mass prose update)

~99 files cite filenames without runtime dependency:

- Skills: `ship`, `project-new`, `project-new-apply`, `project-spec-implement`, `verify-loop`, `stage-file-main-session`, `stage-compress`, `sprite-gen-visual-review`
- Top-level: `AGENTS.md`, `CLAUDE.md` (via `terminology-consistency.md`)
- Rules: `agent-router.md`, `agent-tooling-hints.md`, `agent-output-caveman-authoring.md`, `agent-code-review-self.md`, `terminology-consistency-authoring.md`
- Specs: `glossary.md`, `architecture/data-flows.md`, `architecture/interchange.md`, `architecture/layers.md`, `ui-design-system.md`, `unity-development-context.md`, `persistence-system.md`, `managers-reference.md`, `simulation-system.md`
- Templates: `ia/templates/project-spec-template.md`, `project-spec-review-prompt.md`, `master-plan-template.md`
- Docs: `agent-lifecycle.md`, `mcp-ia-server.md`, `cron-jobs-ops.md`, `information-architecture-overview.md`, `PROJECT-SPEC-STRUCTURE.md`, `human-resume-without-ai.md`, `cursor-agents-skills-mcp-study.md`, `agent-tooling-verification-priority-tasks.md`, `master-plan-foldering-refactor-design.md`, `bug-62-icon-slug-case-postmortem.md`, several `*-exploration.md`, `*-findings.md`, `*-implementation.md`
- Generated agents/commands: `.claude/agents/spec-implementer.md`, `.claude/agents/project-new-applier.md`, `.claude/commands/implement.md`, `.claude/commands/project-new.md`, `.claude/commands/stage-file-main-session.md` (these regen from skill bodies — edit SKILLs, run `npm run skill:sync:all`)

### CI / validators

- `validate:all:readonly` runs `validate:backlog-yaml` (yaml schema check, file-independent — keep).
- No `validate:materialize-backlog` in `validate:all`. Generators are on-demand only.
- `.github/workflows/ia-tools.yml` runs `generate:ia-indexes -- --check` (not backlog materialize — unaffected).

### Pre-refactor snapshot — leave alone

`ia/state/pre-refactor-snapshot/` directory carries historical yaml + master plans + project specs. ~200 files cite `BACKLOG`/`BACKLOG-ARCHIVE` inside frozen content. Out of scope: that tree is intentionally immutable historical record.

## Phased plan

### Phase 1 — DB-backed parser (scaffolding, no deletes)

**Goal:** `parseBacklogIssue(repoRoot, issueId)` returns identical shape from DB query, .md still on disk as fallback.

1. Add new module `tools/mcp-ia-server/src/parser/backlog-parser-db.ts`:
   - `parseBacklogIssueFromDb(issueId)` — joins `ia_tasks` + `ia_master_plans` + `ia_stages`; pulls `raw_markdown` for verbatim block; resolves depends-on via `ia_task_deps` table.
   - `parseAllIssuesFromDb({ scope })` — open vs archive split via `ia_tasks.status` filter (`!= 'archived'` vs `= 'archived'`).
   - `resolveDependsOnStatusFromDb(ids)` — single batched query.
2. Behind feature flag `IA_BACKLOG_SOURCE=db|file` (default `file`). All 5 MCP tools call dispatcher, dispatcher selects path.
3. Parity tests: golden fixture asserts `parseBacklogIssueFromDb(id)` ≡ `parseBacklogIssueFromFile(id)` for sample of 20 ids spanning open + archive.

**Exit criteria:** `IA_BACKLOG_SOURCE=db npm run test:ia` passes parity test. No file changes outside parser layer.

### Phase 2 — Cut over to DB default

1. Flip `IA_BACKLOG_SOURCE` default → `db`. Keep `file` path for one release as escape hatch.
2. Run full `validate:all` + `verify:local` with DB path live.
3. Manually exercise `backlog_issue`, `backlog_list`, `backlog_search`, `master_plan_locate`, `invariant_preflight` MCP tools end-to-end via Claude session.

**Exit criteria:** zero functional drift across full skill chain (`/project-new`, `/ship-cycle`, `/verify-loop`).

### Phase 3 — Drop file-path code

1. Delete `parseBacklogIssueFromFile` + `parseAllIssuesFromFile` + helpers.
2. Delete `IA_BACKLOG_SOURCE` flag + dispatcher.
3. Refactor `project-spec-dependents.ts` → DB scan over `ia_tasks.raw_markdown` + `ia_task_specs.body_md`.
4. Delete tests: parser/backlog-parser.test.ts, closeout-parse.test.ts, parser-internal helpers in tools tests; rewrite tool tests against DB fixtures (already partially exists in `db-read-batch.test.ts` pattern).

**Exit criteria:** `tools/mcp-ia-server/src/parser/backlog-parser.ts` no longer reads `.md` files; grep `readFileSync.*BACKLOG` returns 0 in `tools/mcp-ia-server/`.

### Phase 4 — Delete generators + manifests + .md files

1. Delete generators:
   - `tools/postgres-ia/materialize-backlog-from-db.mjs`
   - `tools/scripts/materialize-backlog.mjs`
   - `tools/scripts/materialize-backlog.sh`
   - `tools/scripts/migrate-backlog-to-yaml.mjs`
   - `tools/scripts/backlog-yaml-writer.mjs` (verify no other callers first)
   - `tools/scripts/tests/materialize-backlog-mode.test.sh`
2. Delete section manifests:
   - `ia/state/backlog-sections.json`
   - `ia/state/backlog-archive-sections.json`
3. Delete the files:
   - `BACKLOG.md`
   - `BACKLOG-ARCHIVE.md`
4. Remove `npm run materialize:backlog` script from `package.json` if present.

**Exit criteria:** `git ls-files | grep "^BACKLOG"` returns nothing.

### Phase 5 — Mass prose update

Single semantic sweep across the ~99 citation files. Patterns:

- `BACKLOG.md` (open issues) → `MCP backlog_issue` / `ia_tasks (status != archived)`
- `BACKLOG-ARCHIVE.md` (closed) → `ia_tasks (status = archived)`
- `BACKLOG.md / BACKLOG-ARCHIVE.md` lookup hint → `mcp__territory-ia__backlog_issue`
- "rendered view" / "generated from yaml" pattern → drop, no longer applicable.

Edit SKILL.md frontmatter + bodies first (they regen `.claude/agents/*.md` + `.claude/commands/*.md` via `npm run skill:sync:all`). Edit rules + specs + templates + docs after.

Validators to run: `validate:claude-imports`, `validate:skill-drift`, `validate:retired-skill-refs`, `validate:mcp-readme`, `validate:mcp-descriptor-prose`.

**Exit criteria:** `rg -l "BACKLOG\.md|BACKLOG-ARCHIVE\.md"` returns only `pre-refactor-snapshot/` + this audit doc.

### Phase 6 — Verification + commit

1. `npm run validate:all` — green.
2. `npm run verify:local` — green.
3. End-to-end skill exercise: `/project-new` round-trip, `/ship-cycle` smoke, `/verify-loop` exit clean.
4. Single commit: `feat(ia-db): retire BACKLOG.md + BACKLOG-ARCHIVE.md as authoritative artifacts; DB sole source-of-truth`.

## Risks + mitigations

| Risk | Likelihood | Mitigation |
|---|---|---|
| Hidden parser dep in subagent prompt that we miss | medium | Phase 5 grep sweep + spot-check `.claude/agents/_retired/` + active agents. |
| `raw_markdown` column has gaps for older tasks | medium | Phase 1 parity test surfaces. Backfill script: regen `raw_markdown` from current parser output before deleting parser. |
| Section ordering / preamble lost (the `## Open` / `## Archive` sectioning, glossary blocks) | low | Section manifests are layout-only metadata. If anything still wants ordering, push it into a small `master_plan_render`-style MCP tool that emits .md on demand. |
| External tooling (CI, PR templates, contributor docs) cites file paths | low | `.github/workflows/` + root `README.md` + `AGENTS.md` already covered in Phase 5 sweep. |
| Diff history readability — `git log BACKLOG.md` gone | low | DB has audit log via `cron_audit_log`. Unaffected for engineering archaeology. |

## Effort estimate

- Phase 1: 1 day (parser + parity test).
- Phase 2: 0.5 day (cutover + skill smoke).
- Phase 3: 0.5 day (drop + test rewrite).
- Phase 4: 0.25 day (delete sweep).
- Phase 5: 1 day (prose sweep across ~99 files; mostly mechanical sed + manual review of skills).
- Phase 6: 0.25 day (verify + commit).

Total ≈ 3.5 days. One Stage with 4 Tasks (Phases 1–2 together, 3, 4–5 together, 6).

## Out-of-scope follow-ups

- Same pattern applies to `MEMORY.md` (already on-demand only — no parser dep but still cited prose).
- `ia/state/pre-refactor-snapshot/` could be archived to a separate frozen branch + dropped from main, freeing another ~200 files of grep noise. Separate plan.
- `BACKLOG-ARCHIVE.md` deletion alone unlocks Tier B3 leverage on `compaction-loop-mitigation.md` baseline (3016-line top-1 doc).

## Next

Bring this plan to Javier for sign-off. After approval → `/project-new` filing TECH-{next} "DB-authoritative backlog: retire BACKLOG.md + BACKLOG-ARCHIVE.md". Reference this doc as plan body.
