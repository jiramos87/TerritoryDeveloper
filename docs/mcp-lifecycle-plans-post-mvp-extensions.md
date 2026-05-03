# MCP Lifecycle Plans — Post-MVP Extensions (2026-05-01)

> **Created:** 2026-05-01
>
> **Status:** Salvage menu — pre-retirement of two stalled plans (`mcp-lifecycle-tools-opus-4-7-audit`, `backlog-yaml-mcp-alignment`).
>
> **Source plans (DB slugs):** `mcp-lifecycle-tools-opus-4-7-audit` · `backlog-yaml-mcp-alignment` — both flipped retired after this doc lands.
>
> **Source audits:** `docs/master-plan-drift-audit-2026-05-01.md` (CRITICAL → DROP verdict) · `docs/mcp-lifecycle-plans-supersession-deep-dive-2026-05-01.md` (per-stage cross-check).
>
> **Architecture-locks alignment:** 2026-04-22 architecture lock-set (offline-first, three-way data split, SQL-authority migrations). Salvage items below stay inside `territory-ia` MCP surface — no new server, no Neon revival.

---

## 1. Context — why retire both plans

Drift audit flagged both plans CRITICAL: shipped baseline (`db-lifecycle-extensions` 3/3, `parallel-carcass-rollout` 9/9, `architecture-coherence-system` 4/4, `recipe-runner-phase-e` 13/13, `asset-pipeline` 26/26) covers 11/17 stages of Plan A + 9/18 stages of Plan B as SUPERSEDED. Keeping both plans live blocks Stage `validate:master-plan-status` CI gate + drains triage cycles on stage rollups that already shipped through other plans.

Deep-dive (`mcp-lifecycle-plans-supersession-deep-dive-2026-05-01.md` §3 Salvage list) confirms 8 items worth lifting before retirement — split 4 + 4 across Plan A / Plan B, each UNIQUE or PARTIAL with a clear delta vs current MCP surface. Salvage menu below preserves item provenance (source stage id + verdict) so post-MVP filing can cite original stage exit criteria as evidence.

Net effect: 0 surface lost (all SUPERSEDED items already shipped) + 8 items deferred to this doc + 2 net-new TECH issues (consolidated A surface + consolidated B surface) replace 35 stalled stages.

---

## 2. Extension A — `mcp-lifecycle-tools-opus-4-7-audit` salvage

Per-item table sourced from deep-dive §3 + cross-checked against `tools/mcp-ia-server/src/tools/` (sanity scan 2026-05-01: confirmed absent).

| Source stage | Verdict | What survives | Why salvage | Proposed home |
|---|---|---|---|---|
| Stage 6 — Caller Sweep + Snapshot Tests + CI Gate | PARTIAL | `validate:mcp-envelope-shape` CI script (caller sweep + snapshot tests already shipped via Stages 3/4 closeout) | Last sliver of envelope-shape contract enforcement; one-script addition to `package.json` + `tools/scripts/validate/`. Without it, future envelope drift caught only at runtime. | TECH-XXX-A §Phase 1 |
| Stage 11 — IA Authorship Tools | UNIQUE | `glossary_row_create`, `glossary_row_update`, `spec_section_append`, `rule_create` with `caller_agent` gate + cross-ref validation + non-blocking index regen | Glossary / spec / rule edits currently raw file writes; no schema validation, no duplicate-term gate, no `caller_agent` allowlist enforcement. Friction recurs each time `/file-issue` or `/author` touches glossary. | TECH-XXX-A §Phase 2 |
| Stage 12 — Bridge Pipeline + Jobs List | UNIQUE | `unity_bridge_pipeline` (sync ≤30s / auto-async above ceiling) + `unity_bridge_jobs_list` query surface + timeout auto-attach in `unity_bridge_command` | Multi-step bridge flows (`enter_play_mode → get_console_logs → exit_play_mode`) currently require N sequential `unity_bridge_command` calls + manual lease management. Pipeline tool collapses to single envelope. `_jobs_list` lets agent poll long-running batches without holding lease. | TECH-XXX-A §Phase 3 |
| Stage 13 — Journal Lifecycle | UNIQUE | `journal_entry_sync` (idempotent upsert via SHA-256 `content_hash`) + 3-step migration (nullable column → batched backfill → NOT NULL) + cascade-delete on issue archive + `project_spec_closeout_digest.journaled_sections` field | Current `journal_append` lacks dedup — re-running closeout on a re-opened spec doubles entries. Friction recurs on every `/closeout` retry. | TECH-XXX-A §Phase 4 |
| Stage 15 — Transactional Batch | PARTIAL | Generic `mutation_batch` envelope (`all_or_nothing` / `best_effort` modes; in-memory snapshot rollback; flock on `tools/.mutation-batch.lock`) | Per-tool batches (`task_batch_insert`, `stage_decompose_apply`, `catalog_bulk_action`) cover MVP needs; generic wrapper deferred until 3rd batch surface emerges. Salvage retains the design but gates rollout on demand. | TECH-XXX-A §Phase 5 (deferred) |
| Stage 16 — Dry-run Preview | UNIQUE | `dry_run?: boolean` on every mutation + authorship tool; payload returns `{ diff, affected_paths, would_write: true }` without writing | `plan_apply_validate` covers plan-level case; mutation tools have no preview path. Lets `/closeout`, `/release-rollout`, `/stage-file` preview migrations before commit. | TECH-XXX-A §Phase 6 |

