# TECH-21 — JSON use cases (brainstorm + exploration)

**Purpose:** Support the **[TECH-21](../.cursor/projects/TECH-21.md) program** (**TECH-40** → **TECH-41** → **TECH-44a** **§ Completed**) and the completed **TECH-44** program (**TECH-44b**/**c** — [`BACKLOG.md`](../BACKLOG.md) **§ Completed** **TECH-44**; **Program extension mapping** [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md)) with **exploration notes**, a **versioning FAQ**, and **mapping** from ideas to backlog issues. **Not** authoritative game behavior — [`.cursor/specs/glossary.md`](../.cursor/specs/glossary.md) and reference specs win. **B1**/**B3**/**P5** norms: [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md).

**Status:** Living draft — trim or mark **Accepted** / **Deferred** as implementation lands.

**Program specs:** **TECH-40** (infra — completed; [`BACKLOG.md`](../BACKLOG.md) **§ Completed**), **TECH-41** (current payloads — completed; same section), **TECH-44a** (patterns — completed; [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md)), **TECH-44** umbrella (completed — same **§ Completed** section; **TECH-44b**/**c** Postgres milestones). Durable pointers: [`docs/schemas/README.md`](../docs/schemas/README.md), **glossary** **Interchange JSON** / **geography_init_params** / **Postgres interchange patterns (B1, B3, P5)**, [`ARCHITECTURE.md`](../ARCHITECTURE.md).

**Related:** **[TECH-36](../.cursor/projects/TECH-36.md)** program (**TECH-37**–**TECH-39** shares **GeographyInitParams** / **Zod** with MCP); [TECH-38](../.cursor/projects/TECH-38.md) Wave D (geography harness JSON); [TECH-39](../.cursor/projects/TECH-39.md) `geography_init_params_validate`; [`docs/planned-domain-ideas.md`](../docs/planned-domain-ideas.md) (**FEAT-46**–**FEAT-48**); [agent-friendly-tasks-with-territory-ia-context.md](agent-friendly-tasks-with-territory-ia-context.md) (**I1** reduces multi-`spec_section` churn).

---

## FAQ — `schema_version`, files, and databases

**Q: Is `schema_version` mandatory on every JSON?**  
**A:** No. Treat **JSON Schema** as the primary version carrier: **`$id`** with a versioned URL, or a filename such as `geography-init-params.v1.schema.json`. Add an **in-payload** integer **`schema_version`** only when a consumer must branch **without** loading a schema file — typical cases: a **Postgres** row, a **Save-adjacent** export, or an MCP tool that embeds a compact **migration** switch. Bumping an integer on **every** field tweak is unsustainable; instead bump when **breaking** consumers (removed/renamed required fields, semantic change). Non-breaking additive fields often need **only** a schema file revision.

**Q: For folder-based JSON, do we also need a model name?**  
**A:** Yes. Use a stable string **`artifact`** (or **`kind`**) in the payload **or** derive it from path convention (`config/geography-init-params.json` → artifact `geography_init_params`). That is **orthogonal** to **`schema_version`**: the artifact tells **which** schema family applies; the schema file or integer tells **which revision**.

**Q: What about SQL tables?**  
**A:** The **table** name is part of the relational model (e.g. `city_snapshot`). Rows can still store **`artifact`** inside **JSONB** for polymorphic payloads, or use one table per artifact type. **`schema_version`** on the row (or inside **JSONB**) then aligns with migration scripts.

---

## Removed or deferred ideas (not pursued in this brainstorm track)

| ID | Status | Reason |
|----|--------|--------|
| **G3** | Removed from active track | **Save data** migration manifest belongs to a **future** issue when a binary/JSON **Save** format change is scheduled; overlaps **TECH-44b** migrations — do not block **TECH-40**. |
| **I3** | Removed | Backlog cross-index is better handled by **TECH-30** (issue id validation) + optional follow-up; **I1**/**I2** cover IA navigation first. |
| **D1**–**D3** | Deferred to **TECH-16** / **TECH-31** | **Simulation tick** profile JSON and **AUTO systems** ledgers are owned by performance/fixture workstreams; **TECH-41** may **reference** their shapes but does not own the harness. |
| **E1**–**E2** | Deferred | Stable domain ids and normalize-vs-embed policy move to **TECH-44b** / [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) when API surfaces exist; **E3** is enough for **TECH-41** layering story. |

---

## Retained ideas — summary table

| ID | Brief | Primary phase |
|----|--------|----------------|
| **G1** | Read-only world **snapshot** (subset) | **TECH-41** |
| **G2** | Single **cell** / **chunk** JSON (**CellData**-compatible subset) | **TECH-41** |
| **G4** | **Geography initialization** parameters file | **TECH-41** (+ **FEAT-46** roadmap) |
| **I1** | Spec index manifest | **TECH-40** |
| **I2** | Glossary term → anchor index | **TECH-40** |
| **E3** | DTO **layers** (manager ↔ interchange ↔ **CellData**) | **TECH-41** (doc + boundaries) |
| **B1** | Row + **JSONB** pattern | [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) (norms; **TECH-44a** **§ Completed**); **TECH-44b**/**c** (implement) |
| **B2** | Append-only JSON lines | **[TECH-43](../BACKLOG.md)** (backlog only) |
| **B3** | Idempotent **patch** envelope | [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) (contract, not one table) |
| **P1**–**P5** | Load/parse strategies | **TECH-41** / [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) (see below) |

