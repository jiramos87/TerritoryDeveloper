# Backlog YAML ↔ MCP alignment — Master Plan

> **Last updated:** 2026-04-18
>
> **Status:** In Progress — Step 4 / Stage 4.2
>
> **Scope:** Align MCP territory-ia tool surface + `ParsedBacklogIssue` type + validator + skill docs with the per-issue yaml backlog refactor. Nine Implementation Points (HIGH band IP1–IP5, MEDIUM/LOW band IP6–IP9) plus one correctness fix (soft-dep marker preservation, folded into IP1). Steps 3–6 (parent-plan + step/stage locator fields + MCP reverse-lookup tooling + skill patches + late-hardening / archive backfill) appended 2026-04-18 via `/master-plan-extend`. Pure tooling / MCP / validator / skill-docs work — zero Unity runtime touches, zero save-schema touches.
>
> **Exploration source:** `docs/backlog-yaml-mcp-alignment-exploration.md` (§Problem, §Design Expansion block, §Deferred decomposition hints) for Steps 1–2. `docs/parent-plan-locator-fields-exploration.md` (§Design Expansion, Phase 6 Implementation Points) for Steps 3–6. Blocks are ground truth.
>
> **Locked decisions (do not reopen in this plan):**
> - Per-issue yaml layout (`ia/backlog/{id}.yaml`, `ia/backlog-archive/{id}.yaml`) + section manifests (`ia/state/backlog-sections.json`, `ia/state/backlog-archive-sections.json`) stay byte-compatible.
> - Monotonic id source stays `ia/state/id-counter.json` via `tools/scripts/reserve-id.sh` under flock (invariant #13).
> - Materialize stays deterministic — `BACKLOG.md` + `BACKLOG-ARCHIVE.md` are generated views, never hand-edited.
> - Minimal yaml parser in `backlog-yaml-loader.ts` stays — no migration to a real yaml lib in this plan.
> - `proposed_solution` field fate decided by Grep gate (zero consumers → drop; ≥1 consumer → add to yaml schema). Decision captured in IP2 Stage.
> - Approach B selected for locator fields (Steps 3–6): full yaml schema v2 extension (`parent_plan` + `task_key` required; `step` / `stage` / `phase` / `router_domain` / `surfaces` / `mcp_slices` / `skill_hints` optional) + MCP reverse-lookup tools (`master_plan_locate`, `master_plan_next_pending`, `parent_plan_validate`) + dual-mode validator (advisory default + `--strict` flip). Source: `docs/parent-plan-locator-fields-exploration.md` §Recommendation + §Phase 2.
> - Spec-frontmatter mirror = 2 fields only (`parent_plan` + `task_key`); step/stage/phase derivable from `task_key` parser. Lazy — populated on next `/kickoff`, never retroactive rewrite.
> - `surfaces` auto-populated by `stage-file` from plan task-row "Relevant surfaces"; `spec-kickoff` append-only in §4 / §5.2 regions (never reorder / rewrite / drop).
> - `skill_hints` advisory hint only — `stage-file` / `project-new` write; kickoff / implement read as routing suggestion, not mandate.
> - Migration scope hybrid — open-yaml one-shot backfill in Step 3; archive deferred with `--skip-unresolvable` in Step 6; plans zero backfill.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task; ≥2 tasks per phase). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, **never closeable** via `/closeout`).
>
> **Sibling orchestrators in flight:**
> - `ia/projects/multi-scale-master-plan.md` — Unity runtime C# + save schema. Disjoint surface (no `tools/mcp-ia-server/**` touches). No collision.
> - `ia/projects/web-platform-master-plan.md` — Next.js at `web/`. Disjoint surface. No collision.
> - `ia/projects/blip-master-plan.md` — runtime C# audio. Disjoint. No collision.
> - **Parallel-work rule:** do NOT run `/stage-file` or `/closeout` against two sibling orchestrators concurrently on the same branch — MCP index regens (`npm run mcp:regen-index`) must sequence.
>
> **Read first if landing cold:**
> - `docs/backlog-yaml-mcp-alignment-exploration.md` — full problem analysis + 9 Implementation Points (Steps 1–2).
> - `docs/parent-plan-locator-fields-exploration.md` — Approach B + Phase 6 Implementation Points (Steps 3–6).
> - `tools/mcp-ia-server/src/parser/backlog-parser.ts` + `backlog-yaml-loader.ts` — current parser surface.
> - `tools/mcp-ia-server/src/parser/types.ts` (or equivalent) — `ParsedBacklogIssue` shape.
> - `tools/scripts/reserve-id.sh` + `tools/scripts/materialize-backlog.sh` + `tools/scripts/materialize-backlog.mjs` — ID + materialize flow.
> - `tools/validate-backlog-yaml.mjs` — current validator scope.
> - `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` + Stage-scoped `/closeout` pair (`ia/skills/stage-closeout-plan/SKILL.md` → `ia/skills/plan-applier/SKILL.md` Mode `stage-closeout`) — skills that write/mutate yaml.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — phase/task cardinality + permanent-orchestrator rule.
>
> **Invariants implicated:**
> - #12 (`ia/projects/` for issue-specific specs — applies to every `_pending_` row filed under this orchestrator).
> - #13 (monotonic id source = `reserve-id.sh` — IP3 MCP wrapper calls the script, never hand-edits the counter).
> - Invariants #1–#11 NOT implicated — no Unity runtime / no `GridManager` / no road / no HeightMap touches.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final`. Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft`; `/author` (`plan-author`) → `In Review`; `/implement` → `In Progress`; the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode `stage-closeout`) → task rows `Done (archived)` + stage `Final` + stage-level rollup.

---

### Stage 1 — HIGH band (IP1–IP5) / Types + yaml loader (IP1 + IP2)

**Status:** Final

**Backlog state (Stage 1.1):** 7 filed

**Objectives:** Land the `ParsedBacklogIssue` shape extension + yaml-loader field mapping. Fix the soft-dep marker preservation bug (correctness). Resolve the `proposed_solution` fate via a Grep audit and execute the resulting path (drop from the type OR add to yaml schema + loader).

**Exit:**

