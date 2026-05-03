# MCP Lifecycle Plans — Supersession Deep-Dive (2026-05-01)

Per-stage audit of two master plans flagged CRITICAL → DROP in `docs/master-plan-drift-audit-2026-05-01.md`. Confirms each pending stage against shipped baseline before retirement.

Baseline shipped (all 100% Done):
- `db-lifecycle-extensions` 3/3
- `recipe-runner-phase-e` 13/13
- `parallel-carcass-rollout` 9/9
- `architecture-coherence-system` 4/4
- `asset-pipeline` 26/26

Verdict legend:
- **SUPERSEDED** — proposed surface already shipped; retire stage.
- **PARTIAL** — core shipped; one or two slivers remain (CI gate, generic wrapper, doc).
- **UNIQUE** — proposed surface not present in MCP / scripts / DB; salvage candidate.
- **UNCLEAR** — evidence inconclusive; needs probe before drop.

Method: per stage, cross-check proposed tool / table / behavior against `tools/mcp-ia-server/src/tools/`, `tools/scripts/`, DB migrations, and shipped plan stage rollups. Citations point at the shipped stage that delivered each item.

---

## §1 Plan A — `mcp-lifecycle-tools-opus-4-7-audit` (17 stages)

| Stage | Title | Proposes | Shipped via | Verdict |
| --- | --- | --- | --- | --- |
| 1 | Audit `task_*` tool surface | Inventory + retire dead `task_*` tools, document gaps | All tasks archived; `task_state`, `task_status_flip`, `task_bundle`, `task_spec_*`, `task_raw_markdown_write` shipped via db-lifecycle-extensions Stage 1 | SUPERSEDED |
| 2 | Audit `stage_*` tool surface | Map stage tools, retire shadows | All tasks archived; `stage_state`, `stage_render`, `stage_bundle`, `stage_closeout_apply`, `stage_closeout_diagnose`, `stage_decompose_apply`, `stage_verification_flip` shipped via db-lifecycle Stage 1 + 3 | SUPERSEDED |
| 3 | Standardize envelope `{ok, payload, error}` on lifecycle reads | Refactor reads to envelope shape | All tasks archived; envelope visible across `task_bundle`, `stage_bundle`, `master_plan_state` (db-lifecycle Stage 1) | SUPERSEDED |
| 4 | Wrap mutations in PG transactions | Txn wrapping on closeout-class tools | All tasks archived; `stage_closeout_apply` is PG-txn wrapped (db-lifecycle Stage 1 acceptance line); `task_status_flip`, `stage_verification_flip` go through same path | SUPERSEDED |
| 5 | Backfill `arch_surfaces` + write helper | `arch_surfaces_backfill` MCP tool + table | All tasks archived; `arch_surfaces_backfill` ships in MCP tool list; backed by `architecture-coherence-system` 4/4 | SUPERSEDED |
| 6 | Caller-allowlist sweep + CI gate `validate:mcp-envelope-shape` | Sweep callers, add CI envelope-shape check | Caller sweep implicit in Stages 3/4 closeout; `caller-allowlist.ts` exists; CI gate `validate:mcp-envelope-shape` not in `package.json` scripts | PARTIAL |
| 7 | Composite `issue_context_bundle` + `lifecycle_stage_context` | Two composites for cache-warm reads | Both shipped — `issue-context-bundle.ts` + `lifecycle-stage-context.ts` in `src/tools/`; visible in MCP tool list | SUPERSEDED |
| 8 | Stage / task claim helpers | `section_claim`, `stage_claim`, `claim_heartbeat`, `claims_sweep` | All shipped via `parallel-carcass-rollout` 9/9; tools live in MCP list and have backing PG tables | SUPERSEDED |
| 9 | `master_plan_sections` + sectioned render | Section-aware plan render for parallel claims | All tasks archived; `master_plan_sections` ships; backed by parallel-carcass | SUPERSEDED |
| 10 | `master_plan_locate` + `master_plan_next_pending` | Two helpers for locator + next-pending walk | Both shipped: `master-plan-locate.ts`, `master-plan-next-pending.ts` in `src/tools/`; visible in MCP list | SUPERSEDED |
| 11 | IA authorship — `glossary_row_create`, `spec_section_append`, `rule_create` | Three write tools for glossary / spec / rule edits | None present in `src/tools/`; Grep on those names returns only `caller-allowlist.ts`; no DB tables backing | UNIQUE |
| 12 | Unity bridge pipeline tools — `unity_bridge_pipeline`, `unity_bridge_jobs_list` | Pipeline + job-list helpers over the bridge | Bridge tools shipped (`unity_bridge_command`, `unity_bridge_get`, `unity_bridge_lease`) but no `_pipeline` / `_jobs_list` variant in tool list | UNIQUE |
| 13 | `journal_entry_sync` with SHA-256 dedup | Append-only journal sync with content hash | `journal_append` exists in MCP; `_sync` + dedup hash logic not confirmed in source | UNIQUE |
| 14 | `master_plan_*` mutation surface — `master_plan_health`, `master_plan_cross_impact_scan`, `master_plan_next_actionable` | Three plan-mutation/read tools | All shipped via db-lifecycle Stage 2: `master-plan-health.ts`, `master-plan-cross-impact-scan.ts`, `master-plan-next-actionable.ts` in `src/tools/` | SUPERSEDED |
| 15 | Generic `mutation_batch` envelope | One generic batch tool wrapping arbitrary mutations in single txn | Per-tool batches shipped (`task_batch_insert`, `stage_decompose_apply`, `catalog_bulk_action`); generic `mutation_batch` not present | PARTIAL |
| 16 | `dry_run` flag on mutation tools | Optional dry-run preview path on writes | No mutation tool in current source accepts `dry_run`; closest is `plan_apply_validate` (read-only check) | UNIQUE |
| 17 | Parse cache + dist launcher hardening | `parse-cache.ts`, manifest cache, dist launcher for MCP startup speed | All shipped: `tools/mcp-ia-server/.cache/parse-cache.json` exists; `parser/parse-cache.ts` + tests (`parse-cache.test.ts`, `backlog-yaml-manifest-cache.test.ts`, `progressive-disclosure.test.ts`); `bin/launch.mjs` referenced in `.mcp.json` | SUPERSEDED |

