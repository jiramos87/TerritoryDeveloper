# TECH-62 — Spec pipeline layer B: territory-ia MCP tools and handlers

> **Issue:** [TECH-62](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

**Parent program:** [TECH-60](TECH-60.md) · **Prior layer:** [TECH-61](TECH-61.md) (**layer A**) · **Next layer:** [TECH-63](TECH-63.md) (**layer C**)
**Exploration:** [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md)

## 1. Summary

**Layer B** of [TECH-60](TECH-60.md): extend **territory-ia** MCP with tools and handler improvements that reduce token use and mis-routing: optional **composite** context reads, **router** hints from file paths, filtered **invariant** slices, richer **`backlog_issue`** output, lightweight **project spec** status, and **`spec_section`** depth control. Ship **`npm run test:ia`**, **`npm run verify`**, and **`docs/mcp-ia-server.md`** updates. **Coordinate** **TECH-48** to avoid duplicate discovery tools. **Does not** own root **`validate:all`** only — that is **TECH-61** (**layer A**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Ship **≥1** measurable improvement from [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md) **§3** (prioritize in **Decision Log**).
2. **`snake_case`** tool names; update **`tools/mcp-ia-server/src/index.ts`**, **`docs/mcp-ia-server.md`**, **`tools/mcp-ia-server/README.md`**.
3. Extend **`scripts/verify-mcp.ts`** (and **`node:test`** as needed) for new tools / behaviors.
4. If **glossary** / **reference spec** tool descriptions feed **IA indexes**, run **`npm run generate:ia-indexes -- --check`**.

### 2.2 Non-Goals (Out of Scope)

1. **Postgres**-backed retrieval (**TECH-18**).
2. Root **`npm run`** aggregates without MCP code — **TECH-61**.
3. Rewriting **Cursor Skills** prose — **TECH-63**.

**Candidate tools** (pick and ship subset; document cuts in **Decision Log**):

- **`context_bundle`** — single call combining **`backlog_issue`**, **`invariants_summary`**, **`router_for_task`**, **`spec_section`** slices, **`glossary_discover`** (budgeted **`max_chars`**).
- **`router_for_task`** — optional **`files`** array for path-based domain hints.
- **`invariants_for_files`** or **`invariants_summary`** args — subset by path/domain keywords.
- **`backlog_issue`** — **`depends_on_status`** for cited ids.
- **`project_spec_status`** — **Status** + **Open Questions** excerpt from **`.cursor/projects/{id}.md`**.
- **`spec_section`** — **`include_children`** boolean (default preserves current behavior).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Agent | I want fewer sequential MCP calls at kickoff/implement. | Shipped composite or ranked batch tool with docs. |
| 2 | Maintainer | I want **verify** to fail if tools break. | **`npm run verify`** green after merge. |

## 4. Current State

### 4.1 Domain behavior

N/A.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Server entry | `tools/mcp-ia-server/src/index.ts` |
| Shared parser | `tools/mcp-ia-server/src/parser/project-spec-closeout-parse.ts` |
| **TECH-48** | **BACKLOG** row — discovery from **project spec** prose |
| Umbrella | [TECH-60](TECH-60.md) — **TECH-48** overlap recorded in **Decision Log** |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed here)

Reuse internal functions (**`runSpecSectionExtract`**, **`parseBacklogIssue`**, etc.); avoid duplicating markdown parsers. Version bump **`package.json`** / **`index.ts`** server version when tools ship.

### 5.3 Method / algorithm notes (optional)

Composite tools must enforce **total response size** caps to avoid blowing context windows.

**Handoff to layer C (**TECH-63**):** Final **`snake_case`** tool names and **Tool recipe** order updates live in **Skills** after merge; if **§7b Test Contracts** requires **`project_spec_closeout_digest`** / **`project_spec_closeout-parse`** changes, implement here or document deferral for a follow-up **TECH-**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Layer **B** = MCP only | Matches exploration layering | — |
| 2026-04-04 | **Kickoff** — suggested **MVP** order | Ship **`backlog_issue`** **`depends_on_status`** and/or **`router_for_task`** file hints before large **`context_bundle`** (lower risk, immediate UX) | Big-bang composite only — higher review cost |

## 7. Implementation Plan

### Phase 1 — Design and overlap with TECH-48

- [ ] List tools to ship vs defer; reconcile with **TECH-48** **Notes** in **Decision Log** (single owner per feature where possible).

### Phase 2 — Implementation

- [ ] Implement chosen tool(s) + **Zod** / parser tests.
- [ ] Update **`verify-mcp.ts`** required tool list if new names added.

### Phase 3 — Docs and indexes

- [ ] Patch **`docs/mcp-ia-server.md`** tool table and **`README.md`**.
- [ ] Regenerate **IA indexes** if required.

## 8. Acceptance Criteria

- [ ] **`npm run test:ia`** and **`npm run verify`** green.
- [ ] Tool descriptions in English; terminology matches **glossary** where domain terms appear.
- [ ] At least one **§2.1 Goal** item demonstrably merged (tool or major handler UX win).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Merge **`context_bundle`** scope into **TECH-48** vs implement under **TECH-62** first?
2. **`invariants_for_files`**: static map vs parse **`invariants.mdc`** sections by keyword?
