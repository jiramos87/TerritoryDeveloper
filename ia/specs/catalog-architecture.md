---
purpose: "Reference spec for the asset-pipeline catalog — spine + detail schema, version lifecycle, snapshot export, publish lint, blob resolver, capability + pool composition."
audience: agent
loaded_by: ondemand
slices_via: spec_section
---
# Catalog Architecture — Asset Pipeline Reference Spec

> Canonical schema, version lifecycle, snapshot export, publish-lint, blob resolver, capability matrix + pool composition for the asset-pipeline catalog. Graduated 2026-04-30 (asset-pipeline Stage 20.1) from the design-trail [`docs/asset-pipeline-architecture.md`](../../docs/asset-pipeline-architecture.md). Spec wins on conflict per glossary header.

Cross-refs:
- Scene contract: [`docs/asset-pipeline-scene-contract.md`](../../docs/asset-pipeline-scene-contract.md).
- Architectural decisions DEC-A1..A52: [`docs/asset-pipeline-architecture.md`](../../docs/asset-pipeline-architecture.md) §5 (preserved as design-trail; this spec lifts the locked subset).
- Glossary: `archetype_version`, `blob_resolver`, `capability`, `entity_version`, `panel_child`, `pool_member`, `publish_lint_rule`, `render_run`, `snapshot`, `wire_asset_from_catalog`, `rollback_token`, `scene contract`.
- Master plan: DB-backed slug `asset-pipeline` (render via `mcp__territory-ia__master_plan_render({slug: "asset-pipeline"})`).

## 1. Scope

The catalog is a Postgres-resident polymorphic content registry feeding two consumers:

- **Web authoring console** (`web/app/catalog/**`) — CRUD UX over draft entities, render runs, publish gate.
- **Unity loader** (`Assets/Scripts/Catalog/CatalogLoader.cs`) — cold-boot read of the published snapshot bundle.

Sprite assets are produced offline by the sprite-gen FastAPI service ([`tools/sprite-gen/`](../../tools/sprite-gen/)); their byte content lives in the blob store (`BLOB_ROOT`) and is referenced by `gen://` URIs.

Out of scope for this spec: scene-contract path resolution (separate doc), exploration narrative (design trail), audit history, open questions GAP-1..GAP-7 (design trail §6).

## 2. Spine + Detail pattern

DEC-A4 — single thin polymorphic spine + per-kind detail tables. Migration `0021_catalog_spine.sql` introduces the spine; per-kind detail migrations follow (see `db/migrations/0029_*..0040_*`).

### 2.1 Spine — `catalog_entity`

```
catalog_entity
  id                              uuid pk
  kind                            text  -- sprite | asset | button | panel | pool | archetype | token | audio
  slug                            text  -- DEC-A24 regex; unique per (kind, slug)
  status                          text  -- draft | published | retired
  display_name                    text
  owner_id                        uuid  -- FK users
  current_published_version_id    uuid  -- FK entity_version (nullable until first publish)
  has_unpublished_changes         bool
  search_tsv                      tsvector
  created_at / updated_at / retired_at  timestamptz
```

All cross-kind operations (search, retire, audit log, status workflow) live on the spine — written once, applied everywhere. Inter-entity references FK to `catalog_entity.id` so integrity is typed.

### 2.2 Per-kind detail tables

| Kind | Detail table | Migration | Notes |
| --- | --- | --- | --- |
| `sprite` | `sprite_detail` | `0021_catalog_spine.sql` | `png_blob_ref` (`gen://` URI), ppu, pivot, size, `source_variant_id`. |
| `asset` | `asset_detail` | `0029_asset_detail_primary_subtype.sql` | Many-to-many to `sprite` via `asset_sprite_binding`. |
| `button` | `button_detail` | `0030_button_detail.sql` | Typed slot columns: `idle_sprite_id`, `hover_sprite_id`, `pressed_sprite_id`, `disabled_sprite_id`, `icon_sprite_id`, `palette_id`, `frame_style_id`, `font_id`. |
| `panel` | `panel_detail` + `panel_child` | `0031_panel_detail_and_child.sql` | `panel_child(panel_id, child_id, position, layout_json)` junction — see §6.1. |
| `audio` | `audio_detail` | `0032_audio_detail.sql` | Archetype-driven params + minimal output detail (DEC-A31). |
| `token` | `token_detail` | `0035_token_detail.sql` | Palette / frame style / font face / motion curve / illumination — DEC-A14. |
| `pool` | `pool_detail` + `pool_member` | `0011_catalog_core.sql` + `0028_pool_member_conditions.sql` | `pool_member(pool_id, member_id, weight, conditions_json)` junction — see §6.2. |
| `archetype` | `archetype_authoring` | `0037_archetype_authoring.sql` | Versioned authoring rows; one `archetype_version` per spec edit. |

