# MCP Lifecycle Tools — Opus 4.7 Audit — Master Plan (IA Infrastructure)

> **Status:** In Progress — Step 2 / Stage 2.2
>
> **Scope:** Reshape `territory-ia` MCP surface (32 tools) from 4.6-era sequential-call design to 4.7-era composite-bundle + structured-envelope architecture. Phased: quick wins → breaking envelope cut → composite bundles → mutation/authorship surface → bridge/journal lifecycle. Out of scope: backlog-yaml mutations (sibling master plan), Sonnet skill extractions (TECH-302), bridge transport rewrite, web dashboard tooling, computational-family batching.
>
> **Exploration source:** `docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md` (§Design Expansion — ground truth for all phases).
>
> **Locked decisions (do not reopen in this plan):**
> - Approach B selected — phased sequencing (P1 quick wins → P2 envelope → P3 composites → P4 mutations → P5 bridge/journal → P6 graph).
> - Breaking envelope cut: no dual-mode migration; all 32 handlers rewritten in one PR; caller sweep lands in same PR.
> - Hybrid bridge ceiling: `UNITY_BRIDGE_PIPELINE_CEILING_MS` env var (default 30 000 ms).
> - Caller-agent allowlist source of truth: `tools/mcp-ia-server/src/auth/caller-allowlist.ts`.
> - Journal `content_hash` dedup: 3-step migration (nullable column → batched SHA-256 backfill → NOT NULL).
> - Composite core vs optional sub-fetch: core fail → `ok: false`; optional fail → `meta.partial` tick, `ok: true`.
> - IA-authorship server split rejected — stays in `territory-ia` MCP, guarded by `caller_agent`.
>
> **Hierarchy rules:** `ia/rules/project-hierarchy.md` (step > stage > phase > task). `ia/rules/orchestrator-vs-spec.md` (this doc = orchestrator, never closeable).
>
> **Read first if landing cold:**
> - `docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md` — full audit + design expansion + examples + review notes.
> - `docs/mcp-ia-server.md` — current MCP tool catalog (pre-reshape).
> - `tools/mcp-ia-server/src/tools/` — 22 existing handler files.
> - `ia/rules/project-hierarchy.md` + `ia/rules/orchestrator-vs-spec.md` — doc semantics + phase/task cardinality (≥2 tasks per phase).
> - `ia/rules/invariants.md` — **#12** (specs under `ia/specs/` / orchestrators under `ia/projects/` — mutation tools validate path) + **#13** (monotonic id counter never hand-edited — mutation tools never touch `id:` field).
> - MCP: `backlog_issue {id}` per referenced id once tasks file; never full `BACKLOG.md` read.

---

## Stages

> **Tracking legend:** Step / Stage `Status:` uses enum `Draft | In Review | In Progress — {active child} | Final` (per `ia/rules/project-hierarchy.md`). Phase bullets use `- [ ]` / `- [x]`. Task tables carry a **Status** column: `_pending_` (not filed) → `Draft` → `In Review` → `In Progress` → `Done (archived)`. Markers flipped by lifecycle skills: `stage-file` → task rows gain `Issue` id + `Draft` status; `/kickoff` → `In Review`; `/implement` → `In Progress`; `/closeout` → `Done (archived)` + phase box when last task of phase closes; `project-stage-close` → stage `Final` + stage-level rollup.

---

### Stage 1 — Quick Wins / Glossary Bulk-Terms Extension

**Status:** Done (2026-04-18)

**Objectives:** Extend `glossary-lookup.ts` to accept a `terms: string[]` array alongside the existing `term: string` param; return per-term `{ results, errors }` partial-result shape. Back-compat: single `term` param still works unchanged.

**Exit:**

- `glossary_lookup({ terms: ["HeightMap", "wet run", "nonexistent"] })` returns `ok: true`, `payload.results` for found terms, `payload.errors` for not-found, `meta.partial` counts.
- `glossary_lookup({ term: "HeightMap" })` (single term) still returns existing shape unwrapped.
- Tests green; `npm run validate:all` passes.
- Phase 1 — Bulk-terms handler + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T1.1 | Bulk terms handler | **TECH-314** | Done | Extend `tools/mcp-ia-server/src/tools/glossary-lookup.ts` to accept `terms?: string[]` alongside `term?: string`. When `terms` present, fan out to per-term lookup, aggregate into `{ results: {[term]: GlossaryEntry}, errors: {[term]: { code, message }} }` + `meta.partial: { succeeded, failed }`. Single-`term` path returns existing shape via backward-compat branch. |
| T1.2 | Bulk terms tests | **TECH-315** | Done | Unit tests in `tools/mcp-ia-server/tests/tools/glossary-lookup.test.ts`: bulk happy path (all found), partial failure (one term not found → in `errors`, rest in `results`), single-`term` back-compat, empty `terms: []` → `{ results: {}, errors: {}, meta.partial: {succeeded:0,failed:0} }`. |

---

### Stage 2 — Quick Wins / Structured Invariants Summary

**Status:** Final (2026-04-18)

**Backlog state (Stage 1.2):** 4 filed, all Done (archived) — TECH-371 / TECH-372 / TECH-373 / TECH-374

**Objectives:** Extend `invariants-summary.ts` to return a structured per-invariant array with `subsystem_tags` and an optional `domain` filter. Author `invariants-tags.json` sidecar mapping each invariant number to its subsystem tags. Ship as `v0.6.0`.

**Exit:**

