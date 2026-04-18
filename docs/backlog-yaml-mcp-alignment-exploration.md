# Backlog yaml ↔ MCP tooling alignment — exploration

> **Cross-doc reference:** This doc owns the backlog-side yaml shape, per-record MCP tools (Implementation Points 1–9), and `materialize-backlog.sh` hardening. The unified MCP mutation envelope, `caller_agent` allowlist, composite bundles, and bridge/journal surface are owned by [`docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md`](mcp-lifecycle-tools-opus-4-7-audit-exploration.md) — see its §3.6 for mutation envelope detail that wraps `backlog_record_create` from IP6 below.

Status: locked design — seeds umbrella master plan.
Scope: align MCP territory-ia tools + validator + loader with per-issue yaml backlog refactor.
Out of scope: yaml schema redesign, BACKLOG.md format changes, new section types, migration rollback.

## Context

Backlog refactor split monolithic `BACKLOG.md` + `BACKLOG-ARCHIVE.md` into per-issue yaml records under `ia/backlog/{id}.yaml` (open) and `ia/backlog-archive/{id}.yaml` (closed). Materialize step (`tools/scripts/materialize-backlog.sh` → `.mjs`) regenerates the `.md` views deterministically from yaml + section manifests (`ia/state/backlog-sections.json`, `backlog-archive-sections.json`). ID reservation moved to flock-guarded `tools/scripts/reserve-id.sh` + `ia/state/id-counter.json`. `stage-file` skill now runs N parallel task-file loops, batch-reserves ids once, writes yaml per task, materializes once at end.

MCP parser (`tools/mcp-ia-server/src/parser/backlog-parser.ts`) gained yaml-first guard — if `ia/backlog/` exists, delegates to `backlog-yaml-loader.ts`; else falls back to markdown parse. This works but loses fields + loses soft-dependency markers.

## Problem

Tooling + MCP surface did not fully catch up to yaml refactor. Nine gaps, ranked.

### HIGH priority

1. **`ParsedBacklogIssue` missing `priority`, `related`, `created`** — yaml records carry them, loader drops them. Downstream MCP tools (`backlog_issue`, `backlog_search`) cannot surface priority filters or cross-refs.
2. **`proposed_solution` field dangling** — legacy `ParsedBacklogIssue` exposes it, yaml schema does not emit it, loader cannot set it. Either drop from the type or add to yaml schema + loader.
3. **No MCP `reserve_backlog_ids` tool** — agents shell out to `reserve-id.sh`. Should be a first-class MCP call so subagents + skills call through MCP instead of bash.
4. **No MCP `backlog_list` tool** — agents must call `backlog_search` with empty query to list all; no structured filter on `section` / `priority` / `type` / `status`. Parallel `stage-file` runs need efficient list-by-section + by-status.
5. **No MCP `backlog_record_validate` tool** — agents have no way to lint a yaml body before writing. `tools/validate-backlog-yaml.mjs` runs whole-dir; no per-record path.

### MEDIUM / LOW priority

6. **No MCP `backlog_record_create` tool** — skills write yaml via `Write` tool + bash. First-class MCP wrapper would atomically reserve id + emit yaml + run materialize.
7. **`materialize-backlog.sh` not flock-guarded** — parallel `stage-file` → N writers could race on BACKLOG.md regen. Low risk (atomic write, idempotent regen) but should flock `.backlog.lock` for safety.
8. **`validate-backlog-yaml.mjs` missing cross-checks** — does not verify `related: []` ids exist; does not enforce `depends_on_raw` non-empty when `depends_on: []` is non-empty.
9. **`backlog_search` lacks priority / type / age filters** — only `scope` + `max_results`. Agents filter client-side.

### Correctness issue (inside HIGH #1)

`backlog-yaml-loader.ts` fallback `depends_on_raw = array.join(", ")` loses soft-dep markers (e.g. `FEAT-12 (soft)`, `TECH-7 [optional]`) that `resolveDependsOnStatus` relies on. Loader must preserve the raw source string when yaml carries one.

## Approaches

**Approach selected — single umbrella, fully decomposed.** All 9 items tracked under one orchestrator (`ia/projects/backlog-yaml-mcp-alignment-master-plan.md`). Staged by priority band (HIGH → MEDIUM/LOW). One backlog row per task filed later via `/stage-file`.

Rejected — nine separate BACKLOG rows authored via `/project-new`. Higher overhead, no shared context across tasks, loses the "MCP + validator alignment" framing.