- `priority`, `related`, `created` present on `ParsedBacklogIssue`; loader maps from yaml; markdown fallback sets sane defaults (`null` / `[]`).
- `depends_on_raw` fallback in loader prefers the yaml source string; only synthesizes from array when source was empty. Soft markers (e.g. `FEAT-12 (soft)`) preserved across round-trip.
- `proposed_solution` Grep audit complete — zero consumers → removed from the type + every read call-site; ≥1 consumer → added to yaml schema via `buildYaml` + loader + validator.
- Tests extended under `tools/mcp-ia-server/tests/**` with fixtures covering: all three new fields present / absent, soft-dep marker preservation, `proposed_solution` presence/absence per decision.
- `npm run validate:all` green.
- Phase 1 — Type + loader extension (three new fields + soft-dep fallback fix).
- Phase 2 — `proposed_solution` decision + execution.
- Phase 3 — Test coverage + downstream payload surfacing.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Extend `ParsedBacklogIssue` shape | **TECH-295** | Done (archived) | Add `priority: string \ | null`, `related: string[]`, `created: string \ | null` to `ParsedBacklogIssue` in `tools/mcp-ia-server/src/parser/backlog-parser.ts` (or the extracted types module if one exists). Update any dependent type exports. No behavior change yet — loader mapping lands in T1.1.2. |
| T1.2 | Map new fields in yaml loader | **TECH-296** | Done (archived) | In `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `yamlToIssue` sets `priority`, `related`, `created` from yaml record. Markdown-path callers (legacy parser) default to `null` / `[]` when yaml absent. Cover both existing fixtures + at least one new fixture with all three fields. |
| T1.3 | Fix `depends_on_raw` soft-marker fallback | **TECH-297** | Done (archived) | In `backlog-yaml-loader.ts`, replace fallback `depends_on_raw = array.join(", ")` with: prefer yaml source string when non-empty; only synthesize from array when raw is absent. Add fixture with `depends_on: ["FEAT-12"]` + `depends_on_raw: "FEAT-12 (soft)"` and assert `resolveDependsOnStatus` sees the `(soft)` marker. |
| T1.1 | Grep-audit `proposed_solution` consumers | **TECH-298** | Done (archived) | Run `Grep` across repo for reads of `.proposed_solution` / `proposed_solution:` / `"proposed_solution"`. Record the full consumer list in a scratch note on the issue. Decision rule: zero consumers → choose Option A (drop); ≥1 consumer → choose Option B (add to yaml). Record decision + rationale in the spec's §1 of the filed project spec. |
| T1.2 | Execute `proposed_solution` decision | **TECH-299** | Done (archived) | Execute per T1.2.1 decision. **Option A:** remove `proposed_solution` from `ParsedBacklogIssue` + loader + parser + all reads; no yaml schema change. **Option B:** add `proposed_solution?: string` to yaml schema via `buildYaml` in `tools/scripts/migrate-backlog-to-yaml.mjs`, emit in loader, extend validator schema, update at least one fixture. Either option lands tests for the chosen behavior. |
| T1.1 | Surface new fields in `backlog_issue` + `backlog_search` payloads | **TECH-300** | Done (archived) | In `tools/mcp-ia-server/src/tools/backlog-issue.ts` + `backlog-search.ts`, return the three new fields (`priority`, `related`, `created`) in the MCP response payload. No new filters here (IP9 adds them). Snapshot-update existing tests. |
| T1.2 | Round-trip soft-dep marker integration test | **TECH-301** | Done (archived) | Integration test under `tools/mcp-ia-server/tests/tools/` — load a yaml fixture with `depends_on_raw: "FEAT-12 (soft)"`, call `parseBacklogIssue` + `resolveDependsOnStatus`, assert `soft_only: true`. Plain-id counter-fixture asserts `soft_only: false`. `[optional]` deferred (parser has no classifier — see TECH-301 §OpenQ1). Prevents regression of the loader bug. |

### Stage 2 — HIGH band (IP1–IP5) / MCP tools batch 1 (IP3 + IP4 + IP5)

**Status:** Final

**Backlog state (Stage 1.2):** 7 filed

**Objectives:** Ship the three new MCP tools: `reserve_backlog_ids` wrapping `reserve-id.sh`, `backlog_list` for structured filter queries, `backlog_record_validate` for pre-write lint. Extract the shared lint core (`backlog-record-schema.ts`) so `validate-backlog-yaml.mjs` and `backlog_record_validate` share logic.

**Exit:**

- `tools/mcp-ia-server/src/tools/reserve-backlog-ids.ts` — spawns `reserve-id.sh {PREFIX} {N}`, returns `{ ids: string[] }`.
- `tools/mcp-ia-server/src/tools/backlog-list.ts` — filters by `section` / `priority` / `type` / `status` / `scope`, returns ordered `{ issues, total }`.
- `tools/mcp-ia-server/src/tools/backlog-record-validate.ts` — input `{ yaml_body: string }`, output `{ ok, errors, warnings }`.
- `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` — shared schema / lint core consumed by validator script + MCP tool.
- Tools registered in `tools/mcp-ia-server/src/index.ts` tool registry.
- Tests under `tools/mcp-ia-server/tests/tools/` cover: reserve concurrency (N parallel calls → zero dup ids), list filter combinations + empty result + scope switch, validator good + bad records (required fields, id format, status enum, soft-dep consistency).
- Tool descriptors match `mcp__territory-ia__*` naming convention.
- Phase 1 — Shared lint core extraction + `backlog_record_validate`.
- Phase 2 — `reserve_backlog_ids` tool + concurrency test.
- Phase 3 — `backlog_list` tool + filter combinations.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Extract shared lint core | **TECH-323** | Done (archived) | Create `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` exporting `validateBacklogRecord(yamlBody: string): { ok, errors, warnings }`. Move schema checks (required fields, id format, status enum, `depends_on_raw` non-empty when `depends_on: []` non-empty) out of `tools/validate-backlog-yaml.mjs`. Update the validator script to import + call the shared core. |
| T2.2 | Implement `backlog_record_validate` MCP tool | **TECH-324** | Done (archived) | `tools/mcp-ia-server/src/tools/backlog-record-validate.ts` — input schema `{ yaml_body: string }`, output `{ ok, errors, warnings }`. Delegate to shared core from T1.2.1. Register in `tools/mcp-ia-server/src/index.ts`. |
| T2.3 | Test `backlog_record_validate` against fixtures | **TECH-325** | Done (archived) | Add tests under `tools/mcp-ia-server/tests/tools/backlog-record-validate.test.ts` — good record passes; each bad-record fixture (missing required field, bad id format, invalid status, empty `depends_on_raw` with non-empty `depends_on`) returns the expected error. |
| T2.4 | Implement `reserve_backlog_ids` MCP tool | **TECH-326** | Done (archived) | `tools/mcp-ia-server/src/tools/reserve-backlog-ids.ts` — input `{ prefix: "TECH"\ | "FEAT"\ | "BUG"\ | "ART"\ | "AUDIO", count: 1..50 }`, spawn `tools/scripts/reserve-id.sh {prefix} {count}` via `child_process`, parse stdout, return `{ ids: string[] }`. Register in `tools/mcp-ia-server/src/index.ts`. |
| T2.5 | Concurrency test for `reserve_backlog_ids` | **TECH-327** | Done (archived) | Add `tools/mcp-ia-server/tests/tools/reserve-backlog-ids.test.ts` — spawn 8 parallel invocations of the tool (counts 2 each), assert 16 unique ids returned + counter advanced correctly. Mirrors `tools/scripts/test/reserve-id-concurrent.sh` at the MCP layer. |
| T2.6 | Implement `backlog_list` MCP tool | **TECH-328** | Done (archived) | `tools/mcp-ia-server/src/tools/backlog-list.ts` — input `{ section?, priority?, type?, status?, scope? (default "open") }`, load via `parseAllBacklogIssues`, apply filters in-memory, return `{ issues, total }` ordered by id desc. Register in `tools/mcp-ia-server/src/index.ts`. |
| T2.7 | Test `backlog_list` filter combinations | **TECH-329** | Done (archived) | Add `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` — fixture set covering ≥2 sections, ≥2 priorities, ≥2 types, open + archive. Assert: scope switch, single-filter cases, multi-filter intersection, empty result, id desc ordering. |

### Stage 3 — HIGH band (IP1–IP5) / Skill wiring + docs

**Status:** Final

**Backlog state (Stage 1.3):** 4 filed

**Objectives:** Update the skill bodies that shell out to `reserve-id.sh` + manually construct yaml + manually invoke `materialize-backlog.sh` so they document the MCP-first call path (`reserve_backlog_ids`, `backlog_record_validate`). Keep the bash fallback so skills work even when MCP is unavailable. Update `docs/mcp-ia-server.md` tool catalog.

**Exit:**

- `ia/skills/stage-file/SKILL.md` — call-path step for batch id reservation names `reserve_backlog_ids` MCP tool first; bash fallback kept as alternative.
- `ia/skills/project-new/SKILL.md` — single-id reservation step names `reserve_backlog_ids (count: 1)` first; bash fallback kept.
- `ia/skills/project-spec-close/SKILL.md` — no call-path change (closeout does not reserve ids); add a note that `backlog_record_validate` may lint the archive-bound yaml before move.
- `docs/mcp-ia-server.md` — three new tools documented in the catalog (inputs, outputs, when to use).
- `CLAUDE.md` §2 MCP-first ordering — add the three new tools to the suggested order where relevant.
- Phase 1 — Skill body updates (`stage-file`, `project-new`, `project-spec-close`).
- Phase 2 — Tool catalog + CLAUDE ordering updates. (TECH-345, TECH-346 Done)

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Wire MCP tools into `stage-file` + `project-new` skills | **TECH-343** | Done (archived) | Edit `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` — reserve-id step names `reserve_backlog_ids` MCP tool first; `backlog_record_validate` step added after yaml body authoring + before disk write; bash fallbacks kept as "if MCP unavailable" alternative. Caveman prose. |
| T3.2 | Note `backlog_record_validate` use in close skill | **TECH-344** | Done (archived) | Edit `ia/skills/project-spec-close/SKILL.md` — add a single-line note that `backlog_record_validate` may lint the archive-destination yaml before the move (defensive; optional). No behavior change. |
| T3.3 | Document new tools in `docs/mcp-ia-server.md` | **TECH-345** | Done (archived) | Add three catalog entries in `docs/mcp-ia-server.md` for `reserve_backlog_ids`, `backlog_list`, `backlog_record_validate` — input schema, output shape, canonical use case. Preserve existing catalog ordering. |
| T3.4 | Update `CLAUDE.md` §2 MCP-first ordering | **TECH-346** | Done (archived) | Edit `CLAUDE.md` §2 "MCP first" — insert `reserve_backlog_ids` / `backlog_record_validate` into the suggested order for issue-creation flows, and `backlog_list` for structured list queries. Do not rewrite the full ordering block — additive edits only. |

---

### Stage 4 — MEDIUM / LOW band (IP6–IP9) / Script hardening (IP7)

**Status:** In Progress (TECH-355, TECH-356, TECH-357 filed)

**Objectives:** Flock-guard `materialize-backlog.sh` so parallel stage-file runs + parallel MCP `backlog_record_create` callers serialize on the regen step. Add a concurrency test mirroring the existing `reserve-id-concurrent.sh` harness. No schema or behavior change for single-writer callers.

**Exit:**

- `tools/scripts/materialize-backlog.sh` invocations route through `flock ia/state/.backlog.lock` (create the lock file if absent; same pattern as `reserve-id.sh`).
- `tools/scripts/test/materialize-concurrent.sh` — N=8 parallel invocations, assert BACKLOG.md + BACKLOG-ARCHIVE.md byte-identical to a serial baseline regen. Runs under `npm run validate:all` or a dedicated `npm run validate:materialize-concurrent` script.
- `ia/state/.backlog.lock` documented in `tools/scripts/materialize-backlog.sh` header comment.
- Phase 1 — flock wrapper + lock-file creation + self-documenting header (single task per Decision Log 2026-04-18: flock wrap + header = atomic edit on same file; split creates thrash).
- Phase 2 — concurrency harness + CI wire-in.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Flock-guard `materialize-backlog.sh` + self-documenting header | **TECH-355** | Draft | Wrap the `node tools/scripts/materialize-backlog.mjs …` invocation inside `tools/scripts/materialize-backlog.sh` with `flock ia/state/.materialize-backlog.lock` (invariant #13 lockfile-per-domain — supersedes earlier `.backlog.lock` prose). Create the lock file if absent (touch under flock trap, same pattern as `reserve-id.sh`). Caveman header comment documents lock path + rationale ("parallel stage-file + MCP `backlog_record_create` writers serialize here") + cross-ref to `reserve-id.sh`. Merges original T2.1.1 + T2.1.2 per Decision Log. |
| T4.3 | Concurrency test `materialize-concurrent.sh` | **TECH-356** | Draft | Author `tools/scripts/test/materialize-concurrent.sh` — spawn N=8 parallel `materialize-backlog.sh` invocations; after all complete, diff BACKLOG.md + BACKLOG-ARCHIVE.md against a serial baseline regen; fail on any diff. Mirrors `tools/scripts/test/reserve-id-concurrent.sh` structure. |
| T4.4 | Wire concurrency test into validate chain | **TECH-357** | Draft | Add `validate:materialize-concurrent` script to root `package.json`; chain into `validate:all` OR a new `validate:concurrency` sub-chain (match existing convention). Document in `ARCHITECTURE.md` Local verification table if listed there. |

### Stage 5 — MEDIUM / LOW band (IP6–IP9) / Validator extensions (IP8)

**Status:** Draft — tasks `_pending_`.

**Objectives:** Extend `tools/validate-backlog-yaml.mjs` with cross-record checks: `related` ids must exist; `depends_on_raw` non-empty when `depends_on: []` non-empty; warn on drift when `depends_on_raw` mentions ids not in `depends_on: []`. All new checks land fixtures under `tools/scripts/test-fixtures/`.

**Exit:**

- `validate-backlog-yaml.mjs` implements the three new checks via the shared lint core (`backlog-record-schema.ts` from Stage 1.2) where applicable — cross-record checks (which need the whole set) stay in the script.
- Fixture set under `tools/scripts/test-fixtures/` — for each check, one passing fixture + one failing fixture + expected error text.
- `npm run validate:backlog-yaml` + `npm run validate:all` green on passing fixtures, red on failing fixtures (via a fixture-runner test harness).
- Phase 1 — `related` id existence check + fixtures.
- Phase 2 — `depends_on_raw` non-empty + drift warning + fixtures.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Cross-check `related` ids exist | _pending_ | _pending_ | In `tools/validate-backlog-yaml.mjs`, after loading both dirs, iterate records + assert every id in `related: []` exists in the combined set (open + archive). Emit error with source record + missing id. |
| T5.2 | Fixtures for `related` existence check | _pending_ | _pending_ | Add to `tools/scripts/test-fixtures/` — `related-exists-pass/` (two records, one refers to the other), `related-exists-fail/` (record refers to nonexistent id). Extend fixture harness to assert pass/fail outcomes + expected error text. |
| T5.3 | Enforce `depends_on_raw` non-empty | _pending_ | _pending_ | In `validate-backlog-yaml.mjs`, reject records where `depends_on: []` is non-empty AND `depends_on_raw` is empty / missing. Error names the record id + field. |
| T5.4 | Warn on `depends_on_raw` drift | _pending_ | _pending_ | Warning (not error) when `depends_on_raw` mentions an id not present in `depends_on: []`. Tokenize raw by `,` + strip soft markers before compare. Emit warning w/ record id + drift token. |
| T5.5 | Fixtures for `depends_on_raw` checks | _pending_ | _pending_ | Add fixtures — `depends-raw-pass/`, `depends-raw-empty-fail/`, `depends-raw-drift-warn/`. Fixture harness asserts error / warning outcomes + expected text. |

### Stage 6 — MEDIUM / LOW band (IP6–IP9) / MCP extensions (IP6 + IP9)

**Status:** Draft — tasks `_pending_`.

**Objectives:** Ship `backlog_record_create` MCP tool (atomic reserve → validate → write → materialize) + extend `backlog_search` with `priority` / `type` / `created_after` / `created_before` filters. Depends on Stage 1.1 (field extension), Stage 1.2 (reserve + validate tools), Stage 2.1 (flock-guarded materialize).

**Exit:**

- `tools/mcp-ia-server/src/tools/backlog-record-create.ts` — input `{ prefix, fields: Omit<ParsedBacklogIssue,"id"> }`, output `{ id, yaml_path }`. Flow: call `reserve_backlog_ids(count: 1)` → build yaml body → call `validateBacklogRecord` → tmp-file-then-rename write to `ia/backlog/{id}.yaml` → spawn `materialize-backlog.sh` (flock-guarded).
- `backlog-search.ts` accepts `priority?: string`, `type?: "BUG"|"FEAT"|"TECH"|"ART"|"AUDIO"`, `created_after?: string`, `created_before?: string`. Filters applied before scoring.
- Tests under `tools/mcp-ia-server/tests/tools/` — `backlog-record-create` happy path + validation-failure path + race (two parallel creates, distinct ids, both yaml files on disk, materialize ran). `backlog-search` filter combinations.
- Phase 1 — `backlog_record_create` implementation + atomicity test.
- Phase 2 — `backlog_search` filter extensions + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Implement `backlog_record_create` tool | _pending_ | _pending_ | `tools/mcp-ia-server/src/tools/backlog-record-create.ts` — input `{ prefix, fields }`, flow per Stage 2.3 Exit. Use `reserve_backlog_ids` (IP3) + shared lint core (Stage 1.2) + flock-guarded materialize (Stage 2.1). Tmp-file-then-rename for the yaml write. Register in `tools/mcp-ia-server/src/index.ts`. |
| T6.2 | Happy / failure path tests | _pending_ | _pending_ | `tools/mcp-ia-server/tests/tools/backlog-record-create.test.ts` — happy path (record created, yaml on disk, BACKLOG.md regenerated); validation-failure path (bad field → no yaml on disk, no id consumed, counter unchanged); concurrent-create path (two parallel calls → two distinct ids, both yaml files, BACKLOG.md has both entries). |
| T6.3 | Extend `backlog_search` filter inputs | _pending_ | _pending_ | In `tools/mcp-ia-server/src/tools/backlog-search.ts`, add optional input fields `priority`, `type`, `created_after`, `created_before` (ISO date strings). Apply filters before scoring. Update tool descriptor + any exported schema. |
| T6.4 | Test `backlog_search` filter extensions | _pending_ | _pending_ | Extend `tools/mcp-ia-server/tests/tools/backlog-search.test.ts` with fixture set covering each filter dimension + combined filters + date-range edge cases. Assert ordering preserved after filter. |

---

### Stage 7 — yaml schema v2 + backfill + validator MVP (locator fields) / yaml schema v2 + parser

**Status:** Final
**Backlog state (2026-04-18):** 4 tasks filed (TECH-363, TECH-364, TECH-365, TECH-366 all archived)

**Objectives:** Extend `ParsedBacklogIssue` + yaml loader to accept the 2 required + 7 optional locator fields. Regex-allowlist `task_key` per `^T\d+\.\d+(\.\d+)?$` (N1). Additive only — existing v1 records round-trip without the new fields.

**Exit:**

- `ParsedBacklogIssue` carries 9 new members (2 required on v2 writes; 7 optional throughout).
- Loader `yamlToIssue` populates new fields from yaml; absent = defaults (`null` / `[]`).
- `buildYaml` + writer path emit new fields when present; omit when absent (keep v1 records byte-identical on round-trip).
- Fixture set extends `tools/scripts/test-fixtures/` with full-v2 + missing-optional + missing-required examples.
- Phase 1 — Type + loader read-path extension.
- Phase 2 — Writer path + round-trip fixtures.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | Extend `ParsedBacklogIssue` v2 shape | **TECH-363** | Done (archived) | Add to `tools/mcp-ia-server/src/parser/types.ts` (or wherever `ParsedBacklogIssue` lives post-Step-1): `parent_plan: string \ | null`, `task_key: string \ | null`, `step: number \ | null`, `stage: string \ | null`, `phase: number \ | null`, `router_domain: string \ | null`, `surfaces: string[]`, `mcp_slices: string[]`, `skill_hints: string[]`. Null allowed on all to keep markdown-fallback path compilable. |
| T7.2 | Map new fields in yaml read path | **TECH-364** | Done (archived) | In `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `yamlToIssue` sets all 9 fields from yaml record. Arrays default `[]`, scalars default `null`. Add regex guard on read for `task_key`: reject on parse if present + not matching `^T\d+\.\d+(\.\d+)?$`. |
| T7.3 | Emit new fields in writer path | **TECH-365** | Done (archived) | In `backlog-yaml-loader.ts` `buildYaml` (or the equivalent writer) + `tools/scripts/migrate-backlog-to-yaml.mjs`: emit `parent_plan`, `task_key`, optional scalars + arrays when present. Omit absent fields (no empty arrays or `null:` keys written). Preserve existing section order + block-literal style. |
| T7.4 | Round-trip fixtures for schema v2 | **TECH-366** | Done (archived) | Add `tools/scripts/test-fixtures/schema-v2-full.yaml` (all 9 fields) + `schema-v2-minimal.yaml` (only 2 required) + `schema-v1-legacy.yaml` (zero locator fields, proves back-compat). Load + round-trip test asserts byte-identical output per fixture. Hook into MCP tests folder too. |

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

