---
purpose: "Sequential implementation plan for the IA dev system DB-primary refactor. Opus agents iterate step by step, logging findings + filling gaps inline. Living doc — not a mechanical pre-laid plan; anchors on locked decisions from `docs/master-plan-foldering-refactor-design.md` and leaves voids for implementers to resolve."
audience: both
loaded_by: ondemand
slices_via: none
---

# IA dev system DB-primary refactor — sequential implementation

> **Branch:** `feature/ia-dev-db-refactor` (cut from `main` @ `1563765`)
> **Mode:** Pure sequential. One step at a time. No parallel stages. No `/master-plan-new` ceremony. Implementers (Opus agents) iterate; each step logs findings + verifies + commits before the next begins.
> **Source of truth for decisions:** [`docs/master-plan-foldering-refactor-design.md`](master-plan-foldering-refactor-design.md) Rounds 1, 2, 4, 5 (all locked). Gaps intentional — implementers fill inline.
> **Companion:** [`docs/master-plan-execution-friction-log.md`](master-plan-execution-friction-log.md) — log unexpected friction during this refactor to feed post-refactor retrospective.

---

## 0. Operating model

- **One step at a time.** Finish step N, log findings, commit. Only then start step N+1.
- **Implementer fills voids.** Each step lists *what* + *why* + *acceptance*. It does NOT pre-specify file diffs, exact column schemas, or exact migration SQL. Implementer reads the design doc, makes concrete choices, writes code, records what was chosen + rationale in this doc under the step's `Findings` block.
- **No §Plan Digest / §Audit / §Closeout Plan.** This refactor does not use the broken master-plan pipeline. Direct commits per step. Commit message format: `feat(ia-dev-db): step N.M — {short description}`.
- **Scope lock.** Do NOT extend scope mid-step. If a step uncovers unavoidable adjacent work, record it under `Followups` in this doc; defer execution until current step is green.
- **Rollback discipline.** Every step must be independently revertable (single commit or tight commit cluster). If a step goes sideways, revert, reset `Findings`, retry.
- **Tooling invariants.** Do NOT regress `db:migrate` / `unity:compile-check` / `validate:all` on the Unity + web surfaces during the refactor. New IA DB tables land additively (`ia_*` prefix); nothing existing breaks until cleanup phase (last step).

---

## 1. Step sequence

### Step 1 — DB schema foundation

**Goal:** Create all `ia_*` tables + types + indexes in Postgres via a new migration. No data imported yet. No MCP changes yet.

**Inputs:**

- Design doc §4.2 (DB vs filesystem split — table list).
- Design doc §4.5 F1–F15 (schema specifics: enum types, text PK, join table for deps, dual tsvector + trigram indexes, full-snapshot history table, mixed concurrency primitives).
- Existing `db:migrate` chain used by Unity bridge.

**Outputs:**

- New migration file under `tools/scripts/db-migrations/` (or wherever the existing chain lives — implementer discovers).
- Tables: `ia_master_plans`, `ia_stages`, `ia_tasks`, `ia_task_deps`, `ia_task_spec_history`, `ia_task_commits`, `ia_stage_verifications`, `ia_ship_stage_journal`, `ia_fix_plan_tuples`.
- Types: `task_status` ENUM, `stage_verdict` ENUM (values — implementer decides final set, record under Findings).
- Indexes: `GIN(ia_tasks.body_tsv)`, `GIN(ia_tasks.body gin_trgm_ops)`, btree on `(slug, stage_id)` for stages, reverse-lookup index on `ia_task_deps(depends_on_id)`.
- Sequences per prefix: `tech_id_seq`, `feat_id_seq`, `bug_id_seq`, `art_id_seq`, `audio_id_seq`.

**Acceptance:**

- `npm run db:migrate` applies clean.
- `psql` inspection confirms all tables + types + indexes present.
- `db:bridge-preflight` still green (no regression on Unity-bridge schema).
- Sequence `SELECT nextval('tech_id_seq')` returns a value > current `ia/state/id-counter.json` max (implementer determines exact seed + documents).

**Voids for implementer:**

- Exact column types (`text` vs `varchar(N)`), nullability, defaults.
- Enum values — start from design doc §4.5 F1 recommendation (`pending | implemented | verified | done | archived`) but confirm set is sufficient after reviewing lifecycle.
- FK cascade rules (`ON DELETE CASCADE` vs `RESTRICT` vs `SET NULL`).
- Audit column names (`created_at`, `updated_at`, `actor` — choose convention consistent with existing Unity-bridge tables).
- Migration file naming (timestamp convention — match existing chain).