Rejected — ship only HIGH band now, defer MEDIUM/LOW. User asked for fully-staged plan; deferred items still belong in the orchestrator, just in a later Step.

## Design Expansion — backlog-yaml-mcp-alignment

### Implementation Point 1 — extend `ParsedBacklogIssue` + yaml loader with `priority`, `related`, `created`

- **File:** `tools/mcp-ia-server/src/parser/types.ts` (or wherever `ParsedBacklogIssue` is defined) + `tools/mcp-ia-server/src/parser/backlog-yaml-loader.ts`.
- **Change:** add `priority: string | null`, `related: string[]`, `created: string | null` to the shape. Map from yaml in `yamlToIssue`. Keep markdown-path fallback setting them too (`null` / `[]` when not present).
- **Callers:** `backlog-issue.ts`, `backlog-search.ts` — surface the fields in the MCP response payload.
- **Tests:** extend `tools/mcp-ia-server/tests/**/backlog-*.test.ts` with yaml fixture carrying all three fields.
- **Correctness fix (same PR):** replace `depends_on_raw` fallback `array.join(", ")` with: prefer yaml `depends_on_raw` when present; only synthesize from array when source had no raw string. Preserve soft markers.

### Implementation Point 2 — decide `proposed_solution` fate

- **Option A — drop.** Remove from `ParsedBacklogIssue`. Remove all reads. Any consumer expecting it returns `undefined`.
- **Option B — add to yaml schema.** Update `migrate-backlog-to-yaml.mjs buildYaml` to emit the field. Update `validate-backlog-yaml.mjs` schema. Update loader.
- **Decision gate:** run `Grep` across repo for `proposed_solution` reads. Zero consumers → Option A. ≥1 consumer → Option B.
- **File touch (Option A):** loader + types + `backlog-parser.ts`. **(Option B):** same + `migrate-backlog-to-yaml.mjs` + validator + skill docs that reference the field.

### Implementation Point 3 — new MCP tool `reserve_backlog_ids(prefix, count)`

- **File:** `tools/mcp-ia-server/src/tools/reserve-backlog-ids.ts` (new).
- **Wraps:** `tools/scripts/reserve-id.sh {PREFIX} {N}` as child_process spawn with flock already handled by the script.
- **Input schema:** `{ prefix: "TECH"|"FEAT"|"BUG"|"ART"|"AUDIO", count: 1..50 }`.
- **Output:** `{ ids: string[] }` — e.g. `["TECH-295","TECH-296"]`.
- **Registration:** add to `tools/mcp-ia-server/src/index.ts` tool registry.
- **Tests:** snapshot + concurrency test under `tools/mcp-ia-server/tests/tools/reserve-backlog-ids.test.ts`.
- **Skill update:** `ia/skills/stage-file/SKILL.md` + `ia/skills/project-new/SKILL.md` — replace bash invocation guidance with MCP tool when agent has MCP access.

### Implementation Point 4 — new MCP tool `backlog_list`

- **File:** `tools/mcp-ia-server/src/tools/backlog-list.ts` (new).
- **Input:** `{ section?: string, priority?: string, type?: "BUG"|"FEAT"|"TECH"|"ART"|"AUDIO", status?: string, scope?: "open"|"archive"|"all" (default "open") }`.
- **Output:** `{ issues: ParsedBacklogIssue[], total: number }` — ordered by id desc.
- **Impl:** load via `backlog-yaml-loader.parseAllBacklogIssues`, filter in-memory.
- **Registration:** tool registry + index export.
- **Tests:** filter combinations, scope switching, empty result, fixture under `tools/mcp-ia-server/tests/fixtures/`.

### Implementation Point 5 — new MCP tool `backlog_record_validate`

- **File:** `tools/mcp-ia-server/src/tools/backlog-record-validate.ts` (new).
- **Input:** `{ yaml_body: string }` — a single record as yaml string.
- **Output:** `{ ok: boolean, errors: string[], warnings: string[] }`.
- **Impl:** share schema with `tools/validate-backlog-yaml.mjs` — extract lint core into `tools/mcp-ia-server/src/parser/backlog-record-schema.ts` and call it from both.
- **Checks:** required fields, id format, status enum, `depends_on_raw` non-empty when `depends_on: []` non-empty.
- **Tests:** good + bad record fixtures.

### Implementation Point 6 — new MCP tool `backlog_record_create`

## Deferred / superseded by mcp-lifecycle-tools-opus-4-7-audit-exploration §3.6

