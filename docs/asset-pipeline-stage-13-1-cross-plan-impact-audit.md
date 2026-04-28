# Asset-pipeline Stage 13.1 — Cross-plan impact audit

**Date:** 2026-04-28
**Trigger:** asset-pipeline Stage 13.1 PASSED — commit `aac8fa1c` (Snapshot export + Unity FileSystemWatcher hot-reload).
**Source data:** `ia_master_plans` + `ia_stages` DB queries; preamble ILIKE scan for `asset-pipeline`/`catalog`/`snapshot`/`CatalogLoader`/`TokenCatalog`.

## Cross-plan impact table

| Slug | Done/Total | Block vs 13.1 | Drift risk |
|---|---|---|---|
| asset-pipeline | 14/23 | self — 19.2 in-flight; 19.3 (bridge composite `wire_asset_from_catalog`) is sibling-unblock critical-path; 14.1 parallel authoring polish | n/a (source) |
| architecture-coherence-system | 4/4 | done | low |
| multi-scale | 2/15 | **GREEN** Step 2 Stage 6 unblocked (TokenCatalog gate cleared) | low |
| game-ui-design-system | 4/9 | **GREEN** Stage 3+ bake consumers ready | low |
| full-game-mvp | 0/1 umbrella | n/a | n/a |
| grid-asset-visual-registry | 0/13 | **SUPERSEDED** 2026-04-26 | retired |
| web-platform | 0/38 | **YELLOW** dashboard catalog views ready | **medium** |
| citystats-overhaul | 0/10 | YELLOW snapshot ref unclear | medium |
| landmarks | 0/12 | YELLOW catalog consumer | medium |
| utilities | 0/13 | YELLOW catalog consumer | medium |
| distribution | 0/6 | YELLOW likely catalog consumer | medium |
| ui-polish | 0/14 | YELLOW post game-ui Stage 3 | medium |
| sprite-gen | 0/25 | **DRIFT** parallel render pipeline | **high** |
| mcp-lifecycle-tools-opus-4-7-audit | 0/17 | YELLOW IA-meta only | low |
| music-player | 0/8 | unrelated | none |
| unity-agent-bridge | 0/7 | unrelated | none |
| lifecycle-refactor | 0/11 | unrelated | none |
| backlog-yaml-mcp-alignment | 0/18 | unrelated (backlog catalog) | none |
| dashboard-prod-database | 0/7 | unrelated (DB snapshot) | none |
| session-token-latency | 0/12 | unrelated | none |
| skill-training | 0/6 | unrelated | none |
| city-sim-depth | 12/16 | unrelated | none |
| zone-s-economy | 0/9 | unrelated | none |
| blip | 20/20 superseded | done | n/a |

## Newly green for `/stage-decompose` / `/stage-file` / `/ship-stage`

1. `asset-pipeline 19.2` (in-flight) → `19.3` — true sibling-unblock critical-path. 19.3 ships `wire_asset_from_catalog` bridge composite + scene contract doc, gating multi-scale Step 2 Stage 6 region-cluster wiring.
2. `asset-pipeline 14.1` — parallel authoring-console feature (diff + history + references). Not a sibling-unblock gate; safe to defer.
3. `multi-scale 2.6` — TokenCatalog gate cleared (DEC-A14, asset-pipeline Stage 11 done; 13.1 ships Unity-side reader). Region cluster gated on asset-pipeline 19.3 `wire_asset_from_catalog`. Cleaner-pivot D2g/D4g still gates downstream.
4. `game-ui-design-system Stage 3+` — UiTheme SO + bake driver shipped; component bake can wire to catalog sprite refs.
5. `web-platform` — dashboard catalog views have populated snapshot to read (run `/arch-drift-scan web-platform` first).
6. Catalog-consumer plans (`landmarks`, `utilities`, `distribution`, `ui-polish`, `citystats-overhaul`) — green for `/design-explore` but each needs alignment-pass against DEC-A1..A52 before `/stage-file`.

## Drift hotspots

- **sprite-gen (HIGH)** — parallel render pipeline pre-dates asset-pipeline Stage 4.1 / DEC-A21..A25 (rendering pipeline locks). Run `/arch-drift-scan sprite-gen` or merge into asset-pipeline scope before resuming.
- **web-platform (MEDIUM)** — 38 pending stages, no recent DEC alignment pass. Run `/arch-drift-scan web-platform`.
- **landmarks / utilities / distribution / ui-polish / citystats-overhaul (MEDIUM)** — catalog consumers without explicit DEC alignment — run `/arch-drift-scan {slug}` per plan before resuming.

## Recommended next actions

1. `/ship-stage asset-pipeline Stage 19.2` — finish in-flight save-remap work (TECH-1585/1586/1587).
2. `/stage-file asset-pipeline Stage 19.3` then `/ship-stage` — bridge composite `wire_asset_from_catalog` is sibling-unblock critical-path (multi-scale 2.6 + game-ui consumers depend on this).
3. `/arch-drift-scan sprite-gen` — clear highest drift risk before restart.
4. `/arch-drift-scan web-platform` — clear medium drift before resuming dashboard work.
5. `/ship-stage asset-pipeline Stage 14.1` — parallel authoring polish; defer if 19.3 path is preferred.

## Methodology

- Stage counts from `ia_stages` grouped by `slug` with status filter (`done` / `in_progress` / other).
- Drift flags inferred from preamble ILIKE scan + presence/absence of explicit DEC-A* references.
- Block status inferred from preamble cross-references + Stage 13.1 deliverables (snapshot JSON export + Unity FileSystemWatcher reload + CatalogLoader).
- Plans flagged `unrelated` carry no `asset-pipeline` / `catalog` / `snapshot` / `CatalogLoader` / `TokenCatalog` token in preamble.