### Stage 9 — yaml schema v2 + backfill + validator MVP (locator fields) / `parent_plan_validate` + `backlog_record_validate` v2

**Status:** Final

**Backlog state (Stage 3.3):** 5 filed (TECH-406, TECH-407, TECH-408, TECH-409, TECH-410 Done (archived)).

**Objectives:** Ship the `parent_plan_validate` MCP tool + matching `tools/validate-parent-plan-locator.mjs` CLI validator (dual-mode: advisory default + `--strict` flag). Extend the existing `backlog_record_validate` shared lint core (`backlog-record-schema.ts`) with schema-v2 awareness (new-field regex / type checks). Keep advisory-default through Step 6; strict-flip lives in Step 6 late-hardening.

**Exit:**

- `tools/validate-parent-plan-locator.mjs (new)` — scans `ia/backlog/*.yaml` + `ia/backlog-archive/*.yaml`; checks: `parent_plan` path resolves on disk; `task_key` matches `^T\d+\.\d+(\.\d+)?$`; `task_key` present as row in `parent_plan` (line match); plan row `Issue: **{id}**` back-references yaml id. Dual-mode per source doc Phase 6 Step 1.
- `tools/mcp-ia-server/src/tools/parent-plan-validate.ts (new)` — input `{ strict?: boolean = false }`, output `{ errors: string[], warnings: string[], exit_code: 0|1 }`. Delegates to shared validator core.
- `backlog-record-schema.ts` schema-v2 awareness — regex guard on `task_key`; type guards on arrays (`surfaces`, `mcp_slices`, `skill_hints`); `parent_plan` path-string format check (no existence check here — that lives in `parent_plan_validate`).
- Fixtures under `tools/scripts/test-fixtures/parent-plan-validate/` — plan-exists-pass, plan-missing-fail, task-key-bad-regex-fail, task-key-drift-warn, issue-back-ref-missing-warn.
- Advisory run emits drift count line when drift exists; silent when clean. `--strict` (CLI) / `strict: true` (MCP) escalates to errors + exit 1.
- Phase 1 — Shared validator core + CLI dual-mode.
- Phase 2 — MCP tool wrapper + schema-v2 lint extensions + fixtures.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | Author shared validator core | **TECH-406** | Done (archived) | Create `tools/mcp-ia-server/src/parser/parent-plan-validator.ts (new)` exporting `validateParentPlanLocator({ yamlDirs: string[], planGlob: string, strict: boolean }): { errors, warnings, exit_code }`. Implements the 4 checks (path resolve / regex / task_key-in-plan / back-ref). Pure function; no process exit. |
| T9.2 | CLI wrapper + dual-mode flag | **TECH-407** | Done (archived) | `tools/validate-parent-plan-locator.mjs (new)` — wraps core from T3.3.1; `--strict` / `--advisory` flag parsing; default advisory; prints drift count on advisory; full errors on strict. Exit 0 advisory (always) or 1 on strict + error. Add `npm run validate:parent-plan-locator` script to root `package.json`; chain into `validate:all` as advisory (non-blocking) for now. |
| T9.3 | MCP tool wrapper | **TECH-408** | Done (archived) | `tools/mcp-ia-server/src/tools/parent-plan-validate.ts (new)` — input schema `{ strict?: boolean }`, calls `validateParentPlanLocator` with repo-relative paths, returns `{ errors, warnings, exit_code }`. Register in `tools/mcp-ia-server/src/index.ts` tool registry. Tool descriptor notes schema-cache restart (N4). |
| T9.4 | Extend `backlog_record_schema.ts` | **TECH-409** | Done (archived) | In `tools/mcp-ia-server/src/parser/backlog-record-schema.ts`, add: `task_key` regex check when present; `surfaces` / `mcp_slices` / `skill_hints` must be `string[]` when present; `parent_plan` must be non-empty string when present (existence check deferred to `parent_plan_validate`). Shared by CLI + `backlog_record_validate` MCP tool. |
| T9.5 | Fixtures for validator | **TECH-410** | Done (archived) | Under `tools/scripts/test-fixtures/parent-plan-validate/`: `plan-exists-pass/`, `plan-missing-fail/`, `task-key-bad-regex-fail/`, `task-key-drift-warn/` (plan exists but no row matches), `issue-back-ref-missing-warn/` (plan has row but `Issue:` points elsewhere). Harness under `tools/mcp-ia-server/tests/tools/parent-plan-validate.test.ts` asserts advisory vs strict outputs per fixture. |

