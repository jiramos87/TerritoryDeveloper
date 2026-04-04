# PostgreSQL + Interchange JSON patterns (B1, B3, P5)

**Status:** Durable documentation (migrated from completed **TECH-44a** — see [`BACKLOG.md`](../BACKLOG.md) **§ Completed**). **Merged Postgres program:** **TECH-44** (umbrella — completed — [`BACKLOG.md`](../BACKLOG.md) **§ Completed** **TECH-44**); **extension IDs** and execution order are in **Program extension mapping (E1–E3)** below. **First DB milestone:** **TECH-44b** **§ Completed**; **E1** **§ Completed** (**TECH-44c**).

This document defines **architecture patterns** for **PostgreSQL** and HTTP/sync clients **without** changing player **Save data**. Canonical separation: [`.cursor/specs/persistence-system.md`](../.cursor/specs/persistence-system.md) (**Save**, **Load pipeline order**); [`docs/schemas/README.md`](schemas/README.md) (**Interchange JSON** **`artifact`** / **`schema_version`**).

## Program extension mapping (E1–E3)

Backlog mapping for the completed **TECH-44** program (charter removed after closure). **Open** rows remain in [`BACKLOG.md`](../BACKLOG.md) until shipped or superseded.

| ID | Direction | Backlog row | Durable trace |
|----|-----------|-------------|---------------|
| **E1** | **Repro bundle registry** | **TECH-44c** **§ Completed** | [`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md) (**Dev repro bundle registry**); glossary **Dev repro bundle** |
| **E2** | **Schema validation history** | **TECH-53** | — |
| **E3** | **Agent patch proposal staging** | **TECH-54** | — |

**Editor export registry (completed):** **TECH-55** + **TECH-55b** **§ Completed** — [`BACKLOG.md`](../BACKLOG.md); glossary **Editor export registry**; **Editor** **Reports** → **Postgres** (one **B1** table per export family + **`document jsonb`** full body, **DB-first** with **`tools/reports/`** fallback): migrations **`0004_editor_export_tables.sql`**, **`0005_editor_export_document.sql`**; **`register-editor-export.mjs`** **`--document-file`** ([`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md) **Editor export registry**).