### 2.3 Implications

- Per-kind detail migrates independently — adding a column to buttons doesn't churn sprite migrations.
- Reads use `catalog_entity_full_*` views (`JOIN spine + detail`) so API code feels flat.
- Snapshot export selects per-kind via the views; no UNION-of-everything query.
- "Find every panel using button X" = single index scan on `panel_child(child_id)`.

## 3. Entity version lifecycle

DEC-A8 — drafts mutable, publishes frozen. The `entity_version` row is the immutable snapshot of an entity at one publish moment.

### 3.1 `entity_version` table

```
entity_version
  id                       uuid pk
  entity_id                uuid    -- FK catalog_entity
  version_no               int     -- monotonic per entity
  published_at             timestamptz
  published_by             uuid    -- FK users
  detail_snapshot_json     jsonb   -- full denormalized contents (spine + detail + resolved FK targets as version_ids)
  source_change_summary    text    -- human-readable diff label
  lint_overrides_json      jsonb   -- per-rule acknowledgements (§4)
```

### 3.2 Lifecycle stages

1. **Draft** — `catalog_entity.status = 'draft'`. All edits mutate `*_detail` rows in place.
2. **Publish** — server resolves every outbound FK to its current `*.current_published_version_id`, denormalizes the row tree, writes `entity_version`, bumps `current_published_version_id`, clears `has_unpublished_changes`.
3. **Re-edit** — flips `has_unpublished_changes = true`; next publish writes a new `entity_version`.
4. **Reference pinning** — references from one published entity to another are pinned at the publishing entity's publish-time. Editing a dependency post-publish does NOT break the dependent until the dependent is re-published.
5. **Retire** — `catalog_entity.retired_at` set on spine; old `entity_version` rows remain readable for any snapshot referencing them (DEC-A23 soft-retire, restore allowed).

### 3.3 Implications

- Save games store `version_id` references → safe across content edits.
- Two Unity builds can ship simultaneously referencing different version sets.
- Output of snapshot export is a deterministic, content-addressable function of `(set of current_published_version_ids)`.
- Unity bridge `wire_asset_from_catalog` records the wired `version_id` so scene contents are reproducible.

## 4. Publish lint layers

DEC-A30 — two-layer validation pipeline runs on every publish attempt.

### 4.1 Layer 1 — Hard gates (block publish, no override)

- `params_json` validates against pinned `archetype_version.params_schema` (Pydantic / JSON Schema).
- `slug` matches DEC-A24 regex `^[a-z][a-z0-9_]{2,63}$` + uniqueness + frozen-after-publish rule.
- No outbound ref points at a retired entity (DEC-A23 hard block).
- Cycle check on panel children (DEC-A27).
- Required `*_detail` row exists for the kind.

### 4.2 Layer 2 — Soft lints (warn, author can publish-anyway)

```
publish_lint_rule
  rule_id      text pk         -- e.g. 'sprite.missing_ppu'
  kind         text            -- entity kind this applies to
  severity     text            -- 'warn' | 'info'
  enabled      bool default true
  config_json  jsonb default '{}'
  description  text
```

Lint runner: `tools/scripts/lint-catalog-entity.ts` exports `runLints(entity, version, kind) → LintResult[]`. Each rule = TS module under `tools/scripts/lint-rules/{kind}/{rule_id}.ts` exporting `(entity, version, ctx) → LintResult | null`.

Migrations: seed sets in `0033_publish_lint_rule_audio_seed.sql` + `0039_publish_lint_rule_non_audio_seed.sql`. Per-rule overrides recorded in `entity_version.lint_overrides_json` when author publishes through warning (audit trail).

### 4.3 MVP rule seed set (canonical)

`sprite.missing_ppu`, `sprite.missing_pivot`, `asset.no_sprite_bound`, `button.missing_icon`, `button.missing_label`, `panel.empty_slot_below_min`, `panel.unfilled_required_slot`, `audio.loudness_out_of_range`, `pool.empty`, `pool.no_primary_subtype`, `token.no_consumers`.