- `invariants_summary({ domain: "roads" })` returns only road-tagged invariants in structured form.
- `invariants_summary({})` returns all 13 invariants structured + `markdown` side-channel.
- `tools/mcp-ia-server/data/invariants-tags.json` committed with all 13 invariants + guardrail tags.
- `tools/mcp-ia-server/package.json` at `0.6.0`; `CHANGELOG.md` entry present.
- Tests green.
- Phase 1 — Sidecar + handler extension.
- Phase 2 — Tests + release prep.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T2.1 | Invariants-tags sidecar | **TECH-371** | Done (archived) | Author `tools/mcp-ia-server/data/invariants-tags.json` — array of `{ number: N, subsystem_tags: string[] }` for all 13 invariants + Guardrails rows (derive tags from `ia/rules/invariants.md` prose: HeightMap/Cell/roads/water/cliff/urbanization mentions). |
| T2.2 | Structured invariants handler | **TECH-372** | Done (archived) | Extend `tools/mcp-ia-server/src/tools/invariants-summary.ts` to load `invariants-tags.json`; accept `domain?: string` filter param (substring match against `subsystem_tags`); return `{ invariants: [{number, title, body, subsystem_tags, code_touches}], markdown?: string }`. `markdown` preserves existing prose for agents that still prefer text rendering. |
| T2.3 | Invariants tests | **TECH-373** | Done (archived) | Unit tests in `tools/mcp-ia-server/tests/tools/invariants-summary.test.ts`: `domain` filter match; `domain` matches nothing → `{ invariants: [], markdown: "" }` (not error); no `domain` → all 13 returned; `markdown` side-channel populated regardless of filter. |
| T2.4 | Release prep v0.6.0 | **TECH-374** | Done (archived) | Bump `tools/mcp-ia-server/package.json` `version` to `0.6.0`; add `CHANGELOG.md` entry: `v0.6.0 — Quick wins: glossary bulk-terms + structured invariants`. Advisory note: "tag this commit `mcp-pre-envelope-v0.5.0` for P2 rollback target". |

---

### Stage 3 — Envelope Foundation (Breaking Cut) / Envelope Infrastructure + Auth

**Status:** Final (2026-04-18)

**Objectives:** Author the `ToolEnvelope<T>` type + `wrapTool()` middleware + `ErrorCode` enum that all 32 handlers will use in Stage 2.2. Author `caller-allowlist.ts` with per-tool map. Both files are the foundation for all remaining stages and steps.

**Exit:**

- `tools/mcp-ia-server/src/envelope.ts` exports `ToolEnvelope<T>`, `EnvelopeMeta`, `ErrorCode`, `wrapTool(handler)`.
- `tools/mcp-ia-server/src/auth/caller-allowlist.ts` exports `checkCaller(tool, caller_agent)` returning `true` or throwing `unauthorized_caller`.
- Unit tests green for both files.
- Phase 1 — Core types + middleware authoring.
- Phase 2 — Unit tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T3.1 | Envelope middleware | **TECH-388** | Done (archived) | Author `tools/mcp-ia-server/src/envelope.ts`: `ToolEnvelope<T>` discriminated union (`ok: true, payload: T, meta?` / `ok: false, error: {code, message, hint?, details?}`); `EnvelopeMeta` with `graph_generated_at?`, `graph_stale?`, `partial?: {succeeded, failed}`; `ErrorCode` enum (12 values from §3.1); `wrapTool<I,O>(handler: (input:I)=>Promise<O>)` that catches throws + converts to error envelope. |
| T3.2 | Caller allowlist | **TECH-389** | Done (archived) | Author `tools/mcp-ia-server/src/auth/caller-allowlist.ts`: per-tool allowlist map `Record<string, string[]>` covering all mutation + authorship tools from Steps 3–4 (pre-populate with known callers per §3.8); export `checkCaller(tool: string, caller_agent: string | undefined): void` — throws `{ code: "unauthorized_caller", message, hint }` if caller not in allowlist or allowlist missing `caller_agent`. |
| T3.3 | Envelope unit tests | **TECH-390** | Done (archived) | Tests in `tools/mcp-ia-server/tests/envelope.test.ts`: `wrapTool` happy path (`ok: true, payload`); envelope passthrough (no double-wrap); bare `Error` → `internal_error` (per TECH-388 Decision Log); typed throw `{code: "db_unconfigured", hint?, details?}` preserves code + optional fields; `meta` passthrough. |
| T3.4 | Allowlist unit tests | **TECH-391** | Done (archived) | Tests for `checkCaller`: authorized caller → no throw; unauthorized caller → `unauthorized_caller`; `caller_agent` undefined → `unauthorized_caller`; tool not in map (read-only) → no throw (allowlist only gates mutation/authorship tools; read tools bypass). |

---

### Stage 4 — Envelope Foundation (Breaking Cut) / Rewrite 32 Tool Handlers

**Status:** Done

**Backlog state (Stage 2.2):** 8 filed — 8 Done (archived) TECH-398, TECH-399, TECH-400, TECH-401, TECH-402, TECH-403, TECH-404, TECH-405

**Objectives:** Wrap all 32 tool handlers in `wrapTool()`; convert all error paths to typed `ErrorCode` values; add `payload.meta` to `spec_section` response. Handlers split by family across 4 phases for reviewability.

**Exit:**

- All 22 handler files use `wrapTool`; no bare `return { content: [...] }` at top level.
- `spec_section` response includes `payload.meta: { section_id, line_range, truncated, total_chars }`.
- `unity_bridge_command` timeout path includes `error.details: { command_id, last_output_preview }`.
- `db_unconfigured` returns `{ ok: false, error: { code: "db_unconfigured", ... } }` across all DB tools.
- Existing passing tests still pass after snapshot regen.
- Phase 1 — Read + spec + rule tools.
- Phase 2 — Glossary + invariant tools.
- Phase 3 — Backlog + DB-coupled tools.
- Phase 4 — Bridge + Unity analysis tools.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T4.1 | Wrap spec tools | **TECH-398** | Done (archived) | Wrap `list-specs.ts`, `spec-outline.ts`, `spec-section.ts`, `spec-sections.ts` in `wrapTool`; add `payload.meta: { section_id, line_range, truncated, total_chars }` to `spec-section` success response; convert `spec_not_found` / `section_not_found` to typed `ErrorCode`. |
| T4.2 | Wrap rule + router tools | **TECH-399** | Done (archived) | Wrap `list-rules.ts`, `rule-content.ts`, `router-for-task.ts` in `wrapTool`; add new `rule_section` tool (symmetric to `spec_section`, canonical params `{ rule, section, max_chars }`) in `rule-content.ts` alongside existing `rule_content`; register in MCP server index. |
| T4.3 | Wrap glossary tools | **TECH-400** | Done (archived) | Wrap `glossary-discover.ts`, `glossary-lookup.ts` (including bulk-`terms` path from Stage 1.1) in `wrapTool`; ensure `meta.graph_generated_at` + `meta.graph_stale` preview fields flow through envelope `meta` (full freshness logic in Stage 3.3). |
| T4.4 | Wrap invariant tools | **TECH-401** | Done (archived) | Wrap `invariants-summary.ts` (structured response from Stage 1.2) and `invariant-preflight.ts` in `wrapTool`; convert hardcoded section-cap constants to `INVARIANT_PREFLIGHT_MAX_SECTIONS` / `INVARIANT_PREFLIGHT_MAX_CHARS` env vars with existing defaults. |
| T4.5 | Wrap backlog tools | **TECH-402** | Done (archived) | Wrap `backlog-issue.ts`, `backlog-search.ts` in `wrapTool`; `issue_not_found` → `{ ok: false, error: { code: "issue_not_found", hint: "Check ia/backlog/ and ia/backlog-archive/" } }`. |
| T4.6 | Wrap DB-coupled tools | **TECH-403** | Done (archived) | Wrap `city-metrics-query.ts`, `project-spec-closeout-digest.ts`, `project-spec-journal.ts` (all 4 journal ops) in `wrapTool`; `db_unconfigured` branch → `{ ok: false, error: { code: "db_unconfigured", hint: "Start Postgres on :5434" } }` uniformly across all four. |
| T4.7 | Wrap bridge tools | **TECH-404** | Done (archived) | Wrap `unity-bridge-command.ts`, `unity-bridge-lease.ts`, `unity-bridge-get.ts` (via `unity_bridge_get` / `unity_compile` kinds), `unity-compile.ts` in `wrapTool`; timeout path: inject `error.details = { command_id, last_output_preview }` before wrapping; `db_unconfigured` → `{ ok: false, error: { code: "db_unconfigured" } }`. |
| T4.8 | Wrap Unity analysis tools | **TECH-405** | Done (archived) | Wrap `findobjectoftype-scan.ts`, `unity-callers-of.ts`, `unity-subscribers-of.ts`, `csharp-class-summary.ts` in `wrapTool`; no-results path returns `ok: true, payload: { matches: [] }` (not error); parse failure → `ok: false, error: { code: "invalid_input" }`. |