**Locked decisions carried forward** (from Plan A preamble — preserve in TECH-XXX-A):

- IA-authorship server split rejected — stays in `territory-ia` MCP, guarded by `caller_agent`.
- Hybrid bridge ceiling: `UNITY_BRIDGE_PIPELINE_CEILING_MS` env var (default 30 000 ms).
- Caller-agent allowlist source of truth: `tools/mcp-ia-server/src/auth/caller-allowlist.ts`.
- Journal `content_hash` dedup: 3-step migration (nullable column → batched SHA-256 backfill → NOT NULL).
- Composite core vs optional sub-fetch: core fail → `ok: false`; optional fail → `meta.partial` tick, `ok: true`.

**Out-of-scope reminders** (do NOT re-import from retired plan):

- Backlog-yaml mutations → consolidated under TECH-XXX-B below.
- Sonnet skill extractions (TECH-302) → already shipped via parallel-carcass.
- Bridge transport rewrite → not in salvage scope; transport stable.
- Web dashboard tooling → out of `territory-ia` MCP surface.
- Computational-family batching → covered by per-tool batches; deferred indefinitely.

---

## 3. Extension B — `backlog-yaml-mcp-alignment` salvage

Per-item table sourced from deep-dive §3 + cross-checked against `tools/scripts/validate-backlog-yaml.mjs`, `tools/scripts/backfill-parent-plan-locator.mjs`, `tools/mcp-ia-server/src/tools/`.