## 5. Snapshot export contract

DEC-A9 — per-kind JSON files, normalized by `version_id`. DEC-A20 — auto-on-publish (debounced ~30s) + manual override.

### 5.1 Output layout

```
Assets/StreamingAssets/catalog/
  snapshot.manifest.json     -- top-level: snapshot_id (hash), generated_at, file list, schema_version
  tokens.json                -- all published token versions
  sprites.json
  buttons.json
  panels.json
  pools.json
  assets.json
  archetypes.json
  audio.json
```

### 5.2 Per-kind file shape

```jsonc
{
  "snapshot_id": "...",
  "kind": "buttons",
  "schema_version": 1,
  "items": [
    {
      "entity_id": "...",
      "version_id": "...",
      "slug": "...",
      "fields": { /* denormalized detail */ },
      "refs": {
        "idle_sprite": "<sprite version_id>",
        "palette":     "<token   version_id>"
      }
    }
  ]
}
```

### 5.3 Loader contract

`GridAssetCatalog.LoadAtBoot()`:

1. Read manifest.
2. Read each per-kind file.
3. Build `version_id → row` dict per kind.
4. Resolve refs in single pass.

Unresolved ref at boot = hard error with diagnostic log (e.g. `[catalog] panel:abc references missing button:xyz@v3`).

### 5.4 Export driver

`tools/scripts/export-catalog-snapshot.ts` walks `current_published_version_id` per kind, writes 8 files + manifest atomically (temp dir → rename). Manifest header records `snapshot_id` (content hash) for downstream wiring traceability.

### 5.5 `catalog_snapshot` table

```
catalog_snapshot                   -- migration 0040_catalog_snapshot.sql
  snapshot_id        text pk       -- content hash
  generated_at       timestamptz
  schema_version     int
  entity_versions    jsonb         -- list of version_id refs (drives is_pinned in §3)
  retired_at         timestamptz
```

`is_pinned` flag on `entity_version` derives from join with `catalog_snapshot.entity_versions[]` — pinned versions are GC-protected (DEC-A29).

### 5.6 DriftGate

Protected pair: `panel_detail.rect_json` (DB jsonb) ↔ `Assets/UI/Snapshots/panels.json` (`items[].fields.rect_json`).

Enforcement: `npm run validate:ui-drift` (`tools/scripts/validate-ui-def-drift.mjs`).
- Compares per-slug `rect_json` deep equality: DB rows vs snapshot items.
- Exit 0 = match. Exit 1 = drift; stdout lists offending slugs (`drift: {slug} field={field}`).
- Wired into `validate:all` CI chain after `validate:catalog-naming`.
- Hard-fail only (Q4) — no warning mode.
- DB unreachable in CI (no DATABASE_URL) → exit 0 with info line (skip-graceful).

Locked baseline: `hud-bar`, `toolbar` panels seeded in migrations 0109, 0110.

Future expansion: per-kind snapshots (`tokens.json`, `components.json`) — Stage 4 surface.

See also: [`docs/explorations/ui-implementation-mvp-rest.md`](../../docs/explorations/ui-implementation-mvp-rest.md).

## 6. Composition junctions

### 6.1 `panel_child`

DEC-A27 — slot-based archetype-driven children. Junction row composes a panel from buttons / sprites / nested panels.

```
panel_child
  panel_id    uuid     -- FK catalog_entity (kind=panel)
  child_id    uuid     -- FK catalog_entity (kind=button|panel|sprite)
  position    int      -- ordering within parent
  layout_json jsonb    -- x/y/anchor/colspan/etc.
  PRIMARY KEY (panel_id, position)
```

Cycle check (Layer 1 lint §4.1) prevents panel-A → panel-B → panel-A composition.

### 6.2 `pool_member`

DEC-A10 — asset-only, subtype-tagged, weights + JSONB conditions. User-facing framing: pool = variant bag for a Zone subtype. Authoring an asset picks which subtype(s) it belongs to.

```
pool_member
  pool_id          uuid     -- FK catalog_entity (kind=pool)
  member_id        uuid     -- FK catalog_entity (kind=asset)
  weight           int      -- summed for weighted pick
  conditions_json  jsonb    -- predicate set, e.g. {"min_growth_ring":2,"biome":"plains"}
  PRIMARY KEY (pool_id, member_id)
```