**Phased delivery (core — all § Completed):** **TECH-44a** (patterns in this document) → **TECH-44b** ([`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md), **`db/migrations/`**) → **TECH-44c** (**E1** **`dev_repro_bundle`**). **TECH-53** / **TECH-54** remain out-of-charter follow-ups (see [`BACKLOG.md`](../BACKLOG.md)).

**Out of scope for this program:** Player **Save data** migration; **Markdown** replacement (**TECH-18**); **B2** append-only lines (**TECH-43**).

## Persistence and interchange (guardrails)

| Layer | Authority | Notes |
|-------|-----------|--------|
| Player **Save data** / **Load pipeline** | Unity runtime + **persistence-system** | Do not use **Postgres** as a **Load pipeline** input without a dedicated **BACKLOG** migration issue. |
| **Interchange JSON** | **`artifact`** + optional **`schema_version`**; JSON Schema under `docs/schemas/` | Editor exports, MCP fixtures, init config — not the binary **Save** file format. |
| **Postgres** (**TECH-44b** onward) | **SQL** names and migrations | Keep **SQL** identifiers in a **separate** namespace from interchange field names (see **Naming: SQL vs interchange** below). |

Any **B1** row that mirrors interchange shape inside **JSONB** should document expected top-level keys (`artifact`, `schema_version`, domain body) when the blob round-trips through the same validators as file-based interchange.

## B1 — Row + JSONB

**Intent:** Query and index on **small, stable scalars**; put evolving or nested shapes in **`payload jsonb`** (or equivalent), versioned with an integer compatible with interchange **`schema_version`** semantics where applicable.

**Illustrative example (not a migration mandate):**

```sql
-- Example only — table/column names are product decisions under TECH-44b.
CREATE TABLE city_snapshot (
  id              bigserial PRIMARY KEY,
  save_slot       text NOT NULL,
  player_id       text NOT NULL,
  updated_at      timestamptz NOT NULL,
  interchange_revision int NOT NULL DEFAULT 1,
  payload         jsonb NOT NULL
);
CREATE INDEX city_snapshot_player_slot ON city_snapshot (player_id, save_slot);
```

**Rules of thumb:**

- **Scalars** for: identity, tenancy, **updated_at**, slot keys, flags used in **WHERE** / **JOIN** daily.
- **JSONB** for: document-shaped **interchange** bodies, partial **CellData**-like slices, **FEAT-47** / **FEAT-48** experimental fields — document expected top-level keys when mirroring **Interchange JSON** policy.
- **Split tables** when: secondary indexes on JSON paths become hot, referential integrity between aggregates matters, or row size hurts **VACUUM** / backup SLAs.

## B3 — Idempotent upsert envelope (standard)

**Intent:** One **HTTP** / **message** body shape for “apply this logical patch once” across services. This is a **contract standard**, not a single physical SQL table.

**Illustrative envelope:**

```json
{
  "artifact": "city_patch",
  "schema_version": 1,
  "natural_key": {
    "player_id": "p-001",
    "save_slot": "slot-a"
  },
  "patch": {
    "desirability_sample": [ { "x": 0, "y": 0, "score": 0.42 } ]
  }
}
```

**Normative rules:**

- **`artifact`**: logical model id for the **patch**. Consumers map **`artifact` + `schema_version`** to validation (**JSON Schema**, **Zod**, etc.) — same branching idea as **Interchange JSON** in [`docs/schemas/README.md`](schemas/README.md).
- **`natural_key`**: stable business identity for idempotency (composite unique in the consumer’s store, e.g. `(artifact, schema_version, natural_key)` or tenant-scoped variant). Conflict policy and HTTP **Idempotency-Key** vs body **`natural_key`** are recorded when the first implementing service ships (**TECH-44b** / **TECH-44c**).
- **`patch`**: payload only; no **SQL** or raw **DDL** in interchange JSON.
- **Replay:** duplicate-delivery semantics (**at-least-once** vs **409** / **412**) — document in the implementing issue’s **Decision Log**, not here.

**B2** (append-only **JSON Lines**) remains **[TECH-43](../BACKLOG.md)** backlog-only (no spec until scheduled).

## P5 — Streaming and large documents

**Triggers (any one suggests profiling):**

- Single JSON string **> ~50–100 MB** in Editor export or batch tool, or parse latency / **GC** spikes in Unity or Node.
- Full-grid **interchange** exports, **`terrain_cell_chunk`**-sized batches, or repeated full-document re-parse in a loop.
- **OOM** or **LOH** pressure when loading **`world_snapshot_dev`**-class files in **.NET** or **Node**.

| Approach | When |
|----------|------|
| **NDJSON** / **JSON Lines** | Append-only logs (**TECH-43**). |
| Chunked files | Split by **chunk** bounds (same idea as **`terrain_cell_chunk`**) + manifest. |
| **Utf8JsonReader** (.NET) / streaming **JSON** APIs | Large files on game-adjacent services without full DOM load. |
| **Pagination** / **cursor** HTTP | API returns windows; complements **B3**. |

**Non-goal:** Require streaming on player **Save** / **Load** hot path — **Save data** remains per **persistence-system** until a dedicated migration issue.

## Naming: SQL vs interchange

| Concept | Interchange JSON | SQL / migrations (**TECH-44b**) |
|---------|------------------|----------------------------------|
| Logical model id | **`artifact`** (string) | Prefer **not** a column named `artifact` unless it stores that string literally; consider `interchange_kind`, `document_type`, or encode in **JSONB** only. |
| Consumer branching integer | **`schema_version`** | `interchange_revision`, `payload_version`, or row **`schema_version`** if the team agrees it mirrors interchange semantics. |
| Player save blob | N/A | **GameSaveData** pipeline — not this document. |

## FEAT-47 / FEAT-48 (field names only, no behavior)

- **FEAT-47:** Multipolar **urban growth rings** — optional keys inside **B1** payloads for experiment metadata only (e.g. centroid ids, ring indices). Does **not** change **AUTO** pipeline or **Rings** behavior.
- **FEAT-48:** **Water body** / **surface height (S)** — optional keys for scenario ids and measured samples; **Water map data** and live **Water map** authority remain in Unity **persistence-system** until a future issue explicitly migrates them.

## Related pointers

- [`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md) — first **Postgres** **IA** tables (**TECH-44b** completed — [`BACKLOG.md`](../BACKLOG.md) **§ Completed**); **E1** **`dev_repro_bundle`** registry (**TECH-44c**) with **`artifact`**: `dev_repro_bundle` inside **`payload jsonb`**; migrations under **`db/migrations/`**.
- [`projects/ia-driven-dev-backend-database-value.md`](../projects/ia-driven-dev-backend-database-value.md) — workflow mapping.
- [`projects/TECH-21-json-use-cases-brainstorm.md`](../projects/TECH-21-json-use-cases-brainstorm.md) — **G1** / **G2**, versioning **FAQ**.
- **Glossary:** **Interchange JSON (artifact)**, **Save data**, **Load pipeline order**, **Water map data**, **CellData**, **Postgres interchange patterns (B1, B3, P5)**.
