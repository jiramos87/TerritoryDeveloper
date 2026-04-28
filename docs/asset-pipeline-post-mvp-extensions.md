---
purpose: "Asset Pipeline — extension stages folding `grid-asset-visual-registry` residual scope into the active orchestrator"
audience: master-plan-extend
source_orchestrator: asset-pipeline
superseded_orchestrator: grid-asset-visual-registry
created: 2026-04-26
status: ready-for-extend
---

# Asset Pipeline — Post-MVP Extensions (Save Remap + Bridge Composite)

## Why this doc exists

`grid-asset-visual-registry` master plan (archived foundation Stages 1.x–3.2) has been superseded by `asset-pipeline` Stage 1.1 catalog spine migration. Two residual scopes remain:

1. **Save-game `asset_id` remap** — old saves carry `subTypeId` (0..6 from `ZoneSubTypeRegistry`); new runtime resolves `catalog_entity.id` via `asset_detail.legacy_asset_id` UNIQUE column. Includes `replaced_by_entity_id` chain follow + missing-asset placeholder policy.
2. **Bridge composite + scene contract** — `wire_asset_from_catalog` IDE-agent bridge command, transactional snapshot/rollback, dry_run preflight, IA scene contract doc enumerating canonical scene paths.

Both fold INSIDE MVP scope as Stages **19.2** and **19.3**, sequenced before existing Stage **20.1 — MVP closeout**.

`grid-asset-visual-registry` orchestrator gets a "Superseded by asset-pipeline 2026-04-26" preamble banner, stage table preserved for history. Never-filed `TECH-772..775` references dropped.

---

## Design Expansion

### Architectural alignment (catalog spine ground truth)

DB schema (db/migrations/0021_catalog_spine.sql, 0026_auth_users_capabilities.sql) confirms:

- `catalog_entity.id` = `bigserial` (NOT uuid) — save format must persist `bigint`.
- `catalog_entity.replaced_by_entity_id bigint REFERENCES catalog_entity(id) ON DELETE SET NULL` — chain follow target.
- `catalog_entity.retired_at timestamptz` — retire signal.
- `asset_detail.legacy_asset_id bigint UNIQUE` — Zone S subTypeId 0..6 carrier (DEC-A8).
- `audit_log` table = append-only mutation trail; columns: `action text NOT NULL`, `target_kind text`, `target_id text`, `payload jsonb`. Reused for runtime asset-miss telemetry (no new table).

### Decision log (this doc)

| # | Decision | Rationale |
|---|---|---|
| D1 | Save format = `catalog_entity.id` bigint, persist as `entity_id` field | Spine id is identity; `subTypeId` retired from save format |
| D2 | Old saves load via `legacy_asset_id` lookup, transparent on-load remap, NO save-schema version bump | `legacy_asset_id` column persists forever; remap idempotent; re-save persists new shape |
| D3 | Retired entity → follow `replaced_by_entity_id` chain → placeholder fallback | Soft-retire honored; no save-game corruption |
| D4 | Placeholder = `Assets/Sprites/Placeholders/missing_asset.png` 64×64, magenta tint, runtime-scaled per cell footprint | Single-file authoring; loud + stable visual |
| D5 | Dev mode = magenta sprite + `console.warn` (`UNITY_EDITOR \|\| DEVELOPMENT_BUILD`); ship mode = hide cell + `audit_log` row | Fail loud in dev, fail safe in ship |
| D6 | Asset-miss telemetry sink = existing `audit_log` (action='asset_missing'\|'asset_retired_chain', target_kind='catalog_entity', target_id=entity_id::text, payload jsonb={cell_x, cell_y, chain_followed, fallback}) | Reuse existing table; no new migration |
| D7 | Bridge composite kind = `wire_asset_from_catalog` (legacy name from GAVR exploration) | Matches `asset_detail` row mental model |
| D8 | Stage 19.2 → depends on 18.1 (sprite GC closeout); Stage 19.3 → depends on 19.2 (save remap proven first) | Sequential: GC validates spine quiescent → save remap → bridge wiring on stable runtime |
| D9 | Sprite-asset GC = folded into existing Stage 18.1 (NOT a new stage) | No duplication; 18.1 already owns GC + ops + backups |
| D10 | GAVR orchestrator = banner + stage table preserved; never-filed `TECH-772..775` references stripped | Permanent orchestrator (per orchestrator-vs-spec.md); audit history readable |