**Plan A counts:** SUPERSEDED 11 · PARTIAL 2 · UNIQUE 4 · UNCLEAR 0

---

## §2 Plan B — `backlog-yaml-mcp-alignment` (18 stages incl. 4.1 + 4.2)

| Stage | Title | Proposes | Shipped via | Verdict |
| --- | --- | --- | --- | --- |
| 1 | Schema v2 — extend yaml with `parent_plan` + `task_key` + 7 locator fields | YAML schema bump | All tasks archived; live in current backlog yaml; `validate:backlog-yaml` honors fields | SUPERSEDED |
| 2 | Migrate existing backlog yaml to v2 | Backfill locator fields across active + archive yaml | All tasks archived; `migrate-backlog-to-yaml.mjs` + `backfill-parent-plan-locator.{sh,mjs}` shipped under `tools/scripts/` | SUPERSEDED |
| 3 | Validator updates for v2 | `validate:backlog-yaml` honors new fields | All tasks archived; validator extended (parallel-carcass tail) | SUPERSEDED |
| 4 | Concurrency hardening — flock on `materialize-backlog.sh` | flock guard + per-domain lockfile | All tasks archived; `tools/scripts/materialize-backlog.sh` lines 33-71 use `LOCK_FILE=ia/state/.materialize-backlog.lock`, `flock -x 9` | SUPERSEDED |
| 4.1 | flock sub-stage — runtime-state + closeout + id-counter lockfiles | Per-domain lockfile guardrail | All tasks archived; `.runtime-state.lock`, `.closeout.lock`, `.id-counter.lock` enforced (see `ia/rules/invariants.md` Guardrail) | SUPERSEDED |
| 4.2 | flock sub-stage — remaining mutation paths (TECH-355 / TECH-356 / TECH-357) | Backlog row-level flock (4 pending tasks in plan) | TECH-355/356/357 still pending in plan but materialize-backlog flock already in place; tasks may be redundant trackers | PARTIAL |
| 5 | Validator extensions — `related`, `depends_on_raw` | Script-side extension to backlog yaml validator | Validator script may carry partial; no MCP tool surface; needs probe of `validate-backlog-yaml.mjs` for these specific keys | UNIQUE |
| 6 | `backlog_record_create` MCP tool | Authoring tool for new backlog records via MCP | Not present — `backlog-record-create.ts` absent from `src/tools/`; only `backlog_record_validate`, `backlog_list`, `backlog_search`, `backlog_issue`, `reserve_backlog_ids` shipped | UNIQUE |
| 7 | `backlog_search` MCP tool | Search across backlog yaml | Shipped: `backlog-search.ts` in `src/tools/`; visible in MCP list | SUPERSEDED |
| 8 | `backlog_list` MCP tool | List filter over backlog yaml | Shipped: `backlog-list.ts` in `src/tools/`; visible in MCP list | SUPERSEDED |
| 9 | `backlog_record_validate` MCP tool | Single-record validator wrapping `validate:backlog-yaml` | Shipped: `backlog-record-validate.ts` in `src/tools/` | SUPERSEDED |
| 10 | `master_plan_locate` + `master_plan_next_pending` | Same two helpers as Plan A Stage 10 | Shipped: `master-plan-locate.ts`, `master-plan-next-pending.ts`; visible in MCP list | SUPERSEDED |
| 11 | `master_plan_health` MCP tool | Plan-level health roll-up | Shipped via db-lifecycle Stage 2: `master-plan-health.ts`; backed by materialized view | SUPERSEDED |
| 12 | Skill-body wiring — `/file-issue` uses `backlog_record_create` | Skill switches to MCP authoring tool | Tool itself not shipped (Stage 6 UNIQUE); skill bodies still call shell scripts; can't wire what doesn't exist | PARTIAL |
| 13 | Skill-body wiring — `/audit` reads via `backlog_search` | Skill switches to MCP search tool | `backlog_search` exists; audit / triage skills still use raw yaml reads in body — partial wiring | PARTIAL |
| 14 | Skill-body wiring — lifecycle skills consume `master_plan_*` reads | Lifecycle skills cut over to plan tools | Plan tools exist; some skill bodies cut over (`/stage-file`, `/ship-stage`); not 100% audited | PARTIAL |
| 15 | Validator strict-flip — fail on legacy schema after grace window | Policy + CI strict-flip | Not yet flipped; validator still permissive on optional locator fields; policy task | UNIQUE |
| 16 | Archive backfill — `--archive` flag on `backfill-parent-plan-locator.mjs` | One-shot archive sweep | Script exists; `--archive` flag presence not confirmed; tracked-in-archive coverage may be partial | UNIQUE |
| `master_plan_cross_impact_scan` | (referenced in plan body as Stage 14 footnote) | Cross-impact reads across plans | Shipped via db-lifecycle Stage 2 — already counted under Stage 14 wiring above | SUPERSEDED (covered) |