The `backlog_record_create` tool (atomic reserve → validate → write → materialize) is part of the broader mutation surface owned by [`docs/mcp-lifecycle-tools-opus-4-7-audit-exploration.md`](mcp-lifecycle-tools-opus-4-7-audit-exploration.md) §3.6. Implementation below remains as detail spec; execution sequenced after the unified envelope (Phase P4 of that plan) so the mutation ships inside the `caller_agent`-gated `wrapTool` middleware.

- **File:** `tools/mcp-ia-server/src/tools/backlog-record-create.ts` (new).
- **Input:** `{ prefix, fields: Omit<ParsedBacklogIssue,"id">, caller_agent: string }`.
- **Flow:** reserve id (via shared helper from #3) → validate (via #5) → write `ia/backlog/{id}.yaml` → spawn `tools/scripts/materialize-backlog.sh`.
- **Atomicity:** use tmp-file-then-rename for yaml write; flock-guard the materialize invocation (depends on #7).
- **Output:** `{ id: string, yaml_path: string }`.
- **Tests:** happy path, validation failure path, concurrent-create race.
- **`caller_agent` gate:** allowed callers = `stage-file`, `project-new`, `spec-kickoff` (per lifecycle-tools-audit allowlist).

### Implementation Point 7 — flock-guard `materialize-backlog.sh`

- **File:** `tools/scripts/materialize-backlog.sh`.
- **Change:** wrap the `.mjs` invocation in `flock ia/state/.backlog.lock node tools/scripts/materialize-backlog.mjs …`.
- **Concurrency test:** extend `tools/scripts/test/` with N=8 parallel materialize invocations — assert BACKLOG.md regen is deterministic + no truncation.
- **No schema change.** Pure script hardening.

### Implementation Point 8 — validator cross-checks

- **File:** `tools/validate-backlog-yaml.mjs`.
- **Add checks:**
  - `related: []` ids must exist (in open or archive dir).
  - `depends_on_raw` must be non-empty when `depends_on: []` non-empty.
  - Warning when `depends_on_raw` contains id not present in `depends_on: []` (drift check).
- **Test fixtures:** under `tools/scripts/test-fixtures/` add passing + failing records for each new check.
- **Skill update:** none (validator is already wired into `validate:all`).

### Implementation Point 9 — `backlog_search` filters (priority / type / age)

- **File:** `tools/mcp-ia-server/src/tools/backlog-search.ts`.
- **Add input fields:** `priority?: string, type?: "BUG"|"FEAT"|"TECH"|"ART"|"AUDIO", created_after?: string (ISO date), created_before?: string (ISO date)`.
- **Impl:** apply filters before scoring.
- **Tests:** extend `tools/mcp-ia-server/tests/tools/backlog-search.test.ts`.
- **Depends on #1** — needs `priority` + `created` fields on `ParsedBacklogIssue`.

## Deferred decomposition hints (per step, for stage-decompose)

- **Step 1 (HIGH band, Implementation Points 1-5):** stage by subsystem — Stage 1.1 types/loader (IP1 + IP2 decision), Stage 1.2 MCP tools batch 1 (IP3 + IP4 + IP5), Stage 1.3 skill wiring + docs.
- **Step 2 (MEDIUM / LOW band, Implementation Points 6-9):** stage by risk — Stage 2.1 script hardening (IP7), Stage 2.2 validator extensions (IP8), Stage 2.3 MCP extensions (IP6 + IP9).

## Acceptance (whole umbrella)

- All 9 Implementation Points shipped behind green `validate:all` + `unity:compile-check` (no unity touched here — MCP + tooling only).
- `ia/state/id-counter.json` untouched outside `reserve-id.sh`.
- Per-issue yaml + materialized BACKLOG.md remain byte-identical when only MCP tools change (deterministic regen proven).
- Soft-dep markers preserved end-to-end across yaml round-trip.
- Parallel `stage-file` runs remain race-free after #7.

## Non-goals

- Replace `materialize-backlog.mjs` reconstruction logic.
- Change yaml file layout or section manifest shape.
- Add unity runtime dependencies to MCP tools.
- Migrate to a real yaml library (current minimal parser stays — bug fixes only).

## Next step

`/master-plan-new docs/backlog-yaml-mcp-alignment-exploration.md` — full decomposition to step > stage > phase > task. Tasks seeded `_pending_`. **Do NOT run `/stage-file`** — user dispatches that in a separate agent.