| Source stage | Verdict | What survives | Why salvage | Proposed home |
|---|---|---|---|---|
| Stage 4.2 — Reverse-lookup wiring (TECH-438 / TECH-439 / TECH-440 / TECH-441) | PARTIAL | `backlog_list` locator filter inputs (`parent_plan` / `stage` / `task_key`) + tests + `docs/mcp-ia-server.md` doc updates + `CLAUDE.md` §2 MCP-first reorder | TECH-438/439/440/441 still pending in plan; `backlog_list` already ships but lacks locator filters. Mechanical extension. Probe each TECH yaml for actual scope before refiling — if redundant with materialize-backlog flock (Stage 4 ship), archive rows. | TECH-XXX-B §Phase 1 |
| Stage 5 — Validator extensions (`related` / `depends_on_raw`) | UNIQUE | Cross-record check that `related` ids exist; `depends_on_raw` non-empty when `depends_on: []` non-empty; warn on drift when `depends_on_raw` mentions ids absent from `depends_on: []`; fixture-runner harness | Validator extensions live in `validate-backlog-yaml.mjs` only (not MCP); ~half-day script edit. Without it, link rot in `related` / `depends_on_raw` slips through. | TECH-XXX-B §Phase 2 |
| Stage 6 — `backlog_record_create` MCP tool | UNIQUE | Atomic `reserve_backlog_ids → validate → write → materialize` flow as single MCP tool; `backlog_search` filter extensions (`priority` / `type` / `created_after` / `created_before`) | `/file-issue` skill + `project-new` skill currently shell out to `reserve-id.sh` + raw yaml write + `materialize-backlog.sh`. Single MCP tool collapses 3-step to 1 envelope + lets remote callers (web dashboard) author backlog records without shell access. | TECH-XXX-B §Phase 3 |
| Stages 12 / 13 / 14 — Skill-body wiring | PARTIAL | `/file-issue` cuts over to `backlog_record_create`; `/audit` cuts over to `backlog_search`; lifecycle skills (`plan-author`, `project-spec-implement`, `/ship` dispatcher, `release-rollout-enumerate`) consume `surfaces` / `mcp_slices` / `skill_hints` from yaml + `master_plan_next_pending` for next-task lookup; append-only `surfaces` guardrail in `plan-author` | MCP read tools exist; some skill bodies cut over (`/stage-file`, `/ship-stage`); 100% audit pending. Mechanical edits per skill body — consolidate into one audit pass. | TECH-XXX-B §Phase 4 |
| Stage 15 — Validator strict-flip | UNIQUE | Flip `validate-parent-plan-locator.mjs` default mode advisory → strict; keep `--advisory` opt-out; chain into `validate:all`; fixture covers strict-default-fail-on-drift + advisory-opt-out-still-green | Policy decision gated on zero-drift-for-≥1-week-in-production. Drift currently tolerated; no CI gate against locator-field rot. | TECH-XXX-B §Phase 5 (gated) |
| Stage 16 — Archive backfill `--archive` flag | UNIQUE | `--archive` flag on `backfill-parent-plan-locator.mjs` (currently no-op stub per script line 56 warning); proper `--skip-unresolvable` for plan-missing + task_key-missing edges; per-reason skip-count logging; `--dry-run` for archive mode | Script stub already accepts the flag with a `[WARN]` no-op. One-shot 1–2 hour script edit; archive coverage of locator fields currently 0%. | TECH-XXX-B §Phase 6 |

**Locked decisions carried forward** (from Plan B preamble — preserve in TECH-XXX-B):

- yaml schema v2 stays as canonical (locator fields locked: `parent_plan`, `task_key`, `step`, `stage`, `phase`, `router_domain`, `surfaces`, `mcp_slices`, `skill_hints`).
- `parent_plan_validate` advisory by default through grace window; strict-flip gated on Stage 15 acceptance.
- `surfaces` field append-only guardrail (never reorder / rewrite / drop) — enforced in `plan-author` body.
- Single-issue outside-plan path → `parent_plan` + `task_key` both empty; validator advisory ignores.

---

## 4. TECH-XXX-A — MCP Lifecycle Authoring + Journal + Batch (placeholder spec stub)

> **Placeholder id:** `TECH-XXX-A` — reserve real id via `tools/scripts/reserve-id.sh TECH 1` when filing.
> **Status:** Draft (pre-file).
> **Created:** 2026-05-01.
> **Last updated:** 2026-05-01.
> **Source:** Plan A salvage (Stages 6, 11, 12, 13, 15, 16).

### 4.1 Summary

Consolidate 6 salvaged surfaces from retired Plan A into one TECH issue. Surface set: envelope-shape CI gate, IA authorship tools (glossary / spec / rule writes), Unity bridge pipeline + jobs-list, journal idempotent sync with SHA-256 dedup, generic transactional batch wrapper (deferred sub-phase), `dry_run` flag on every mutation + authorship tool. All inside `territory-ia` MCP server; no new transport.

### 4.2 Goals + Non-Goals

**Goals:**

1. Close envelope-shape contract gap — CI fails on bare non-envelope returns in `src/tools/*.ts`.
2. Ship 4 IA-authorship tools w/ `caller_agent` gate + duplicate-term + cross-ref + index-regen flow.
3. Ship `unity_bridge_pipeline` hybrid sync/async + `unity_bridge_jobs_list` query.
4. Ship `journal_entry_sync` w/ SHA-256 dedup + 3-step `content_hash` migration + cascade-delete on archive.
5. Ship `dry_run?: boolean` on every mutation + authorship tool — `payload.diff` + `affected_paths` + `would_write: true` w/o write.
6. Ship `mutation_batch` (deferred sub-phase) — `all_or_nothing` / `best_effort` modes + in-memory snapshot rollback + flock coordination.

