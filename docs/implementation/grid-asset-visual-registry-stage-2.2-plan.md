# Grid asset visual registry — Stage 2.2 plan digest (compiled)

> **Source:** `ia/projects/grid-asset-visual-registry-master-plan.md` **Stage 2.2** — `GridAssetCatalog` runtime loader.  
> **Filed issues:** **TECH-669**–**TECH-673** — closed rows in **`BACKLOG-ARCHIVE.md`**.  
> **Generated:** 2026-04-22 — inline `/ship-stage-main-session` chain (no MCP `plan_digest_compile_stage_doc` — hand-compiled).  
> **Closeout + audit:** see **`ia/projects/grid-asset-visual-registry-master-plan.md` Stage 2.2** — `#### §Stage Audit` and `#### §Stage Closeout Plan`.

## Stage exit (from master plan)

- Main scene holds `GridAssetCatalog` `MonoBehaviour` wired in Inspector; **`Awake`** loads snapshot; **`GetAsset` / `TryGet`** APIs have XML `///` summaries; **`OnCatalogReloaded`** runs after load/reload.
- **Depends on** archived **TECH-663** (schema) + **TECH-664** (Unity path) where applicable in backlog yaml.

## Task index

| issue | task_key | title | closed |
|---|---|---|---|
| TECH-669 | T2.2.1 | DTOs + parser | `BACKLOG-ARCHIVE.md` (TECH-669) |
| TECH-670 | T2.2.2 | Indexes by id and slug | `BACKLOG-ARCHIVE.md` (TECH-670) |
| TECH-671 | T2.2.3 | Missing sprite resolution | `BACKLOG-ARCHIVE.md` (TECH-671) |
| TECH-672 | T2.2.4 | Boot load path | `BACKLOG-ARCHIVE.md` (TECH-672) |
| TECH-673 | T2.2.5 | Hot-reload signal stub | `BACKLOG-ARCHIVE.md` (TECH-673) |

## Mechanical ordering

1. **TECH-669** — DTOs + `TryParse` (no runtime scene dependency).
2. **TECH-670** — indexes (consumes 669 types).
3. **TECH-671** — missing-sprite policy (touches row/sprite fields from indexes).
4. **TECH-672** — `Awake` load + public API XML + `OnCatalogReloaded` (orchestrates 669+670+671).
5. **TECH-673** — `ReloadFromDisk` + Editor menu; refactor shared load w/ 672.

## Verification gate (Stage ship)

- `npm run unity:compile-check` after all five tasks implement (cumulative) — per spec §Plan Digest **Gate** blocks.

## Plan-review

**PASS** — `§Plan Fix` under Stage 2.2 in master plan; no drift tuples at chain end.

## §Stage Audit (durable)

Same bullets as `ia/projects/grid-asset-visual-registry-master-plan.md` under Stage 2.2 **`#### §Stage Audit`** (post-closeout; task specs no longer on disk).

## Closeout

- Backlog: **TECH-669**–**TECH-673** in `ia/backlog-archive/`.
- Specs: `ia/projects/TECH-669`–`TECH-673` **removed** at closeout.
- Master plan: task table **Done (archived)**; Stage 2.2 **Status: Final**.

## Verification (recorded 2026-04-22)

- `npm run validate:all` — **PASS** (at closeout and after any doc-only follow-up).
- `npm run unity:compile-check` — **PASS** (Unity Editor and workers fully quit; log `tools/reports/unity-compile-check-20260422-212822.log` or later sibling).
