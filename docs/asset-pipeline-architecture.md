# Asset Pipeline — Architecture & Roadmap

> Interconnection of **sprite-gen CLI** ↔ **grid-asset visual registry** ↔ **web catalog tool** ↔ **Unity runtime**.  
> Status: 2026-04-25. Branch: `feature/skill-files-audit`.

---

## 1. System Overview

Three tiers with clean boundaries. Contract between tiers = **snapshot manifest** (JSON).

```
┌─────────────────────────────────────────────────────────────────────┐
│  TIER 1 — ART GENERATION                                           │
│  tools/sprite-gen/  (Python CLI)                                    │
│                                                                     │
│  YAML archetype → render → PNG variants → promote → catalog push   │
└──────────────────────────────┬──────────────────────────────────────┘
                               │  promote CLI
                               │  POST /api/catalog/assets/:id/sprite
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  TIER 2 — CATALOG & REGISTRY                                        │
│  Postgres DB  +  Next.js API  +  MCP catalog_* tools                │
│                                                                     │
│  ia_tasks / catalog_asset / catalog_sprite / catalog_economy        │
│  /api/catalog/assets  (CRUD + retire + preview-diff)                │
│  MCP: catalog_list, catalog_get, catalog_upsert, catalog_pool_*     │
│                                                                     │
│  Export step: DB → snapshot JSON (StreamingAssets/)                 │
└──────────────────────────────┬──────────────────────────────────────┘
                               │  snapshot JSON at boot
                               │  agent bridge: wire_asset_from_catalog
                               ▼
┌─────────────────────────────────────────────────────────────────────┐
│  TIER 3 — UNITY RUNTIME                                             │
│  Assets/Scripts/Managers/GameManagers/                              │
│                                                                     │
│  GridAssetCatalog  →  ZoneManager / ZoneSubTypeRegistry             │
│                    →  PlacementValidator                            │
│                    →  UI: IlluminatedButton / ThemedPanel           │
│  AgentBridgeCommandRunner.Mutations.cs                              │
│    kinds: wire_asset_from_catalog, wire_panel_from_catalog (TBD)   │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Detailed Data Flow

### 2.1 Art → Catalog (promote path)

```
tools/sprite-gen/specs/building_residential_small.yaml
        │
        │  python -m src render building_residential_small
        ▼
tools/sprite-gen/out/building_residential_small_v01..v04.png
        │
        │  python -m src promote out/building_residential_small_v01.png
        │                         --as residential_small
        ▼
┌─── Assets/Sprites/Generated/residential_small.png  ◄── Unity import
│    Assets/Sprites/Generated/residential_small.png.meta  (PPU=64, pivot)
│
└─── POST /api/catalog/assets/:id/sprite             ◄── web catalog
          body: { path, archetype_id, build_fingerprint }
          → catalog_sprite row + catalog_asset_sprite binding
```

**Contract fields:** `archetype_id` (stable slug from YAML filename), `build_fingerprint` (git sha + palette hash), `path` (relative from Assets/).

### 2.2 Catalog → Unity (snapshot export)

```
Postgres
  catalog_asset  ──────────────────────────────┐
  catalog_sprite (world / button slots)        │
  catalog_economy (cost, upkeep, ticks)        │  export script
  catalog_spawn_pool / catalog_pool_member     │  tools/scripts/export-catalog-snapshot.ts
                                               ▼
Assets/StreamingAssets/catalog/
  grid-asset-catalog-snapshot.json
        │
        │  GridAssetCatalog.cs  (MonoBehaviour singleton)
        │  .LoadAtBoot()
        ▼
  GridAssetCatalog.Instance.Get(assetId)
        │
        ├──► ZoneSubTypeRegistry  (Zone S consumers)
        ├──► PlacementValidator.CanPlace(assetId, cell, rotation)
        └──► UiTheme / IlluminatedButton wiring