---

### Stage 10 — MCP reverse-lookup tooling / `master_plan_locate` + `master_plan_next_pending`

**Status:** Final

**Backlog state (Stage 4.1):** 5 filed

**Objectives:** Ship the two reverse-lookup tools. `master_plan_locate` reads yaml `parent_plan` + `task_key`, then greps plan for the task row line. `master_plan_next_pending` scans plan task tables + returns the first `_pending_` / Draft row (top-of-table tie-break per S3). Both tools deterministic; both register new in the MCP tool registry; both ship tests against fixture plans.

**Exit:**

- `master_plan_locate` responds `{ plan, step, stage, phase, task_key, row_line, row_raw }` for fixture TECH-283 or any fixture v2 yaml.
- `master_plan_next_pending(plan, stage?)` returns first unfiled / Draft row; deterministic top-of-table; `null` when stage complete.
- Tests cover: locate happy path, locate on yaml-without-`parent_plan` (returns error with reason), next-pending with stage filter, next-pending returning null on fully-filed stage, tie-break determinism (2 pending rows → first wins).
- Both tools registered in `tools/mcp-ia-server/src/index.ts`.
- Phase 1 — `master_plan_locate` implementation + tests.
- Phase 2 — `master_plan_next_pending` implementation + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | Implement `master_plan_locate` | **TECH-413** | Done (archived) | `tools/mcp-ia-server/src/tools/master-plan-locate.ts (new)` — input `{ issue_id: string }`. Load yaml via `parseBacklogIssue`; read `parent_plan` + `task_key`; read plan file; regex-match `^\ | ${task_key} \ | ` to find row line. Return `{ plan, step, stage, phase, task_key, row_line, row_raw }`. Error when yaml missing fields or plan path absent. Register in `tools/mcp-ia-server/src/index.ts`. |
| T10.2 | Fixture + tests for locate | **TECH-414** | Done (archived) | `tools/mcp-ia-server/tests/tools/master-plan-locate.test.ts` — fixture yaml with full v2 fields + fixture plan with matching row. Assert row_line + row_raw. Plus negative cases: yaml w/o `parent_plan` (error), plan-path-not-on-disk (error), task_key not found in plan (error with drift reason). |
| T10.3 | Implement `master_plan_next_pending` | **TECH-415** | Done (archived) | `tools/mcp-ia-server/src/tools/master-plan-next-pending.ts (new)` — input `{ plan: string, stage?: string }`. Read plan file; scan task tables; optionally filter to stage heading (`#### Stage X.Y`); return first row whose Status column matches `_pending_` / `Draft` (top-of-table order). Shape `{ issue_id, task_key, row_line, status } \ | null`. Register in `tools/mcp-ia-server/src/index.ts`. |
| T10.4 | Fixture + tests for next-pending | **TECH-416** | Done (archived) | `tools/mcp-ia-server/tests/tools/master-plan-next-pending.test.ts` — fixture plan with mixed Status column values. Assert: first `_pending_` wins; `Draft` wins over later `_pending_` only if top-of-table; stage filter respected; fully-`Done` stage returns `null`. Deterministic ordering per S3. |
| T10.5 | Tool descriptors + schema cache note | **TECH-417** | Done (archived) | Update both tool descriptors (`master_plan_locate`, `master_plan_next_pending`) with canonical use-case prose. Add schema-cache-restart note to descriptor text + to `docs/mcp-ia-server.md` tool catalog entries (N4). Document `--dry` NOT needed. |

