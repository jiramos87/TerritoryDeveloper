# Backlog YAML ↔ MCP alignment — Master Plan

> **Last updated:** 2026-04-17
>
> **Status:** Draft — Steps 1 + 2 fully decomposed; all tasks `_pending_` (not filed). Ready for `/stage-file` in a follow-up agent session.
>
> **Scope:** Align MCP territory-ia tool surface + `ParsedBacklogIssue` type + validator + skill docs with the per-issue yaml backlog refactor. Nine Implementation Points (HIGH band IP1–IP5, MEDIUM/LOW band IP6–IP9) plus one correctness fix (soft-dep marker preservation, folded into IP1). Pure tooling / MCP / validator / skill-docs work — zero Unity runtime touches, zero save-schema touches.
>
> **Exploration source:** `docs/backlog-yaml-mcp-alignment-exploration.md` (§Problem, §Design Expansion block, §Deferred decomposition hints). Block is ground truth.
>
> **Locked decisions (do not reopen in this plan):**
> - Per-issue yaml layout (`ia/backlog/{id}.yaml`, `ia/backlog-archive/{id}.yaml`) + section manifests (`ia/state/backlog-sections.json`, `ia/state/backlog-archive-sections.json`) stay byte-compatible.
> - Monotonic id source stays `ia/state/id-counter.json` via `tools/scripts/reserve-id.sh` under flock (invariant #13).
> - Materialize stays deterministic — `BACKLOG.md` + `BACKLOG-ARCHIVE.md` are generated views, never hand-edited.
> - Minimal yaml parser in `backlog-yaml-loader.ts` stays — no migration to a real yaml lib in this plan.
> - `proposed_solution` field fate decided by Grep gate (zero consumers → drop; ≥1 consumer → add to yaml schema). Decision captured in IP2 Stage.
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
> - `docs/backlog-yaml-mcp-alignment-exploration.md` — full problem analysis + 9 Implementation Points.
> - `tools/mcp-ia-server/src/parser/backlog-parser.ts` + `backlog-yaml-loader.ts` — current parser surface.
> - `tools/mcp-ia-server/src/parser/types.ts` (or equivalent) — `ParsedBacklogIssue` shape.
> - `tools/scripts/reserve-id.sh` + `tools/scripts/materialize-backlog.sh` + `tools/scripts/materialize-backlog.mjs` — ID + materialize flow.
> - `tools/validate-backlog-yaml.mjs` — current validator scope.
> - `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` + `ia/skills/project-spec-close/SKILL.md` — skills that write/mutate yaml.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — phase/task cardinality + permanent-orchestrator rule.
>
> **Invariants implicated:**
> - #12 (`ia/projects/` for issue-specific specs — applies to every `_pending_` row filed under this orchestrator).
> - #13 (monotonic id source = `reserve-id.sh` — IP3 MCP wrapper calls the script, never hand-edits the counter).
> - Invariants #1–#11 NOT implicated — no Unity runtime / no `GridManager` / no road / no HeightMap touches.

---

## Steps

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final`. Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft`; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

---

### Step 1 — HIGH band (IP1–IP5)

**Status:** Draft — tasks `_pending_`, not filed.

**Objectives:** Close the five HIGH-priority gaps. Extend `ParsedBacklogIssue` + yaml loader with `priority`, `related`, `created` + preserve soft-dep markers (IP1). Decide `proposed_solution` fate via Grep gate and execute the decision (IP2). Ship three new MCP tools: `reserve_backlog_ids` (IP3), `backlog_list` (IP4), `backlog_record_validate` (IP5). Wire skill docs so `stage-file` / `project-new` / closeout agents call MCP tools first when available.

**Exit criteria:**

- `ParsedBacklogIssue` carries `priority: string | null`, `related: string[]`, `created: string | null`.
- `backlog-yaml-loader.ts` maps all three fields from yaml + preserves `depends_on_raw` source string (soft-dep markers survive round-trip).
- `proposed_solution` fate locked + executed (dropped or added to yaml schema + loader).
- MCP server registers `reserve_backlog_ids`, `backlog_list`, `backlog_record_validate` with green tests under `tools/mcp-ia-server/tests/tools/`.
- Shared lint core extracted to `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` — consumed by both `validate-backlog-yaml.mjs` and `backlog_record_validate` MCP tool.
- `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` updated — MCP-first call path documented; bash fallback kept.
- `npm run validate:all` green (lint + typecheck + tests + fixtures).
- No BACKLOG.md hand-edits. `ia/state/id-counter.json` touched only via `reserve-id.sh`.

**Art:** None. Pure tooling / MCP / docs.

**Relevant surfaces (load when step opens):**
- `docs/backlog-yaml-mcp-alignment-exploration.md` §Design Expansion — IP1–IP5 details.
- `tools/mcp-ia-server/src/parser/backlog-parser.ts` + `backlog-yaml-loader.ts` + `backlog-parser.ts`'s type source.
- `tools/mcp-ia-server/src/tools/backlog-issue.ts` + `backlog-search.ts` — downstream payload shape.
- `tools/mcp-ia-server/src/index.ts` — tool registry.
- `tools/mcp-ia-server/tests/**` — fixture + test harness conventions.
- `tools/validate-backlog-yaml.mjs` — lint surface to refactor.
- `tools/scripts/reserve-id.sh` — id reservation script.
- `ia/skills/stage-file/SKILL.md`, `ia/skills/project-new/SKILL.md`, `ia/skills/project-spec-close/SKILL.md` — skill bodies.

#### Stage 1.1 — Types + yaml loader (IP1 + IP2)

**Status:** In Progress — 7 tasks filed (TECH-295..TECH-301), all Draft.

**Objectives:** Land the `ParsedBacklogIssue` shape extension + yaml-loader field mapping. Fix the soft-dep marker preservation bug (correctness). Resolve the `proposed_solution` fate via a Grep audit and execute the resulting path (drop from the type OR add to yaml schema + loader).

**Exit:**

- `priority`, `related`, `created` present on `ParsedBacklogIssue`; loader maps from yaml; markdown fallback sets sane defaults (`null` / `[]`).
- `depends_on_raw` fallback in loader prefers the yaml source string; only synthesizes from array when source was empty. Soft markers (e.g. `FEAT-12 (soft)`) preserved across round-trip.
- `proposed_solution` Grep audit complete — zero consumers → removed from the type + every read call-site; ≥1 consumer → added to yaml schema via `buildYaml` + loader + validator.
- Tests extended under `tools/mcp-ia-server/tests/**` with fixtures covering: all three new fields present / absent, soft-dep marker preservation, `proposed_solution` presence/absence per decision.
- `npm run validate:all` green.

**Phases:**

- [ ] Phase 1 — Type + loader extension (three new fields + soft-dep fallback fix).
- [ ] Phase 2 — `proposed_solution` decision + execution.
- [ ] Phase 3 — Test coverage + downstream payload surfacing.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.1.1 | Extend `ParsedBacklogIssue` shape | 1 | **TECH-295** | Done (archived) | Add `priority: string \| null`, `related: string[]`, `created: string \| null` to `ParsedBacklogIssue` in `tools/mcp-ia-server/src/parser/backlog-parser.ts` (or the extracted types module if one exists). Update any dependent type exports. No behavior change yet — loader mapping lands in T1.1.2. |
| T1.1.2 | Map new fields in yaml loader | 1 | **TECH-296** | Done (archived) | In `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`, `yamlToIssue` sets `priority`, `related`, `created` from yaml record. Markdown-path callers (legacy parser) default to `null` / `[]` when yaml absent. Cover both existing fixtures + at least one new fixture with all three fields. |
| T1.1.3 | Fix `depends_on_raw` soft-marker fallback | 1 | **TECH-297** | Done (archived) | In `backlog-yaml-loader.ts`, replace fallback `depends_on_raw = array.join(", ")` with: prefer yaml source string when non-empty; only synthesize from array when raw is absent. Add fixture with `depends_on: ["FEAT-12"]` + `depends_on_raw: "FEAT-12 (soft)"` and assert `resolveDependsOnStatus` sees the `(soft)` marker. |
| T1.2.1 | Grep-audit `proposed_solution` consumers | 2 | **TECH-298** | Done (archived) | Run `Grep` across repo for reads of `.proposed_solution` / `proposed_solution:` / `"proposed_solution"`. Record the full consumer list in a scratch note on the issue. Decision rule: zero consumers → choose Option A (drop); ≥1 consumer → choose Option B (add to yaml). Record decision + rationale in the spec's §1 of the filed project spec. |
| T1.2.2 | Execute `proposed_solution` decision | 2 | **TECH-299** | Done (archived) | Execute per T1.2.1 decision. **Option A:** remove `proposed_solution` from `ParsedBacklogIssue` + loader + parser + all reads; no yaml schema change. **Option B:** add `proposed_solution?: string` to yaml schema via `buildYaml` in `tools/scripts/migrate-backlog-to-yaml.mjs`, emit in loader, extend validator schema, update at least one fixture. Either option lands tests for the chosen behavior. |
| T1.3.1 | Surface new fields in `backlog_issue` + `backlog_search` payloads | 3 | **TECH-300** | Done (archived) | In `tools/mcp-ia-server/src/tools/backlog-issue.ts` + `backlog-search.ts`, return the three new fields (`priority`, `related`, `created`) in the MCP response payload. No new filters here (IP9 adds them). Snapshot-update existing tests. |
| T1.3.2 | Round-trip soft-dep marker integration test | 3 | **TECH-301** | Done (archived) | Integration test under `tools/mcp-ia-server/tests/tools/` — load a yaml fixture with `depends_on_raw: "FEAT-12 (soft)"`, call `parseBacklogIssue` + `resolveDependsOnStatus`, assert `soft_only: true`. Plain-id counter-fixture asserts `soft_only: false`. `[optional]` deferred (parser has no classifier — see TECH-301 §OpenQ1). Prevents regression of the loader bug. |

#### Stage 1.2 — MCP tools batch 1 (IP3 + IP4 + IP5)

**Status:** In Progress — 7 tasks filed (TECH-323..TECH-329), all Draft.

**Objectives:** Ship the three new MCP tools: `reserve_backlog_ids` wrapping `reserve-id.sh`, `backlog_list` for structured filter queries, `backlog_record_validate` for pre-write lint. Extract the shared lint core (`backlog-record-schema.ts`) so `validate-backlog-yaml.mjs` and `backlog_record_validate` share logic.

**Exit:**

- `tools/mcp-ia-server/src/tools/reserve-backlog-ids.ts` — spawns `reserve-id.sh {PREFIX} {N}`, returns `{ ids: string[] }`.
- `tools/mcp-ia-server/src/tools/backlog-list.ts` — filters by `section` / `priority` / `type` / `status` / `scope`, returns ordered `{ issues, total }`.
- `tools/mcp-ia-server/src/tools/backlog-record-validate.ts` — input `{ yaml_body: string }`, output `{ ok, errors, warnings }`.
- `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` — shared schema / lint core consumed by validator script + MCP tool.
- Tools registered in `tools/mcp-ia-server/src/index.ts` tool registry.
- Tests under `tools/mcp-ia-server/tests/tools/` cover: reserve concurrency (N parallel calls → zero dup ids), list filter combinations + empty result + scope switch, validator good + bad records (required fields, id format, status enum, soft-dep consistency).
- Tool descriptors match `mcp__territory-ia__*` naming convention.

**Phases:**

- [ ] Phase 1 — Shared lint core extraction + `backlog_record_validate`.
- [ ] Phase 2 — `reserve_backlog_ids` tool + concurrency test.
- [ ] Phase 3 — `backlog_list` tool + filter combinations.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.2.1 | Extract shared lint core | 1 | **TECH-323** | Done (archived) | Create `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` exporting `validateBacklogRecord(yamlBody: string): { ok, errors, warnings }`. Move schema checks (required fields, id format, status enum, `depends_on_raw` non-empty when `depends_on: []` non-empty) out of `tools/validate-backlog-yaml.mjs`. Update the validator script to import + call the shared core. |
| T1.2.2 | Implement `backlog_record_validate` MCP tool | 1 | **TECH-324** | Draft | `tools/mcp-ia-server/src/tools/backlog-record-validate.ts` — input schema `{ yaml_body: string }`, output `{ ok, errors, warnings }`. Delegate to shared core from T1.2.1. Register in `tools/mcp-ia-server/src/index.ts`. |
| T1.2.3 | Test `backlog_record_validate` against fixtures | 1 | **TECH-325** | Draft | Add tests under `tools/mcp-ia-server/tests/tools/backlog-record-validate.test.ts` — good record passes; each bad-record fixture (missing required field, bad id format, invalid status, empty `depends_on_raw` with non-empty `depends_on`) returns the expected error. |
| T1.2.4 | Implement `reserve_backlog_ids` MCP tool | 2 | **TECH-326** | Draft | `tools/mcp-ia-server/src/tools/reserve-backlog-ids.ts` — input `{ prefix: "TECH"\|"FEAT"\|"BUG"\|"ART"\|"AUDIO", count: 1..50 }`, spawn `tools/scripts/reserve-id.sh {prefix} {count}` via `child_process`, parse stdout, return `{ ids: string[] }`. Register in `tools/mcp-ia-server/src/index.ts`. |
| T1.2.5 | Concurrency test for `reserve_backlog_ids` | 2 | **TECH-327** | Draft | Add `tools/mcp-ia-server/tests/tools/reserve-backlog-ids.test.ts` — spawn 8 parallel invocations of the tool (counts 2 each), assert 16 unique ids returned + counter advanced correctly. Mirrors `tools/scripts/test/reserve-id-concurrent.sh` at the MCP layer. |
| T1.2.6 | Implement `backlog_list` MCP tool | 3 | **TECH-328** | Draft | `tools/mcp-ia-server/src/tools/backlog-list.ts` — input `{ section?, priority?, type?, status?, scope? (default "open") }`, load via `parseAllBacklogIssues`, apply filters in-memory, return `{ issues, total }` ordered by id desc. Register in `tools/mcp-ia-server/src/index.ts`. |
| T1.2.7 | Test `backlog_list` filter combinations | 3 | **TECH-329** | Draft | Add `tools/mcp-ia-server/tests/tools/backlog-list.test.ts` — fixture set covering ≥2 sections, ≥2 priorities, ≥2 types, open + archive. Assert: scope switch, single-filter cases, multi-filter intersection, empty result, id desc ordering. |

#### Stage 1.3 — Skill wiring + docs

**Status:** Draft — tasks `_pending_`.

**Objectives:** Update the skill bodies that shell out to `reserve-id.sh` + manually construct yaml + manually invoke `materialize-backlog.sh` so they document the MCP-first call path (`reserve_backlog_ids`, `backlog_record_validate`). Keep the bash fallback so skills work even when MCP is unavailable. Update `docs/mcp-ia-server.md` tool catalog.

**Exit:**

- `ia/skills/stage-file/SKILL.md` — call-path step for batch id reservation names `reserve_backlog_ids` MCP tool first; bash fallback kept as alternative.
- `ia/skills/project-new/SKILL.md` — single-id reservation step names `reserve_backlog_ids (count: 1)` first; bash fallback kept.
- `ia/skills/project-spec-close/SKILL.md` — no call-path change (closeout does not reserve ids); add a note that `backlog_record_validate` may lint the archive-bound yaml before move.
- `docs/mcp-ia-server.md` — three new tools documented in the catalog (inputs, outputs, when to use).
- `CLAUDE.md` §2 MCP-first ordering — add the three new tools to the suggested order where relevant.

**Phases:**

- [ ] Phase 1 — Skill body updates (`stage-file`, `project-new`, `project-spec-close`).
- [ ] Phase 2 — Tool catalog + CLAUDE ordering updates.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T1.3.1 | Wire MCP tools into `stage-file` + `project-new` skills | 1 | _pending_ | _pending_ | Edit `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` — reserve-id step names `reserve_backlog_ids` MCP tool first; `backlog_record_validate` step added after yaml body authoring + before disk write; bash fallbacks kept as "if MCP unavailable" alternative. Caveman prose. |
| T1.3.2 | Note `backlog_record_validate` use in close skill | 1 | _pending_ | _pending_ | Edit `ia/skills/project-spec-close/SKILL.md` — add a single-line note that `backlog_record_validate` may lint the archive-destination yaml before the move (defensive; optional). No behavior change. |
| T1.3.3 | Document new tools in `docs/mcp-ia-server.md` | 2 | _pending_ | _pending_ | Add three catalog entries in `docs/mcp-ia-server.md` for `reserve_backlog_ids`, `backlog_list`, `backlog_record_validate` — input schema, output shape, canonical use case. Preserve existing catalog ordering. |
| T1.3.4 | Update `CLAUDE.md` §2 MCP-first ordering | 2 | _pending_ | _pending_ | Edit `CLAUDE.md` §2 "MCP first" — insert `reserve_backlog_ids` / `backlog_record_validate` into the suggested order for issue-creation flows, and `backlog_list` for structured list queries. Do not rewrite the full ordering block — additive edits only. |

---

### Step 2 — MEDIUM / LOW band (IP6–IP9)

**Status:** Draft — tasks `_pending_`, not filed.

**Objectives:** Close the four MEDIUM/LOW gaps. Harden `materialize-backlog.sh` with a flock on `.backlog.lock` (IP7). Extend `validate-backlog-yaml.mjs` with cross-checks on `related` + `depends_on_raw` drift (IP8). Ship `backlog_record_create` MCP tool (IP6) + filter extensions to `backlog_search` (IP9). Step 2 depends on Step 1 — `backlog_record_create` reuses `reserve_backlog_ids` + `backlog_record_validate` + the shared lint core; `backlog_search` filters depend on `priority` / `created` fields on `ParsedBacklogIssue`.

**Exit criteria:**

- `tools/scripts/materialize-backlog.sh` wraps the `.mjs` invocation in `flock ia/state/.backlog.lock`.
- Parallel-materialize concurrency test under `tools/scripts/test/` — N=8 parallel invocations, BACKLOG.md regen deterministic + no truncation.
- `tools/validate-backlog-yaml.mjs` new checks: `related: []` ids must exist (in either dir); `depends_on_raw` non-empty when `depends_on: []` non-empty; warning when `depends_on_raw` mentions id not in `depends_on: []` (drift check).
- `tools/mcp-ia-server/src/tools/backlog-record-create.ts` — end-to-end atomic: reserve id → validate → write yaml → spawn materialize (flock-guarded via IP7).
- `tools/mcp-ia-server/src/tools/backlog-search.ts` gains `priority` / `type` / `created_after` / `created_before` filters.
- `npm run validate:all` green.

**Art:** None. Pure tooling / MCP / validator.

**Relevant surfaces (load when step opens):**
- `tools/scripts/materialize-backlog.sh` + `materialize-backlog.mjs`.
- `tools/validate-backlog-yaml.mjs`.
- `tools/mcp-ia-server/src/tools/backlog-search.ts` + IP1 / IP3 / IP5 outputs from Step 1.
- `tools/scripts/test/` — existing concurrency harness (`reserve-id-concurrent.sh`).
- `tools/scripts/test-fixtures/` — yaml fixture directory.

#### Stage 2.1 — Script hardening (IP7)

**Status:** Draft — tasks `_pending_`.

**Objectives:** Flock-guard `materialize-backlog.sh` so parallel stage-file runs + parallel MCP `backlog_record_create` callers serialize on the regen step. Add a concurrency test mirroring the existing `reserve-id-concurrent.sh` harness. No schema or behavior change for single-writer callers.

**Exit:**

- `tools/scripts/materialize-backlog.sh` invocations route through `flock ia/state/.backlog.lock` (create the lock file if absent; same pattern as `reserve-id.sh`).
- `tools/scripts/test/materialize-concurrent.sh` — N=8 parallel invocations, assert BACKLOG.md + BACKLOG-ARCHIVE.md byte-identical to a serial baseline regen. Runs under `npm run validate:all` or a dedicated `npm run validate:materialize-concurrent` script.
- `ia/state/.backlog.lock` documented in `tools/scripts/materialize-backlog.sh` header comment.

**Phases:**

- [ ] Phase 1 — flock wrapper + lock-file creation.
- [ ] Phase 2 — concurrency harness + CI wire-in.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.1.1 | Flock-guard `materialize-backlog.sh` | 1 | _pending_ | _pending_ | Wrap the `node tools/scripts/materialize-backlog.mjs …` invocation inside `tools/scripts/materialize-backlog.sh` with `flock ia/state/.backlog.lock`. Create the lock file if absent (touch under flock trap, same pattern as `reserve-id.sh`). Header comment documents the lock path + rationale. |
| T2.1.2 | Document flock in script header | 1 | _pending_ | _pending_ | Add a caveman header comment to `tools/scripts/materialize-backlog.sh` naming `ia/state/.backlog.lock` + rationale ("parallel stage-file + MCP `backlog_record_create` writers serialize here"). Cross-ref to `tools/scripts/reserve-id.sh` for the flock pattern. |
| T2.1.3 | Concurrency test `materialize-concurrent.sh` | 2 | _pending_ | _pending_ | Author `tools/scripts/test/materialize-concurrent.sh` — spawn N=8 parallel `materialize-backlog.sh` invocations; after all complete, diff BACKLOG.md + BACKLOG-ARCHIVE.md against a serial baseline regen; fail on any diff. Mirrors `tools/scripts/test/reserve-id-concurrent.sh` structure. |
| T2.1.4 | Wire concurrency test into validate chain | 2 | _pending_ | _pending_ | Add `validate:materialize-concurrent` script to root `package.json`; chain into `validate:all` OR a new `validate:concurrency` sub-chain (match existing convention). Document in `ARCHITECTURE.md` Local verification table if listed there. |

#### Stage 2.2 — Validator extensions (IP8)

**Status:** Draft — tasks `_pending_`.

**Objectives:** Extend `tools/validate-backlog-yaml.mjs` with cross-record checks: `related` ids must exist; `depends_on_raw` non-empty when `depends_on: []` non-empty; warn on drift when `depends_on_raw` mentions ids not in `depends_on: []`. All new checks land fixtures under `tools/scripts/test-fixtures/`.

**Exit:**

- `validate-backlog-yaml.mjs` implements the three new checks via the shared lint core (`backlog-record-schema.ts` from Stage 1.2) where applicable — cross-record checks (which need the whole set) stay in the script.
- Fixture set under `tools/scripts/test-fixtures/` — for each check, one passing fixture + one failing fixture + expected error text.
- `npm run validate:backlog-yaml` + `npm run validate:all` green on passing fixtures, red on failing fixtures (via a fixture-runner test harness).

**Phases:**

- [ ] Phase 1 — `related` id existence check + fixtures.
- [ ] Phase 2 — `depends_on_raw` non-empty + drift warning + fixtures.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.2.1 | Cross-check `related` ids exist | 1 | _pending_ | _pending_ | In `tools/validate-backlog-yaml.mjs`, after loading both dirs, iterate records + assert every id in `related: []` exists in the combined set (open + archive). Emit error with source record + missing id. |
| T2.2.2 | Fixtures for `related` existence check | 1 | _pending_ | _pending_ | Add to `tools/scripts/test-fixtures/` — `related-exists-pass/` (two records, one refers to the other), `related-exists-fail/` (record refers to nonexistent id). Extend fixture harness to assert pass/fail outcomes + expected error text. |
| T2.2.3 | Enforce `depends_on_raw` non-empty | 2 | _pending_ | _pending_ | In `validate-backlog-yaml.mjs`, reject records where `depends_on: []` is non-empty AND `depends_on_raw` is empty / missing. Error names the record id + field. |
| T2.2.4 | Warn on `depends_on_raw` drift | 2 | _pending_ | _pending_ | Warning (not error) when `depends_on_raw` mentions an id not present in `depends_on: []`. Tokenize raw by `,` + strip soft markers before compare. Emit warning w/ record id + drift token. |
| T2.2.5 | Fixtures for `depends_on_raw` checks | 2 | _pending_ | _pending_ | Add fixtures — `depends-raw-pass/`, `depends-raw-empty-fail/`, `depends-raw-drift-warn/`. Fixture harness asserts error / warning outcomes + expected text. |

#### Stage 2.3 — MCP extensions (IP6 + IP9)

**Status:** Draft — tasks `_pending_`.

**Objectives:** Ship `backlog_record_create` MCP tool (atomic reserve → validate → write → materialize) + extend `backlog_search` with `priority` / `type` / `created_after` / `created_before` filters. Depends on Stage 1.1 (field extension), Stage 1.2 (reserve + validate tools), Stage 2.1 (flock-guarded materialize).

**Exit:**

- `tools/mcp-ia-server/src/tools/backlog-record-create.ts` — input `{ prefix, fields: Omit<ParsedBacklogIssue,"id"> }`, output `{ id, yaml_path }`. Flow: call `reserve_backlog_ids(count: 1)` → build yaml body → call `validateBacklogRecord` → tmp-file-then-rename write to `ia/backlog/{id}.yaml` → spawn `materialize-backlog.sh` (flock-guarded).
- `backlog-search.ts` accepts `priority?: string`, `type?: "BUG"|"FEAT"|"TECH"|"ART"|"AUDIO"`, `created_after?: string`, `created_before?: string`. Filters applied before scoring.
- Tests under `tools/mcp-ia-server/tests/tools/` — `backlog-record-create` happy path + validation-failure path + race (two parallel creates, distinct ids, both yaml files on disk, materialize ran). `backlog-search` filter combinations.

**Phases:**

- [ ] Phase 1 — `backlog_record_create` implementation + atomicity test.
- [ ] Phase 2 — `backlog_search` filter extensions + tests.

**Tasks:**

| Task | Name | Phase | Issue | Status | Intent |
|---|---|---|---|---|---|
| T2.3.1 | Implement `backlog_record_create` tool | 1 | _pending_ | _pending_ | `tools/mcp-ia-server/src/tools/backlog-record-create.ts` — input `{ prefix, fields }`, flow per Stage 2.3 Exit. Use `reserve_backlog_ids` (IP3) + shared lint core (Stage 1.2) + flock-guarded materialize (Stage 2.1). Tmp-file-then-rename for the yaml write. Register in `tools/mcp-ia-server/src/index.ts`. |
| T2.3.2 | Happy / failure path tests | 1 | _pending_ | _pending_ | `tools/mcp-ia-server/tests/tools/backlog-record-create.test.ts` — happy path (record created, yaml on disk, BACKLOG.md regenerated); validation-failure path (bad field → no yaml on disk, no id consumed, counter unchanged); concurrent-create path (two parallel calls → two distinct ids, both yaml files, BACKLOG.md has both entries). |
| T2.3.3 | Extend `backlog_search` filter inputs | 2 | _pending_ | _pending_ | In `tools/mcp-ia-server/src/tools/backlog-search.ts`, add optional input fields `priority`, `type`, `created_after`, `created_before` (ISO date strings). Apply filters before scoring. Update tool descriptor + any exported schema. |
| T2.3.4 | Test `backlog_search` filter extensions | 2 | _pending_ | _pending_ | Extend `tools/mcp-ia-server/tests/tools/backlog-search.test.ts` with fixture set covering each filter dimension + combined filters + date-range edge cases. Assert ordering preserved after filter. |

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