### Scene contract scope (Stage 19.3, all paths)

Per user direction "full feature day 1, avoid refactoring in visible roadmap":

| Surface | Canonical path |
|---|---|
| Toolbar / button parent | `Canvas/Toolbar/ZoneButtons` |
| ThemedPanel tier mounts | `Canvas/Panels/{tier}` (tier = `floating`, `modal`, `overlay`) |
| World grid cell parent | `World/GridRoot/Cells/{cell_xy}` |
| UIManager bootstrap entry | `Bootstrap/UIManager` |

Bridge composite resolves these by named convention; rejects unknown paths with `unknown_scene_path` error before mutation.

---

## Stage 19.2 — Save remap + replaced_by chain + missing-asset policy

**Goal:** Old + new saves both load cleanly under spine schema. Retired entities follow `replaced_by` chain; orphans fall through to magenta placeholder (dev) or hidden cell (ship). Telemetry rows land in `audit_log`.

**Depends on:** Stage 18.1 (GC + ops + backups) — needs sprite-asset GC quiescent so placeholder rendering not racing eviction.

**Exit criteria:**

- Save with legacy `subTypeId` field loads → in-memory rewrite to `entity_id` → re-save persists new shape.
- Save with `entity_id` loads directly (no remap path).
- Retired `entity_id` → `replaced_by_entity_id` chain follow until non-retired or NULL → placeholder fallback emits `audit_log` row with `chain_followed=true`.
- Missing `entity_id` (no row) → placeholder; dev `console.warn`; ship hides cell + `audit_log` row with `chain_followed=false`.
- Compile-time symbol split (`#if UNITY_EDITOR || DEVELOPMENT_BUILD`) verified by editor + ship build smoke.

**Tasks (3, lean):**

| # | task_key | Title | Notes |
|---|---|---|---|
| T1 | `save-load-remap` | On-load idempotent remap subTypeId → entity_id | Read save; if `subTypeId` present, lookup `asset_detail.legacy_asset_id = subTypeId` → resolve `entity_id` → replace in-memory; re-save writes `entity_id`. Idempotent: second pass no-ops. NO save-schema version bump. |
| T2 | `replaced-by-resolver` | Chain follow runtime helper | `ResolveLiveEntityId(entity_id)` walks `replaced_by_entity_id` until non-retired or NULL; returns final id or null. Cycle guard (max 16 hops). Caller dispatches to placeholder if null. |
| T3 | `missing-asset-policy` | Placeholder PNG + audit_log emit + dev/ship symbol split | Author `Assets/Sprites/Placeholders/missing_asset.png` 64×64 magenta. Runtime: scale to cell footprint. `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` → render placeholder + `Debug.LogWarning`; `#else` → hide cell. Both modes emit `audit_log` row (action='asset_missing'\|'asset_retired_chain', target_kind='catalog_entity', payload jsonb). |

**Touched surfaces:**

- `Assets/Scripts/**/Persistence/*.cs` — save/load pipeline (T1).
- `Assets/Scripts/**/Catalog/*.cs` — new `ResolveLiveEntityId` helper (T2).
- `Assets/Scripts/**/Cells/*.cs` — placeholder rendering + cell hide (T3).
- `Assets/Sprites/Placeholders/missing_asset.png` (T3, new).
- DB: `audit_log` rows only (no schema change).

---

## Stage 19.3 — Bridge composite `wire_asset_from_catalog` + scene contract doc

**Goal:** IDE agent bridge can wire any catalog asset onto the scene by `entity_id`, transactionally (snapshot → mutate → verify → commit/rollback) with `dry_run` preflight. Scene contract doc enumerates canonical paths so future scene refactors won't break the bridge.

**Depends on:** Stage 19.2 (save remap proven first — bridge wiring depends on stable `ResolveLiveEntityId` + placeholder policy).

**Exit criteria:**

- `unity_bridge_command name=wire_asset_from_catalog args={entity_id, cell_xy, dry_run?}` returns `{ok, mutations[], rollback_token}`.
- `dry_run=true` returns proposed mutations without scene changes.
- Snapshot captures pre-state of target cell; rollback restores on verification failure.
- Verification = post-mutation `findobjectoftype_scan` confirms expected GameObjects under canonical paths.
- Scene contract doc filed at `docs/asset-pipeline-scene-contract.md` enumerating all 4 canonical path surfaces.
- Glossary row added for `wire_asset_from_catalog` + `scene contract`.

