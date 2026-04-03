# TECH-40 — JSON infrastructure: identity, schemas, CI validation, spec/glossary indexes

> **Issue:** [TECH-40](../../BACKLOG.md)  
> **Status:** In Review  
> **Created:** 2026-04-03  
> **Last updated:** 2026-04-03 (implementation pass)

**Parent program:** [TECH-21](TECH-21.md) · **Depends on:** none · **Feeds:** [TECH-41](TECH-41.md)

## 1. Summary

Establish **machine-readable** rules for **JSON** artifacts in this repo: how each file names its **logical type** (`artifact` / `kind`), when an in-payload **`schema_version`** is required vs when **JSON Schema** **`$id`** / filename is enough, where schemas live, and how **CI** validates fixtures (**P3** — no hot-path runtime cost). Deliver **I1** (reference **spec** index manifest) and **I2** (**glossary** term → **spec** anchor index) as **generated** JSON **without** embedding full Markdown (**TECH-18**).

**Canonical boundaries:** Player **Save data** (`GameSaveData`, **CellData**, **WaterMapData**) and the **Load pipeline** restore order are defined in **persistence-system**; this issue **must not** change on-disk **Save** layout or load ordering. Interchange JSON (fixtures, MCP-adjacent exports, **Geography initialization** params) stays separate—see [`projects/TECH-21-json-use-cases-brainstorm.md`](../../projects/TECH-21-json-use-cases-brainstorm.md) **FAQ** and **E3** layers.

**Editor contrast:** **Unity** **Editor** diagnostics under `tools/reports/` (e.g. agent context JSON) are **gitignored** exports with **`schema_version`** per **unity-development-context** §10; **I1**/**I2** are **spec**/**glossary** derivatives and default to **version-controlled** outputs so **CI** and agents get stable paths unless the **Decision Log** chooses CI-only regeneration.

## 2. Goals and Non-Goals

### 2.1 Goals

1. Document **artifact identity** (required string `artifact` or equivalent convention) for every interchange JSON type; optional integer **`schema_version`** only when consumers need a **single migration branch** (game DB row, **Save-adjacent** export, or MCP)—aligned with the **TECH-21** brainstorm **FAQ**.
2. Check in **JSON Schema** (Draft 2020-12 or team choice recorded in **Decision Log**) for at least one real artifact (pilot) + **good**/**bad** fixtures; **`npm run validate:fixtures`** (or equivalent) in **CI**.
3. Ship **I1**: manifest listing reference **specs** with `key`, `path`, stable `section_id` hints (from headings / slugs), optional `last_generated` timestamp—shape sketched in brainstorm **I1**.
4. Ship **I2**: manifest mapping **glossary** terms → `{ spec_key, anchor }` (English keys only)—shape sketched in brainstorm **I2**.
5. Cross-link **TECH-37** **`compute-lib`** **Zod** schemas: reuse **field names** where the same interchange DTO appears in Node and **Unity**.

### 2.2 Non-Goals

1. Changing **Save data** on disk or **Load pipeline** binary/JSON layout (`GameSaveData` / **CellData** / **WaterMapData** semantics).
2. Replacing **`spec_section`** / file-backed MCP with indexes alone (**TECH-18** remains separate).
3. Runtime schema validation in **release** player builds (Editor/dev optional and off by default).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | I want one command to fail **CI** on invalid fixtures. | Script exits non-zero on bad JSON / schema mismatch. |
| 2 | Agent | I want to resolve a **reference spec** path without reading every `.md`. | **I1** manifest committed or reproducible from a documented `npm run` / script. |
| 3 | Maintainer | I want **glossary** terms mapped to **spec** anchors for linters and reviews. | **I2** manifest committed or reproducible; English keys only. |

## 4. Current State

- **TECH-21** brainstorm records **FAQ** on `schema_version` vs schema file versioning, **I1**/**I2** shapes, and **E3** (interchange vs **Save data** layers).
- **territory-ia** reads **specs** from disk; **I1**/**I2** reduce token churn for **custom** tooling; they complement **`spec_outline`** / **`spec_section`**, not replace parsers (**TECH-18**).
- **Related backlog:** **TECH-24** (parser regression), **TECH-30** (issue id validation in **specs**), **TECH-34** (generated JSON pattern)—touch **Decision Log** when a choice overlaps these tracks.

## 5. Proposed Design

### 5.1 Versioning policy (Decision Log must record final choice)

| Approach | When to use |
|----------|-------------|
| **Schema file only** | **`$id`** URL or `*.v1.schema.json` naming; payload has **no** `schema_version`. |
| **Payload `schema_version`** | DB row, game **Save-adjacent** export, or MCP tool result where **one integer** gates **switch/migrate** logic. |
| **Both** | Allowed: `artifact` + `schema_version` + schema **`$id`** pointing to exact JSON Schema revision. |

**Folder vs DB:** Always include **`artifact`** (logical model name). **Table** name lives at the **SQL** layer; the JSON column or file still carries **`artifact`** so validators and agents know which schema to apply.

### 5.2 **I1** / **I2** generation

- Prefer **Node** script under `tools/` parsing `.cursor/specs/*.md` headings and `glossary.md` tables.
- Output paths: e.g. `tools/mcp-ia-server/data/spec-index.json` and `glossary-index.json` (exact paths in **Decision Log**; backlog lists these as TBD).
- **Pilot artifact** for schemas/fixtures: prefer an interchange type already in motion (e.g. **Geography initialization** / **TECH-39**-adjacent) over **Save data** samples.

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | Spec split from **TECH-21** | Umbrella program **TECH-21** → **TECH-40**/**41**/**42** |
| 2026-04-03 | JSON Schema **Draft 2020-12** + pilot **`artifact`** `geography_init_params` | Pilot schema: `docs/schemas/geography-init-params.v1.schema.json`; fixtures under `docs/schemas/fixtures/`; **AJV** validation in `tools/mcp-ia-server/scripts/validate-fixtures.ts`. |
| 2026-04-03 | **I1**/**I2** committed under `tools/mcp-ia-server/data/` | `spec-index.json`, `glossary-index.json` version-controlled; **CI** runs `generate:ia-indexes -- --check` to prevent drift. |

