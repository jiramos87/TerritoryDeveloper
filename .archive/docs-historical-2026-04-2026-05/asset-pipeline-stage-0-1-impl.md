# Asset Pipeline — Stage 0 + 1 Implementation Plan

> Informal companion to `docs/asset-pipeline-architecture.md` (DEC-A1–A52).
> Scope: pre-migration audit (Stage 0) + spine schema migration (Stage 1).
> Skill layer is mid-refactor — this doc replaces `/master-plan-new` flow for these two stages.

## Migration numbering correction

DEC-A52 referenced migrations `0013/0014/0015`. Latest in repo is `0020_drop_source_path_columns.sql`. **Real numbers:** `0021`, `0022`, `0023`. Doc below uses real numbers.

## Existing tables this work touches

From `0011_catalog_core.sql` + `0011_catalog_core_indexes.sql` + `0012_catalog_spawn_pools.sql` + `0013_zone_s_seed.sql` + `0014_catalog_pool_smoke.sql`:

| Table | Notes |
|---|---|
| `catalog_asset` | bigserial PK, `(category, slug)` unique, status enum, `updated_at` already present. Seeded ids 0–6 (Zone S) by `0013_zone_s_seed.sql`. |
| `catalog_sprite` | bigserial PK, no slug, art-side metadata |
| `catalog_asset_sprite` | M:N binding via `slot` enum |
| `catalog_economy` | 1:1 with asset. Seeded for ids 0–6 (Zone S). |
| `catalog_spawn_pool` | bigserial PK, `slug` unique, owner category/subtype. Seeded `smoke_zone_s_tool` (`0014`). |
| `catalog_pool_member` | (pool_id, asset_id) PK + weight. Seeded smoke row pointing at asset 0. |

Existing indexes (`0011_catalog_core_indexes.sql`) — spine schema must replicate equivalent coverage:
- `idx_catalog_asset_status` → spine: `entity_version_status_idx` already covers, plus `catalog_entity_retired_idx`.
- `idx_catalog_asset_category` → `asset_detail.category` index.
- `idx_catalog_asset_sprites_by_sprite` → asset_detail FK indexes per sprite slot.
- `idx_catalog_asset_replaced_by` → `catalog_entity.replaced_by_entity_id` index (partial).

Spine migration must preserve existing `bigserial` ids (cheaper than swap to UUID; less invasive). DEC-A8 spine model was authored UUID-leaning — adapt to bigserial for in-place migration. Future hosted swap can introduce a `uuid_external_id` column without refactor.

### Legacy id preservation (consumer compatibility)

Unity `ZoneSubTypeRegistry` keys off `catalog_asset.id` (per `0013_zone_s_seed.sql` comment "application matches ZoneSubTypeRegistry subTypeId"). Spine's unified `catalog_entity.id` sequence cannot reproduce per-table legacy ids exactly because legacy `catalog_asset` and `catalog_sprite` use independent bigserial sequences.

**Resolution:** detail tables carry an additive `legacy_*_id BIGINT UNIQUE NULL` column referencing the original PK. `catalog_asset_compat` view exposes `legacy_asset_id AS id` so consumer code reads the same numeric ids during transition. Columns drop in a later stage once consumers migrate to slug-based lookup.

Affected detail tables:
- `asset_detail.legacy_asset_id`
- `sprite_detail.legacy_sprite_id`
- `pool_detail.legacy_pool_id`

---

## Stage 0 — Foundation

**Goal:** ship verifiable safety net before any destructive SQL. Output: DB dump + audit report under version control.

### Deliverables

