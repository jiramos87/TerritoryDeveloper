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

## 5. Authoring-Console Design Decisions (in progress 2026-04-25)

> Live polling thread expanding Wave 3 + Wave 5 into one coherent authoring console.
> Goal: web catalog UI = full authoring surface for sprites + in-game UI buttons + panels.

### DEC-A1: Deployment model — **B) Local-dev MVP, hosted-ready schema**

MVP runs single-user on dev machine. Schema, API contracts, asset path fields designed from day one to migrate to a hosted worker queue + blob store with no migration of catalog rows. Concrete implications:
- Sprite paths stored as **logical refs** (e.g. `gen://{run_id}/{variant_id}` + resolved `assets_path`), not raw `tools/sprite-gen/out/...` strings.
- Every render output carries a **build_fingerprint** (already planned) **+ run_id** so future remote runs are addressable identically.
- All sprite-gen invocations sit behind backend API routes — no direct CLI shell from browser. Forward-compatible with swapping Python subprocess → HTTP worker.

### DEC-A52: Master-plan handoff — **graduate to lifecycle, file Stage 0–N orchestrator**

Polling thread closes here. Decisions DEC-A1–A52 form the persisted Design Expansion. Next step: hand off to `/master-plan-new` with this doc as the source.

**Proposed Stage breakdown (orchestrator skeleton, decomposed by `/master-plan-new`):**

| Stage | Theme | Key tasks |
|---|---|---|
| **0 — Foundation** | Pre-migration audit + DB snapshot freeze | DEC-A32 audit script, baseline `pg_dump`, decision-doc graduation to `ia/specs/catalog-architecture.md`. |
| **1 — Spine schema migration** | Transform existing catalog tables to spine model | Migrations `0021_catalog_spine`, `0022_catalog_detail_link`, `0023_catalog_legacy_drop`; `validate:catalog-spine`. Real numbers — repo's last migration is `0020_drop_source_path_columns.sql`. Detailed plan: [`docs/asset-pipeline-stage-0-1-impl.md`](asset-pipeline-stage-0-1-impl.md). |
| **2 — Auth + capability matrix** | Users, roles, capabilities, audit_log | NextAuth magic-link, `users` table, `capability` + `role_capability` seed, middleware, audit emitter. |
| **3 — Blob resolver + sprite-gen FastAPI** | Long-lived Python service + `BlobResolver` + `var/blobs/` | FastAPI serve mode, env-driven blob root, `gen://` resolver, sprite-gen contract. |
| **4 — Render pipeline** | Job queue, render worker, render-run rows | `job_queue`, `render_run`, single-FIFO worker, render API, retry/error handling. |
| **5 — Authoring console scaffolding** | Next.js `/catalog` shell + sidebar groups + shared list/detail layout | Routes, search bar, optimistic concurrency middleware, error envelope. |
| **6 — Per-kind authoring (sprite first)** | Sprite list/detail + render form + variant disposition + promote | `<ArchetypeParamsForm>`, render form, save-as-sprite, edit + versions tabs. |
| **7 — Asset / Pool authoring** | Asset edit + pool membership + primary-subtype tagging | Pool detail + member CRUD + bulk add. |
| **8 — Button + Panel authoring** | Slot-based composition, panel_child, in-game button assembly | `panel_child`, slot UI, cycle check, accepts[] enforcement. |
| **9 — Audio authoring** | Audio detail, archetype-driven params, loudness measure on promote | `audio_detail`, FastAPI synth + upload paths. |
| **10 — Token authoring + game DS** | 5 token kinds + structural-fidelity React preview | Token CRUD, `TokenCatalog` Unity binder, ripple semantics. |
| **11 — Archetype authoring** | Archetype + version lifecycle + migration helpers | Archetype edit, schema editor, `migration_hint_json` upgrade flow. |
| **12 — Publish pipeline + lint framework** | Hard gates + soft lints + publish dialog | Layer 1/2 lint runner, lint rule registry, publish flow. |
| **13 — Snapshot export + Unity reload** | `catalog_snapshot`, manifest, `FileSystemWatcher`, atomic swap | Auto + manual triggers, retire snapshot, GC integration. |
| **14 — Diff + history + references** | Versions tab, diff viewer, refs tab, ref edge view | `<EntityVersionDiff>`, `catalog_ref_edge` materialization, refs API. |
| **15 — Bulk + search + dashboard** | Trgm search, bulk action UI, dashboard widgets | Search API, bulk endpoints, unresolved-refs widget, lint summary. |
| **16 — Preview-in-Unity bridge** | DEC-A28 push handler, `PreviewCatalog`, screenshot return | Bridge command kind, sandboxed scene, side-by-side compare UI. |
| **17 — MCP tool surface parity** | Full catalog tool set, validator gate | All read/mutate/bridge tools registered, `validate:mcp-catalog-coverage`. |
| **18 — GC + ops + backups** | Sweep workers, restore drill, runbooks | GC catalog versions, blob sweep, nightly backup, DR docs. |
| **19 — Test harness + CI gates** | Unit/integration/roundtrip/E2E, snapshot Unity test | Vitest, Playwright smoke, `catalog-snapshot-roundtrip` test-mode scenario. |
| **20 — MVP closeout** | Documentation surface, glossary, IA spec graduation | `web/app/catalog/README.md`, runbooks, `ia/specs/catalog-architecture.md`. |

Hand-off command (manual): `/master-plan-new docs/asset-pipeline-architecture.md`.

Master plan goes to `ia/projects/asset-pipeline-master-plan.md` per project lifecycle. Stage decomposition into Tasks per stage runs via subsequent `/stage-decompose` and `/stage-file` per project conventions.

### DEC-A51: Documentation surface — **layered, mirrors codebase + lifecycle**

- `docs/asset-pipeline-architecture.md` — this file. Stays canonical authority for design intent + persisted decisions.
- `web/app/catalog/README.md` — console-specific quickstart: dev login, where each route lives, how to add a kind.
- `tools/sprite-gen/README.md` — Python tool dev guide, FastAPI run command, archetype YAML format.
- `docs/runbooks/catalog-recovery.md` — DR (DEC-A50).
- `docs/runbooks/catalog-publish-flow.md` — publish → snapshot → Unity reload sequence diagram, troubleshooting tree.
- `docs/runbooks/catalog-archetype-authoring.md` — how to create a new archetype kind end-to-end.
- IA glossary additions: every new term introduced (`render_run`, `entity_version`, `snapshot`, `publish_lint_rule`, `panel_child`, `archetype_version`, `blob_resolver`, `capability`, `pool_member`) added to `ia/specs/glossary.md` per project terminology rule.
- IA spec promotion: once master plan ships first stage, this design doc graduates relevant sections to `ia/specs/catalog-architecture.md` (durable spec) — `docs/` retains as exploration trail.

### DEC-A50: Backups + DB ops — **scheduled snapshots, verified restore, dump-tested**

- Nightly cron: `tools/scripts/backup-db.sh` runs `pg_dump -Fc` → `var/db-snapshots/nightly-{YYYY-MM-DD}.dump` + sha256 manifest.
- Retention: 14 daily, 8 weekly (Sundays), 6 monthly. Sweep script enforces.
- Pre-migration freeze (DEC-A32 reuse) — every destructive migration calls `freeze-db-snapshot.sh` automatically.
- Restore drill: monthly `verify:db-restore` script restores latest nightly into ephemeral local DB, runs `validate:catalog-spine` → confirms backup integrity. Failure pages dashboard.
- Blob backups (`var/blobs/`): rsync to secondary path nightly via `backup-blobs.sh`. Hosted future = swap to S3 lifecycle policy.
- Audit log retention: full preserve in MVP. Schema-forward partition-by-month + cold-storage strategy reserved.
- Migration rollback play documented per migration file header (`-- Rollback: see DEC-A32 restore command`).
- Disaster recovery one-pager: `docs/runbooks/catalog-recovery.md` (new) with steps for: corrupted snapshot, lost blob dir, broken migration, accidental mass-retire.

### DEC-A49: Test strategy — **layered: unit + integration + roundtrip + lint coverage**

**Unit (Vitest):**
- `web/lib/**` modules — schema validation, ref resolver, lint rules, search ranker.
- Pure logic only, no DB.

**Integration (Vitest + Postgres testcontainer or local test DB):**
- API routes per kind: CRUD, retire/restore, publish flow, lint flow, bulk actions, conflict handling.
- Fixture seeds reset per test via `BEGIN/ROLLBACK` transaction per case.
- MCP tool handlers tested at handler level (skip stdio bridge), parity with REST endpoints.

**Snapshot roundtrip (key test):**
- Build a representative draft entity graph (tokens → sprites → assets → buttons → panels → audio).
- Publish all → trigger snapshot export.
- Read every per-kind file in `Assets/StreamingAssets/catalog/`.
- Assert: every entity round-trips, slug stable, refs resolve, NULL refs only where intended (DEC-A22).
- Re-import via in-memory C# snapshot loader (shared serialization contract package, validated by Roslyn-side test).

**Lint coverage:**
- Each rule under `tools/scripts/lint-rules/` ships a sibling `*.test.ts`.
- Generic harness drives `entity` fixture inputs, asserts result severity + message.

**E2E (Playwright, smoke tier):**
- "Author flow": login → render → save → publish → snapshot → reference resolves in panel.
- Single happy-path scenario MVP. Regression net, not exhaustive.

**Validators (npm scripts, run in `validate:all`):**
- `validate:catalog-spine` — schema invariants per DEC-A8/A24/A38.
- `validate:trgm-indexes` (DEC-A36).
- `validate:capability-coverage` (DEC-A33).
- `validate:mcp-catalog-coverage` (DEC-A43).
- `validate:blob-roots` (DEC-A25).
- `validate:lint-rules-registry` — every TS lint module is registered in DB.
- All gate CI red.

**CI matrix:**
- Web build + Vitest + Playwright smoke + validators on every PR.
- Snapshot roundtrip test runs against a Unity batchmode harness via `unity:testmode-batch` scenario `catalog-snapshot-roundtrip` (new). Optional in PR, required nightly.

### DEC-A48: API error format + REST conventions — **structured envelope, idempotent semantics**

- Every `/api/catalog/*` route conforms to:
  - **200** body `{ ok: true, data: { ... } }` for read.
  - **200** body `{ ok: true, data, audit_id }` for mutate.
  - **4xx/5xx** body `{ ok: false, error: { code, message, details? }, retry_hint? }`.
- Error code taxonomy:
  - `validation` — schema/field-level (per-field details list).
  - `stale` — DEC-A38 fingerprint mismatch (carries `current_payload`).
  - `forbidden` — capability denied.
  - `not_found` — slug or version unknown.
  - `conflict` — slug taken, retired ref, cycle, etc.
  - `lint_blocked` — Layer 1 hard gate (DEC-A30) — carries failed gate id list.
  - `queue_full` — DEC-A40 backpressure (`retry_hint.after_seconds`).
  - `internal` — uncaught; safe message, full trace in audit_log.
- Idempotency: mutate routes accept optional `Idempotency-Key` header. Server stores `(actor, key, response_payload, response_status)` for 24h. Replays return cached response. Required on render-enqueue + bulk actions (prevents accidental double-fire).
- Pagination: every list route uses cursor-based (`cursor`, `limit`, default 50, max 200). No offset pagination (avoids large-table regression).
- Filtering: shared query parser. `?filter=kind:sprite,retired:false,tags:building+stone` semicolon-separated, AND across keys, comma OR within key.
- Sorting: `?sort=-updated_at,slug` minus prefix = desc.
- Time fields ISO 8601 with timezone in responses; UNIX ms accepted on input.
- Response shape validated by zod schema co-located with route. Schema exported for client TS types (no drift).
- Rate limiting placeholder: middleware reads `users.role` rate config. Defaults open in MVP; data path ready for hosted.

