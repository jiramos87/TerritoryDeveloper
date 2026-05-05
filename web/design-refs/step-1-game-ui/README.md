# `step-1-game-ui/` — sketchpad-only

Status: **sketchpad** per DEC-A24 §6 D6 (game-ui-catalog-bake Stage 6).

## What lives here

Snapshot JSON + tokens + layout-rects authored during pre-catalog explorations. Visual reference only.

## Runtime contract

Severed. `LayoutRectsLoader` deleted; `UiBakeHandler` no longer reads `layout-rects.json` / `ir.json` / `transcribe-cd` outputs. Frame bake emits sentinel rects with anti-loss prefab guard.

## Where bake-time data lives now

Catalog tables (migration 0061; `panels`, `buttons`, `sprites`, `tokens`). MCP entry: `catalog_panel_*`, `catalog_button_*`, `catalog_sprite_*`, `catalog_token_*`. Bake handler: `CatalogBakeHandler`.

## Cross-links

- DEC-A24 (DB `arch_decisions`).
- `docs/game-ui-catalog-bake-exploration.md §6`.
- Master plan `game-ui-catalog-bake` Stage 6.

## Don't

Don't wire this folder into runtime. Don't restore `LayoutRectsLoader`. Don't reference `ir-schema.ts` from `Assets/Scripts/**`.