1. `tools/scripts/freeze-db-snapshot.sh` — wraps `pg_dump -Fc`. Inputs: snapshot tag (e.g. `pre-spine`). Outputs: `var/db-snapshots/{tag}-{YYYY-MM-DD}.dump` + sha256 manifest line in same dir's `MANIFEST.txt`. Idempotent (same-day run overwrites).
2. `tools/scripts/restore-db-snapshot.sh` — `pg_restore` against current `$DATABASE_URL`. Refuses without `--confirm` flag (destructive).
3. `tools/scripts/audit-catalog-pre-spine.ts` — read-only Node script. Connects via `pg` client (use existing `web/lib/db.ts` pool helper). Emits two artifacts:
   - `docs/audits/catalog-pre-spine-{YYYY-MM-DD}.md` (human).
   - `docs/audits/catalog-pre-spine-{YYYY-MM-DD}.json` (machine, consumed by Stage 1 backfill).
4. npm scripts in `package.json`:
   - `db:snapshot:freeze` → `freeze-db-snapshot.sh pre-spine`.
   - `db:snapshot:restore` → `restore-db-snapshot.sh`.
   - `db:audit-pre-spine` → snapshot first, then run audit, then `open` audit report.
5. `var/db-snapshots/.gitkeep` + `var/db-snapshots/.gitignore` (`*.dump`, `MANIFEST.txt`).
6. `docs/audits/.gitkeep` + audits committed to git (text-only, useful provenance).

### Audit script — required sections

Emit Markdown in this order. JSON mirrors structure.

```
# Catalog Pre-Spine Audit — {YYYY-MM-DD}

Snapshot: var/db-snapshots/pre-spine-{date}.dump
sha256:   {hex}

## 1. Row counts
| table | rows |
| catalog_asset           | N |
| catalog_sprite          | N |
| catalog_asset_sprite    | N |
| catalog_economy         | N |
| catalog_spawn_pool      | N |
| catalog_pool_member     | N |

## 2. FK integrity
- Orphan catalog_asset_sprite rows (asset missing): N
- Orphan catalog_asset_sprite rows (sprite missing): N
- Orphan catalog_economy rows: N
- Orphan catalog_pool_member rows (pool missing): N
- Orphan catalog_pool_member rows (asset missing): N
- Asset.replaced_by pointing at missing id: N

## 3. Slug collisions across categories
Once spine collapses to (kind=asset, slug), category-scoped uniqueness becomes (kind, slug). Audit:
- Slugs duplicated across categories: list (slug, count, categories[])

## 4. Spawn pool slugs vs asset slugs
- Conflicts where a pool slug equals an asset slug across kinds: list

## 5. Sprite duplicate fingerprints
- catalog_sprite.generator_build_fingerprint duplicates: list (fingerprint, count, sprite_ids[])

## 6. Sprite path issues
- Paths not resolving on disk under Assets/: list (sprite_id, path)

## 7. Pool integrity
- Pools with zero members: list (pool_id, slug, owner_category, owner_subtype)
- Pools missing owner_subtype: list
- Pool members pointing at retired assets: list (pool_id, asset_id, slug)

## 8. Field census (drives Stage 1 mapping)
| source_table.col | target | rationale |
| catalog_asset.category          | entity_version.params_json.category    | retained as param |
| catalog_asset.slug              | catalog_entity.slug                    | spine column |
| catalog_asset.display_name      | catalog_entity.display_name            | spine column |
| catalog_asset.status            | derived (retired_at + current_version) | computed not stored |
| catalog_asset.replaced_by       | catalog_entity.replaced_by_entity_id   | reserved (DEC-A23) |
| catalog_asset.footprint_w/h     | asset_detail.footprint_w/h             | detail row |
| catalog_asset.placement_mode    | asset_detail.placement_mode            | detail row |
| catalog_asset.unlocks_after     | asset_detail.unlocks_after             | detail row |
| catalog_asset.has_button        | asset_detail.has_button                | detail row |
| catalog_sprite.path             | sprite_detail.assets_path              | detail row |
| catalog_sprite.ppu              | sprite_detail.pixels_per_unit          | detail row |
| catalog_sprite.pivot_x/y        | sprite_detail.pivot_x/y                | detail row |
| catalog_sprite.provenance       | sprite_detail.provenance               | detail row |
| catalog_sprite.generator_*      | sprite_detail.source_run_*             | detail row |
| catalog_sprite.art_revision     | drop                                   | superseded by entity_version count |
| catalog_asset_sprite (slot)     | asset_detail.{slot}_sprite_id          | flatten |
| catalog_economy.*               | economy_detail.*                       | new detail kind, asset-scoped |
| catalog_spawn_pool.owner_*      | pool_detail.primary_subtype            | DEC-A10 mapping |
| catalog_pool_member.weight      | pool_member.weight                     | preserved |

## 9. Issues requiring manual triage before migration
- {bullet list of any blockers found above}

## 10. Sign-off
Auto-generated by audit-catalog-pre-spine.ts. Migration 0021/0022/0023 may proceed iff section 9 is empty OR each item carries a noted resolution.
```