**Non-Goals:**

1. New transport layer (bridge stays as-is).
2. Web dashboard surface (out of `territory-ia` MCP scope).
3. Sonnet skill extractions (covered by parallel-carcass).
4. IA-authorship server split (rejected — stays in `territory-ia`, guarded by `caller_agent`).
5. Computational-family batching beyond per-tool surfaces.

### 4.3 Acceptance Criteria

- [ ] `npm run validate:mcp-envelope-shape` exits 0; chained into `validate:all`.
- [ ] `glossary_row_create({ caller_agent: "plan-author", row })` appends to correct category bucket; duplicate term (case-insensitive) → `invalid_input`; non-existent `spec_reference` → `invalid_input` w/ nearest-spec hint.
- [ ] `glossary_row_update`, `spec_section_append`, `rule_create` ship w/ heading-uniqueness + filename-uniqueness + `unauthorized_caller` paths green.
- [ ] `unity_bridge_pipeline([k1, k2, k3])` ≤30s → sync envelope `{ results, lease_released: true, elapsed_ms }`; >30s → async `{ job_id, status: "running", poll_with: "unity_bridge_jobs_list" }`; timeout on kind 2 of 3 → `{ ok: false, error.code: "timeout", details.completed_kinds }`.
- [ ] `unity_bridge_jobs_list` queries `agent_bridge_job` table; `db_unconfigured` → graceful envelope error.
- [ ] `journal_entry_sync(issue_id, mode: "upsert", body)` called twice w/ same body → one DB row (SHA-256 dedup); `mode: "delete", cascade: true` removes all rows for issue.
- [ ] Migration `add-journal-content-hash.ts` idempotent on re-run.
- [ ] `project_spec_closeout_digest` payload includes `journaled_sections: string[]`.
- [ ] All mutation + authorship tools accept `dry_run?: boolean` (default `false`); response includes `payload.diff` + `affected_paths`; no file write, no index regen on dry-run path.
- [ ] `mutation_batch({ ops, mode })` rollback on `all_or_nothing`, partial-result on `best_effort`, flock on `tools/.mutation-batch.lock`. Deferred — gate on 3rd batch surface emergence.
- [ ] Snapshot tests for diff fixtures per mutation tool.

### 4.4 Implementation Plan

**Phase 1 — Envelope CI gate (Stage 6 salvage)**

- [ ] Author `tools/scripts/validate/validate-mcp-envelope-shape.mjs` — scan `src/tools/*.ts` for bare non-envelope returns.
- [ ] Wire into `package.json` `validate:all` chain.
- [ ] Snapshot fixtures for all 32+ tools.

**Phase 2 — IA authorship tools (Stage 11 salvage)**

- [ ] Implement `glossary_row_create` + `glossary_row_update` w/ category-bucket append + duplicate-term gate.
- [ ] Implement `spec_section_append` w/ heading-uniqueness check via `spec_outline`.
- [ ] Implement `rule_create` w/ filename-uniqueness check.
- [ ] All four trigger non-blocking `npm run build:glossary-index` / `generate:ia-indexes` regen.
- [ ] Wire into `caller-allowlist.ts`; `unauthorized_caller` rejection path.
- [ ] Tests green for all 4 tools.

**Phase 3 — Unity bridge pipeline (Stage 12 salvage)**

- [ ] Implement `unity_bridge_pipeline` w/ `UNITY_BRIDGE_PIPELINE_CEILING_MS` env var (default 30 000).
- [ ] Implement `unity_bridge_jobs_list` querying `agent_bridge_job` table.
- [ ] Wire timeout auto-attach in existing `unity_bridge_command`.
- [ ] Tests cover sync ≤ ceiling + async > ceiling + timeout-on-kind-N + `db_unconfigured` paths.

**Phase 4 — Journal lifecycle (Stage 13 salvage)**

