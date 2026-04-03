# TECH-19 — PostgreSQL first milestone: IA schema and minimal read surface

> **Issue:** [TECH-19](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-02
> **Last updated:** 2026-04-02

**Related tooling:** [docs/agent-tooling-verification-priority-tasks.md](../../docs/agent-tooling-verification-priority-tasks.md) — task **11**; prerequisite for **TECH-18**.

## 1. Summary

Introduce a **game-owned** **PostgreSQL** database whose **first milestone** stores **Information Architecture** rows: glossary terms, spec sections, invariants, and typed **relationships** (e.g. **HeightMap** ↔ **cell** height). Ship **migrations**, optional **seed** from a subset of `.cursor/specs/glossary.md`, and a **minimal programmatic read surface** (views, functions, or thin API) that **TECH-18** MCP can call. **Markdown remains authoritative** for authoring until **TECH-18** migration completes.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Versioned schema (migrations) checked into repo or adjacent infra repo per team choice.
2. Tables align with backlog description: `glossary`, `spec_sections`, `invariants`, `relationships` (names adjustable in Decision Log).
3. Proof: read path used by at least one non-MCP script or one MCP tool pilot.

### 2.2 Non-Goals (Out of Scope)

1. Full spec body ingestion replacing `.md` in milestone 1 (that is **TECH-18** scope).
2. Production game runtime reading city simulation from Postgres.
3. Public internet exposure of DB without auth — document **local/dev** default.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want reproducible schema. | Migrations apply cleanly on empty DB. |
| 2 | AI tooling | I want MCP to query IA without parsing all markdown. | Read API returns glossary row by key. |

## 4. Current State

### 4.1 Domain behavior

N/A — infrastructure.

### 4.2 Systems map

| Area | Pointer |
|------|---------|
| Backlog | `BACKLOG.md` — **TECH-19**, **TECH-18** |
| Sources | `.cursor/specs/glossary.md`, `.cursor/rules/invariants.mdc` |

## 5. Proposed Design

### 5.1 Target behavior (product)

N/A.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

- Choose migration tool (e.g. Flyway, sqitch, raw SQL + npm script).
- Document connection string via env (e.g. `DATABASE_URL`) — never commit secrets.
- Seed: optional first 10–20 glossary rows for pipeline validation.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | Backlog integration | — |

## 7. Implementation Plan

### Phase 1 — Schema

- [ ] Create tables + migrations.
- [ ] Document local setup in `docs/` or `tools/` README.

### Phase 2 — Read surface

- [ ] Expose SQL views or TypeScript/Node client for MCP pilot.

### Phase 3 — Seed

- [ ] Optional glossary seed + validation query.

## 8. Acceptance Criteria

- [ ] Fresh `psql` (or chosen client) can apply migrations and run sample selects.
- [ ] **TECH-18** has a documented integration point (connection, query module).
- [ ] No committed passwords; `.env.example` documents variables only.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

None — tooling only; stack choices in **Decision Log**.
