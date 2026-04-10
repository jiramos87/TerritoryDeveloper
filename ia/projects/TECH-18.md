---
purpose: "Project spec for TECH-18 — IA migration to PostgreSQL and extended territory-ia MCP."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-18 — IA migration to PostgreSQL and extended territory-ia MCP

> **Issue:** [TECH-18](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — tasks **12–20**, **28–32**, **35**; rows **36–37** deferred here.

## 1. Summary

After **TECH-44b** provides the **PostgreSQL** schema and read surface, migrate **Information Architecture** consumption so **territory-ia** MCP can use **DB-backed** retrieval as the **primary** path (Markdown generated or secondary). Implement additional tools in **phases**: cross-spec search, kickoff checklist, dependency edges, markdown-backed quick tools, domain **topic bundles**, and **`unity_context_section`** over **`.cursor/specs/unity-development-context.md`** (reference spec; **TECH-20** completed — [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md)). **Depends on:** **TECH-44b**; **TECH-17** baseline MCP is archived (same file).

## 2. Goals and Non-Goals

### 2.1 Goals

1. **DB read path** proven for glossary, spec sections, invariants, relationships — aligned with **TECH-44b** tables.
2. New tools registered in `tools/mcp-ia-server/src/index.ts` with **`snake_case`** names; docs in [docs/mcp-ia-server.md](../../docs/mcp-ia-server.md) and package README per project policy.
3. Phased rollout; existing tools keep working until each phase flips.

### 2.2 Non-Goals (Out of Scope)

1. **`findobjectoftype_scan`** MCP tool — use **TECH-26** script as source of truth (**defer**).
2. **`find_symbol`** MCP tool — **defer** unless repo-specific index is justified (IDE overlap).
3. Replacing Unity runtime with DB — IA only.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | I want one search across specs for multi-domain bugs. | `search_specs` (or `ia_search`) returns ranked snippets. |
| 2 | AI agent | I want a checklist before editing roads/water. | `what_do_i_need_to_know` returns specs + invariants pointers. |
| 3 | Developer | I want invariant relationships queryable. | `dependency_chain` returns HeightMap ↔ **cell** height style edges. |

## 4. Current State

### 4.1 Domain behavior

N/A — infrastructure.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| MCP | `tools/mcp-ia-server/` — **TECH-17** baseline |
| DB | **`db/migrations/`** (IA tables + `ia_glossary_row_by_key`), **`tools/postgres-ia/`** (migrate / seed / glossary-by-key), **`DATABASE_URL`** — see [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md) and [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) **PostgreSQL IA (TECH-44b) integration point for TECH-18** |
| Rules | `.cursor/rules/terminology-consistency.mdc` — tool naming |

## 5. Proposed Design

### 5.1 Target behavior (product)

Agents retrieve IA with **lower token cost** and fewer manual `spec_section` chains.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

**Phase A — Core search & graph**

- `search_specs` / `ia_search` — ranked snippets, size-capped responses.
- `what_do_i_need_to_know(task_description)` — structured checklist (router + spec ids + invariant hooks).
- `dependency_chain(term)` — query `relationships` (and glossary links).

**Phase B — Markdown-backed quick tools** (may ship on files before full DB if agreed)

- `backlog_search`, `backlog_by_file`, `architecture_slice` — parse `BACKLOG.md` / `ARCHITECTURE.md`.

**Phase C — Topic bundles**

- `geo_topic_bundle`, `roads_topic_bundle`, `simulation_tick_outline`, `persistence_restore_checklist`, `economy_concepts`, `ui_input_patterns`, `coding_conventions_slice`.
- Optional: `violations_direct_grid_access` — only if not redundant with **TECH-26** CI gate.

**Phase D — Unity context**

- `unity_context_section` — slices of **unity-development-context.md** (file present; implement when this phase is scheduled).

**Phase E — TECH-34 integration**

- Optional MCP tool `gridmanager_region_map` reading generated JSON from **TECH-34**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Defer `find_symbol` / `findobjectoftype_scan` | Avoid duplication | Ship redundant MCP |

## 7. Implementation Plan

### Phase 1 — Wire MCP to **TECH-44b** read API

- [ ] Implement DB client layer; feature-flag or env for DB vs file fallback.
- [ ] Migrate one tool end-to-end (e.g. `glossary_lookup`) as pilot.

### Phase 2 — `search_specs`, `what_do_i_need_to_know`, `dependency_chain`

- [ ] Implement + tests + doc updates.

### Phase 3 — Markdown-backed tools

- [ ] `backlog_search`, `backlog_by_file`, `architecture_slice`.

### Phase 4 — Topic bundles

- [ ] Implement bundles; tune max chars per response.

### Phase 5 — `unity_context_section`

- [ ] Register **unity-development-context** in spec registry; implement section slices.

### Phase 6 — Optional gridmanager MCP

- [ ] Consume **TECH-34** JSON.

## 8. Acceptance Criteria

- [ ] **`npm run verify`** and **`npm test`** green under `tools/mcp-ia-server/` after changes.
- [ ] [docs/mcp-ia-server.md](../../docs/mcp-ia-server.md) tool table matches `registerTool` names.
- [ ] **TECH-18** backlog acceptance paragraph satisfied (multi-spec task with bounded context).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only; MCP response caps and ranking policies belong in **Decision Log** or **§5.2**.
