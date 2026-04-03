# TECH-44c — Dev repro bundle registry (**E1**)

> **Issue:** [TECH-44c](../../BACKLOG.md)  
> **Program:** [TECH-44](TECH-44.md) (phase C)  
> **Depends on:** **TECH-44b** (Postgres + migrations applied)  
> **Status:** Draft  
> **Created:** 2026-04-03  
> **Last updated:** 2026-04-03

## 1. Summary

Implement **E1** from **[TECH-44](TECH-44.md) section 3**: a **repro bundle registry** that stores **metadata** linking Editor **Agent context** (`tools/reports/agent-context-*.json`) and optional **Sorting debug** Markdown exports to a **BACKLOG** issue id, `git` commit SHA, and timestamps. Uses **TECH-44a** **B1** shape (queryable scalars + **`payload jsonb`** for paths, bounded embedded summaries, or object-storage pointers later).

**Rationale:** Directly reduces **AI ↔ Unity** friction documented in [`projects/ia-driven-dev-territory-prompt.md`](../../projects/ia-driven-dev-territory-prompt.md) — agents and humans can query “latest repros for issue **BUG-XX**” without ad-hoc file sharing alone. **E2** → **TECH-53**, **E3** → **TECH-54** in [`BACKLOG.md`](../../BACKLOG.md).

## 2. Goals and Non-Goals

### 2.1 Goals

1. One **Postgres** table (or view + table) for **dev repro bundles** documented in migrations.
2. Documented **`artifact`** / **Interchange JSON** alignment for rows inserted by tooling (e.g. top-level keys in **JSONB** mirror **TECH-40** policy).
3. At least one **write path** (SQL insert template, small CLI, or Editor menu item) and one **read path** (query by `backlog_issue_id` or `git_sha`).

### 2.2 Non-Goals

1. Replacing **Save data** or storing full city state in **JSONB** (metadata and pointers only unless explicitly bounded samples).
2. **TECH-53** (**E2**) or **TECH-54** (**E3**) scope in this issue.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | I want to register a repro after exporting **Agent context**. | Row created with issue id + paths + SHA. |
| 2 | Agent author | I want to list recent repro rows for an issue. | Documented query returns ordered rows. |

## 4. Proposed schema (illustrative)

**Product names are adjustable in Decision Log.** Example **B1** mapping:

| Column | Role |
|--------|------|
| `id` | Primary key |
| `backlog_issue_id` | e.g. `BUG-53` |
| `git_sha` | Short or full commit |
| `exported_at_utc` | timestamptz |
| `interchange_revision` | int — mirrors consumer branching per **TECH-44a** §5.4 |
| `payload` | **jsonb** — e.g. `agent_context_relative_path`, `sorting_debug_relative_path`, optional `schema_version`, `artifact: "dev_repro_bundle"` |

## 5. Implementation Plan

- [ ] Add migration under same toolchain as **TECH-44b**.
- [ ] Document insert/select in `docs/` (or `tools/` README next to DB docs).
- [ ] Optional: script that takes paths + issue id + reads `git rev-parse HEAD`.

## 6. Acceptance Criteria

- [ ] Migrations apply on top of **TECH-44b** schema without error.
- [ ] **Decision Log** records final table/column names and **JSONB** key conventions.
- [ ] Example query + example insert published in English.
- [ ] No secrets committed; **Save data** / **Load pipeline** unchanged.

## 7. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | **E1** chosen for **TECH-44c**; **E2**/**E3** → **TECH-53**/**TECH-54** | Fastest path to agent-visible value from Editor exports |

## Open Questions

- Store file **paths only** vs small embedded JSON slice — default **paths** to avoid huge rows; **P5** if embeddings grow.