- [ ] Author Postgres migration `add-journal-content-hash.ts` — 3-step (nullable column → batched SHA-256 backfill → NOT NULL); idempotent on re-run.
- [ ] Implement `journal_entry_sync` upsert + `cascade: true` delete.
- [ ] Extend `project_spec_closeout_digest` payload w/ `journaled_sections: string[]`.
- [ ] Update `closeout` skill body — call `journal_entry_sync` instead of `project_spec_journal_persist`.
- [ ] Tests for dedup + cascade-delete + migration idempotency.

**Phase 5 — Generic transactional batch (Stage 15 salvage — deferred)**

- [ ] Snapshot helper + batch infrastructure.
- [ ] `mutation_batch({ ops, mode: "all_or_nothing" | "best_effort" })`.
- [ ] flock on `tools/.mutation-batch.lock`.
- [ ] Caller adoption — `stage-file` per-stage file-batch; `closeout` yaml-archive-move + BACKLOG regen + spec-delete sequence.
- [ ] Tests + caller adoption.
- [ ] **Gate:** activate phase only on 3rd batch surface emergence (per deep-dive §3 deferral).

**Phase 6 — Dry-run preview (Stage 16 salvage)**

- [ ] Add `dry_run?: boolean` to every mutation + authorship tool from Phases 2 + 4 + 5 (default `false`).
- [ ] Dry-run response: `{ ok: true, payload: { diff: string, affected_paths: string[], would_write: true } }`.
- [ ] `mutation_batch({ dry_run: true })` propagates to nested ops; aggregates into `payload.diffs`.
- [ ] Snapshot fixtures for diff output per tool.
- [ ] Caller adoption — `/closeout`, `/release-rollout`, `/stage-file` preview before commit.

### 4.5 Test Blueprint

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| Envelope CI gate green on clean tree | Node | `npm run validate:mcp-envelope-shape` | Chained into `validate:all` |
| IA-authorship tool happy + reject paths | Node | `npm test --workspace tools/mcp-ia-server` | Per-tool fixtures under `tools/mcp-ia-server/tests/tools/` |
| Bridge pipeline sync + async + timeout paths | Node + Postgres | `npm test --workspace tools/mcp-ia-server` + `agent_bridge_job` table | Requires `db:bridge-preflight` green |
| Journal dedup + cascade + migration idempotency | Postgres | Migration replay + `journal_entry_sync` integration test | 3-step migration covered |
| `dry_run` returns diff + no write | Node | Snapshot fixtures per mutation tool | `validate-mcp-envelope-shape` covers shape |
| `mutation_batch` rollback + flock | Node | Race fixture + flock contention test | Gate phase activation |

### 4.6 Invariants Touched

- Invariant #12 — IA path-validation guard. All mutation tools validate target file path before write; reject outside `ia/specs/` / `ia/rules/` / `ia/projects/`.
- Invariant #13 — id-counter monotonicity. Mutation tools never touch `id:` field; never regenerate `ia/state/id-counter.json`.
- Hook denylist — no change.

---

## 5. TECH-XXX-B — Backlog YAML Validator + Record-Create + Strict-Flip + Archive (placeholder spec stub)

> **Placeholder id:** `TECH-XXX-B` — reserve real id via `tools/scripts/reserve-id.sh TECH 1` when filing.
> **Status:** Draft (pre-file).
> **Created:** 2026-05-01.
> **Last updated:** 2026-05-01.
> **Source:** Plan B salvage (Stages 4.2, 5, 6, 12, 13, 14, 15, 16).

### 5.1 Summary

Consolidate 6 phases of salvaged surfaces from retired Plan B. Surface set: `backlog_list` locator filter inputs (`parent_plan` / `stage` / `task_key`); validator extensions for `related` + `depends_on_raw`; `backlog_record_create` MCP tool + `backlog_search` filter extensions; skill-body MCP-first wiring audit (`/file-issue`, `/audit`, `plan-author`, `project-spec-implement`, `/ship`, `release-rollout-enumerate`); validator strict-flip after grace window; archive backfill `--archive` flag.

### 5.2 Goals + Non-Goals

**Goals:**