---

## Exploration — **G1** (snapshot)

**Intent:** Export a **read-only** slice of world state for debugging, **TECH-31** capsules, or agent fixtures — **not** a player **Save** file.

**Shape (truncated):**  
`{ "artifact": "world_snapshot", "schema_version": 1, "bounds": { "x0": 0, "y0": 0, "w": 64, "h": 64 }, "cells": [ /* subset */ ], "water_summary": { /* … */ } }`

**Integration:** Editor menu or dev command writes under `tools/reports/` (gitignored by default). Consumers: **Node** validators (**TECH-40**), diff tools, future **MCP** batchmode handoff. **Risk:** keep **HeightMap** / **`Cell.height`** consistent in exported fields; never write back as **Save data** without migration.

**Planned product:** Large **snapshot** exports may feed **FEAT-46**/**FEAT-48** tooling later; start with **small** bounds.

---

## Exploration — **G2** (JSON per **cell** or **chunk**)

**Intent:** One **cell** or one **chunk** of cells in JSON so **external** scripts validate **zone** ids, **water** ids, and **height** against glossary semantics without loading a full map.

**Shape (truncated):**  
`{ "artifact": "cell_chunk", "schema_version": 1, "origin": { "x": 10, "y": 20 }, "cells": [ { "x": 10, "y": 20, "height": 3, "zoneId": null, "waterBodyId": 2 } ] }`

**Integration:** **TECH-38** Wave D and **GridManager**-adjacent **read-only** APIs (no **`gridArray`** escape). Pairs with **TECH-26**-style hygiene. **Alignment:** field names mirror **CellData** / **persistence-system** where applicable.

---

## Exploration — **G4** (**Geography initialization** parameters)

**Intent:** Move **New Game** knobs (seeds, map size, **river** / **lake** / **forest** weights, template ids) into data so **TECH-15** can profile and **FEAT-46** can attach UI later ([`docs/planned-domain-ideas.md`](../docs/planned-domain-ideas.md) §1).

**Shape (truncated):**  
`{ "artifact": "geography_init_params", "schema_version": 1, "seed": 42, "map": { "width": 128, "height": 128 }, "water": { "seaBias": 0.2 }, "rivers": { "enabled": true }, "forest": { "coverageTarget": 0.15 } }`

**Integration:** Loaded **once** at **Geography initialization** (**P1**). **TECH-39** `geography_init_params_validate` and **TECH-37** **Zod** must accept the same shape. **TECH-38** RNG derivation doc lists how **seed** fans out to **procedural** **rivers** and **depression-fill**.

---

## Exploration — **I1** (spec index manifest)

**Intent:** Small JSON listing each **reference spec** (`key`, file `path`, heading `section_id`s) so **custom** tools (and future MCP helpers) resolve files **without** embedding Markdown (**TECH-18**).

**Shape (truncated):**  
`{ "artifact": "spec_index", "schema_version": 1, "specs": [ { "key": "persistence-system", "path": ".cursor/specs/persistence-system.md", "sections": ["save", "load-pipeline"] } ] }`

**Integration:** Generated by `tools/` script (**TECH-40**). Complements **territory-ia** `spec_outline` (file-backed). Helps **agent-friendly** workflows that today chain many `spec_section` calls.

---

## Exploration — **I2** (glossary → anchor)

**Intent:** Map each **glossary** term to a **spec** file + anchor for linters, payload naming review, and **TECH-27** glossary alignment.

**Shape (truncated):**  
`{ "artifact": "glossary_index", "schema_version": 1, "terms": { "CellData": { "spec": "persistence-system", "anchor": "save" } } }`

**Integration:** Generated from `glossary.md` (**TECH-40**). English-only keys (same as MCP `glossary_lookup`).

---

## Exploration — **E3** (layers)

**Intent:** Avoid attaching JSON schemas to **Unity** **internal** serialization; bind schemas to **interchange DTOs** that **managers** map to/from **CellData** and **Save data**.

**Layers (conceptual):**  
1. **Runtime:** **`MonoBehaviour`** / **managers** (scene truth, **GridManager** API).  
2. **Interchange:** versioned DTOs for MCP, `tools/`, **StreamingAssets** config — **schemas** live here.  
3. **Persistence:** **CellData** / **Save data** pipeline per **persistence-system** — change only with migration.

**Integration:** Document in **TECH-41** + optional **`ARCHITECTURE.md`** pointer. **TECH-38** **`GeographyInitParams`** is interchange-layer, not **`Cell`**.

---

## Exploration — **B1** (row + blob/**JSONB**)

**Intent:** **Postgres** pattern: scalar columns for **query** (`player_id`, `save_slot`, `updated_at`, optional top-level **`schema_version`**) + **JSONB** for evolving nested game state snapshots.

**Shape (illustrative):**  
Row: `(id, save_slot, schema_version, updated_at, payload jsonb)` where `payload` holds `{ "artifact": "city_snapshot", ... }`.

**Integration:** Documented in [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md); implemented under **TECH-44b**/**c**. Enables **index** on scalars and **flexible** inner structure.

