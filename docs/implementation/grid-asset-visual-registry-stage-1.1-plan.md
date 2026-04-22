# grid-asset-visual-registry — Stage 1.1 Plan Digest

Compiled 2026-04-22 from 6 task spec(s).

---

## §Plan Digest

### §Goal

Add core catalog migration with four tables matching exploration §8.1.

### §Acceptance

- [ ] Four core tables created per exploration.
- [ ] `(category, slug)` UNIQUE enforced.
- [ ] Required cents columns NOT NULL.
- [ ] `updated_at` present; ownership in Decision Log.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| migrate_fresh | clean DB + migrate | exit 0 | shell |
| reject_dup_slug | second insert same pair | SQL error | sql |

### §Examples

| artifact | note |
|----------|------|
| exploration §8.1 | column names source of truth |

### §Mechanical Steps

#### Step 1 — Write core DDL

**Goal:** Land `0011` migration file with catalog tables.

**Edits:**
- Create new SQL migration under `db/migrations/` immediately after `0010` series files: filename pattern `0011_catalog_core.sql`. Body: `CREATE TABLE` / `CREATE INDEX` statements for `catalog_asset`, `catalog_sprite`, `catalog_asset_sprite`, `catalog_economy`; UNIQUE on `(category, slug)`; NOT NULL on required economy cents; `updated_at` on asset.

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Gate non-zero → repair SQL; repeat until exit 0 on fresh database.

**MCP hints:** `plan_digest_verify_paths`

#### Step 2 — Decision log row

**Goal:** Record `updated_at` trigger vs application ownership.

**Edits:**
- `ia/backlog-archive/TECH-612.yaml` (spec removed on Stage 1.1 closeout) — **before**:
```
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |
```
  **after**:
```
| 2026-04-22 | Stub from stage-file | Parent Stage 1.1 | — |
| 2026-04-22 | updated_at | Documented trigger or app-owned | none |
```

**Gate:**
```bash
npm run validate:dead-project-specs
```

**STOP:** Non-zero → fix spec links or paths; re-run.

**MCP hints:** `plan_digest_resolve_anchor`

---
## §Plan Digest

### §Goal

Add indexes and FK actions on core catalog tables for filter and join paths; document delete semantics for soft-retire.

### §Acceptance

- [ ] Indexes on `status` and join keys as per Intent.
- [ ] FK `ON DELETE` / `ON UPDATE` documented in §7 and match exploration retire story.
- [ ] `npm run db:migrate` exit 0 after change.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| migrate_after_indexes | clean DB | exit 0 | shell |
| explain_status | `EXPLAIN` list query | uses index when rows grow | sql |

### §Examples

| policy | note |
|--------|------|
| soft-retire | avoid hard `CASCADE` that drops economy rows unexpectedly |

### §Mechanical Steps

#### Step 1 — Patch core migration

**Goal:** Extend `0011` migration body with indexes + FK clauses aligned to exploration.

**Edits:**
- Open the migration file created for TECH-612 under `db/migrations/` matching prefix `0011_catalog_core` and extension `.sql`. Append or integrate `CREATE INDEX` for `status` and foreign-key columns; set FK actions consistent with exploration §8.2 retire / `replaced_by` narrative.

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Non-zero → fix SQL; re-run until clean DB succeeds.

**MCP hints:** `plan_digest_verify_paths`

#### Step 2 — Document policy in spec

**Goal:** Capture FK behavior in implementation narrative.

**Edits:**
- `ia/backlog-archive/TECH-613.yaml` (spec removed on Stage 1.1 closeout) — **before**:
```
### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Prefer RESTRICT/SET NULL patterns that match soft-delete; document in §7.
```
  **after**:
```
### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Document exact FK actions chosen (RESTRICT / SET NULL / NO ACTION) per table edge; align with soft-retire + `replaced_by` from exploration §8.2.
```

**Gate:**
```bash
npm run validate:dead-project-specs
```

**STOP:** Non-zero → fix markdown; re-run gate.

**MCP hints:** `plan_digest_resolve_anchor`

---
## §Plan Digest

### §Goal

Prove `db:migrate` applies cleanly once and replays safely on the same database state.

### §Acceptance

- [ ] First migrate on empty DB exits 0.
- [ ] Second migrate exits 0 without duplicate-object failures.
- [ ] §Findings records any script/doc gap for CI replay.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| migrate_twice | same DB | both exit 0 | shell |
| fresh_db | new container/DB | exit 0 | shell |

### §Examples

| failure class | signal |
|---------------|--------|
| duplicate type / enum | error text in migrate log |

### §Mechanical Steps