---

### Stage 5 — Envelope Foundation (Breaking Cut) / Alias Removal + Structured Prose + Batch Shape

**Status:** Final (2026-04-18)

**Backlog state (Stage 2.3):** 4 filed, all Done (archived) — TECH-426 / TECH-427 / TECH-428 / TECH-429

**Objectives:** Hard-remove all legacy param aliases from `spec_section`, `spec_sections`, `project_spec_journal_*`; convert `rule_content` to structured payload + `markdown` side-channel; implement partial-result batch schema for `spec_sections` and `glossary_lookup (terms)`.

**Exit:**

- `spec_section({ section_heading: "..." })` → `{ ok: false, error: { code: "invalid_input", message: "Unknown param 'section_heading'. Canonical: 'section'." } }`.
- `spec_sections` returns `{ results: {[key]: ...}, errors: {[key]: ...}, meta.partial }` — one bad key does not fail whole batch.
- `rule_content` returns `{ rule_key, title, sections: [{id, heading, body}], markdown?: string }`.
- `glossary_lookup({ terms: [...] })` returns partial-result shape (from Stage 1.1, now wrapped).
- Phase 1 — Alias removal + structured rule_content.
- Phase 2 — Partial-result batch shape.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T5.1 | Drop spec_section aliases | **TECH-426** | Done (archived) | Remove alias params from `spec-section.ts` Zod schema: `key`/`doc`/`document_key` → reject with `invalid_input` (hint: "Use 'spec'"); `section_heading`/`section_id`/`heading` → reject (hint: "Use 'section'"); `maxChars` → reject (hint: "Use 'max_chars'"). Same cleanup for `spec-sections.ts` and `project-spec-journal.ts` journal-search params. |
| T5.2 | Structured rule_content | **TECH-427** | Done (archived) | Convert `rule-content.ts` response to `{ rule_key, title, sections: [{id, heading, body}], markdown?: string }` — parse headings from rule markdown; `markdown` side-channel = raw file text. Ensures `rule_section` tool (T2.2.2) has a structured base to slice. |
| T5.3 | Batch partial-result — spec_sections | **TECH-428** | Done (archived) | Refactor `spec-sections.ts` to return `{ results: {[spec_key]: {sections: [...]}}, errors: {[spec_key]: {code, message}}, meta: {partial: {succeeded, failed}} }`. One bad input key → `errors[key]`, rest still succeed; envelope `ok: true` when ≥1 succeeds. |
| T5.4 | Batch partial-result — glossary_lookup | **TECH-429** | Done (archived) | Wire partial-result shape for `glossary_lookup({ terms: [...] })` (handler extended in Stage 1.1) through the Stage 2.2 envelope wrapper; ensure `meta.partial` propagates to `EnvelopeMeta`; single-`term` path still returns unwrapped `GlossaryEntry` in `payload`. |

---

### Stage 6 — Envelope Foundation (Breaking Cut) / Caller Sweep + Snapshot Tests + CI Gate

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Sweep all lifecycle skill bodies, agent bodies, and docs for legacy param aliases and bare tool-recipe sequences; author snapshot test fixtures for all 32 tools; add `validate:mcp-envelope-shape` CI script; bump to v1.0.0 with rollback note.

**Exit:**

- `npm run validate:mcp-envelope-shape` exits 0 (no bare non-envelope returns in `src/tools/*.ts`).
- `tools/mcp-ia-server/tests/envelope.test.ts` snapshots exist for all 32 tools.
- All `ia/skills/**/SKILL.md` tool-recipe sections reference canonical param names; no `section_heading`/`key`/`doc`/`maxChars` in any skill/agent/doc.
- `docs/mcp-ia-server.md` updated with alias-drop migration note + new tools from Stage 2.2 (`rule_section`).
- `tools/mcp-ia-server/package.json` at `1.0.0`.
- Phase 1 — Snapshot tests + caller sweep.
- Phase 2 — CI gate + release prep.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T6.1 | Snapshot tests | _pending_ | _pending_ | Author `tools/mcp-ia-server/tests/envelope.test.ts` — one `ok: true` + one `ok: false` fixture per tool (input → output JSON); cover alias-rejection responses, `db_unconfigured`, partial-batch shape. Run `npm run validate:all` post-regen to confirm no regressions. |
| T6.2 | Caller sweep | _pending_ | _pending_ | Grep `\b(spec_section\ | spec_sections\ | router_for_task\ | invariants_summary\ | glossary_lookup\ | glossary_discover)\b` across `ia/skills/**/SKILL.md`, `.claude/agents/**/*.md`, `ia/rules/**/*.md`, `docs/**/*.md`, `CLAUDE.md`, `AGENTS.md`; replace legacy aliases + bare patterns with canonical params + envelope-aware call patterns; update 8+ lifecycle skill tool-recipe sections to note composite first (Step 3). |
| T6.3 | CI envelope-shape script | _pending_ | _pending_ | Author `tools/scripts/validate-mcp-envelope-shape.mjs` — greps `tools/mcp-ia-server/src/tools/*.ts` for function bodies that `return {` without `wrapTool`; exits non-zero if found. Add `"validate:mcp-envelope-shape"` to root `package.json` scripts + add to `validate:all` composition. |
| T6.4 | Release prep v1.0.0 | _pending_ | _pending_ | Bump `tools/mcp-ia-server/package.json` to `1.0.0`; add `CHANGELOG.md` entry `v1.0.0 — Breaking: unified ToolEnvelope, alias removal, structured prose tools, partial-result batch`; include migration table (alias → canonical); note rollback path (`git revert <merge-sha>`) and pre-envelope tag `mcp-pre-envelope-v0.5.0`. |