1. Extend `backlog_list` w/ locator filters; cover TECH-438/439/440/441 scope or archive redundant rows.
2. Extend `validate-backlog-yaml.mjs` w/ cross-record `related` existence + `depends_on_raw` non-empty + drift-warning checks.
3. Ship `backlog_record_create` MCP tool (atomic reserve → validate → write → materialize) + `backlog_search` priority/type/date filters.
4. Cut over `/file-issue`, `/audit`, lifecycle skills to MCP-first reads (`backlog_record_create`, `backlog_search`, `master_plan_next_pending`).
5. Flip `validate-parent-plan-locator.mjs` default advisory → strict; chain into `validate:all`.
6. Wire `--archive` flag on `backfill-parent-plan-locator.mjs` (currently no-op stub per line 56 `[WARN]`).

**Non-Goals:**

1. yaml schema v3 (v2 locked).
2. Backlog rendering changes (`BACKLOG.md` shape stable).
3. New `BACKLOG-` view (open/archive view set frozen).
4. Web-side backlog write surface (gated separately).

### 5.3 Acceptance Criteria

- [ ] `backlog_list({ parent_plan, stage?, task_key? })` filters honored; tests green; `docs/mcp-ia-server.md` documents the filter set.
- [ ] `CLAUDE.md` §2 MCP-first ordering reflects reverse-lookup tools.
- [ ] `validate-backlog-yaml.mjs` implements 3 new cross-record checks; fixture-runner harness covers passing + failing fixtures.
- [ ] `backlog_record_create` ships — atomic `reserve_backlog_ids(count: 1) → validate → tmp-file-then-rename → materialize-backlog.sh (flock-guarded)`; race test (parallel creates) green.
- [ ] `backlog_search` accepts `priority` / `type` / `created_after` / `created_before`; filters applied before scoring.
- [ ] `/file-issue` skill body cuts over to `backlog_record_create`.
- [ ] `/audit` + triage skill bodies cut over to `backlog_search`.
- [ ] `plan-author` body reads `surfaces` / `mcp_slices` / `skill_hints` first; append-only `surfaces` guardrail enforced via validator warning.
- [ ] `project-spec-implement` body consumes `skill_hints` (advisory).
- [ ] `/ship` dispatcher next-task-lookup calls `master_plan_next_pending` first; plan-scan fallback retained.
- [ ] `release-rollout-enumerate` per-row data pull reads `backlog_list parent_plan=`; inference fallback retained.
- [ ] `validate-parent-plan-locator.mjs` default exit code = 1 on drift; `--advisory` flag retained + documented; chained into `validate:all`; gate on zero-drift-for-≥1-week-in-production.
- [ ] `backfill-parent-plan-locator.mjs --archive` scans `ia/backlog-archive/*.yaml` w/ proper `--skip-unresolvable` + per-reason skip-count logging + `--dry-run` support.
- [ ] Rehearsal fixture proves one full `/project-new → /author → /implement → /closeout` cycle on schema-v2 yaml w/ MCP happy path + zero scan fallbacks triggered.

### 5.4 Implementation Plan

**Phase 1 — `backlog_list` locator filters (Stage 4.2 salvage)**

- [ ] Probe TECH-438/439/440/441 yaml — verify scope vs current `backlog-list.ts`.
- [ ] Extend `backlog_list` inputs w/ `parent_plan` / `stage` / `task_key` filters.
- [ ] Tests under `tools/mcp-ia-server/tests/tools/`.
- [ ] Document in `docs/mcp-ia-server.md`.
- [ ] Update `CLAUDE.md` §2 MCP-first ordering.

**Phase 2 — Validator extensions (Stage 5 salvage)**

- [ ] Cross-record check: `related` ids exist.
- [ ] Cross-record check: `depends_on_raw` non-empty when `depends_on: []` non-empty.
- [ ] Warn on drift when `depends_on_raw` mentions ids absent from `depends_on: []`.
- [ ] Fixture set under `tools/scripts/test-fixtures/` — passing + failing pair per check + expected error text.
- [ ] Fixture-runner test harness.

**Phase 3 — `backlog_record_create` + `backlog_search` filters (Stage 6 salvage)**