**Findings (implementer fills after green):**

```
- Migration file path:
- Tables created (count + names):
- Enum values chosen + rationale:
- FK cascade policy + rationale:
- Index build timing + size estimate:
- Seed values for sequences:
- Surprises / gotchas:
```

**Commit:** `feat(ia-dev-db): step 1 — DB schema foundation (ia_* tables + types + indexes)`

---

### Step 2 — One-shot import script

**Goal:** Port existing state from filesystem (yaml + markdown) into DB tables. Idempotent. Re-runnable without data corruption.

**Inputs:**

- `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml` → `ia_tasks` rows (status `pending` / `in_progress` / `done` / `archived` per current yaml shape).
- `ia/projects/*master-plan*.md` → `ia_master_plans` + `ia_stages` rows (parse stage headers + objectives + exit).
- `ia/projects/{ISSUE_ID}.md` files → `ia_tasks.body` text + section index.
- `ia/state/id-counter.json` → sequence seed values (Step 1 already seeded; script verifies consistency).

**Outputs:**

- New script `tools/scripts/ia-db-migrate.ts` (or `.mjs` — implementer picks language matching existing tool chain).
- `npm run` target registered (suggest `ia:db-import`).
- Transactional per-run execution (F12=c: idempotent re-run).

**Acceptance:**

- Running `npm run ia:db-import` on clean DB populates all rows.
- Re-running same command is a no-op (or safe overwrite; implementer chooses — document in Findings).
- Row counts match filesystem: `ia_tasks` row count ≈ union of `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml` counts.
- `ia_tasks.body_tsv` populated + full-text query returns expected results (smoke: `SELECT task_id FROM ia_tasks WHERE body_tsv @@ to_tsquery('english', 'heightmap')` returns ≥1 row).
- Filesystem untouched.

**Voids for implementer:**

- Parser strategy for master-plan stage blocks (regex vs markdown AST).
- Handling for archived-but-present project spec files (are all archived yamls still on disk, or does the filesystem have gaps?).
- Dep graph — how to parse `depends_on` from yaml and populate `ia_task_deps`.
- Body section extraction — store whole body in one column or also write per-section cache? (F4 says whole body + tsvector; per-section slicing is MCP-layer concern per §4.3).

**Findings (implementer fills):**

```
- Script path + npm target:
- Rows imported per table:
- Parse errors encountered + resolution:
- Idempotency strategy chosen:
- Import wall-clock time:
- Data gaps discovered:
- Surprises:
```

**Commit:** `feat(ia-dev-db): step 2 — one-shot import script (yaml + master-plan + spec body → DB)`

---

### Step 3 — Read MCP tools (DB-backed)

**Goal:** Expose read-only DB query tools via `territory-ia` MCP server. No mutations yet. Keep filesystem reads as fallback during transition (shortest possible window).

**Inputs:**

- Existing `tools/mcp-ia-server/src/` surface + `list_*` registration patterns.
- Design doc §4.3 MCP tool surface (Read section).
- Step 1 schema + Step 2 populated DB.

**Outputs:**

- New MCP tools registered:
  - `task_state(task_id)` — metadata + status + commits + deps from DB.
  - `stage_state(slug, stage_id)` — progress + blockers + next_pending from DB.
  - `master_plan_state(slug)` — rollup across stages.
  - `task_spec_body(task_id)` — full body.
  - `task_spec_section(task_id, section)` — single section slice.
  - `task_spec_search(query, filters)` — full-text + trigram.
  - `stage_bundle(slug, stage_id)` — DB state + narrative slices in one payload.
  - `task_bundle(task_id)` — DB state + body slices.
- Existing tools (`backlog_issue`, `backlog_list`, `backlog_search`, `spec_section`, etc.) re-implemented as DB queries. Schemas unchanged.
- Singleton `pg.Pool` at MCP server boot (F15=a).

**Acceptance:**

- Restart MCP server; `list_*` returns new tools + existing tools still functional.
- `backlog_issue TECH-001` returns data identical to pre-refactor yaml parse.
- `task_spec_search 'heightmap'` returns ≥1 hit.
- No filesystem reads for task metadata during test calls (implementer greps tool code to confirm — or logs query source).
- Existing skill invocations that use old tools still work (tools are a compatibility surface).

**Voids for implementer:**

