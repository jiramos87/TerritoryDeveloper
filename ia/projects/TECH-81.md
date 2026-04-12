---
purpose: "Project spec for TECH-81 — Knowledge graph: evolve IA from document retrieval to queryable entity-relationship model."
audience: both
loaded_by: ondemand
slices_via: none
---
# TECH-81 — Knowledge graph: evolve IA from document retrieval to queryable entity-relationship model

> **Issue:** [TECH-81](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-07
> **Last updated:** 2026-04-07

## 1. Summary

Evolve the IA system from document retrieval (agents ask for slices of Markdown) to a queryable knowledge graph where entities (managers, cells, water bodies, zones, buildings, prefabs, invariants, glossary terms, spec sections) have typed relationships ("depends on", "modifies", "validates", "persists", "affects demand of"). Enables transitive queries like "What are all the things that happen when I change a cell's height?"

## 2. Goals and Non-Goals

### 2.1 Goals

1. Postgres-backed entity-relationship store with typed nodes and edges
2. MCP tool `dependency_chain(entity, direction?)` — follow relationships transitively (e.g., "HeightMap write" → sync Cell.height → refresh shore terrain → update sorting order → invalidate cliff faces)
3. MCP tool `impact_analysis(entity)` — given a concept or class, return all related invariants, spec sections, glossary terms, and affected managers
4. Ingest pipeline that populates the graph from existing IA sources (glossary → spec references, invariants → affected entities, manager dependencies from ARCHITECTURE.md)
5. Visualization export (JSON for rendering in a web-based graph viewer)

### 2.2 Non-Goals (Out of Scope)

1. Replacing existing MCP tools (spec_section, glossary_lookup, etc. remain for precise retrieval)
2. Real-time graph updates on file save (batch rebuild)
3. Full ontology or OWL/RDF formalism (lightweight typed graph is sufficient)
4. Code-level call graph analysis (this is a *domain knowledge* graph, not an AST)

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | AI agent | As an agent about to modify `HeightMap`, I want to know all downstream effects | `dependency_chain("HeightMap")` returns: Cell.height sync (invariant #1), shore band recalc (invariant #7), cliff face update (geo §5), sorting order recalc (geo §7), water body membership check (geo §11), terraform undo (geo §8) |
| 2 | AI agent | As an agent implementing a new water feature, I want to know which managers are affected | `impact_analysis("WaterBody")` returns: WaterManager (modifies), TerrainManager (depends on for shore/cliff), GridManager (reads water body type), GeographyManager (initializes), ForestManager (excludes water cells), DemandManager (desirability from water proximity) |
| 3 | Developer | As a developer planning a refactor, I want to see all entities connected to RoadManager within 2 hops | Graph query with depth limit returns: TerrainManager, GridManager, CityStats, UIManager, ZoneManager, InterstateManager, RoadPrefabResolver, PathTerraformPlan, RoadPathCostConstants, RoadStrokeTerrainRules, GridPathfinder |
| 4 | IA maintainer | As a system maintainer, I want to export the graph as JSON for visualization | Export endpoint produces nodes + edges JSON suitable for D3/vis.js rendering |

## 4. Current State

### 4.1 Domain behavior

The glossary is a proto-knowledge graph: each term links to a spec and category. The agent-router maps tasks to specs. ARCHITECTURE.md has a full dependency table. Invariants reference specific entities. But these connections are implicit in Markdown — not queryable as a graph. An agent asking "what does changing HeightMap affect?" must read invariants, 3+ spec sections, and the dependency table manually.

### 4.2 Systems map

- `ia/specs/glossary.md` — term → spec → category (proto-graph)
- `ia/rules/agent-router.md` — task → spec routing (proto-graph)
- `ia/rules/invariants.md` — constraint → affected entities (implicit)
- `ARCHITECTURE.md` — manager → manager dependencies (explicit table)
- `ia/specs/*.md` — cross-references between sections (implicit)
- `tools/mcp-ia-server/src/ia-db/` — Postgres infrastructure

## 5. Proposed Design

### 5.1 Target behavior (product)

**Graph structure:**

Nodes have a `kind` (manager, helper, data_structure, concept, invariant, spec_section, glossary_term) and a `label`.

Edges have a `relation` (depends_on, modifies, validates, persists, reads, affects, initialized_by, contains).

**Example subgraph around HeightMap:**

```
[HeightMap] --modifies--> [Cell.height]
[HeightMap] --validated_by--> [Invariant #1: HeightMap sync]
[HeightMap] --affects--> [Shore band calculation]
[Shore band calculation] --validated_by--> [Invariant #7: shore band]
[HeightMap] --affects--> [Sorting order]
[Sorting order] --spec_section--> [geo §7]
[HeightMap] --persisted_by--> [GameSaveData]
[HeightMap] --initialized_by--> [GeographyManager]
[TerrainManager] --modifies--> [HeightMap]
[WaterManager] --reads--> [HeightMap]
```

**Example query:**

```
dependency_chain({ entity: "HeightMap", direction: "downstream", max_depth: 3 })
→ {
    root: "HeightMap",
    chain: [
      { entity: "Cell.height", relation: "modifies", depth: 1,
        invariant: "HeightMap[x,y] == Cell.height — always in sync" },
      { entity: "Shore band", relation: "affects", depth: 1,
        invariant: "Shore band: land Moore-adjacent to water must have height ≤ min(S)" },
      { entity: "Cliff faces", relation: "affects", depth: 2,
        spec: "geo §5" },
      { entity: "Sorting order", relation: "affects", depth: 2,
        spec: "geo §7" },
      { entity: "Water body membership", relation: "affects", depth: 2,
        spec: "geo §11" }
    ]
  }
```

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Implementation approach left to the implementing agent. Key considerations: graph storage model (adjacency table vs property graph in Postgres), ingest pipeline (parse glossary cross-references, invariant entity mentions, ARCHITECTURE.md dependency table, spec section cross-links), query traversal (recursive CTE or application-level BFS).

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-07 | Postgres adjacency table over dedicated graph DB | Keeps infrastructure simple; existing Postgres; recursive CTEs are sufficient for small graphs (<1000 nodes) | Neo4j, dgraph; in-memory graph in Node.js |
| 2026-04-07 | Domain knowledge graph, not code-level call graph | Code-level analysis is better served by IDE tools and LSP; the IA graph captures *domain* relationships that aren't in code | Full AST-based call graph; combined code+domain graph |

## 7. Implementation Plan

### Phase 1 — Graph schema and ingest

- [ ] Design `ia_graph_node` and `ia_graph_edge` tables
- [ ] Migration script
- [ ] Ingest pipeline: parse glossary, ARCHITECTURE.md dependencies, invariant entities, spec cross-references

### Phase 2 — Query tools

- [ ] Register `dependency_chain` MCP tool
- [ ] Register `impact_analysis` MCP tool
- [ ] Visualization JSON export

### Phase 3 — Enrichment

- [ ] Add edges from agent session logs (TECH-79) — "agents who queried X also queried Y"
- [ ] Add edges from project spec journal (existing) — "issue FEAT-43 touched these entities"

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Graph populated from current IA | Node | Ingest script + node/edge count assertions | Part of test suite |
| Traversal produces correct chains | Node | `npm run test:ia` with fixture queries | Known entity → expected chain |

## 8. Acceptance Criteria

- [ ] Graph tables created via migration and populated from current IA sources
- [ ] `dependency_chain` MCP tool returns transitive downstream/upstream relationships
- [ ] `impact_analysis` MCP tool returns all related invariants, specs, glossary terms, and managers for an entity
- [ ] Visualization JSON export works
- [ ] Graceful `db_unconfigured` when Postgres unavailable
- [ ] Documented in `docs/mcp-ia-server.md`
- [ ] `npm run verify` and `npm run test:ia` green

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

-

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.