### Acceptance

- `npm run db:audit-pre-spine` runs end-to-end on a clean local checkout, exits 0.
- Audit report committed under `docs/audits/`.
- DB dump exists under `var/db-snapshots/` (gitignored, sha256 in `MANIFEST.txt` committed-but-empty).
- Section 9 reviewed manually. Empty or each item annotated.

### Estimated scope

~150–250 LOC across 1 shell script wrapper, 1 TS audit script, 4 npm script lines, 2 directory placeholders. No new deps (use existing `pg` + Node `crypto`).

---

## Stage 1 — Spine schema migration

**Goal:** flatten existing catalog_* tables into the spine + detail model. Preserve all data. Validators green.

### Migration files

#### `0021_catalog_spine.sql`

Create new tables. Do NOT mutate existing rows yet. Pure additive.

```sql
BEGIN;

CREATE TABLE catalog_entity (
  id                          bigserial PRIMARY KEY,
  kind                        text NOT NULL CHECK (kind IN (
    'sprite','asset','button','panel','pool','token','archetype','audio'
  )),
  slug                        text NOT NULL,
  display_name                text NOT NULL,
  tags                        text[] NOT NULL DEFAULT '{}',
  current_published_version_id bigint,                              -- FK after entity_version exists
  slug_frozen_at              timestamptz,
  retired_at                  timestamptz,
  retired_by_user_id          bigint,                               -- FK once users table lands (Stage 2)
  retired_reason              text,
  replaced_by_entity_id       bigint REFERENCES catalog_entity(id) ON DELETE SET NULL,
  lint_overrides_json         jsonb NOT NULL DEFAULT '{}',
  created_at                  timestamptz NOT NULL DEFAULT now(),
  updated_at                  timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_catalog_entity_kind_slug UNIQUE (kind, slug),
  CONSTRAINT ck_catalog_entity_slug_format CHECK (slug ~ '^[a-z][a-z0-9_]{2,63}$')
);
CREATE INDEX catalog_entity_retired_idx ON catalog_entity (retired_at);
CREATE INDEX catalog_entity_kind_idx    ON catalog_entity (kind);

CREATE TABLE entity_version (
  id                  bigserial PRIMARY KEY,
  entity_id           bigint NOT NULL REFERENCES catalog_entity(id) ON DELETE CASCADE,
  version_number      int    NOT NULL,
  status              text   NOT NULL CHECK (status IN ('draft','published')),
  archetype_version_id bigint,                                      -- FK self-reference (kind=archetype)
  params_json         jsonb  NOT NULL DEFAULT '{}',
  parent_version_id   bigint REFERENCES entity_version(id),
  source_run_id       uuid,                                         -- DEC-A41, render_run lands later
  source_variant_idx  int,
  lint_overrides_json jsonb NOT NULL DEFAULT '{}',
  manual_pin          boolean NOT NULL DEFAULT false,
  created_at          timestamptz NOT NULL DEFAULT now(),
  updated_at          timestamptz NOT NULL DEFAULT now(),
  CONSTRAINT uq_entity_version_number UNIQUE (entity_id, version_number)
);
CREATE INDEX entity_version_entity_idx ON entity_version (entity_id);
CREATE INDEX entity_version_status_idx ON entity_version (status);

ALTER TABLE catalog_entity
  ADD CONSTRAINT fk_catalog_entity_current_version
  FOREIGN KEY (current_published_version_id) REFERENCES entity_version(id)
  ON DELETE SET NULL DEFERRABLE INITIALLY DEFERRED;

-- Detail tables — one row per entity, kind-scoped. PK = entity_id.

CREATE TABLE sprite_detail (
  entity_id           bigint PRIMARY KEY REFERENCES catalog_entity(id) ON DELETE CASCADE,
  legacy_sprite_id    bigint UNIQUE,                               -- DEC-A8 transition
  source_uri          text,                                         -- gen://run_id/idx or asset://...
  assets_path         text,                                         -- Assets/Sprites/Generated/...
  pixels_per_unit     int    NOT NULL DEFAULT 100,
  pivot_x             real   NOT NULL DEFAULT 0.5,
  pivot_y             real   NOT NULL DEFAULT 0.5,
  provenance          text   NOT NULL CHECK (provenance IN ('hand','generator')),
  source_run_id       uuid,
  source_variant_idx  int,
  build_fingerprint   text,
  palette_hash        text
);

CREATE TABLE asset_detail (
  entity_id              bigint PRIMARY KEY REFERENCES catalog_entity(id) ON DELETE CASCADE,
  legacy_asset_id        bigint UNIQUE,                                -- DEC-A8 transition: ZoneSubTypeRegistry.subTypeId
  category               text NOT NULL,
  footprint_w            int  NOT NULL DEFAULT 1,
  footprint_h            int  NOT NULL DEFAULT 1,
  placement_mode         text,
  unlocks_after          text,
  has_button             boolean NOT NULL DEFAULT true,
  world_sprite_entity_id        bigint REFERENCES catalog_entity(id) ON DELETE SET NULL,
  button_target_sprite_entity_id  bigint REFERENCES catalog_entity(id) ON DELETE SET NULL,
  button_pressed_sprite_entity_id bigint REFERENCES catalog_entity(id) ON DELETE SET NULL,
  button_disabled_sprite_entity_id bigint REFERENCES catalog_entity(id) ON DELETE SET NULL,
  button_hover_sprite_entity_id   bigint REFERENCES catalog_entity(id) ON DELETE SET NULL
);
CREATE INDEX asset_detail_category_idx ON asset_detail (category);
CREATE INDEX asset_detail_world_sprite_idx ON asset_detail (world_sprite_entity_id)
  WHERE world_sprite_entity_id IS NOT NULL;

CREATE TABLE economy_detail (
  entity_id              bigint PRIMARY KEY REFERENCES catalog_entity(id) ON DELETE CASCADE,
  base_cost_cents        bigint NOT NULL,
  monthly_upkeep_cents   bigint NOT NULL,
  demolition_refund_pct  int    NOT NULL DEFAULT 0
    CHECK (demolition_refund_pct >= 0 AND demolition_refund_pct <= 100),
  construction_ticks     int    NOT NULL DEFAULT 0,
  budget_envelope_id     int,
  cost_catalog_row_id    bigint
);

CREATE TABLE pool_detail (
  entity_id        bigint PRIMARY KEY REFERENCES catalog_entity(id) ON DELETE CASCADE,
  legacy_pool_id   bigint UNIQUE,                                 -- DEC-A8 transition
  primary_subtype  text,
  owner_category   text                                           -- preserved from legacy.owner_category
);

CREATE TABLE pool_member (
  pool_entity_id  bigint NOT NULL REFERENCES catalog_entity(id) ON DELETE CASCADE,
  asset_entity_id bigint NOT NULL REFERENCES catalog_entity(id) ON DELETE RESTRICT,
  weight          int    NOT NULL DEFAULT 1 CHECK (weight > 0),
  PRIMARY KEY (pool_entity_id, asset_entity_id)
);
CREATE INDEX pool_member_asset_idx ON pool_member (asset_entity_id);

-- updated_at triggers (DEC-A38).
CREATE OR REPLACE FUNCTION catalog_touch_updated_at() RETURNS trigger AS $$
BEGIN NEW.updated_at = now(); RETURN NEW; END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_catalog_entity_touch    BEFORE UPDATE ON catalog_entity
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();
CREATE TRIGGER trg_entity_version_touch    BEFORE UPDATE ON entity_version
  FOR EACH ROW EXECUTE FUNCTION catalog_touch_updated_at();

COMMIT;

-- Rollback: tools/scripts/restore-db-snapshot.sh var/db-snapshots/pre-spine-{date}.dump --confirm
```

