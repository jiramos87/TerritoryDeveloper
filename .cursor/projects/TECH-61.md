# TECH-61 — Spec pipeline layer A: repo scripts and validation infrastructure

> **Issue:** [TECH-61](../../BACKLOG.md)
> **Status:** In Review
> **Created:** 2026-04-04
> **Last updated:** 2026-04-04

**Parent program:** [TECH-60](TECH-60.md) (**layer A**) · **Next phase:** [TECH-62](TECH-62.md) (**layer B**)
**Exploration:** [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md)

## 1. Summary

**Layer A** of [TECH-60](TECH-60.md): implement **Node** / root **`npm run`** building blocks for the spec pipeline — aggregate validation matching the **IA tools** **Node** job ([`.github/workflows/ia-tools.yml`](../../.github/workflows/ia-tools.yml)), optional **impact** / **diff** / **dependency** helpers, and stubs or first implementations for **invariant** / **golden** checks once **JSON** shapes from **TECH-15** / **TECH-38** / **TECH-31** stabilize. **Does not** add new **territory-ia** **`registerTool`** entries ( **TECH-62** only).

## 2. Goals and Non-Goals

### 2.1 Goals

1. **`validate:all`** (or agreed name) at repo root chaining **`validate:dead-project-specs`** → **`test:ia`** → **`validate:fixtures`** → **`generate:ia-indexes -- --check`** (same order as **project-implementation-validation** / **ia-tools** workflow).
2. Optional scripts (prioritize in **Decision Log**): **`impact:check`** (files × **invariants** heuristic), **`diff:summary`** (git diff grouped by subsystem for an issue id / branch), **`validate:backlog-deps`** (open **Depends on** check for one issue).
3. Optional package **`tools/invariant-checks/`** (or under **`tools/mcp-ia-server`**) with **Node** predicates consuming **grid** **JSON** when schema exists — can ship as **no-op** or **skipped** until fixtures land.
4. Document new commands in root **`package.json`** and, if non-obvious, **`docs/`** or **TECH-60** exploration cross-link.

### 2.2 Non-Goals (Out of Scope)

1. New MCP tool registration — **TECH-62**.
2. Editing **`.cursor/skills/*.md`** bodies beyond a one-line pointer if needed — **TECH-63**.
3. **Unity** **UTF** harness implementation — **TECH-15** / **TECH-16** / **TECH-31**.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want one command to run the same **Node** checks as **CI** after MCP/spec edits. | `validate:all` (or equivalent) documented and exits non-zero on failure. |
| 2 | Maintainer | I want optional scripts to see impact of an issue before closeout. | At least one of **impact** / **diff** / **deps** shipped **or** explicitly deferred in **Decision Log** with reason. |

## 4. Current State

### 4.1 Domain behavior

N/A.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Dead spec scanner | `tools/validate-dead-project-spec-paths.mjs` |
| Root scripts | `package.json` |
| Closeout helpers | `npm run closeout:*` (**TECH-58** **§ Completed**) |
| Validation skill | [`.cursor/skills/project-implementation-validation/SKILL.md`](../skills/project-implementation-validation/SKILL.md) |
| Umbrella | [TECH-60](TECH-60.md) — layer **A**/**B**/**C** boundaries |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed here)

Prefer **TypeScript** under **`tools/mcp-ia-server/scripts/`** or small **`tools/*.mjs`** for consistency with **TECH-50** / **TECH-58**. Share parsing with **`backlog-parser.ts`** when validating deps (**coordinate** **TECH-30**).

**Handoff to layer B (**TECH-62**):** Document the final **`npm run`** names in **`docs/mcp-ia-server.md`** or root **`package.json`** comments so **TECH-63** **Skills** can cite them; any **shared** file-path → domain map for **`router_for_task`** belongs in **TECH-62** (MCP), not duplicated as a second source in **layer A** unless **Decision Log** records a temporary script.

### 5.3 Method / algorithm notes (optional)

**`validate:all`** should mirror [`.github/workflows/ia-tools.yml`](../../.github/workflows/ia-tools.yml) **node** job order: dead project-spec paths (repo root) → **`npm ci`** under **`tools/mcp-ia-server`** → **`npm test`** → **`validate:fixtures`** → **`generate:ia-indexes --check`**.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-04 | Layer **A** = scripts only | Matches **spec-pipeline-exploration** §4 | — |
| 2026-04-04 | Default aggregate script name **`validate:all`** | Matches exploration + **TECH-52** deferred aggregate note; rename in **Decision Log** if maintainers prefer **`validate:ia-full`** | — |

## 7. Implementation Plan

### Phase 1 — Aggregate validation

- [ ] Add **`npm run validate:all`** (or final name from **Decision Log**) to root **`package.json`** chaining IA subset per §5.3.
- [ ] Document target in **TECH-63** (**project-implementation-validation**) and/or **`docs/mcp-ia-server.md`** (**project spec workflows**).

### Phase 2 — Optional helpers

- [ ] Implement **`diff:summary`** and/or **`validate:backlog-deps`** and/or **`impact:check`** per **Decision Log** priority; wire **`npm run`** entries.

### Phase 3 — Invariant / golden stubs

- [ ] If **JSON** contract exists, add **`tools/invariant-checks/`** skeleton + **`npm run test:invariants`** (or defer with **Decision Log**).

## 8. Acceptance Criteria

- [ ] **`npm run validate:all`** (or agreed name) runs and matches **CI** subset documented in **§7 Phase 1**.
- [ ] **`npm run validate:dead-project-specs`** still passes after changes.
- [ ] Deferred optional scripts explicitly recorded in **Decision Log** if not shipped.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| — | … | … | … |

## 10. Lessons Learned

- …

## Open Questions (resolve before / during implementation)

1. Final script names: **`validate:all`** vs **`validate:ia-full`** — align with maintainer preference.
2. Should **`impact:check`** parse **`.cursor/rules/invariants.mdc`** or consume a generated manifest?