- Tool parameter schemas — match existing shapes where possible, extend where needed.
- Error shape when DB is down (throw? return typed error payload? — pick convention).
- `spec_section` reimplementation: does it read body from DB + regex-extract section? Or does it also persist per-section?
- Caching strategy for hot queries (prepared statements? in-memory LRU?).

**Findings (implementer fills):**

```
- Tools registered (names):
- Tools reimplemented (names):
- Connection pool config (size, idle timeout):
- Query latency p50 / p95 on representative ops:
- Section-slicing strategy:
- Error handling convention:
- Surprises:
```

**Commit:** `feat(ia-dev-db): step 3 — read MCP tools (DB-backed state queries + full-text search)`

---

### Step 4 — Write MCP tools (atomic mutations)

**Goal:** Expose mutation tools for task insert, status flip, body write, commit record, journal append, fix-plan write. Transactional. Replace filesystem `flock` with DB primitives.

**Inputs:**

- Step 3 read tools + DB pool.
- Design doc §4.3 Mutate section + F10 concurrency (mixed advisory + row locks).

**Outputs:**

- `task_insert(slug, stage_id, title, body, type, priority, depends_on, related)` → reserves id via DB sequence, inserts row + body + deps in one tx.
- `task_status_flip(task_id, new_status)` — row-level lock + update.
- `task_spec_section_write(task_id, section, content)` — body update + optional history row.
- `task_commit_record(task_id, commit_sha)` — append to `ia_task_commits`.
- `stage_verification_flip(slug, stage_id, verdict, commit_sha, notes)` — upsert latest row (F11=a).
- `stage_closeout_apply(slug, stage_id)` — flip task statuses → archived + optional `mv` stage file to `_closed/` (filesystem op covered separately in Step 8).
- `journal_append(session_id, task_id, phase, payload_kind, payload)` / `journal_get` / `journal_search`.
- `fix_plan_write(task_id, round, tuples)` / `fix_plan_consume(task_id, round)`.

**Acceptance:**

- Round-trip: insert task → read via `task_state` → body matches.
- Concurrency test: two parallel `task_status_flip` calls on same row → second blocks + resolves without corruption.
- `task_insert` under parallel load → unique ids (no duplicates, no gaps that break sequence).
- Journal append + get round-trip.
- Commit smoke: `task_commit_record` rows queryable.

**Voids for implementer:**

- Advisory lock key naming scheme (`pg_advisory_lock('ia_id_seq_tech'::regclass::oid, ...)` — pick convention).
- History table trigger vs explicit write in tool (F5 says full snapshots — implementer picks mechanism).
- Journal payload schema enforcement — runtime validation (zod / ajv) or trust-but-document?
- Rollback policy on partial failure (tx abort + re-raise, or partial commit + report?).

**Findings:**

```
- Tools added (names):
- Concurrency test outcome:
- Sequence seed + first inserted id:
- History trigger vs tool-side write:
- Journal schema enforcement:
- Surprises:
```

**Commit:** `feat(ia-dev-db): step 4 — write MCP tools (atomic task/stage/journal mutations)`

---

### Step 5 — `BACKLOG.md` view generation (DB → markdown)

**Goal:** Replace `materialize-backlog.sh` with a DB-sourced generator. `BACKLOG.md` + `BACKLOG-ARCHIVE.md` become regenerated views; content identical to current shape.

**Inputs:**

- Step 3 read tools.
- Current `materialize-backlog.sh` logic + output format.
- `ia/state/backlog-sections.json` + `ia/state/backlog-archive-sections.json` (section ordering rules).

**Outputs:**

- New script `tools/scripts/materialize-backlog-from-db.ts` (or replace existing shell — implementer picks).
- `npm run materialize:backlog` flips to new source.
- Generated `BACKLOG.md` + `BACKLOG-ARCHIVE.md` byte-identical (or near-identical, documented) to pre-refactor output.

**Acceptance:**

- `diff BACKLOG.md pre-refactor-BACKLOG.md` shows only expected delta (formatting, section ordering intentional).
- No filesystem yaml reads during generation.
- `validate:all` passes.

**Voids for implementer:**

- Handling for intentional formatting drift (date-stamp lines, generated-by markers).
- Whether to keep the old shell as fallback or remove immediately (recommend keep during Step 6 skill flip, remove at Step 9 cleanup).

**Findings:**

```
- Script path:
- Byte-diff vs pre-refactor output:
- Wall-clock time:
- Surprises:
```

