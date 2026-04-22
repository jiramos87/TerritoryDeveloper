# Grid asset visual registry — Stage 1.4 plan digest (compiled)

**Master plan:** `ia/projects/grid-asset-visual-registry-master-plan.md`  
**Stage:** 1.4 — MCP `catalog_*` tools + `caller-allowlist` updates  
**Filed tasks:** **TECH-650**–**TECH-654** — see [BACKLOG.md](../../BACKLOG.md)  
**Date:** 2026-04-22

## Stage exit (orchestrator)

- MCP server lists new **`catalog_*`** tools; `tools/mcp-ia-server` tests cover happy + error paths; **`caller-allowlist.ts`** classifies read vs mutation callers.
- **`docs/mcp-ia-server.md`** human catalog updated; **`npm run validate:all`** green at Stage boundary (task **TECH-654**).

## Task index

| Issue | Task key | Short goal |
|-------|----------|------------|
| **TECH-650** | T1.4.1 | `catalog_list` + `catalog_get` (published default) |
| **TECH-651** | T1.4.2 | `catalog_upsert` + `catalog_pool_*` |
| **TECH-652** | T1.4.3 | MCP unit tests (fixtures/mocks) |
| **TECH-653** | T1.4.4 | `caller-allowlist.ts` map + `checkCaller` |
| **TECH-654** | T1.4.5 | `docs/mcp-ia-server.md` + `validate:all` |

**Depends on:** Stage 1.3 HTTP routes + DTOs archived (**TECH-640**–**TECH-645**); prior Stage 1.2 DTOs (**TECH-626**–**TECH-629**). New tasks use empty yaml `depends_on` (sibling order per task table).  
**Authority:** `docs/grid-asset-visual-registry-exploration.md` §8; `docs/architecture-audit-handoff-2026-04-22.md` (no Drizzle in `web/`).

## Verification spine

1. `npm test` in `tools/mcp-ia-server` (catalog tool + allowlist tests).  
2. `npm run validate:all` for doc + `ia/` + backlog touches (**TECH-654**).  
3. No Unity compile unless `Assets/**` changes (not expected in Stage 1.4 default path).

## plan-review

**2026-04-22:** `plan-review` **PASS** for Stage 1.4 — `### §Plan Fix — PASS (no drift)` under Stage 1.4 in `ia/projects/grid-asset-visual-registry-master-plan.md`.

## Per-spec digests

Specs closed 2026-04-22 — archived issue rows: **TECH-650**–**TECH-654** in [BACKLOG-ARCHIVE.md](../../BACKLOG-ARCHIVE.md); digest content was inlined in this doc’s **Task index** + **Verification spine** during Stage execution.
