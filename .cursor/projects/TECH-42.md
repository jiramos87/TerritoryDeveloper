# TECH-42 — JSON for future domains: database shapes, API envelopes, streaming

> **Issue:** [TECH-42](../../BACKLOG.md)  
> **Status:** Draft  
> **Created:** 2026-04-03  
> **Last updated:** 2026-04-03

**Parent program:** [TECH-21](TECH-21.md) · **Depends on:** **TECH-41** (completed — [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-41**; soft: **TECH-40** for policy) · **Feeds:** **TECH-19** implementation

## 1. Summary

Capture **architecture patterns** for **future** **Postgres** (**TECH-19**) and HTTP/sync clients without implementing the database: **B1** scalar row + **JSONB** blob; **B3** idempotent **patch** **envelope** (natural keys + **`schema_version`** + body) as a **contract standard**, not a single physical table; **P5** incremental **JSON** reading when **Load pipeline** or exports produce **very large** documents. Link planned product work (**FEAT-47**, **FEAT-48**) to **DTO** evolution. **B2** (**append-only** JSON lines) stays **TECH-43** backlog-only until scheduled.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Written **B1** pattern: queryable columns vs JSONB payload; when to split tables vs one document.
2. Written **B3** pattern: **envelope** shape reusable across endpoints; clarify it is a **message standard**, not “one table only.”
3. **P5** guidance: **Utf8JsonReader**-style streaming, **NDJSON**, or chunked files; when profiling triggers adoption.
4. Cross-reference **TECH-19** milestone tables so column names **do not** collide with **TECH-40** **`artifact`** convention.

### 2.2 Non-Goals

1. Creating **TECH-43** spec or shipping a log sink (separate issue).
2. Implementing **FEAT-47** / **FEAT-48** gameplay.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Backend planner | I want a documented row+blob pattern before migrations. | **B1** doc merged in spec §5. |
| 2 | API designer | I want a standard merge envelope for client patches. | **B3** example + rules merged. |

## 4. Current State

- [`docs/planned-domain-ideas.md`](../../docs/planned-domain-ideas.md) lists **FEAT-46**–**FEAT-48** and **TECH-36** alignment.
- **TECH-41** delivers **current** JSON paths; this issue **documents** **future** consumption.

## 5. Proposed Design

### 5.1 **B1** — Row + **JSONB**

- Example: `city_snapshot` table with `save_slot`, `player_id`, `updated_at`, `schema_version`, `payload jsonb`.
- **Query** filters on scalars; **migrations** evolve `payload` with **`schema_version`** gates.

### 5.2 **B3** — Idempotent upsert **envelope** (standard)

- **Contract** for HTTP or message queue bodies, e.g. `{ "artifact": "city_patch", "schema_version": 1, "natural_key": { ... }, "patch": { ... } }`.
- **Not** tied to one SQL table: same envelope can map to **upsert** stored procedure, **event** outbox, or **sync** worker.

### 5.3 **P5** — Streaming

- Trigger when single-string parse allocates too much (large **G1** exports, hypothetical full-grid JSON).
- Prefer **chunked** files or **NDJSON** over one multi-GB string.

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | **B2** → **TECH-43** only | User requested backlog placeholder without spec |

## 7. Implementation Plan

- [ ] Add §5 content to this spec + link from **TECH-19** spec “Related” if present.
- [ ] Optional one-page `docs/` appendix if **TECH-19** spec should stay shorter.
- [ ] Review **FEAT-47**/**FEAT-48** for **DTO** fields that should appear in **B1**/**B3** examples (illustrative only).

## 8. Acceptance Criteria

- [ ] **B1**, **B3**, **P5** sections complete with **English** examples.
- [ ] **TECH-43** referenced for **B2**.
- [ ] No false claim that **Postgres** is already deployed.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions

- Whether **TECH-19** first milestone should include a **`jsonb`** column or stay normalized-only v1.