- [ ] Author `tools/mcp-ia-server/src/tools/backlog-record-create.ts` — input `{ prefix, fields }`, output `{ id, yaml_path }`.
- [ ] Flow: `reserve_backlog_ids(count: 1)` → build yaml body → `validateBacklogRecord` → tmp-file-then-rename → spawn `materialize-backlog.sh` (flock-guarded).
- [ ] Extend `backlog-search.ts` w/ `priority` / `type` / `created_after` / `created_before` filters; applied before scoring.
- [ ] Tests — happy path + validation-failure + race (two parallel creates, distinct ids, both yaml files on disk, materialize ran) + filter combinations.

**Phase 4 — Skill-body wiring (Stages 12 / 13 / 14 salvage)**

- [ ] `/file-issue` body cutover to `backlog_record_create`.
- [ ] `/audit` body cutover to `backlog_search`.
- [ ] `plan-author` body — read `surfaces` / `mcp_slices` / `skill_hints` first; append-only `surfaces` guardrail (never reorder / rewrite / drop); validator warning on diff vs plan's Relevant-surfaces block.
- [ ] `project-spec-implement` body — consume `skill_hints` as advisory routing hint.
- [ ] `/ship` dispatcher (`.claude/commands/ship.md` or equivalent) — next-task-lookup calls `master_plan_next_pending {plan, stage?}` first; plan-scan fallback retained.
- [ ] `release-rollout-enumerate` body — per-row data pull via `backlog_list parent_plan=`; inference fallback retained.
- [ ] Rehearsal fixture: one full `/project-new → /author → /implement → /closeout` cycle on schema-v2 yaml, MCP happy path, zero scan fallbacks.
- [ ] Run `npm run validate:skill-drift` after each skill body edit (catches drift between SKILL.md + generated `.claude/agents/` + `.claude/commands/`).

**Phase 5 — Validator strict-flip (Stage 15 salvage — gated)**

- [ ] Flip `validate-parent-plan-locator.mjs` default exit code 0 → 1 on drift.
- [ ] Retain `--advisory` flag (flips exit code back to 0); documented.
- [ ] `package.json` `validate:all` chains validator in strict mode.
- [ ] `docs/agent-led-verification-policy.md` entry documents flip + opt-out.
- [ ] Fixture covers strict-default-fail-on-drift + advisory-opt-out-still-green.
- [ ] **Gate:** activate phase only after zero-drift-for-≥1-week-in-production observed in `runtime_state` snapshots.

**Phase 6 — Archive backfill `--archive` flag (Stage 16 salvage)**

- [ ] Replace `[WARN] --archive flag accepted but archive scan not yet supported` (currently `tools/scripts/backfill-parent-plan-locator.mjs:56`) w/ functional archive scan over `ia/backlog-archive/*.yaml`.
- [ ] `--skip-unresolvable` handles both edges: (a) plan path missing from disk (archived + plan later deleted); (b) task_key suffix absent from title (archive-only + pre-locator vintage). Each skip reason logged separately.
- [ ] `--dry-run` for archive mode — preview skip reasons.
- [ ] Archive pass runs clean on current `ia/backlog-archive/*.yaml`; log resolved / skipped-plan-missing / skipped-task-key-missing counts.
- [ ] Doc append to `docs/parent-plan-locator-fields-exploration.md` — archive backfill is one-shot; no re-run unless plans move.

### 5.5 Test Blueprint

| Acceptance / goal | Check type | Command or artifact | Notes |
|---|---|---|---|
| `backlog_list` locator filters | Node | `npm test --workspace tools/mcp-ia-server` | Per-filter fixture |
| Validator cross-record checks | Node | `npm run validate:backlog-yaml` + fixture-runner | `tools/scripts/test-fixtures/` |
| `backlog_record_create` race-safe | Node + flock | Parallel-create race fixture | `materialize-backlog.sh` flock-guarded |
| `backlog_search` filter combos | Node | `npm test --workspace tools/mcp-ia-server` | priority + type + date matrix |
| Skill body MCP-first cutover | Node | `npm run validate:skill-drift` + rehearsal fixture | Catches generated-surface drift |
| Strict-flip default exit 1 | Node | Validator fixture: drift → exit 1; `--advisory` → exit 0 | `validate:all` chain |
| Archive backfill `--archive` | Node | `tools/scripts/backfill-parent-plan-locator.mjs --archive --dry-run` | Per-reason log counts |
| Full lifecycle rehearsal | Node + skill | `/project-new → /author → /implement → /closeout` on v2 yaml | Zero scan fallbacks |

