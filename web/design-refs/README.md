# `web/design-refs/` — advisory sketchpad

Status: **sketchpad-only** (DEC-A24 §6 D6, game-ui-catalog-bake Stage 6).

## Scope

`step-1-game-ui/`, `step-1-game-ui-v2/`, `step-8-console/` snapshots survive as visual + token reference. Runtime no longer reads these surfaces — bake pipeline is asset-pipeline catalog (migration 0061; `CatalogBakeHandler`).

## What changed

- Claude-design IR JSON pipeline severed from runtime (Stage 6 closeout).
- Layout / token / panel data ships through `catalog_*` MCP + asset-pipeline bake.
- Files retained for human eyeballing only — no `LayoutRectsLoader`, no `transcribe-cd`, no `ir.json` runtime read.

## Authoritative surfaces

- Decision: `arch_decisions DEC-A24` (DB) + `docs/game-ui-catalog-bake-exploration.md §6`.
- Master plan: `game-ui-catalog-bake` Stage 6 (`master_plan_state` MCP).
- Catalog data: `catalog_panel_*`, `catalog_button_*`, `catalog_sprite_*`, `catalog_token_*` MCP.

## Don't

Don't add new IR-shaped JSON here expecting runtime pickup. Add to catalog instead.
