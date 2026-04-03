# TECH-40 — JSON infrastructure: identity, schemas, CI validation, spec/glossary indexes

> **Issue:** [TECH-40](../../BACKLOG.md)  
> **Status:** Draft  
> **Created:** 2026-04-03  
> **Last updated:** 2026-04-03

**Parent program:** [TECH-21](TECH-21.md) · **Depends on:** none · **Feeds:** [TECH-41](TECH-41.md)

## 1. Summary

Establish **machine-readable** rules for **JSON** artifacts in this repo: how each file names its **logical type** (`artifact` / `kind`), when an in-payload **`schema_version`** is required vs when **JSON Schema** **`$id`** / filename is enough, where schemas live, and how **CI** validates fixtures (**P3** — no hot-path runtime cost). Deliver **I1** (spec index manifest) and **I2** (glossary term → spec anchor index) as **generated** JSON **without** embedding full Markdown (**TECH-18**).

## 2. Goals and Non-Goals

### 2.1 Goals

1. Document **artifact identity** (required string `artifact` or equivalent convention) for every interchange JSON type; optional integer **`schema_version`** only when consumers need a **single migration branch** (game, DB, or MCP).
2. Check in **JSON Schema** (Draft 2020-12 or team choice) for at least one real artifact (pilot) + **good**/**bad** fixtures; **`npm run validate:fixtures`** (or equivalent) in **CI**.
3. Ship **I1**: manifest listing reference specs with `key`, `path`, stable `section_id` hints (from headings), optional `last_generated` timestamp.
4. Ship **I2**: manifest mapping **glossary** terms → `{ spec_key, anchor }` (English keys only).
5. Cross-link **TECH-37** **`compute-lib`** **Zod** schemas: reuse **field names** where the same DTO appears in Node and Unity.

### 2.2 Non-Goals

1. Changing **Save data** on disk or **Load pipeline** binary/JSON layout.
2. Replacing **`spec_section`** / file-backed MCP with indexes alone (**TECH-18** remains separate).
3. Runtime schema validation in **release** player builds (Editor/dev optional and off by default).

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Maintainer | I want one command to fail CI on invalid fixtures. | Script exits non-zero on bad JSON. |
| 2 | Agent | I want to resolve a spec path without reading every `.md`. | **I1** manifest committed or reproducible from script. |

## 4. Current State

- **TECH-21** brainstorm records **FAQ** on `schema_version` vs schema file versioning.
- **territory-ia** reads specs from disk; **I1**/**I2** reduce **token** waste for **custom** tooling, not replace MCP parsers.

## 5. Proposed Design

### 5.1 Versioning policy (Decision Log must record final choice)

| Approach | When to use |
|----------|-------------|
| **Schema file only** | **`$id`** URL or `*.v1.schema.json` naming; payload has **no** `schema_version`. |
| **Payload `schema_version`** | DB row, game **Save-adjacent** export, or MCP tool result where **one integer** gates **switch/migrate** logic. |
| **Both** | Allowed: `artifact` + `schema_version` + schema **`$id`** pointing to exact JSON Schema revision. |

**Folder vs DB:** Always include **`artifact`** (logical model name). **Table name** lives at the **SQL** layer; the JSON column or file still carries **`artifact`** so validators and agents know which schema to apply.

### 5.2 **I1** / **I2** generation

- Prefer **Node** script under `tools/` parsing `.cursor/specs/*.md` headings and `glossary.md` tables.
- Output paths: e.g. `tools/mcp-ia-server/data/spec-index.json` and `glossary-index.json` (exact paths in **Decision Log**).

## 6. Decision Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-04-03 | Spec split from **TECH-21** | Umbrella program **TECH-21** → **TECH-40**/**41**/**42** |

## 7. Implementation Plan

- [ ] Keep versioning + **`artifact`** convention aligned with [`projects/TECH-21-json-use-cases-brainstorm.md`](../../projects/TECH-21-json-use-cases-brainstorm.md) **FAQ** (single source) and **Decision Log** here.
- [ ] Add schema directory + pilot schema + fixtures.
- [ ] Wire **CI** / `npm run` validate.
- [ ] Implement **I1** + **I2** generators; document regen command in `README` or `docs/mcp-ia-server.md`.

## 8. Acceptance Criteria

- [ ] Versioning policy documented (**Decision Log** + brainstorm link).
- [ ] ≥1 JSON Schema + ≥1 good + ≥1 bad fixture; **CI** validates.
- [ ] **I1** and **I2** artifacts or deterministic generation merged.
- [ ] **English** only in machine keys and schema `description` glossary terms.

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|
|  |  |  |  |

## 10. Lessons Learned

- 

## Open Questions

- **I1**/**I2** should be **gitignored**. Eventually we will have a database with a single source of truth for the spec and glossary.