---

## Exploration — **B2** (append-only JSON lines)

**Intent:** **NDJSON** / JSON Lines for telemetry or sim anomalies — one JSON object per line, optional **`schema_version`** per line.

**Integration:** **Not** part of **TECH-40**–**TECH-41** deliverables beyond a pointer. Tracked as **[TECH-43](../BACKLOG.md)** until a consumer and storage are chosen.

---

## Exploration — **B3** (idempotent upsert)

**Intent:** Standard **envelope** for sync/API **patches**: **natural key** + **`schema_version`** + **`patch`** body so servers merge **explicitly** (no silent field drop).

**Shape (truncated):**  
`{ "artifact": "city_patch", "schema_version": 1, "natural_key": { "player_id": "p1", "city_id": "c7" }, "patch": { "population": 12000 } }`

**Standard vs table:** This is a **message contract** (HTTP body, queue message, or **RPC** payload), **not** a single mandatory table. **SQL** might use `UPDATE ... FROM jsonb` or an **outbox** table storing the same envelope as **JSONB**.

**Integration:** [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) text + examples; implementation with **TECH-44b**.

---

## Exploration — **P1** (parse once)

**Intent:** Read **G4**, static **catalogs**, and **I1**/**I2** (if loaded at tool startup) **once** per boundary (boot, **New Game**, **Load pipeline** phase), then keep **structs** / **ScriptableObjects** / in-memory maps.

**Integration:** **TECH-41** explicitly avoids per-tick file I/O for JSON. **Unity** `JsonUtility`/`System.Text.Json` cost and **GC** spikes drop when not re-parsing every frame.

---

## Exploration — **P2** (catalog as table/array)

**Q: Per tick or per save/load?**  
**A:** **Neither** for the main use case. **P2** targets **static** or **session-static** data: building defs, flora tables, biome weights — loaded **at init** or after **Load pipeline** completes a phase. **Save**/**Load** may **emit** JSON reports **once** per operation for tooling, but **gameplay** does not re-read giant catalogs each **simulation tick**.

**Integration:** **TECH-41**; aligns with **FEAT-46** sharing parameter tables across **Geography initialization** and future authoring UI.

---

## Exploration — **P3** (validation vs performance)

**CI path:** **JSON Schema** / **Zod** validation on fixtures and golden files — **zero** player **runtime** cost.  
**Runtime path:** Optional **Editor** checks only; **release** builds should **trust** shipped data + **CI** unless a **critical** security boundary exists.

**Verdict:** **Worth it** for **CI**; **avoid** heavy validation in **hot** paths.

---

## Exploration — **P4** (`by_id` in file)

**Intent:** Optional top-level map `{ "by_id": { "h1": { ... } } }` for **O(1)** lookup instead of scanning arrays.

**Scope:** Use for **large static catalogs** and selected interchange DTOs — **not** a mandate for **every** future **entity** model. Some entities are naturally **row-oriented** in **DB** without a bundled `by_id` map in JSON.

**Integration:** **TECH-41** for static game data; [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) when **JSONB** documents mirror the same pattern.

---

## Exploration — **P5** (streaming / incremental)

**Intent:** When a single string or `TextAsset` for a **full** grid **snapshot** would **allocate** too much memory or stall **Load pipeline**, use **chunked** files, **NDJSON**, or a **streaming** UTF-8 reader so **partial** structures materialize.

**Triggers:** Profiling shows large allocations or frame spikes on load; **G1** exports beyond modest bounds.

**Integration:** [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md) guidance first; **TECH-41** implements only if **G1** grows large enough to warrant it.

---

## Prioritization dimensions (unchanged)

1. **Risk to Save data** — default **no** on-disk change.  
2. **Glossary / persistence alignment** — names and schema `description`s.  
3. **Tooling reuse** — one validator (**TECH-40**).  
4. **Path to TECH-44b** — **B1**/**B3** documented in [`docs/postgres-interchange-patterns.md`](../docs/postgres-interchange-patterns.md).

---

## Open discussion points

- **G1** default **bounds** and whether **MCP** may request **batchmode** **snapshot** export (**TECH-39** / **TECH-28**).  
- **I1**/**I2**: committed snapshots vs **CI**-only artifacts.  
- **English-only** keys in interchange JSON vs localized **display** strings in separate columns/files.