#### `0022_catalog_detail_link.sql`

Backfill spine + detail rows from existing tables. Read-only against legacy tables; only inserts into new ones.

Logic per legacy table:

1. **`catalog_asset` → `catalog_entity` (kind='asset') + `asset_detail`:**
   - For each row: insert `catalog_entity` with `slug = old.slug`, `display_name = old.display_name`, `retired_at = (status='retired' ? now() : null)`. Insert `entity_version (status='published', version_number=1, params_json='{}')`. Set `current_published_version_id`. Insert `asset_detail` mirroring footprint, placement, unlocks, has_button, category from old.
2. **`catalog_sprite` → `catalog_entity` (kind='sprite') + `sprite_detail`:**
   - Sprites have no slug today. Synthesize: `sprite_{id}_{archetype_id_or_'hand'}` lowercased + sanitized to slug regex. Audit-driven dedupe (Stage 0 §3 collisions resolved manually pre-migration if any).
   - `display_name = path basename`.
   - One published version per sprite.
   - Map fields per Stage 0 audit §8 census.
3. **`catalog_asset_sprite` → flatten into `asset_detail.{slot}_sprite_entity_id`:**
   - Update each `asset_detail` row resolving slot binding via the new entity ids of the corresponding sprites.
4. **`catalog_economy` → `economy_detail`:**
   - 1:1 by `asset_id` → `entity_id` mapping captured during step 1 (use temp lookup table or join through legacy id).
