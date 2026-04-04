# TECH-21 — JSON program (umbrella)

> **Issue:** [TECH-21](../../BACKLOG.md)  
> **Status:** Draft  
> **Created:** 2026-04-02  
> **Last updated:** 2026-04-03

**Phased delivery (separate backlog issues + project specs):** **TECH-40** (**Phase A** — completed; [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-40**), **TECH-41** (**Phase B** — completed; **G1**/**G2**/**G4**, **E3** — durable: **glossary** **Interchange JSON** / **geography_init_params**, **`ARCHITECTURE.md`**, `docs/schemas/`, `StreamingAssets/Config/`, Editor **Reports** menus; closure [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-41**), **TECH-44a** (**Phase C** — completed; [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** — **B1**/**B3**/**P5** patterns in [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md); merged **TECH-44** program for **TECH-44b** **§ Completed** / **TECH-44c** **§ Completed** — umbrella charter closed; trace [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-44** + **Program extension mapping** in same patterns doc). **B2** append-only log → **[TECH-43](../../BACKLOG.md)** (backlog placeholder, no project spec yet).

**Brainstorm (exploration + FAQ):** [`projects/TECH-21-json-use-cases-brainstorm.md`](../../projects/TECH-21-json-use-cases-brainstorm.md)

**Related programs / issues:** **[TECH-36](TECH-36.md)** (**compute-lib**, **TECH-39** `geography_init_params_validate`), **TECH-15** / **TECH-16**, **FEAT-37c** (completed — [`BACKLOG-ARCHIVE.md`](../../BACKLOG-ARCHIVE.md) **Recent archive**), **TECH-44** program (completed — [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-44**; **TECH-44b**/**c**; **Editor export registry** **TECH-55**/**TECH-55b** **§ Completed**; open follow-ups **TECH-53**/**TECH-54**; **TECH-18**), [`docs/planned-domain-ideas.md`](../../docs/planned-domain-ideas.md).

**territory-ia:** `glossary_discover` / `glossary_lookup` / `spec_outline` — **Save data**, **CellData**, **Load pipeline**, **Geography initialization**, **HeightMap**, **Water map**.

## 1. Summary

This file is the **program charter** for **TECH-21**. **TECH-40**, **TECH-41**, and **TECH-44a** are **completed** (see [`BACKLOG.md`](../../BACKLOG.md) **§ Completed**); **Phase C** pattern documentation lives in [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md) (**B1**/**B3**/**P5**). The program introduces **versioned, validated JSON** for tooling and **init**/**interchange** layers, **machine indexes** for specs and glossary (**without** duplicating authoritative Markdown — **TECH-18**), and documented patterns for a **Postgres** layer (**TECH-44** program **§ Completed** — [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-44**; **TECH-44b**: [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md), **`db/migrations/`**). Player **Save data** must not change format without a **dedicated migration** issue.

## 2. Resolved decisions (architecture)

| Topic | Decision |
|-------|----------|
| Umbrella vs child specs | **TECH-21** = charter only; **Phase C** patterns durable in [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md) (**TECH-44a** **§ Completed**); **TECH-40** / **TECH-41** completed — **BACKLOG** **§ Completed**. |
| Spec duplication | No full **reference spec** bodies in JSON; **I1**/**I2** are **indexes** and anchors only. |
| `schema_version` | Optional in payload when JSON Schema `$id` / filename semver suffices; required when **DB**, **Save-adjacent** export, or **MCP** consumer needs **one integer** for **branching migrations**. See brainstorm **§FAQ**. |
| Artifact identity | Every interchange JSON type carries a logical `artifact` (or `kind`) string; **SQL** table name is separate at persistence layer. |
| Validation cost | **P3**: validate fixtures in **CI**; **avoid** schema validation on **player** hot paths in **release** builds. |

## 3. Dependency graph

```text
TECH-40 (infra + I1 + I2)
    → TECH-41 (G1, G2, G4, E3, P1, P2, P4)
        → TECH-44a (B1, B3, P5)
            → TECH-44b **§ Completed** (Postgres IA — [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md))
                → TECH-44c **§ Completed** (E1 repro registry)

TECH-43 (B2) — depends soft on TECH-40; no spec until scheduled

TECH-36 / TECH-37–39 — soft coordination on **GeographyInitParams** / Zod
```

## 4. Goals and Non-Goals (program level)

### 4.1 Goals

1. **TECH-40**, **TECH-41**, and **TECH-44a** **Acceptance** satisfied (**TECH-44a** — **§ Completed**; patterns in [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md)).
2. Glossary / **persistence-system** vocabulary in schema `description` fields and DTO docs.
3. Clear **E3** layering: **MonoBehaviour** ↔ interchange DTO ↔ **CellData** / **Save data**.

### 4.2 Non-Goals

1. Replacing the entire **Load pipeline** / **Save data** format in this program.
2. Implementing **FEAT-46** UI, **FEAT-47** multipolar behavior, or **FEAT-48** volume gameplay (payloads may **anticipate** fields only).

## 5. User / Developer Stories (program)

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | I want phased issues so infra lands before gameplay JSON. | **TECH-40** → **TECH-41** order in **BACKLOG**. |
| 2 | Agent | I want stable **artifact** names and schemas. | **TECH-40** pilot + policy merged. |

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-02 | Spec created | Backlog integration | — |
| 2026-04-03 | Link brainstorm use-case doc | Prioritize scenarios before **pilot** | Inline only in spec |
| 2026-04-03 | Split **TECH-40**/**41**/**42** | Separate infra, current payloads, future DB/API | Single **TECH-21** issue only |
| 2026-04-11 | **TECH-40** closed | **Phase A** shipped | — |
| 2026-04-11 | **TECH-41** closed | **Phase B** shipped | — |
| 2026-04-03 | **TECH-42**/**TECH-19** → **TECH-44** + **44a**/**b**/**c** | Single implementation program; **44a** = Phase C doc | Keep split issues |
| 2026-04-03 | Umbrella **§8** item 1 | **TECH-44a** **§8** acceptance criteria all satisfied (**Implementation Plan** executed) | — |
| 2026-04-03 | **TECH-44a** **project-spec-close** | **Phase C** patterns persisted to [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md) + **glossary**; **TECH-44a** project spec removed after closure | — |

## 7. Implementation Plan

**Delegated** to child specs:

| Phase | Issue | Spec | Focus |
|-------|-------|------|--------|
| A | **TECH-40** | (completed — **BACKLOG** **§ Completed**) | Schemas, **CI**, **I1**, **I2**, versioning policy |
| B | **TECH-41** | (completed — **BACKLOG** **§ Completed**) | **G1**, **G2**, **G4**, **E3**, **P1**, **P2**, **P4** |
| C | **TECH-44a** | (completed — [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md); [`BACKLOG.md`](../../BACKLOG.md) **§ Completed**) | **B1**, **B3**, **P5**; **TECH-44** program (completed — [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-44**) for DB (**44b**) + **E1** (**44c**) |

**Phase C patterns** (future DB/API): [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md) (row+**JSONB**, **B3** envelope, **P5** streaming, SQL vs **`artifact`** naming).

**Postgres + first dev row:** **TECH-44** program (completed — [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-44**; [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md) **Program extension mapping**) — **TECH-44b** **§ Completed**, **TECH-44c** **§ Completed**.

**Placeholder:** **TECH-43** — **B2** append-only JSON lines (see **BACKLOG.md**).

## 8. Acceptance Criteria (umbrella)

- [x] **TECH-40**, **TECH-41**, and **TECH-44a** each meet their spec **§8 Acceptance**. *(**TECH-40**/**TECH-41**/**TECH-44a** — **BACKLOG** **§ Completed**; **TECH-44a** patterns in [`docs/postgres-interchange-patterns.md`](../../docs/postgres-interchange-patterns.md).)*
- [ ] **Save data** format unchanged without a tracked migration issue.
- [ ] Brainstorm **§FAQ** and **§Retained** ideas stay aligned with implemented phases (update as rows are **Accepted** / **Deferred**).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions

- **N/A (program charter).** **Phase A** committed **I1**/**I2** under `tools/mcp-ia-server/data/` with **CI** drift checks; **TECH-44b** shipped **Postgres** **IA** schema (see [`docs/postgres-ia-dev-setup.md`](../../docs/postgres-ia-dev-setup.md)); **TECH-18** may flip **MCP** to DB-backed IA without removing Markdown source of truth.
