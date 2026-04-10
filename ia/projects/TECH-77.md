---
purpose: "Project spec for TECH-77 — Unified semantic search across all IA surfaces (FTS in Postgres)."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-77 — Unified semantic search across all IA surfaces (FTS in Postgres)

> **Issue:** [TECH-77](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Create a single `ia_search(query, scope?)` MCP tool backed by Postgres full-text search that returns ranked results across all IA surfaces — glossary terms, spec section bodies, invariants, rule descriptions, backlog issue Notes, and journal entries — with source attribution. Eliminates the need for agents to know which specific tool to call first.

## 2. Goals and Non-Goals

### 2.1 Goals

1. A single MCP tool `ia_search` that queries all IA content with one call
2. Postgres-backed FTS index covering: glossary, spec sections, invariants, rules, backlog issues, journal entries
3. Results ranked by relevance with source type, key, and section attribution
4. Optional `scope` filter (e.g., `glossary`, `spec`, `rule`, `journal`, `backlog`)
5. Graceful degradation when Postgres is unavailable (fall back to file-based search or `db_unconfigured`)

### 2.2 Non-Goals (Out of Scope)

1. Replacing existing tools (`glossary_discover`, `spec_section`, etc.) — those remain for precise retrieval
2. Vector/embedding search (FTS first; embeddings are a future evolution)
3. Real-time indexing on file save (batch rebuild on demand or at server start)

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | As an agent exploring an unfamiliar domain, I want to search "what happens when a cell height changes" and get results from invariants, spec sections, and glossary in one call | `ia_search("cell height change")` returns HeightMap sync invariant, geo §2, glossary "HeightMap", persist §Load pipeline |
| 2 | AI agent | As an agent starting on a road issue, I want `ia_search("road placement validation", scope: "spec")` to return only spec sections about road validation pipeline | Scoped results from roads-system and geo §13 |
| 3 | Developer | As a developer querying via MCP inspector, I want to find all IA content mentioning "shore band" across specs, rules, and glossary | Results from invariants (shore band), geo §5, geo §14, glossary "Shore band", water-terrain-system |

## 4. Current State

### 4.1 Domain behavior

Currently agents must call multiple tools in sequence: `glossary_discover` (keyword match over glossary rows), `router_for_task` (substring match on agent-router tables), `spec_section` (heading match within one spec). There is no cross-surface search. The `ia_project_spec_journal` table already has a `body_tsv` GIN index demonstrating the FTS pattern.

### 4.2 Systems map

- `tools/mcp-ia-server/src/index.ts` — MCP tool registration
- `tools/mcp-ia-server/src/ia-db/` — Postgres pool and journal repo (existing FTS pattern)
- `tools/mcp-ia-server/src/parser/` — markdown, glossary, table parsers
- `tools/mcp-ia-server/src/config.ts` — registry building from filesystem
- `db/migrations/` — existing migration infrastructure

## 5. Proposed Design

### 5.1 Target behavior (product)

**Example calls and expected results:**

```
ia_search("shore band water height")
→ [
    { source: "invariant", id: 7, text: "Shore band: land Moore-adjacent to water...", score: 0.95 },
    { source: "glossary", term: "Shore band", definition: "...", score: 0.88 },
    { source: "spec_section", spec: "geo", section: "§5", title: "Water-shore...", score: 0.82 },
    { source: "spec_section", spec: "water-terrain", section: "Shore...", score: 0.75 },
    { source: "journal", issue: "BUG-45", kind: "lessons_learned", excerpt: "...", score: 0.60 }
  ]
```

```
ia_search("expenses maintenance", scope: "backlog")
→ [
    { source: "backlog", issue_id: "FEAT-52", title: "City services coverage model (fire, police, education, health)", score: 0.88 }
  ]
```

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key considerations: ingest pipeline (parse all IA sources → populate Postgres FTS table), index refresh strategy, ranking across heterogeneous sources.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Postgres FTS over vector embeddings | FTS is sufficient for keyword/phrase search, zero external dependencies, proven pattern in journal table | pgvector embeddings (higher accuracy but requires embedding model + more complexity) |
| 2026-04-07 | New tool alongside existing tools, not replacing them | Existing tools serve precise retrieval; unified search serves exploration | Replace glossary_discover and router_for_task |

## 7. Implementation Plan

### Phase 1 — Index infrastructure

- [ ] Design `ia_search_index` table schema (source_kind, source_key, section_id, title, body, body_tsv)
- [ ] Migration script
- [ ] Ingest script: parse all IA sources and populate the index

### Phase 2 — MCP tool

- [ ] Register `ia_search` tool with query + scope parameters
- [ ] Implement search with `ts_rank` ordering and source attribution
- [ ] Tests and fixtures

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| MCP tool registered and functional | Node | `npm run verify` + `npm run test:ia` | Repo root |
| Index populated from current IA sources | Node | Ingest script + row count assertion | Part of test suite |

## 8. Acceptance Criteria

- [ ] `ia_search` MCP tool registered and documented in `docs/mcp-ia-server.md`
- [ ] Searches across glossary, spec sections, invariants, rules, backlog issues, and journal entries
- [ ] Results include source attribution (source_kind, key, section, score)
- [ ] Optional `scope` filter works
- [ ] Graceful `db_unconfigured` when Postgres unavailable
- [ ] `npm run verify` and `npm run test:ia` green

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