### DEC-A47: MVP seed archetypes — **opinionated minimum, demonstrates every kind**

Day-one seed shipped via migration `0016_catalog_seed_archetypes.sql` so the console boots usable.

- **sprite** kind seeds:
  - `building_residential_small_v1` — geometry params (width, height, window_count, story_count), palette token, roof_style enum.
  - `tree_v1` — species enum, height_variance, leaf_density.
  - `decor_small_v1` — generic small-prop placeholder for content fill.
- **asset** kind seeds:
  - `zone_building_v1` — accepts world sprite + button icon + cost economy params.
- **button** kind seeds:
  - `primary_button_v1` — illuminated style, icon slot, label slot, on_click_audio ref.
  - `icon_button_v1` — icon-only, square footprint.
- **panel** kind seeds:
  - `panel_grid_v1` — header + body + footer slots, body up to 12 children.
  - `dialog_v1` — modal panel, header + body + 1–2 action footer slots.
- **audio** kind seeds:
  - `ui_click_v1`, `ui_hover_v1`, `ambient_loop_v1`.
- **pool** kind seeds:
  - one per primary subtype (`residential_light_building`, `residential_dense_building`, `commercial_light_building`, etc.) — empty member sets, pre-tagged.
- **token** kind seeds:
  - `default_palette_v1`, `default_frame_v1`, `default_font_v1`, `default_motion_v1`, `default_illumination_v1`.

Each seed archetype carries:
- minimal `params_schema_json` exercising 2–4 widget kinds (DEC-A45).
- `ui_hints_json` with one preset.
- demo render-run row (admin-actor) producing one `pending` variant in `var/blobs/seed/` (DEC-A41) — shows the full pipeline without fresh user inputs.

Re-runs of seed migration are idempotent (slug uniqueness + ON CONFLICT DO NOTHING).

### DEC-A46: Archetype version + breaking-change handling — **C) Versioned schemas + migration helpers + lazy entity bumps**

- `archetype` is itself a `catalog_entity` with `kind = 'archetype'`. Each `archetype_version` row carries:
  - `params_schema_json` — JSON Schema (or Pydantic dump).
  - `ui_hints_json` (DEC-A45).
  - `target_kind` — what entity kind this archetype produces (`sprite`, `button`, `panel`, `audio`, `pool`, `token`).
  - `migration_hint_json` — optional mapping from prior version's params to this version's params (rename, default fill, transform).
- Entities pin archetype version: `entity_version.archetype_version_id`. Pinned forever (immutable per DEC-A8).
- Edit flow on draft:
  - Author opens entity. UI compares pinned `archetype_version_id` to archetype's `current_published_version_id`.
  - If mismatch: banner "Newer archetype available (v3 → v5). [Upgrade] [Compare]".
  - Upgrade applies `migration_hint_json`-driven transform → produces new `params_json` validated against new schema. Drift surfaced as warnings (e.g. "field `window_count` removed, value 4 dropped").
  - Author confirms or rejects. Migration writes new draft revision; old pinned version untouched.
- Publish flow remains: every publish freezes `(archetype_version_id, params_json)` pair. Re-publishing without upgrade keeps old archetype version pinned.
- Hard rules:
  - Archetype version publish does NOT auto-bump consumers (avoids forced cascading invalidation; matches DEC-A44 ripple model only for tokens).
  - Retiring an archetype version does NOT unpin existing entity versions.
  - Retiring archetype entity = hard block while any published entity_version pins one of its versions (mirrors DEC-A23 hard block on retired refs).
- Lint: `archetype.consumers_on_outdated_version` info-level rule surfaces drift as a dashboard counter, never blocks.
- Schema-forward: codified migration as TS/Python plugin (per archetype) reachable via additive `migration_module_path` column. Default JSON-mapping covers 80%.

### DEC-A45: Parametric render form — **C) Schema-driven generated UI (sliders/knobs/pickers)**

- Render form auto-generates from pinned `archetype_version.params_schema` + sidecar `ui_hints_json`. No hand-coded form per archetype.
- Field types → widgets:
  - `int`, `float` → slider with numeric input. `min`/`max`/`step` from schema. `ui_hint.preset_marks` puts ticks at named values.
  - `enum` → segmented control if ≤6 values, dropdown if more.
  - `bool` → toggle.
  - `color` → hex picker + palette-token quick-pick (resolves via `TokenCatalog`).
  - `entity_ref` → searchable dropdown filtered by `accepts_kind[]` (DEC-A36 search).
  - `array` → list editor with add/remove + per-item nested form.
  - `object` → grouped section, optional collapsible per `ui_hint.collapsed_default`.
- `ui_hints_json` sidecar (per archetype version):
  ```json
  {
    "groups": [
      { "label": "Geometry", "fields": ["width","height","window_count"], "icon": "ruler" },
      { "label": "Style",    "fields": ["palette","trim_style"],          "icon": "palette" }
    ],
    "field_overrides": {
      "window_count": { "widget": "stepper", "step": 1 },
      "palette":      { "widget": "token_picker", "kind": "palette" }
    },
    "preset_buttons": [
      { "label": "Default",  "params": { /* full param set */ } },
      { "label": "Dense",    "params": { "window_count": 12 } }
    ]
  }
  ```
- Shared component `<ArchetypeParamsForm>` reads schema + hints, renders. Form state mirrors `params_json` shape directly.
- Validation: live client-side via JSON Schema; server-side re-validates on submit (defense in depth, matches DEC-A30 hard gate).
- Preset buttons one-click load. Reset button reverts to archetype defaults.
- Schema-forward: new field types (`audio_clip_picker`, `coordinate_picker_2d`, `gradient_editor`) = additive widget registry. Archetype authors add `widget` hint, no form-engine change.
- Authoring of `ui_hints_json` lives on the archetype edit screen, alongside `params_schema` (JSON editor + form preview side-by-side).

### DEC-A44: Token edit propagation — **A) Token publish ripples; no re-publish of consumers**

- Tokens (palette, frame_style, font_face, motion_curve, illumination) are global theme data. Edits land, publish freezes a new `entity_version`, snapshot export ships new token data.
- Consumers reference tokens by **slug + token_role** (e.g. `palette.primary`), not by token version pin. Snapshot table for tokens carries the latest published version per slug.
- Unity binders look up tokens by slug at runtime via `TokenCatalog`. Material/shader effects (`IlluminatedButton` glow, `ThemedPanel` border) pull live token values → ripple is automatic on `CatalogReloaded` event (DEC-A21).
- Implication: token edits are **global visual changes**. UI surfaces this with a banner on the token edit screen: "Editing this changes every entity using this slug. Affected: 47 entities (link)."
- Audit log captures token publish + computed `affected_entity_count`.
- Token retire = hard block if any published entity still references it (DEC-A23 hard block extension). Author must repoint consumers first.
- Schema-forward:
  - "Pin token version" mode (option B) reachable as additive `entity_version.token_pins JSONB` per consumer; default empty = ripple, populated = pinned. No core change.
  - "Preview ripple" widget (option C) = new endpoint `/api/catalog/tokens/{slug}/impact` returning aggregated visual diff sample. Reachable additive.

### DEC-A43: MCP catalog tool surface — **C) Full CRUD parity + bridge tools (agent-automation surface)**

MCP tools mirror REST endpoints 1:1, registered in `tools/mcp-ia-server/src/index.ts` with proper schemas (per `terminology-consistency-authoring` rule). All tools capability-gated by actor role.

**Read tools (extend existing):**
- `catalog_list` — already exists; extend with `kind`, `include_retired`, `tags[]`, `pinned_only` filters.
- `catalog_get` — already exists; add `version_id` optional to fetch frozen version snapshot.
- `catalog_search` — new; trgm-fuzzy search (DEC-A36).
- `catalog_refs_query` — new; outbound + inbound graph slice (DEC-A42), `direction` + `hops` params.
- `catalog_diff_versions` — new; returns normalized diff DTO between two version_ids (DEC-A37).
- `catalog_render_run_list` — new; filter by archetype/user/date.
- `catalog_render_run_get` — new; full row + variant disposition.
- `catalog_snapshot_list` — new; filter by tag/pin/auto.
- `catalog_snapshot_get` — new; manifest contents + entity_versions[].
- `catalog_audit_log_query` — new; filter by actor/target/date.
- `catalog_lint_results` — new; per-entity or global current-state lint findings.

**Mutate tools (new, mirror REST):**
- `catalog_entity_create` / `catalog_entity_edit_draft` / `catalog_entity_publish` / `catalog_entity_retire` / `catalog_entity_restore` / `catalog_entity_delete_draft`.
- `catalog_entity_bulk` — accepts `action` enum + `entity_ids[]` + per-action params, single-transaction (DEC-A35).
- `catalog_pool_create` / `catalog_pool_edit` / `catalog_pool_member_add` / `catalog_pool_member_remove` / `catalog_pool_set_primary_subtype`.
- `catalog_panel_child_set` — replace child tree for a panel-draft (DEC-A27).
- `catalog_render_run_enqueue` — body `{ archetype_id, archetype_version_id, params_json }` → returns job_id (DEC-A40).
- `catalog_render_variant_save` — body `{ run_id, variant_idx, slug, display_name?, tags?, bind_to? }` (DEC-A41).
- `catalog_render_variant_discard`.
- `catalog_snapshot_export` — body `{ note?, tags?, manual_pin? }` → returns snapshot_id (DEC-A39 manual path).
- `catalog_snapshot_retire`.
- `catalog_lint_run` — runs Layer 2 soft lints on entity-or-version, returns `LintResult[]` without mutating (DEC-A30).
- `catalog_archetype_create` / `catalog_archetype_version_publish` (DEC-A17 archetype lifecycle).
- `catalog_token_*` (palette/frame_style/font_face/motion_curve/illumination CRUD; DEC-A14).

**Bridge tools (cross-tier):**
- `catalog_preview_in_unity` — wraps DEC-A28 push-to-Editor handler. Body `{ entity_id, version_id? }`. Returns screenshot blob URI from Unity bridge. Internally delegates to `unity_bridge_command` with kind `catalog_preview_load`.
- `catalog_unity_compile_after_promote` — convenience wrapper: promotes blob → `Assets/`, calls `unity_compile` to gate on import errors.
- `catalog_blob_resolve` — debug helper: returns local path for `gen://` URI via `BlobResolver`.

**Conventions:**
- All mutating tools require `If-Match`-equivalent fingerprint param (`expected_updated_at`) per DEC-A38. Stale fingerprint → tool error with `current_payload` for retry.
- Every tool registered with full JSON schema (per project invariant; no descriptor lag).
- Tool descriptions caveman-style (per `agent-output-caveman` rule), examples normal English where appropriate.
- Capability check inside tool handler before any DB access. Reject with structured `forbidden` error.
- Tools ship in groups via `tools/mcp-ia-server/src/tools/catalog/{group}.ts`:
  - `entity.ts`, `pool.ts`, `panel.ts`, `render.ts`, `snapshot.ts`, `lint.ts`, `archetype.ts`, `token.ts`, `audit.ts`, `bridge.ts`.

**Validator:**
- `validate:mcp-catalog-coverage` — asserts every REST endpoint under `web/app/api/catalog/**` has a registered MCP tool counterpart. CI gate.

**Use cases unlocked:**
- Agent loops: "scan all retired sprites, find any still referenced by published panels, surface in dashboard issue."
- Mass authoring: "for every residential subtype, render 3 variants from archetype X with palette token Y, save best per pool."
- Automated lint sweeps: pre-commit hook calls `catalog_lint_run` over all dirty drafts.
- Bridge automation: render → save → preview-in-unity → diff vs previous version, all from single agent turn.

