# Grid asset visual registry — Stage 1.3 plan digest (compiled)

**Master plan:** `ia/projects/grid-asset-visual-registry-master-plan.md`  
**Stage:** 1.3 — Next `/api/catalog/*` routes (Postgres + Next App Router; DTOs from Stage 1.2)  
**Filed tasks:** **TECH-640**–**TECH-645** — see [BACKLOG.md](../../BACKLOG.md)  
**Date:** 2026-04-22

## Stage exit (orchestrator)

- Local `curl` / test proves list + get + create + patch + 409 + retire + preview paths as per Step 1 Exit criteria in master plan.
- Errors are structured JSON consistent with `web/app/api/*` (via shared **TECH-642** helper).
- `npm run validate:web` (or `validate:all` if IA touched) green at Stage boundary.

## Task index

| Issue | Task key | Short goal |
|-------|----------|------------|
| **TECH-640** | T1.3.1 | `GET /api/catalog/assets` + filters, published default |
| **TECH-641** | T1.3.2 | `GET /api/catalog/assets/:id` joined shape |
| **TECH-642** | T1.3.3 | HTTP 400/404/409 + logging; no client stack |
| **TECH-643** | T1.3.4 | `POST` create transactional |
| **TECH-644** | T1.3.5 | `PATCH` optimistic lock, 409 + current row |
| **TECH-645** | T1.3.6 | Retire + `POST /api/catalog/preview-diff` (read-only) |

**Depends on:** prior Stage 1.2 DTOs archived (**TECH-626**–**TECH-629**); no blocking yaml `depends_on` (1.2 complete).  
**Authority:** `docs/grid-asset-visual-registry-exploration.md` §8; `docs/architecture-audit-handoff-2026-04-22.md` (no Drizzle in `web/`).

## Verification spine (Path A)

1. `npm run validate:web` from repo root for `web/**` changes.  
2. `npm run validate:all` if `ia/`, `tools/mcp-ia-server/`, or backlog yamls touched.  
3. DB-backed tests optional — local `DATABASE_URL` + migrate per `web/README.md` when implementers add integration tests.

## plan-review

**2026-04-22:** `plan-review` **PASS** for Stage 1.3 — `### §Plan Fix — PASS (no drift)` under Stage 1.3 in `ia/projects/grid-asset-visual-registry-master-plan.md`.

## Per-spec digests

Stage 1.3 closed 2026-04-22 — project specs removed per Stage closeout; issue records archived under [BACKLOG-ARCHIVE.md](../../BACKLOG-ARCHIVE.md) (**TECH-640**–**TECH-645**).