**Commit:** `feat(ia-dev-db): step 5 — materialize-backlog from DB (markdown view regen)`

---

### Step 6 — `stage-file` skill flip (DB-aware)

**Goal:** Merge `stage-file-plan` + `stage-file-apply` into single `stage-file` skill. Writes DB rows via `task_insert` MCP instead of yaml + `materialize-backlog.sh`. No filesystem yaml writes.

**Inputs:**

- Current `ia/skills/stage-file-plan/SKILL.md` + `ia/skills/stage-file-apply/SKILL.md`.
- Design doc B1 + E2 + E3.
- Step 4 write tools.

**Outputs:**

- Merged `ia/skills/stage-file/SKILL.md` (retire the `-plan` + `-apply` pair).
- `.claude/agents/stage-file*.md` + `.claude/commands/stage-file.md` updated to reference new skill.
- Old subagents moved to `.claude/agents/_retired/`.

**Acceptance:**

- Smoke: `/stage-file {orchestrator} {stage_id}` on a small test orchestrator (implementer picks — ideally a throwaway or existing Stage with `_pending_` tasks) files N tasks into DB.
- DB reads back the N tasks via `stage_state`.
- `BACKLOG.md` regenerated (Step 5) includes the new rows.
- No yaml files written under `ia/backlog/`.

**Voids for implementer:**

- How to test end-to-end without polluting real backlog (throwaway slug? revert after?).
- Dispatch shape: single skill invocation, no Opus pair-tail (per B1).
- Plan-author / plan-digest still called inline? Or that's step 7?

**Findings:**

```
- Skill file path:
- Retired subagent files:
- Smoke test slug + stage:
- Rows created:
- Surprises:
```

**Commit:** `feat(ia-dev-db): step 6 — stage-file skill merged + DB-backed (retire plan+apply pair)`

---

### Step 7 — `stage-authoring` skill flip (merge plan-author + plan-digest)

**Goal:** Merge `plan-author` + `plan-digest` into single `stage-authoring` skill. Writes §Plan Digest via `task_spec_section_write` MCP. Drop §Plan Author section + aggregate `docs/implementation/` doc.

**Inputs:**

- Current `ia/skills/plan-author/` + `ia/skills/plan-digest/SKILL.md`.
- Design doc B2 + B6 + C7 + D8.
- Step 4 `task_spec_section_write`.

**Outputs:**

- New `ia/skills/stage-authoring/SKILL.md`.
- Retire plan-author + plan-digest.
- Drop aggregate `docs/implementation/{slug}-stage-X.Y-plan.md` pattern.
- `.claude/agents/` + `.claude/commands/` updated.

**Acceptance:**

- Smoke: run stage-authoring on a stage filed in Step 6 → §Plan Digest lands in DB body for each task.
- `task_spec_section task_id §Plan\ Digest` returns expected content.
- No aggregate doc written.

**Voids:**

- Does stage-file (Step 6) call stage-authoring inline (per C8=b) or keep as separate step? Recommend inline; confirm during implementation.
- §Plan Author intermediate state — does stage-authoring still go through an "author draft → digest mechanization" two-sub-phase internally, or direct-to-digest?

**Findings:** (standard template)

**Commit:** `feat(ia-dev-db): step 7 — stage-authoring skill merged + DB-backed spec section write`

---

### Step 8 — `ship-stage` skill flip (Pass A no-commit / Pass B stage-end)

**Goal:** Rewrite ship-stage: Pass A per-task implement + compile (no commits). Pass B per-stage verify-loop + code-review + closeout + single stage-end commit. Fold closeout inline (drop `stage-closeout-*` pair + `plan-applier`).

**Inputs:**

- Current `ia/skills/ship-stage/SKILL.md`.
- Design doc C9 + C10 + E13 + E14.
- Steps 4 + 5 + 6 + 7.

**Outputs:**

- Rewritten `ia/skills/ship-stage/SKILL.md`.
- Retire `ia/skills/stage-closeout-plan/` + `ia/skills/stage-closeout-apply/` + `ia/skills/plan-applier/` (code-fix mode specifically; if plan-fix mode survives elsewhere, keep only that path).
- `ship-stage` writes `stage_verification_flip` on verify pass, moves stage file to `_closed/` in `stage_closeout_apply`, commits single stage commit.

**Acceptance:**