**Schema-forward:**
- Streaming tools (e.g. live render progress) reachable later as MCP streaming; today: poll via `catalog_render_run_get`.
- Cross-org filters (multi-tenant) gated by capability check using `org_id` filter param. Reserved.

### DEC-A42: Reference graph view — **B) Grouped tables with counts (outbound + inbound)**

- Edit screen `References` tab splits into two collapsible sections.

**Outbound — what this entity points at:**
- Grouped by ref kind (e.g. "World sprite", "Button icon", "Panel header child", "Style token: palette", etc.). Each group header shows count.
- Per row: target slug + display_name (link), target kind, current published version pin (or "unresolved" badge per DEC-A22), source field name (e.g. `panel_child.body[2].child_entity_id`).
- Empty groups hidden.

**Inbound — who points at this entity:**
- Grouped by source kind (e.g. "Referenced by 3 panels", "Referenced by 7 buttons").
- Per row: source slug + display_name (link), source kind, source version status (draft / published / retired source).
- Counts split: `published_inbound` (count from currently-published versions) vs `draft_inbound` (count from drafts only).
- Filter chip: `Active sources only` (default) | `Include retired sources`.

**Backend:**
- Backed by single endpoint `GET /api/catalog/{kind}/{slug}/refs` returning shape:
  ```json
  {
    "outbound": [
      { "ref_kind": "world_sprite", "field_path": "asset_detail.world_sprite_id",
        "target": { "kind": "sprite", "slug": "oak_tree", "version_pin": "v3", "resolved": true } }
    ],
    "inbound": {
      "by_kind": { "panel": 3, "button": 7 },
      "rows": [
        { "source": { "kind":"panel", "slug":"build_menu" },
          "via": "panel_child.body[2]",
          "source_status": "published" }
      ]
    }
  }
  ```
- Implementation: derived view `catalog_ref_edge` materialized from `entity_version`-pinned + draft graph traversal. Refresh on publish + draft save (cheap incremental).

**Schema additions:**
```
catalog_ref_edge (
  edge_id           UUID PK,
  source_entity_id  UUID NOT NULL FK catalog_entity,
  source_version_id UUID NULL FK entity_version,        -- NULL = draft state
  target_entity_id  UUID NOT NULL FK catalog_entity,
  target_version_id UUID NULL FK entity_version,        -- pinned at publish; NULL on draft or unresolved
  ref_kind          TEXT NOT NULL,                      -- 'world_sprite' | 'panel_child' | 'token_palette' | ...
  field_path        TEXT NOT NULL,                      -- structural locator string
  is_active         BOOLEAN GENERATED ALWAYS AS (...)   -- excludes retired sources
)
CREATE INDEX catalog_ref_edge_target ON catalog_ref_edge (target_entity_id, ref_kind);
CREATE INDEX catalog_ref_edge_source ON catalog_ref_edge (source_entity_id);
```

**UI niceties:**
- "Find unresolved" filter on outbound table → highlights rows where `version_pin` is null (DEC-A22 lenient cases).
- Inbound table click-through opens source's edit screen with `References` tab pre-focused on this entity → 1-hop navigation feels graph-like without renderer cost.

**Capability:**
- `catalog.entity.read` covers refs view. No special perm needed.

**Schema-forward:**
- Graph viz (option C) = additive React component consuming same `/refs` endpoint recursively, capped at N hops. No backend or schema change.
- Bulk impact preview ("retire this entity → 3 buttons + 1 panel will have unresolved ref") already implementable from `catalog_ref_edge` query — wired into DEC-A23 retire confirm dialog.

### DEC-A41: Render output disposition — **C) Ephemeral blobs; explicit "Save as sprite" promotes**

- Render finishes → outputs land as `var/blobs/{run_id}/{variant_idx}.png` (DEC-A25) + `render_run` row (DEC-A26). **Zero `catalog_entity` rows created.**
- Render result UI (modal or `/catalog/render-runs/{run_id}` page) shows variant thumbnails grid. Per-variant actions:
  - **Save as sprite** — opens minimal form: `slug` (suggested from archetype + run + variant idx), `display_name` (defaults to slug-titled), optional `tags`. Submit → creates `catalog_entity (kind=sprite, status=draft)` + `entity_version` (draft) + `sprite_detail` row pointing at `gen://{run_id}/{variant_idx}`.
  - **Save and bind to asset** — like "Save as sprite" + extra picker for target asset entity + slot (`world` / `button` / etc.). Single transaction.
  - **Discard** — flags variant `discarded_at` in `render_run.variant_disposition_json` (additive column, no entity created). Hidden from default render-run view; "show discarded" toggle reveals.
  - **Re-render variant** — re-enqueues just this variant with same params (rare; mostly for non-deterministic archetypes).
- Default disposition: all variants `pending`. Render-run row visible in `/catalog/render-runs` until at least one variant promoted (visual cue: yellow chip "0/4 saved"). Author can leave entire run pending.
- GC interaction:
  - `render_run` rows with all variants `discarded` AND not referenced by any `entity_version.source_run_id` → eligible for deletion after 30 days (matches DEC-A29 grace).
  - Sweep removes blob dir `var/blobs/{run_id}/` only when **no** variant is referenced by any `sprite_detail.source_uri`.
- Schema additions:
  ```
  ALTER TABLE render_run
    ADD COLUMN variant_disposition_json JSONB DEFAULT '{}';
    -- shape: { "0": { "state":"saved", "entity_id": "...", "saved_at":"..." },
    --         "1": { "state":"discarded", "discarded_at":"..." },
    --         "2": { "state":"pending" } }

  ALTER TABLE entity_version
    ADD COLUMN source_run_id UUID NULL FK render_run,
    ADD COLUMN source_variant_idx INT NULL;
  ```
- Lineage: every sprite version traces back to its `render_run` (provenance trail). Versions tab on sprite shows "Rendered from archetype X v3 with params {...}".
- Schema-forward:
  - Bulk-save-all button (option B fallback) = additive UI calling per-variant endpoint in a loop. Zero schema change.
  - Auto-promote heuristics (e.g. "save best variant by some quality metric") reachable via additive worker step writing `recommended_variant_idx` to `render_run`. UI hint, not auto-action.

### DEC-A40: Render queue — **B) Single FIFO queue, one render at a time**

- All render requests funnel through `job_queue` (DEC-A39 reuse) with `kind = 'render_run'`. Concurrency = 1.
- Worker process: `tools/scripts/render-worker.ts` (or part of unified job-worker). On startup, claims next `queued` row of kind `render_run`, sets `running`, dispatches to FastAPI sprite-gen tool (DEC-A1), writes outputs into `var/blobs/{run_id}/` (DEC-A25), inserts `render_run` row (DEC-A26), marks job `done`.
- Failure → job `failed` with `error`, blob dir cleaned, `render_run` row not inserted. UI shows red status + retry button.
- API contract:
  - `POST /api/render/runs` body `{ archetype_id, archetype_version_id, params_json }` → returns `job_id`.
  - `GET /api/render/runs/{job_id}` → polls status. Returns position in queue when `queued`.
- UI behaviour:
  - "Generate now" click → POST → modal with **spinner + position indicator** ("Position 3 in queue") + ETA estimate (rolling avg of last 10 runs of same archetype).
  - Polls `GET` every 1s. Cancel button hidden (per earlier decision: no cancel).
  - On `done`: modal swaps to render result thumbnails (variants), promote/discard actions.
  - On `failed`: shows error + Retry (re-enqueues with same params + `parent_run_id`).
- Backpressure: hard cap `job_queue` queued of kind `render_run` = 50. New POST when full → 429 + "Queue full, wait for current jobs."
- Worker liveness: heartbeat updates `started_at` every 30s while running. Stale rows (running for >2× p99 duration without heartbeat) auto-marked `failed` by sweep.
- Audit log: `render.run.enqueued` + `render.run.completed` events.
- Capability: `render.run` (author + admin).
- Schema-forward:
  - Parallel slots (option C) = config var `MAX_PARALLEL_RENDERS`. Worker reads config; queue logic unchanged. Reachable as 1-line tweak.
  - Per-archetype priority lanes (e.g. previews vs batch) = additive `priority INT` column on `job_queue` + ORDER BY in worker claim. Reachable as additive.
  - Hosted: swap FastAPI subprocess → remote HTTP worker; queue stays Postgres-backed.

### DEC-A39: Snapshot export trigger — **C) Auto-on-publish (debounced) + manual tagged trigger**

- Two trigger paths.

**Auto path (continuous freshness):**
- Every successful publish on any entity enqueues a `snapshot_rebuild` job into the `job_queue` table (new, see schema-forward).
- Debouncer: if a job is already pending (status `queued`) within last 5s, skip enqueue. If running, mark `re_run_requested = true` so worker re-runs immediately after current finish.
- Worker (`tools/scripts/snapshot-export-worker.ts`, runs as long-lived Node process or on-demand `npm run catalog:snapshot:rebuild`) drains queue, runs export, writes new snapshot files atomically (temp dir → rename), inserts new `catalog_snapshot` row with `note = 'auto: post-publish'`.
- Auto-snapshot row carries `is_auto = true` flag. Treated as ephemeral — eligible for GC under DEC-A29 once unreferenced AND older than 7 days, even if "pinned" by export trail (auto rows are not protected).

**Manual path (release tagging):**
- "Export snapshot" button on `/catalog/snapshots` and dashboard. Opens dialog:
  - **Note** (required): release name / commit message (e.g. "v0.4-pre-alpha-balance-pass").
  - **Tag** (optional, free text, indexed): e.g. `release`, `playtest-04`. Multiple allowed.
  - **Pin permanently** checkbox: blocks GC entirely on this snapshot regardless of age (sets `manual_pin = true`).
- Manual run blocks UI with progress modal, returns `snapshot_id` link on success. `is_auto = false`.

**Schema additions:**
```
job_queue (
  job_id          UUID PK,
  kind            TEXT NOT NULL,          -- 'snapshot_rebuild' | future kinds
  status          TEXT NOT NULL,          -- 'queued' | 'running' | 'done' | 'failed'
  payload_json    JSONB DEFAULT '{}',
  re_run_requested BOOLEAN DEFAULT false,
  enqueued_at     TIMESTAMPTZ DEFAULT now(),
  started_at      TIMESTAMPTZ NULL,
  finished_at     TIMESTAMPTZ NULL,
  error           TEXT NULL
)

-- DEC-A29 catalog_snapshot extended:
ALTER TABLE catalog_snapshot
  ADD COLUMN is_auto      BOOLEAN DEFAULT false,
  ADD COLUMN tags         TEXT[] DEFAULT '{}',
  ADD COLUMN manual_pin   BOOLEAN DEFAULT false;
CREATE INDEX catalog_snapshot_tags_gin ON catalog_snapshot USING GIN (tags);
```

**Failure handling:**
- Worker logs `error` into job row + emits `audit.snapshot_export_failed` event. Last-good snapshot stays live (Unity DEC-A21 already keeps old on reload failure).
- Dashboard widget shows last 3 snapshot statuses; red if last failed.

**Capability:**
- `catalog.snapshot.export_manual` for tagged releases (admin only by default).
- Auto path runs as system actor (no user attribution beyond triggering publish).

**Schema-forward:**
- Multiple snapshot channels (e.g. `dev`, `staging`, `release`) — additive `channel TEXT` column; same export code, separate manifest files. Reachable once hosted.

### DEC-A38: Concurrent edit safety — **B) Optimistic concurrency via updated_at fingerprint**

