# ship-protocol Stage 5 — Locked Design Decisions

> **Status**: pre-stage-decompose tmp doc. Captures Q1–Q5 review findings + locked decisions before Stage 5 task-table refresh. Compaction-safe — never regenerated from chat.
> **Owner skill**: `ship-protocol` master plan, Stage 5 (`design-explore extensions + retirement migration`).
> **Pre-reqs done**: Stage 1.0 tracer (commit `b4ca3788`) · Stage 2 ship-plan (`c9b7799e`) · Stage 3 ship-cycle (`7232bc48`) · Stage 4 ship-final (commit pending — done in DB).
> **Drift-fix dep**: bigint µs cursor + panel_child kind discriminator landed at `47b0665e`.

---

## 1. Intent recap

Q3 directive (verbatim): *"I wish to fully REMOVE, not retire, the previous skills that are being replaced. ... New skills should not include `_retired` skills refs. After migration, there should be no presence of dropped skills in the repo, except maybe for history purposes, but not in the skill prose."*

Q5 directive (verbatim): *"Choose all recommended options. Then annotate findings and decisions in tmp doc so context compaction does not loose it. Then continue to draft stage 5"*.

**Outcome**: hard-removal (not `_retired/`) + bulk backfill phase to unblock in-flight master plans + chain extensions so old plans become workable on new chain after migration.

---

## 2. Retired skills (HARD-REMOVE — `git rm`, NOT `_retired/`)

| Slug                | Folded into                            | Successor seam                       |
|---------------------|----------------------------------------|--------------------------------------|
| `master-plan-new`   | `ship-plan`                            | bulk INSERT via `master_plan_bundle_apply` |
| `master-plan-extend`| `ship-plan` (`--version-bump`)         | new version row, lineage via `parent_plan_slug` + `version` |
| `stage-file`        | `ship-plan` (single bundle)            | task rows seeded inline             |
| `stage-authoring`   | `ship-plan` Phase 7 (`§Plan Digest` 3-section emit) | digest authored at bundle time     |
| old `ship-stage`    | `ship-cycle` (Pass A) + `ship-final` (Pass B + closeout) | stage-atomic batch implement       |
| `stage-decompose`   | `design-explore` Phase 4 (lean YAML emits decomposed tasks) | inline at exploration time          |
| `code-review`       | `verify-loop` + branch self-review (`agent-code-review-self.md`) | review baked into verification |
| `plan-review`       | `design-explore` Phase 1 grilling exit token | gating moved upstream              |

**Hard-remove scope**: `ia/skills/{slug}/` · `.claude/agents/{slug}.md` · `.claude/commands/{slug}.md` · `.cursor/rules/cursor-skill-{slug}.mdc` · cross-refs in `docs/**` · cross-refs in surviving skills' SKILL.md prose.

**Preserved for rollback**: `ia/skills/_pre_retire_archive.tar.gz` (committed once, immutable, bundles all 8 skill dirs + matching `.claude/` + `.cursor/` shadows).

---

## 3. Locked decisions (all Q5 recommended options adopted)

### D1 — Versioning convention: **version column on same slug**

