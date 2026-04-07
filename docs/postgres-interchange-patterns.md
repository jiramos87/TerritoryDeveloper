# PostgreSQL + Interchange JSON patterns (B1, B3, P5)

**Status:** Durable architecture reference. **Charter trace** for the **Postgres** + **interchange** program lives in [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md). **Open** follow-ups live in [`BACKLOG.md`](../BACKLOG.md). This file does **not** duplicate backlog ids — use those two files for row-level history.

This document defines **architecture patterns** for **PostgreSQL** and HTTP/sync clients **without** changing player **Save data**. Canonical separation: [`.cursor/specs/persistence-system.md`](../.cursor/specs/persistence-system.md) (**Save**, **Load pipeline order**); [`docs/schemas/README.md`](schemas/README.md) (**Interchange JSON** **`artifact`** / **`schema_version`**).

## Program extension mapping (E1–E3)

| ID | Direction | Trace |
|----|-----------|-------|
| **E1** | **Repro bundle registry** | Shipped — [`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md) (**Dev repro bundle registry**); glossary **Dev repro bundle**; [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md) |
| **E2** | **Schema validation history** | Open — [`BACKLOG.md`](../BACKLOG.md) |
| **E3** | **Agent patch proposal staging** | Open — [`BACKLOG.md`](../BACKLOG.md) |

**Editor export registry:** Shipped — glossary **Editor export registry**; **Editor** **Reports** → **Postgres** (one **B1** table per export family + **`document jsonb`** full body; **Postgres-only**, no workspace fallback): migrations **`0004_editor_export_tables.sql`**, **`0005_editor_export_document.sql`**; **`register-editor-export.mjs`** **`--document-file`** ([`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md)); [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md). **IDE bridge queue:** **`agent_bridge_job`** (**`0008_agent_bridge_job.sql`**) — MCP **`unity_bridge_command`** / **`unity_bridge_get`** + Unity **`AgentBridgeCommandRunner`** (see **postgres-ia-dev-setup**).

**Phased delivery (core):** Patterns in this document → first **Postgres** **IA** DDL ([`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md), **`db/migrations/`**) → **E1** **`dev_repro_bundle`**. Out-of-charter follow-ups remain on [`BACKLOG.md`](../BACKLOG.md).

**Out of scope for this program:** Player **Save data** migration; **Markdown** IA replacement (open backlog); **B2** append-only **JSON Lines** (open backlog).

## Persistence and interchange (guardrails)

| Layer | Authority | Notes |
|-------|-----------|--------|
| Player **Save data** / **Load pipeline** | Unity runtime + **persistence-system** | Do not use **Postgres** as a **Load pipeline** input without a dedicated **BACKLOG** migration issue. |
| **Interchange JSON** | **`artifact`** + optional **`schema_version`**; JSON Schema under `docs/schemas/` | Editor exports, MCP fixtures, init config — not the binary **Save** file format. |
| **Postgres** (dev **IA** tables onward) | **SQL** names and migrations | Keep **SQL** identifiers in a **separate** namespace from interchange field names (see **Naming: SQL vs interchange** below). |

Any **B1** row that mirrors interchange shape inside **JSONB** should document expected top-level keys (`artifact`, `schema_version`, domain body) when the blob round-trips through the same validators as file-based interchange.

## B1 — Row + JSONB

**Intent:** Query and index on **small, stable scalars**; put evolving or nested shapes in **`payload jsonb`** (or equivalent), versioned with an integer compatible with interchange **`schema_version`** semantics where applicable.

**Illustrative example (not a migration mandate):**

```sql
-- Example only — table/column names are product decisions under the Postgres IA migrations.
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
- **JSONB** for: document-shaped **interchange** bodies, partial **CellData**-like slices, **multipolar rings** / **water volume** experimental fields (see [`planned-domain-ideas.md`](planned-domain-ideas.md)) — document expected top-level keys when mirroring **Interchange JSON** policy.
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
- **`natural_key`**: stable business identity for idempotency (composite unique in the consumer’s store, e.g. `(artifact, schema_version, natural_key)` or tenant-scoped variant). Conflict policy and HTTP **Idempotency-Key** vs body **`natural_key`** are recorded when the first implementing service ships (see open [`BACKLOG.md`](../BACKLOG.md) rows).
- **`patch`**: payload only; no **SQL** or raw **DDL** in interchange JSON.
- **Replay:** duplicate-delivery semantics (**at-least-once** vs **409** / **412**) — document in the implementing issue’s **Decision Log**, not here.

**B2** (append-only **JSON Lines**) remains **backlog-only** until scheduled (see [`BACKLOG.md`](../BACKLOG.md)).

## P5 — Streaming and large documents

**Triggers (any one suggests profiling):**

- Single JSON string **> ~50–100 MB** in Editor export or batch tool, or parse latency / **GC** spikes in Unity or Node.
- Full-grid **interchange** exports, **`terrain_cell_chunk`**-sized batches, or repeated full-document re-parse in a loop.
- **OOM** or **LOH** pressure when loading **`world_snapshot_dev`**-class files in **.NET** or **Node**.

| Approach | When |
|----------|------|
| **NDJSON** / **JSON Lines** | Append-only logs (open backlog). |
| Chunked files | Split by **chunk** bounds (same idea as **`terrain_cell_chunk`**) + manifest. |
| **Utf8JsonReader** (.NET) / streaming **JSON** APIs | Large files on game-adjacent services without full DOM load. |
| **Pagination** / **cursor** HTTP | API returns windows; complements **B3**. |

**Non-goal:** Require streaming on player **Save** / **Load** hot path — **Save data** remains per **persistence-system** until a dedicated migration issue.

## Naming: SQL vs interchange

| Concept | Interchange JSON | SQL / migrations (Postgres **IA**) |
|---------|------------------|----------------------------------|
| Logical model id | **`artifact`** (string) | Prefer **not** a column named `artifact` unless it stores that string literally; consider `interchange_kind`, `document_type`, or encode in **JSONB** only. |
| Consumer branching integer | **`schema_version`** | `interchange_revision`, `payload_version`, or row **`schema_version`** if the team agrees it mirrors interchange semantics. |
| Player save blob | N/A | **GameSaveData** pipeline — not this document. |

## Multipolar rings / water volume (field names only, no behavior)

- **Multipolar urban growth rings:** optional keys inside **B1** payloads for experiment metadata only (e.g. centroid ids, ring indices). Does **not** change **AUTO** pipeline or **Rings** behavior.
- **Water body / surface height (S):** optional keys for scenario ids and measured samples; **Water map data** and live **Water map** authority remain in Unity **persistence-system** until a future issue explicitly migrates them.

## Related pointers

- [`docs/postgres-ia-dev-setup.md`](postgres-ia-dev-setup.md) — first **Postgres** **IA** tables (charter trace [`BACKLOG-ARCHIVE.md`](../BACKLOG-ARCHIVE.md)); **E1** **`dev_repro_bundle`** registry with **`artifact`**: `dev_repro_bundle` inside **`payload jsonb`**; migrations under **`db/migrations/`**.
- [`projects/ia-driven-dev-backend-database-value.md`](../projects/ia-driven-dev-backend-database-value.md) — workflow mapping.
- [`projects/json-use-cases-brainstorm.md`](../projects/json-use-cases-brainstorm.md) — **G1** / **G2**, versioning **FAQ**.
- **Glossary:** **Interchange JSON (artifact)**, **Save data**, **Load pipeline order**, **Water map data**, **CellData**, **Postgres interchange patterns (B1, B3, P5)**.