### Stage 11 — MCP reverse-lookup tooling / `backlog_list` filter extensions + catalog docs

**Status:** In Progress

**Objectives:** Extend `backlog_list` with three locator-field filters (`parent_plan`, `stage`, `task_key`). Lowercase substring compare per N3 (matches existing filter pattern). Document the three new tools (`master_plan_locate`, `master_plan_next_pending`, `backlog_list`-extended) in `docs/mcp-ia-server.md` + update `CLAUDE.md` §2 MCP-first ordering (additive only).

**Exit:**

- `backlog_list` accepts `parent_plan?`, `stage?`, `task_key?` as optional inputs. Filters applied in-memory via lowercase substring compare.
- Tests extended under `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` — each new filter + multi-filter intersection + empty result + scope switch with new filters.
- `docs/mcp-ia-server.md` carries catalog entries for `master_plan_locate`, `master_plan_next_pending`, `parent_plan_validate` (from Step 3), + notes the `backlog_list` filter extensions.
- `CLAUDE.md` §2 MCP-first ordering — `master_plan_locate` added to single-issue lookup flows; `master_plan_next_pending` added to `/ship` suggested order; additive only (no rewrite).
- Phase 1 — `backlog_list` filter extensions + tests.
- Phase 2 — Tool catalog + CLAUDE ordering updates.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | Extend `backlog_list` inputs | **TECH-438** | Draft | In `tools/mcp-ia-server/src/tools/backlog-list.ts`, add optional input fields `parent_plan?`, `stage?`, `task_key?`. Apply filters after existing `section`/`priority`/`type`/`status`/`scope` filters (in-memory, lowercase substring compare per N3). Preserve id-desc ordering. Update tool descriptor. |
| T11.2 | Test `backlog_list` locator filters | **TECH-439** | Draft | Extend `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` fixture set to cover schema-v2 records across ≥2 plans + ≥2 stages. Assert: each new filter alone, multi-filter intersection with existing priority/type filters, empty result, scope switch. |
| T11.3 | Document new tools in `docs/mcp-ia-server.md` | **TECH-440** | Draft | Add catalog entries for `master_plan_locate` (from Stage 4.1), `master_plan_next_pending` (from Stage 4.1), `parent_plan_validate` (from Step 3 Stage 3.3). Append filter-extension note to existing `backlog_list` entry (3 new filters). Preserve catalog ordering + existing entries. |
| T11.4 | Update `CLAUDE.md` §2 MCP-first ordering | **TECH-441** | Draft | Edit `CLAUDE.md` §2 "MCP first" — append: `master_plan_locate` for issue→plan reverse lookup; `master_plan_next_pending` for `/ship` next-task; note `parent_plan_validate` runs in advisory mode during `validate:all`. Additive edits only — do not rewrite existing ordering. |