- Smoke: run ship-stage on a filed+authored throwaway stage → all tasks `implemented → verified → done` → stage file lands in `_closed/` → single commit on branch.
- Drop: no per-task commits generated during Pass A.
- `ia_stage_verifications` row populated.

**Voids:**

- Retry loop shape (verify-loop fix iteration — where in Pass B?).
- Code-review fix-apply inline per E14 — how does reviewer apply fix without plan-applier? Direct Edit in reviewer subagent?
- Stage commit message format.

**Findings:** (standard)

**Commit:** `feat(ia-dev-db): step 8 — ship-stage rewritten (Pass A no-commit / Pass B stage-end + inline closeout)`

---

### Step 9 — Foldering migration + drop retired surface

**Goal:** One-shot filesystem reshape — move existing master plans into `ia/projects/{slug}/` folders with `index.md` + `stage-*.md` + `_closed/`. Retire `tasks/` subfolder (bodies in DB per E4=b). Drop yaml + materialize-backlog shell + reserve-id + runtime-state + filesystem lockfiles + dropped validators.

**Inputs:**

- Design doc §2 foldering shape + A4 retirement per E4=b.
- All prior steps green (DB + tools + skills all working).

**Outputs:**

- New script `tools/scripts/fold-master-plan.ts` (one-shot; run once per plan OR bulk).
- All `ia/projects/*master-plan*.md` → `ia/projects/{slug}/index.md` + `stage-*.md`.
- `ia/projects/{id}.md` task spec files → DELETED (bodies already in DB).
- `ia/backlog/` + `ia/backlog-archive/` → DELETED.
- `BACKLOG.md` + `BACKLOG-ARCHIVE.md` kept as generated view (regenerated from DB via Step 5).
- `tools/scripts/reserve-id.sh` + `ia/state/id-counter.json` + `.id-counter.lock` + `.materialize-backlog.lock` + `.runtime-state.lock` + `ia/state/runtime-state.json` → DELETED.
- Drop `validate:dead-project-specs`, `validate:backlog-yaml`, `validate:frontmatter`, `validate:claude-imports` from package.json scripts + CI.
- Drop `materialize-backlog.sh` (replaced by Step 5 TS).
- Drop retired skills: `plan-review`, `opus-auditor`, `plan-reviewer-mechanical`, `plan-reviewer-semantic`, `stage-closeout-*`, `plan-applier` code-fix, `stage-decompose`.

**Acceptance:**

- `git ls-files ia/backlog/` → empty.
- `git ls-files ia/projects/TECH-*.md` → empty (bodies in DB).
- `npm run validate:all` passes on reduced validator set.
- `/stage-file` + `/ship-stage` + `/author` smokes still green on test orchestrator.
- Web dashboard (if Step 10 shipped first — optional re-order) still reads.

**Voids:**

- Ordering: does foldering run before or after web dashboard (Step 10)? Recommend before — cleanup is the last step.
- History preservation for moved files — `git mv` vs copy+delete (git mv preserves blame).
- What to do with `ia/plans/` contents.

**Findings:** (standard)

**Commit:** `feat(ia-dev-db): step 9 — foldering migration + cleanup (yaml retires, task specs DB-only, dropped validators)`

---

### Step 10 — Web dashboard read surface

**Goal:** Read-only dashboard (E6=a) showing master plans + stages + tasks + verification verdicts + journal tail. Next.js API routes in `web/app/api/...` (E7=a) call MCP tools (F14=b) — not direct Postgres.

**Inputs:**

- Design doc §4.3 Read tools + E6 + E7 + F14.
- Existing `web/` workspace conventions.

**Outputs:**

- API routes: `web/app/api/ia/plans/route.ts`, `web/app/api/ia/plans/[slug]/route.ts`, `web/app/api/ia/tasks/[id]/route.ts`, `web/app/api/ia/tasks/[id]/body/route.ts`, etc.
- Dashboard page(s): `web/app/ia/page.tsx` (list), `web/app/ia/[slug]/page.tsx` (plan view), `web/app/ia/tasks/[id]/page.tsx` (task view).
- MCP client helper in `web/lib/` that proxies to `territory-ia` server.

**Acceptance:**

- `npm run dev` in `web/` + navigate to dashboard → list of master plans loads from DB.
- Click plan → stages + task table render from DB.
- Click task → body renders (markdown).
- Search bar → calls `task_spec_search`.

**Voids:**

- MCP client transport from Next.js server (spawn subprocess? HTTP bridge? — design question).
- Auth gate (none for now? Basic IP-restricted?).
- Styling / design system integration (use existing `web/lib/design-system.md`).