**Plan B counts:** SUPERSEDED 9 · PARTIAL 4 · UNIQUE 4 · UNCLEAR 0

(Stage 4.2 + Stages 12/13/14 share PARTIAL bucket; Stages 5/6/15/16 share UNIQUE bucket. Plan-body footnote on cross-impact is already SUPERSEDED via db-lifecycle Stage 2.)

---

## §3 Salvage list — UNIQUE / PARTIAL items worth extracting

Drop both plans, but lift the following into a tight post-MVP doc / new short plan **before retirement**. Each line: source stage → suggested salvage target.

### From Plan A

- **Plan A Stage 6 (PARTIAL)** — CI gate `validate:mcp-envelope-shape`. One-script addition to `package.json` + `tools/scripts/validate/`. Salvage → `docs/mcp-server-post-mvp-extensions.md` §CI gates.
- **Plan A Stage 11 (UNIQUE)** — IA authoring tools (`glossary_row_create`, `spec_section_append`, `rule_create`). Currently glossary/spec/rule edits are direct file writes. Salvage → backlog FEAT for "MCP IA authorship surface" if seam friction recurs; else leave as direct edit (low frequency).
- **Plan A Stage 12 (UNIQUE)** — Unity bridge pipeline (`unity_bridge_pipeline`, `unity_bridge_jobs_list`). Salvage → backlog FEAT only if multi-step bridge flows become common; current `unity_bridge_command` + `_get` + `_lease` cover MVP.
- **Plan A Stage 13 (UNIQUE)** — `journal_entry_sync` with SHA-256 dedup. `journal_append` already exists; dedup is the delta. Salvage → backlog TECH-issue when journal collisions actually observed.
- **Plan A Stage 15 (PARTIAL)** — Generic `mutation_batch`. Per-tool batches cover current needs. Salvage → defer; revisit if a third batch surface emerges.
- **Plan A Stage 16 (UNIQUE)** — `dry_run` flag on mutations. `plan_apply_validate` covers plan-level case. Salvage → add to mutation-tool checklist when next mutation tool authored; not a standalone plan.