5. **`catalog_spawn_pool` → `catalog_entity` (kind='pool') + `pool_detail`:**
   - `slug = old.slug`. Insert pool_detail with `primary_subtype = old.owner_subtype`. Pool naming collision with asset slug handled via Stage 0 audit §4.
6. **`catalog_pool_member` → `pool_member`:**
   - Resolve via lookup table; preserve weight.

Strategy:
- Wrap entire backfill in `BEGIN ... COMMIT`.
- Use temporary tables `_legacy_asset_map (legacy_id bigint, entity_id bigint)`, `_legacy_sprite_map`, `_legacy_pool_map` to resolve foreign keys cleanly.
- Drop temp tables at end.

Checksum gate: count(legacy) == count(new) per table. Fail loud if mismatch (raises exception, transaction aborts).

#### `0023_catalog_legacy_drop.sql`

Drop legacy tables once spine is verified.

```sql
BEGIN;
DROP TABLE catalog_pool_member;
DROP TABLE catalog_spawn_pool;
DROP TABLE catalog_economy;
DROP TABLE catalog_asset_sprite;
DROP TABLE catalog_sprite;
DROP TABLE catalog_asset;
COMMIT;
```

**Gate:** `validate:catalog-spine` script (next section) must pass before this migration is applied.

### `validate:catalog-spine` script

`tools/scripts/validate-catalog-spine.ts`. Asserts:

1. Every `catalog_entity` row has matching `*_detail` row appropriate for its `kind`.
2. Every `entity_version` row references valid entity_id; version_number monotonic per entity.
3. `current_published_version_id` (when not null) points at a row whose `status='published'` and `entity_id` matches.
4. Slug regex satisfied for every row (defensive, also enforced by CHECK).
5. No `(kind, slug)` collisions.
6. `asset_detail.*_sprite_entity_id` references entities of `kind='sprite'` only.
7. `pool_member.pool_entity_id` references entities of `kind='pool'`; `asset_entity_id` references entities of `kind='asset'`.
8. `catalog_touch_updated_at` trigger present on both spine tables.

