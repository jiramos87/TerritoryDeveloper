# TECH-44a — Interchange + PostgreSQL patterns (B1, B3, P5)

> **Issue:** [TECH-44a](../../BACKLOG.md)  
> **Program:** [TECH-44](TECH-44.md) (phase A) · **Parent program:** [TECH-21](TECH-21.md) (**Phase C** documentation)  
> **Depends on:** **TECH-41** (completed — [`BACKLOG.md`](../../BACKLOG.md) **§ Completed** **TECH-41**; soft: **TECH-40** for **`artifact`** / **`schema_version`** policy)  
> **Feeds:** **TECH-44b** implementation  
> **Status:** Draft  
> **Created:** 2026-04-03  
> **Last updated:** 2026-04-03

## 1. Summary

Document **architecture patterns** for **PostgreSQL** and HTTP/sync clients **without** shipping the database or changing player **Save data** (`GameSaveData` / **CellData** / **WaterMapData** per **persistence-system**). Patterns must stay consistent with **Interchange JSON (artifact)** (glossary): JSON documents carry logical **`artifact`** and optional **`schema_version`**; **SQL** table and column names are a **separate** namespace — avoid reusing the interchange field name **`artifact`** as a generic DB column name unless it stores exactly that interchange concept (prefer `payload`, `interchange_kind`, or namespaced keys inside **JSONB**).

**Scope:** **Normative prose and examples** for **B1**, **B3**, **P5**, and the **SQL vs interchange** naming table (section 5.4) — **not** migrations (**TECH-44b**) or product features (**E1**–**E3** map to **TECH-44c**, **TECH-53**, **TECH-54** per **[TECH-44](TECH-44.md) section 3**).

Deliverables: **B1** scalar row + **JSONB** payload; **B3** idempotent **patch** **envelope** as a **message contract** (not one physical table); **P5** guidance for incremental **JSON** reading when **Load pipeline**-scale exports or **G1**-style snapshots (e.g. **`world_snapshot_dev`**, [`docs/schemas/world-snapshot-dev.v1.schema.json`](../../docs/schemas/world-snapshot-dev.v1.schema.json)) could grow large. Link illustrative **DTO** fields to **FEAT-47** / **FEAT-48** without implementing gameplay. **B2** (**append-only** JSON lines) remains **TECH-43** backlog-only.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Written **B1** pattern: queryable scalar columns vs **JSONB** blob; criteria for splitting tables vs one document per aggregate.
2. Written **B3** pattern: **envelope** shape reusable across HTTP endpoints, queues, and workers; normative text that it is a **contract standard**, not a single SQL table layout.
3. **P5** guidance: streaming / chunking (**NDJSON**, chunked files, `Utf8JsonReader`-style incremental parse in .NET, or Node line-delimited consumers); when profiling should trigger adoption vs single-string parse.
4. Cross-reference **TECH-44b** milestone tables and future “game row” storage so **SQL** identifiers and **JSONB** inner keys **do not** collide with or obscure **TECH-40** **`artifact`** semantics.

### 2.2 Non-Goals

1. Creating **TECH-43** spec or shipping a log sink.
2. Implementing **FEAT-47** / **FEAT-48** gameplay.
3. Replacing **Markdown**-authoritative specs or file-backed **territory-ia** in **TECH-44b** milestone 1 (**IA** tables only there).
4. Defining concrete **Postgres** deployment or migrations in this issue (**TECH-44b**).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Backend planner | I want a documented row+blob pattern before migrations. | **B1** subsection in **§5** with **English** example + when to split tables. |
| 2 | API designer | I want a standard merge envelope for client patches. | **B3** subsection with example JSON + idempotency / **natural_key** rules. |
| 3 | Performance engineer | I want guidance before full-grid or giant **interchange** files force OOM or GC spikes. | **P5** subsection with triggers + options. |

## 4. Current State

- [`projects/ia-driven-dev-backend-database-value.md`](../../projects/ia-driven-dev-backend-database-value.md) — maps workflows to **B1**/**B3**/**P5**; **E1**–**E3** mapping: **[TECH-44](TECH-44.md) section 3**.
- [`docs/planned-domain-ideas.md`](../../docs/planned-domain-ideas.md) — **FEAT-46**–**FEAT-48**, **TECH-36** alignment.
- **TECH-41** (**§ Completed**): **`terrain_cell_chunk`** / **`world_snapshot_dev`** exports, schemas under [`docs/schemas/README.md`](../../docs/schemas/README.md).
- **Glossary:** **Interchange JSON (artifact)**, **CellData**, **Load pipeline order**.