### From Plan B

- **Plan B Stage 4.2 (PARTIAL)** — TECH-355/356/357 row-level flock. Probe each TECH yaml for actual scope; if redundant with materialize-backlog flock, archive the rows; if real, file as bare backlog issues.
- **Plan B Stage 5 (UNIQUE)** — Validator extensions for `related` / `depends_on_raw`. Script-side, ~half-day. Salvage → file as TECH-issue against `tools/scripts/validate/validate-backlog-yaml.mjs`.
- **Plan B Stage 6 (UNIQUE)** — `backlog_record_create` MCP tool. Genuine missing surface if MCP-first authoring is desired. Salvage → backlog FEAT, gated on whether `/file-issue` skill needs MCP cutover.
- **Plan B Stages 12/13/14 (PARTIAL)** — Skill-body wiring to MCP plan tools. Mechanical edits per skill. Salvage → single TECH-issue "audit lifecycle skill bodies for MCP-first reads".
- **Plan B Stage 15 (UNIQUE)** — Validator strict-flip after grace window. Policy decision; salvage → MEMORY entry "flip backlog-yaml validator strict mode after Q3" + delete from plan.
- **Plan B Stage 16 (UNIQUE)** — Archive backfill `--archive` flag. Probe `backfill-parent-plan-locator.mjs` for the flag; if missing, file as TECH-issue (1-2 hour script edit).

### Post-MVP extension doc target

Recommend single new doc: `docs/mcp-lifecycle-plans-post-mvp-extensions.md` — lifts the 4 Plan A UNIQUE items + 4 Plan B UNIQUE items into a concise menu, ordered by friction-driven salvage rather than waterfall stage sequence.

---

## §4 Final recommendation

**Plan A — `mcp-lifecycle-tools-opus-4-7-audit`: PARTIAL DROP**

- 11/17 stages SUPERSEDED → retire immediately.
- 2/17 stages PARTIAL → 1 CI gate sliver (Stage 6) + 1 deferred generic batch (Stage 15) → file 1 TECH-issue, drop the rest.
- 4/17 stages UNIQUE (Stages 11, 12, 13, 16) → roll into post-MVP extensions doc; do NOT keep as live plan stages.
- **Action:** mark plan retired in `master_plan_state`; move salvage items to `docs/mcp-lifecycle-plans-post-mvp-extensions.md`; archive plan markdown to `_retired/`.

**Plan B — `backlog-yaml-mcp-alignment`: PARTIAL DROP**

- 9/18 stages SUPERSEDED → retire immediately.
- 4/18 stages PARTIAL (4.2 row-level flock + 12/13/14 skill wiring) → consolidate into 1 audit TECH-issue.
- 4/18 stages UNIQUE (5, 6, 15, 16) → roll into post-MVP extensions doc; gate Stage 6 (`backlog_record_create`) on whether MCP-first authoring is a 2026 priority.
- **Action:** mark plan retired; salvage 4 UNIQUE items + 1 consolidated wiring TECH-issue; archive plan markdown to `_retired/`.

**Combined drop verdict:** retire both plans. Net surface lost = 0 (all SUPERSEDED items already shipped). Net surface deferred to post-MVP doc = 8 items. Net new TECH-issues = 2 (skill wiring audit, validator extension). One new short doc replaces two stalled long plans.

**Next step:** author `docs/mcp-lifecycle-plans-post-mvp-extensions.md` (one-pass salvage menu) + flip both plans to retired via `master_plan_state` once the salvage doc lands.