**Tasks (3, lean):**

| # | task_key | Title | Notes |
|---|---|---|---|
| T1 | `wire-composite` | `wire_asset_from_catalog` bridge command | Implement composite: resolve entity → load asset_detail row → instantiate prefab under canonical world cell parent path → bind world_sprite + button slot sprites if `has_button=true` → emit mutations[]. Reject unknown scene paths with `unknown_scene_path`. |
| T2 | `snapshot-rollback` | Transactional snapshot + dry_run + rollback | Pre-mutation: serialize cell GameObject tree to in-memory snapshot keyed by `rollback_token`. Post-mutation: verify via `findobjectoftype_scan`. On failure: deserialize snapshot, dispatch rollback, return `{ok:false, error, rollback_applied:true}`. `dry_run=true` returns proposed mutations[] without instantiation. |
| T3 | `scene-contract-doc` | IA scene contract doc + glossary | Author `docs/asset-pipeline-scene-contract.md` listing canonical paths (toolbar/buttons, panel tiers, world cells, bootstrap/UIManager). Add glossary rows for `wire_asset_from_catalog`, `scene contract`, `rollback_token`. Run `npm run generate:ia-indexes`. |

**Touched surfaces:**

- `Assets/Scripts/Editor/Bridge/Commands/WireAssetFromCatalog.cs` (T1, new).
- `Assets/Scripts/Editor/Bridge/Snapshot/*.cs` (T2, new — or extend existing snapshot infra if present).
- `docs/asset-pipeline-scene-contract.md` (T3, new).
- `ia/specs/glossary.md` (T3 — add rows).
- MCP: `mcp__territory-ia-bridge__unity_bridge_command` schema gains `wire_asset_from_catalog` composite kind.

---

## Implementer latitude

- T1 of 19.2 may use either eager remap (read pass rewrites all entries) or lazy remap (per-cell on first access) — pick whichever lands smaller diff.
- T2 of 19.2 cycle guard threshold (16) is advisory; raise if real chains exceed but log warning.
- T2 of 19.3 snapshot scope = cell subtree only (not whole scene) — minimize memory.
- Glossary row wording owned by T3 of 19.3 author; must align with existing entries in `ia/specs/glossary.md`.

## Pending decisions

None. All cleared via 4-round polling 2026-04-26.

## Closeout note for grid-asset-visual-registry

After `/master-plan-extend asset-pipeline docs/asset-pipeline-post-mvp-extensions.md` succeeds:

1. Append to `grid-asset-visual-registry` master plan preamble:
   > **SUPERSEDED 2026-04-26** — Stage 1.x–3.2 obsoleted by `asset-pipeline` Stage 1.1 catalog spine migration. Residual scope (save remap + bridge composite + scene contract) folded into `asset-pipeline` Stages 19.2 + 19.3. This orchestrator stays dormant for history; do not file new tasks here.
2. Strip `TECH-772 / TECH-773 / TECH-774 / TECH-775` references (never filed in BACKLOG).
3. Append change-log row: `2026-04-26 | superseded | Folded residual scope into asset-pipeline 19.2 + 19.3 | docs/asset-pipeline-post-mvp-extensions.md`.

---

## Source decisions (polling rounds)

Captured 2026-04-26 across 4 polling rounds:

- R1: Save id format = spine id; Retired = chain → placeholder; Sprite GC = fold into 18.1; Bridge stages = single stage.
- R2: Position = inside MVP before 20.1; Save migration = on-load remap (idempotent); Placeholder = magenta sprite dev / hide ship; GAVR = mark superseded keep dormant.
- R3: Placeholder PNG = single 64×64 file; Composite name = `wire_asset_from_catalog`; No save version bump; 3 tasks per stage.
- R4: Dependencies = 19.2→18.1, 19.3→19.2; Telemetry = `audit_log` reuse; Scene contract = all 4 canonical paths; GAVR closeout = banner + table preserved.
- R5: Placeholder spec = single fixed PNG; Stage 19.2 tasks = save-load-remap + replaced-by-resolver + missing-asset-policy; Stage 19.3 tasks = wire-composite + snapshot-rollback + scene-contract-doc; audit shape = action + payload jsonb (existing columns).