---

### Stage 12 — Skill patches + plan consumers / Seed skills (`project-new`, `stage-file`)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Teach `project-new` + `stage-file` to write full schema-v2 yaml at seed time. Both skills already reserve ids + write yaml; now they populate `parent_plan` + `task_key` + optional locator fields (`step`, `stage`, `phase`, `router_domain`, `surfaces`, `mcp_slices`, `skill_hints`) from plan task-row context. `stage-file` also runs `parent_plan_validate` on the freshly-seeded records (advisory).

**Exit:**

- `ia/skills/stage-file/SKILL.md` — seed step documents full v2 field population. `surfaces` pulled from the plan's `**Relevant surfaces (load when step opens):**` block + the task row's Intent column path refs. `mcp_slices` + `skill_hints` pulled from plan notes when present.
- `ia/skills/project-new/SKILL.md` — single-issue path requires `parent_plan` + `task_key` inputs; skill documents the fallback when neither plan nor task_key known (single-issue outside-plan path → both fields empty; validator advisory ignores).
- Both skills reference `backlog_record_validate` pre-write + `parent_plan_validate` post-write (advisory).
- Phase 1 — `stage-file` body patches.
- Phase 2 — `project-new` body patches.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | Patch `stage-file` body — full v2 seed | _pending_ | _pending_ | Edit `ia/skills/stage-file/SKILL.md` seed-yaml step — document v2 field population: `parent_plan` = orchestrator path arg; `task_key` from task row id column; `step` / `stage` / `phase` derived from `task_key` parser; `router_domain` from MCP `router_for_task` first match; `surfaces` from plan's Relevant-surfaces block + task Intent path refs; `mcp_slices` + `skill_hints` from plan notes when present. Caveman prose. |
| T12.2 | Wire `parent_plan_validate` advisory into stage-file | _pending_ | _pending_ | Add to `ia/skills/stage-file/SKILL.md` a post-write step: call `parent_plan_validate` (MCP) in advisory mode after all yaml writes + before `materialize-backlog.sh`. Warn on drift count; do NOT block (strict flip lives in Step 6). |
| T12.3 | Patch `project-new` body — single-issue v2 seed | _pending_ | _pending_ | Edit `ia/skills/project-new/SKILL.md` — require `parent_plan` + `task_key` inputs when caller passes plan context; allow both empty for single-issue outside-plan flows. Document derivation rules + fallback. Bash fallback kept for MCP-unavailable case. |
| T12.4 | Update `project-new` input interview | _pending_ | _pending_ | Edit `ia/skills/project-new/SKILL.md` interview step — add `parent_plan?` + `task_key?` to the structured-input block; skill prompts when missing + plan context detected via `--plan` arg. Document in slash-command dispatcher (`.claude/commands/project-new.md`) if input schema exposed there. |