- Use existing `ia_master_plans.version int` column.
- New version of a plan → `INSERT new row, same slug, version + 1, parent_plan_slug = slug`.
- `master_plan_bundle_apply` UPSERT-gated on existing slug (see D5 mitigation #12).
- Slug-keyed surfaces (`master_plan_state(slug)`, `BACKLOG.md` section, exploration filename `{slug}-v{N+1}.md`) stay clean.
- Lineage queryable via `select * from ia_master_plans where slug = $1 order by version desc`.

### D2 — Seeded-digest ship policy: **halt-on-encounter**

- Backfill writes `<!-- seeded: backfill_v1 -->` marker as first line of every task body created by Phase 0.
- `ship-cycle` Pass A pre-scans task body for marker → halts with `seeded_digest_requires_human_review` (NEVER auto-implements seeded body).
- `ship-final` blocks closeout when `select count(*) from ia_tasks where master_plan_id = $1 and body like '%<!-- seeded: backfill_v1 -->%' > 0` — error code `seeded_tasks_must_be_upgraded_before_close`.
- Upgrade path: human runs `/design-explore --resume {slug}` → re-emits proper digest → `ship-plan --version-bump` lifts the marker via UPSERT.

### D3 — Rollback strategy: **git revert + DB rollback + preserved skill archive**

- Per-phase atomic commits (Phase 0–4 = 5 commits) — each phase reversible independently.
- Annotated rollback tag: `pre-ship-protocol-stage-5` cut at HEAD before Phase 0.
- DB rollback script: `db/migrations/rollback/00XX_ship_protocol_stage_5_revert.sql` — drops backfill marker rows, resets `backfilled` flag, removes `BACKFILL-` sequence.
- Skill archive: `ia/skills/_pre_retire_archive.tar.gz` — single-step restore = `tar xzf` + `npm run skill:sync:all`.

### D4 — Soft-fail window for retired-ref validator: **7-day grace**

- New validator `validate:retired-skill-refs` lands in `package.json` at Phase 3.
- First 7 days post-migration: validator emits warnings (exit 0) + telemetry counts.
- Day 8: promote to hard-fail (exit 1) via simple bool flip. Calendar-driven, captured in master-plan change log.
- Scope: source surfaces only (`ia/skills/**/SKILL.md`, `.claude/agents/**`, `.claude/commands/**`, `.cursor/rules/**`, `docs/**`). Excludes `git log`, `CHANGELOG`, archive doc.

---

## 4. Adopted mitigations (8 highest-leverage from 24-case review)

| # | Mitigation | Phase | Maps to failure case |
|---|------------|-------|----------------------|
| #2 | Seeded digests carry **NO `§Red-Stage Proof` anchors** — placeholder text only, no file:line refs | P0 backfill | F2 (anchor drift) |
| #5 | Pre-scan classifies stages as `present_complete` / `partial` / `missing` — backfill operates ONLY on `missing` | P0 backfill | F5 (overwrite human work) |
| #6 | Seeded marker forces `ship-cycle` Pass A halt — never silent execution | P0 + P2 | F6 (silent ship of placeholder) |
| #11 | Versioned exploration filename `{slug}-v{N+1}.md` — old `{slug}.md` immutable | P2 chain ext | F11 (filename collision) |
| #12 | `master_plan_bundle_apply` UPSERT path **gated on `WHERE backfilled = true`** — guards against accidental overwrites of human-authored plans | P2 chain ext | F12 (silent overwrite) |
| #19 | Atomic per-phase commits + rollback tag `pre-ship-protocol-stage-5` | P4 audit | F19 (mid-migration corruption) |
| #20 | `ship-final` blocks on `seeded_count > 0` for the parent plan | P4 + P2 | F20 (close with placeholders) |
| #23 | Preserved skill archive `ia/skills/_pre_retire_archive.tar.gz` — single-step restore | P1 hard-remove | F23 (irreversible removal) |

---

## 5. 4-phase migration design

### Phase 0 — Data backfill (DB-only, no source-tree edits)

**Goal**: every in-flight master plan gets minimal `§Plan Digest` body so `ship-cycle` can read it without crashing — but each task carries the seeded marker so it can never be auto-shipped.

**Steps**:
1. New migration `db/migrations/00XX_backfill_seeded_digests.sql`:
   - Add column `ia_tasks.backfilled boolean default false`.
   - Add column `ia_master_plans.backfill_version text` (= `backfill_v1` when migrated by this phase).
   - New sequence `backfill_id_seq` (separate from prefix sequences) for placeholder task ids prefixed `BACKFILL-`.
2. Pre-scan classifier:
   - For each `ia_stages` row in non-done plans:
     - Stage's tasks all have non-empty `§Plan Digest` body → `present_complete` (skip).
     - Some tasks missing → `partial` (skip — never overwrite human work).
     - Stage has zero tasks → `missing` (decompose + backfill).
3. Backfill writer (only on `missing`):
   - Insert N placeholder tasks (N = stage objective bullet count, default 1).
   - Body shape:
     ```
     <!-- seeded: backfill_v1 -->
     ## §Plan Digest

     ### §Goal
     Placeholder — re-author via `/design-explore --resume {slug}` before ship.

     ### §Red-Stage Proof
     _(intentionally empty — backfill emits no anchors)_

     ### §Work Items
     - [ ] (placeholder)
     ```
   - Set `backfilled = true`, `task_id` = next `BACKFILL-` from sequence.
4. Plan-level mark: `update ia_master_plans set backfill_version = 'backfill_v1' where id in (...)`.

**Idempotent**: re-run skips rows where `backfilled = true` already set.

### Phase 1 — Hard-remove (source tree)

**Goal**: zero presence of retired skills in `ia/skills/` + `.claude/` + `.cursor/` + `docs/`.

**Steps**:
1. Snapshot: create `ia/skills/_pre_retire_archive.tar.gz` covering all 8 retired skill dirs + matching agents/commands/cursor-rules.
2. `git rm -r ia/skills/{master-plan-new,master-plan-extend,stage-file,stage-authoring,ship-stage,stage-decompose,code-review,plan-review}`.
3. `git rm .claude/agents/{slug}.md .claude/commands/{slug}.md .cursor/rules/cursor-skill-{slug}.mdc` (the 8 generated triplets).
4. `npm run skill:sync:all` — regenerates remaining set, asserts no orphan references.
5. `sed -i.bak` sweep across `docs/**/*.md` + surviving `ia/skills/**/SKILL.md` for retired slugs (replace with successor pointer text).
6. Update `CLAUDE.md` task-routing table: drop retired rows.

### Phase 2 — Chain extensions (new skill features)

**Goal**: new chain handles the 3 paths old chain handled (new plan / extend plan / decompose stage).

**Steps**:
1. `design-explore --resume {slug}` mode — reads existing plan from DB, regrills only stages with `backfilled = true` or `partial`, re-emits lean YAML pointing at next version.
2. `ship-plan --version-bump` flag — inserts new `ia_master_plans` row at `version + 1` for same slug; UPSERT-gates `master_plan_bundle_apply` on `WHERE backfilled = true OR version > existing_max_version`.
3. Versioned exploration filename: `{slug}-v{N+1}.md` (Phase 4 emit path).
4. New MCP tool `master_plan_lineage(slug)` — returns `[{version, parent_plan_slug, created_at, closed_at}]` ordered ASC.

### Phase 3 — Validators (gates)

**Goal**: drift gates prevent re-introduction of retired refs + catch seeded leakage.

**New validators (added to `npm run validate:all`)**:
- `validate:retired-skill-refs` — greps source surfaces for retired slugs. Soft-fail 7 days, then hard-fail (D4).
- `validate:plan-digest-coverage` — every non-done task has non-empty `§Plan Digest` body; seeded marker classified separately.
- `validate:seeded-task-stale` — flags `backfilled = true` rows older than 30 days (signals human-review backlog).

### Phase 4 — Audit + recipe

**Goal**: rollback-ready, observable.

**Artifacts**:
- Per-phase atomic commits (5 separate commits, each `feat(ship-protocol-stage-5): phase {N} — {summary}`).
- Annotated tag `pre-ship-protocol-stage-5` at HEAD before Phase 0.
- DB rollback script `db/migrations/rollback/00XX_ship_protocol_stage_5_revert.sql`.
- Self-host dogfood: `ship-protocol` master plan closes via `/ship-final ship-protocol` — journal entry confirms close-via-new-pipeline.

---

## 6. Failure-mode table (24 cases, full reference)

### Phase 0 — Backfill failure modes

| # | Failure | Severity | Mitigation |
|---|---------|----------|------------|
| F1 | Backfill writes to plan that's actually complete (counts wrong) | HIGH | Pre-scan classifier (M#5) — never overwrite `present_complete` |
| F2 | Backfill emits stale `§Red-Stage Proof` anchors (file:line refs that don't exist) | HIGH | **M#2 — no anchors in seeded digests, placeholder text only** |
| F3 | Backfill collides with `BACKFILL-` sequence already used elsewhere | LOW | Dedicated sequence, separate from prefix seqs |
| F4 | Backfill runs twice → duplicate task rows | MED | `WHERE backfilled = false` guard, idempotent re-run |
| F5 | Backfill overwrites partial human-authored stage | HIGH | **M#5 — `partial` class skipped entirely** |

### Phase 1 — Hard-remove failure modes

| # | Failure | Severity | Mitigation |
|---|---------|----------|------------|
| F21 | `git rm` strips a still-referenced surface, build breaks | HIGH | `npm run skill:sync:all` post-rm catches orphans |
| F22 | sed sweep misses ref in code comment / commit-msg template | MED | Validator F#3 catches; soft-fail window |
| F23 | Removal irreversible without history dive | HIGH | **M#23 — `_pre_retire_archive.tar.gz` preserved** |
| F24 | Cursor rules cache stale post-removal | LOW | `.cursor/rules/` regenerates on next session start |

### Phase 2 — Chain extensions failure modes

| # | Failure | Severity | Mitigation |
|---|---------|----------|------------|
| F6 | Seeded digest auto-shipped by ship-cycle Pass A | HIGH | **M#6 — halt-on-marker (D2)** |
| F7 | `--resume` mode drifts from baseline plan, emits wrong slug | MED | Lineage check via `master_plan_lineage` MCP |
| F8 | `--version-bump` creates orphan version (parent_plan_slug NULL) | LOW | NOT NULL constraint + bundle_apply enforces |
| F9 | UPSERT path silently overwrites human plan | HIGH | **M#12 — `WHERE backfilled = true` gate** |
| F10 | Two parallel `--version-bump` runs race | MED | Migration 0067 advisory lock pattern |
| F11 | Exploration filename `{slug}.md` collision | MED | **M#11 — versioned filename** |
| F12 | bundle_apply INSERT-only behavior breaks UPSERT | HIGH | **M#12 — gated path; non-gated stays INSERT-only** |

### Phase 3 — Validators failure modes

| # | Failure | Severity | Mitigation |
|---|---------|----------|------------|
| F13 | retired-skill-refs validator catches false positive in `git log` | LOW | Scope-exclude `git log`, `CHANGELOG`, archive doc |
| F14 | plan-digest-coverage flags seeded marker as missing | MED | Marker classified as `seeded`, separate band |
| F15 | seeded-task-stale fires on legitimate WIP | LOW | 30-day grace window; warning not fail |
| F16 | Validators block Stage 5's own closing commit | MED | 7-day soft-fail (D4) |
| F17 | New validator slow → `validate:all` timeout | LOW | Bounded grep + path filter |

### Phase 4 — Audit + cross-cutting failure modes

| # | Failure | Severity | Mitigation |
|---|---------|----------|------------|
| F18 | Rollback tag missing → no clean revert point | HIGH | Tag-cut step is Phase 4 step 1 |
| F19 | Mid-migration corruption (Phase 2 commit half-applied) | HIGH | **M#19 — per-phase atomic commits** |
| F20 | `ship-final` closes plan with seeded tasks still present | HIGH | **M#20 — block on `seeded_count > 0`** |

---

## 7. Stage 5 task-table refresh — proposed (6 tasks, +1 vs current)

Current: TECH-12646…12650 (5 tasks). Refreshed: 6 tasks — adds **TECH-14103** for backfill migration + new validators (the new work surfaced by Q3 hard-removal directive).

| Task ID    | Title                                                                  | Primary surfaces |
|------------|------------------------------------------------------------------------|------------------|
| TECH-12646 | design-explore Phase 1 exit token + relentless polling                 | `ia/skills/design-explore/SKILL.md` Phase 1 |
| TECH-12647 | design-explore Phase 4 + lean YAML emitter + `--resume` mode          | `ia/skills/design-explore/SKILL.md` Phase 4 |
| TECH-12648 | **HARD-REMOVE retired skills + preserved archive + sed sweep**         | `git rm` 8 skill dirs + 24 generated shadows; `ia/skills/_pre_retire_archive.tar.gz` |
| TECH-14103 | **NEW — Backfill migration + UPSERT gating + new validators**          | `db/migrations/00XX_backfill_seeded_digests.sql` + `master_plan_bundle_apply` UPSERT path + `validate:retired-skill-refs` + `validate:plan-digest-coverage` + `validate:seeded-task-stale` + `master_plan_lineage` MCP |
| TECH-12649 | Doc updates — MASTER-PLAN-STRUCTURE + lifecycle + verification + CLAUDE | `docs/MASTER-PLAN-STRUCTURE.md`, `docs/agent-lifecycle.md`, `docs/agent-led-verification-policy.md`, `CLAUDE.md` |
| TECH-12650 | Self-host dogfood — close ship-protocol via ship-final                  | `/ship-final ship-protocol` execution + journal entry |

**Sequencing**:
1. TECH-14103 (backfill migration) **first** — unblocks all in-flight plans BEFORE removal.
2. TECH-12646 + TECH-12647 — design-explore extensions.
3. TECH-12648 — hard-remove (only safe after #1 + #2 land).
4. TECH-12649 — docs catch up.
5. TECH-12650 — self-host close.

---

## 8. Resume pointers

- **Stage 5 DB row**: ship-protocol slug, stage_id `5`, status `pending`, 5 task rows (TECH-12646–12650) currently — needs add of TECH-14103.
- **Last commit on branch**: `7dab3313` (game-ui-catalog-bake-stage-5).
- **Stage 4 DB-only commit**: closeout already in DB; ship-final dogfood (TECH-12650) re-runs.
- **Drift fix prereq**: `47b0665e` (bigint µs cursor + panel_child kind discriminator).
- **Branch**: `feature/asset-pipeline`.

---

## 9. Stage 5 — refreshed shape (DB-ready draft)

### 9.1 Refreshed `objective`

> Hard-remove 8 retired skills (`master-plan-new`, `master-plan-extend`, `stage-file`, `stage-authoring`, old `ship-stage`, `stage-decompose`, `code-review`, `plan-review`) — `git rm` source surfaces, NOT `_retired/`. Backfill in-flight master plans with seeded `§Plan Digest` bodies (halt-on-marker pattern) so the new chain (`design-explore` → `ship-plan` → `ship-cycle` → `ship-final`) is unblocked without overwriting human work. Extend `design-explore` (Phase 1 grilling exit token + Phase 4 lean YAML emitter + `--resume` mode) and `ship-plan` (`--version-bump`) to handle plan extension + decomposition paths previously held by the retired skills. Land 3 new validators (`validate:retired-skill-refs` 7-day soft-fail → hard-fail, `validate:plan-digest-coverage`, `validate:seeded-task-stale`). Self-host: this plan dogfoods its own closeout via `/ship-final ship-protocol`.

### 9.2 Refreshed `exit_criteria`

```
- Phase 0 backfill landed: db/migrations/00XX_backfill_seeded_digests.sql adds ia_tasks.backfilled bool + ia_master_plans.backfill_version + BACKFILL- sequence; pre-scan classifier writes seeded marker `<!-- seeded: backfill_v1 -->` ONLY on `missing` stages; `partial` + `present_complete` skipped.
- Phase 1 hard-remove: 8 retired skill dirs git-rm'd from ia/skills/; 24 generated shadows (.claude/agents/, .claude/commands/, .cursor/rules/) git-rm'd; ia/skills/_pre_retire_archive.tar.gz committed; sed sweep across docs/** + surviving SKILL.md prose; npm run skill:sync:all clean.
- Phase 2 chain extensions: design-explore --resume mode reads existing plan from DB + regrills only backfilled stages; ship-plan --version-bump inserts new ia_master_plans row at version+1 same slug; master_plan_bundle_apply UPSERT path gated on `WHERE backfilled = true`; versioned exploration filename `{slug}-v{N+1}.md`; new MCP tool master_plan_lineage(slug) lands.
- Phase 3 validators: validate:retired-skill-refs (7-day soft-fail per D4), validate:plan-digest-coverage (seeded-marker classified separately), validate:seeded-task-stale (30-day grace window) all wired into npm run validate:all.
- Phase 4 audit + recipe: rollback tag `pre-ship-protocol-stage-5` cut at HEAD before Phase 0; per-phase atomic commits (5 separate commits); db/migrations/rollback/00XX_ship_protocol_stage_5_revert.sql.
- ship-cycle Pass A halts on seeded marker with `seeded_digest_requires_human_review` (red-stage proof test asserts).
- ship-final blocks closeout when seeded_count > 0 with `seeded_tasks_must_be_upgraded_before_close` (red-stage proof test asserts).
- Self-host dogfood: this `ship-protocol` master plan closes via `/ship-final ship-protocol`; journal entry confirms close-via-new-pipeline.
- Docs updated: docs/MASTER-PLAN-STRUCTURE.md (3-section digest, version model), docs/agent-lifecycle.md (new pipeline diagram + seam matrix, retired skills removed), docs/agent-led-verification-policy.md (validate:fast band), CLAUDE.md (task-routing table, retired rows dropped).
- Visibility delta: `master_plan_state ship-protocol` returns version=1 closed_at IS NOT NULL; ls ia/skills/{master-plan-new,master-plan-extend,stage-file,stage-authoring,ship-stage,stage-decompose,code-review,plan-review} returns 0 hits (post-rm); npm run validate:retired-skill-refs returns warnings only during 7-day grace then hard-fails.
```

### 9.3 Refreshed task table — 6 rows

| seq | task_id    | title                                                                  | depends_on        | seed `§Plan Digest` ref |
|-----|------------|------------------------------------------------------------------------|-------------------|--------------------------|
| 1   | TECH-14103 | Backfill migration + UPSERT gating + new validators                    | (none)            | §5 Phase 0 + Phase 3     |
| 2   | TECH-12646 | design-explore Phase 1 exit token + relentless polling                 | (none)            | §5 Phase 2 step 1        |
| 3   | TECH-12647 | design-explore Phase 4 + lean YAML emitter + `--resume` mode          | TECH-12646, TECH-14103 | §5 Phase 2 step 1–3 |
| 4   | TECH-12648 | HARD-REMOVE retired skills + preserved archive + sed sweep             | TECH-14103, TECH-12647 | §5 Phase 1            |
| 5   | TECH-12649 | Doc updates — MASTER-PLAN-STRUCTURE + lifecycle + verification + CLAUDE | TECH-12648        | §5 Phase 1 step 5–6     |
| 6   | TECH-12650 | Self-host dogfood — close ship-protocol via ship-final                 | TECH-12649, TECH-12648 | §5 Phase 4 + dogfood |

> **Rationale for sequencing**: backfill (TECH-14103) MUST land before hard-remove (TECH-12648) — otherwise in-flight plans break mid-migration. design-explore extensions (TECH-12646, TECH-12647) before hard-remove so new chain has full coverage. Docs (TECH-12649) trail removal so they accurately describe post-state. Self-host (TECH-12650) last — closes the loop.

### 9.4 Apply recipe (next session)

```
1. mcp__territory-ia__stage_update slug=ship-protocol stage_id=5 \
     objective="<§9.1>" exit_criteria="<§9.2>"
2. mcp__territory-ia__task_insert prefix=TECH stage_id=5 slug=ship-protocol \
     title="Backfill migration + UPSERT gating + new validators" \
     status=pending \
     body="<seed digest pointing at §5 Phase 0 + Phase 3>"
   → expect TECH-14103
3. (Existing TECH-12646–12650 stay; only TECH-14103 added.)
4. /stage-authoring ship-protocol 5 — to populate full 3-section §Plan Digest per task body.
5. /ship-cycle ship-protocol 5 — Pass A batch implement.
```

> **Pre-decompose gate**: do NOT run `/stage-decompose` — that skill is one of the 8 being retired. Stage 5 already has explicit task table; decompose phase is upfront in this doc.

---

_End of locked decisions._