- Every mutable row (`catalog_entity`, `entity_version`-draft, `*_detail`, `panel_child`, `pool_member`) carries `updated_at TIMESTAMPTZ DEFAULT now()` + DB trigger to refresh on UPDATE.
- API contract:
  - `GET` returns row + `updated_at` (also exposed as `etag` HTTP header for native browser cache support).
  - `PATCH` / `PUT` / `DELETE` requires `If-Match: {etag}` header (or `expected_updated_at` body field as fallback).
  - Server compares received fingerprint against current `updated_at`. Mismatch → `409 Conflict` with body `{ error: 'stale_entity', current_updated_at, current_payload }`.
- UI handling:
  - Edit screen captures `updated_at` on load.
  - Save click sends fingerprint. On 409: modal **"Stale edit — someone else changed this"**. Two buttons: **Reload (lose my changes)** | **Show diff** (renders 3-way: original-loaded vs current-server vs my-pending). User picks per-field which to keep, then re-saves with refreshed fingerprint.
- Composite saves (panel children, pool members): one transaction, fingerprint check on parent + each child row. Any mismatch → whole save rejected, surface granular conflict per row.
- Form auto-save (drafts): every 30s background `PATCH` to draft `entity_version`. Conflict → silent retry once with refresh, then surface modal.
- Audit log (DEC-A33): conflict outcomes emit `catalog.entity.save_conflict` event for telemetry.
- Schema-forward:
  - Soft lock (option C) = additive `entity.locked_by_user_id`, `entity.locked_at`, TTL sweep job. Layer atop optimistic check. No API breakage.
  - CRDT-style live collab reachable far later — not reserved.
- Validation: `validate:catalog-spine` asserts trigger exists on every mutable table.

### DEC-A37: Diff + history view — **B) Side-by-side JSON diff (params_json + detail)**

