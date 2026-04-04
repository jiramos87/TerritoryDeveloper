# TECH-30 — Validate BACKLOG issue IDs in `.cursor/projects/*.md`

> **Issue:** [TECH-30](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-03

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **9**.

**Spec pipeline program:** **TECH-60** **§ Completed** — **glossary** **territory-ia spec-pipeline program (TECH-60)** lists this issue for **project spec** / **BACKLOG** id hygiene alongside the spec-driven pipeline — [`projects/spec-pipeline-exploration.md`](../../projects/spec-pipeline-exploration.md).

## 1. Summary

Implement a **Node** (or shell) script that scans **`.cursor/projects/*.md`** for references to backlog ids (`BUG-`, `FEAT-`, `TECH-`, `ART-`, `AUDIO-` per `AGENTS.md`) and verifies each id exists in **`BACKLOG.md`** (open or completed sections — configurable). Exit non-zero on orphan references so agents do not follow dead links.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Regex or markdown-aware scan of `**BUG-12**`, `[BUG-12](...)`, `BUG-12.md` front matter, etc.
2. `npm run` or `node tools/validate-project-spec-ids.mjs` documented.
3. Optional: exclude `PROJECT-SPEC-STRUCTURE.md`, templates, completed-only references — **Decision Log**.

### 2.2 Non-Goals (Out of Scope)

1. Validating links to **external** URLs.
2. Parsing full markdown AST unless needed.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | I want CI to catch typos in issue ids. | Script fails on unknown `TECH-99`. |
| 2 | Agent | I want to run one command before commit. | Documented in **TECH-30** backlog. |

## 4. Current State

### 4.1 Domain behavior

N/A.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Files | `.cursor/projects/*.md`, `BACKLOG.md` |
| Dead path hygiene (**TECH-50** completed) | `npm run validate:dead-project-specs` — [`tools/validate-dead-project-spec-paths.mjs`](../../tools/validate-dead-project-spec-paths.mjs). **Coordinate:** share **Node** helpers with **TECH-30** when that script lands. |

### 4.3 Implementation investigation notes (**TECH-50** cross-link)

- **TECH-30** validates that **issue ids** cited **inside** active project specs exist in **BACKLOG.md**.
- **TECH-50** (completed) validates that **paths** under `.cursor/projects/*.md` cited in durable docs (and open **BACKLOG** `Spec:` lines) **exist on disk**. Different scope from **TECH-30**; prefer one shared `tools/` module if implementing **TECH-30** soon.

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Load all ids from `BACKLOG.md` via regex `\*\*(BUG-\d+|TECH-\d+|...)` — tune for backlog format.
- Scan project specs for same patterns; diff.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | Roadmap task 9 | — |

## 7. Implementation Plan

- [ ] Implement script under `tools/`.
- [ ] Add npm script at repo root or document `node` invocation.
- [ ] Optional CI job.

## 8. Acceptance Criteria

- [ ] Script returns 0 on current repo after any one-time fixes.
- [ ] False positive list empty or allowlisted with comment file.
- [ ] **TECH-24** parser regression policy respected if script lives near MCP — share patterns only.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only.