Exit non-zero on any failure. Wired into `validate:all`.

### Sequencing (single PR or split)

Recommended split into **3 PRs**:

1. **PR Stage-0** — audit + snapshot tooling. Easy review, no schema risk.
2. **PR Stage-1a** — `0021` + `0022` migrations + validator + integration test (load fixture, run both migrations, run validator, assert row counts match). Legacy tables untouched.
3. **PR Stage-1b** — `0023` legacy drop. Lands only after PR Stage-1a has shipped + been smoke-tested locally + any consumer code (web API routes, MCP catalog tools) has been switched to read from spine.

PR Stage-1b is the higher-risk one because consumer code must already be reading from spine. **Do not merge until Stage 5+ consumers are wired** (or add a compatibility view: see below).

### Compatibility view (optional, recommended)

Between PR Stage-1a and consumer-rewrite stages, ship a backward-compat view:

```sql
CREATE OR REPLACE VIEW catalog_asset_compat AS
SELECT
  ad.legacy_asset_id AS id,                                       -- preserves Zone S subTypeId 0..6
  ad.category       AS category,
  e.slug            AS slug,
  e.display_name    AS display_name,
  CASE WHEN e.retired_at IS NOT NULL THEN 'retired'
       WHEN e.current_published_version_id IS NOT NULL THEN 'published'
       ELSE 'draft' END AS status,
  (SELECT ad2.legacy_asset_id FROM asset_detail ad2
     WHERE ad2.entity_id = e.replaced_by_entity_id) AS replaced_by,
  ad.footprint_w, ad.footprint_h, ad.placement_mode, ad.unlocks_after, ad.has_button,
  e.updated_at
FROM catalog_entity e
JOIN asset_detail ad ON ad.entity_id = e.id
WHERE e.kind = 'asset';
```

Lets existing API/MCP code keep working unmodified during transition. Drop view in PR Stage-1b together with legacy tables.

### Test plan

- **Migration test fixture:** seed legacy schema with representative rows (3 assets, 4 sprites, 2 pools), run `0021 + 0022`, query spine + detail, assert shape.
- **Idempotency:** re-running migrations on already-migrated DB → no-op or clean error (Postgres' `IF NOT EXISTS` covers `0021`; `0022` should detect already-migrated state via row count check + early exit).
- **Roundtrip integration:** read via `catalog_asset_compat` view, compare to legacy snapshot's row counts and key fields.
- **Validator test:** seed deliberate corruption (orphan sprite ref), run `validate:catalog-spine`, assert it exits non-zero with helpful message.

### Acceptance

- `npm run db:migrate` from a fresh `pre-spine` snapshot reaches `0022` cleanly.
- `npm run validate:catalog-spine` exits 0.
- Compat view returns same row count as legacy `catalog_asset` table at snapshot time.
- Stage 1b held for separate ship after consumer wiring.

### Estimated scope

- Migrations: ~250 LOC SQL across 3 files.
- Validator: ~200 LOC TS.
- Test harness: ~150 LOC TS.
- Total: roughly 600 LOC, plus the audit work from Stage 0.

---

## What's NOT in this doc

- Stage 2 onwards (auth, blob resolver, render pipeline) — separate impl docs as we get there.
- DEC-A8 UUID-based external ids — deferred to hosted-readiness stage.
- Detail tables for kinds not yet present in legacy data (`button_detail`, `panel_detail`, `audio_detail`, `token_detail`, `archetype_detail`) — created lazily in their respective stages, no migration cost since no rows exist.

## Next action

Start with Stage 0. Single PR. Once audit report committed and reviewed, evaluate whether to ship Stage 1a immediately or hold for consumer-wiring planning.