#### Step 1 — Run migrate twice

**Goal:** Capture evidence for idempotency or document gap.

**Edits:**
- `ia/backlog-archive/TECH-614.yaml` (spec removed on Stage 1.1 closeout) — **before**:
```
---
## §Plan Digest

### §Goal

Add `0012` migration for `catalog_spawn_pool` and `catalog_pool_member` with `weight` and FK to assets.

### §Acceptance

- [ ] Pool tables exist; FK targets `catalog_asset` (or chosen PK column).
- [ ] Filename sorts after `0011` migration in `db/migrations/`.
- [ ] `npm run db:migrate` exit 0.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| migrate_chain | DB with 0011 applied | 0012 applies | shell |

### §Examples

| column | type |
|--------|------|
| weight | numeric or int per exploration |

### §Mechanical Steps

#### Step 1 — Author pool migration

**Goal:** Create `0012` SQL file after `0011` file in migration directory.

**Edits:**
- Add new file under `db/migrations/` with name pattern `0012_catalog_spawn_pools.sql` containing `CREATE TABLE` for pool + member, `weight` column, FK to asset table from TECH-612.

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Non-zero → fix ordering or FK target; re-run.

**MCP hints:** `plan_digest_verify_paths`

#### Step 2 — Note deferral if unused

**Goal:** Record empty-table OK for MVP if no consumer yet.

**Edits:**
- `ia/backlog-archive/TECH-615.yaml` (spec removed on Stage 1.1 closeout) — **before**:
```
### Phase 1 — Pool DDL

- [ ] Author `0012` with constraints + indexes for pool lookups.
- [ ] Note deferral if pools unused until Step 2 (tables still exist).
```
  **after**:
```
### Phase 1 — Pool DDL

- [ ] Author `0012` with constraints + indexes for pool lookups.
- [ ] Empty tables acceptable at Stage exit; Step 2 consumer validates writes.
```

**Gate:**
```bash
npm run validate:dead-project-specs
```

**STOP:** Non-zero → fix spec; re-run.

**MCP hints:** `plan_digest_resolve_anchor`

---
## §Plan Digest

### §Goal

Insert seven Zone S reference rows (ids 0–6) aligned with `Assets/Resources/Economy/zone-sub-types.json` vocabulary.

### §Acceptance

- [ ] Seven rows in catalog tables with stable ids 0–6 (or documented mapping if DB uses serial — adjust spec if exploration requires fixed ids via `INSERT` with explicit keys).
- [ ] Slugs / categories match JSON intent.
- [ ] Sprite binds nullable; economy cents valid or defaulted.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| count_zone_s | SQL count | seven rows | sql |
| slug_match | JSON vs DB | names align | manual |

### §Examples

| source | path |
|--------|------|
| JSON vocabulary | `Assets/Resources/Economy/zone-sub-types.json` |

### §Mechanical Steps

#### Step 1 — Author seed artifact

**Goal:** Repeatable seed (SQL block in migration, or script under `tools/`).

**Edits:**
- Add seed SQL or script path referenced from `TECH-616` §7; inserts seven assets + economy rows; uses category vocabulary consistent with exploration and JSON file above.

**Gate:**
```bash
npm run db:migrate
```

**STOP:** Non-zero → fix seed ordering (0011/0012 must apply first); re-run.

**MCP hints:** `plan_digest_verify_paths`

#### Step 2 — Verification note in spec

**Goal:** Record verification query in §Verification or §9.

**Edits:**
- `ia/backlog-archive/TECH-616.yaml` (spec removed on Stage 1.1 closeout) — **before**:
```
---
## §Plan Digest

### §Goal

Either insert minimal pool+member rows referencing seeded assets, or document explicit deferral while keeping pool tables empty.

### §Acceptance

- [ ] Minimal seed exists **or** §Findings states deferral with rationale.
- [ ] No orphan FK references.

### §Test Blueprint

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|
| optional_insert | SQL | one member row **or** deferral note | sql / prose |

### §Examples

| outcome | §Findings text |
|---------|----------------|
| defer | "Pools deferred to Step 2; tables empty." |

### §Mechanical Steps

#### Step 1 — Pool smoke or deferral

**Goal:** Execute optional SQL against `catalog_spawn_pool` / `catalog_pool_member` **or** edit §Findings.

**Edits:**
- If implementing smoke: run SQL (documented in §7) inserting one pool + one member with `weight`, referencing existing `catalog_asset` id from TECH-616 seed.
- If deferring: `ia/backlog-archive/TECH-617.yaml` (spec removed on Stage 1.1 closeout) — **before**:
```

## Final gate

```bash
npm run validate:all
```
