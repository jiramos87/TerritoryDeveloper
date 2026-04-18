---
purpose: "TECH-426 — drop legacy param aliases from spec_section / spec_sections / project_spec_journal_*."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/mcp-lifecycle-tools-opus-4-7-audit-master-plan.md"
task_key: "T2.3.1"
---
# TECH-426 — Drop legacy param aliases from spec_section / spec_sections / project_spec_journal_*

> **Issue:** [TECH-426](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-18
> **Last updated:** 2026-04-18

## 1. Summary

Hard-remove legacy param aliases from `spec_section`, `spec_sections`, `project_spec_journal_*` Zod schemas. Aliases (`key`, `doc`, `document_key`, `section_heading`, `section_id`, `heading`, `maxChars`) reject w/ typed `invalid_input` + canonical-name hint. Part of Stage 2.3 exit (alias removal before batch partial-result refactor in TECH-428).

## 2. Goals and Non-Goals

### 2.1 Goals

1. `spec_section({ section_heading: "..." })` returns `{ ok: false, error: { code: "invalid_input", message: "Unknown param 'section_heading'. Canonical: 'section'." } }`.
2. Same treatment for `key` / `doc` / `document_key` (canonical: `spec`), `section_id` / `heading` (canonical: `section`), `maxChars` (canonical: `max_chars`).
3. `spec_sections` per-request element schemas share same alias rejection.
4. `project-spec-journal.ts` journal-search params (`query_text` / `keyword_list` old aliases, if any) canonicalized.

### 2.2 Non-Goals

1. No behavior change on canonical param paths.
2. No batch partial-result refactor — TECH-428.
3. No structured payload changes — TECH-427.

## 4. Current State

### 4.2 Systems map

- `tools/mcp-ia-server/src/tools/spec-section.ts` — Zod schema w/ alias preprocess layer (remove).
- `tools/mcp-ia-server/src/tools/spec-sections.ts` — per-request element Zod schema (remove aliases).
- `tools/mcp-ia-server/src/tools/project-spec-journal.ts` — journal-search param schema (remove aliases).
- Stage 2.1 envelope (`envelope.ts`) + `wrapTool` surface errors as `{ ok: false, error: { code: "invalid_input", ... } }`.

## 5. Proposed Design

### 5.2 Architecture / implementation

Drop `.transform()` alias-preprocess layers from Zod schemas. Explicit `.strict()` on input schemas — unknown keys throw. Catch Zod errors in `wrapTool` path (or tool body); convert to `invalid_input` envelope error w/ hint message naming canonical param.

## 7. Implementation Plan

### Phase 1 — Alias removal

- [ ] Strip alias handling from `spec-section.ts` input schema; switch to `.strict()`.
- [ ] Same for `spec-sections.ts` per-request element schema.
- [ ] Same for `project-spec-journal.ts` journal-search params.
- [ ] Map Zod `unrecognized_keys` error → envelope `invalid_input` w/ canonical-name hint.

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Each alias rejected per tool | Node unit | `cd tools/mcp-ia-server && npm test` | Fixtures: alias input → error envelope w/ canonical hint |
| `validate:all` green | Node | `npm run validate:all` (repo root) | No IA-index regressions |

## 8. Acceptance Criteria

- [ ] All 7 aliases (`key`, `doc`, `document_key`, `section_heading`, `section_id`, `heading`, `maxChars`) reject w/ `invalid_input` across all 3 tools.
- [ ] Canonical params (`spec`, `section`, `max_chars`) unchanged.
- [ ] Unit tests cover each alias per tool.
- [ ] `validate:all` green.

## Open Questions

1. None — tooling only; see §8 Acceptance criteria.