## 5. Proposed Design

### 5.1 **B1** — Row + **JSONB**

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
- **JSONB** for: document-shaped **interchange** bodies, partial **CellData**-like slices, **FEAT-47**/**FEAT-48** experimental fields — document expected top-level keys (`artifact`, `schema_version`, domain body) when mirroring **Interchange JSON** policy.
- **Split tables** when: secondary indexes on JSON paths become hot, referential integrity between aggregates matters, or row size hurts **VACUUM** / backup SLAs.

### 5.2 **B3** — Idempotent upsert **envelope** (standard)

**Intent:** One **HTTP** / **message** body shape for “apply this logical patch once” across services.

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

- **`artifact`**: logical model id for the **patch**. Consumers map **`artifact` + `schema_version`** to validation (**JSON Schema**, **Zod**, etc.).
- **`natural_key`**: stable business identity for idempotency (define conflict policy in **Decision Log** when implementing).
- **`patch`**: payload only; no **SQL** in interchange JSON.

### 5.3 **P5** — Streaming and large documents

**Triggers (any one suggests profiling):**

- Single JSON string **> ~50–100 MB** in Editor export or batch tool, or parse latency / **GC** spikes in Unity or Node.
- Full-grid **interchange** exports or repeated full-document re-parse in a loop.

| Approach | When |
|----------|------|
| **NDJSON** / **JSON Lines** | Append-only logs (**TECH-43**). |
| Chunked files | Split by **chunk** bounds (same idea as **`terrain_cell_chunk`**) + manifest. |
| **Utf8JsonReader** (.NET) / streaming **JSON** APIs | Large files on game-adjacent services without full DOM load. |
| **Pagination** / **cursor** HTTP | API returns windows; complements **B3**. |

**Non-goal:** Require streaming on player **Save** / **Load** hot path — **Save data** remains per **persistence-system** until a dedicated migration issue.

### 5.4 **Naming: SQL vs interchange**

| Concept | Interchange JSON | SQL / migrations (**TECH-44b**) |
|---------|------------------|----------------------------------|
| Logical model id | **`artifact`** (string) | Prefer **not** a column named `artifact` unless it stores that string literally; consider `interchange_kind`, `document_type`, or encode in **JSONB** only. |
| Consumer branching integer | **`schema_version`** | `interchange_revision`, `payload_version`, or row **`schema_version`** if team agrees it mirrors interchange semantics. |
| Player save blob | N/A | **GameSaveData** pipeline — not this spec. |

### 5.5 **FEAT-47** / **FEAT-48** (field names only, no behavior)

- **FEAT-47:** multipolar **urban growth rings** — optional keys inside **B1** payloads for experiment metadata (e.g. centroid ids, ring indices).
- **FEAT-48:** **water body** / **surface height (S)** — optional keys for scenario ids and measured samples; no live **Water map** authority in Postgres.

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | **B2** → **TECH-43** only | Backlog placeholder without spec |
| 2026-04-03 | Merged from retired **TECH-42** | Superseded by **TECH-44** program |
| 2026-04-11 | Postgres v1 **JSONB** (carry-forward) | **TBD** in **TECH-44b** **Decision Log** — not **game logic** |

## 7. Implementation Plan

### Phase A — Finalize pattern prose

- [ ] Finalize **B1** / **B3** / **P5** and **§5.4**; keep **[TECH-44](TECH-44.md) section 3** synchronized when **E\*** rows change.
- [ ] Optional: one-page [`docs/`](../../docs/) appendix (e.g. `docs/json-interchange-future-patterns.md`) if maintainers want a second entry point.

### Phase B — **TECH-21** umbrella

- [ ] After **§8** acceptance, confirm [TECH-21](TECH-21.md) **Phase C** row points to this spec.

## 8. Acceptance Criteria

- [ ] **B1**, **B3**, **P5**, and **§5.4** naming table complete with **English** examples.
- [ ] **TECH-43** referenced for **B2**; no implication that **Postgres** or **JSONB** is already deployed by this issue alone.
- [ ] Explicit separation: **Interchange JSON** / tools vs **Save data** / **Load pipeline**.
- [ ] **TECH-40** **`artifact`** policy compatible (no contradictory SQL naming without **Decision Log**).
- [ ] **[TECH-44](TECH-44.md) section 3** remains the canonical **E1**–**E3** mapping (link from this spec’s **§4**).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions

**Game logic / simulation:** **N/A**.

**Infrastructure:** Normalized-only vs **JSONB** for **TECH-44b** milestone 1 — **TECH-44b** **Decision Log**.