### Stage 13 — Skill patches + plan consumers / Read skills (author / implement)

**Status:** Draft (tasks _pending_ — not yet filed; T13.1 + T13.3 cancelled by M6 collapse)

**Objectives:** Teach the surviving live read skills to consume `surfaces` / `mcp_slices` / `skill_hints` from yaml before round-tripping `router_for_task` / `spec_section`. Append-only `surfaces` guardrail fires inside `plan-author` (absorbs the retired `project-spec-kickoff` surface-reading path per M6 collapse). Plan-row-flip is now owned by the Stage-scoped `/closeout` pair (`stage-closeout-plan` → `plan-applier` Mode stage-closeout), already MCP-driven; no separate patch needed. All optional — fallbacks (router / grep) kept for MCP-unavailable + field-absent cases.

**Exit:**

- `ia/skills/plan-author/SKILL.md` — reads `surfaces` / `mcp_slices` / `skill_hints` FIRST during Stage-bulk §Plan Author authoring; append-only guardrail on `surfaces` in §4 / §5.2 regions (never reorder / rewrite / drop). Guardrail documented + enforced via validator warning.
- `ia/skills/project-spec-implement/SKILL.md` — `skill_hints` consumed as routing hint (advisory, not mandate); doc notes hint NOT enforced on drift (N5 policy).
- `parent_plan_validate` gains a `surfaces`-guardrail check — warns on reorder / rename / drop relative to last-seen state (tracked via content hash in yaml + new optional field `surfaces_hash` OR just warns on any diff vs plan's Relevant-surfaces block).
- Phase 1 — plan-author + implement patches.
- Phase 2 — surfaces-guardrail validator extension + fixtures.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T13.1 | ~~Patch kickoff~~ | Cancelled (obsolete) | Cancelled | Retired surface `project-spec-kickoff`. Functionality absorbed into `plan-author` Stage-bulk authoring — surface-reading patch should attach to `plan-author/SKILL.md` spec-section-load step instead. File as replacement task if still desired. |
| T13.2 | Patch implementer — skill_hints as advisory | _pending_ | _pending_ | Edit `ia/skills/project-spec-implement/SKILL.md` routing step — consume `skill_hints` from yaml as advisory suggestion; document fallback to `router_for_task` when empty; explicitly non-binding per N5 (hint, not mandate). |
| T13.3 | ~~Patch close skill — MCP plan-row flip~~ | Cancelled (obsolete) | Cancelled | Retired surface `project-spec-close`. Plan-row-flip now owned by `plan-applier` Mode stage-closeout (Stage-scoped `/closeout` pair), already calls `master_plan_locate` — patch not needed. |
| T13.4 | Surfaces-guardrail validator check | _pending_ | _pending_ | Extend `tools/mcp-ia-server/src/parser/parent-plan-validator.ts` (from T3.3.1) with a `surfaces` append-only check — warn when yaml `surfaces` list reorders / drops / renames entries relative to the last-written order (computed by storing a `surfaces_hash` in yaml OR diff-parsing the yaml history — pick during implementation). Warning, not error. |
| T13.5 | Fixture test for surfaces guardrail | _pending_ | _pending_ | Add fixtures under `tools/scripts/test-fixtures/surfaces-guardrail/` — `append-ok/`, `reorder-warn/`, `drop-warn/`, `rename-warn/`. Extend `parent-plan-validate.test.ts` to assert warning outputs per fixture. Matches exploration Example 4. |

### Stage 14 — Skill patches + plan consumers / Dispatcher consumers (`/ship`, `release-rollout-enumerate`)

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Swap scan-driven next-task + rollout-enumerate paths for direct MCP / yaml reads. `/ship` next-task uses `master_plan_next_pending`. `release-rollout-enumerate` reads yaml `parent_plan` + `task_key` directly instead of inferring from plan scans. Fallbacks kept; next-task behavior stays aligned with `.claude/commands/ship.md` (Next-handoff resolver — master-plan scan, no BACKLOG numeric adjacency).

**Exit:**

- `/ship` dispatcher (`.claude/commands/ship.md` or equivalent) — next-task-lookup step calls `master_plan_next_pending {plan, stage?}` first; plan-scan fallback kept.
- `ia/skills/release-rollout-enumerate/SKILL.md` — per-row data pull reads yaml `parent_plan` + `task_key` + `stage` directly via `backlog_list parent_plan=`; inference fallback noted.
- Rehearsal fixture proves one full `/project-new → /author → /implement → /closeout` cycle on schema-v2 yaml with MCP happy path + no scan fallbacks triggered (post-M6 flow; `/kickoff` retired, replaced by `/author` (`plan-author` Stage 1×N)).
- Canonical next-task behavior documented in `.claude/commands/ship.md` — or note added in skill that MCP path supersedes scan guidance where applicable.
- Phase 1 — `/ship` dispatcher wiring.
- Phase 2 — `release-rollout-enumerate` + end-to-end rehearsal.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T14.1 | Verify `/ship` dispatcher surface | _pending_ | _pending_ | Glob for `/ship` dispatcher path — likely `.claude/commands/ship.md` OR a `/ship`-named skill under `ia/skills/`. Read + document the canonical surface in the spec. Do NOT guess; `stage-file` kicked this off via user-memory hint but dispatcher wiring may live in a different surface. |
| T14.2 | Wire `master_plan_next_pending` into `/ship` | _pending_ | _pending_ | Patch the dispatcher from T5.3.1 — next-task-lookup step calls `master_plan_next_pending {plan, stage?}` first; scan fallback kept with "if MCP unavailable" clause. Caveman body prose. |
| T14.3 | Wire `release-rollout-enumerate` to yaml direct | _pending_ | _pending_ | Edit `ia/skills/release-rollout-enumerate/SKILL.md` per-row-enumeration step — read `parent_plan` + `task_key` + `stage` directly from yaml via `backlog_list parent_plan=` (extended filter from Stage 4.2); inference-from-plan-scan fallback kept as "if yaml missing fields" clause. |
| T14.4 | End-to-end rehearsal fixture + note | _pending_ | _pending_ | Document in `docs/parent-plan-locator-fields-exploration.md` (append section) OR in this master plan's Acceptance section: one full `/project-new → /author → /implement → /closeout` cycle on fixture yaml with all MCP happy-path calls succeeding + zero fallback triggers (post-M6 flow). Rehearsal = manual; documentation = written evidence, not automated test. |

---

### Stage 15 — Late-hardening + archive backfill (deferred) / Flip validator default to blocking

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Flip `validate-parent-plan-locator.mjs` default mode from advisory → strict; keep `--advisory` as opt-out. Update `validate:all` chain so CI fails on drift. Document the flip in `docs/agent-led-verification-policy.md`. Gate on zero-drift-for-≥1-week-in-production (tracked in this plan's acceptance).

**Exit:**

- Validator CLI default exit code = 1 on any error (was 0 advisory-default in Step 3).
- `--advisory` flag retained + documented; flips exit code back to 0.
- `package.json` `validate:all` script chains the validator in strict mode.
- `docs/agent-led-verification-policy.md` entry documents the flip + opt-out.
- Fixture test covers: strict-default-fail-on-drift, advisory-opt-out-still-green.
- Phase 1 — CLI default flip + `validate:all` wire-in.
- Phase 2 — Docs + fixture updates.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T15.1 | Flip validator CLI default | _pending_ | _pending_ | Edit `tools/validate-parent-plan-locator.mjs` — default mode = strict (exit 1 on any error); `--advisory` flag retained (exit 0 + drift count). MCP tool `parent_plan_validate` default input flipped to `strict: true` as well; `strict: false` retained as opt-out. |
| T15.2 | Chain strict validator into `validate:all` | _pending_ | _pending_ | Edit root `package.json` — `validate:all` script includes `npm run validate:parent-plan-locator` (strict by default after T6.1.1). Document chain entry in `ARCHITECTURE.md` Local verification table if the script is listed there. |
| T15.3 | Document blocking-flip in verification policy | _pending_ | _pending_ | Edit `docs/agent-led-verification-policy.md` — add entry documenting the advisory → strict flip: gate criteria (≥1 week zero drift in production), `--advisory` opt-out contract, fallback on CI red (run `backfill-parent-plan-locator.sh` + re-run). |
| T15.4 | Fixture tests for strict default | _pending_ | _pending_ | Update `tools/mcp-ia-server/tests/tools/parent-plan-validate.test.ts` fixtures — assert strict-default-fail on drift fixtures (previously advisory-green); assert `--advisory` opt-out still exits 0. Cover MCP tool `strict: false` input path too. |

### Stage 16 — Late-hardening + archive backfill (deferred) / Archive backfill pass

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Extend `tools/scripts/backfill-parent-plan-locator.sh` + `.mjs` driver with archive-mode scan (`ia/backlog-archive/*.yaml`) + proper `--skip-unresolvable` handling of plan-missing AND task_key-missing edge cases (N5). Archive records generally won't have full-plan context; backfill skips gracefully + logs per-reason counts.

**Exit:**

- Backfill driver accepts `--archive` flag → scans `ia/backlog-archive/*.yaml` instead of open dir.
- `--skip-unresolvable` handles both edge cases: (a) plan path missing from disk (archived + plan later deleted), (b) task_key suffix absent from title (archive-only records + pre-locator vintage). Each skip reason logged separately.
- Archive pass runs clean on current `ia/backlog-archive/*.yaml`; log reports resolved / skipped-plan-missing / skipped-task-key-missing counts.
- `--dry-run` supported for archive mode (preview skip reasons).
- Doc in `docs/parent-plan-locator-fields-exploration.md` (append) or this master plan Handoff — archive backfill is one-shot; no re-run expected unless plans move.
- Phase 1 — Archive-mode flag + skip-reason logging.
- Phase 2 — Dry-run + fixture tests + doc.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T16.1 | Add `--archive` flag to backfill driver | _pending_ | _pending_ | Edit `tools/scripts/backfill-parent-plan-locator.mjs` — `--archive` flag swaps the yaml-dir glob from `ia/backlog/*.yaml` to `ia/backlog-archive/*.yaml`. Default stays open-dir. Shell wrapper (`backfill-parent-plan-locator.sh`) passes the flag through. |
| T16.2 | Per-reason skip logging | _pending_ | _pending_ | Extend `--skip-unresolvable` behavior — track + log separately: `plan-missing` (parent_plan path not on disk), `task-key-missing` (title has no `(Stage X.Y Phase Z)` suffix + no other coord source). Per-run summary reports both counts + a combined resolved count. |
| T16.3 | Dry-run + fixture tests for archive mode | _pending_ | _pending_ | Extend `tools/scripts/test-fixtures/backfill-locator/archive/` — fixtures for archive-resolved, plan-missing-skip, task-key-missing-skip. Harness asserts count outputs + reason breakdown + dry-run emits preview without writes. |
| T16.4 | Document archive backfill handoff | _pending_ | _pending_ | Append section to `docs/parent-plan-locator-fields-exploration.md` Handoff OR to this master plan's Handoff (under Step 6) — archive backfill is one-shot; document the expected skip counts on current repo state; note re-run only needed if plans move / rename. |

---

## Acceptance (whole orchestrator)

- All 9 Implementation Points (IP1–IP9) shipped + soft-dep marker fix folded in.
- `npm run validate:all` green across the whole chain (lint + typecheck + MCP tests + validator + concurrency harness).
- `unity:compile-check` N/A — zero Unity / C# touches in this plan.
- `ia/state/id-counter.json` never hand-edited — all writes through `reserve-id.sh` (direct) or `reserve_backlog_ids` MCP tool (indirect).
- Soft-dep markers (e.g. `(soft)`, `[optional]`) preserved end-to-end across yaml round-trip.
- Deterministic materialize remains byte-identical for workloads unaffected by this plan.
- Parallel `stage-file` + parallel MCP `backlog_record_create` runs race-free.
- `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` document the MCP-first call path with bash fallback.
- `docs/mcp-ia-server.md` + `CLAUDE.md` §2 updated with new tools.

## Non-goals

- Replace the minimal yaml parser with a real yaml library.
- Change per-issue yaml layout or section manifest shape.
- Migrate `BACKLOG.md` / `BACKLOG-ARCHIVE.md` away from the generated-view model.
- Touch Unity runtime, save schema, glossary entries, or any other IA surface beyond skill docs + tool catalog.
- File the BACKLOG rows for this plan — user runs `/stage-file ia/projects/backlog-yaml-mcp-alignment-master-plan.md Stage 1.1` (etc) in a separate agent session.

## Handoff

Next: `/stage-file ia/projects/backlog-yaml-mcp-alignment-master-plan.md Stage 1.1` — file the Stage 1.1 tasks as BACKLOG rows + per-issue yaml records. Repeat per stage, priority order (1.1 → 1.2 → 1.3 → 2.1 → 2.2 → 2.3). Do NOT file Stage 2.* before Stage 1.* completes — Step 2 depends on Step 1 outputs (shared lint core, reserve tool, flock-guarded materialize).