```

### 2.3 Agent Bridge (MCP → Unity mutation)

```
Agent (Claude / Cursor)
        │
        │  mcp: unity_bridge_command
        │  kind: wire_asset_from_catalog
        │  { asset_id, target_parent_path, dry_run }
        ▼
AgentBridgeCommandRunner.Mutations.cs
  1. Read snapshot  →  resolve asset row + sprite path
  2. Instantiate world prefab  (or button prefab)
  3. Parent under target_parent_path
  4. Bind UiTheme refs  (IlluminatedButton / ThemedPanel)
  5. Wire onClick  →  UIManager entry point
  6. save_scene
  7. smoke_check  (if !dry_run)
  → rollback on any fail
```

---

## 3. Component Status Matrix

| Component | Location | Status |
|---|---|---|
| Sprite render engine | `tools/sprite-gen/src/` | ✅ Done (Stage 1.1–1.3) |
| Slope-aware foundation | `tools/sprite-gen/src/slopes.py` | ✅ Done (TECH-175–178) |
| Unity `.meta` writer | `tools/sprite-gen/src/unity_meta.py` | ❌ Draft (TECH-179) |
| Promote CLI | `tools/sprite-gen/src/curate.py` | ❌ Draft (TECH-180) |
| Aseprite Tier 2 integration | `tools/sprite-gen/src/aseprite_io.py` | ❌ Draft (TECH-181–183) |
| Catalog DB schema | `db/migrations/0011_catalog_core.sql` | ✅ Done |
| Spawn pool schema | `db/migrations/0012_catalog_spawn_pools.sql` | ✅ Done |
| Catalog API routes | `web/app/api/catalog/` | ✅ Done |
| MCP catalog tools | `tools/mcp-ia-server/src/` | ✅ Done (TECH-650–654) |
| Snapshot export script | `tools/scripts/export-catalog-snapshot.ts` | ❌ Not started |
| `GridAssetCatalog.cs` | `Assets/Scripts/Managers/GameManagers/` | ❌ Not started |
| `PlacementValidator.cs` | `Assets/Scripts/Managers/GameManagers/` | ❌ Not started |
| `wire_asset_from_catalog` bridge kind | `AgentBridgeCommandRunner.Mutations.cs` | ❌ Not started |
| Web catalog admin UI | `web/app/admin/catalog/` | ❌ Draft (Step 9, Stages 30–33) |
| Sprite web preview (promote → URL) | — | ❌ Blocked by promote CLI |

---

## 4. Stage Completion Roadmap

Ordered by dependency. Each stage = one shippable unit.

### Wave 1 — Unblock Promote (sprite-gen Stage 1.4 tail)

> Goal: sprites reachable from web + Unity import.

| Stage | Tasks | Outcome |
|---|---|---|
| **sprite-gen 1.4a** | TECH-179: unity_meta.py | `.meta` file generated on promote |
| **sprite-gen 1.4b** | TECH-180: promote/reject CLI | `promote out/X.png --as slug` → Assets/ + catalog push |
| **sprite-gen 1.4c** | TECH-181–183: Aseprite Tier 2 | `--layered` + `--edit` round-trip |

**Minimal shippable:** 1.4a + 1.4b only. 1.4c (Aseprite) can slip to post-MVP.

### Wave 2 — Snapshot Export + Unity Loader

> Goal: Unity reads catalog at boot.

| Stage | Tasks | Outcome |
|---|---|---|
| **registry Stage 1.1** | `export-catalog-snapshot.ts` script | `grid-asset-catalog-snapshot.json` emitted |
| **registry Stage 1.2** | `GridAssetCatalog.cs` singleton | Unity loads snapshot at boot, exposes `Get(assetId)` |
| **registry Stage 1.3** | Migrate `ZoneSubTypeRegistry` → catalog | Zone S reads from `GridAssetCatalog` |

### Wave 3 — Web Catalog Admin UI

> Goal: humans can author + review sprites in browser.

| Stage | Tasks | Outcome |
|---|---|---|
| **web Step 9 Stage 30** | Asset list + filter view | Browse all catalog rows |
| **web Step 9 Stage 31** | Asset detail + sprite slot editor | Bind promote PNG to button/world slots |
| **web Step 9 Stage 32** | Create / retire asset | Full CRUD loop |
| **web Step 9 Stage 33** | Spawn pool editor | Manage pool members + weights |

### Wave 4 — Agent Bridge Wiring

> Goal: agents can wire assets into Unity scenes.

| Stage | Tasks | Outcome |
|---|---|---|
| **registry Stage 2.1** | `PlacementValidator.cs` | `CanPlace(assetId, cell)` with structured reason |
| **registry Stage 2.2** | `wire_asset_from_catalog` kind | Agents instantiate + wire prefab from catalog row |
| **registry Stage 2.3** | scene-contract appendix | Canonical parent paths documented + enforced |

### Wave 5 — Composite Components (Post-MVP)

> Goal: agents can wire full panels, not just single assets.

| Stage | Tasks | Outcome |
|---|---|---|
| **registry Stage 3.x** | Panel / Button composite tables | `wire_panel_from_catalog` bridge kind |
| **sprite-gen Step 2** | Diffusion overlay | Optional img2img pass post-render |
| **sprite-gen Step 3** | EA bulk render | Batch 60–80 final sprites for EA |

---

## 5. Design Gaps — Decisions Needed Before Implementation

### GAP-1: Promote push contract (BLOCKING Wave 1)

**Question:** What exact payload does `promote` send to `/api/catalog/assets/:id/sprite`?  
**Options:**
- A) Promote pushes full metadata (path, archetype_id, build_fingerprint, ppu, pivot) in one POST
- B) Promote writes local file only; separate `catalog push` step reads a manifest and syncs

**Recommendation:** Option A — single atomic promote. Simpler, matches existing promote CLI intent.  
**Decision needed:** Confirm payload shape + authentication (local dev token in config.toml?).

---

### GAP-2: Snapshot format (BLOCKING Wave 2)

**Question:** What is the canonical shape of `grid-asset-catalog-snapshot.json`?  
**Minimum needed fields:** `asset_id`, `slug`, `display_name`, `status`, `sprite_slots` (world/button_target/button_pressed), `economy` (base_cost, upkeep), `footprint_w/h`, `placement_mode`.  
**Open:** Does Unity need pool memberships embedded, or loaded separately?  
**Recommendation:** Embed pool memberships inline. One file = one cold-start read.  
**Decision needed:** Confirm schema + whether `catalog_pool` rows ship in snapshot or are loaded on-demand.

---

### GAP-3: Hot-reload signal (affects Wave 2 dev UX)

**Question:** How does Unity refresh snapshot after a catalog edit during development?  
**Options:**
- A) Manual: re-run export script + reload scene
- B) FileSystemWatcher on StreamingAssets/ triggers `GridAssetCatalog.Reload()`
- C) MCP tool `catalog_snapshot_reload` triggers via bridge

**Recommendation:** Option A for MVP (simplest); Option B for dev comfort post-Wave 2.  
**Decision needed:** Accept Option A for now?

---

### GAP-4: Partial row validation (affects Wave 2 correctness)

**Question:** What is the minimum valid catalog row for a successful snapshot bake?  
**Risk:** Rows with no sprite bound but `status=published` would bake a broken prefab.  
**Recommendation:** Snapshot export gate — skip rows with no `world` sprite bound; log warning.  
**Decision needed:** Confirm gate behavior (skip-and-warn vs hard error vs include stub).

---

### GAP-5: Scene-contract paths (BLOCKING Wave 4)

**Question:** What are the canonical Unity scene parent paths for toolbar, HUD strips, modal host, Control Panel mount?  
**These do not exist yet** as a written spec. `wire_asset_from_catalog` needs them to parent correctly.  
**Example:**
```
Canvas/ControlPanel/ToolbarRoot        → world-grid toolbar buttons
Canvas/HUDStrip                        → economy / status indicators
Canvas/ModalHost                       → pop-up modal dialogs
```
**Action:** Append §Scene Contract to `ia/specs/ui-design-system.md` before Wave 4 starts.

---

### GAP-6: Aseprite binary discovery (affects sprite-gen 1.4c)

**Question:** How does `promote --edit` find the Aseprite binary cross-platform?  
**Options:** env var `ASEPRITE_BIN` → `config.toml [aseprite] bin` → platform defaults (macOS: `/Applications/Aseprite.app/.../aseprite`, Linux: `~/.local/bin/aseprite`).  
**Recommendation:** Try in that order; fail with exit code 4 if not found (already specified in exit-code table).  
**Decision needed:** Confirm fallback chain is acceptable.

---

### GAP-7: Composite components scope (affects Wave 5 planning)

**Question:** Does `asset-snapshot-mvp-exploration.md` need a full `/design-explore` pass before Wave 5 stages are filed?  
**Context:** The exploration defines Panel/Button/Prefab composites and `wire_panel_from_catalog` but has NO Design Expansion block yet.  
**Recommendation:** Yes — run `/design-explore docs/asset-snapshot-mvp-exploration.md` before filing any Wave 5 stages. Wave 1–4 can ship independently.

---

## 6. Dependency Graph

```
sprite-gen 1.4a (meta writer)
        │