---

### Stage 7 — Composite Bundles + Graph Freshness / `issue_context_bundle` + `lifecycle_stage_context`

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement the two most-used composite tools. `issue_context_bundle` fans out to 5 sub-fetches and aggregates under one envelope call. `lifecycle_stage_context` wraps `issue_context_bundle` with a stage-specific extras dispatch map.

**Exit:**

- `issue_context_bundle({ issue_id: "TECH-301" })` returns full bundle per §7.2 example.
- `depends_on` references archived issue → resolves from `ia/backlog-archive/`.
- Journal `db_unconfigured` → `ok: true`, `recent_journal: []`, `meta.partial.failed: 1`.
- `lifecycle_stage_context` all 4 stages return enriched bundles; partial failures propagate.
- Tests green.
- Phase 1 — `issue_context_bundle` implementation + tests.
- Phase 2 — `lifecycle_stage_context` implementation + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T7.1 | issue_context_bundle | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/issue-context-bundle.ts`: core sub-fetch = `backlog_issue` (fail → `ok: false`); optional = `router_for_task` + `spec_section × N` + `invariants_summary` + `project_spec_journal_search` (each fail → `meta.partial.failed++`). Search both `ia/backlog/` and `ia/backlog-archive/` for `depends_on` chain. Return `{ issue, depends_chain, routed_specs, invariant_guardrails, recent_journal }` under `wrapTool`. |
| T7.2 | issue_context_bundle tests | _pending_ | _pending_ | Tests: happy path (all 5 sub-fetches succeed); `depends_on` references archived issue → resolved; `project_spec_journal_search` unconfigured (`db_unconfigured`) → `ok: true`, `recent_journal: []`, `meta.partial: {succeeded:4, failed:1}`; `backlog_issue` not found → `ok: false, error.code = "issue_not_found"`. |
| T7.3 | lifecycle_stage_context | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/lifecycle-stage-context.ts`: `stage ∈ {kickoff, implement, verify, close}` → stage map dispatches `issue_context_bundle` + stage-specific extras: `kickoff` adds glossary anchors; `implement` adds per-phase domain prep + invariants; `verify` adds bridge preflight hints; `close` adds closeout digest + journal search. `meta.partial` aggregates across all sub-fetches. |
| T7.4 | lifecycle_stage_context tests | _pending_ | _pending_ | Tests: all 4 stage values return enriched bundles; unknown `stage` value → `{ ok: false, error: { code: "invalid_input" } }`; optional stage-extra sub-fetch failure → `ok: true`, `meta.partial.failed++`; `stage: "close"` + db unconfigured → graceful degradation on journal + digest. |

---

### Stage 8 — Composite Bundles + Graph Freshness / `orchestrator_snapshot`

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement a Markdown parser for master-plan task tables + status pointers, and the `orchestrator_snapshot` tool that surfaces current orchestrator state in one call. Replaces the Glob + Grep + Read chain agents currently use to inspect master plans.

**Exit:**