Runtime path: `ZoneSubTypeRegistry.PickVariant(subtype_slug, spawn_context)`:
1. Lookup pool by `subtype_slug` (column on `pool_detail`).
2. Filter `pool_member` rows where `conditions_json` matches `spawn_context`.
3. Weighted random pick over filtered set.

Predicate vocab is canonical + Unity-evaluable; documented under `ia/specs/glossary.md` rows.

## 7. Render run lifecycle

DEC-A5 — persistent runs + variants, GC by policy. DEC-A26 — `render_run` table + one-click identical replay.

### 7.1 Tables

```
render_run
  id                  uuid pk
  archetype_entity_id uuid     -- FK catalog_entity (kind=archetype)
  archetype_version   uuid     -- pinned authoring version (§Archetype authoring)
  params_json         jsonb    -- full param set
  build_fingerprint   text     -- git sha + palette hash + service version
  parent_run_id       uuid     -- nullable, "re-render with tweak" lineage
  status              text     -- queued | running | done | failed
  started_at / finished_at  timestamptz
  owner_id            uuid
  gc_policy           text default 'keep_promoted_only_after_90d'

render_variant
  id                    uuid pk
  run_id                uuid     -- FK render_run
  variant_idx           int      -- 0..N-1 within the run
  blob_ref              text     -- gen:// URI
  resolved_path         text     -- cached on-disk path / URL
  status                text     -- unpromoted | promoted | rejected | archived
  promoted_at / rejected_at  timestamptz
  promoted_to_entity_id uuid     -- FK catalog_entity (kind=sprite); nullable
```

Migration: `0027_job_queue_render_run.sql`.

### 7.2 Replay semantics

- Promoted sprite (`catalog_entity kind=sprite` + `sprite_detail`) carries `source_variant_id` for full provenance.
- "Re-render with same params + one tweak" = new `render_run` with `parent_run_id` set + diff'd params; UI shows param-tree (DEC-A37).
- Rejected variants stay browseable until GC; user can un-reject, swap promoted variants (DEC-A41).

## 8. Archetype authoring

DEC-A18 — code-canonical, DB-mirrored at deploy. DEC-A46 — versioned schemas + migration helpers + lazy entity bumps.

`archetype_authoring` migration `0037_archetype_authoring.sql` mirrors `tools/sprite-gen/specs/{slug}.yaml` content rows. Each spec edit bumps a new `archetype_version` row; `render_run.archetype_version` pins one version for reproducibility.

Top-level keys + per-kind subschema: see [`tools/sprite-gen/README.md`](../../tools/sprite-gen/README.md) §Archetype YAML format.

## 9. Blob resolver protocol

DEC-A25 — `var/blobs/` repo-local root, swap-ready. `gen://` URIs resolve through a single mediator with one swap point.

### 9.1 URI shape

`gen://{run_id}/{variant_idx}` — render output. Stable across local + future hosted blob stores.

### 9.2 Resolver implementations

- TypeScript: `web/lib/blob-resolver.ts` — `resolve(uri) → local path | URL`.
- Python: `tools/sprite-gen/src/blob_resolver.py` — same surface.

Both read `BLOB_ROOT` env var; fall back to repo-local `var/blobs/` when unset. Bootstrap: `bash tools/scripts/bootstrap-blob-root.sh` (creates dir + gitignore rules).

### 9.3 Promote action

`BlobResolver.read(uri)` → copy to `Assets/Sprites/Generated/{slug}.png` + write `sprite_detail.assets_path`. Original blob stays in `var/blobs/` as immutable provenance trail.

### 9.4 Validation

`validate:blob-roots` asserts every `gen://` URI in `sprite_detail.png_blob_ref` resolves to an existing file under `BLOB_ROOT`.

## 10. Capability matrix

DEC-A33 — reserved schema + capability matrix from day one. Auth model is hosted-ready at MVP creation.

### 10.1 Tables

```
users                              -- migration 0026
  id              uuid pk
  email           citext unique
  display_name    text
  role            text default 'admin'
  org_id          uuid          -- reserved; NULL until multi-tenant
  last_login_at   timestamptz
  created_at      timestamptz
  retired_at      timestamptz   -- soft-retire mirrors entity pattern

capability
  capability_id   text pk        -- e.g. 'catalog.entity.publish'

role_capability
  role            text
  capability_id   text           -- FK capability
  PRIMARY KEY (role, capability_id)
```