- Edit screen `Versions` tab lists every `entity_version` row chronologically (newest first), columns: version label (`v{n}`), `created_at`, author, lint summary, pin badges (snapshot count), action buttons.
- Action buttons per row: **View** (read-only render of that version), **Diff vs current**, **Diff vs…** (picker for any other version), **Restore as draft** (creates new draft with this version's payload — does NOT mutate frozen version).
- Diff viewer: left pane = older version, right pane = newer. Renders as canonicalized JSON with deep-diff highlighting:
  - **Added** keys/arrays = green, prefix `+`.
  - **Removed** = red, prefix `−`.
  - **Changed** = yellow, both old and new shown inline with arrow.
  - Unchanged keys collapsed by default, "expand context" toggle.
- Diff scope per kind:
  - Always: `entity.display_name`, `entity.tags`, `entity_version.params_json`.
  - Plus all `*_detail` columns for the kind (sprite → `assets_path`, `pixels_per_unit`, `pivot`, `palette_hash`; panel → child tree from `panel_child` rows pinned at version; audio → `loudness_lufs`, `peak_db`, `duration_ms`, etc.).
  - Outbound refs (DEC-A22 resolve targets) shown by **slug** (stable per DEC-A24), not UUID — diff stays human-readable.
- Implementation: shared component `<EntityVersionDiff>` consumes a normalized DTO `{ left: VersionPayload, right: VersionPayload }` from `GET /api/catalog/{kind}/{slug}/versions/diff?a={va}&b={vb}`. Backend joins entity + detail + child rows, returns flat JSON.
- Audit log integration: each diff view logs `audit.read` event (DEC-A33) at info level — useful for "who looked at version X" trail.
- "Restore as draft" mechanic: creates new entity_version draft (or overwrites existing draft if any) with payload from picked version + bumps `parent_version_id` linkage for lineage. Does not auto-publish.
- Schema-forward:
  - Per-kind visual diff (option C) = additive renderers under `web/components/catalog/diff/{kind}.tsx`. Tab toggle "JSON / Visual" appears once any kind ships a visual renderer. No backend change.
  - Multi-version timeline (3+ versions side by side) reachable via additive UI; same diff API parameterized.
- Capability: `catalog.entity.read` to view diff, `catalog.entity.edit` to "Restore as draft".

### DEC-A36: Catalog search — **B) Slug + display_name + tags, trigram-fuzzy, cross-kind grouped**

- Header search bar visible on every `/catalog/*` page. Keyboard shortcut `/` focuses input.
- Search index columns (per `catalog_entity` row): `slug`, `display_name`, `tags TEXT[]` (DEC-A35 reserved column).
- Matching strategy: Postgres `pg_trgm` similarity. Threshold 0.3, ranked by `similarity(query, slug) * 2 + similarity(query, display_name) + tag_match_bonus`. Slug weighted highest (most stable identifier).
- Index migration:
  ```sql
  CREATE EXTENSION IF NOT EXISTS pg_trgm;
  CREATE INDEX catalog_entity_slug_trgm  ON catalog_entity USING GIN (slug gin_trgm_ops);
  CREATE INDEX catalog_entity_name_trgm  ON catalog_entity USING GIN (display_name gin_trgm_ops);
  CREATE INDEX catalog_entity_tags_gin   ON catalog_entity USING GIN (tags);
  ```
- Result UI: dropdown panel under search bar, results grouped by kind (matches DEC-A34 sidebar groups), max 8 per kind, "show all in {kind}" footer link → kind list page with query pre-applied as filter.
- Filters: kind chips above results to narrow (`only sprites`, `only retired`, `only drafts`). Persist last-used filter in `localStorage`.
- API: `GET /api/catalog/search?q={query}&kind={kind?}&include_retired={bool}`. Capability `catalog.search` (granted to `viewer`+).
- Performance budget: ≤120ms on 50k entities. Trigram + LIMIT keeps it well under.
- Empty state: when query has 0 hits, suggest "create new" CTA per kind in result groups.
- Schema-forward:
  - Full-text body search (option C) = additive `search_doc tsvector` materialized column + scheduled refresh + new `tsvector` index. No API change beyond optional `mode=fulltext` flag.
  - Cross-entity reference search (find all entities pointing at this slug) = separate endpoint `/api/catalog/refs/in?entity_id=`, surfaced on entity edit `References` tab (DEC-A34).
- Validation: `validate:trgm-indexes` asserts indexes exist post-migration.

### DEC-A35: Bulk operations — **B) Multi-select bulk actions, single-transaction**

- Every list page exposes per-row checkbox + header "select all" + "select all matching filter" (paginated-aware).
- Selection bar appears at top of list when ≥1 row checked: count + action buttons. Buttons gated by capability (DEC-A33).

**Bulk action set (MVP):**
- **Retire** — soft-retires all selected (DEC-A23). Confirms count + lists inbound published refs that will become unresolved.
- **Restore** — clears `retired_at` for retired selections.
- **Publish** — runs publish pipeline (lints + hard gates per DEC-A30) on each selected entity. Lint warnings aggregated into one dialog. Hard-gate failures itemized; user opts to publish-the-rest or abort. All-or-rest mode toggle.
- **Add to pool** — appears on sprite/asset list. Picks target pool, optional primary-subtype tagging applied uniformly.
- **Remove from pool** — removes selected from chosen pool.
- **Tag** — adds free-form tag(s) to `entity.tags TEXT[]` (reserved column, additive). Tag list searchable, no enforcement.
- **Delete drafts** — only entities with zero published versions; hard-deletes row + cascade. Hard gate: refuses if any version is pinned.

**Transaction model:**
- One DB transaction per bulk action. Rollback on first hard-gate failure unless user picked "skip-and-continue" mode.
- Audit log (DEC-A33) emits one row per affected entity + one summary row (`bulk_action` + count).
- UI shows progress modal: "47/200 retired". Final report: succeeded, skipped (with reason), failed.

**Pagination interaction:**
- "Select all matching filter" stores filter predicate + count, not row ids. Backend re-applies predicate at action time → no stale-selection bugs.

**Schema-forward:**
- CSV import/export (option C) reachable as additive — same backend bulk endpoints, new UI route `/catalog/{kind}/import`. No core change.
- Async background bulk jobs (for huge sets) reachable via job queue table — bulk endpoint returns job_id, UI polls. Today: synchronous, OK at expected scale.

### DEC-A34: Authoring console navigation — **C) Grouped sidebar (Content / Configuration / Operations)**

- Root route: `/catalog` redirects to `/catalog/dashboard`.
- Top-level layout: persistent left sidebar with three labelled groups + collapsible sections.

**Group: Content** (entities authors actively make)
- `/catalog/sprites` — list, filter, create, render-from-archetype.
- `/catalog/assets` — building/zone-bound assets, sprite slot bindings.
- `/catalog/buttons` — in-game UI buttons.
- `/catalog/panels` — in-game UI panel composition.
- `/catalog/audio` — sfx/music/ui/ambient clips + variants.

**Group: Configuration** (data that shapes content)
- `/catalog/pools` — subtype variant bags + primary subtype tagging.
- `/catalog/tokens` — game DS palette / frame_style / font_face / motion_curve / illumination.
- `/catalog/archetypes` — parametric templates per kind, schema versions.

**Group: Operations** (state of the system)
- `/catalog/dashboard` — landing tile: unresolved refs widget, lint summary, recent publishes, render queue, broken-snapshot alerts.
- `/catalog/snapshots` — snapshot list, export trigger, retire, manifest viewer.
- `/catalog/render-runs` — global render-run history (cross-archetype), filter by archetype/user/date.
- `/catalog/audit-log` — append-only event stream.
- `/catalog/settings` — lint rules toggle, GC config, capability matrix viewer.

- Per-kind list pages share a layout component: list panel left, edit/detail panel right (split). Tabs on edit: **Edit / Versions / References (in/out) / Lints / Audit**.
- Header bar: search across all kinds (kind-prefixed slug match), actor email + role badge, "Preview in Unity" status indicator (online/offline), snapshot freshness chip ("3 entities published since last snapshot").
- Active/retired toggle: every list page has filter chips `Active` (default) | `Retired` | `All`. Wires into DEC-A23 retired_at filter.
- URL convention: `/catalog/{kind}/{slug}` for direct entity link. `/catalog/{kind}/{slug}/v/{version_id}` for pinned-version view (read-only). Stable slugs (DEC-A24) → URLs survive renames-via-republish.
- Schema-forward: future kind = sidebar entry under appropriate group, page reuses shared list+detail layout. Zero nav refactor.
- Caveman exception: page body strings full English (per `web/lib/design-system.md` page-copy rule); identifiers + IA prose stay caveman.

### DEC-A33: Auth hardening path — **C) Reserved schema + capability matrix from day one**

- `users` table provisions hosted-ready columns at MVP creation:
  ```
  users (
    id              UUID PK,
    email           CITEXT UNIQUE NOT NULL,
    display_name    TEXT NOT NULL,
    role            TEXT NOT NULL DEFAULT 'admin',     -- single-user MVP defaults all to admin
    org_id          UUID NULL,                         -- reserved; NULL until multi-tenant
    last_login_at   TIMESTAMPTZ NULL,
    created_at      TIMESTAMPTZ DEFAULT now(),
    retired_at      TIMESTAMPTZ NULL                   -- soft-retire, mirrors entity pattern
  )
  ```
- Capability matrix as data:
  ```
  capability (
    capability_id   TEXT PK                            -- e.g. 'catalog.entity.publish'
  )
  role_capability (
    role            TEXT NOT NULL,
    capability_id   TEXT NOT NULL FK capability,
    PRIMARY KEY (role, capability_id)
  )
  ```
- Seed roles + capabilities (MVP):
  - `admin` → all capabilities.
  - `author` → entity create/edit/draft, render, publish, lint, preview-in-unity. **No** retire/delete cross-author content, no capability matrix edit, no GC trigger, no snapshot retire.
  - `viewer` → read all, no mutate.
  - Capabilities seeded: `catalog.entity.{create,edit,publish,retire,delete}`, `catalog.snapshot.{export,retire}`, `render.run`, `preview.unity_push`, `lint.config_edit`, `auth.role_assign`, `gc.trigger`, `audit.read`.
- API middleware: every route declares `requires: 'capability_id'`. Middleware joins `users.role` → `role_capability`. Route handler reads only the capability check.
- Audit log:
  ```
  audit_log (
    id              BIGSERIAL PK,
    actor_user_id   UUID FK users,
    action          TEXT NOT NULL,                     -- e.g. 'catalog.entity.published'
    target_kind     TEXT NULL,                         -- 'entity' | 'snapshot' | 'role'
    target_id       UUID NULL,
    payload_json    JSONB NULL,                        -- before/after diff or context
    created_at      TIMESTAMPTZ DEFAULT now()
  )
  ```
  Append-only. No update/delete API. Index on `(actor_user_id, created_at)` + `(target_kind, target_id)`.
- Hosted future = swap NextAuth provider (magic-link → SSO/email), populate `org_id`, expand seed `role_capability` rows. **Zero API rewrites.**
- Schema-forward: per-org capability override (`org_role_capability` table) reachable as additive. Per-entity ACL (`entity_acl`) reachable as additive. No core change.
- Validation: `validate:capability-coverage` script asserts every API route's `requires` capability exists in `capability` table.

### DEC-A32: Pre-migration audit task — **C) Audit script + DB snapshot freeze**

- New script `tools/scripts/audit-catalog-pre-spine.ts`. Runs read-only against current DB. Emits Markdown report.
- Report sections:
  - **Row counts** per existing table (`catalog_asset`, `catalog_sprite`, `catalog_economy`, `catalog_spawn_pool`, `catalog_pool_member`).
  - **FK integrity** — orphan rows where target is missing.
  - **Slug collisions** — duplicate slugs that would violate DEC-A24 `(kind, slug)` uniqueness once kinds collapse into spine.
  - **Duplicate fingerprints** — `catalog_sprite.build_fingerprint` collisions that may indicate redundant rows to dedupe.
  - **Pool integrity** — pools with zero members, members pointing at retired/missing assets, primary subtype unset.
  - **Path issues** — `assets_path` values that don't resolve on disk.
  - **Pre-spine field census** — every column not yet mapped to a spine column, flagged as either "→ entity_version.params_json", "→ *_detail", or "drop".
- Output paths:
  - `docs/audits/catalog-pre-spine-{YYYY-MM-DD}.md` (human-readable).
  - `docs/audits/catalog-pre-spine-{YYYY-MM-DD}.json` (machine-readable, consumed by migration validator).
- Snapshot freeze: `tools/scripts/freeze-db-snapshot.sh` runs `pg_dump -Fc` → `var/db-snapshots/pre-spine-{YYYY-MM-DD}.dump`. Gitignored. Audit report header references snapshot filename + sha256.
- Migration `0013_catalog_spine.sql` author reads the audit's `pre-spine-field-census` block to drive the backfill SQL, not guesswork.
- Rollback play: `tools/scripts/restore-db-snapshot.sh` accepts dump path → drops + re-creates DB → `pg_restore`. Documented in migration's header comment.
- New npm script `db:audit-pre-spine` chains: snapshot → audit → opens report. Single command for the migration prep step.
- Future use: same audit shape becomes template for any future destructive migration (rename `audit-pre-{change_slug}.ts`).

### DEC-A31: Audio kind detail shape — **C) Archetype-driven params + minimal output detail table**

- `audio_detail` table holds rendered/output fields only:
  ```
  audio_detail (
    entity_id        UUID PK FK catalog_entity,
    source_uri       TEXT NOT NULL,            -- gen://run_id/0 or asset://path/to/file.ogg
    assets_path      TEXT NULL,                -- Assets/Audio/... once promoted to Unity
    duration_ms      INT NOT NULL,
    sample_rate      INT NOT NULL,
    channels         INT NOT NULL,             -- 1 mono / 2 stereo
    loudness_lufs    REAL NULL,                -- measured at promote time
    peak_db          REAL NULL,
    fingerprint      TEXT NOT NULL             -- sha256 of source bytes
  )
  ```
- Authoring fields live in `params_json` (DEC-A17 unified pattern), validated against pinned `archetype_version.params_schema`. Example archetype `ui_click_v1`:
  ```json
  {
    "category":      { "type": "enum", "values": ["sfx","music","ui","ambient"] },
    "loop_default":  { "type": "bool", "default": false },
    "volume_default":{ "type": "float", "min": 0, "max": 1, "default": 0.8 },
    "pitch_default": { "type": "float", "min": 0.5, "max": 2.0, "default": 1.0 },
    "envelope":      { "type": "object", "fields": { "attack_ms":"int","decay_ms":"int","sustain":"float","release_ms":"int" } },
    "variants":      { "type": "array", "items": { "type": "audio_ref", "max": 8 } },
    "randomize":     { "type": "object", "fields": { "pitch_jitter":"float","volume_jitter":"float" } }
  }
  ```
- Future archetypes (sound_bank, footstep_set, music_layer_stem, ambient_loop) plug in as new `archetype` entities for `kind=audio` with their own `params_schema` — zero schema change.
- Render path (mirrors sprite-gen): authoring console exposes Python-tool-driven audio synthesis (FastAPI endpoint, DEC-A1 contract). MVP can also accept upload-only (skip synth) — `source_uri` then = `upload://{run_id}/0`.
- Promote action: `BlobResolver.read(source_uri)` → copy to `Assets/Audio/Generated/{slug}.ogg` → re-measure `loudness_lufs` + `peak_db` → fill `audio_detail`. Unity consumes via `AudioCatalog` runtime binder.
- Snapshot export: `audio.json` per-kind file ships full row + resolved `assets_path`. Variants flatten into per-clip refs (similar to panel children).
- Lint hooks (DEC-A30): `audio.loudness_out_of_range` (target window configurable per category), `audio.no_variants_for_random` (random params set but variants empty), `audio.peak_clipping` (peak_db > -1).

### DEC-A30: Publish lint framework — **C) Hard gates + pluggable soft lints**

- Two-layer validation pipeline runs on every publish attempt.

**Layer 1 — Hard gates (block publish, no override):**
- `params_json` validates against pinned `archetype_version.params_schema` (Pydantic / JSON Schema).
- `slug` matches DEC-A24 regex + uniqueness + frozen-after-publish rule.
- No outbound ref points at retired entity (DEC-A23 hard block).
- Cycle check on panel children (DEC-A27).
- Required `*_detail` row exists for the kind.

**Layer 2 — Soft lints (warn, author can publish-anyway):**
- New table `publish_lint_rule`:
  ```
  publish_lint_rule (
    rule_id         TEXT PK,                   -- e.g. 'sprite.missing_ppu'
    kind            TEXT NOT NULL,             -- entity kind this applies to
    severity        TEXT NOT NULL,             -- 'warn' | 'info'
    enabled         BOOLEAN DEFAULT true,
    config_json     JSONB DEFAULT '{}',        -- per-rule thresholds (e.g. { "min_loudness_db": -23 })
    description     TEXT NOT NULL
  )
  ```
- Lint runner: `tools/scripts/lint-catalog-entity.ts` exports `runLints(entity, version, kind) → LintResult[]`. Each rule = TS module under `tools/scripts/lint-rules/{kind}/{rule_id}.ts` exporting `(entity, version, ctx) → LintResult | null`.
- MVP rule seed set:
  - `sprite.missing_ppu` — sprite has no `pixels_per_unit` set.
  - `sprite.missing_pivot` — pivot null or out-of-bounds.
  - `asset.no_sprite_bound` — asset has zero `world_sprite_id`.
  - `button.missing_icon` — button has no icon sprite ref.
  - `button.missing_label` — button label empty AND no icon.
  - `panel.empty_slot_below_min` — slot's children count < archetype min.
  - `panel.unfilled_required_slot` — required slot empty.
  - `audio.loudness_out_of_range` — placeholder until audio detail lands.
  - `pool.empty` — pool has zero members.
  - `pool.no_primary_subtype` — pool missing primary subtype tag.
  - `token.no_consumers` — orphan token (info-level only).
- Publish dialog renders results grouped by severity. Hard-gate failures block submit button. Warnings show count + expandable list + "Publish anyway" toggle.
- Per-rule overrides: `entity_version.lint_overrides_json` records `{ rule_id: 'acknowledged' }` when author publishes through warning. Audit trail.
- Authoring console settings page: enable/disable rules globally, tweak `config_json` thresholds. Per-entity override (suppress rule for this entity) reachable later via `entity.lint_overrides_json` column reserved.
- Schema-forward: future linters (Unity-side render check, accessibility contrast, color-palette mismatch) plug in as new `publish_lint_rule` rows + new TS module. No core change.

### DEC-A29: Per-version GC policy — **C) Keep snapshot-referenced + recent; GC orphans after 30 days**

- Every `entity_version` row carries computed flag `is_pinned` derived from joins:
  - Pinned IF version_id appears in any `snapshot.entity_versions[]` row of `catalog_snapshot` table (DEC-A20 export trail).
  - Pinned IF version_id is `catalog_entity.current_published_version_id` (live publish pointer).
  - Pinned IF version_id is referenced by any `panel_child.child_version_id` in a pinned panel version (transitive).
- New table `catalog_snapshot`:
  ```
  catalog_snapshot (
    snapshot_id     UUID PK,
    manifest_path   TEXT NOT NULL,             -- StreamingAssets/catalog/snapshot.manifest.json content path
    created_at      TIMESTAMPTZ DEFAULT now(),
    created_by      UUID FK user,
    entity_versions UUID[] NOT NULL,           -- flat list of all entity_version_ids in this snapshot
    note            TEXT NULL,                 -- author-provided "release tag" / commit message
    retired_at      TIMESTAMPTZ NULL           -- soft-retire snapshots; pinning lost on retire
  )
  ```
- GC sweep job (`tools/scripts/gc-catalog-versions.ts`, run nightly via cron / manual):
  1. Compute pin set across all non-retired snapshots + live publish pointers + panel transitive.
  2. Find `entity_version` rows with `is_pinned = false` AND `created_at < now() - 30d`.
  3. Delete row + cascade purge owned `*_detail` rows + dereference any `gen://` blobs no longer referenced anywhere → `var/blobs/` sweep.
- Manual pin (schema-forward): `entity_version.manual_pin BOOLEAN DEFAULT false` reserved column. UI button "Pin this version" reachable later, no GC change.
- Validation: `validate:catalog-spine` asserts no pinned version is missing from `entity_version` table (referential integrity).
- Authoring UX: edit screen "Versions" tab shows version list with `pinned` badges (snapshot icon + count of snapshots holding it). Unpinned old versions show "GC eligible in N days".
- Snapshot retire (option, additive): mark snapshot retired → its versions lose pin source → may become GC-eligible. Used to deliberately drop very old releases from disk.

### DEC-A28: Editor → Unity live preview — **B) Manual "Preview in Unity" push**

- Web edit screen for any kind with Unity-side rendering (sprite, button, panel, token) shows **Preview in Unity** button.
- Click → web POSTs to `/api/catalog/preview/push` → backend builds an ephemeral draft-snapshot fragment (just this entity + transitive deps) → calls `mcp__territory-ia__unity_bridge_command` with kind `catalog_preview_load`.
- Unity bridge handler: loads fragment into a sandboxed `PreviewCatalog` instance (separate from production `GridAssetCatalog`), instantiates target entity in `PreviewScene` (button → mounts in `PreviewCanvas`, panel → mounts as ThemedPanel, sprite → places sprite on grid origin).
- Bridge response carries screenshot path (PNG written to `var/blobs/preview/{request_id}.png`) — web shows it next to the web-side React preview for side-by-side compare.
- One-way push only. No subscription, no auto-refresh. Author re-clicks to see latest after edits.
- Bridge command lease scoped to single request (DEC-A22 / `unity_bridge_lease` semantics) — no contention with playmode.
- Preview fragment never persists in DB. Pure ephemeral wire payload.
- Schema-forward: live link (option C) = same endpoint called from web edit-form `onChange` debounced 500ms, plus toggle in user prefs. No backend or schema change.
- Failure modes: Unity not running → web shows "Editor offline, run Unity to preview". Bridge lease busy → toast "preview queued, retry in N s".

### DEC-A27: Panel composition — **C) Slot-based archetype-driven children**

- Panel archetype declares named slots in archetype `params_schema` extension `slots_schema`:
  ```json
  {
    "slots": [
      { "name": "header",  "accepts": ["button","label"], "min": 0, "max": 1 },
      { "name": "body",    "accepts": ["button","panel","label","spacer"], "min": 1, "max": 12 },
      { "name": "footer",  "accepts": ["button"], "min": 0, "max": 3 }
    ]
  }
  ```
- New table `panel_child`:
  ```
  panel_child (
    id              UUID PK,
    panel_entity_id UUID FK catalog_entity,        -- owner panel
    panel_version_id UUID NULL FK entity_version,  -- NULL on draft, set on publish snapshot
    slot_name       TEXT NOT NULL,                 -- must match archetype slots.name
    order_idx       INT NOT NULL,                  -- position within slot
    child_kind      TEXT NOT NULL,                 -- 'button' | 'panel' | 'label' | 'spacer' | 'audio'
    child_entity_id UUID NULL FK catalog_entity,   -- NULL for spacer/inline label
    child_version_id UUID NULL FK entity_version,  -- pinned at publish time
    params_json     JSONB NOT NULL DEFAULT '{}',   -- per-child overrides (label text, size, etc.)
    UNIQUE (panel_entity_id, slot_name, order_idx)
  )
  ```
- Authoring UI: panel edit screen renders one column per declared slot. Author picks child entity from dropdown filtered by `accepts[]`. Reorder via up/down arrows (drag-drop nice-to-have, not MVP).
- Validation on save:
  - Each child's `child_kind` is in slot's `accepts[]`.
  - Slot child count within `[min, max]`.
  - No cycle (`panel A → child panel B → child panel A`) — graph walk on save.
- Publish step: snapshot export resolves all `child_version_id` to currently-published versions (DEC-A22 lenient = NULL where unpublished), serializes panel + child rows into single JSON node tree under `panels.json`.
- Unity binder reads tree, instantiates `ThemedPanel` prefab, mounts children into named child transforms (must match slot names) — convention-over-config wiring.
- Schema-forward: free-canvas panels later = new archetype kind that includes `x/y/w/h/z` in `params_json`. Same `panel_child` row, different schema. No table change.

### DEC-A26: Render-run replay — **C) `render_run` table + one-click identical replay**

- New table `render_run`:
  ```
  render_run (
    run_id          UUID PK,
    archetype_id    UUID FK catalog_entity,
    archetype_version_id UUID FK entity_version,
    params_json     JSONB NOT NULL,        -- validated against archetype schema
    params_hash     TEXT NOT NULL,         -- sha256 of canonicalized params_json
    output_uris     TEXT[] NOT NULL,       -- gen://run_id/0, gen://run_id/1, ...
    build_fingerprint TEXT NOT NULL,       -- git sha + python tool version
    duration_ms     INT NOT NULL,
    triggered_by    UUID FK user,
    created_at      TIMESTAMPTZ DEFAULT now(),
    parent_run_id   UUID NULL FK render_run -- replay lineage
  )
  ```
- Author workflow surfaces:
  - **Replay (prefill)** button on any `render_run` row → opens render form pre-populated with `params_json`, archetype + version pinned. Author tweaks any knob then submits.
  - **Re-render identical** button → POSTs `render_run.params_json` verbatim with same archetype_version_id. New row inserted with `parent_run_id = source.run_id`. Useful for re-running after pipeline tool update (different `build_fingerprint`).
- UI list view per archetype: render-run history table sorted desc by `created_at`, columns: thumbnail strip, params summary chip, fingerprint, duration, replay/identical buttons.
- `params_hash` enables future de-dup ("identical params already rendered, reuse outputs?") — reserved as additive UI, not enforced in MVP.
- Snapshot export ignores `render_run` entirely — render history is authoring-only metadata, never shipped to Unity.

### DEC-A25: Logical blob storage — **B) `var/blobs/` repo-local root, swap-ready**

- Canonical blob root: `var/blobs/` at repo root. Gitignored. Created by bootstrap script.
- Render output path: `var/blobs/{run_id}/{variant_idx}.png` + sidecar `var/blobs/{run_id}/manifest.json` (params, archetype_version, build_fingerprint, render_duration_ms, timestamp).
- `gen://{run_id}/{variant_idx}` resolves through `BlobResolver` service (single class, one method `resolve(uri) → local path | URL`). Local impl reads `var/blobs/`. Hosted impl swaps to S3/GCS via env var without touching catalog rows.
- Promote action: `BlobResolver.read(uri)` → copy to `Assets/Sprites/Generated/{slug}.png` + write `catalog_sprite.assets_path`. Original blob stays in `var/blobs/` as immutable provenance trail.
- GC policy (deferred, schema-forward): `render_run` table tracks `last_referenced_at`; sweep job deletes blobs unreferenced for N days. Not built in MVP — disk is cheap on dev machine.
- Sprite-gen Python tool writes via blob root env var (`BLOB_ROOT=/abs/path/var/blobs`), not hardcoded `out/` dir. CLI default falls back to `tools/sprite-gen/out/` only when env unset (back-compat).
- Validation: `validate:blob-roots` (new) asserts every `gen://` URI in `catalog_sprite` resolves to existing file in `var/blobs/`.

### DEC-A24: Slug rules — **B) Strict format, frozen after first publish**

- `catalog_entity.slug` regex: `^[a-z][a-z0-9_]{2,63}$`. Unique per `(kind, slug)` — same slug allowed across kinds (sprite `oak_tree` + asset `oak_tree` distinct).
- Reserved prefixes: `_` (system), `tmp_` (autogen drafts). Reject on insert.
- Slug editable while entity has zero published versions. First publish flips `slug_frozen_at` — UI hides edit field after that.
- Rename post-publish = create new entity, manually re-author. No alias table in MVP.
- Snapshot export uses slug as join key for Unity-side dictionary; freezing guarantees Unity refs never break across snapshots.
- Schema-forward: `slug_alias` table (slug → entity_id, kind) reserved as future additive — enables option C without entity table change.
- Validation: `validate:catalog-spine` asserts no slug collisions, no reserved-prefix violations, no edits to frozen slugs.

### DEC-A23: Retire + restore lifecycle — **B) Soft retire, restore allowed**

- Every `catalog_entity` row carries `retired_at TIMESTAMPTZ NULL` + `retired_by user_id NULL` + `retired_reason TEXT NULL`.
- Retire action: sets `retired_at = now()`. Entity disappears from default authoring lists, search, and **picker dropdowns when authoring new outbound refs**.
- Existing `entity_version` rows referencing retired entity stay intact. Already-published snapshots keep working — frozen by design (DEC-A8 immutability).
- New publishes: publish dialog rejects if any outbound ref points at a retired entity (hard block, not lenient). DEC-A22 NULL-safety only covers unpublished refs, not retired refs — different failure mode.
- Restore action: clears `retired_at`. Entity reappears in active lists. No version or reference changes — restore is reversible mtime flip.
- UI: "Retired" tab on every list view (sprites, assets, buttons, panels, pools, tokens, archetypes, audio). Filter toggle on default list. Retire button on edit screen confirms count of inbound refs from currently-published entities.
- Schema-forward: `replaced_by_entity_id UUID NULL` column reserved (NULL in MVP, no auto-redirect logic). Future tombstone redirect (option C) lands as additive UI + export-time slug rewrite without schema change.

### DEC-A22: Publish-time reference integrity — **B) Lenient publish, NULL-safe at runtime**

- Publish never blocks on missing/unpublished refs. Outbound FK that has no `current_published_version_id` resolves to NULL in the entity_version snapshot.
- Snapshot export ships rows with NULL refs unchanged.
- Unity loader treats NULL refs as missing-asset placeholder (visible warning sprite/text), logs warning with entity slug + ref kind at boot.
- Required safety net (added to web UI):
  - Dashboard widget **Unresolved References** — count + drill-down list per entity.
  - Per-entity edit screen shows red badge per slot with unresolved ref.
  - `snapshot.manifest.json` carries `unresolved_ref_count` for at-a-glance build health.
  - Optional pre-flight lint runnable from publish dialog ("3 unresolved refs — publish anyway?").

Rationale: WIP iteration is the common case. User publishes a panel while one sprite slot still cooking — placeholder is acceptable, blocking the flow is not.

### DEC-A21: Unity snapshot reload — **B) FileSystemWatcher auto-reload**

- Unity watches `Assets/StreamingAssets/catalog/snapshot.manifest.json` mtime.
- Change detected → `GridAssetCatalog.Reload()` reads all per-kind files, validates refs in a temp set, atomically swaps in the new dictionary, fires `CatalogReloaded` event.
- Reload failure (corrupt JSON, missing ref) → keep old snapshot live, log error with `snapshot_id`; surface in Editor inspector.
- Subscribers: `ZoneSubTypeRegistry`, `PlacementValidator`, button/panel UI binders re-resolve their refs on event.

### DEC-A20: Snapshot export trigger — **C) Auto + ~30s debounce + manual override**

- Each `publish` action enqueues a snapshot-export job (debounce window 30s).
- Within window, additional publishes coalesce into the same job.
- Window expires → export script runs → writes the 7 JSON files + manifest atomically.
- Manual "Export snapshot now" button on dashboard bypasses debounce.
- Snapshot is always full (never incremental) — Unity always reads a coherent set.
- `snapshot.manifest.json` carries `snapshot_id` (hash of version_id set) for cache busting + Unity wire-records.

### DEC-A19: Sprite-gen request transport — **Single POST + spinner, no streaming, no cancel**

- `POST /render` with full param JSON → blocks until all variants done → returns `{run_id, fingerprint, variants:[{idx, blob_ref, ...}]}`.
- UI shows spinner with elapsed seconds.
- No streaming, no progress events, no cancel.
- DB shape (DEC-A5 per-variant rows + status enum) preserves the option to add a streaming endpoint or fast-preview tier later without schema change — just deferred indefinitely.

### DEC-A18: Archetype schema authoring — **C) Code-canonical, DB-mirrored at deploy**

- Pydantic models live in repo (`tools/sprite-gen/src/archetypes/`, `web/lib/archetypes/`, shared types via codegen).
- On deploy / migration apply, a sync script publishes each archetype + version into `archetype_detail` rows.
- Web UI shows archetypes read-only; "edit" = open a PR; schema diffs visible in catalog.
- CI lints Pydantic ↔ UI hints sidecar (DEC-A12 pattern) per archetype.

### DEC-A17: Parametric metadata — **C) Archetype-scoped schemas, generalized to every kind**

`archetype` becomes a kind-scoped parametric template. New columns on `archetype_detail`:
```
archetype_detail (entity_id pk)
  target_kind        ← enum: 'sprite' | 'asset' | 'button' | 'panel' | 'pool' | 'audio'
  pydantic_schema_ref← path/import for the Pydantic model (or DB blob — see DEC-A18)
  ui_hints_json      ← form-rendering metadata (DEC-A12 pattern)
  schema_version     ← integer per archetype version
```

Slugs become qualified: `sprite:residential_small`, `asset:residential_small_building`, `button:icon_button`, `panel:modal_dialog`, `audio:button_click`, ...

Every instance kind carries:
```
asset_detail.archetype_version_id   → entity_version (kind=archetype)
asset_detail.params_json            ← validated against pinned archetype version
button_detail.archetype_version_id  + params_json
panel_detail.archetype_version_id   + params_json
```

Server validates `params_json` on write/publish against the pinned archetype version's schema.

New kind `audio` added (cousin of `sprite`) — supports "button sound" + similar refs.

Examples:
- `archetype:asset:residential_small_building` params: `window_count: 1..6`, `tree_count: 0..3`, `chimney: bool`, `balcony_style: enum`.
- `archetype:button:icon_button` params: `icon_position: left|right`, `badge_count: int`, `sound_id: audio_ref`, `hold_to_repeat: bool`.
- `archetype:panel:modal_dialog` params: `animation: motion_curve_ref`, `dismiss_on_backdrop: bool`, `header_style: enum`.

Growth path: add a knob = bump archetype version → new instances offer the knob → old instances stay valid against their pinned version. Zero table migration.

### DEC-A16: Web UI architecture — **C) Single SPA + workflow shortcuts overlay**

Backbone:
- Left-rail kind tabs: Sprites / Assets / Buttons / Panels / Pools / Tokens (sub-tabbed by token kind) / Archetypes / Render Runs.
- Universal kind shape: `list (filter+search+sort) → detail → edit form → publish action`.
- Reused form components per field type; data-driven from per-kind schema (DEC-A12 + DEC-A14 + future archetype params — see DEC-A17).

Workflow shortcuts (Dashboard at `/catalog`):
1. **Generate sprite** → param form (DEC-A12) → render variants → variant grid → promote one → land on Sprite detail with promoted variant.
2. **New asset** → pick subtype + archetype → render or pick existing sprite → fill parametric metadata → set economy → publish.
3. **New button** → pick button archetype → bind sprite slots → pick tokens (palette/frame/font) → set action_id → preview → publish.
4. **New panel** → pick panel archetype → drag-drop child buttons/sprites → preview → publish.
5. **Manage pool / subtype** → list pool members → drag to weight + edit conditions.
6. **Publish stage** → cross-kind diff list of "draft has unpublished changes" → bulk publish.

Cross-cutting overlays available from any list/detail:
- Reverse-lookup ("which panels reference this button?", "which assets in this pool?").
- Audit timeline (`entity_version` history per entity).
- Snapshot inspector (current published-set hash, last export time, Unity boot status).

### DEC-A15: Auth + ownership — **B) Real users table + minimal dev session from day 1**

```
users
  id (uuid pk)
  email (unique, not null)
  display_name
  role               ← enum: 'owner'|'editor'|'viewer'   (only 'owner' used in MVP)
  created_at / last_seen_at
```

- All `owner_id`, `published_by`, `created_by`, `render_run.owner_id` columns are typed `uuid REFERENCES users(id)`.
- MVP seeds one user row at first migration apply; dev login = NextAuth magic-link provider or a signed dev-cookie route that picks the seeded user.
- Hosted upgrade later = swap NextAuth provider to GitHub OAuth (or similar), no schema touch.
- Authorization model in MVP = "single owner sees everything". Multi-user policy (per-entity owner-only edits, viewer role, role-based publish gate) parked under §Future Auth Hardening.

### DEC-A14: Token kind modelling — **C) Separate catalog_entity kinds + shared token_meta view**

Five new catalog kinds, each with its own typed detail table:

```
palette_detail (entity_id pk)
  roles_json         ← ordered list: [{role:"primary", hex:"#0aa"}, {role:"accent", hex:"#fa3"}, ...]
                      role set canonical (mirrors ia/specs/ui-design-system.md)

frame_style_detail (entity_id pk)
  type               ← enum: 'nine_slice' | 'vector'
  nine_slice_atlas_id  → catalog_entity (kind=sprite)  nullable
  vector_radius_px / vector_thickness_px / vector_shadow_json   nullable
  state_variants_json← {raised:..., inset:..., pressed:...}

font_face_detail (entity_id pk)
  font_blob_ref      ← logical ref to TTF/OTF (DEC-A1 logical refs)
  size_scale_json    ← {sm:12, md:16, lg:20, xl:28}
  weight             ← enum: regular|medium|bold|black
  fallback_chain_json← ordered list of family names

motion_curve_detail (entity_id pk)
  easing             ← enum: linear|ease_in|ease_out|ease_in_out|cubic_bezier|spring
  cubic_params_json  ← {x1,y1,x2,y2}  nullable
  spring_params_json ← {tension,friction,mass}  nullable
  duration_ms

illumination_detail (entity_id pk)
  glow_color_hex
  pulse_rate_hz
  glow_radius_px
  falloff_curve      ← enum: linear|exp|gaussian
```

Cross-kind read view:
```sql
CREATE VIEW token_meta AS
  SELECT 'palette'      AS token_kind, e.* FROM catalog_entity e WHERE kind='palette'
  UNION ALL
  SELECT 'frame_style'  ,e.* FROM catalog_entity e WHERE kind='frame_style'
  UNION ALL
  ... (each token kind);
```

Implications:
- Button/panel FK columns (`palette_id`, `frame_style_id`, etc.) typed correctly — DB enforces "this column points at a palette".
- Web UI "all tokens" list reads `token_meta`; per-kind editors hit per-kind detail.
- Adding a new token kind = same cost as adding any catalog kind (kind enum + detail table + view branch).
- Snapshot export's `tokens.json` (DEC-A9) groups all token kinds; consumer Unity loader keys by `token_kind + version_id`.

### DEC-A13: Migration of existing catalog tables — **B) Transform in place**

Existing infra: `0011_catalog_core.sql` (`catalog_asset`, `catalog_sprite`, `catalog_economy`), `0012_catalog_spawn_pools.sql` (`catalog_spawn_pool`, `catalog_pool_member`), web routes, MCP tools. **No Unity dependence yet** — internal-only refactor.

Migration plan:
1. **§Pre-Migration Audit (parked task)**: read `0011` + `0012` SQL + `web/app/api/catalog/` + MCP `catalog_*` to confirm column-by-column fit; record any awkward overlaps (e.g. existing PK columns that need to be demoted to `entity_id` FKs).
2. **`0013_catalog_spine.sql`**: introduce `catalog_entity`, `entity_version`, `panel_child`, `pool_member` (replacing `catalog_pool_member` if shape differs).
3. **`0014_catalog_detail_link.sql`**: add `entity_id uuid` FK column to existing tables (`catalog_asset`, `catalog_sprite`, `catalog_economy`, etc.); rename them to canonical detail names (`asset_detail`, `sprite_detail`); add detail tables for new kinds (`button_detail`, `panel_detail`, `pool_detail`, `archetype_detail`, plus token detail tables — see DEC-A14).
4. **`0015_catalog_backfill.sql`**: for each existing row, insert matching `catalog_entity` + link `entity_id`; map `catalog_pool_member` → `pool_member`.
5. **API + MCP adaptation**: existing routes + tools switch to `catalog_entity_full_*` views; signatures preserved where possible; breaking changes called out per route.
6. **`validate:catalog-spine` script**: walks every detail row, asserts paired `catalog_entity` row exists, asserts FK integrity. Runs in `validate:all`.

Risk mitigation: each step is an independent migration + atomic. Rollback = previous migration. No "production live" risk because Unity loader doesn't exist yet.

### DEC-A12: Sprite-gen parameter schema — **C) Pydantic contract + JSON UI-hints sidecar**

```
tools/sprite-gen/
  src/params/
    schema.py                    ← Pydantic v2 models (RenderParams, PromoteParams, ...)
                                    one model per service endpoint
    ui_hints.json                ← form-rendering metadata, versioned
    schema_version.txt           ← single integer; bumped on any breaking change
  schemas/
    (auto-generated at build)    ← Pydantic → JSON Schema dump for cross-language consumers
```

Service endpoints (added to DEC-A3 service):
- `GET /parameter-schema/{endpoint}` → returns `{ "schema": <jsonschema>, "ui_hints": <sidecar slice>, "schema_version": N }`.
- `GET /parameter-schema` → manifest of all endpoints + their schema versions.

UI hints sidecar shape (per field):
```json
{
  "render": {
    "lighting.sun_angle_deg": {
      "control": "slider", "min": 0, "max": 360, "step": 1, "scale": "linear",
      "group": "Lighting", "label": "Sun angle", "help": "0=east, 90=south..."
    },
    "geometry.subdiv": {
      "control": "knob", "min": 1, "max": 8, "step": 1, "scale": "log2",
      "group": "Geometry", "label": "Subdivision"
    },
    "palette_id": {
      "control": "select", "source_endpoint": "/list-palettes",
      "group": "Palette", "label": "Palette"
    },
    "_groups_order": ["Lighting", "Geometry", "Palette", "Output"]
  }
}
```

CI lint: `npm run validate:sprite-gen-schema` walks Pydantic field set + UI hints field set, fails on field present in one but not the other.

Versioning: `render_run.params_json` (DEC-A5) carries `schema_version` so old runs remain interpretable when schema evolves; UI shows a "this run used schema v2; current is v3" notice if user re-opens an old run.

### DEC-A11: Asset → subtype cardinality — **C) Many-to-many with primary pointer**

```
asset_detail (entity_id pk)
  ...
  primary_subtype_pool_id → catalog_entity (kind=pool)   ← canonical "home" subtype
  -- additional memberships live in pool_member rows
```

Authoring UX:
- Asset edit screen has "Primary subtype" single-select (required) + "Also valid in" multi-select chips (optional, zero-or-many).
- Saving writes `primary_subtype_pool_id` AND ensures a `pool_member` row exists for the primary (so spawn-time queries are uniform — Unity always reads `pool_member`, never the pointer).
- Removing a subtype from "Also valid in" deletes the corresponding `pool_member` row; primary cannot be removed without picking a new primary.

Runtime: `ZoneSubTypeRegistry.PickVariant` only looks at `pool_member`. Primary is a web-only concept, surfaces in default list grouping, search highlights, and "this asset's home" UI labels.

Validator rule: every asset must have its `primary_subtype_pool_id` present in its `pool_member` set; enforced by DB trigger or app-level check on publish.

### DEC-A10: Pool semantics — **B) Asset-only, subtype-tagged, weights + JSONB conditions**

User-facing framing: **pool = the variant bag for a Zone subtype**. Authoring an asset includes picking which subtype(s) it belongs to. That selection writes a `pool_member` row.

```
catalog_entity (kind=pool)        ← one pool per subtype (e.g. slug='residential_light_building')
pool_detail (entity_id pk)
  subtype_slug      ← canonical subtype identifier (mirrors ZoneSubTypeRegistry)
  default_weight    ← used when member rows omit explicit weight

pool_member(pool_id, asset_entity_id, weight, conditions_json)
  weight            ← integer, summed for weighted pick
  conditions_json   ← JSONB predicate set, e.g. {"min_growth_ring":2,"biome":"plains"}
```

Authoring UX (web catalog → asset detail screen):
- "Subtype membership" multi-select control. Shows all `kind=pool` entities by display_name.
- Each membership line lets user set `weight` + open a condition editor for `conditions_json`.
- Saving the asset writes `pool_member` rows; pool entities are NOT edited from this screen, only joined to.

Runtime (Unity spawn path):
- `ZoneSubTypeRegistry.PickVariant(subtype_slug, spawn_context)`:
  1. Lookup pool by `subtype_slug`.
  2. Filter `pool_member` rows where `conditions_json` matches `spawn_context`.
  3. Weighted random pick over filtered set.
- Predicate vocab is canonical + Unity-evaluable; documented under `ia/specs/glossary.md` (or a `pool-conditions.md` rule).

Implications:
- One asset can belong to multiple pools / subtypes (junction table is many-to-many natively).
- "Heterogeneous pools" deferred — adding `member_kind` enum + relaxing FK is a small future migration if a real use case appears.
- Existing `0012_catalog_spawn_pools.sql` schema migrates into this shape; concrete migration plan parked under §Migration Plan.

### DEC-A9: Snapshot export shape — **C) Per-kind JSON files, normalized by version_id**

```
Assets/StreamingAssets/catalog/
  snapshot.manifest.json       ← top-level: snapshot_id (hash), generated_at, file list, schema_version
  tokens.json                  ← all published token versions across kinds
  sprites.json
  buttons.json
  panels.json
  pools.json
  assets.json
  archetypes.json
```

Each per-kind file shape:
```json
{
  "snapshot_id": "...",
  "kind": "buttons",
  "schema_version": 1,
  "items": [
    { "entity_id": "...", "version_id": "...", "slug": "...", "fields": { ... },
      "refs": { "idle_sprite": "<sprite version_id>", "palette": "<token version_id>", ... } }
  ]
}
```

Implications:
- Unity loader = `GridAssetCatalog.LoadAtBoot()` reads manifest → reads each file → builds version_id dict per kind → resolves refs in single pass.
- Unresolved ref at boot = hard error with clear log (`[catalog] panel:abc references missing button:xyz@v3`).
- Hot-reload (post-MVP, GAP-3) = re-read just one file (`tokens.json` after a palette tweak).
- `snapshot_id` recorded in Unity scene wire-records by `wire_asset_from_catalog` for full reproducibility.
- Export script `tools/scripts/export-catalog-snapshot.ts` walks `current_published_version_id` per kind, writes 7 files + manifest atomically (temp dir → rename).

### DEC-A8: Versioning + publishing — **C) Drafts mutable, publishes frozen**

```
catalog_entity                    ← always-current head
  ...
  current_published_version_id  → entity_version.id  (nullable until first publish)
  has_unpublished_changes (bool)

entity_version                    ← immutable snapshot at publish time
  id (uuid pk)
  entity_id      → catalog_entity.id
  version_no     ← monotonic per entity
  published_at
  published_by   → owner_id
  detail_snapshot_json  ← full denormalized contents (spine + detail + resolved FK targets as version_ids)
  source_change_summary ← human-readable diff label
```

Lifecycle:
1. Create entity → status `draft`. All edits mutate `*_detail` rows in place.
2. Click **Publish** → server resolves every outbound FK to its current `*.current_published_version_id`, denormalizes the row tree, writes `entity_version`, bumps `current_published_version_id`, clears `has_unpublished_changes`.
3. Re-edit a published entity → flips `has_unpublished_changes=true`; next publish writes a new `entity_version`.
4. References from one published entity to another are pinned at the publishing entity's publish-time. Editing dependency post-publish does not break the dependent until the dependent is re-published.
5. Retire → `retired_at` set on spine; old `entity_version` rows remain readable for any snapshot referencing them.

Snapshot export:
- Reads only `entity_version` rows where the spine entity is `current_published_version_id` non-null + not retired.
- Output is a deterministic, content-addressable function of `(set of current_published_version_ids)`.
- Unity bridge `wire_asset_from_catalog` records the wired `version_id` so scene contents are reproducible.

Implications:
- Save games store `version_id` references → safe across content edits.
- Two Unity builds can ship simultaneously referencing different version sets.
- Adding versioning later would require backfilling `version_id` columns into every reference site — explicitly avoided by deciding now.

### DEC-A7: Binding model — **C) Hybrid: typed columns for fixed slots, junction tables for variable composition**

```
sprite_detail (entity_id pk)
  png_blob_ref      ← logical ref (DEC-A1)
  ppu / pivot_x / pivot_y / size_w / size_h
  source_variant_id → render_variant.id

button_detail (entity_id pk)         ← typed slot columns
  idle_sprite_id    → catalog_entity (kind=sprite)
  hover_sprite_id   → catalog_entity (kind=sprite)  nullable
  pressed_sprite_id → catalog_entity (kind=sprite)  nullable
  disabled_sprite_id→ catalog_entity (kind=sprite)  nullable
  icon_sprite_id    → catalog_entity (kind=sprite)  nullable
  badge_sprite_id   → catalog_entity (kind=sprite)  nullable
  palette_id        → catalog_entity (kind=palette token)
  frame_style_id    → catalog_entity (kind=frame_style token)
  font_id           → catalog_entity (kind=font_face token)
  illumination_id   → catalog_entity (kind=illumination token)  nullable
  size_variant      ← enum: sm|md|lg
  action_id         ← UIManager entry-point slug
  enable_predicate_json  ← when button is interactive

panel_detail (entity_id pk)
  background_sprite_id → catalog_entity (kind=sprite)  nullable
  palette_id           → palette
  frame_style_id       → frame_style
  layout_template      ← enum: vstack|hstack|grid|free
  modal                ← bool

panel_child(panel_id, child_id, position, layout_json)   ← junction
  panel_id   → catalog_entity (kind=panel)
  child_id   → catalog_entity (kind=button|panel|sprite)
  position   ← integer for ordering within parent
  layout_json← JSONB: x/y/anchor/colspan/etc.

pool_member(pool_id, member_id, weight, conditions_json) ← junction
  conditions_json ← JSONB: e.g. {min_growth_ring:2, biome:"plains"}
```

Implications:
- "Find every panel using button X" = single index scan on `panel_child(child_id)`.
- Slot enum change (e.g. add `focus_sprite_id`) = one migration, rare event.
- Layout + pool conditions stay JSONB exactly where free-form is real.
- No silent dangling refs — every reference is an FK.

### DEC-A6: Game design system surface — **C) Catalog-authority tokens + structural-fidelity preview**

Game DS tokens live as first-class catalog data, seeded from `ia/specs/ui-design-system.md`. Both Unity + web read the same tokens via snapshot.

```
Token kinds (each = a catalog_entity kind, OR a separate game_ds_* table — see DEC-A8):
  game_ds_palette        ← named color sets (primary, secondary, danger, success, ...)
  game_ds_frame_style    ← 9-slice border + radius + thickness presets
  game_ds_font_face      ← typeface + size variants (sm/md/lg/xl)
  game_ds_motion_curve   ← named easing + duration presets
  game_ds_illumination   ← IlluminatedButton glow/pulse presets (preview = static swatch in web)
```

Web preview commitment:
- **Structural fidelity**: correct frame, correct palette swatch, correct font, correct layout, correct slot binding.
- **NOT pixel fidelity**: no shader glow animation, no `IlluminatedButton` real-time effects. Preview cards labelled "approximate — Unity is final".
- React component lib `<GameButton>` / `<GamePanel>` consumes tokens via web API; Unity consumes same tokens via snapshot. One contract.

Implications:
- Token edits flow web → DB → snapshot → Unity (same path as sprites).
- Authoring buttons/panels in browser produces a useful, honest preview without spinning up Unity.
- Pixel-perfect look-and-feel verification still requires Unity (acceptable tradeoff).

### DEC-A5: Render-run + variant lifecycle — **C) Persistent runs + variants, GC by policy**

```
render_run                        ← one invocation of sprite-gen
  id (uuid pk)
  archetype_entity_id  → catalog_entity (kind=archetype)
  params_json          ← full param set (versioned)
  build_fingerprint    ← git sha + palette hash + service version
  parent_run_id        ← nullable, for "re-render with tweak" lineage
  status (queued|running|done|failed)
  started_at / finished_at
  owner_id
  gc_policy (default 'keep_promoted_only_after_90d')

render_variant                    ← one PNG output of a render_run
  id (uuid pk)
  run_id → render_run.id
  variant_idx          ← 0..N-1 within the run
  blob_ref             ← logical ref (DEC-A1): gen://{run_id}/{variant_idx}
  resolved_path        ← cached on-disk path (MVP) or blob URL (hosted)
  status (unpromoted|promoted|rejected|archived)
  promoted_at / rejected_at
  promoted_to_entity_id → catalog_entity (kind=sprite)  -- nullable
```

Implications:
- Promoted sprite (`catalog_entity kind=sprite` + `sprite_detail`) carries `source_variant_id` for full provenance.
- "Re-render with same params + one tweak" = new `render_run` with `parent_run_id` set + diff'd params; UI can show param-tree.
- Rejected variants stay browseable until GC; user can un-reject, swap promoted variants, etc.
- GC policy lives on the run row, not in code — design-now, implement-later.

### DEC-A4: Catalog entity model — **C) Spine + per-kind detail tables**

```
catalog_entity            ← thin polymorphic spine
  id (uuid pk)
  kind (sprite|asset|button|panel|pool|archetype)
  slug (unique per-kind)
  status (draft|published|retired)
  display_name
  owner_id
  created_at / updated_at / retired_at
  search_tsv

sprite_detail (entity_id pk → catalog_entity.id)
asset_detail (entity_id pk)
button_detail (entity_id pk)         ← references sprite entity ids in slot columns
panel_detail (entity_id pk)          ← references button/sprite entity ids
pool_detail (entity_id pk) + pool_member (entity_id, member_id, weight)
archetype_detail (entity_id pk)
```

Implications:
- All cross-kind operations (search, retire, audit log, status workflow) live on the spine — written once, applied everywhere.
- All inter-entity references (button → sprite, panel → button, pool → asset) FK to `catalog_entity.id` → typed integrity, no orphans.
- Per-kind `*_detail` tables migrate independently — adding a column to buttons doesn't churn sprite migrations.
- Reads use `catalog_entity_full_*` views (`JOIN spine + detail`) so API code feels flat.
- Snapshot export selects per-kind via the views; no UNION-of-everything query.

### DEC-A3: Sprite-gen process model — **B) Long-lived Python service**

`tools/sprite-gen/` ships an HTTP service (`python -m src serve`, FastAPI/uvicorn, bound to `127.0.0.1` in MVP). Next.js API routes proxy to it. Implications:
- One extra dev process tracked alongside Postgres + Next.js + Unity Editor — added to `npm run dev` orchestration.
- Schema-shaped for queue migration (DEC-A1): every render request carries a `run_id`, records a `render_runs` row up-front, marks status `queued → running → done|failed`. Flipping to a real queue later = swap transport, not data model.
- Service exposes `/render`, `/promote`, `/list-archetypes`, `/list-palettes`, plus a `/parameter-schema` introspection endpoint that the web UI consumes to build forms (selectors / sliders / knobs).

### DEC-A2: Sprite-gen integration — **strictly backend API, fully parametrized**

Web UI never shells out to Python. Every sprite-gen capability + every CLI knob is exposed as a typed backend endpoint; the UI surfaces those parameters as selectors / sliders / knobs. Implications:
- Sprite-gen capabilities enumerated as a **parameter schema** (single source of truth for both backend validation + UI form generation).
- `tools/sprite-gen/` grows a stable parameter contract (CLI flags + Python entrypoint signature + JSON schema all aligned).
- Next.js API routes wrap Python entrypoints with structured JSON in/out; no free-form command strings cross the boundary.

---

## 6. Design Gaps — Decisions Needed Before Implementation

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