### 5.6 Invariants Touched

- Invariant #13 — id-counter monotonicity. `backlog_record_create` calls `reserve_backlog_ids` (the canonical path) — never hand-edits counter.
- Guardrail — flock on mutation paths. `materialize-backlog.sh` flock retained inside `backlog_record_create` flow.
- Caveman authoring rule — skill bodies edited in Phase 4 stay caveman per `ia/rules/agent-output-caveman.md`.

---

## 6. Retirement plan

### 6.1 Order of operations

1. **Land this doc** — `docs/mcp-lifecycle-plans-post-mvp-extensions.md` (this file). Captures all 8 salvaged items + 2 placeholder spec stubs. Read-only against IA — no `ia/backlog/` / `ia/projects/` / `ia/master-plans/` mutations yet.
2. **File 2 TECH issues** — `tools/scripts/reserve-id.sh TECH 2` to grab two real ids; replace `TECH-XXX-A` / `TECH-XXX-B` placeholders in this doc + author `ia/projects/{TECH-XXX-A}-mcp-lifecycle-authoring-journal-batch.md` + `ia/projects/{TECH-XXX-B}-backlog-yaml-validator-record-create.md` from the §4 / §5 stubs above; `materialize-backlog.sh` to land BACKLOG rows.
3. **Flip both plans retired** — `master_plan_state` mutation: `mcp-lifecycle-tools-opus-4-7-audit` + `backlog-yaml-mcp-alignment` both → `Status: Final (retired)`. Move plan markdown under `ia/projects/_retired/` or DB-equivalent flip per current archive policy.

### 6.2 Validation gates after retirement

Run after step 3 lands:

- `npm run validate:all` — full IA / MCP / fixture / index / rules chain. Confirms no orphan refs to retired plan slugs.
- `npm run validate:master-plan-status` — confirms drift-audit CRITICAL bucket clears.
- `npm run validate:claude-imports` — confirms `CLAUDE.md` `@`-imports still resolve.
- Optional: `npm run validate:skill-drift` if any skill body referenced retired plan tasks.

### 6.3 3-bullet retirement summary

- Retire `mcp-lifecycle-tools-opus-4-7-audit` + `backlog-yaml-mcp-alignment` after this doc + 2 TECH issues land. Net surface lost = 0 (all SUPERSEDED items shipped via `db-lifecycle-extensions` / `parallel-carcass-rollout` / `architecture-coherence-system` / `recipe-runner-phase-e` / `asset-pipeline`); net surface deferred = 8 items captured in §2 + §3.
- Replace 35 stalled stages w/ 2 consolidated TECH specs (TECH-XXX-A: 6 phases, ~20 tasks; TECH-XXX-B: 6 phases, ~22 tasks). Each phase carries source-stage citation for evidence trail back to retired plan exit criteria.
- Gate Phases A.5 (generic `mutation_batch`) + B.5 (validator strict-flip) on demand triggers — 3rd batch surface emergence + zero-drift-for-≥1-week production observation respectively. Other phases ship on normal cadence once TECH issues filed.

---

## 7. Provenance

| Doc | Role |
|---|---|
| `docs/master-plan-drift-audit-2026-05-01.md` | CRITICAL → DROP verdict source |
| `docs/mcp-lifecycle-plans-supersession-deep-dive-2026-05-01.md` | Per-stage SUPERSEDED / PARTIAL / UNIQUE verdicts |
| `docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md` | Plan A original §Design Expansion (locked decisions) |
| `docs/session-token-latency-audit-exploration.md` | Plan A Stage 17 source (already shipped — no salvage) |
| Plan B exploration (DB-only — render via `master_plan_render backlog-yaml-mcp-alignment`) | Plan B preamble + locked decisions |

## 8. Open Questions

None — tooling only; salvage menu is dense + each item maps 1:1 to a retired stage. Resolve via TECH-XXX-A + TECH-XXX-B spec authoring once placeholder ids reserved.