### 10.2 Seed roles + capabilities (MVP)

- `admin` — all capabilities.
- `author` — `catalog.entity.{create,edit}`, `render.run`, `lint.config_edit`, `preview.unity_push`. NO `entity.retire/delete`, `auth.role_assign`, `gc.trigger`, `snapshot.retire`.
- `viewer` — read all; no mutate.

Capabilities seeded: `catalog.entity.{create,edit,publish,retire,delete}`, `catalog.snapshot.{export,retire}`, `render.run`, `preview.unity_push`, `lint.config_edit`, `auth.role_assign`, `gc.trigger`, `audit.read`.

### 10.3 Route gating

API routes declare `routeMeta: {METHOD: {requires: capability_id}}`. `proxy.ts` joins `users.role` → `role_capability` → returns DEC-A48 forbidden envelope on miss. Dev fallback: `NEXT_PUBLIC_AUTH_DEV_FALLBACK=1` + `dev_user_id` cookie.

Validation: `validate:capability-coverage` asserts every API route's `requires` capability exists in the `capability` table.

### 10.4 Audit log

```
audit_log                          -- migration 0026
  id              bigserial pk
  actor_user_id   uuid              -- FK users
  action          text              -- e.g. 'catalog.entity.published'
  target_kind     text              -- 'entity' | 'snapshot' | 'role'
  target_id       uuid
  payload_json    jsonb             -- before/after diff or context
  created_at      timestamptz
```

Append-only. No update/delete API. Indexed on `(actor_user_id, created_at)` + `(target_kind, target_id)`.

## 11. Bridge composite surface

`wire_asset_from_catalog` (TECH-1591) is the canonical bridge composite kind that materializes a published catalog `entity_id` into the active Unity scene. Returns `{ok, mutations[], rollback_token}` envelope; supports `dry_run=true` preflight; transactional snapshot → mutate → verify → commit/rollback.

Scene path resolution + pre-mutation `unknown_scene_path` rejection: see [`docs/asset-pipeline-scene-contract.md`](../../docs/asset-pipeline-scene-contract.md).

Snapshot capture / verify / restore: `Assets/Scripts/Editor/Bridge/Snapshot/CellSubtreeSnapshot.cs` (TECH-1592) — eager eviction on commit OR rollback completion; stale-token reuse → `token_unknown` sentinel.

## 12. Decision pointers (canonical)

The following DEC-A entries are canonical; the design trail [`docs/asset-pipeline-architecture.md`](../../docs/asset-pipeline-architecture.md) §5 holds rationale + alternatives.

| DEC | Topic | Section here |
| --- | --- | --- |
| DEC-A4 | Spine + detail | §2 |
| DEC-A5 | Render run + variant lifecycle | §7 |
| DEC-A7 | Binding model — typed slots + junctions | §2, §6 |
| DEC-A8 | Versioning + publishing | §3 |
| DEC-A9 | Snapshot export shape | §5 |
| DEC-A10 | Pool semantics | §6.2 |
| DEC-A18 | Archetype schema authoring | §8 |
| DEC-A20 | Snapshot export trigger | §5 |
| DEC-A23 | Retire + restore | §3.2 |
| DEC-A24 | Slug rules | §2.1 |
| DEC-A25 | Logical blob storage | §9 |
| DEC-A27 | Panel composition | §6.1 |
| DEC-A29 | Per-version GC policy | §5.5 |
| DEC-A30 | Publish lint framework | §4 |
| DEC-A33 | Auth + capability matrix | §10 |
| DEC-A46 | Archetype version + breaking-change handling | §8 |
| DEC-A48 | API error format + REST conventions | §10.3 |

## 13. Future extensions (post-MVP)

Not in this spec — listed for orientation. See design trail §6 for full open questions.

- Hot-reload signal (GAP-3) — FileSystemWatcher OR MCP `catalog_snapshot_reload` bridge.
- Scene contract pre-resolve / mutate split (`snapshot_phase_split` escalation).
- Per-org capability override (`org_role_capability`) and per-entity ACL (`entity_acl`) — additive, no core change.
- Per-kind metadata exposed on `CatalogEntity` (`world_sprite`, `has_button` columns) — replaces synthesized `Assets/Prefabs/Catalog/{slug}.prefab` paths.
