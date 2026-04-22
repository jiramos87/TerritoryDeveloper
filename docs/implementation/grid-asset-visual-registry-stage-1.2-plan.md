# Grid asset visual registry — Stage 1.2 plan digest (compiled)

**Master plan:** `ia/projects/grid-asset-visual-registry-master-plan.md`  
**Stage:** 1.2 — Catalog DTOs + API types (**no Drizzle** in `web/` per architecture audit 2026-04-22)  
**Filed tasks:** **TECH-626**–**TECH-629** — see [BACKLOG.md](../../BACKLOG.md)  
**Date:** 2026-04-22 (amended same day: Drizzle removed from `web/`; DTO path = `web/types/api/catalog*.ts`)

## Stage exit (orchestrator)

- Hand-written DTOs under `web/types/api/` align to `0011` / `0012` SQL; `npm run validate:web` (or `web` typecheck) passes.
- No drift vs migrations (column names + nullability) — documented in spec §7 / Decision Log as needed.

## Task index

| Issue | Task key | Short goal |
|-------|----------|------------|
| TECH-626 (archived) | T1.2.1 | Core catalog DTOs for 0011 (four tables + join shape) |
| TECH-627 (archived) | T1.2.2 | Pool DTOs for 0012 + test helpers |
| TECH-628 (archived) | T1.2.3 | API filter + lock + preview DTOs |
| TECH-629 (archived) | T1.2.4 | DTO ↔ SQL alignment (script or doc; no `drizzle-kit`) |

**Depends on (archived):** Stage 1.1 **TECH-612** (0011), **TECH-615** (0012) per task yaml.

**Authority:** `docs/architecture-audit-handoff-2026-04-22.md` Row 2; `docs/architecture-audit-change-list-2026-04-22.md` (DTO + optional zod).

## Verification spine (Path A)

After implementation across the four tasks:

1. `npm run validate:web` from repo root (touches `web/`).
2. If `package.json` scripts change in **TECH-629**: `npm run validate:all` and `npm run validate:backlog-yaml`.

## Per-spec digests (historical)

Stage closeout removed `ia/projects/TECH-626`–`TECH-629.md`. Archived backlog rows: `ia/backlog-archive/TECH-626.yaml` … `TECH-629.yaml`.

## plan-review

**2026-04-22:** `plan-review` PASS for Stage 1.2 — `### §Plan Fix — PASS (no drift)` under Stage 1.2 in `ia/projects/grid-asset-visual-registry-master-plan.md` (recheck line after DTO-path alignment).