sprite-gen 1.4b (promote CLI) ──────────────► web catalog admin UI (Wave 3)
        │                                              │
        │                                              │
        ▼                                              ▼
registry Stage 1.1 (snapshot export)        web Step 9 Stages 30–33
        │
registry Stage 1.2 (GridAssetCatalog.cs)
        │
registry Stage 1.3 (ZoneSubTypeRegistry migration)
        │
registry Stage 2.1 (PlacementValidator)
        │
registry Stage 2.2 (wire_asset_from_catalog)
        │
registry Stage 2.3 (scene-contract) ◄── GAP-5 must be resolved first
        │
        ▼
Wave 5: composites (needs /design-explore on asset-snapshot-mvp-exploration.md)
```

---

## 7. Files & Pointers

| File | Role |
|---|---|
| `docs/isometric-sprite-generator-exploration.md` | Sprite-gen ground truth — geometry, palettes, 5-step spine |
| `docs/grid-asset-visual-registry-exploration.md` | Registry ground truth — schema, bridge kinds, §8 Design Expansion |
| `docs/asset-snapshot-mvp-exploration.md` | Three-plan positioning + composite types — needs `/design-explore` before Wave 5 |
| `ia/projects/grid-asset-visual-registry-master-plan.md` | Registry orchestrator (not yet filed — trigger: `/master-plan-new docs/grid-asset-visual-registry-exploration.md`) |
| `tools/sprite-gen/config.toml` | Catalog URL + Aseprite bin (commented template, ready for TECH-180/181) |
| `web/app/api/catalog/` | Catalog CRUD routes (done) |
| `Assets/StreamingAssets/catalog/grid-asset-catalog-snapshot.json` | Unity snapshot artifact (target) |
| `Assets/Scripts/Editor/AgentBridgeCommandRunner.Mutations.cs` | Unity bridge mutations (extend for wire_asset_from_catalog) |
| `ia/specs/ui-design-system.md` | UiTheme / IlluminatedButton / ThemedPanel contracts (extend: §Scene Contract) |

---

## 8. Immediate Next Actions

1. **Resolve GAP-1 + GAP-2** (promote payload + snapshot schema) — 30 min design session → unblocks Wave 1 + 2 filing.
2. **File registry master plan** — `/master-plan-new docs/grid-asset-visual-registry-exploration.md` → orchestrator created.
3. **Ship sprite-gen 1.4a + 1.4b** (TECH-179 + TECH-180) — unblocks promote + web preview.
4. **Resolve GAP-5** (scene contract) — append to `ia/specs/ui-design-system.md` → unblocks Wave 4.
5. **Run `/design-explore docs/asset-snapshot-mvp-exploration.md`** — lock composite types before Wave 5 filing.