**Findings:** (standard)

**Commit:** `feat(ia-dev-db): step 10 — web dashboard read surface (Next.js API → MCP → DB)`

---

### Step 11 — Daily snapshot + history audit

**Goal:** Committed daily DB snapshot (E10) + `ia_task_spec_history` audit table wired to `task_spec_section_write` (F5=a).

**Inputs:**

- Design doc E10 + F5 + F6 (split plain/binary snapshot).
- Step 4 `task_spec_section_write`.

**Outputs:**

- Cron / CI job writing `ia/state/db-snapshot-metadata.sql` (plain SQL, diffable) + `ia/state/db-snapshot-bodies.dump` (binary, compressed).
- History table populated by trigger OR tool-side write (implementer chose in Step 4).

**Acceptance:**

- Snapshot regenerates cleanly.
- Restore-from-snapshot smoke (to throwaway DB) round-trips.
- `ia_task_spec_history` rows appear after `task_spec_section_write` calls.

**Voids:**

- Cron vs CI (GitHub Action on schedule? local cron?).
- Snapshot size + gitignore strategy for binary portion.

**Findings:** (standard)

**Commit:** `feat(ia-dev-db): step 11 — daily snapshot + spec body history`

---

### Step 12 — Retrospective + friction log close

**Goal:** Close this refactor. Merge to main. Open followup tickets for gaps surfaced during execution.

**Outputs:**

- Append final retrospective section to `docs/master-plan-execution-friction-log.md` comparing pre- vs post-refactor friction.
- PR from `feature/ia-dev-db-refactor` → `main`.
- File followup issues (via `project-new` on the new DB-backed system — meta: refactor bootstraps its own issue tracker).

**Acceptance:**

- PR green.
- Followups filed.
- Design doc (`master-plan-foldering-refactor-design.md`) locked + unchanged by this branch (preserved as historical record).

**Findings:** (standard)

**Commit:** `chore(ia-dev-db): step 12 — retrospective + PR open`

---

## 2. Running log

### 2.1 Step status table

| Step | Status | Started | Done | Commit | Notes |
|------|--------|---------|------|--------|-------|
| 1 — DB schema foundation | pending | — | — | — | — |
| 2 — Import script | pending | — | — | — | — |
| 3 — Read MCP tools | pending | — | — | — | — |
| 4 — Write MCP tools | pending | — | — | — | — |
| 5 — BACKLOG.md generator | pending | — | — | — | — |
| 6 — stage-file skill | pending | — | — | — | — |
| 7 — stage-authoring skill | pending | — | — | — | — |
| 8 — ship-stage skill | pending | — | — | — | — |
| 9 — Foldering + cleanup | pending | — | — | — | — |
| 10 — Web dashboard read | pending | — | — | — | — |
| 11 — Snapshot + history | pending | — | — | — | — |
| 12 — Retrospective + PR | pending | — | — | — | — |

### 2.2 Cross-step findings + followups

Implementers append entries here as discoveries surface that do NOT belong to a single step.

```
— empty —
```

### 2.3 Open questions surfaced during execution

```
— empty —
```

---

## 3. Guardrails for implementers

- **Do NOT write the whole plan upfront.** Current doc is the whole plan. Do not replace step bodies with pre-planned code diffs before starting.
- **Do NOT skip `Findings` blocks.** Every step green = Findings filled + committed in same commit (or commit immediately after).
- **Do NOT extend scope.** If a step uncovers an issue, record under §2.3 (open questions) and defer.
- **Do NOT couple steps.** Each step must be revertable in isolation. If two steps must land together, merge them into one step before starting.
- **Commit per step.** No mega-commits across steps. Commit messages follow `feat(ia-dev-db): step N — {summary}`.
- **No §Plan Digest ceremony.** This refactor is hand-driven. Do not invoke `/author`, `/plan-digest`, `/plan-review`, `/audit`, `/closeout` on this branch. Direct Edit + Bash + commit.
- **Verify before next step.** Acceptance block for step N must be fully green before starting step N+1.

---

## 4. Change log

- **2026-04-24** — Doc seeded on `feature/ia-dev-db-refactor` branch cut from `main @ 1563765`. All 12 steps outlined with Goal / Inputs / Outputs / Acceptance / Voids / Findings / Commit. Running log + status table initialized empty. Operating model: pure sequential, implementer fills voids + findings, no master-plan ceremony.