- `orchestrator_snapshot({ slug: "mcp-lifecycle-tools-opus-4-7-audit" })` returns `{ status_pointer, stages: [{id, title, phases, tasks}], rollout_tracker_row? }`.
- Slug pointing outside `ia/projects/` → `{ ok: false, error: { code: "invalid_input" } }` (invariant #12).
- Rollout-tracker sibling absent → `ok: true`, `rollout_tracker_row: null`.
- `- [ ]` / `- [x]` phase checkboxes parsed; task rows with `_pending_` preserved.
- Tests green.
- Phase 1 — Parser + snapshot tool.
- Phase 2 — Tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T8.1 | Orchestrator parser | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/parser/orchestrator-parser.ts`: parses `ia/projects/*master-plan*.md` → `{ status_pointer: string, stages: [{id, title, status, phases: [{label, checked}], tasks: [{id, name, phase, issue, status, intent}]}] }`. Validates file path starts with `ia/projects/` (invariant #12 guard). Parse task-table rows: pipe-separated markdown table, extract Issue + Status columns. |
| T8.2 | orchestrator_snapshot tool | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/orchestrator-snapshot.ts` via `wrapTool`: resolve `ia/projects/{slug}-master-plan.md`, call parser (T3.2.1); Glob `ia/projects/{slug}-*rollout-tracker.md` for optional sibling; if found parse rollout-tracker row into `rollout_tracker_row?`; return full snapshot under envelope; rollout absent → `rollout_tracker_row: null`, `meta.partial` unchanged. |
| T8.3 | Snapshot tool tests | _pending_ | _pending_ | Tests for `orchestrator_snapshot`: multi-stage master-plan with mixed `_pending_`/`Draft`/`Done` task rows parsed; file outside `ia/projects/` → `invalid_input`; rollout sibling absent → `ok: true`, `rollout_tracker_row: null`; slug not found → `issue_not_found`. |
| T8.4 | Parser unit tests | _pending_ | _pending_ | Tests for `orchestrator-parser.ts`: partial stage table (some `_pending_`) → `_pending_` preserved in output; phase checkbox `- [ ]` → `checked: false`, `- [x]` → `checked: true`; task row without Issue id → `issue: "_pending_"`; status pointer regex: `**Status:** In Progress — Stage 1.1` → `{ pointer: "In Progress — Stage 1.1" }`. |

---

### Stage 9 — Composite Bundles + Graph Freshness / Graph Freshness + Skill Recipe Sweep

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Wire real freshness metadata into `glossary_lookup` / `glossary_discover` responses; add `refresh_graph` non-blocking regen trigger. Sweep lifecycle skill bodies and agent docs to call composite bundle tools first, with bash fallback for MCP-unavailable path.

**Exit:**

- `glossary_lookup` response includes `meta.graph_generated_at` (ISO from `glossary-graph-index.json` mtime) + `meta.graph_stale` (true when > `GLOSSARY_GRAPH_STALE_DAYS` days, default 14).
- `refresh_graph: true` spawns regen child process; response returns without waiting.
- All 8+ lifecycle skill tool-recipe sections updated; subagent bodies + `docs/mcp-ia-server.md` catalog updated with all 3 composite tools.
- `npm run validate:all` passes.
- Phase 1 — Graph freshness metadata.
- Phase 2 — Skill recipe + docs sweep.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T9.1 | Graph freshness handler | _pending_ | _pending_ | Extend `glossary-lookup.ts` + `glossary-discover.ts`: read `fs.stat("tools/mcp-ia-server/data/glossary-graph-index.json").mtime`; compute `graph_stale = mtime < Date.now() - (GLOSSARY_GRAPH_STALE_DAYS * 86400000)` (env default 14); attach to `EnvelopeMeta`. `refresh_graph?: boolean` input: if `true`, spawn `npm run build:glossary-graph` as detached child process (`child_process.spawn(..., { detached: true, stdio: "ignore" }).unref()`), return immediately. |
| T9.2 | Freshness tests | _pending_ | _pending_ | Tests: mock `fs.stat` mtime = now - 15d → `graph_stale: true`; mtime = now - 1d → `graph_stale: false`; `GLOSSARY_GRAPH_STALE_DAYS=1` env override → stale threshold respected; `refresh_graph: true` → child process spawned without blocking (spy on `child_process.spawn`); `graph_generated_at` ISO format valid. |
| T9.3 | Skill recipe sweep | _pending_ | _pending_ | Update `ia/skills/design-explore/SKILL.md`, `ia/skills/master-plan-new/SKILL.md`, `ia/skills/stage-file/SKILL.md`, `ia/skills/project-spec-kickoff/SKILL.md`, `ia/skills/project-spec-implement/SKILL.md`, `ia/skills/project-spec-close/SKILL.md`, `ia/skills/release-rollout/SKILL.md`, `ia/skills/closeout/SKILL.md` — replace 3–8 call opening sequence with `lifecycle_stage_context(issue_id, stage)` or `issue_context_bundle(issue_id)` as first call; add bash-fallback note for MCP-unavailable. |
| T9.4 | Agent + docs catalog update | _pending_ | _pending_ | Update `.claude/agents/*.md` subagent bodies referencing old sequential recipe (same grep pattern as T2.4.2); update `docs/mcp-ia-server.md` MCP catalog: add `issue_context_bundle`, `lifecycle_stage_context`, `orchestrator_snapshot`, `rule_section` tool entries; mark `glossary_lookup` bulk-terms + freshness metadata; mark `spec_section` alias-drop migration. |

---

### Stage 10 — Mutations + Authorship + Bridge + Journal Lifecycle / Orchestrator + Rollout Mutations

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement two mutation tools that replace fragile regex-based `Edit` calls in lifecycle skills: `orchestrator_task_update` for task-table + phase-checkbox + status-pointer edits, and `rollout_tracker_flip` for rollout lifecycle cell advances.

**Exit:**

- `orchestrator_task_update({ slug, issue_id: "TECH-301", patch: { status: "Draft" }, caller_agent: "stage-file" })` flips task-table row; writes back atomically.
- `rollout_tracker_flip` advances cell; preserves glyph vocabulary exactly.
- Unauthorized caller → `unauthorized_caller` from `checkCaller`.
- File outside `ia/projects/` → `invalid_input` (invariant #12).
- Tests green.
- Phase 1 — Mutation tool authoring.
- Phase 2 — Tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T10.1 | orchestrator_task_update | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/orchestrator-task-update.ts` via `wrapTool` + `checkCaller`: resolve `ia/projects/{slug}-master-plan.md` (validate path per invariant #12); load via orchestrator-parser; apply `patch` — `status` flips task-table Status cell; `phase_checkbox` toggles `- [ ]`/`- [x]`; `top_status_pointer` rewrites `**Status:**` header line; write back via atomic temp-file swap. Never touch `id:` field. |
| T10.2 | rollout_tracker_flip | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/rollout-tracker-flip.ts` via `wrapTool` + `checkCaller` (allowlist: `release-rollout-track`, `release-rollout`): resolve `ia/projects/{slug}-rollout-tracker.md`; find row by `row` slug; find column by `cell` label `(a)`–`(g)`; replace value; preserve glyph vocabulary `❓`/`⚠️`/`🟢`/`✅`/`🚀`/`—` — validate `value` is one of these glyphs or raises `invalid_input`. |
| T10.3 | orchestrator mutation tests | _pending_ | _pending_ | Tests for `orchestrator_task_update`: status flip `_pending_ → Draft` in task table; phase checkbox toggle; top-status-pointer rewrite; unauthorized caller → `unauthorized_caller`; file outside `ia/projects/` → `invalid_input`; issue_id not found in table → `invalid_input`; no `id:` field mutation. |
| T10.4 | rollout flip tests | _pending_ | _pending_ | Tests for `rollout_tracker_flip`: cell advance happy path with snapshot of written markdown; glyph-preservation: invalid glyph → `invalid_input`; valid glyph set passes; unauthorized caller → `unauthorized_caller`; cell label not found in row → `invalid_input`; row slug not found in tracker → `invalid_input`. |

---

### Stage 11 — Mutations + Authorship + Bridge + Journal Lifecycle / IA Authorship Tools

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement 4 IA-authorship tools — `glossary_row_create`, `glossary_row_update`, `spec_section_append`, `rule_create` — with cross-ref validation and `caller_agent` gating. All four trigger non-blocking index regen after successful write.

**Exit:**

- `glossary_row_create({ caller_agent: "spec-kickoff", row: {...} })` appends to correct category bucket in `ia/specs/glossary.md`; triggers `npm run build:glossary-index` regen non-blocking.
- Duplicate term (case-insensitive) → `invalid_input`.
- `spec_reference` pointing to non-existent spec → `invalid_input` (hint: nearest spec name).
- `spec_section_append` validates heading uniqueness via `spec_outline`.
- `rule_create` validates filename uniqueness.
- Tests green for all 4 tools including `unauthorized_caller` paths.
- Phase 1 — Glossary authorship tools.
- Phase 2 — Spec + rule authorship tools.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T11.1 | glossary_row_create | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/glossary-row-create.ts` via `wrapTool` + `checkCaller`: validate `spec_reference` → call `list_specs` to confirm spec exists; check duplicate term (case-insensitive) against glossary index; append row to correct `## {Category}` bucket in `ia/specs/glossary.md`; spawn non-blocking `npm run build:glossary-index`; return `{ term, inserted_at, graph_regen_triggered: true }`. |
| T11.2 | glossary_row_update | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/glossary-row-update.ts` via `wrapTool` + `checkCaller`: fuzzy-then-exact term match against glossary index; apply `patch` fields (`definition`, `spec_reference`, `category`); write back; spawn non-blocking regen; term not found → `{ ok: false, error: { code: "issue_not_found", hint: "Use glossary_row_create." } }`. |
| T11.3 | spec_section_append | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/spec-section-append.ts` via `wrapTool` + `checkCaller`: validate `spec` exists via `list_specs`; call `spec_outline` to check heading uniqueness (duplicate heading → `invalid_input`); append new section markdown to bottom of spec file; spawn non-blocking `npm run build:spec-index`; return `{ spec, heading, appended_at }`. |
| T11.4 | rule_create + authorship tests | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/rule-create.ts` via `wrapTool` + `checkCaller`: validate `path` under `ia/rules/`; check file uniqueness; write file with required frontmatter; return `{ path, created_at }`. Tests for all 4 authorship tools: happy paths; unauthorized caller → `unauthorized_caller`; cross-ref validation failure → `invalid_input` with nearest-match hint; duplicate guard. |

---

### Stage 12 — Mutations + Authorship + Bridge + Journal Lifecycle / Bridge Pipeline + Jobs List

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement `unity_bridge_pipeline` hybrid tool (sync ≤30s, auto-async above ceiling) and `unity_bridge_jobs_list` query surface. Wire timeout auto-attach in existing `unity_bridge_command`.

**Exit:**

- `unity_bridge_pipeline([enter_play_mode, get_compilation_status, exit_play_mode])` completes in <30s → `{ results, lease_released: true, elapsed_ms }`.
- Same pipeline >30s → `{ job_id, status: "running", poll_with: "unity_bridge_jobs_list" }`.
- Timeout on kind 2 of 3 → `{ ok: false, error: { code: "timeout", details: { completed_kinds, last_output_preview, command_id } } }`.
- `unity_bridge_jobs_list` queries `agent_bridge_job` table; `db_unconfigured` → graceful envelope error.
- Tests green.
- Phase 1 — Pipeline + jobs-list tools.
- Phase 2 — Timeout auto-attach + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T12.1 | unity_bridge_pipeline | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/unity-bridge-pipeline.ts` via `wrapTool`: accept `commands: CommandKind[]` + optional `caller_agent`; acquire lease internally (calls `unity_bridge_lease` acquire); execute kinds sequentially with `UNITY_BRIDGE_PIPELINE_CEILING_MS` wall-clock budget; on completion ≤ ceiling → release lease, return `{ results, lease_released: true, elapsed_ms }`; on ceiling exceeded → detach to async job, return `{ job_id, status: "running", current_kind, poll_with, lease_held_by: caller_agent }`. |
| T12.2 | unity_bridge_jobs_list | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/unity-bridge-jobs-list.ts` via `wrapTool`: `filter?: { status?, caller_agent?, since? }`; query `agent_bridge_job` Postgres table; `db_unconfigured` → `{ ok: false, error: { code: "db_unconfigured" } }`; return `{ jobs: [{job_id, caller_agent, started_at, status, last_output_preview}] }` filtered by provided params; empty result → `{ jobs: [] }`, `ok: true`. |
| T12.3 | Timeout auto-attach | _pending_ | _pending_ | Extend `unity-bridge-command.ts` timeout error path: before `wrapTool` surfaces the `timeout` error, inject `details: { command_id, last_output_preview, completed_kinds: string[] }` — where `completed_kinds` = list of kinds that completed before timeout; `last_output_preview` = last N chars of bridge job output column. Update snapshot test in `tools/mcp-ia-server/tests/tools/unity-bridge-command.test.ts`. |
| T12.4 | Bridge + jobs tests | _pending_ | _pending_ | Tests for `unity_bridge_pipeline`: sync-complete path (3 mock kinds < 30s ceiling); async-convert path (> 30s ceiling mock → `{ job_id }`); timeout on kind 2 → `error.details.completed_kinds` contains completed kinds only. Tests for `unity_bridge_jobs_list`: filter by `status: "running"`; empty result; `db_unconfigured`. |

---

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
| T13.3 | Closeout digest journaled_sections | _pending_ | _pending_ | Extend `project-spec-closeout-digest.ts`: after computing checklist, query `SELECT DISTINCT kind FROM ia_project_spec_journal WHERE issue_id = $1`; add `journaled_sections: string[]` to `payload`; `db_unconfigured` → `journaled_sections: []`, `meta.partial.failed++`. Update `ia/skills/closeout/SKILL.md` to read `journaled_sections` before calling `journal_entry_sync` (skip if already persisted). |
| T13.4 | Journal lifecycle tests | _pending_ | _pending_ | Tests for `journal_entry_sync`: dedup — same `(issue_id, kind, body)` twice → single DB row; different body same issue → two rows; cascade delete removes all issue rows; migration: second run no-op (idempotent). Tests for `project_spec_closeout_digest.journaled_sections`: populated when journal has prior entries; empty `[]` when db_unconfigured. |

---

### Stage 14 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Master-plan Authoring Tools

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement three authoring tools mirroring the glossary/spec/rule authorship pattern from Stage 4.2, targeted at the highest-churn IA surface (master-plan orchestrators). All three validate cardinality + `caller_agent` gate + path-under-`ia/projects/` guard per invariant #12. Reuses Stage 3.2 orchestrator-parser via a new `serialize()` inverse.

**Exit:**

- `master_plan_create({ slug, metadata, steps, caller_agent: "master-plan-new" })` writes `ia/projects/{slug}-master-plan.md` via orchestrator-parser serializer; rejects if file exists (hint: use `master_plan_step_append`).
- `master_plan_step_append({ slug, step, caller_agent: "master-plan-extend" })` appends new Step to existing orchestrator; preserves existing Steps verbatim (never rewrites).
- `stage_decompose_apply({ slug, step_id, decomposition, caller_agent: "stage-decompose" })` expands one skeleton step into stages → phases → tasks in-place; preserves step header + Objectives + Exit criteria.
- Cardinality validator rejects: `< 2 phases per stage`, `< 2 tasks per phase` (unless `justification` field present), duplicate stage ids, duplicate task ids within stage.
- Unauthorized caller → `unauthorized_caller`.
- Tests green.
- Phase 1 — Serializer + cardinality validator + create/append tools.
- Phase 2 — Stage-decompose tool + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T14.1 | Orchestrator serializer + cardinality validator | _pending_ | _pending_ | Extend `tools/mcp-ia-server/src/parser/orchestrator-parser.ts` with `serialize(snapshot): string` inverse of the Stage 3.2 parser — emits master-plan Markdown (header block, Steps, Stages, phase checkboxes, task tables, orchestration guardrails footer). Author `tools/mcp-ia-server/src/parser/cardinality-validator.ts` — enforces `project-hierarchy` rules: ≥2 phases/stage, ≥2 tasks/phase (allow `justification?: string` override), unique stage ids, unique task ids within stage; returns `{ ok, violations: [{ stage_id, level, message }] }`. |
| T14.2 | master_plan_create + master_plan_step_append | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/master-plan-create.ts` + `master-plan-step-append.ts` via `wrapTool` + `checkCaller`. `create`: path guard (invariant #12 — under `ia/projects/`), file-exists guard, cardinality-validate input, `serialize()` to markdown, atomic temp-file swap write. `step_append`: parse existing orchestrator, validate new step cardinality, splice new Step block before `## Orchestration guardrails` footer, re-`serialize()`, atomic swap — never modify existing Steps prose. |
| T14.3 | stage_decompose_apply | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/stage-decompose-apply.ts` via `wrapTool` + `checkCaller` (allowlist: `stage-decompose`). Parse orchestrator; locate step by `step_id`; detect skeleton marker (missing stages section OR `_pending decomposition_` placeholder); replace with generated stages → phases → tasks structure; preserve step header + Objectives + Exit criteria + Relevant surfaces verbatim; cardinality-validate before write; target step already decomposed → `invalid_input` (hint: edit in place or rerun `stage-decompose` skill). |
| T14.4 | Authoring tool tests | _pending_ | _pending_ | Tests in `tools/mcp-ia-server/tests/tools/master-plan-authoring.test.ts`: happy paths for all three tools; cardinality violations (phase with 1 task, stage with 1 phase, duplicate ids) → `invalid_input` with violation list; path outside `ia/projects/` → `invalid_input`; unauthorized caller → `unauthorized_caller`; `master_plan_create` on existing file → `invalid_input` (hint: `master_plan_step_append`); `stage_decompose_apply` on already-decomposed step → `invalid_input`; `master_plan_step_append` snapshot test confirms existing Steps byte-identical post-append. |

---

### Stage 15 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Transactional Batch

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Implement `mutation_batch` tool wrapping N mutation / authorship calls in an atomic boundary. Uses in-memory snapshot-based rollback: reads all affected files before any write, executes ops sequentially, restores all snapshots on any failure (`all_or_nothing`) or continues past failures with partial-result response (`best_effort`).

**Exit:**

- `mutation_batch({ ops, mode: "all_or_nothing" })` — any op fails → all prior ops' file writes reverted from snapshot; envelope `ok: false, error.code: "batch_aborted", error.details: { failed_op_index, rollback_complete: true }`.
- `mutation_batch({ ops, mode: "best_effort" })` — continues past failures; returns `{ results: {[op_index]: ...}, errors: {[op_index]: ...}, meta.partial }`; envelope `ok: true` when ≥1 succeeds.
- Concurrent `mutation_batch` calls coordinate via `flock` on `tools/.mutation-batch.lock` (distinct lockfile per invariants guardrail).
- Callers updated: `stage-file` wraps per-stage file-batch in `all_or_nothing`; `closeout` wraps yaml-archive-move + BACKLOG regen + spec-delete sequence.
- Phase 1 — Snapshot helper + batch infrastructure.
- Phase 2 — Tests + caller adoption.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T15.1 | File snapshot helper + flock guard | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/mutation/file-snapshot.ts` — `snapshotFiles(paths: string[]): Map<string, Buffer>` reads current content for later restore; `restoreSnapshots(snapshots): void` writes back via atomic temp-file swap per path (preserves mtime semantics). Add `tools/.mutation-batch.lock` sentinel + helper `withBatchLock(fn)` wrapping `flock` for batch lifetime. Batch lockfile distinct from `.id-counter.lock` / `.closeout.lock` / `.materialize-backlog.lock` per invariants guardrail. |
| T15.2 | mutation_batch tool | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/tools/mutation-batch.ts` via `wrapTool` + `checkCaller`. Input: `ops: Array<{ tool: string, args: object }>`, `mode: "all_or_nothing" | "best_effort"`. Wrap entire body in `withBatchLock`. Static-analyze each op to collect affected paths (per-tool dispatch map: `orchestrator_task_update` → `{slug}-master-plan.md`; `glossary_row_create` → `glossary.md` + index; etc.); `snapshotFiles(paths)`; execute ops sequentially dispatching to existing tool handlers; on failure + `all_or_nothing` → `restoreSnapshots` + `batch_aborted` envelope; on failure + `best_effort` → append to `errors`, continue. |
| T15.3 | Atomic + partial batch tests | _pending_ | _pending_ | Tests in `tools/mcp-ia-server/tests/tools/mutation-batch.test.ts`: `all_or_nothing` happy path (3 ops all succeed, all writes persist); mid-batch failure (op 2 of 3 fails) → rollback verified via SHA-256 equality between pre-batch snapshot and post-rollback files; `best_effort` returns `{results: {0:..., 2:...}, errors: {1:...}}` + `meta.partial: {succeeded:2, failed:1}`; flock contention — two concurrent batches serialize deterministically; unauthorized op `caller_agent` → `unauthorized_caller` from inner op (batch still rolls back under `all_or_nothing`). |
| T15.4 | Caller skill adoption | _pending_ | _pending_ | Update `ia/skills/stage-file/SKILL.md` to wrap per-stage file-creation ops (N × `backlog_record_create` + N × `spec_create` + 1 × `orchestrator_task_update` + 1 × BACKLOG regen) in `mutation_batch(mode: "all_or_nothing")` — prevents half-filed stages. Update `ia/skills/project-spec-close/SKILL.md` closeout sequence (yaml archive move + BACKLOG row delete + spec-file delete + orchestrator status flip) to batch. Both skills document bash fallback path when MCP unavailable. |

---

### Stage 16 — Critical Misses: Authoring Parity + Atomicity + Dry-run (post-plan review addendum) / Dry-run Preview

**Status:** Draft (tasks _pending_ — not yet filed)

**Objectives:** Add `dry_run?: boolean` to every mutation + authorship tool (Stages 4.1, 4.2, 5.1, 5.2). When `dry_run: true`, tool returns `payload.diff` (unified diff format) + `affected_paths` per file without writing. Lets `/closeout`, `/release-rollout`, `/stage-file` preview the full migration before committing. `caller_agent` gate still runs first — unauthorized callers still reject without computing the diff.

**Exit:**

- All 8+ mutation / authorship tools accept `dry_run?: boolean` (default `false`).
- Dry-run response: `{ ok: true, payload: { diff: string, affected_paths: string[], would_write: true } }` — no file write, no index regen spawn.
- `mutation_batch({ dry_run: true })` propagates to each nested op; aggregates into `payload.diffs: { [op_index]: { diff, affected_paths } }`.
- Non-dry-run path unchanged; existing Step 4 tests pass without modification.
- Snapshot tests for diff output fixtures per tool.
- Phase 1 — Dry-run helper + wire into orchestrator mutations.
- Phase 2 — Wire into authorship + authoring + batch + tests.

**Tasks:**

| Task | Name | Issue | Status | Intent |
| --- | --- | --- | --- | --- |
| T16.1 | Dry-run helper + orchestrator mutation wiring | _pending_ | _pending_ | Author `tools/mcp-ia-server/src/mutation/dry-run.ts` — `computeDiff(path, newContent): string` unified-diff generator (use `diff` npm package or hand-roll via existing text-diff util). Wire dry-run branch into `orchestrator_task_update` + `rollout_tracker_flip` (Stage 4.1 tools): if `dry_run: true` → compute diff from current file content + proposed write, return `{ diff, affected_paths, would_write: true }` under envelope without calling atomic-swap writer. |
| T16.2 | Dry-run for IA authorship tools | _pending_ | _pending_ | Wire dry-run path into `glossary_row_create`, `glossary_row_update`, `spec_section_append`, `rule_create` (Stage 4.2 tools). Each computes proposed new file content, generates diff, returns without writing or spawning index regen. Multi-file ops (e.g. glossary row insert + graph-index regen) return `diff: string` for primary file only + `affected_paths: [primary, index]` listing both; index regen explicitly marked as side-effect in response `meta.side_effects: ["glossary_index_regen"]`. |
| T16.3 | Dry-run for master-plan authoring + mutation_batch | _pending_ | _pending_ | Wire dry-run path into `master_plan_create`, `master_plan_step_append`, `stage_decompose_apply` (Stage 5.1 tools) — each computes serialized output, diffs against current file (empty string for `master_plan_create` new file), returns without writing. Extend `mutation_batch` (Stage 5.2) to propagate `dry_run: true` into each nested op's args; aggregate per-op diffs into `payload.diffs: { [op_index]: { diff, affected_paths, would_write: true } }`; skip `snapshotFiles` + `restoreSnapshots` entirely when dry-run. |
| T16.4 | Dry-run tests + release prep | _pending_ | _pending_ | Snapshot tests in `tools/mcp-ia-server/tests/mutation/dry-run.test.ts`: fixture input → stable diff string per tool (one `ok: true` fixture per mutation + authorship tool). Behavioral tests: dry-run never writes (compare SHA-256 of affected files before + after call); `dry_run: true` + unauthorized `caller_agent` → still `unauthorized_caller` (auth gate runs before dry-run branch); dry-run via `mutation_batch` returns aggregated `payload.diffs` map. Bump `tools/mcp-ia-server/package.json` to `1.1.0`; `CHANGELOG.md` entry `v1.1.0 — Master-plan authoring (create/step_append/stage_decompose_apply) + mutation_batch (all_or_nothing/best_effort) + dry_run across all mutation/authorship tools`. |

---

## Orchestration guardrails

**Do:**

- Open one stage at a time. Next stage opens only after current stage's `project-stage-close` runs.
- Run `claude-personal "/stage-file ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md Stage 1.1"` to materialize pending tasks → BACKLOG rows + `ia/projects/{ISSUE_ID}.md` stubs.
- Update stage / step `Status` + phase checkboxes as lifecycle skills flip them — do NOT edit by hand.
- Preserve locked decisions (see header block). Changes require explicit re-decision + sync edit to exploration doc.
- **Step 2 breaking cut:** land caller sweep + tool rewrite in the same PR; never split — half-state leaves skills referencing envelope while tools still return legacy shapes.
- **Invariant #12 guard:** all mutation tools (`orchestrator_task_update`, `rollout_tracker_flip`, IA-authorship tools) must validate their target file path before writing. Reject anything outside `ia/projects/` (orchestrators) or `ia/specs/` / `ia/rules/` (authorship).
- **Invariant #13 guard:** mutation tools never touch `id:` fields in YAML backlog records. Never regenerate `ia/state/id-counter.json`.

**Do not:**

- Close this orchestrator via `/closeout` — orchestrators are permanent (see `ia/rules/orchestrator-vs-spec.md`). Only the terminal Step 4 landing triggers `Status: Final`; the file stays.
- Silently promote post-MVP items — out-of-scope items enumerated in §Non-scope of the exploration doc.
- Merge partial stage state — every stage must land on a green bar (`npm run validate:all` passes).
- Insert BACKLOG rows directly into this doc — only `stage-file` materializes them.
- Commit the master plan from the skill — user decides when to commit the new orchestrator.