## 7. Implementation Plan

### Phase A — Policy and layout

- [x] Align **`artifact`** / **`schema_version`** rules with [`projects/TECH-21-json-use-cases-brainstorm.md`](../../projects/TECH-21-json-use-cases-brainstorm.md) **FAQ** (single narrative with this spec’s **§5.1**).
- [x] Choose schema root: `docs/schemas/` vs `tools/schemas/` (per backlog **Files**); add `README` stub listing naming (`$id`, optional `.vN.` in filename).
- [x] Record JSON Schema draft, pilot **artifact** name, and **I1**/**I2** output paths in **§6 Decision Log**.

### Phase B — Pilot schema and fixtures

- [x] Add one **JSON Schema** + ≥1 **good** + ≥1 **bad** fixture under the chosen schema root (bad cases: wrong `artifact`, missing required field, wrong type—document intent in fixture comments or sibling `.md` only if needed).
- [x] Ensure pilot fields that mirror **TECH-37** **Zod** use the same names as **compute-lib** (soft dependency).

### Phase C — **CI** validation

- [x] Add `npm run validate:fixtures` (or equivalent) in repo root `package.json`; wire into existing **CI** workflow that already runs **Node** scripts.
- [x] Script: validate fixtures with a **Node** validator (e.g. `ajv` for Draft 2020-12); exit non-zero on failure.

### Phase D — **I1** + **I2** generators

- [x] Implement generator(s): scan `.cursor/specs/*.md` for headings → **I1** (`key`, `path`, section slugs / ids); scan `glossary.md` → **I2** (term → `spec_key`, `anchor`).
- [x] Add `npm run` target to regenerate; document in `tools/mcp-ia-server/README.md` and [`docs/mcp-ia-server.md`](../../docs/mcp-ia-server.md) (when indexes are consumed or hand-tested).
- [x] Optional: assert **English**-only machine keys in a small lint step (or document in **§8** checklist).

### Phase E — Handoff to **TECH-41**

- [x] Leave a short pointer in **TECH-41** (or parent **TECH-21**) that interchange DTOs should declare **`artifact`** / versioning per this spec.

## 8. Acceptance Criteria

- [x] Versioning policy documented (**Decision Log** + brainstorm link).
- [x] ≥1 JSON Schema + ≥1 good + ≥1 bad fixture; **CI** validates.
- [x] **I1** and **I2** generated artifacts or deterministic generation merged per **§6** (committed vs CI-only resolved).
- [x] **English** only in machine keys and schema `description` / glossary-driven strings where applicable.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
| 1 | `parseBacklogIssue` integration test expected archived **BUG-37** in `BACKLOG.md` | Completed rows moved to **BACKLOG-ARCHIVE** | Retarget test to open **TECH-40** (`tools/mcp-ia-server/tests/parser/backlog-parser.test.ts`) |

## 10. Lessons Learned

- 

## Open Questions (resolve before / during implementation)

**Tooling / IA scope:** This issue does **not** define simulation, **Save data**, or **Load pipeline** behavior. Per [`.cursor/projects/PROJECT-SPEC-STRUCTURE.md`](PROJECT-SPEC-STRUCTURE.md), there are **no** player- or **grid**-logic **Open Questions** here.

Unresolved **delivery** choices (**schema** root path, pilot **artifact**, whether **I1**/**I2** are committed or CI-regenerated only) belong in **§6 Decision Log**, not as faux game-design questions.
